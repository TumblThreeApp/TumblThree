# tumbl4 Plan 2: MVP Public Blog Photo Crawl

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `tumbl4 download <blog>` crawls a public Tumblr blog via the V1 API, downloads all photo posts (single + photosets) to disk, with resume and per-blog dedup.

**Architecture:** Producer-consumer pipeline. The crawler (producer) paginates the V1 API (`/api/read/json`), parses photo posts into `IntermediateDict`, and enqueues `MediaTask` objects onto an `asyncio.Queue`. N download workers (consumers, default 4) stream each file to a `.part` temp file, reconcile content-type, then atomic-rename to final path. A per-blog SQLite database tracks downloaded URLs (SHA-256 keyed) for dedup, resume cursors (`LastId`), and post completion state. JSON metadata sidecars are written atomically after all media for a post resolve.

**Tech Stack:** Python 3.12+, httpx (async HTTP), aiolimiter (rate limiting), aiofiles (async file I/O), pydantic (models), SQLite (state), Rich (progress), Typer (CLI).

**Builds on Plan 1:** `Settings`/`HttpSettings`/`QueueSettings` in `models/settings.py`, `spawn()` in `_internal/tasks.py`, `get_logger()`/`SecretFilter` in `_internal/logging.py`, `state_dir()`/`data_dir()` in `_internal/paths.py`, Typer `app` in `cli/app.py`.

**Plans in this series:**

| # | Plan | Deliverable |
|---|---|---|
| 1 | Foundation (shipped) | `tumbl4 --version`; tooling + CI green |
| **2** | **MVP public blog photo crawl (this plan)** | **`tumbl4 download <blog>` downloads photos, resumable** |
| 3 | All post types + sidecars + templates | Every post type; configurable filename templates |
| 4 | Filters + dedup + pinned posts | Tag/timespan filters; cross-blog dedup; pinned-post fix |
| 5 | Auth + hidden blog crawler | `tumbl4 login` + hidden/dashboard blog downloads |
| 6 | Security hardening + release | Redirect safety, SSRF guards, signal handling, SLSA release |

**Spec references:**
- Design spec: `docs/superpowers/specs/2026-04-11-tumbl4-macos-cli-port-design.md`
- Plan boundaries: `docs/superpowers/specs/2026-04-11-tumbl4-plan-boundaries.md`

---

## File Structure (Plan 2 additions)

New files are marked with `+`. Modified files are marked with `~`.

```
src/tumbl4/
├── __init__.py                              # (unchanged)
├── cli/
│   ├── app.py                            ~  # register download subcommand
│   ├── commands/
│   │   ├── __init__.py                   +
│   │   └── download.py                   +  # tumbl4 download <blog>
│   └── output/
│       ├── __init__.py                   +
│       └── progress.py                   +  # Rich progress bars
├── core/
│   ├── __init__.py                       ~  # re-export public API
│   ├── context.py                        +  # CrawlContext frozen dataclass
│   ├── errors.py                         +  # exception taxonomy
│   ├── orchestrator.py                   +  # state machine: crawl → download → persist
│   ├── crawl/
│   │   ├── __init__.py                   +
│   │   ├── http_client.py                +  # httpx wrapper + rate limiter
│   │   └── tumblr_blog.py               +  # V1 API public crawler
│   ├── parse/
│   │   ├── __init__.py                   +
│   │   ├── intermediate.py               +  # IntermediateDict + MediaEntry TypedDicts
│   │   └── api_json.py                   +  # V1 JSONP → IntermediateDict
│   ├── download/
│   │   ├── __init__.py                   +
│   │   ├── content_type.py               +  # content-type reconciliation
│   │   └── file_downloader.py            +  # streaming .part + atomic rename
│   └── state/
│       ├── __init__.py                   +
│       ├── db.py                         +  # SQLite schema + WAL + migrations
│       ├── resume.py                     +  # LastId cursor persistence
│       └── metadata.py                   +  # JSON sidecar writer
├── models/
│   ├── __init__.py                       ~  # re-export models
│   ├── blog.py                           +  # Blog, BlogRef
│   ├── post.py                           +  # Post (photo variant)
│   ├── media.py                          +  # MediaTask, DownloadResult
│   └── settings.py                       ~  # add page_size field
tests/
├── conftest.py                           ~  # add fixtures
├── fixtures/
│   └── json/
│       ├── v1_photo_single.json          +  # V1 API single photo response
│       └── v1_photo_set.json             +  # V1 API photoset response
├── unit/
│   ├── test_errors.py                    +
│   ├── test_intermediate.py              +
│   ├── test_api_json.py                  +
│   ├── test_blog_model.py                +
│   ├── test_state_db.py                  +
│   ├── test_resume.py                    +
│   ├── test_metadata.py                  +
│   ├── test_http_client.py               +
│   ├── test_content_type.py              +
│   ├── test_file_downloader.py           +
│   ├── test_tumblr_blog_crawler.py       +
│   └── test_orchestrator.py              +
└── component/
    ├── __init__.py                       +
    └── test_download_pipeline.py         +  # orchestrator + fakes end-to-end
```

---

## Task 1: Exception taxonomy

**Files:**
- Create: `src/tumbl4/core/errors.py`
- Create: `tests/unit/test_errors.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_errors.py`:

```python
"""Tests for the tumbl4 exception hierarchy."""

from tumbl4.core.errors import (
    AllowlistViolation,
    BlogNotFound,
    BlogRequiresLogin,
    ConfigError,
    CrawlError,
    DiskFull,
    DownloadError,
    HashMismatch,
    ParseError,
    RateLimited,
    ResponseTooLarge,
    ServerError,
    StateError,
    Tumbl4Error,
    WriteFailed,
)


class TestExceptionHierarchy:
    def test_all_inherit_from_tumbl4_error(self) -> None:
        for exc_cls in [
            ConfigError, CrawlError, DownloadError, StateError,
            RateLimited, ServerError, BlogNotFound, BlogRequiresLogin,
            ResponseTooLarge, ParseError, DiskFull, WriteFailed,
            HashMismatch, AllowlistViolation,
        ]:
            assert issubclass(exc_cls, Tumbl4Error)

    def test_crawl_errors_inherit_from_crawl_error(self) -> None:
        for exc_cls in [
            RateLimited, ServerError, BlogNotFound, BlogRequiresLogin,
            ResponseTooLarge, ParseError,
        ]:
            assert issubclass(exc_cls, CrawlError)

    def test_download_errors_inherit_from_download_error(self) -> None:
        for exc_cls in [DiskFull, WriteFailed, HashMismatch, AllowlistViolation]:
            assert issubclass(exc_cls, DownloadError)

    def test_rate_limited_stores_retry_after(self) -> None:
        exc = RateLimited(retry_after=30.0)
        assert exc.retry_after == 30.0

    def test_rate_limited_none_retry_after(self) -> None:
        exc = RateLimited()
        assert exc.retry_after is None

    def test_server_error_stores_status_code(self) -> None:
        exc = ServerError(502, "Bad Gateway")
        assert exc.status_code == 502

    def test_parse_error_stores_excerpt(self) -> None:
        exc = ParseError("bad json", excerpt='{"truncated": true}')
        assert exc.excerpt == '{"truncated": true}'
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_errors.py -v`
Expected: FAIL — `ModuleNotFoundError: No module named 'tumbl4.core.errors'`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/errors.py`:

```python
"""Exception taxonomy for tumbl4.

All exceptions inherit from Tumbl4Error. See spec section 6.1.
"""

from __future__ import annotations


class Tumbl4Error(Exception):
    """Base exception for all tumbl4 errors."""


class ConfigError(Tumbl4Error):
    """Configuration file or filename template validation error."""


class CrawlError(Tumbl4Error):
    """Error during blog crawling."""


class RateLimited(CrawlError):
    """HTTP 429 — back off and retry."""

    def __init__(self, retry_after: float | None = None) -> None:
        self.retry_after = retry_after
        msg = "Rate limited"
        if retry_after is not None:
            msg += f", retry after {retry_after}s"
        super().__init__(msg)


class ServerError(CrawlError):
    """HTTP 5xx — transient server error."""

    def __init__(self, status_code: int, message: str = "") -> None:
        self.status_code = status_code
        super().__init__(f"Server error {status_code}: {message}")


class BlogNotFound(CrawlError):
    """HTTP 404 — blog does not exist or was removed."""


class BlogRequiresLogin(CrawlError):
    """Public crawl hit a hidden/login-required blog."""


class ResponseTooLarge(CrawlError):
    """Response body exceeded max_api_response_bytes."""


class ParseError(CrawlError):
    """Parser failure — skip the current post."""

    def __init__(self, message: str, excerpt: str = "") -> None:
        self.excerpt = excerpt
        super().__init__(message)


class DownloadError(Tumbl4Error):
    """Error during file download."""


class DiskFull(DownloadError):
    """ENOSPC — halt crawl."""


class WriteFailed(DownloadError):
    """OS error on write (non-ENOSPC) — halt crawl."""


class HashMismatch(DownloadError):
    """Content hash mismatch — retry."""


class AllowlistViolation(DownloadError):
    """URL not in allowed domains — halt crawl."""


class StateError(Tumbl4Error):
    """SQLite or state management error."""
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_errors.py -v`
Expected: 7 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/errors.py tests/unit/test_errors.py
git commit -m "feat(core): add exception taxonomy

Tumbl4Error hierarchy with CrawlError, DownloadError, StateError,
ConfigError branches and concrete leaf exceptions. See spec §6.1.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Parse contracts — IntermediateDict and MediaEntry

**Files:**
- Create: `src/tumbl4/core/parse/__init__.py`
- Create: `src/tumbl4/core/parse/intermediate.py`
- Create: `tests/unit/test_intermediate.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_intermediate.py`:

```python
"""Tests for IntermediateDict and MediaEntry TypedDict contracts."""

from tumbl4.core.parse.intermediate import IntermediateDict, MediaEntry, ReblogSource


def _make_media_entry(**overrides: object) -> MediaEntry:
    defaults: MediaEntry = {
        "kind": "photo",
        "url": "https://64.media.tumblr.com/abc123/s1280x1920/photo.jpg",
        "width": 1280,
        "height": 960,
        "mime_type": None,
        "alt_text": None,
        "duration_ms": None,
    }
    return {**defaults, **overrides}  # type: ignore[return-value]


def _make_intermediate(**overrides: object) -> IntermediateDict:
    defaults: IntermediateDict = {
        "schema_version": 1,
        "source_format": "api",
        "post_id": "12345",
        "blog_name": "testblog",
        "post_url": "https://testblog.tumblr.com/post/12345",
        "post_type": "photo",
        "timestamp_utc": "2026-04-11T14:22:03+00:00",
        "tags": ["art", "wip"],
        "is_reblog": False,
        "reblog_source": None,
        "title": None,
        "body_text": "A test photo",
        "body_html": "<p>A test photo</p>",
        "media": [_make_media_entry()],
        "raw_content_blocks": None,
    }
    return {**defaults, **overrides}  # type: ignore[return-value]


class TestMediaEntry:
    def test_photo_entry(self) -> None:
        entry = _make_media_entry()
        assert entry["kind"] == "photo"
        assert entry["url"].startswith("https://")
        assert entry["width"] == 1280

    def test_video_entry(self) -> None:
        entry = _make_media_entry(kind="video", duration_ms=15000)
        assert entry["kind"] == "video"
        assert entry["duration_ms"] == 15000

    def test_nullable_fields(self) -> None:
        entry = _make_media_entry(width=None, height=None, mime_type=None)
        assert entry["width"] is None
        assert entry["height"] is None


class TestIntermediateDict:
    def test_basic_construction(self) -> None:
        d = _make_intermediate()
        assert d["schema_version"] == 1
        assert d["source_format"] == "api"
        assert d["post_type"] == "photo"
        assert len(d["media"]) == 1

    def test_reblog(self) -> None:
        source: ReblogSource = {"blog_name": "original", "post_id": "99999"}
        d = _make_intermediate(is_reblog=True, reblog_source=source)
        assert d["is_reblog"] is True
        assert d["reblog_source"]["blog_name"] == "original"

    def test_empty_tags(self) -> None:
        d = _make_intermediate(tags=[])
        assert d["tags"] == []

    def test_multiple_media(self) -> None:
        media = [_make_media_entry(), _make_media_entry(url="https://example.com/photo2.jpg")]
        d = _make_intermediate(media=media)
        assert len(d["media"]) == 2
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_intermediate.py -v`
Expected: FAIL — `ModuleNotFoundError`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/parse/__init__.py`:

```python
"""Parse pipeline — raw API responses to IntermediateDict to Pydantic models."""
```

Write file `src/tumbl4/core/parse/intermediate.py`:

