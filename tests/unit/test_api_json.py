"""Tests for the V1 API JSON parser (api_json.py)."""

from __future__ import annotations

import json
from pathlib import Path

import pytest

from tumbl4.core.errors import ParseError
from tumbl4.core.parse.api_json import (
    normalize_photo_post,
    parse_v1_response,
    strip_jsonp,
)
from tumbl4.core.parse.intermediate import IntermediateDict

# ---------------------------------------------------------------------------
# Fixture helpers
# ---------------------------------------------------------------------------

FIXTURE_DIR = Path(__file__).parent.parent / "fixtures" / "json"


def load_fixture(name: str) -> dict:  # type: ignore[type-arg]
    """Load a JSON fixture file and return its contents."""
    return json.loads((FIXTURE_DIR / name).read_text())


# ---------------------------------------------------------------------------
# strip_jsonp
# ---------------------------------------------------------------------------


def test_strip_jsonp_removes_wrapper() -> None:
    raw = 'var tumblr_api_read = {"key": "value"};'
    result = strip_jsonp(raw)
    assert result == {"key": "value"}


def test_strip_jsonp_handles_leading_whitespace() -> None:
    raw = '  \n  var tumblr_api_read = {"posts": []};'
    result = strip_jsonp(raw)
    assert result == {"posts": []}


def test_strip_jsonp_handles_trailing_whitespace_and_newlines() -> None:
    raw = 'var tumblr_api_read = {"x": 1};  \n  '
    result = strip_jsonp(raw)
    assert result == {"x": 1}


def test_strip_jsonp_plain_json_passthrough() -> None:
    """If input is already plain JSON (no JSONP wrapper), parse it directly."""
    raw = '{"tumblelog": {"name": "blog"}, "posts": []}'
    result = strip_jsonp(raw)
    assert result["tumblelog"]["name"] == "blog"


def test_strip_jsonp_invalid_json_raises_parse_error() -> None:
    raw = "var tumblr_api_read = {bad json here};"
    with pytest.raises(ParseError) as exc_info:
        strip_jsonp(raw)
    assert exc_info.value.excerpt != ""


def test_strip_jsonp_completely_invalid_raises_parse_error() -> None:
    raw = "not json at all !!!"
    with pytest.raises(ParseError):
        strip_jsonp(raw)


def test_strip_jsonp_empty_string_raises_parse_error() -> None:
    with pytest.raises(ParseError):
        strip_jsonp("")


def test_strip_jsonp_uses_fixture_raw_jsonp() -> None:
    fixture = load_fixture("v1_photo_single.json")
    result = strip_jsonp(fixture["_raw_jsonp"])
    assert "tumblelog" in result
    assert "posts" in result
    assert result["tumblelog"]["name"] == "testblog"


# ---------------------------------------------------------------------------
# parse_v1_response
# ---------------------------------------------------------------------------


def test_parse_v1_response_extracts_blog_name() -> None:
    fixture = load_fixture("v1_photo_single.json")
    blog_name, _, _ = parse_v1_response(fixture["parsed"])
    assert blog_name == "testblog"


def test_parse_v1_response_extracts_total_posts_as_int() -> None:
    fixture = load_fixture("v1_photo_single.json")
    _, total, _ = parse_v1_response(fixture["parsed"])
    assert total == 42
    assert isinstance(total, int)


def test_parse_v1_response_extracts_post_list() -> None:
    fixture = load_fixture("v1_photo_single.json")
    _, _, posts = parse_v1_response(fixture["parsed"])
    assert len(posts) == 1
    assert posts[0]["id"] == "123456789"


def test_parse_v1_response_returns_tuple() -> None:
    fixture = load_fixture("v1_photo_single.json")
    result = parse_v1_response(fixture["parsed"])
    blog_name, total, posts = result
    assert isinstance(blog_name, str)
    assert isinstance(total, int)
    assert isinstance(posts, list)


def test_parse_v1_response_photoset_fixture() -> None:
    fixture = load_fixture("v1_photo_set.json")
    blog_name, total, posts = parse_v1_response(fixture["parsed"])
    assert blog_name == "testblog"
    assert total == 42
    assert len(posts) == 1
    assert posts[0]["id"] == "987654321"


def test_parse_v1_response_posts_total_as_string() -> None:
    """posts-total is a string in the V1 API; must be coerced to int."""
    data = {
        "tumblelog": {"name": "blog"},
        "posts-total": "150",
        "posts": [],
    }
    _, total, _ = parse_v1_response(data)
    assert total == 150


