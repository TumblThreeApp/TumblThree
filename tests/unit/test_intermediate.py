"""Tests for IntermediateDict and MediaEntry TypedDict contracts."""

from __future__ import annotations

from tumbl4.core.parse.intermediate import (
    IntermediateDict,
    MediaEntry,
    ReblogSource,
)

# ---------------------------------------------------------------------------
# MediaEntry construction
# ---------------------------------------------------------------------------


def test_media_entry_photo_all_fields() -> None:
    entry: MediaEntry = {
        "kind": "photo",
        "url": "https://example.com/image.jpg",
        "width": 1280,
        "height": 720,
        "mime_type": "image/jpeg",
        "alt_text": "A sunset over the ocean",
        "duration_ms": None,
    }
    assert entry["kind"] == "photo"
    assert entry["url"] == "https://example.com/image.jpg"
    assert entry["width"] == 1280
    assert entry["height"] == 720
    assert entry["mime_type"] == "image/jpeg"
    assert entry["alt_text"] == "A sunset over the ocean"
    assert entry["duration_ms"] is None


def test_media_entry_video_with_duration() -> None:
    entry: MediaEntry = {
        "kind": "video",
        "url": "https://example.com/clip.mp4",
        "width": 1920,
        "height": 1080,
        "mime_type": "video/mp4",
        "alt_text": None,
        "duration_ms": 15000,
    }
    assert entry["kind"] == "video"
    assert entry["duration_ms"] == 15000
    assert entry["alt_text"] is None


def test_media_entry_audio_nullable_dimensions() -> None:
    entry: MediaEntry = {
        "kind": "audio",
        "url": "https://example.com/track.mp3",
        "width": None,
        "height": None,
        "mime_type": "audio/mpeg",
        "alt_text": None,
        "duration_ms": 240000,
    }
    assert entry["kind"] == "audio"
    assert entry["width"] is None
    assert entry["height"] is None
    assert entry["duration_ms"] == 240000


def test_media_entry_photo_all_nullable_fields_none() -> None:
    entry: MediaEntry = {
        "kind": "photo",
        "url": "https://example.com/pic.png",
        "width": None,
        "height": None,
        "mime_type": None,
        "alt_text": None,
        "duration_ms": None,
    }
    assert entry["width"] is None
    assert entry["height"] is None
    assert entry["mime_type"] is None
    assert entry["alt_text"] is None
    assert entry["duration_ms"] is None


# ---------------------------------------------------------------------------
# IntermediateDict construction — basic (non-reblog)
# ---------------------------------------------------------------------------


def test_intermediate_basic_photo_post() -> None:
    media: MediaEntry = {
        "kind": "photo",
        "url": "https://example.com/img.jpg",
        "width": 640,
        "height": 480,
        "mime_type": "image/jpeg",
        "alt_text": None,
        "duration_ms": None,
    }
    doc: IntermediateDict = {
        "schema_version": 1,
        "source_format": "api",
        "post_id": "123456789",
        "blog_name": "myblog",
        "post_url": "https://myblog.tumblr.com/post/123456789",
        "post_type": "photo",
        "timestamp_utc": "2024-01-15T10:30:00Z",
        "tags": ["nature", "photography"],
        "is_reblog": False,
        "reblog_source": None,
        "title": None,
        "body_text": None,
        "body_html": None,
        "media": [media],
        "raw_content_blocks": None,
    }
    assert doc["schema_version"] == 1
    assert doc["source_format"] == "api"
    assert doc["post_id"] == "123456789"
    assert doc["blog_name"] == "myblog"
    assert doc["is_reblog"] is False
    assert doc["reblog_source"] is None
    assert len(doc["media"]) == 1
    assert doc["media"][0]["kind"] == "photo"


def test_intermediate_reblog_with_source() -> None:
    source: ReblogSource = {
        "blog_name": "originalblog",
        "post_id": "987654321",
    }
    doc: IntermediateDict = {
        "schema_version": 1,
        "source_format": "npf",
        "post_id": "111222333",
        "blog_name": "rebloggerblog",
        "post_url": "https://rebloggerblog.tumblr.com/post/111222333",
        "post_type": "photo",
        "timestamp_utc": "2024-03-20T08:00:00Z",
        "tags": [],
        "is_reblog": True,
        "reblog_source": source,
        "title": None,
        "body_text": None,
        "body_html": None,
        "media": [],
        "raw_content_blocks": None,
    }
    assert doc["is_reblog"] is True
    assert doc["reblog_source"] is not None
    assert doc["reblog_source"]["blog_name"] == "originalblog"
    assert doc["reblog_source"]["post_id"] == "987654321"