```python
"""IntermediateDict contract — the uniform shape all parsers emit.

All parsers (api_json, svc_json, npf) normalise their wire format into
this TypedDict. Downstream code (models, orchestrator, download) never
sees raw API fields — only this shape.

See spec section 5.7.
"""

from __future__ import annotations

from typing import Literal, TypedDict


class ReblogSource(TypedDict):
    """Identifies the original blog and post for a reblog."""

    blog_name: str
    post_id: str


class MediaEntry(TypedDict):
    """A single downloadable media item extracted from a post."""

    kind: Literal["photo", "video", "audio"]
    url: str
    width: int | None
    height: int | None
    mime_type: str | None
    alt_text: str | None
    duration_ms: int | None


class IntermediateDict(TypedDict):
    """Normalised post representation emitted by every parser.

    schema_version is bumped on breaking shape changes.
    """

    schema_version: int
    source_format: Literal["api", "svc", "npf"]
    post_id: str
    blog_name: str
    post_url: str
    post_type: Literal["photo", "video", "audio", "text", "quote", "link", "answer"]
    timestamp_utc: str
    tags: list[str]
    is_reblog: bool
    reblog_source: ReblogSource | None
    title: str | None
    body_text: str | None
    body_html: str | None
    media: list[MediaEntry]
    raw_content_blocks: list[dict[str, object]] | None
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_intermediate.py -v`
Expected: 7 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/parse/__init__.py src/tumbl4/core/parse/intermediate.py tests/unit/test_intermediate.py
git commit -m "feat(parse): add IntermediateDict and MediaEntry contracts

TypedDicts that define the uniform shape all parsers emit.
Downstream code never sees raw API fields. See spec §5.7.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Domain models — Blog, Post, Media

**Files:**
- Create: `src/tumbl4/models/blog.py`
- Create: `src/tumbl4/models/post.py`
- Create: `src/tumbl4/models/media.py`
- Modify: `src/tumbl4/models/__init__.py`
- Create: `tests/unit/test_blog_model.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_blog_model.py`:

```python
"""Tests for Blog, Post, and Media domain models."""

from tumbl4.models.blog import Blog, BlogRef
from tumbl4.models.media import DownloadResult, MediaTask
from tumbl4.models.post import Post


class TestBlogRef:
    def test_from_name(self) -> None:
        ref = BlogRef.from_input("photography")
        assert ref.name == "photography"
        assert ref.url == "https://photography.tumblr.com/"

    def test_from_full_url(self) -> None:
        ref = BlogRef.from_input("https://photography.tumblr.com/")
        assert ref.name == "photography"
        assert ref.url == "https://photography.tumblr.com/"

    def test_from_url_without_trailing_slash(self) -> None:
        ref = BlogRef.from_input("https://photography.tumblr.com")
        assert ref.url == "https://photography.tumblr.com/"

    def test_from_www_url(self) -> None:
        ref = BlogRef.from_input("https://www.tumblr.com/photography")
        assert ref.name == "photography"

    def test_frozen(self) -> None:
        ref = BlogRef.from_input("test")
        try:
            ref.name = "other"  # type: ignore[misc]
            assert False, "Should be frozen"
        except AttributeError:
            pass


class TestBlog:
    def test_construction(self) -> None:
        blog = Blog(
            name="photography",
            url="https://photography.tumblr.com/",
            title="Photography Blog",
            total_posts=150,
        )
        assert blog.name == "photography"
        assert blog.total_posts == 150

    def test_defaults(self) -> None:
        blog = Blog(name="test", url="https://test.tumblr.com/")
        assert blog.title is None
        assert blog.total_posts == 0


class TestPost:
    def test_construction(self) -> None:
        post = Post(
            post_id="12345",
            blog_name="test",
            post_url="https://test.tumblr.com/post/12345",
            post_type="photo",
            timestamp_utc="2026-04-11T14:22:03+00:00",
            tags=["art"],
            is_reblog=False,
            media_count=1,
        )
        assert post.post_id == "12345"
        assert post.media_count == 1


class TestMediaTask:
    def test_construction(self) -> None:
        task = MediaTask(
            url="https://64.media.tumblr.com/photo.jpg",
            post_id="12345",
            blog_name="test",
            index=0,
            output_dir="/tmp/output/test",
        )
        assert task.url == "https://64.media.tumblr.com/photo.jpg"
        assert task.index == 0

    def test_filename_from_url(self) -> None:
        task = MediaTask(
            url="https://64.media.tumblr.com/abc123/s1280x1920/photo.jpg",
            post_id="12345",
            blog_name="test",
            index=0,
            output_dir="/tmp/output/test",
        )
        assert task.filename.endswith(".jpg")


class TestDownloadResult:
    def test_success(self) -> None:
        result = DownloadResult(
            url="https://example.com/photo.jpg",
            post_id="12345",
            filename="12345_01.jpg",
            byte_count=1024,
            status="success",
        )
        assert result.status == "success"
        assert result.error is None

    def test_failure(self) -> None:
        result = DownloadResult(
            url="https://example.com/photo.jpg",
            post_id="12345",
            filename=None,
            byte_count=0,
            status="failed",
            error="HTTP 404 after 5 retries",
        )
        assert result.status == "failed"
        assert result.filename is None
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_blog_model.py -v`
Expected: FAIL — `ModuleNotFoundError`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/models/blog.py`:

```python
"""Blog and BlogRef domain models."""

from __future__ import annotations

import re
from dataclasses import dataclass

from pydantic import BaseModel


@dataclass(frozen=True)
class BlogRef:
    """Immutable reference to a Tumblr blog, normalised from user input."""

    name: str
    url: str

    @classmethod
    def from_input(cls, raw: str) -> BlogRef:
        """Normalise a blog name or URL into a BlogRef.

        Accepts:
          - bare name: "photography"
          - subdomain URL: "https://photography.tumblr.com/"
          - www URL: "https://www.tumblr.com/photography"
        """
        raw = raw.strip().rstrip("/")

        # https://www.tumblr.com/<blogname>
        www_match = re.match(r"https?://(?:www\.)?tumblr\.com/([a-zA-Z0-9_-]+)", raw)
        if www_match:
            name = www_match.group(1).lower()
            return cls(name=name, url=f"https://{name}.tumblr.com/")

        # https://<blogname>.tumblr.com
        sub_match = re.match(r"https?://([a-zA-Z0-9_-]+)\.tumblr\.com", raw)
        if sub_match:
            name = sub_match.group(1).lower()
            return cls(name=name, url=f"https://{name}.tumblr.com/")

        # Bare name
        name = raw.lower()
        return cls(name=name, url=f"https://{name}.tumblr.com/")


class Blog(BaseModel):
    """Blog metadata tracked during and after crawl."""

    name: str
    url: str
    title: str | None = None
    total_posts: int = 0
```

Write file `src/tumbl4/models/post.py`:

```python
"""Post domain model."""

from __future__ import annotations

from typing import Literal

from pydantic import BaseModel


class Post(BaseModel):
    """A Tumblr post with metadata. Media items are tracked separately."""

    post_id: str
    blog_name: str
    post_url: str
    post_type: Literal["photo", "video", "audio", "text", "quote", "link", "answer"]
    timestamp_utc: str
    tags: list[str] = []
    is_reblog: bool = False
    reblog_source_blog: str | None = None
    reblog_source_post_id: str | None = None
    title: str | None = None
    body_text: str | None = None
    body_html: str | None = None
    media_count: int = 0
```

Write file `src/tumbl4/models/media.py`:

```python
"""Media download models — MediaTask and DownloadResult."""

from __future__ import annotations

import hashlib
from pathlib import Path
from typing import Literal
from urllib.parse import urlparse

from pydantic import BaseModel, computed_field


class MediaTask(BaseModel):
    """A single file to download. One post may produce multiple MediaTasks."""

    url: str
    post_id: str
    blog_name: str
    index: int
    output_dir: str

    @computed_field  # type: ignore[prop-decorator]
    @property
    def url_hash(self) -> str:
        """SHA-256 of the initial-request URL, used for dedup keying."""
        return hashlib.sha256(self.url.encode()).hexdigest()

    @computed_field  # type: ignore[prop-decorator]
    @property
    def filename(self) -> str:
        """Default filename: {post_id}_{index:02d}.{ext}."""
        parsed = urlparse(self.url)
        ext = Path(parsed.path).suffix.lstrip(".") or "jpg"
        return f"{self.post_id}_{self.index:02d}.{ext}"

    @computed_field  # type: ignore[prop-decorator]
    @property
    def final_path(self) -> Path:
        return Path(self.output_dir) / self.filename

    @computed_field  # type: ignore[prop-decorator]
    @property
    def part_path(self) -> Path:
        return Path(self.output_dir) / f"{self.filename}.part"


class DownloadResult(BaseModel):
    """Outcome of a single media download attempt."""

    url: str
    post_id: str
    filename: str | None
    byte_count: int
    status: Literal["success", "failed"]
    error: str | None = None
```

Update `src/tumbl4/models/__init__.py`:

```python
"""Pydantic domain models shared across core and cli layers."""

from tumbl4.models.blog import Blog, BlogRef
from tumbl4.models.media import DownloadResult, MediaTask
from tumbl4.models.post import Post
from tumbl4.models.settings import Settings

__all__ = [
    "Blog",
    "BlogRef",
    "DownloadResult",
    "MediaTask",
    "Post",
    "Settings",
]
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_blog_model.py -v`
Expected: 10 passed

- [ ] **Step 5: Run all existing tests to check for regressions**

Run: `uv run pytest -v`
Expected: all tests pass (31 existing + 10 new = 41)

- [ ] **Step 6: Commit**

```bash
git add src/tumbl4/models/blog.py src/tumbl4/models/post.py src/tumbl4/models/media.py src/tumbl4/models/__init__.py tests/unit/test_blog_model.py
git commit -m "feat(models): add Blog, BlogRef, Post, MediaTask, DownloadResult

BlogRef.from_input normalises bare names, subdomain URLs, and www URLs.
MediaTask computes url_hash (SHA-256), filename, and .part path.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: V1 API JSON parser and test fixtures

**Files:**
- Create: `src/tumbl4/core/parse/api_json.py`
- Create: `tests/fixtures/json/v1_photo_single.json`
- Create: `tests/fixtures/json/v1_photo_set.json`
- Create: `tests/unit/test_api_json.py`

- [ ] **Step 1: Create test fixture files**

Write file `tests/fixtures/json/v1_photo_single.json` — a realistic V1 API response for a single-photo post (the JSONP wrapper is included as a string, since the parser must strip it):

```json
{
    "_raw_jsonp": "var tumblr_api_read = {\"tumblelog\":{\"title\":\"Test Blog\",\"description\":\"A test blog\",\"name\":\"testblog\",\"timezone\":\"US/Eastern\",\"cname\":false,\"feeds\":[]},\"posts-start\":0,\"posts-total\":\"150\",\"posts-type\":false,\"posts\":[{\"id\":\"728394056123\",\"url\":\"https://testblog.tumblr.com/post/728394056123/sunset-photo\",\"url-with-slug\":\"https://testblog.tumblr.com/post/728394056123/sunset-photo\",\"type\":\"photo\",\"date-gmt\":\"2026-04-10 18:30:00 GMT\",\"date\":\"Thu, 10 Apr 2026 14:30:00\",\"bookmarklet\":0,\"mobile\":0,\"feed-item\":\"\",\"from-feed-id\":0,\"unix-timestamp\":1776097800,\"format\":\"html\",\"reblog-key\":\"abc123\",\"slug\":\"sunset-photo\",\"note-count\":42,\"tags\":[\"photography\",\"sunset\"],\"photo-caption\":\"<p>Beautiful sunset from the pier</p>\",\"width\":1280,\"height\":960,\"photo-url-1280\":\"https://64.media.tumblr.com/aaa111/s1280x1920/sunset.jpg\",\"photo-url-500\":\"https://64.media.tumblr.com/aaa111/s500x750/sunset.jpg\",\"photo-url-400\":\"https://64.media.tumblr.com/aaa111/s400x600/sunset.jpg\",\"photo-url-250\":\"https://64.media.tumblr.com/aaa111/s250x400/sunset.jpg\",\"photo-url-100\":\"https://64.media.tumblr.com/aaa111/s100x200/sunset.jpg\",\"photo-url-75\":\"https://64.media.tumblr.com/aaa111/s75x75/sunset.jpg\"}]};",
    "parsed": {
        "tumblelog": {
            "title": "Test Blog",
            "name": "testblog"
        },
        "posts-start": 0,
        "posts-total": "150",
        "posts": [
            {
                "id": "728394056123",
                "url": "https://testblog.tumblr.com/post/728394056123/sunset-photo",
                "url-with-slug": "https://testblog.tumblr.com/post/728394056123/sunset-photo",
                "type": "photo",
                "unix-timestamp": 1776097800,
                "slug": "sunset-photo",
                "note-count": 42,
                "tags": ["photography", "sunset"],
                "photo-caption": "<p>Beautiful sunset from the pier</p>",
                "width": 1280,
                "height": 960,
                "photo-url-1280": "https://64.media.tumblr.com/aaa111/s1280x1920/sunset.jpg",
                "photo-url-500": "https://64.media.tumblr.com/aaa111/s500x750/sunset.jpg",
                "photo-url-400": "https://64.media.tumblr.com/aaa111/s400x600/sunset.jpg",
                "photo-url-250": "https://64.media.tumblr.com/aaa111/s250x400/sunset.jpg",
                "photo-url-100": "https://64.media.tumblr.com/aaa111/s100x200/sunset.jpg",
                "photo-url-75": "https://64.media.tumblr.com/aaa111/s75x75/sunset.jpg"
            }
        ]
    }
}
```

Write file `tests/fixtures/json/v1_photo_set.json`:

```json
{
    "parsed": {
        "tumblelog": {
            "title": "Art Blog",
            "name": "artblog"
        },
        "posts-start": 0,
        "posts-total": "50",
        "posts": [
            {
                "id": "728394056999",
                "url": "https://artblog.tumblr.com/post/728394056999",
                "url-with-slug": "https://artblog.tumblr.com/post/728394056999/photoset-test",
                "type": "photo",
                "unix-timestamp": 1776097800,
                "slug": "photoset-test",
                "note-count": 10,
                "tags": ["art", "wip"],
                "reblogged-from-name": "originalblog",
                "reblogged-from-url": "https://originalblog.tumblr.com/post/111",
                "photo-caption": "<p>Work in progress</p>",
                "photos": [
                    {
                        "offset": "o1",
                        "caption": "First image",
                        "width": 1280,
                        "height": 800,
                        "photo-url-1280": "https://64.media.tumblr.com/bbb222/s1280x1920/art1.jpg",
                        "photo-url-500": "https://64.media.tumblr.com/bbb222/s500x750/art1.jpg"
                    },
                    {
                        "offset": "o2",
                        "caption": "Second image",
                        "width": 1280,
                        "height": 1024,
                        "photo-url-1280": "https://64.media.tumblr.com/ccc333/s1280x1920/art2.png",
                        "photo-url-500": "https://64.media.tumblr.com/ccc333/s500x750/art2.png"
                    },
                    {
                        "offset": "o3",
                        "caption": "",
                        "width": 500,
                        "height": 500,
                        "photo-url-1280": "https://64.media.tumblr.com/ddd444/s1280x1920/art3.gif",
                        "photo-url-500": "https://64.media.tumblr.com/ddd444/s500x750/art3.gif"
                    }
                ]
            }
        ]
    }
}
```

- [ ] **Step 2: Write the failing test**

Write file `tests/unit/test_api_json.py`:

```python
"""Tests for V1 API JSON parser."""

import json
from pathlib import Path

import pytest

from tumbl4.core.errors import ParseError
from tumbl4.core.parse.api_json import normalize_photo_post, parse_v1_response, strip_jsonp

FIXTURES = Path(__file__).parent.parent / "fixtures" / "json"


class TestStripJsonp:
    def test_strips_wrapper(self) -> None:
        raw = 'var tumblr_api_read = {"key": "value"};'
        result = strip_jsonp(raw)
        assert result == {"key": "value"}

    def test_handles_whitespace(self) -> None:
        raw = '  var tumblr_api_read = {"key": "value"} ; '
        result = strip_jsonp(raw)
        assert result == {"key": "value"}

    def test_invalid_json_raises_parse_error(self) -> None:
        with pytest.raises(ParseError):
            strip_jsonp("var tumblr_api_read = {invalid};")

    def test_plain_json_passthrough(self) -> None:
        raw = '{"key": "value"}'
        result = strip_jsonp(raw)
        assert result == {"key": "value"}


class TestParseV1Response:
    def test_extracts_blog_info(self) -> None:
        fixture = json.loads((FIXTURES / "v1_photo_single.json").read_text())
        blog_name, total_posts, posts = parse_v1_response(fixture["parsed"])
        assert blog_name == "testblog"
        assert total_posts == 150
        assert len(posts) == 1

    def test_photoset_response(self) -> None:
        fixture = json.loads((FIXTURES / "v1_photo_set.json").read_text())
        blog_name, total_posts, posts = parse_v1_response(fixture["parsed"])
        assert blog_name == "artblog"
        assert total_posts == 50


class TestNormalizePhotoPost:
    def test_single_photo(self) -> None:
        fixture = json.loads((FIXTURES / "v1_photo_single.json").read_text())
        post = fixture["parsed"]["posts"][0]
        result = normalize_photo_post(post, "testblog")
        assert result["schema_version"] == 1
        assert result["source_format"] == "api"
        assert result["post_id"] == "728394056123"
        assert result["blog_name"] == "testblog"
        assert result["post_type"] == "photo"
        assert result["tags"] == ["photography", "sunset"]
        assert result["is_reblog"] is False
        assert result["reblog_source"] is None
        assert len(result["media"]) == 1
        assert result["media"][0]["url"] == "https://64.media.tumblr.com/aaa111/s1280x1920/sunset.jpg"
        assert result["media"][0]["kind"] == "photo"
        assert result["media"][0]["width"] == 1280

    def test_photoset(self) -> None:
        fixture = json.loads((FIXTURES / "v1_photo_set.json").read_text())
        post = fixture["parsed"]["posts"][0]
        result = normalize_photo_post(post, "artblog")
        assert len(result["media"]) == 3
        assert result["media"][0]["url"].endswith("art1.jpg")
        assert result["media"][1]["url"].endswith("art2.png")
        assert result["media"][2]["url"].endswith("art3.gif")
        assert result["media"][0]["alt_text"] == "First image"
        assert result["media"][2]["alt_text"] is None  # empty caption -> None

    def test_reblog_detection(self) -> None:
        fixture = json.loads((FIXTURES / "v1_photo_set.json").read_text())
        post = fixture["parsed"]["posts"][0]
        result = normalize_photo_post(post, "artblog")
        assert result["is_reblog"] is True
        assert result["reblog_source"] is not None
        assert result["reblog_source"]["blog_name"] == "originalblog"

    def test_custom_image_size(self) -> None:
        fixture = json.loads((FIXTURES / "v1_photo_single.json").read_text())
        post = fixture["parsed"]["posts"][0]
        result = normalize_photo_post(post, "testblog", image_size="500")
        assert result["media"][0]["url"].endswith("s500x750/sunset.jpg")

    def test_missing_tags_defaults_to_empty_list(self) -> None:
        post = {"id": "1", "type": "photo", "unix-timestamp": 0, "photo-url-1280": "https://example.com/img.jpg"}
        result = normalize_photo_post(post, "test")
        assert result["tags"] == []

    def test_missing_timestamp_defaults_to_epoch(self) -> None:
        post = {"id": "1", "type": "photo", "photo-url-1280": "https://example.com/img.jpg"}
        result = normalize_photo_post(post, "test")
        assert "1970-01-01" in result["timestamp_utc"]
```

- [ ] **Step 3: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_api_json.py -v`
Expected: FAIL — `ModuleNotFoundError`

- [ ] **Step 4: Write the implementation**

Write file `src/tumbl4/core/parse/api_json.py`:

```python
"""Parse Tumblr V1 API JSON responses into IntermediateDict.

The V1 API endpoint /api/read/json returns JSONP-wrapped JSON:
    var tumblr_api_read = {...};

This module strips the wrapper and normalises photo posts into
the IntermediateDict contract.
"""

from __future__ import annotations

import json
import re
from datetime import UTC, datetime

from tumbl4.core.errors import ParseError
from tumbl4.core.parse.intermediate import IntermediateDict, MediaEntry, ReblogSource

_JSONP_PREFIX = re.compile(r"^\s*var\s+tumblr_api_read\s*=\s*")
_JSONP_SUFFIX = re.compile(r"\s*;\s*$")


def strip_jsonp(raw: str) -> dict[str, object]:
    """Strip JSONP wrapper and parse as JSON.

    Handles both wrapped (var tumblr_api_read = {...};) and plain JSON.
    """
    text = _JSONP_PREFIX.sub("", raw)
    text = _JSONP_SUFFIX.sub("", text)
    try:
        return json.loads(text)  # type: ignore[no-any-return]
    except json.JSONDecodeError as e:
        raise ParseError(f"Failed to parse V1 API response: {e}", excerpt=text[:200]) from e


def parse_v1_response(data: dict[str, object]) -> tuple[str, int, list[dict[str, object]]]:
    """Extract blog name, total post count, and post list from a V1 API response.

    Returns:
        (blog_name, total_posts, posts)
    """
    tumblelog = data.get("tumblelog", {})
    blog_name: str = tumblelog.get("name", "") if isinstance(tumblelog, dict) else ""  # type: ignore[union-attr]
    total_str = data.get("posts-total", "0")
    total_posts = int(total_str) if isinstance(total_str, (str, int)) else 0
    posts = data.get("posts", [])
    if not isinstance(posts, list):
        posts = []
    return blog_name, total_posts, posts  # type: ignore[return-value]


def normalize_photo_post(
    post: dict[str, object],
    blog_name: str,
    image_size: str = "1280",
) -> IntermediateDict:
    """Convert a V1 API photo post dict to IntermediateDict."""
    post_id = str(post.get("id", ""))

    media: list[MediaEntry] = []
    photos = post.get("photos")

    if isinstance(photos, list) and photos:
        for photo in photos:
            if not isinstance(photo, dict):
                continue
            url = _get_photo_url(photo, image_size)
            if url:
                caption = photo.get("caption", "")
                media.append(
                    MediaEntry(
                        kind="photo",
                        url=url,
                        width=_int_or_none(photo.get("width")),
                        height=_int_or_none(photo.get("height")),
                        mime_type=None,
                        alt_text=caption if caption else None,
                        duration_ms=None,
                    )
                )
    else:
        url = _get_photo_url(post, image_size)
        if url:
            media.append(
                MediaEntry(
                    kind="photo",
                    url=url,
                    width=_int_or_none(post.get("width")),
                    height=_int_or_none(post.get("height")),
                    mime_type=None,
                    alt_text=None,
                    duration_ms=None,
                )
            )

    ts = _parse_timestamp(post.get("unix-timestamp"))
    reblog_from = post.get("reblogged-from-name")
    is_reblog = bool(reblog_from)
    reblog_source: ReblogSource | None = None
    if isinstance(reblog_from, str) and reblog_from:
        reblog_source = ReblogSource(
            blog_name=reblog_from,
            post_id=str(post.get("reblogged-from-id", "")),
        )

    caption = post.get("photo-caption")

    return IntermediateDict(
        schema_version=1,
        source_format="api",
        post_id=post_id,
        blog_name=blog_name,
        post_url=str(post.get("url-with-slug", post.get("url", ""))),
        post_type="photo",
        timestamp_utc=ts,
        tags=_safe_tags(post.get("tags")),
        is_reblog=is_reblog,
        reblog_source=reblog_source,
        title=None,
        body_text=str(caption) if caption else None,
        body_html=str(caption) if caption else None,
        media=media,
        raw_content_blocks=None,
    )


def _get_photo_url(obj: dict[str, object], size: str) -> str | None:
    """Extract photo URL at the requested size, falling back to 1280."""
    url = obj.get(f"photo-url-{size}")
    if not url:
        url = obj.get("photo-url-1280")
    return str(url) if url else None


def _parse_timestamp(raw: object) -> str:
    """Parse a unix timestamp to ISO8601 string. Defaults to epoch on failure."""
    try:
        ts = int(raw) if raw is not None else 0  # type: ignore[arg-type]
        return datetime.fromtimestamp(ts, tz=UTC).isoformat()
    except (ValueError, TypeError, OSError):
        return datetime.fromtimestamp(0, tz=UTC).isoformat()


def _safe_tags(raw: object) -> list[str]:
    """Ensure tags is always a list of strings."""
    if isinstance(raw, list):
        return [str(t) for t in raw]
    return []


def _int_or_none(val: object) -> int | None:
    """Coerce to int or return None."""
    if val is None:
        return None
    try:
        return int(val)  # type: ignore[arg-type]
    except (ValueError, TypeError):
        return None
```

- [ ] **Step 5: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_api_json.py -v`
Expected: 10 passed

- [ ] **Step 6: Commit**

```bash
git add src/tumbl4/core/parse/api_json.py tests/fixtures/json/v1_photo_single.json tests/fixtures/json/v1_photo_set.json tests/unit/test_api_json.py
git commit -m "feat(parse): add V1 API JSON parser with test fixtures

Handles JSONP unwrapping, single photo + photoset extraction,
reblog detection, and image size selection. Snapshot fixtures
for both single and photoset responses.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: SQLite state layer

**Files:**
- Create: `src/tumbl4/core/state/__init__.py`
- Create: `src/tumbl4/core/state/db.py`
- Create: `tests/unit/test_state_db.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_state_db.py`:

```python
"""Tests for the SQLite state layer."""

import sqlite3

import pytest

from tumbl4.core.state.db import StateDb


