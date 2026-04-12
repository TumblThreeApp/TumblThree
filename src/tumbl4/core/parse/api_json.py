"""V1 API JSON parser for Tumblr's ``/api/read/json`` endpoint.

The V1 API returns a JSONP-wrapped payload of the form::

    var tumblr_api_read = {...};

This module provides three public functions:

* :func:`strip_jsonp` — unwrap the JSONP envelope and return a plain ``dict``.
* :func:`parse_v1_response` — extract the blog name, total post count, and raw
  post list from the top-level response dict.
* :func:`normalize_photo_post` — convert a single raw V1 photo post dict into
  a typed :class:`~tumbl4.core.parse.intermediate.IntermediateDict`.
"""

from __future__ import annotations

import json
from datetime import UTC, datetime
from typing import Any, cast

from tumbl4.core.errors import ParseError
from tumbl4.core.parse.intermediate import IntermediateDict, MediaEntry, ReblogSource

__all__ = [
    "normalize_photo_post",
    "parse_v1_response",
    "strip_jsonp",
]

# JSONP wrapper variable name used by the Tumblr V1 API.
_JSONP_PREFIX = "var tumblr_api_read"


def _as_int(value: object, default: int = 0) -> int:
    """Coerce *value* to int, returning *default* when conversion is impossible."""
    if isinstance(value, int):
        return value
    if isinstance(value, float):
        return int(value)
    if isinstance(value, str):
        try:
            return int(value)
        except ValueError:
            return default
    return default


def _as_str(value: object, default: str = "") -> str:
    """Return *value* as a str, or *default* if value is None/falsy."""
    if value is None:
        return default
    return str(value)


def _as_str_or_none(value: object) -> str | None:
    """Return *value* as a str if truthy, otherwise None."""
    if not value:
        return None
    return str(value)


def strip_jsonp(raw: str) -> dict[str, Any]:
    """Strip the JSONP wrapper from a V1 API response and return the JSON object.

    The V1 API wraps its JSON in a JavaScript variable assignment::

        var tumblr_api_read = {...};

    This function locates the first ``{`` in the string, strips everything
    before it plus the trailing ``};`` (and any surrounding whitespace), then
    parses the remaining JSON.

    If the input contains no ``{`` character or the extracted JSON is invalid,
    a :class:`~tumbl4.core.errors.ParseError` is raised.

    Plain JSON (without the JSONP wrapper) is also accepted and parsed directly.

    Args:
        raw: The raw string returned by the V1 API endpoint.

    Returns:
        The parsed JSON object as a ``dict``.

    Raises:
        ParseError: If the string cannot be parsed as JSON.
    """
    text = raw.strip()

    if not text:
        raise ParseError("Empty response", excerpt=repr(raw[:80]))

    # If the response looks like JSONP, extract just the JSON object.
    if _JSONP_PREFIX in text:
        brace_index = text.find("{")
        if brace_index == -1:
            raise ParseError(
                "JSONP wrapper present but no JSON object found",
                excerpt=repr(text[:80]),
            )
        # The response ends with "};" — drop the trailing semicolon.
        json_text = text[brace_index:].rstrip()
        if json_text.endswith(";"):
            json_text = json_text[:-1].rstrip()
    else:
        json_text = text

    try:
        result: dict[str, Any] = json.loads(json_text)
    except json.JSONDecodeError as exc:
        raise ParseError(
            f"Failed to parse V1 API JSON: {exc}",
            excerpt=repr(json_text[:80]),
        ) from exc

    return result


def parse_v1_response(data: dict[str, Any]) -> tuple[str, int, list[dict[str, Any]]]:
    """Extract the blog name, total post count, and post list from a V1 response.

    Args:
        data: The parsed JSON object from :func:`strip_jsonp`.

    Returns:
        A three-tuple of ``(blog_name, total_posts, posts)`` where:

        * ``blog_name`` is the ``tumblelog.name`` string.
        * ``total_posts`` is the integer value of ``posts-total``
          (which the API sometimes returns as a string).
        * ``posts`` is the raw list of post dicts.
    """
    raw_tumblelog: Any = data.get("tumblelog")
    tumblelog: dict[str, Any] = cast(
        "dict[str, Any]", raw_tumblelog if isinstance(raw_tumblelog, dict) else {}
    )
    blog_name = _as_str(tumblelog.get("name"))

    raw_total = data.get("posts-total", 0)
    total_posts: int
    if isinstance(raw_total, int):
        total_posts = raw_total
    elif isinstance(raw_total, str):
        total_posts = int(raw_total)
    else:
        total_posts = 0

    raw_posts: Any = data.get("posts", [])
    posts: list[dict[str, Any]] = (
        [
            cast("dict[str, Any]", item)
            for item in cast("list[Any]", raw_posts)
            if isinstance(item, dict)
        ]
        if isinstance(raw_posts, list)
        else []
    )

    return blog_name, total_posts, posts


