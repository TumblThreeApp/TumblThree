"""End-to-end component tests for the full download pipeline with mocked HTTP.

These tests wire together the real TumblrBlogCrawler, file_downloader, and
run_crawl orchestrator, but with all HTTP requests intercepted by respx.
"""

from __future__ import annotations

import json
from pathlib import Path

import pytest
import respx

from tumbl4.core.crawl.http_client import TumblrHttpClient
from tumbl4.core.crawl.tumblr_blog import TumblrBlogCrawler
from tumbl4.core.orchestrator import CrawlResult, run_crawl
from tumbl4.models.blog import BlogRef
from tumbl4.models.settings import Settings

# ---------------------------------------------------------------------------
# Mock V1 API response — 2 posts: 1 single photo + 1 photoset (2 images)
# ---------------------------------------------------------------------------

_BLOG_RESPONSE = (
    "var tumblr_api_read = "
    + json.dumps(
        {
            "tumblelog": {"title": "Photo Blog", "name": "photoblog"},
            "posts-start": 0,
            "posts-total": "2",
            "posts": [
                {
                    "id": "200",
                    "url-with-slug": "https://photoblog.tumblr.com/post/200",
                    "type": "photo",
                    "unix-timestamp": 1776097800,
                    "tags": ["nature"],
                    "photo-url-1280": "https://64.media.tumblr.com/aaa/photo1.jpg",
                },
                {
                    "id": "100",
                    "url-with-slug": "https://photoblog.tumblr.com/post/100",
                    "type": "photo",
                    "unix-timestamp": 1776011400,
                    "tags": [],
                    "photo-url-1280": "https://64.media.tumblr.com/bbb/photo2.jpg",
                    "photos": [
                        {
                            "caption": "First",
                            "width": 1280,
                            "height": 960,
                            "photo-url-1280": "https://64.media.tumblr.com/bbb/set1.jpg",
                        },
                        {
                            "caption": "Second",
                            "width": 1280,
                            "height": 960,
                            "photo-url-1280": "https://64.media.tumblr.com/ccc/set2.jpg",
                        },
                    ],
                },
            ],
        }
    )
    + ";"
)

_FAKE_JPEG = b"\xff\xd8\xff\xe0fake-jpeg-data"

_MEDIA_URLS = [
    "https://64.media.tumblr.com/aaa/photo1.jpg",
    "https://64.media.tumblr.com/bbb/set1.jpg",
    "https://64.media.tumblr.com/ccc/set2.jpg",
]


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _setup_api_mock(router: respx.Router) -> None:
    """Register the V1 API endpoint on the given respx router."""
    router.get(
        "https://photoblog.tumblr.com/api/read/json",
        params={"debug": "1", "num": "50", "start": "0"},
    ).respond(200, text=_BLOG_RESPONSE)


def _setup_media_mocks(router: respx.Router) -> None:
    """Register all media download endpoints on the given respx router."""
    for url in _MEDIA_URLS:
        router.get(url).respond(
            200,
            content=_FAKE_JPEG,
            headers={"content-type": "image/jpeg"},
        )


async def _run_pipeline(
    tmp_path: Path,
) -> tuple[CrawlResult, Path]:
    """Run the full crawl pipeline and return (CrawlResult, blog_output_dir)."""
    output_dir = tmp_path / "output"
    output_dir.mkdir(parents=True, exist_ok=True)

    settings = Settings(output_dir=output_dir)
    blog = BlogRef.from_input("photoblog")

    http = TumblrHttpClient(settings.http)
    crawler = TumblrBlogCrawler(http, blog, page_size=settings.page_size)

    try:
        result = await run_crawl(
            settings=settings,
            blog=blog,
            crawler=crawler,
            no_resume=True,
        )
    finally:
        await http.aclose()

    blog_output_dir = output_dir / "photoblog"
    return result, blog_output_dir


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------


@respx.mock
async def test_full_pipeline_with_mock_http(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Crawl 2 posts (single + photoset), download 3 files, verify on disk."""
    # Redirect state DB to temp dir so we don't pollute the real XDG location.
    monkeypatch.setattr("tumbl4.core.orchestrator.data_dir", lambda: tmp_path / "state")

    _setup_api_mock(respx.mock)
    _setup_media_mocks(respx.mock)

    result, blog_output_dir = await _run_pipeline(tmp_path)

    # Pipeline result assertions.
    assert result.posts_crawled == 2
    assert result.downloads_success == 3
    assert result.complete is True

    # 3 .jpg files on disk in the blog output directory.
    jpg_files = sorted(blog_output_dir.glob("*.jpg"))
    assert len(jpg_files) == 3, f"Expected 3 .jpg files, got: {jpg_files}"

    # Each file contains the fake JPEG data.
    for f in jpg_files:
        assert f.read_bytes() == _FAKE_JPEG

    # 2 .json sidecar files in the _meta/ subdirectory.
    meta_dir = blog_output_dir / "_meta"
    json_files = sorted(meta_dir.glob("*.json"))
    assert len(json_files) == 2, f"Expected 2 .json sidecars, got: {json_files}"

    # Sidecar filenames should correspond to post IDs.
    sidecar_names = {f.stem for f in json_files}
    assert sidecar_names == {"100", "200"}


@respx.mock
async def test_dedup_skips_already_downloaded(
    tmp_path: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """Second run skips all downloads via dedup; no new files are written."""
    monkeypatch.setattr("tumbl4.core.orchestrator.data_dir", lambda: tmp_path / "state")

    _setup_api_mock(respx.mock)
    _setup_media_mocks(respx.mock)

    # --- First run: 3 downloads succeed ---
    result1, blog_output_dir = await _run_pipeline(tmp_path)
    assert result1.posts_crawled == 2
    assert result1.downloads_success == 3
    assert result1.downloads_skipped == 0

    # --- Second run: same settings, same output dir ---
    # Re-register mocks since respx clears between @respx.mock invocations
    # within the same test function they persist.
    _setup_api_mock(respx.mock)
    _setup_media_mocks(respx.mock)

    result2, _ = await _run_pipeline(tmp_path)
    assert result2.posts_crawled == 2
    assert result2.downloads_success == 0
    assert result2.downloads_skipped == 3

    # Still only 3 .jpg files (no duplicates created).
    jpg_files = sorted(blog_output_dir.glob("*.jpg"))
    assert len(jpg_files) == 3
