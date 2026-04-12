"""Post domain model."""

from __future__ import annotations

from typing import Literal

from pydantic import BaseModel

PostType = Literal["photo", "video", "audio", "text", "quote", "link", "answer"]


class Post(BaseModel):
    """A single Tumblr post with normalised fields."""

    post_id: str
    blog_name: str
    post_url: str
    post_type: PostType
    timestamp_utc: str
    tags: list[str] = []
    is_reblog: bool = False
    reblog_source_blog: str | None = None
    reblog_source_post_id: str | None = None
    title: str | None = None
    body_text: str | None = None
    body_html: str | None = None
    media_count: int = 0
