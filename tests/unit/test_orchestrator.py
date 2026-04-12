"""Tests for tumbl4.core.orchestrator — crawl pipeline integration."""

from __future__ import annotations

from collections.abc import AsyncIterator
from pathlib import Path
from unittest.mock import patch

import httpx

from tumbl4.core.orchestrator import CrawlResult, run_crawl
from tumbl4.core.parse.intermediate import IntermediateDict, MediaEntry
from tumbl4.core.state.db import StateDb
from tumbl4.models.blog import BlogRef
from tumbl4.models.media import DownloadResult, MediaTask
from tumbl4.models.settings import Settings

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _make_intermediate(post_id: str, blog_name: str = "testblog") -> IntermediateDict:
    """Build a minimal IntermediateDict with one photo media entry."""
    media: MediaEntry = {
        "kind": "photo",
        "url": f"https://example.com/{post_id}.jpg",
        "width": 1280,
        "height": 853,
        "mime_type": "image/jpeg",
        "alt_text": None,
        "duration_ms": None,
    }
    return IntermediateDict(
        schema_version=1,
        source_format="api",
        post_id=post_id,
        blog_name=blog_name,
        post_url=f"https://{blog_name}.tumblr.com/post/{post_id}/slug",
        post_type="photo",
        timestamp_utc="2025-01-01T00:00:00Z",
        tags=["test"],
        is_reblog=False,
        reblog_source=None,
        title=None,
        body_text=None,
        body_html=None,
        media=[media],
        raw_content_blocks=None,
    )


class FakeCrawler:
    """Mock crawler satisfying CrawlerProtocol."""

    def __init__(self, intermediates: list[IntermediateDict]) -> None:
        self._intermediates = intermediates
        self.highest_post_id: int = 0
        self.total_posts: int = len(intermediates)
        self.rate_limited: bool = False

    async def crawl(self) -> AsyncIterator[IntermediateDict]:
        for item in self._intermediates:
            post_id = int(item["post_id"])
            self.highest_post_id = max(self.highest_post_id, post_id)
            yield item


async def _fake_download(task: MediaTask, client: httpx.AsyncClient) -> DownloadResult:
    """Mock download function — always succeeds."""
    return DownloadResult(
        url=task.url,
        post_id=task.post_id,
        filename=f"{task.post_id}_{task.index:02d}.jpg",
        byte_count=1024,
        status="success",
    )


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------


async def test_processes_posts_and_returns_result(tmp_path: Path) -> None:
    """Two posts with 1 media each are crawled successfully."""
    output_dir = tmp_path / "output"
    output_dir.mkdir()

    settings = Settings(output_dir=output_dir, max_concurrent_downloads=1)
    blog = BlogRef(name="testblog", url="https://testblog.tumblr.com/")

    intermediates = [
        _make_intermediate("100", "testblog"),
        _make_intermediate("200", "testblog"),
    ]
    crawler = FakeCrawler(intermediates)

    data_path = tmp_path / "data"
    data_path.mkdir()

    with patch("tumbl4.core.orchestrator.data_dir", return_value=data_path):
        result = await run_crawl(
            settings=settings,
            blog=blog,
            crawler=crawler,
            download_fn=_fake_download,
        )

    assert isinstance(result, CrawlResult)
    assert result.blog_name == "testblog"
    assert result.posts_crawled == 2
    assert result.downloads_success == 2
    assert result.downloads_failed == 0
    assert result.downloads_skipped == 0
    assert result.complete is True

    # Blog output directory was created.
    blog_dir = output_dir / "testblog"
    assert blog_dir.is_dir()

    # Sidecar metadata files were written.
    meta_dir = blog_dir / "_meta"
    assert meta_dir.is_dir()
    sidecars = sorted(meta_dir.glob("*.json"))
    assert len(sidecars) == 2

    # Resume cursor was saved (crawler.highest_post_id > 0, not rate limited).
    db = StateDb(str(data_path / "testblog.db"))
    try:
        row = db.execute(
            "SELECT last_id FROM crawl_state WHERE blog_name = ?", ("testblog",)
        ).fetchone()
        assert row is not None
        assert row[0] == 200
    finally:
        db.close()