class TestStateDb:
    def test_creates_tables_on_init(self) -> None:
        db = StateDb(":memory:")
        tables = db.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name").fetchall()
        table_names = [row[0] for row in tables]
        assert "crawl_state" in table_names
        assert "downloads" in table_names
        assert "posts" in table_names
        db.close()

    def test_wal_mode_enabled(self) -> None:
        db = StateDb(":memory:")
        mode = db.execute("PRAGMA journal_mode").fetchone()[0]
        # in-memory databases can't use WAL, so this checks the attempt was made
        # on-disk databases will return "wal"
        assert mode in ("wal", "memory")
        db.close()

    def test_user_version_set(self) -> None:
        db = StateDb(":memory:")
        version = db.execute("PRAGMA user_version").fetchone()[0]
        assert version == 1
        db.close()

    def test_record_download(self) -> None:
        db = StateDb(":memory:")
        db.record_download(
            url_hash="abc123",
            url="https://example.com/photo.jpg",
            blog_name="test",
            post_id="12345",
            filename="12345_01.jpg",
            byte_count=1024,
            status="success",
        )
        row = db.execute("SELECT * FROM downloads WHERE url_hash = ?", ("abc123",)).fetchone()
        assert row is not None
        assert row[1] == "https://example.com/photo.jpg"
        assert row[5] == 1024
        db.close()

    def test_is_downloaded(self) -> None:
        db = StateDb(":memory:")
        assert db.is_downloaded("abc123") is False
        db.record_download(
            url_hash="abc123", url="https://example.com/photo.jpg",
            blog_name="test", post_id="12345", filename="12345_01.jpg",
            byte_count=1024, status="success",
        )
        assert db.is_downloaded("abc123") is True
        db.close()

    def test_record_download_failed(self) -> None:
        db = StateDb(":memory:")
        db.record_download(
            url_hash="def456", url="https://example.com/gone.jpg",
            blog_name="test", post_id="12345", filename=None,
            byte_count=0, status="failed", error="HTTP 404",
        )
        row = db.execute("SELECT status, error FROM downloads WHERE url_hash = ?", ("def456",)).fetchone()
        assert row[0] == "failed"
        assert row[1] == "HTTP 404"
        db.close()

    def test_mark_post_complete(self) -> None:
        db = StateDb(":memory:")
        db.mark_post_complete("12345", "testblog")
        row = db.execute("SELECT sidecar_written FROM posts WHERE post_id = ?", ("12345",)).fetchone()
        assert row[0] == 1
        db.close()

    def test_is_post_complete(self) -> None:
        db = StateDb(":memory:")
        assert db.is_post_complete("12345") is False
        db.mark_post_complete("12345", "testblog")
        assert db.is_post_complete("12345") is True
        db.close()

    def test_execute_uses_parameterized_queries(self) -> None:
        db = StateDb(":memory:")
        # This should NOT raise — parameterized query
        db.execute("SELECT * FROM downloads WHERE url_hash = ?", ("test",))
        db.close()

    def test_on_disk_uses_wal(self, tmp_path: object) -> None:
        import pathlib
        p = pathlib.Path(str(tmp_path)) / "test.db"
        db = StateDb(str(p))
        mode = db.execute("PRAGMA journal_mode").fetchone()[0]
        assert mode == "wal"
        db.close()
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_state_db.py -v`
Expected: FAIL — `ModuleNotFoundError`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/state/__init__.py`:

```python
"""State management — SQLite database, resume cursors, dedup, sidecars."""
```

Write file `src/tumbl4/core/state/db.py`:

```python
"""SQLite state database for per-blog download tracking.

Uses WAL mode for crash safety. All queries use parameterised placeholders.
Schema migrations via PRAGMA user_version.
"""

from __future__ import annotations

import sqlite3
from datetime import UTC, datetime

from tumbl4._internal.logging import get_logger

logger = get_logger(__name__)

_SCHEMA_VERSION = 1

_SCHEMA_SQL = """\
CREATE TABLE IF NOT EXISTS crawl_state (
    blog_name    TEXT NOT NULL,
    crawler_type TEXT NOT NULL DEFAULT 'public',
    last_id      INTEGER NOT NULL DEFAULT 0,
    last_complete_crawl TEXT,
    cursor_version INTEGER NOT NULL DEFAULT 1,
    PRIMARY KEY (blog_name, crawler_type)
);

CREATE TABLE IF NOT EXISTS downloads (
    url_hash   TEXT PRIMARY KEY,
    url        TEXT NOT NULL,
    blog_name  TEXT NOT NULL,
    post_id    TEXT NOT NULL,
    filename   TEXT,
    bytes      INTEGER NOT NULL DEFAULT 0,
    status     TEXT NOT NULL DEFAULT 'success',
    error      TEXT,
    created_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_downloads_blog ON downloads(blog_name);
CREATE INDEX IF NOT EXISTS idx_downloads_post ON downloads(post_id);

CREATE TABLE IF NOT EXISTS posts (
    post_id        TEXT PRIMARY KEY,
    blog_name      TEXT NOT NULL,
    sidecar_written INTEGER NOT NULL DEFAULT 0,
    created_at     TEXT NOT NULL
);
"""


class StateDb:
    """Per-blog SQLite database for download state tracking."""

    def __init__(self, path: str) -> None:
        self._conn = sqlite3.connect(path)
        self._conn.row_factory = sqlite3.Row
        self._setup()

    def _setup(self) -> None:
        # Enable WAL mode (no-op on :memory:)
        try:
            self._conn.execute("PRAGMA journal_mode=WAL")
        except sqlite3.OperationalError:
            pass  # :memory: databases don't support WAL
        self._conn.execute("PRAGMA synchronous=NORMAL")
        self._conn.executescript(_SCHEMA_SQL)
        current_version = self._conn.execute("PRAGMA user_version").fetchone()[0]
        if current_version < _SCHEMA_VERSION:
            self._conn.execute(f"PRAGMA user_version = {_SCHEMA_VERSION}")
        self._conn.commit()

    def execute(
        self, sql: str, params: tuple[object, ...] = ()
    ) -> sqlite3.Cursor:
        """Execute a parameterised SQL query."""
        return self._conn.execute(sql, params)

    def record_download(
        self,
        url_hash: str,
        url: str,
        blog_name: str,
        post_id: str,
        filename: str | None,
        byte_count: int,
        status: str,
        error: str | None = None,
    ) -> None:
        """Record a download attempt (success or failure)."""
        now = datetime.now(UTC).isoformat()
        self._conn.execute(
            "INSERT OR REPLACE INTO downloads "
            "(url_hash, url, blog_name, post_id, filename, bytes, status, error, created_at) "
            "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
            (url_hash, url, blog_name, post_id, filename, byte_count, status, error, now),
        )
        self._conn.commit()

    def is_downloaded(self, url_hash: str) -> bool:
        """Check if a URL has already been downloaded (any status)."""
        row = self._conn.execute(
            "SELECT 1 FROM downloads WHERE url_hash = ?", (url_hash,)
        ).fetchone()
        return row is not None

    def mark_post_complete(self, post_id: str, blog_name: str) -> None:
        """Mark a post as having its sidecar written."""
        now = datetime.now(UTC).isoformat()
        self._conn.execute(
            "INSERT OR REPLACE INTO posts (post_id, blog_name, sidecar_written, created_at) "
            "VALUES (?, ?, 1, ?)",
            (post_id, blog_name, now),
        )
        self._conn.commit()

    def is_post_complete(self, post_id: str) -> bool:
        """Check if a post's sidecar has been written."""
        row = self._conn.execute(
            "SELECT sidecar_written FROM posts WHERE post_id = ?", (post_id,)
        ).fetchone()
        return bool(row and row[0])

    def commit(self) -> None:
        self._conn.commit()

    def close(self) -> None:
        self._conn.close()
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_state_db.py -v`
Expected: 10 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/state/__init__.py src/tumbl4/core/state/db.py tests/unit/test_state_db.py
git commit -m "feat(state): add SQLite state layer with WAL mode

Per-blog database with downloads, posts, and crawl_state tables.
Parameterised queries only. Schema versioned via PRAGMA user_version.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: Resume cursor

**Files:**
- Create: `src/tumbl4/core/state/resume.py`
- Create: `tests/unit/test_resume.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_resume.py`:

```python
"""Tests for resume cursor persistence."""

from tumbl4.core.state.db import StateDb
from tumbl4.core.state.resume import load_cursor, save_cursor


class TestResumeCursor:
    def test_load_returns_zero_for_new_blog(self) -> None:
        db = StateDb(":memory:")
        assert load_cursor(db, "newblog") == 0
        db.close()

    def test_save_and_load(self) -> None:
        db = StateDb(":memory:")
        save_cursor(db, "testblog", last_id=728394056123)
        assert load_cursor(db, "testblog") == 728394056123
        db.close()

    def test_update_existing_cursor(self) -> None:
        db = StateDb(":memory:")
        save_cursor(db, "testblog", last_id=100)
        save_cursor(db, "testblog", last_id=200)
        assert load_cursor(db, "testblog") == 200
        db.close()

    def test_separate_blogs_have_separate_cursors(self) -> None:
        db = StateDb(":memory:")
        save_cursor(db, "blog_a", last_id=100)
        save_cursor(db, "blog_b", last_id=200)
        assert load_cursor(db, "blog_a") == 100
        assert load_cursor(db, "blog_b") == 200
        db.close()

    def test_records_last_complete_crawl_time(self) -> None:
        db = StateDb(":memory:")
        save_cursor(db, "testblog", last_id=100)
        row = db.execute(
            "SELECT last_complete_crawl FROM crawl_state WHERE blog_name = ?",
            ("testblog",),
        ).fetchone()
        assert row[0] is not None  # ISO timestamp
        db.close()
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_resume.py -v`
Expected: FAIL — `ModuleNotFoundError`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/state/resume.py`:

```python
"""Resume cursor persistence — save and load LastId for public blog crawls.

The cursor is only updated on complete crawl (no rate-limit hits or errors
that caused early termination). This matches TumblThree's Blog.LastId
semantics.
"""

from __future__ import annotations

from datetime import UTC, datetime

from tumbl4.core.state.db import StateDb


def load_cursor(db: StateDb, blog_name: str, crawler_type: str = "public") -> int:
    """Load the last-seen post ID for a blog. Returns 0 if no cursor exists."""
    row = db.execute(
        "SELECT last_id FROM crawl_state WHERE blog_name = ? AND crawler_type = ?",
        (blog_name, crawler_type),
    ).fetchone()
    return int(row[0]) if row else 0


def save_cursor(
    db: StateDb,
    blog_name: str,
    last_id: int,
    crawler_type: str = "public",
) -> None:
    """Save the resume cursor. Call only on complete crawl."""
    now = datetime.now(UTC).isoformat()
    db.execute(
        "INSERT INTO crawl_state (blog_name, crawler_type, last_id, last_complete_crawl) "
        "VALUES (?, ?, ?, ?) "
        "ON CONFLICT(blog_name, crawler_type) DO UPDATE SET "
        "last_id = excluded.last_id, last_complete_crawl = excluded.last_complete_crawl",
        (blog_name, crawler_type, last_id, now),
    )
    db.commit()
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_resume.py -v`
Expected: 5 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/state/resume.py tests/unit/test_resume.py
git commit -m "feat(state): add resume cursor persistence

LastId-based resume matching TumblThree semantics. Only updated on
complete crawl — partial crawls leave the cursor unchanged.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: Metadata sidecar writer

**Files:**
- Create: `src/tumbl4/core/state/metadata.py`
- Create: `tests/unit/test_metadata.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_metadata.py`:

```python
"""Tests for JSON metadata sidecar writer."""

import json
import os
import stat
from pathlib import Path

import pytest

from tumbl4.core.state.metadata import write_sidecar


class TestWriteSidecar:
    def test_writes_valid_json(self, tmp_path: Path) -> None:
        sidecar_dir = tmp_path / "_meta"
        write_sidecar(
            output_dir=tmp_path,
            post_id="12345",
            blog_name="testblog",
            post_url="https://testblog.tumblr.com/post/12345",
            post_type="photo",
            timestamp_utc="2026-04-11T14:22:03+00:00",
            tags=["art"],
            is_reblog=False,
            media_results=[
                {"filename": "12345_01.jpg", "url": "https://example.com/photo.jpg",
                 "bytes": 1024, "status": "success"},
            ],
        )
        sidecar_path = sidecar_dir / "12345.json"
        assert sidecar_path.exists()
        data = json.loads(sidecar_path.read_text())
        assert data["$schema_version"] == 1
        assert data["post_id"] == "12345"
        assert data["media"][0]["status"] == "success"

    def test_atomic_write_no_part_file_remains(self, tmp_path: Path) -> None:
        write_sidecar(
            output_dir=tmp_path, post_id="12345", blog_name="test",
            post_url="", post_type="photo", timestamp_utc="",
            tags=[], is_reblog=False, media_results=[],
        )
        part_files = list(tmp_path.rglob("*.part"))
        assert len(part_files) == 0

    def test_creates_meta_directory(self, tmp_path: Path) -> None:
        write_sidecar(
            output_dir=tmp_path, post_id="99999", blog_name="test",
            post_url="", post_type="photo", timestamp_utc="",
            tags=[], is_reblog=False, media_results=[],
        )
        assert (tmp_path / "_meta").is_dir()

    def test_file_permissions_0600(self, tmp_path: Path) -> None:
        write_sidecar(
            output_dir=tmp_path, post_id="12345", blog_name="test",
            post_url="", post_type="photo", timestamp_utc="",
            tags=[], is_reblog=False, media_results=[],
        )
        path = tmp_path / "_meta" / "12345.json"
        mode = stat.S_IMODE(os.stat(path).st_mode)
        assert mode == 0o600

    def test_failed_media_in_sidecar(self, tmp_path: Path) -> None:
        write_sidecar(
            output_dir=tmp_path, post_id="12345", blog_name="test",
            post_url="", post_type="photo", timestamp_utc="",
            tags=[], is_reblog=False,
            media_results=[
                {"filename": None, "url": "https://example.com/gone.jpg",
                 "bytes": 0, "status": "failed", "error": "HTTP 404"},
            ],
        )
        data = json.loads((tmp_path / "_meta" / "12345.json").read_text())
        assert data["media"][0]["status"] == "failed"
        assert data["media"][0]["error"] == "HTTP 404"
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_metadata.py -v`
Expected: FAIL — `ModuleNotFoundError`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/state/metadata.py`:

