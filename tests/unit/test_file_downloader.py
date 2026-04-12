"""Tests for tumbl4.core.download.file_downloader.download_media."""

from __future__ import annotations

from pathlib import Path

import httpx
import respx

from tumbl4.core.download.file_downloader import download_media
from tumbl4.models.media import MediaTask


def _make_task(tmp_path: Path, url: str = "https://64.media.tumblr.com/abc/photo.jpg") -> MediaTask:
    output_dir = tmp_path / "testblog"
    output_dir.mkdir(parents=True, exist_ok=True)
    return MediaTask(
        url=url,
        post_id="12345",
        blog_name="testblog",
        index=0,
        output_dir=str(output_dir),
    )


@respx.mock
async def test_successful_download_file_on_disk(tmp_path: Path) -> None:
    """A successful download writes the file to the output directory."""
    task = _make_task(tmp_path)
    respx.get(task.url).mock(return_value=httpx.Response(200, content=b"fake image bytes"))

    async with httpx.AsyncClient() as client:
        result = await download_media(task, client)

    assert result.status == "success"
    assert result.filename is not None
    final = Path(task.output_dir) / result.filename
    assert final.exists()


@respx.mock
async def test_successful_download_correct_content(tmp_path: Path) -> None:
    """Downloaded file contains exactly the bytes served by the server."""
    payload = b"fake image bytes"
    task = _make_task(tmp_path)
    respx.get(task.url).mock(return_value=httpx.Response(200, content=payload))

    async with httpx.AsyncClient() as client:
        result = await download_media(task, client)

    assert result.status == "success"
    assert result.byte_count == len(payload)
    final = Path(task.output_dir) / result.filename  # type: ignore[arg-type]
    assert final.read_bytes() == payload


@respx.mock
async def test_no_part_file_after_success(tmp_path: Path) -> None:
    """The .part temp file must not exist after a successful download."""
    task = _make_task(tmp_path)
    respx.get(task.url).mock(return_value=httpx.Response(200, content=b"data"))

    async with httpx.AsyncClient() as client:
        result = await download_media(task, client)

    assert result.status == "success"
    output_dir = Path(task.output_dir)
    part_files = list(output_dir.glob("*.part"))
    assert part_files == [], f"unexpected .part files: {part_files}"


@respx.mock
async def test_content_type_reconciliation_png_url_jpeg_response(tmp_path: Path) -> None:
    """A PNG URL served with image/jpeg Content-Type produces a .jpg file."""
    url = "https://64.media.tumblr.com/abc/photo.png"
    task = _make_task(tmp_path, url=url)
    respx.get(url).mock(
        return_value=httpx.Response(
            200,
            content=b"\xff\xd8\xff jpeg bytes",
            headers={"content-type": "image/jpeg"},
        )
    )

    async with httpx.AsyncClient() as client:
        result = await download_media(task, client)

    assert result.status == "success"
    assert result.filename is not None
    assert result.filename.endswith(".jpg"), f"expected .jpg, got {result.filename}"


@respx.mock
async def test_404_returns_failed_result(tmp_path: Path) -> None:
    """A 404 response produces a failed DownloadResult without raising."""
    task = _make_task(tmp_path)
    respx.get(task.url).mock(return_value=httpx.Response(404, content=b"not found"))

    async with httpx.AsyncClient() as client:
        result = await download_media(task, client)

    assert result.status == "failed"
    assert result.error is not None
    assert "404" in result.error
    assert result.byte_count == 0
    assert result.filename is None


@respx.mock
async def test_connection_error_cleans_up_part_file(tmp_path: Path) -> None:
    """A connection error removes the .part file and returns a failed result."""
    task = _make_task(tmp_path)
    respx.get(task.url).mock(side_effect=httpx.ConnectError("connection refused"))

    async with httpx.AsyncClient() as client:
        result = await download_media(task, client)

    assert result.status == "failed"
    assert result.error is not None
    output_dir = Path(task.output_dir)
    part_files = list(output_dir.glob("*.part"))
    assert part_files == [], f"unexpected .part files after error: {part_files}"