async def test_no_resume_flag_skips_cursor_save(tmp_path: Path) -> None:
    """When no_resume=True, the resume cursor is not saved."""
    output_dir = tmp_path / "output"
    output_dir.mkdir()

    settings = Settings(output_dir=output_dir, max_concurrent_downloads=1)
    blog = BlogRef(name="testblog", url="https://testblog.tumblr.com/")

    intermediates = [_make_intermediate("300", "testblog")]
    crawler = FakeCrawler(intermediates)

    data_path = tmp_path / "data"
    data_path.mkdir()

    with patch("tumbl4.core.orchestrator.data_dir", return_value=data_path):
        result = await run_crawl(
            settings=settings,
            blog=blog,
            crawler=crawler,
            download_fn=_fake_download,
            no_resume=True,
        )

    assert result.complete is True

    db = StateDb(str(data_path / "testblog.db"))
    try:
        row = db.execute(
            "SELECT last_id FROM crawl_state WHERE blog_name = ?", ("testblog",)
        ).fetchone()
        assert row is None
    finally:
        db.close()


async def test_rate_limited_crawler_sets_incomplete(tmp_path: Path) -> None:
    """A rate-limited crawler produces complete=False and does not save cursor."""
    output_dir = tmp_path / "output"
    output_dir.mkdir()

    settings = Settings(output_dir=output_dir, max_concurrent_downloads=1)
    blog = BlogRef(name="testblog", url="https://testblog.tumblr.com/")

    intermediates = [_make_intermediate("400", "testblog")]
    crawler = FakeCrawler(intermediates)
    # Simulate rate limiting after yielding posts.
    crawler.rate_limited = True

    data_path = tmp_path / "data"
    data_path.mkdir()

    with patch("tumbl4.core.orchestrator.data_dir", return_value=data_path):
        result = await run_crawl(
            settings=settings,
            blog=blog,
            crawler=crawler,
            download_fn=_fake_download,
        )

    assert result.complete is False
    assert result.posts_crawled == 1

    db = StateDb(str(data_path / "testblog.db"))
    try:
        row = db.execute(
            "SELECT last_id FROM crawl_state WHERE blog_name = ?", ("testblog",)
        ).fetchone()
        assert row is None
    finally:
        db.close()


async def test_skips_already_downloaded_media(tmp_path: Path) -> None:
    """Media that is already in the state DB is skipped, not re-downloaded."""
    output_dir = tmp_path / "output"
    output_dir.mkdir()

    settings = Settings(output_dir=output_dir, max_concurrent_downloads=1)
    blog = BlogRef(name="testblog", url="https://testblog.tumblr.com/")

    intermediates = [_make_intermediate("500", "testblog")]
    crawler = FakeCrawler(intermediates)

    data_path = tmp_path / "data"
    data_path.mkdir()

    # Pre-populate the state DB with the URL hash for the media in post 500.
    pre_task = MediaTask(
        url="https://example.com/500.jpg",
        post_id="500",
        blog_name="testblog",
        index=0,
        output_dir=str(output_dir / "testblog"),
    )
    db = StateDb(str(data_path / "testblog.db"))
    db.record_download(
        url_hash=pre_task.url_hash,
        url=pre_task.url,
        blog_name="testblog",
        post_id="500",
        filename="500_00.jpg",
        byte_count=1024,
        status="success",
    )
    db.commit()
    db.close()

    with patch("tumbl4.core.orchestrator.data_dir", return_value=data_path):
        result = await run_crawl(
            settings=settings,
            blog=blog,
            crawler=crawler,
            download_fn=_fake_download,
        )

    assert result.posts_crawled == 1
    assert result.downloads_skipped == 1
    assert result.downloads_success == 0


async def test_empty_crawl_returns_zero_stats(tmp_path: Path) -> None:
    """A crawler that yields nothing returns a zeroed CrawlResult."""
    output_dir = tmp_path / "output"
    output_dir.mkdir()

    settings = Settings(output_dir=output_dir, max_concurrent_downloads=1)
    blog = BlogRef(name="emptyblog", url="https://emptyblog.tumblr.com/")

    crawler = FakeCrawler([])

    data_path = tmp_path / "data"
    data_path.mkdir()

    with patch("tumbl4.core.orchestrator.data_dir", return_value=data_path):
        result = await run_crawl(
            settings=settings,
            blog=blog,
            crawler=crawler,
            download_fn=_fake_download,
        )

    assert result.posts_crawled == 0
    assert result.downloads_success == 0
    assert result.complete is True