def normalize_photo_post(
    post: dict[str, Any],
    blog_name: str,
    image_size: str = "1280",
) -> IntermediateDict:
    """Convert a raw V1 photo post dict into a normalised :class:`IntermediateDict`.

    For single-photo posts the ``photos`` array is absent or empty; the image
    URL is taken from the top-level ``photo-url-{size}`` field.  For photosets
    the ``photos`` array contains one entry per image, each with its own
    ``photo-url-{size}`` fields.

    Args:
        post: A single post dict from the V1 API ``posts`` array.
        blog_name: The blog name (from ``tumblelog.name``), used to populate
            :attr:`IntermediateDict.blog_name`.
        image_size: The preferred image size suffix, e.g. ``"1280"``, ``"500"``.
            Falls back to ``photo-url-1280`` if the requested size is absent.

    Returns:
        A fully-populated :class:`IntermediateDict` for the post.
    """
    post_id = _as_str(post.get("id"))

    # Prefer url-with-slug; fall back to plain url.
    post_url = _as_str(post.get("url-with-slug") or post.get("url"))

    # Timestamp: default to epoch (0) when missing or zero.
    ts_int = _as_int(post.get("unix-timestamp", 0))
    timestamp_utc = datetime.fromtimestamp(ts_int, tz=UTC).isoformat()

    # Tags: may be absent, null, or a list of strings.
    raw_tags = post.get("tags")
    tags: list[str] = (
        [str(t) for t in cast("list[Any]", raw_tags) if t is not None]
        if isinstance(raw_tags, list)
        else []
    )

    # Reblog detection: present when reblogged-from-name is non-empty.
    reblogged_from_name = _as_str(post.get("reblogged-from-name"))
    reblogged_from_id = _as_str(post.get("reblogged-from-id"))
    is_reblog = bool(reblogged_from_name)
    reblog_source: ReblogSource | None = None
    if is_reblog:
        reblog_source = ReblogSource(
            blog_name=reblogged_from_name,
            post_id=reblogged_from_id,
        )

    # Caption / body HTML.
    body_html: str | None = _as_str_or_none(post.get("photo-caption"))

    # Build media list.
    size_key = f"photo-url-{image_size}"
    fallback_key = "photo-url-1280"

    photos_raw = post.get("photos")
    photos: list[dict[str, Any]] = (
        [
            cast("dict[str, Any]", item)
            for item in cast("list[Any]", photos_raw)
            if isinstance(item, dict)
        ]
        if isinstance(photos_raw, list)
        else []
    )

    media: list[MediaEntry] = []

    if photos:
        # Photoset: each entry in the photos array is one image.
        for photo in photos:
            photo_url = _as_str(photo.get(size_key) or photo.get(fallback_key))
            p_width: int | None = _as_int(photo.get("width")) or None
            p_height: int | None = _as_int(photo.get("height")) or None
            entry: MediaEntry = {
                "kind": "photo",
                "url": photo_url,
                "width": p_width,
                "height": p_height,
                "mime_type": None,
                "alt_text": None,
                "duration_ms": None,
            }
            media.append(entry)
    else:
        # Single photo: URL lives at top-level photo-url-{size}.
        photo_url = _as_str(post.get(size_key) or post.get(fallback_key))
        p_width = _as_int(post.get("width")) or None
        p_height = _as_int(post.get("height")) or None
        single_entry: MediaEntry = {
            "kind": "photo",
            "url": photo_url,
            "width": p_width,
            "height": p_height,
            "mime_type": None,
            "alt_text": None,
            "duration_ms": None,
        }
        media.append(single_entry)

    return IntermediateDict(
        schema_version=1,
        source_format="api",
        post_id=post_id,
        blog_name=blog_name,
        post_url=post_url,
        post_type="photo",
        timestamp_utc=timestamp_utc,
        tags=tags,
        is_reblog=is_reblog,
        reblog_source=reblog_source,
        title=None,
        body_text=None,
        body_html=body_html,
        media=media,
        raw_content_blocks=None,
    )
