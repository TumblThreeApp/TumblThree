"""Crawl orchestrator — producer/consumer pipeline for a single blog.

Wires the entire crawl pipeline together:

1. A *crawler* (producer) async-iterates over blog posts and enqueues
   :class:`~tumbl4.models.media.MediaTask` items onto an asyncio queue.
2. *N* download workers (consumers) pull tasks from the queue, skip
   already-downloaded URLs via the per-blog :class:`StateDb`, call the
   download function, record outcomes, and write JSON sidecar metadata
   once all media for a post are complete.
3. After the crawler finishes, sentinel ``None`` values are pushed to
   stop each worker, and the resume cursor is saved when appropriate.

See design spec §5.3.
"""

from __future__ import annotations

import asyncio
from collections import defaultdict
from collections.abc import AsyncIterator, Awaitable, Callable
from dataclasses import dataclass
from pathlib import Path
from typing import Protocol, runtime_checkable

import httpx

import tumbl4
from tumbl4._internal.logging import get_logger
from tumbl4._internal.paths import data_dir
from tumbl4._internal.tasks import spawn
from tumbl4.core.download.file_downloader import download_media
from tumbl4.core.parse.intermediate import IntermediateDict
from tumbl4.core.state.db import StateDb
from tumbl4.core.state.metadata import write_sidecar
from tumbl4.core.state.resume import save_cursor
from tumbl4.models.blog import BlogRef
from tumbl4.models.media import DownloadResult, MediaTask
from tumbl4.models.settings import Settings

__all__ = ["CrawlResult", "CrawlerProtocol", "DownloadFn", "run_crawl"]

_log = get_logger("core.orchestrator")

# ---------------------------------------------------------------------------
# Type aliases & protocols
# ---------------------------------------------------------------------------

DownloadFn = Callable[[MediaTask, httpx.AsyncClient], Awaitable[DownloadResult]]
"""Signature expected by the orchestrator for the pluggable download function."""


@runtime_checkable
class CrawlerProtocol(Protocol):
    """Minimal interface a crawler must satisfy for :func:`run_crawl`."""

    highest_post_id: int
    total_posts: int
    rate_limited: bool

    def crawl(self) -> AsyncIterator[IntermediateDict]: ...


# ---------------------------------------------------------------------------
# Result container
# ---------------------------------------------------------------------------


@dataclass
class CrawlResult:
    """Aggregate outcome of a single blog crawl run."""

    blog_name: str
    posts_crawled: int = 0
    downloads_success: int = 0
    downloads_failed: int = 0
    downloads_skipped: int = 0
    complete: bool = False


# ---------------------------------------------------------------------------
# Download worker
# ---------------------------------------------------------------------------


async def _download_worker(
    *,
    worker_id: int,
    queue: asyncio.Queue[MediaTask | None],
    client: httpx.AsyncClient,
    db: StateDb,
    download_fn: DownloadFn,
    result: CrawlResult,
    post_media: dict[str, list[DownloadResult]],
    post_data: dict[str, IntermediateDict],
    blog_output_dir: Path,
    lock: asyncio.Lock,
) -> None:
    """Consume :class:`MediaTask` items from *queue* until a ``None`` sentinel."""
    while True:
        task = await queue.get()
        if task is None:
            queue.task_done()
            return

        try:
            # Dedup: skip if already downloaded in a previous run.
            if db.is_downloaded(task.url_hash):
                _log.debug(
                    "skipping already-downloaded URL",
                    extra={"url_hash": task.url_hash, "worker": worker_id},
                )
                async with lock:
                    result.downloads_skipped += 1
                    # Skipped media still counts toward post-media completion
                    # so sidecars get written once all media are accounted for.
                    skip_result = DownloadResult(
                        url=task.url,
                        post_id=task.post_id,
                        filename=None,
                        byte_count=0,
                        status="success",
                        error=None,
                    )
                    post_media[task.post_id].append(skip_result)
                    intermediate = post_data.get(task.post_id)
                    if intermediate is not None:
                        expected = len(intermediate["media"])
                        if len(post_media[task.post_id]) >= expected:
                            _write_post_sidecar(
                                intermediate=intermediate,
                                media_results=post_media[task.post_id],
                                blog_output_dir=blog_output_dir,
                                db=db,
                            )
                continue

            dl_result = await download_fn(task, client)

            # Record in state DB (synchronous sqlite — fast enough).
            db.record_download(
                url_hash=task.url_hash,
                url=task.url,
                blog_name=task.blog_name,
                post_id=task.post_id,
                filename=dl_result.filename,
                byte_count=dl_result.byte_count,
                status=dl_result.status,
                error=dl_result.error,
            )

            async with lock:
                if dl_result.status == "success":
                    result.downloads_success += 1
                else:
                    result.downloads_failed += 1

                # Track per-post media completion.
                post_media[task.post_id].append(dl_result)
                intermediate = post_data.get(task.post_id)
                if intermediate is not None:
                    expected = len(intermediate["media"])
                    if len(post_media[task.post_id]) >= expected:
                        _write_post_sidecar(
                            intermediate=intermediate,
                            media_results=post_media[task.post_id],
                            blog_output_dir=blog_output_dir,
                            db=db,
                        )
        except Exception:
            _log.exception(
                "unhandled error in download worker %d",
                worker_id,
            )
        finally:
            queue.task_done()


