"""Tests for TumblrBlogCrawler (V1 API public blog crawler)."""

from __future__ import annotations

import json

import respx

from tumbl4.core.crawl.http_client import TumblrHttpClient
from tumbl4.core.crawl.tumblr_blog import TumblrBlogCrawler
from tumbl4.models.blog import BlogRef
from tumbl4.models.settings import HttpSettings

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

_BLOG = "testblog"
_BASE_URL = f"https://{_BLOG}.tumblr.com/api/read/json"
_PARAMS = {"debug": "1", "num": "50", "start": "0"}


def _make_response(posts: list[dict], total: int = 0) -> str:  # type: ignore[type-arg]
    """Build a JSONP V1 API response string."""
    payload = {
        "tumblelog": {"name": _BLOG},
        "posts-total": total,
        "posts": posts,
    }
    return "var tumblr_api_read = " + json.dumps(payload) + ";"


def _photo_post(post_id: int, url_suffix: str = "a-photo") -> dict:  # type: ignore[type-arg]
    """Return a minimal V1 photo post dict."""
    return {
        "id": str(post_id),
        "type": "photo",
        "url-with-slug": f"https://{_BLOG}.tumblr.com/post/{post_id}/{url_suffix}",
        "unix-timestamp": 1000000,
        "photo-url-1280": f"https://example.com/{post_id}.jpg",
        "width": 1280,
        "height": 853,
        "tags": [],
        "photos": [],
    }


def _regular_post(post_id: int) -> dict:  # type: ignore[type-arg]
    """Return a minimal V1 'regular' (text) post dict."""
    return {
        "id": str(post_id),
        "type": "regular",
        "url-with-slug": f"https://{_BLOG}.tumblr.com/post/{post_id}/text",
        "unix-timestamp": 1000000,
        "regular-body": "<p>Some text</p>",
        "tags": [],
    }


async def _collect(crawler: TumblrBlogCrawler) -> list:  # type: ignore[type-arg]
    """Drain the crawler generator into a list, always closing the http client."""
    return [p async for p in crawler.crawl()]


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------


@respx.mock
async def test_crawl_yields_photo_posts() -> None:
    """Two photo posts on one page are both yielded."""
    posts = [_photo_post(300), _photo_post(200)]
    respx.get(_BASE_URL, params=_PARAMS).respond(200, text=_make_response(posts, total=2))

    http = TumblrHttpClient(HttpSettings())
    blog = BlogRef.from_input(_BLOG)
    crawler = TumblrBlogCrawler(http, blog, page_size=50)
    try:
        results = await _collect(crawler)
    finally:
        await http.aclose()

    assert len(results) == 2
    post_ids = {r["post_id"] for r in results}
    assert post_ids == {"300", "200"}


@respx.mock
async def test_highest_post_id_tracked() -> None:
    """highest_post_id reflects the largest id seen across the page."""
    posts = [_photo_post(500), _photo_post(100)]
    respx.get(_BASE_URL, params=_PARAMS).respond(200, text=_make_response(posts, total=2))

    http = TumblrHttpClient(HttpSettings())
    blog = BlogRef.from_input(_BLOG)
    crawler = TumblrBlogCrawler(http, blog, page_size=50)
    try:
        await _collect(crawler)
    finally:
        await http.aclose()

    assert crawler.highest_post_id == 500


@respx.mock
async def test_skips_posts_below_last_id() -> None:
    """Posts with id <= last_id are skipped; only the post above the fence is yielded."""
    posts = [_photo_post(200), _photo_post(100)]
    respx.get(_BASE_URL, params=_PARAMS).respond(200, text=_make_response(posts, total=2))

    http = TumblrHttpClient(HttpSettings())
    blog = BlogRef.from_input(_BLOG)
    crawler = TumblrBlogCrawler(http, blog, page_size=50, last_id=150)
    try:
        results = await _collect(crawler)
    finally:
        await http.aclose()

    assert len(results) == 1
    assert results[0]["post_id"] == "200"


@respx.mock
async def test_empty_blog_yields_nothing() -> None:
    """A blog with no posts yields zero IntermediateDicts."""
    respx.get(_BASE_URL, params=_PARAMS).respond(200, text=_make_response([], total=0))

    http = TumblrHttpClient(HttpSettings())
    blog = BlogRef.from_input(_BLOG)
    crawler = TumblrBlogCrawler(http, blog, page_size=50)
    try:
        results = await _collect(crawler)
    finally:
        await http.aclose()

    assert results == []


@respx.mock
async def test_skips_non_photo_posts() -> None:
    """A 'regular' type post is filtered out; nothing is yielded."""
    posts = [_regular_post(300)]
    respx.get(_BASE_URL, params=_PARAMS).respond(200, text=_make_response(posts, total=1))

    http = TumblrHttpClient(HttpSettings())
    blog = BlogRef.from_input(_BLOG)
    crawler = TumblrBlogCrawler(http, blog, page_size=50)
    try:
        results = await _collect(crawler)
    finally:
        await http.aclose()

    assert results == []


@respx.mock
async def test_reports_total_posts() -> None:
    """total_posts is set from the API response's posts-total field."""
    posts = [_photo_post(100)]
    respx.get(_BASE_URL, params=_PARAMS).respond(200, text=_make_response(posts, total=42))

    http = TumblrHttpClient(HttpSettings())
    blog = BlogRef.from_input(_BLOG)
    crawler = TumblrBlogCrawler(http, blog, page_size=50)
    try:
        await _collect(crawler)
    finally:
        await http.aclose()

    assert crawler.total_posts == 42