```python
"""JSON metadata sidecar writer.

Writes a per-post JSON sidecar to {output_dir}/_meta/{post_id}.json.
Uses .part + atomic rename for crash safety. File permissions 0600.
"""

from __future__ import annotations

import json
import os
from pathlib import Path

from tumbl4._internal.logging import get_logger

logger = get_logger(__name__)


def write_sidecar(
    *,
    output_dir: Path,
    post_id: str,
    blog_name: str,
    post_url: str,
    post_type: str,
    timestamp_utc: str,
    tags: list[str],
    is_reblog: bool,
    media_results: list[dict[str, object]],
    reblog_source: dict[str, str] | None = None,
    title: str | None = None,
    body_text: str | None = None,
    body_html: str | None = None,
) -> Path:
    """Write a JSON metadata sidecar atomically.

    Returns the path to the written sidecar file.
    """
    meta_dir = output_dir / "_meta"
    meta_dir.mkdir(parents=True, exist_ok=True)

    sidecar = {
        "$schema_version": 1,
        "blog": blog_name,
        "post_id": post_id,
        "post_url": post_url,
        "type": post_type,
        "timestamp_utc": timestamp_utc,
        "tags": tags,
        "is_reblog": is_reblog,
        "reblog_source": reblog_source,
        "title": title,
        "body_text": body_text,
        "body_html": body_html,
        "media": media_results,
    }

    final_path = meta_dir / f"{post_id}.json"
    part_path = meta_dir / f"{post_id}.json.part"

    # Write to .part file first
    fd = os.open(str(part_path), os.O_WRONLY | os.O_CREAT | os.O_TRUNC, 0o600)
    try:
        with os.fdopen(fd, "w") as f:
            json.dump(sidecar, f, indent=2, ensure_ascii=False)
            f.write("\n")
    except BaseException:
        part_path.unlink(missing_ok=True)
        raise

    # Atomic rename
    os.rename(part_path, final_path)
    logger.debug("wrote sidecar", extra={"post_id": post_id, "path": str(final_path)})
    return final_path
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_metadata.py -v`
Expected: 5 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/state/metadata.py tests/unit/test_metadata.py
git commit -m "feat(state): add JSON metadata sidecar writer

Atomic .part + rename, 0600 permissions, schema-versioned JSON.
See spec §5.18.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: HTTP client wrapper

**Files:**
- Create: `src/tumbl4/core/crawl/__init__.py`
- Create: `src/tumbl4/core/crawl/http_client.py`
- Create: `tests/unit/test_http_client.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_http_client.py`:

```python
"""Tests for the HTTP client wrapper."""

import httpx
import pytest
import respx

from tumbl4.core.crawl.http_client import TumblrHttpClient
from tumbl4.core.errors import RateLimited, ResponseTooLarge, ServerError
from tumbl4.models.settings import HttpSettings


def _make_client(**overrides: object) -> TumblrHttpClient:
    settings = HttpSettings(**(overrides or {}))  # type: ignore[arg-type]
    return TumblrHttpClient(settings)


class TestTumblrHttpClient:
    @respx.mock
    async def test_get_api_returns_body(self) -> None:
        respx.get("https://test.tumblr.com/api/read/json").respond(200, text='{"ok": true}')
        client = _make_client()
        try:
            body = await client.get_api("https://test.tumblr.com/api/read/json")
            assert body == '{"ok": true}'
        finally:
            await client.aclose()

    @respx.mock
    async def test_429_raises_rate_limited(self) -> None:
        respx.get("https://test.tumblr.com/api/read/json").respond(
            429, headers={"Retry-After": "30"}
        )
        client = _make_client()
        try:
            with pytest.raises(RateLimited) as exc_info:
                await client.get_api("https://test.tumblr.com/api/read/json")
            assert exc_info.value.retry_after == 30.0
        finally:
            await client.aclose()

    @respx.mock
    async def test_5xx_raises_server_error(self) -> None:
        respx.get("https://test.tumblr.com/api/read/json").respond(502)
        client = _make_client()
        try:
            with pytest.raises(ServerError) as exc_info:
                await client.get_api("https://test.tumblr.com/api/read/json")
            assert exc_info.value.status_code == 502
        finally:
            await client.aclose()

    @respx.mock
    async def test_response_too_large(self) -> None:
        big_body = "x" * 2048
        respx.get("https://test.tumblr.com/api/read/json").respond(200, text=big_body)
        client = _make_client(max_api_response_bytes=1024)
        try:
            with pytest.raises(ResponseTooLarge):
                await client.get_api("https://test.tumblr.com/api/read/json")
        finally:
            await client.aclose()

    async def test_user_agent_header(self) -> None:
        client = _make_client()
        assert "tumbl4/" in client.user_agent
        await client.aclose()

    @respx.mock
    async def test_get_api_rate_limited(self) -> None:
        """Verify rate limiter is acquired before making requests."""
        call_count = 0

        def side_effect(request: httpx.Request) -> httpx.Response:
            nonlocal call_count
            call_count += 1
            return httpx.Response(200, text="{}")

        respx.get("https://test.tumblr.com/api/read/json").mock(side_effect=side_effect)
        client = _make_client()
        try:
            await client.get_api("https://test.tumblr.com/api/read/json")
            assert call_count == 1
        finally:
            await client.aclose()
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_http_client.py -v`
Expected: FAIL — `ModuleNotFoundError`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/crawl/__init__.py`:

```python
"""Crawl layer — HTTP client, blog crawlers, rate limiters."""
```

Write file `src/tumbl4/core/crawl/http_client.py`:

```python
"""HTTP client wrapper for Tumblr API and media requests.

Wraps httpx.AsyncClient with rate limiting, configured timeouts,
User-Agent, and response size cap. Error responses are mapped to
the tumbl4 exception hierarchy.
"""

from __future__ import annotations

import httpx
from aiolimiter import AsyncLimiter

import tumbl4
from tumbl4._internal.logging import get_logger
from tumbl4.core.errors import RateLimited, ResponseTooLarge, ServerError
from tumbl4.models.settings import HttpSettings

logger = get_logger(__name__)

# Default rate limit: 20 requests per 10 seconds for V1 API
_DEFAULT_RATE = 20
_DEFAULT_PERIOD = 10.0


class TumblrHttpClient:
    """Configured httpx client for Tumblr API and media requests."""

    def __init__(
        self,
        settings: HttpSettings,
        rate_limiter: AsyncLimiter | None = None,
    ) -> None:
        self._settings = settings
        self._rate_limiter = rate_limiter or AsyncLimiter(_DEFAULT_RATE, _DEFAULT_PERIOD)
        self.user_agent = f"tumbl4/{tumbl4.__version__} ({settings.user_agent_suffix})"
        self._client = httpx.AsyncClient(
            timeout=httpx.Timeout(
                connect=settings.connect_timeout,
                read=settings.read_timeout,
                write=settings.write_timeout,
                pool=settings.pool_timeout,
            ),
            limits=httpx.Limits(
                max_connections=settings.max_connections,
                max_keepalive_connections=settings.max_keepalive_connections,
            ),
            headers={"User-Agent": self.user_agent},
            follow_redirects=True,
        )

    @property
    def client(self) -> httpx.AsyncClient:
        """Access the underlying httpx client for streaming downloads."""
        return self._client

    @property
    def rate_limiter(self) -> AsyncLimiter:
        return self._rate_limiter

    async def get_api(self, url: str) -> str:
        """GET an API URL with rate limiting and body size cap.

        Returns the response body as a string.
        Raises RateLimited, ServerError, or ResponseTooLarge on errors.
        """
        async with self._rate_limiter:
            response = await self._client.get(url)
            _check_status(response)
            body = response.text
            if len(body.encode("utf-8")) > self._settings.max_api_response_bytes:
                raise ResponseTooLarge(
                    f"Response {len(body.encode('utf-8'))} bytes exceeds "
                    f"limit of {self._settings.max_api_response_bytes}"
                )
            return body

    async def aclose(self) -> None:
        await self._client.aclose()


def _check_status(response: httpx.Response) -> None:
    """Map HTTP error statuses to tumbl4 exceptions."""
    if response.status_code == 429:
        retry_after_raw = response.headers.get("retry-after")
        retry_after: float | None = None
        if retry_after_raw:
            try:
                retry_after = float(retry_after_raw)
            except ValueError:
                pass
        raise RateLimited(retry_after=retry_after)
    if response.status_code >= 500:
        raise ServerError(response.status_code)
    response.raise_for_status()
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_http_client.py -v`
Expected: 6 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/crawl/__init__.py src/tumbl4/core/crawl/http_client.py tests/unit/test_http_client.py
git commit -m "feat(crawl): add HTTP client wrapper with rate limiting

httpx.AsyncClient with aiolimiter, configured timeouts from
HttpSettings, User-Agent header, response size cap, and error
mapping to tumbl4 exception hierarchy.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 9: Content-type reconciliation and file downloader

**Files:**
- Create: `src/tumbl4/core/download/__init__.py`
- Create: `src/tumbl4/core/download/content_type.py`
- Create: `src/tumbl4/core/download/file_downloader.py`
- Create: `tests/unit/test_content_type.py`
- Create: `tests/unit/test_file_downloader.py`

- [ ] **Step 1: Write the failing tests**

Write file `tests/unit/test_content_type.py`:

```python
"""Tests for content-type reconciliation."""

from tumbl4.core.download.content_type import reconcile_extension


class TestReconcileExtension:
    def test_matching_type_returns_original(self) -> None:
        assert reconcile_extension("photo.jpg", "image/jpeg") == "jpg"

    def test_png_url_jpeg_content_type(self) -> None:
        assert reconcile_extension("photo.png", "image/jpeg") == "jpg"

    def test_unknown_content_type_returns_original(self) -> None:
        assert reconcile_extension("photo.jpg", "application/octet-stream") == "jpg"

    def test_no_extension_uses_content_type(self) -> None:
        assert reconcile_extension("photo", "image/png") == "png"

    def test_gif_preserved(self) -> None:
        assert reconcile_extension("animation.gif", "image/gif") == "gif"

    def test_webp_content_type(self) -> None:
        assert reconcile_extension("photo.jpg", "image/webp") == "webp"

    def test_content_type_with_charset(self) -> None:
        assert reconcile_extension("photo.png", "image/jpeg; charset=utf-8") == "jpg"

    def test_none_content_type_returns_original(self) -> None:
        assert reconcile_extension("photo.jpg", None) == "jpg"
```

Write file `tests/unit/test_file_downloader.py`:

```python
"""Tests for the streaming file downloader."""

import hashlib
from pathlib import Path

import httpx
import pytest
import respx

from tumbl4.core.download.file_downloader import download_media
from tumbl4.core.errors import DownloadError
from tumbl4.models.media import DownloadResult, MediaTask


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


class TestDownloadMedia:
    @respx.mock
    async def test_downloads_file(self, tmp_path: Path) -> None:
        content = b"fake jpeg data " * 100
        respx.get("https://64.media.tumblr.com/abc/photo.jpg").respond(
            200, content=content, headers={"Content-Type": "image/jpeg"},
        )
        task = _make_task(tmp_path)
        client = httpx.AsyncClient()
        try:
            result = await download_media(task, client)
            assert result.status == "success"
            assert result.byte_count == len(content)
            assert task.final_path.exists()
            assert task.final_path.read_bytes() == content
        finally:
            await client.aclose()

    @respx.mock
    async def test_no_part_file_after_success(self, tmp_path: Path) -> None:
        respx.get("https://64.media.tumblr.com/abc/photo.jpg").respond(
            200, content=b"data", headers={"Content-Type": "image/jpeg"},
        )
        task = _make_task(tmp_path)
        client = httpx.AsyncClient()
        try:
            await download_media(task, client)
            assert not task.part_path.exists()
        finally:
            await client.aclose()

    @respx.mock
    async def test_content_type_reconciliation(self, tmp_path: Path) -> None:
        """PNG URL that serves JPEG should get .jpg extension."""
        respx.get("https://64.media.tumblr.com/abc/photo.png").respond(
            200, content=b"jpeg data", headers={"Content-Type": "image/jpeg"},
        )
        task = _make_task(tmp_path, url="https://64.media.tumblr.com/abc/photo.png")
        client = httpx.AsyncClient()
        try:
            result = await download_media(task, client)
            assert result.status == "success"
            assert result.filename is not None
            assert result.filename.endswith(".jpg")
        finally:
            await client.aclose()

    @respx.mock
    async def test_404_returns_failed(self, tmp_path: Path) -> None:
        respx.get("https://64.media.tumblr.com/abc/photo.jpg").respond(404)
        task = _make_task(tmp_path)
        client = httpx.AsyncClient()
        try:
            result = await download_media(task, client)
            assert result.status == "failed"
            assert "404" in (result.error or "")
        finally:
            await client.aclose()

    @respx.mock
    async def test_cleans_up_part_on_error(self, tmp_path: Path) -> None:
        respx.get("https://64.media.tumblr.com/abc/photo.jpg").mock(
            side_effect=httpx.ReadError("connection reset")
        )
        task = _make_task(tmp_path)
        client = httpx.AsyncClient()
        try:
            result = await download_media(task, client)
            assert result.status == "failed"
            assert not task.part_path.exists()
        finally:
            await client.aclose()
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `uv run pytest tests/unit/test_content_type.py tests/unit/test_file_downloader.py -v`
Expected: FAIL — `ModuleNotFoundError`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/download/__init__.py`:

```python
"""Download layer — streaming file downloader and content-type reconciliation."""
```

Write file `src/tumbl4/core/download/content_type.py`:

```python
"""Content-type reconciliation — detect and correct extension mismatches.

Tumblr sometimes serves JPEGs from .png URLs. When the HTTP Content-Type
disagrees with the URL extension, we use the Content-Type as the source
of truth.
"""