def test_parse_v1_response_posts_total_as_int() -> None:
    """posts-total as an int should also work gracefully."""
    data = {
        "tumblelog": {"name": "blog"},
        "posts-total": 75,
        "posts": [],
    }
    _, total, _ = parse_v1_response(data)
    assert total == 75


# ---------------------------------------------------------------------------
# normalize_photo_post — single photo
# ---------------------------------------------------------------------------


def test_normalize_single_photo_post_id() -> None:
    fixture = load_fixture("v1_photo_single.json")
    post = fixture["parsed"]["posts"][0]
    result: IntermediateDict = normalize_photo_post(post, "testblog")
    assert result["post_id"] == "123456789"


def test_normalize_single_photo_blog_name() -> None:
    fixture = load_fixture("v1_photo_single.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert result["blog_name"] == "testblog"


def test_normalize_single_photo_post_url() -> None:
    fixture = load_fixture("v1_photo_single.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert result["post_url"] == "https://testblog.tumblr.com/post/123456789/a-lovely-photo"


def test_normalize_single_photo_post_type() -> None:
    fixture = load_fixture("v1_photo_single.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert result["post_type"] == "photo"


def test_normalize_single_photo_timestamp_utc() -> None:
    fixture = load_fixture("v1_photo_single.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    # unix-timestamp 1718447400 => 2024-06-15T10:30:00+00:00
    assert result["timestamp_utc"] == "2024-06-15T10:30:00+00:00"


def test_normalize_single_photo_tags() -> None:
    fixture = load_fixture("v1_photo_single.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert result["tags"] == ["photography", "sunset", "ocean"]


def test_normalize_single_photo_not_reblog() -> None:
    fixture = load_fixture("v1_photo_single.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert result["is_reblog"] is False
    assert result["reblog_source"] is None


def test_normalize_single_photo_media_list_has_one_entry() -> None:
    fixture = load_fixture("v1_photo_single.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert len(result["media"]) == 1


def test_normalize_single_photo_media_url_default_1280() -> None:
    fixture = load_fixture("v1_photo_single.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert (
        result["media"][0]["url"]
        == "https://64.media.tumblr.com/abc123def456/tumblr_abcdef1234_1280.jpg"
    )


def test_normalize_single_photo_media_dimensions() -> None:
    fixture = load_fixture("v1_photo_single.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert result["media"][0]["width"] == 1280
    assert result["media"][0]["height"] == 853


def test_normalize_single_photo_media_kind() -> None:
    fixture = load_fixture("v1_photo_single.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert result["media"][0]["kind"] == "photo"


def test_normalize_single_photo_schema_version() -> None:
    fixture = load_fixture("v1_photo_single.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert result["schema_version"] == 1


def test_normalize_single_photo_source_format() -> None:
    fixture = load_fixture("v1_photo_single.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert result["source_format"] == "api"


def test_normalize_single_photo_body_html_from_caption() -> None:
    fixture = load_fixture("v1_photo_single.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert result["body_html"] == "<p>A lovely sunset over the ocean</p>"


def test_normalize_single_photo_raw_content_blocks_none() -> None:
    fixture = load_fixture("v1_photo_single.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert result["raw_content_blocks"] is None


# ---------------------------------------------------------------------------
# normalize_photo_post — custom image_size
# ---------------------------------------------------------------------------


def test_normalize_custom_image_size_500() -> None:
    fixture = load_fixture("v1_photo_single.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog", image_size="500")
    assert (
        result["media"][0]["url"]
        == "https://64.media.tumblr.com/abc123def456/tumblr_abcdef1234_500.jpg"
    )


def test_normalize_custom_image_size_250() -> None:
    fixture = load_fixture("v1_photo_single.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog", image_size="250")
    assert (
        result["media"][0]["url"]
        == "https://64.media.tumblr.com/abc123def456/tumblr_abcdef1234_250.jpg"
    )


def test_normalize_falls_back_to_largest_available_when_size_missing() -> None:
    """If the requested size key is absent, fall back to photo-url-1280."""
    post = {
        "id": "1",
        "url-with-slug": "https://blog.tumblr.com/post/1/slug",
        "type": "photo",
        "unix-timestamp": 0,
        "photo-url-1280": "https://example.com/img_1280.jpg",
        # no photo-url-500
        "width": 100,
        "height": 100,
        "photos": [],
    }
    result = normalize_photo_post(post, "blog", image_size="500")
    assert result["media"][0]["url"] == "https://example.com/img_1280.jpg"


# ---------------------------------------------------------------------------
# normalize_photo_post — photoset (3 images)
# ---------------------------------------------------------------------------


def test_normalize_photoset_media_count() -> None:
    fixture = load_fixture("v1_photo_set.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert len(result["media"]) == 3


def test_normalize_photoset_media_urls() -> None:
    fixture = load_fixture("v1_photo_set.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    urls = [m["url"] for m in result["media"]]
    assert "https://64.media.tumblr.com/set001/tumblr_set001_1280.jpg" in urls
    assert "https://64.media.tumblr.com/set002/tumblr_set002_1280.jpg" in urls
    assert "https://64.media.tumblr.com/set003/tumblr_set003_1280.jpg" in urls


def test_normalize_photoset_media_dimensions() -> None:
    fixture = load_fixture("v1_photo_set.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    # Third photo is portrait: 960x1280
    third = result["media"][2]
    assert third["width"] == 960
    assert third["height"] == 1280


def test_normalize_photoset_all_kind_photo() -> None:
    fixture = load_fixture("v1_photo_set.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert all(m["kind"] == "photo" for m in result["media"])


# ---------------------------------------------------------------------------
# normalize_photo_post — reblog detection
# ---------------------------------------------------------------------------


def test_normalize_reblog_detected() -> None:
    fixture = load_fixture("v1_photo_set.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert result["is_reblog"] is True


def test_normalize_reblog_source_blog_name() -> None:
    fixture = load_fixture("v1_photo_set.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert result["reblog_source"] is not None
    assert result["reblog_source"]["blog_name"] == "originalblog"


def test_normalize_reblog_source_post_id() -> None:
    fixture = load_fixture("v1_photo_set.json")
    post = fixture["parsed"]["posts"][0]
    result = normalize_photo_post(post, "testblog")
    assert result["reblog_source"] is not None
    assert result["reblog_source"]["post_id"] == "111222333"


# ---------------------------------------------------------------------------
# normalize_photo_post — missing tags defaults to []
# ---------------------------------------------------------------------------


def test_normalize_missing_tags_defaults_to_empty_list() -> None:
    post = {
        "id": "1",
        "url-with-slug": "https://blog.tumblr.com/post/1/slug",
        "type": "photo",
        "unix-timestamp": 1000000,
        "photo-url-1280": "https://example.com/img.jpg",
        "width": 640,
        "height": 480,
        "photos": [],
        # no "tags" key at all
    }
    result = normalize_photo_post(post, "blog")
    assert result["tags"] == []


def test_normalize_null_tags_defaults_to_empty_list() -> None:
    post = {
        "id": "1",
        "url-with-slug": "https://blog.tumblr.com/post/1/slug",
        "type": "photo",
        "unix-timestamp": 1000000,
        "photo-url-1280": "https://example.com/img.jpg",
        "width": 640,
        "height": 480,
        "photos": [],
        "tags": None,
    }
    result = normalize_photo_post(post, "blog")
    assert result["tags"] == []


# ---------------------------------------------------------------------------
# normalize_photo_post — missing timestamp defaults to epoch
# ---------------------------------------------------------------------------


def test_normalize_missing_timestamp_defaults_to_epoch() -> None:
    post = {
        "id": "1",
        "url-with-slug": "https://blog.tumblr.com/post/1/slug",
        "type": "photo",
        # no "unix-timestamp"
        "photo-url-1280": "https://example.com/img.jpg",
        "width": 640,
        "height": 480,
        "photos": [],
    }
    result = normalize_photo_post(post, "blog")
    assert result["timestamp_utc"] == "1970-01-01T00:00:00+00:00"


def test_normalize_zero_timestamp_is_epoch() -> None:
    post = {
        "id": "1",
        "url-with-slug": "https://blog.tumblr.com/post/1/slug",
        "type": "photo",
        "unix-timestamp": 0,
        "photo-url-1280": "https://example.com/img.jpg",
        "width": 640,
        "height": 480,
        "photos": [],
    }
    result = normalize_photo_post(post, "blog")
    assert result["timestamp_utc"] == "1970-01-01T00:00:00+00:00"


# ---------------------------------------------------------------------------
# normalize_photo_post — post_url fallback
# ---------------------------------------------------------------------------


def test_normalize_uses_url_when_slug_missing() -> None:
    post = {
        "id": "42",
        "url": "https://blog.tumblr.com/post/42",
        # no "url-with-slug"
        "type": "photo",
        "unix-timestamp": 0,
        "photo-url-1280": "https://example.com/img.jpg",
        "width": 100,
        "height": 100,
        "photos": [],
    }
    result = normalize_photo_post(post, "blog")
    assert result["post_url"] == "https://blog.tumblr.com/post/42"