def _write_post_sidecar(
    *,
    intermediate: IntermediateDict,
    media_results: list[DownloadResult],
    blog_output_dir: Path,
    db: StateDb,
) -> None:
    """Write the JSON sidecar and mark the post complete in the DB."""
    post_id = intermediate["post_id"]
    blog_name = intermediate["blog_name"]

    media_dicts: list[dict[str, object]] = [
        {
            "url": r.url,
            "filename": r.filename,
            "byte_count": r.byte_count,
            "status": r.status,
            "error": r.error,
        }
        for r in media_results
    ]

    reblog_src = intermediate["reblog_source"]
    reblog_dict: dict[str, str] | None = None
    if reblog_src is not None:
        reblog_dict = {
            "blog_name": reblog_src["blog_name"],
            "post_id": reblog_src["post_id"],
        }

    write_sidecar(
        output_dir=blog_output_dir,
        post_id=post_id,
        blog_name=blog_name,
        post_url=intermediate["post_url"],
        post_type=intermediate["post_type"],
        timestamp_utc=intermediate["timestamp_utc"],
        tags=intermediate["tags"],
        is_reblog=intermediate["is_reblog"],
        media_results=media_dicts,
        reblog_source=reblog_dict,
        title=intermediate["title"],
        body_text=intermediate["body_text"],
        body_html=intermediate["body_html"],
    )
    db.mark_post_complete(post_id, blog_name)
    db.commit()

    _log.debug(
        "sidecar written and post marked complete",
        extra={"post_id": post_id, "blog_name": blog_name},
    )


# ---------------------------------------------------------------------------
# Main entry point
# ---------------------------------------------------------------------------


async def run_crawl(
    *,
    settings: Settings,
    blog: BlogRef,
    crawler: CrawlerProtocol,
    download_fn: DownloadFn = download_media,
    no_resume: bool = False,
) -> CrawlResult:
    """Execute the full producer/consumer crawl pipeline for a single blog.

    Parameters
    ----------
    settings:
        Application-wide settings (output dir, concurrency, queue sizes).
    blog:
        The blog to crawl.
    crawler:
        An object satisfying :class:`CrawlerProtocol` — typically a
        :class:`~tumbl4.core.crawl.tumblr_blog.TumblrBlogCrawler`.
    download_fn:
        Pluggable download callable.  Defaults to
        :func:`~tumbl4.core.download.file_downloader.download_media`.
    no_resume:
        When ``True``, skip saving the resume cursor even on a complete crawl.

    Returns
    -------
    CrawlResult
        Aggregate statistics for the crawl run.
    """
    blog_output_dir = settings.output_dir / blog.name
    blog_output_dir.mkdir(parents=True, exist_ok=True)

    # Per-blog state database.
    db_dir = data_dir()
    db_dir.mkdir(parents=True, exist_ok=True)
    db = StateDb(str(db_dir / f"{blog.name}.db"))

    result = CrawlResult(blog_name=blog.name)

    # Shared mutable state protected by an asyncio lock.
    post_media: dict[str, list[DownloadResult]] = defaultdict(list)
    post_data: dict[str, IntermediateDict] = {}
    lock = asyncio.Lock()

    queue: asyncio.Queue[MediaTask | None] = asyncio.Queue(
        maxsize=settings.queue.max_pending_media,
    )

    user_agent = f"tumbl4/{tumbl4.__version__} ({settings.http.user_agent_suffix})"

    async with httpx.AsyncClient(
        follow_redirects=True,
        headers={"User-Agent": user_agent},
    ) as client:
        # Start download workers.
        n_workers = settings.max_concurrent_downloads
        workers: list[asyncio.Task[None]] = []
        for i in range(n_workers):
            task = spawn(
                _download_worker(
                    worker_id=i,
                    queue=queue,
                    client=client,
                    db=db,
                    download_fn=download_fn,
                    result=result,
                    post_media=post_media,
                    post_data=post_data,
                    blog_output_dir=blog_output_dir,
                    lock=lock,
                ),
                name=f"download-worker-{i}",
            )
            workers.append(task)

        # Producer: iterate over crawler output and enqueue media tasks.
        try:
            async for intermediate in crawler.crawl():
                post_id = intermediate["post_id"]
                blog_name = intermediate["blog_name"]
                media_entries = intermediate["media"]

                result.posts_crawled += 1
                post_data[post_id] = intermediate

                if not media_entries:
                    # Post has no media — write sidecar immediately.
                    _write_post_sidecar(
                        intermediate=intermediate,
                        media_results=[],
                        blog_output_dir=blog_output_dir,
                        db=db,
                    )
                    continue

                for idx, entry in enumerate(media_entries):
                    media_task = MediaTask(
                        url=entry["url"],
                        post_id=post_id,
                        blog_name=blog_name,
                        index=idx,
                        output_dir=str(blog_output_dir),
                    )
                    await queue.put(media_task)

        except Exception:
            _log.exception("error during crawl iteration for blog %s", blog.name)

        # Signal workers to stop.
        for _ in range(n_workers):
            await queue.put(None)

        # Wait for all workers to finish.
        await asyncio.gather(*workers)

    # Persist resume cursor if the crawl completed without rate limiting.
    if not crawler.rate_limited and crawler.highest_post_id > 0 and not no_resume:
        save_cursor(db, blog.name, crawler.highest_post_id)

    result.complete = not crawler.rate_limited

    db.close()

    _log.info(
        "crawl finished",
        extra={
            "blog_name": blog.name,
            "posts_crawled": result.posts_crawled,
            "downloads_success": result.downloads_success,
            "downloads_failed": result.downloads_failed,
            "downloads_skipped": result.downloads_skipped,
            "complete": result.complete,
        },
    )

    return result