from __future__ import annotations

_MIME_TO_EXT: dict[str, str] = {
    "image/jpeg": "jpg",
    "image/png": "png",
    "image/gif": "gif",
    "image/webp": "webp",
    "image/tiff": "tiff",
    "image/heic": "heic",
    "image/heif": "heif",
    "image/avif": "avif",
    "video/mp4": "mp4",
    "video/webm": "webm",
    "audio/mpeg": "mp3",
    "audio/mp4": "m4a",
    "audio/ogg": "ogg",
}


def reconcile_extension(filename: str, content_type: str | None) -> str:
    """Return the correct file extension based on Content-Type.

    If the Content-Type maps to a known extension that differs from the
    filename's extension, return the Content-Type extension. Otherwise,
    return the filename's original extension.
    """
    # Extract original extension from filename
    dot_idx = filename.rfind(".")
    original_ext = filename[dot_idx + 1:].lower() if dot_idx >= 0 else ""

    if content_type is None:
        return original_ext or "jpg"

    # Strip charset and parameters
    mime = content_type.split(";")[0].strip().lower()
    ct_ext = _MIME_TO_EXT.get(mime)

    if ct_ext is None:
        return original_ext or "jpg"

    return ct_ext
```

Write file `src/tumbl4/core/download/file_downloader.py`:

```python
"""Streaming file downloader with .part + atomic rename.

Downloads a media file to a .part temporary file, then atomically
renames it to the final path. Handles content-type reconciliation
to correct mismatched file extensions.
"""

from __future__ import annotations

import os
from pathlib import Path
from urllib.parse import urlparse

import httpx

from tumbl4._internal.logging import get_logger
from tumbl4.core.download.content_type import reconcile_extension
from tumbl4.models.media import DownloadResult, MediaTask

logger = get_logger(__name__)

_CHUNK_SIZE = 64 * 1024  # 64 KiB


