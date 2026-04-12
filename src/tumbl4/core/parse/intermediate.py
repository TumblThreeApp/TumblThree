"""TypedDicts defining the uniform shape all parsers emit.

Every parser (API, SVC, NPF) converts its raw payload into an
:class:`IntermediateDict`.  Downstream code — validation, deduplication, and
storage — never sees raw API fields.  See design spec §5.7.
"""

from __future__ import annotations

from typing import Literal, TypedDict

__all__ = [
    "IntermediateDict",
    "MediaEntry",
    "ReblogSource",
]


class ReblogSource(TypedDict):
    """Origin of a reblogged post."""

    blog_name: str
    post_id: str


class MediaEntry(TypedDict):
    """A single media asset attached to a post."""

    kind: Literal["photo", "video", "audio"]
    url: str
    width: int | None
    height: int | None
    mime_type: str | None
    alt_text: str | None
    duration_ms: int | None


class IntermediateDict(TypedDict):
    """Normalised representation of a Tumblr post as emitted by all parsers.

    ``schema_version`` is currently ``1``; increment it when the shape changes
    in a backward-incompatible way.
    """

    schema_version: int
    source_format: Literal["api", "svc", "npf"]
    post_id: str
    blog_name: str
    post_url: str
    post_type: Literal["photo", "video", "audio", "text", "quote", "link", "answer"]
    timestamp_utc: str  # ISO 8601
    tags: list[str]
    is_reblog: bool
    reblog_source: ReblogSource | None
    title: str | None
    body_text: str | None
    body_html: str | None
    media: list[MediaEntry]
    raw_content_blocks: list[dict[str, object]] | None