def test_intermediate_empty_tags() -> None:
    doc: IntermediateDict = {
        "schema_version": 1,
        "source_format": "svc",
        "post_id": "444555666",
        "blog_name": "someblog",
        "post_url": "https://someblog.tumblr.com/post/444555666",
        "post_type": "text",
        "timestamp_utc": "2024-06-01T12:00:00Z",
        "tags": [],
        "is_reblog": False,
        "reblog_source": None,
        "title": "My Post",
        "body_text": "Hello world",
        "body_html": "<p>Hello world</p>",
        "media": [],
        "raw_content_blocks": None,
    }
    assert doc["tags"] == []
    assert doc["title"] == "My Post"
    assert doc["body_text"] == "Hello world"
    assert doc["body_html"] == "<p>Hello world</p>"


def test_intermediate_multiple_media_entries() -> None:
    photo1: MediaEntry = {
        "kind": "photo",
        "url": "https://example.com/img1.jpg",
        "width": 800,
        "height": 600,
        "mime_type": "image/jpeg",
        "alt_text": None,
        "duration_ms": None,
    }
    photo2: MediaEntry = {
        "kind": "photo",
        "url": "https://example.com/img2.jpg",
        "width": 1024,
        "height": 768,
        "mime_type": "image/jpeg",
        "alt_text": "Second photo",
        "duration_ms": None,
    }
    doc: IntermediateDict = {
        "schema_version": 1,
        "source_format": "api",
        "post_id": "777888999",
        "blog_name": "photoblog",
        "post_url": "https://photoblog.tumblr.com/post/777888999",
        "post_type": "photo",
        "timestamp_utc": "2024-09-10T18:45:00Z",
        "tags": ["multi", "photo"],
        "is_reblog": False,
        "reblog_source": None,
        "title": None,
        "body_text": None,
        "body_html": None,
        "media": [photo1, photo2],
        "raw_content_blocks": None,
    }
    assert len(doc["media"]) == 2
    assert doc["media"][0]["url"] == "https://example.com/img1.jpg"
    assert doc["media"][1]["alt_text"] == "Second photo"


def test_intermediate_with_raw_content_blocks() -> None:
    blocks: list[dict[str, object]] = [
        {"type": "text", "text": "Hello"},
        {"type": "image", "media": [{"url": "https://example.com/img.jpg"}]},
    ]
    doc: IntermediateDict = {
        "schema_version": 1,
        "source_format": "npf",
        "post_id": "101010101",
        "blog_name": "npfblog",
        "post_url": "https://npfblog.tumblr.com/post/101010101",
        "post_type": "text",
        "timestamp_utc": "2024-11-05T09:15:00Z",
        "tags": ["npf"],
        "is_reblog": False,
        "reblog_source": None,
        "title": None,
        "body_text": None,
        "body_html": None,
        "media": [],
        "raw_content_blocks": blocks,
    }
    assert doc["raw_content_blocks"] is not None
    assert len(doc["raw_content_blocks"]) == 2
    assert doc["raw_content_blocks"][0]["type"] == "text"


# ---------------------------------------------------------------------------
# Schema version and source_format values
# ---------------------------------------------------------------------------


def test_intermediate_schema_version_is_1() -> None:
    doc: IntermediateDict = {
        "schema_version": 1,
        "source_format": "api",
        "post_id": "1",
        "blog_name": "b",
        "post_url": "https://b.tumblr.com/post/1",
        "post_type": "text",
        "timestamp_utc": "2024-01-01T00:00:00Z",
        "tags": [],
        "is_reblog": False,
        "reblog_source": None,
        "title": None,
        "body_text": None,
        "body_html": None,
        "media": [],
        "raw_content_blocks": None,
    }
    assert doc["schema_version"] == 1


def test_intermediate_post_type_video() -> None:
    video: MediaEntry = {
        "kind": "video",
        "url": "https://example.com/vid.mp4",
        "width": 1280,
        "height": 720,
        "mime_type": "video/mp4",
        "alt_text": None,
        "duration_ms": 30000,
    }
    doc: IntermediateDict = {
        "schema_version": 1,
        "source_format": "api",
        "post_id": "202020202",
        "blog_name": "videoblog",
        "post_url": "https://videoblog.tumblr.com/post/202020202",
        "post_type": "video",
        "timestamp_utc": "2024-12-25T00:00:00Z",
        "tags": ["video"],
        "is_reblog": False,
        "reblog_source": None,
        "title": None,
        "body_text": None,
        "body_html": None,
        "media": [video],
        "raw_content_blocks": None,
    }
    assert doc["post_type"] == "video"
    assert doc["media"][0]["kind"] == "video"
    assert doc["media"][0]["duration_ms"] == 30000