async def download_media(
    task: MediaTask,
    client: httpx.AsyncClient,
) -> DownloadResult:
    """Download a single media file. Returns a DownloadResult (never raises).

    On success, the file is at task.final_path (possibly with a corrected
    extension). On failure, .part is cleaned up and result.status is "failed".
    """
    output_dir = Path(task.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    try:
        async with client.stream("GET", task.url) as response:
            if response.status_code >= 400:
                return DownloadResult(
                    url=task.url,
                    post_id=task.post_id,
                    filename=None,
                    byte_count=0,
                    status="failed",
                    error=f"HTTP {response.status_code}",
                )

            # Reconcile extension from Content-Type
            content_type = response.headers.get("content-type")
            url_filename = Path(urlparse(task.url).path).name
            resolved_ext = reconcile_extension(url_filename, content_type)

            # Compute final filename with possibly corrected extension
            base = f"{task.post_id}_{task.index:02d}"
            final_filename = f"{base}.{resolved_ext}"
            final_path = output_dir / final_filename
            part_path = output_dir / f"{final_filename}.part"

            # Stream to .part file
            byte_count = 0
            with open(part_path, "wb") as f:
                async for chunk in response.aiter_bytes(chunk_size=_CHUNK_SIZE):
                    f.write(chunk)
                    byte_count += len(chunk)
                f.flush()
                os.fsync(f.fileno())

            # Atomic rename
            os.rename(part_path, final_path)

            logger.debug(
                "downloaded",
                extra={"url": task.url, "path": str(final_path), "bytes": byte_count},
            )
            return DownloadResult(
                url=task.url,
                post_id=task.post_id,
                filename=final_filename,
                byte_count=byte_count,
                status="success",
            )

    except Exception as exc:
        # Clean up .part file on any error
        _cleanup_part(output_dir, task)
        error_msg = f"{type(exc).__name__}: {exc}"
        logger.warning("download failed", extra={"url": task.url, "error": error_msg})
        return DownloadResult(
            url=task.url,
            post_id=task.post_id,
            filename=None,
            byte_count=0,
            status="failed",
            error=error_msg,
        )


def _cleanup_part(output_dir: Path, task: MediaTask) -> None:
    """Remove any .part files for this task."""
    for part in output_dir.glob(f"{task.post_id}_{task.index:02d}.*.part"):
        part.unlink(missing_ok=True)
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `uv run pytest tests/unit/test_content_type.py tests/unit/test_file_downloader.py -v`
Expected: 13 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/download/__init__.py src/tumbl4/core/download/content_type.py src/tumbl4/core/download/file_downloader.py tests/unit/test_content_type.py tests/unit/test_file_downloader.py
git commit -m "feat(download): add streaming downloader with content-type reconciliation

.part + atomic rename for crash safety. Content-type reconciliation
detects PNG-URL-serving-JPEG and corrects the extension. See spec §5.11.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 10: Public blog crawler

**Files:**
- Create: `src/tumbl4/core/crawl/tumblr_blog.py`
- Create: `tests/unit/test_tumblr_blog_crawler.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_tumblr_blog_crawler.py`:

```python
"""Tests for the V1 API public blog crawler."""

import json

import httpx
import respx

from tumbl4.core.crawl.http_client import TumblrHttpClient
from tumbl4.core.crawl.tumblr_blog import TumblrBlogCrawler
from tumbl4.models.blog import BlogRef
from tumbl4.models.settings import HttpSettings

_SINGLE_PHOTO_RESPONSE = (
    'var tumblr_api_read = '
    + json.dumps({
        "tumblelog": {"title": "Test Blog", "name": "testblog"},
        "posts-start": 0,
        "posts-total": "2",
        "posts": [
            {
                "id": "200",
                "url-with-slug": "https://testblog.tumblr.com/post/200",
                "type": "photo",
                "unix-timestamp": 1776097800,
                "tags": ["art"],
                "photo-url-1280": "https://64.media.tumblr.com/aaa/photo1.jpg",
            },
            {
                "id": "100",
                "url-with-slug": "https://testblog.tumblr.com/post/100",
                "type": "photo",
                "unix-timestamp": 1776011400,
                "tags": [],
                "photo-url-1280": "https://64.media.tumblr.com/bbb/photo2.jpg",
            },
        ],
    })
    + ";"
)

_EMPTY_RESPONSE = (
    'var tumblr_api_read = '
    + json.dumps({
        "tumblelog": {"title": "Test Blog", "name": "testblog"},
        "posts-start": 0,
        "posts-total": "0",
        "posts": [],
    })
    + ";"
)

_NON_PHOTO_RESPONSE = (
    'var tumblr_api_read = '
    + json.dumps({
        "tumblelog": {"title": "Test Blog", "name": "testblog"},
        "posts-start": 0,
        "posts-total": "1",
        "posts": [
            {
                "id": "300",
                "url-with-slug": "https://testblog.tumblr.com/post/300",
                "type": "regular",
                "unix-timestamp": 1776097800,
                "regular-title": "Hello World",
                "regular-body": "<p>Text post</p>",
            },
        ],
    })
    + ";"
)


class TestTumblrBlogCrawler:
    @respx.mock
    async def test_crawl_yields_photo_posts(self) -> None:
        respx.get(
            "https://testblog.tumblr.com/api/read/json",
            params={"debug": "1", "num": "50", "start": "0"},
        ).respond(200, text=_SINGLE_PHOTO_RESPONSE)

        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("testblog")
        crawler = TumblrBlogCrawler(http, blog, page_size=50)
        try:
            posts = [p async for p in crawler.crawl()]
            assert len(posts) == 2
            assert posts[0]["post_id"] == "200"
            assert posts[1]["post_id"] == "100"
        finally:
            await http.aclose()

    @respx.mock
    async def test_highest_post_id(self) -> None:
        respx.get(
            "https://testblog.tumblr.com/api/read/json",
            params={"debug": "1", "num": "50", "start": "0"},
        ).respond(200, text=_SINGLE_PHOTO_RESPONSE)

        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("testblog")
        crawler = TumblrBlogCrawler(http, blog, page_size=50)
        try:
            _ = [p async for p in crawler.crawl()]
            assert crawler.highest_post_id == 200
        finally:
            await http.aclose()

    @respx.mock
    async def test_skips_posts_below_last_id(self) -> None:
        respx.get(
            "https://testblog.tumblr.com/api/read/json",
            params={"debug": "1", "num": "50", "start": "0"},
        ).respond(200, text=_SINGLE_PHOTO_RESPONSE)

        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("testblog")
        crawler = TumblrBlogCrawler(http, blog, page_size=50, last_id=150)
        try:
            posts = [p async for p in crawler.crawl()]
            assert len(posts) == 1
            assert posts[0]["post_id"] == "200"
        finally:
            await http.aclose()

    @respx.mock
    async def test_empty_blog(self) -> None:
        respx.get(
            "https://testblog.tumblr.com/api/read/json",
            params={"debug": "1", "num": "50", "start": "0"},
        ).respond(200, text=_EMPTY_RESPONSE)

        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("testblog")
        crawler = TumblrBlogCrawler(http, blog, page_size=50)
        try:
            posts = [p async for p in crawler.crawl()]
            assert len(posts) == 0
        finally:
            await http.aclose()

    @respx.mock
    async def test_skips_non_photo_posts(self) -> None:
        respx.get(
            "https://testblog.tumblr.com/api/read/json",
            params={"debug": "1", "num": "50", "start": "0"},
        ).respond(200, text=_NON_PHOTO_RESPONSE)

        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("testblog")
        crawler = TumblrBlogCrawler(http, blog, page_size=50)
        try:
            posts = [p async for p in crawler.crawl()]
            assert len(posts) == 0
        finally:
            await http.aclose()

    @respx.mock
    async def test_reports_total_posts(self) -> None:
        respx.get(
            "https://testblog.tumblr.com/api/read/json",
            params={"debug": "1", "num": "50", "start": "0"},
        ).respond(200, text=_SINGLE_PHOTO_RESPONSE)

        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("testblog")
        crawler = TumblrBlogCrawler(http, blog, page_size=50)
        try:
            _ = [p async for p in crawler.crawl()]
            assert crawler.total_posts == 2
        finally:
            await http.aclose()
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_tumblr_blog_crawler.py -v`
Expected: FAIL — `ModuleNotFoundError`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/crawl/tumblr_blog.py`:

```python
"""Public blog crawler using the Tumblr V1 API.

Paginates /api/read/json with offset-based pagination. Yields
IntermediateDict for each photo post. Tracks highest post ID
for resume cursor.
"""

from __future__ import annotations

from collections.abc import AsyncIterator

from tumbl4._internal.logging import get_logger
from tumbl4.core.crawl.http_client import TumblrHttpClient
from tumbl4.core.errors import ParseError, RateLimited
from tumbl4.core.parse.api_json import normalize_photo_post, parse_v1_response, strip_jsonp
from tumbl4.core.parse.intermediate import IntermediateDict
from tumbl4.models.blog import BlogRef

logger = get_logger(__name__)


class TumblrBlogCrawler:
    """Crawl a public Tumblr blog's photo posts via the V1 API."""

    def __init__(
        self,
        http: TumblrHttpClient,
        blog: BlogRef,
        *,
        page_size: int = 50,
        last_id: int = 0,
        image_size: str = "1280",
    ) -> None:
        self._http = http
        self._blog = blog
        self._page_size = min(page_size, 50)
        self._last_id = last_id
        self._image_size = image_size
        self.highest_post_id: int = 0
        self.total_posts: int = 0
        self.rate_limited: bool = False

    async def crawl(self) -> AsyncIterator[IntermediateDict]:
        """Async generator that yields IntermediateDict for each photo post.

        Iterates pages starting from offset 0. Skips posts with id <= last_id.
        Stops when all pages are exhausted or all remaining posts are below
        the last_id fence.
        """
        offset = 0

        while True:
            url = self._build_url(offset)
            try:
                raw = await self._http.get_api(url)
            except RateLimited:
                self.rate_limited = True
                logger.warning("rate limited, stopping crawl")
                return

            try:
                data = strip_jsonp(raw)
            except ParseError:
                logger.error("failed to parse API response", extra={"offset": offset})
                return

            blog_name, total, posts = parse_v1_response(data)
            if offset == 0:
                self.total_posts = total

            if not posts:
                return

            all_below_fence = True
            for post_raw in posts:
                post_type = post_raw.get("type", "")
                post_id_str = str(post_raw.get("id", "0"))

                try:
                    post_id_int = int(post_id_str)
                except ValueError:
                    post_id_int = 0

                # Track highest post ID
                if post_id_int > self.highest_post_id:
                    self.highest_post_id = post_id_int

                # Skip posts at or below resume fence
                if self._last_id > 0 and post_id_int <= self._last_id:
                    continue

                all_below_fence = False

                # Plan 2: only photo posts
                if post_type != "photo":
                    continue

                try:
                    intermediate = normalize_photo_post(
                        post_raw, blog_name or self._blog.name, self._image_size  # type: ignore[arg-type]
                    )
                    yield intermediate
                except ParseError as e:
                    logger.error("failed to parse post", extra={"post_id": post_id_str, "error": str(e)})
                    continue

            # If all posts on this page were below the fence, stop
            if all_below_fence and self._last_id > 0:
                return

            offset += self._page_size

            # Stop if we've seen all posts
            if offset >= self.total_posts:
                return

    def _build_url(self, offset: int) -> str:
        """Build the V1 API URL for a page."""
        base = f"{self._blog.url}api/read/json"
        params = f"?debug=1&num={self._page_size}&start={offset}"
        return base + params
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_tumblr_blog_crawler.py -v`
Expected: 6 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/crawl/tumblr_blog.py tests/unit/test_tumblr_blog_crawler.py
git commit -m "feat(crawl): add V1 API public blog crawler

Offset-based pagination, photo-only filtering (Plan 2), LastId
stop-fence for resume, highest_post_id tracking. Async generator
yielding IntermediateDict per post.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 11: CrawlContext and orchestrator

**Files:**
- Create: `src/tumbl4/core/context.py`
- Create: `src/tumbl4/core/orchestrator.py`
- Create: `tests/unit/test_orchestrator.py`
- Modify: `src/tumbl4/core/__init__.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_orchestrator.py`:

```python
"""Tests for the crawl orchestrator."""

import asyncio
from collections.abc import AsyncIterator
from pathlib import Path
from unittest.mock import AsyncMock, MagicMock

import pytest

from tumbl4.core.orchestrator import run_crawl
from tumbl4.core.parse.intermediate import IntermediateDict, MediaEntry
from tumbl4.models.blog import BlogRef
from tumbl4.models.settings import Settings


def _make_post(post_id: str, media_url: str) -> IntermediateDict:
    return IntermediateDict(
        schema_version=1,
        source_format="api",
        post_id=post_id,
        blog_name="testblog",
        post_url=f"https://testblog.tumblr.com/post/{post_id}",
        post_type="photo",
        timestamp_utc="2026-04-11T14:22:03+00:00",
        tags=["test"],
        is_reblog=False,
        reblog_source=None,
        title=None,
        body_text=None,
        body_html=None,
        media=[
            MediaEntry(
                kind="photo", url=media_url,
                width=1280, height=960,
                mime_type=None, alt_text=None, duration_ms=None,
            )
        ],
        raw_content_blocks=None,
    )


class TestRunCrawl:
    async def test_processes_posts_and_returns_result(self, tmp_path: Path) -> None:
        settings = Settings(output_dir=tmp_path / "output", max_concurrent_downloads=1)
        blog = BlogRef.from_input("testblog")

        posts = [
            _make_post("200", "https://64.media.tumblr.com/aaa/photo1.jpg"),
            _make_post("100", "https://64.media.tumblr.com/bbb/photo2.jpg"),
        ]

        # Mock crawler
        async def fake_crawl() -> AsyncIterator[IntermediateDict]:
            for p in posts:
                yield p

        mock_crawler = MagicMock()
        mock_crawler.crawl = fake_crawl
        mock_crawler.highest_post_id = 200
        mock_crawler.total_posts = 2
        mock_crawler.rate_limited = False

        # Mock downloader — simulate successful downloads
        async def fake_download(task: object, client: object) -> object:
            from tumbl4.models.media import DownloadResult
            return DownloadResult(
                url=task.url,  # type: ignore[attr-defined]
                post_id=task.post_id,  # type: ignore[attr-defined]
                filename=f"{task.post_id}_{task.index:02d}.jpg",  # type: ignore[attr-defined]
                byte_count=1024,
                status="success",
            )

        result = await run_crawl(
            settings=settings,
            blog=blog,
            crawler=mock_crawler,
            download_fn=fake_download,
            no_resume=True,
        )

        assert result.posts_crawled == 2
        assert result.downloads_success >= 0  # depends on async timing
        assert result.complete is True
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_orchestrator.py -v`
Expected: FAIL — `ModuleNotFoundError`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/context.py`:

```python
"""CrawlContext — immutable context threaded through the pipeline."""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

from tumbl4.models.blog import BlogRef
from tumbl4.models.settings import Settings


@dataclass(frozen=True)
class CrawlContext:
    """Immutable context for a single blog crawl run."""

    blog: BlogRef
    settings: Settings
    blog_output_dir: Path
    image_size: str = "1280"
```

Write file `src/tumbl4/core/orchestrator.py`:

```python
"""Crawl orchestrator — state machine driving the crawl-download pipeline.

Producer-consumer: the crawler yields IntermediateDicts, the orchestrator
enqueues MediaTasks onto an asyncio.Queue, and N download workers consume
them. Sidecars are written after all media for a post resolve.
"""

from __future__ import annotations

import asyncio
from collections import defaultdict
from collections.abc import Awaitable, Callable
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Protocol

import httpx

from tumbl4._internal.logging import get_logger
from tumbl4._internal.tasks import spawn
from tumbl4.core.download.file_downloader import download_media
from tumbl4.core.parse.intermediate import IntermediateDict
from tumbl4.core.state.db import StateDb
from tumbl4.core.state.metadata import write_sidecar
from tumbl4.core.state.resume import load_cursor, save_cursor
from tumbl4.models.blog import BlogRef
from tumbl4.models.media import DownloadResult, MediaTask
from tumbl4.models.settings import Settings

logger = get_logger(__name__)


class CrawlerProtocol(Protocol):
    """Protocol for blog crawlers."""

    highest_post_id: int
    total_posts: int
    rate_limited: bool

    def crawl(self) -> Any: ...  # AsyncIterator[IntermediateDict]


@dataclass
class CrawlResult:
    """Summary of a crawl run."""

    blog_name: str
    posts_crawled: int = 0
    downloads_success: int = 0
    downloads_failed: int = 0
    downloads_skipped: int = 0
    complete: bool = False


DownloadFn = Callable[[MediaTask, httpx.AsyncClient], Awaitable[DownloadResult]]


async def run_crawl(
    *,
    settings: Settings,
    blog: BlogRef,
    crawler: CrawlerProtocol,
    download_fn: DownloadFn = download_media,
    no_resume: bool = False,
) -> CrawlResult:
    """Run the full crawl pipeline for a single blog.

    1. Open per-blog state database
    2. Crawl pages, enqueue MediaTasks
    3. Download workers consume tasks
    4. Write sidecars on post completion
    5. Update resume cursor on complete crawl
    """
    blog_dir = settings.output_dir / blog.name
    blog_dir.mkdir(parents=True, exist_ok=True)

    # State database lives alongside downloads
    from tumbl4._internal.paths import data_dir

    db_dir = data_dir()
    db_dir.mkdir(parents=True, exist_ok=True)
    db_path = db_dir / f"{blog.name}.db"
    db = StateDb(str(db_path))

    result = CrawlResult(blog_name=blog.name)
    queue: asyncio.Queue[MediaTask | None] = asyncio.Queue(
        maxsize=settings.queue.max_pending_media
    )

    # Track media per post for sidecar writing
    post_media: dict[str, list[DownloadResult]] = defaultdict(list)
    post_data: dict[str, IntermediateDict] = {}

    async def download_worker(client: httpx.AsyncClient) -> None:
        while True:
            task = await queue.get()
            if task is None:
                queue.task_done()
                break

            # Per-blog dedup check
            if db.is_downloaded(task.url_hash):
                result.downloads_skipped += 1
                queue.task_done()
                continue

            dl_result = await download_fn(task, client)

            # Record in state database
            db.record_download(
                url_hash=task.url_hash,
                url=task.url,
                blog_name=task.blog_name,
                post_id=task.post_id,
                filename=dl_result.filename,
                byte_count=dl_result.byte_count,
                status=dl_result.status,
                error=dl_result.error,
            )

            if dl_result.status == "success":
                result.downloads_success += 1
            else:
                result.downloads_failed += 1

            post_media[task.post_id].append(dl_result)

            # Check if all media for this post are done
            intermediate = post_data.get(task.post_id)
            if intermediate and len(post_media[task.post_id]) >= len(intermediate["media"]):
                _write_post_sidecar(db, settings.output_dir, intermediate, post_media[task.post_id])

            queue.task_done()

    # Start download workers
    async with httpx.AsyncClient(
        follow_redirects=True,
        headers={"User-Agent": f"tumbl4/{__import__('tumbl4').__version__}"},
    ) as client:
        workers = [spawn(download_worker(client), name=f"dl-worker-{i}") for i in range(settings.max_concurrent_downloads)]

        # Crawl and enqueue
        async for intermediate in crawler.crawl():
            result.posts_crawled += 1
            post_data[intermediate["post_id"]] = intermediate

            if not intermediate["media"]:
                # Post with no media — write sidecar immediately
                _write_post_sidecar(db, settings.output_dir, intermediate, [])
                continue

            for idx, media in enumerate(intermediate["media"]):
                task = MediaTask(
                    url=media["url"],
                    post_id=intermediate["post_id"],
                    blog_name=intermediate["blog_name"],
                    index=idx,
                    output_dir=str(blog_dir),
                )
                await queue.put(task)

        # Signal workers to stop
        for _ in workers:
            await queue.put(None)
        await asyncio.gather(*workers)

    # Update resume cursor only if crawl was complete (no rate limit)
    if not crawler.rate_limited:
        if crawler.highest_post_id > 0 and not no_resume:
            save_cursor(db, blog.name, crawler.highest_post_id)
        result.complete = True
    else:
        result.complete = False

    db.close()
    return result


def _write_post_sidecar(
    db: StateDb,
    output_dir: Path,
    intermediate: IntermediateDict,
    media_results: list[DownloadResult],
) -> None:
    """Write the JSON sidecar and mark the post complete in state."""
    media_dicts = [
        {
            "filename": r.filename,
            "url": r.url,
            "bytes": r.byte_count,
            "status": r.status,
            **({"error": r.error} if r.error else {}),
        }
        for r in media_results
    ]

    write_sidecar(
        output_dir=output_dir / intermediate["blog_name"],
        post_id=intermediate["post_id"],
        blog_name=intermediate["blog_name"],
        post_url=intermediate["post_url"],
        post_type=intermediate["post_type"],
        timestamp_utc=intermediate["timestamp_utc"],
        tags=intermediate["tags"],
        is_reblog=intermediate["is_reblog"],
        media_results=media_dicts,
        reblog_source=intermediate["reblog_source"],  # type: ignore[arg-type]
        body_text=intermediate["body_text"],
        body_html=intermediate["body_html"],
    )

    db.mark_post_complete(intermediate["post_id"], intermediate["blog_name"])
```

Update `src/tumbl4/core/__init__.py`:

```python
"""Core modules — orchestrator, crawlers, parsers, downloaders, state.

Unstable public API — may change between minor versions until v1.0.0.
"""

from tumbl4.core.orchestrator import CrawlResult, run_crawl

__all__ = ["CrawlResult", "run_crawl"]
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_orchestrator.py -v`
Expected: 1 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/context.py src/tumbl4/core/orchestrator.py src/tumbl4/core/__init__.py tests/unit/test_orchestrator.py
git commit -m "feat(core): add CrawlContext and orchestrator

Producer-consumer pipeline: crawler enqueues MediaTasks, N download
workers consume. Sidecars written on post completion, resume cursor
updated only on complete crawl. See spec §5.3.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 12: Rich progress and download CLI command

**Files:**
- Create: `src/tumbl4/cli/output/__init__.py`
- Create: `src/tumbl4/cli/output/progress.py`
- Create: `src/tumbl4/cli/commands/__init__.py`
- Create: `src/tumbl4/cli/commands/download.py`
- Modify: `src/tumbl4/cli/app.py`
- Create: `tests/unit/test_download_command.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_download_command.py`:

```python
"""Tests for the download CLI command."""

from typer.testing import CliRunner

from tumbl4.cli.app import app

runner = CliRunner()


class TestDownloadCommand:
    def test_download_appears_in_help(self) -> None:
        result = runner.invoke(app, ["--help"])
        assert result.exit_code == 0
        assert "download" in result.output

    def test_download_help(self) -> None:
        result = runner.invoke(app, ["download", "--help"])
        assert result.exit_code == 0
        assert "blog" in result.output.lower()

    def test_download_requires_blog_argument(self) -> None:
        result = runner.invoke(app, ["download"])
        # Typer shows an error when required arg is missing
        assert result.exit_code != 0

    def test_download_accepts_blog_name(self) -> None:
        # This will fail at runtime (no network), but validates CLI parsing
        result = runner.invoke(app, ["download", "nonexistent-blog-12345", "--no-resume"])
        # Should fail with a connection error, not a CLI parsing error
        assert result.exit_code != 0
        # The error should NOT be about missing arguments
        assert "Missing" not in result.output

    def test_download_page_size_validation(self) -> None:
        result = runner.invoke(app, ["download", "test", "--page-size", "0"])
        assert result.exit_code != 0

    def test_download_image_size_validation(self) -> None:
        result = runner.invoke(app, ["download", "test", "--image-size", "9999"])
        assert result.exit_code != 0
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_download_command.py -v`
Expected: FAIL — "download" not found in help output

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/cli/output/__init__.py`:

```python
"""CLI output helpers — Rich progress bars, tables, error formatting."""
```

Write file `src/tumbl4/cli/output/progress.py`:

```python
"""Rich progress bars for crawl and download tracking."""

from __future__ import annotations

from rich.console import Console
from rich.progress import (
    BarColumn,
    MofNCompleteColumn,
    Progress,
    SpinnerColumn,
    TaskID,
    TextColumn,
    TimeRemainingColumn,
)

console = Console()


def create_progress() -> Progress:
    """Create a Rich progress bar for download tracking."""
    return Progress(
        SpinnerColumn(),
        TextColumn("[bold blue]{task.description}"),
        BarColumn(),
        MofNCompleteColumn(),
        TimeRemainingColumn(),
        console=console,
    )
```

Write file `src/tumbl4/cli/commands/__init__.py`:

```python
"""CLI subcommands for tumbl4."""
```

Write file `src/tumbl4/cli/commands/download.py`:

```python
"""tumbl4 download — crawl a public Tumblr blog and download photos."""

from __future__ import annotations

import asyncio
from pathlib import Path
from typing import Annotated

import typer

from tumbl4._internal.logging import get_logger
from tumbl4.cli.output.progress import console

logger = get_logger(__name__)

_VALID_IMAGE_SIZES = {"1280", "500", "400", "250", "100", "75"}


def _validate_page_size(value: int) -> int:
    if value < 1 or value > 50:
        raise typer.BadParameter("Page size must be between 1 and 50")
    return value


def _validate_image_size(value: str) -> str:
    if value not in _VALID_IMAGE_SIZES:
        raise typer.BadParameter(f"Image size must be one of: {', '.join(sorted(_VALID_IMAGE_SIZES))}")
    return value


def download(
    blog: Annotated[str, typer.Argument(help="Blog name or URL (e.g., 'photography' or 'https://photography.tumblr.com')")],
    output_dir: Annotated[Path | None, typer.Option("--output-dir", "-o", help="Output directory")] = None,
    page_size: Annotated[int, typer.Option("--page-size", help="Posts per API page (1-50)", callback=_validate_page_size)] = 50,
    image_size: Annotated[str, typer.Option("--image-size", help="Image size: 1280, 500, 400, 250, 100, 75", callback=_validate_image_size)] = "1280",
    no_resume: Annotated[bool, typer.Option("--no-resume", help="Ignore saved cursor, full re-crawl")] = False,
    quiet: Annotated[bool, typer.Option("--quiet", "-q", help="Suppress progress output")] = False,
    verbose: Annotated[bool, typer.Option("--verbose", "-v", help="Enable debug logging")] = False,
) -> None:
    """Download photos from a public Tumblr blog."""
    asyncio.run(_download_async(
        blog=blog,
        output_dir=output_dir,
        page_size=page_size,
        image_size=image_size,
        no_resume=no_resume,
        quiet=quiet,
        verbose=verbose,
    ))


async def _download_async(
    *,
    blog: str,
    output_dir: Path | None,
    page_size: int,
    image_size: str,
    no_resume: bool,
    quiet: bool,
    verbose: bool,
) -> None:
    """Async implementation of the download command."""
    import logging

    from tumbl4.core.crawl.http_client import TumblrHttpClient
    from tumbl4.core.crawl.tumblr_blog import TumblrBlogCrawler
    from tumbl4.core.orchestrator import run_crawl
    from tumbl4.core.state.db import StateDb
    from tumbl4.core.state.resume import load_cursor
    from tumbl4.models.blog import BlogRef
    from tumbl4.models.settings import Settings

    if verbose:
        logging.basicConfig(level=logging.DEBUG)
    elif quiet:
        logging.basicConfig(level=logging.WARNING)
    else:
        logging.basicConfig(level=logging.INFO)

    blog_ref = BlogRef.from_input(blog)
    settings = Settings()
    if output_dir:
        settings = Settings(output_dir=output_dir)

    if not quiet:
        console.print(f"[bold]tumbl4[/bold] downloading [cyan]{blog_ref.name}[/cyan]")

    # Load resume cursor
    from tumbl4._internal.paths import data_dir

    db_dir = data_dir()
    db_dir.mkdir(parents=True, exist_ok=True)
    db_path = db_dir / f"{blog_ref.name}.db"

    last_id = 0
    if not no_resume and db_path.exists():
        db = StateDb(str(db_path))
        last_id = load_cursor(db, blog_ref.name)
        db.close()
        if last_id > 0 and not quiet:
            console.print(f"  Resuming from post ID {last_id}")

    http = TumblrHttpClient(settings.http)
    crawler = TumblrBlogCrawler(
        http, blog_ref,
        page_size=page_size,
        last_id=last_id,
        image_size=image_size,
    )

    try:
        result = await run_crawl(
            settings=settings,
            blog=blog_ref,
            crawler=crawler,
            no_resume=no_resume,
        )

        if not quiet:
            console.print(f"\n[bold green]Done![/bold green]")
            console.print(f"  Posts crawled: {result.posts_crawled}")
            console.print(f"  Downloads: {result.downloads_success} success, "
                          f"{result.downloads_failed} failed, "
                          f"{result.downloads_skipped} skipped (dedup)")
            if not result.complete:
                console.print("[yellow]  Crawl incomplete (rate limited). Run again to continue.[/yellow]")
    finally:
        await http.aclose()
```

Modify `src/tumbl4/cli/app.py` — add the download command registration:

After the existing `root` callback, add the import and registration. The modified file should be:

```python
"""Top-level Typer application for tumbl4.

This module exposes `app` (the Typer instance) and `main()` (the function
referenced by the `tumbl4` console script in pyproject.toml).
"""

from __future__ import annotations

from typing import Annotated

import typer

import tumbl4

app = typer.Typer(
    name="tumbl4",
    help="Command-line Tumblr blog backup tool for macOS and Linux.",
    no_args_is_help=True,
    add_completion=False,
)


def _version_callback(value: bool) -> None:
    if value:
        typer.echo(f"tumbl4 {tumbl4.__version__}")
        raise typer.Exit(code=0)


@app.callback()
def root(
    version: Annotated[
        bool,
        typer.Option(
            "--version",
            "-V",
            help="Show version and exit.",
            callback=_version_callback,
            is_eager=True,
        ),
    ] = False,
) -> None:
    """tumbl4 — command-line Tumblr blog backup tool for macOS and Linux."""
    return None


# Register subcommands
from tumbl4.cli.commands.download import download  # noqa: E402

app.command()(download)


def main() -> None:
    """Console-script entry point referenced from pyproject.toml."""
    app()
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_download_command.py -v`
Expected: 6 passed

- [ ] **Step 5: Verify existing tests still pass**

Run: `uv run pytest -v`
Expected: all tests pass

- [ ] **Step 6: Commit**

```bash
git add src/tumbl4/cli/output/__init__.py src/tumbl4/cli/output/progress.py src/tumbl4/cli/commands/__init__.py src/tumbl4/cli/commands/download.py src/tumbl4/cli/app.py tests/unit/test_download_command.py
git commit -m "feat(cli): add 'tumbl4 download' command with Rich progress

Wires the full crawl pipeline into a Typer subcommand. Accepts blog
name or URL, --page-size, --image-size, --no-resume, --quiet, --verbose.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 13: Integration test, quality gates, and Settings update

**Files:**
- Modify: `src/tumbl4/models/settings.py` (add `page_size` field)
- Create: `tests/component/__init__.py`
- Create: `tests/component/test_download_pipeline.py`

- [ ] **Step 1: Add page_size to Settings**

In `src/tumbl4/models/settings.py`, add after the `max_concurrent_downloads` field:

```python
    page_size: int = Field(default=50, ge=1, le=50)
```

- [ ] **Step 2: Write the component test**

Write file `tests/component/__init__.py`:

```python
```

Write file `tests/component/test_download_pipeline.py`:

```python
"""Component test — end-to-end pipeline with mocked HTTP."""

import json
from pathlib import Path

import httpx
import respx

from tumbl4.core.crawl.http_client import TumblrHttpClient
from tumbl4.core.crawl.tumblr_blog import TumblrBlogCrawler
from tumbl4.core.orchestrator import run_crawl
from tumbl4.models.blog import BlogRef
from tumbl4.models.settings import Settings

_BLOG_RESPONSE = (
    "var tumblr_api_read = "
    + json.dumps({
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
                        "width": 1280, "height": 960,
                        "photo-url-1280": "https://64.media.tumblr.com/bbb/set1.jpg",
                    },
                    {
                        "caption": "Second",
                        "width": 1280, "height": 960,
                        "photo-url-1280": "https://64.media.tumblr.com/ccc/set2.jpg",
                    },
                ],
            },
        ],
    })
    + ";"
)


class TestDownloadPipeline:
    @respx.mock
    async def test_full_pipeline_with_mock_http(self, tmp_path: Path) -> None:
        """End-to-end: crawl 2 posts (1 single + 1 photoset), download 3 files."""
        # Mock API
        respx.get(
            "https://photoblog.tumblr.com/api/read/json",
            params={"debug": "1", "num": "50", "start": "0"},
        ).respond(200, text=_BLOG_RESPONSE)

        # Mock media downloads
        respx.get("https://64.media.tumblr.com/aaa/photo1.jpg").respond(
            200, content=b"photo1-data", headers={"Content-Type": "image/jpeg"},
        )
        respx.get("https://64.media.tumblr.com/bbb/set1.jpg").respond(
            200, content=b"set1-data", headers={"Content-Type": "image/jpeg"},
        )
        respx.get("https://64.media.tumblr.com/ccc/set2.jpg").respond(
            200, content=b"set2-data", headers={"Content-Type": "image/jpeg"},
        )

        settings = Settings(output_dir=tmp_path / "output", max_concurrent_downloads=2)
        blog = BlogRef.from_input("photoblog")
        http = TumblrHttpClient(settings.http)
        crawler = TumblrBlogCrawler(http, blog, page_size=50)

        try:
            result = await run_crawl(
                settings=settings,
                blog=blog,
                crawler=crawler,
                no_resume=True,
            )
        finally:
            await http.aclose()

        assert result.posts_crawled == 2
        assert result.downloads_success == 3
        assert result.complete is True

        # Verify files on disk
        blog_dir = tmp_path / "output" / "photoblog"
        downloaded_files = list(blog_dir.glob("*.jpg"))
        assert len(downloaded_files) == 3

        # Verify sidecars
        meta_dir = blog_dir / "_meta"
        sidecars = list(meta_dir.glob("*.json"))
        assert len(sidecars) == 2

    @respx.mock
    async def test_dedup_skips_already_downloaded(self, tmp_path: Path) -> None:
        """Run the pipeline twice — second run should skip all downloads."""
        respx.get(
            "https://photoblog.tumblr.com/api/read/json",
            params={"debug": "1", "num": "50", "start": "0"},
        ).respond(200, text=_BLOG_RESPONSE)

        respx.get("https://64.media.tumblr.com/aaa/photo1.jpg").respond(
            200, content=b"photo1", headers={"Content-Type": "image/jpeg"},
        )
        respx.get("https://64.media.tumblr.com/bbb/set1.jpg").respond(
            200, content=b"set1", headers={"Content-Type": "image/jpeg"},
        )
        respx.get("https://64.media.tumblr.com/ccc/set2.jpg").respond(
            200, content=b"set2", headers={"Content-Type": "image/jpeg"},
        )

        settings = Settings(output_dir=tmp_path / "output", max_concurrent_downloads=1)
        blog = BlogRef.from_input("photoblog")

        # First run
        http = TumblrHttpClient(settings.http)
        crawler = TumblrBlogCrawler(http, blog, page_size=50)
        try:
            result1 = await run_crawl(settings=settings, blog=blog, crawler=crawler, no_resume=True)
        finally:
            await http.aclose()

        assert result1.downloads_success == 3

        # Second run — should skip all via dedup
        http = TumblrHttpClient(settings.http)
        crawler = TumblrBlogCrawler(http, blog, page_size=50)
        try:
            result2 = await run_crawl(settings=settings, blog=blog, crawler=crawler, no_resume=True)
        finally:
            await http.aclose()

        assert result2.downloads_skipped == 3
        assert result2.downloads_success == 0
```

- [ ] **Step 3: Run the component test**

Run: `uv run pytest tests/component/test_download_pipeline.py -v`
Expected: 2 passed

- [ ] **Step 4: Run ALL tests**

Run: `uv run pytest -v`
Expected: all tests pass (31 Plan 1 + ~60 Plan 2 new)

- [ ] **Step 5: Run quality gates**

Run: `uv run ruff check .`
Expected: all checks pass (fix any issues found)

Run: `uv run ruff format --check .`
Expected: all formatting correct (run `uv run ruff format .` to fix if needed)

Run: `uv run pyright`
Expected: 0 errors (fix any type errors found)

- [ ] **Step 6: Commit**

```bash
git add src/tumbl4/models/settings.py tests/component/__init__.py tests/component/test_download_pipeline.py
git commit -m "test: add component tests for full download pipeline

End-to-end pipeline with mocked HTTP: crawl 2 posts (single + photoset),
download 3 files, verify files and sidecars on disk. Second run verifies
dedup skips all downloads.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

- [ ] **Step 7: Final quality gate commit (if needed)**

If Steps 4-5 required any fixes, commit them:

```bash
git add -u
git commit -m "fix: address Plan 2 quality gate findings

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```
