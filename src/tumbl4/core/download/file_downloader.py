"""Streaming media file downloader.

Downloads a single :class:`~tumbl4.models.media.MediaTask` using an
already-open :class:`httpx.AsyncClient`.  The file is written to a
``.part`` temporary path and atomically renamed to its final name only after
``os.fsync`` confirms the bytes are on disk.

This module **never raises** — every outcome is returned as a
:class:`~tumbl4.models.media.DownloadResult`.
"""

from __future__ import annotations

import contextlib
import os
from pathlib import Path

import httpx

from tumbl4._internal.logging import get_logger
from tumbl4.core.download.content_type import reconcile_extension
from tumbl4.models.media import DownloadResult, MediaTask

_log = get_logger("core.download.file_downloader")

_CHUNK_SIZE: int = 64 * 1024  # 64 KiB


async def download_media(task: MediaTask, client: httpx.AsyncClient) -> DownloadResult:
    """Download *task* to disk and return the outcome.

    The caller is responsible for opening and closing *client*.  This
    function will not close it.

    Parameters
    ----------
    task:
        Describes the URL, destination directory, post id, and index.
    client:
        An already-open :class:`httpx.AsyncClient` used for the request.

    Returns
    -------
    DownloadResult
        ``status="success"`` with byte count on success, or
        ``status="failed"`` with an ``error`` message on any failure.
        The function **never raises**.
    """
    url = task.url
    output_dir = Path(task.output_dir)
    part_path: Path | None = None

    try:
        async with client.stream("GET", url) as response:
            _HTTP_CLIENT_ERROR = 400
            if response.status_code >= _HTTP_CLIENT_ERROR:
                _log.warning(
                    "HTTP %d for %s",
                    response.status_code,
                    url,
                )
                return DownloadResult(
                    url=url,
                    post_id=task.post_id,
                    filename=None,
                    byte_count=0,
                    status="failed",
                    error=f"HTTP {response.status_code}",
                )

            content_type: str | None = response.headers.get("content-type")
            url_filename = task.filename
            resolved_filename = reconcile_extension(url_filename, content_type)

            final_path = output_dir / resolved_filename
            part_path = output_dir / f"{resolved_filename}.part"

            byte_count = 0
            with part_path.open("wb") as fh:
                async for chunk in response.aiter_bytes(chunk_size=_CHUNK_SIZE):
                    fh.write(chunk)
                    byte_count += len(chunk)
                os.fsync(fh.fileno())

            os.rename(part_path, final_path)
            part_path = None  # rename succeeded; no cleanup needed

            _log.debug("downloaded %s (%d bytes)", resolved_filename, byte_count)
            return DownloadResult(
                url=url,
                post_id=task.post_id,
                filename=resolved_filename,
                byte_count=byte_count,
                status="success",
            )

    except Exception as exc:
        _log.error("download failed for %s: %s", url, exc)
        if part_path is not None and part_path.exists():
            with contextlib.suppress(OSError):
                part_path.unlink()
        return DownloadResult(
            url=url,
            post_id=task.post_id,
            filename=None,
            byte_count=0,
            status="failed",
            error=str(exc),
        )
