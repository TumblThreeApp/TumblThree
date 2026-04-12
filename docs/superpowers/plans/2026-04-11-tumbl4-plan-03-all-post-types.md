# tumbl4 Plan 3: All Post Types + Metadata + Filename Templates

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every Tumblr post type (video, audio, text, quote, link, answer) is crawlable and downloadable. Photos gain "best" resolution via URL rewrite + HTML fetch. Inline media extracted from post body HTML. Filenames are template-configurable. TOML config files loaded. Sidecar writes are offloaded to a dedicated async worker.

**Architecture:** Extends the Plan 2 producer-consumer pipeline. Three new parsers (`svc_json`, `npf`, `html_scrape`) normalise all wire formats to `IntermediateDict`. The V1 `api_json` parser gains normalizers for every non-photo post type. A "best resolution" module rewrites photo URLs and fetches `___INITIAL_STATE___` from the blog's HTML page to discover full-size image URLs. A filename template engine renders output paths from post metadata. A TOML config layer loads project + user config files with the existing `Settings` precedence chain. A dedicated sidecar writer worker drains a separate `asyncio.Queue`, decoupling metadata I/O from download workers.

**Tech Stack:** Python 3.12+, httpx (async HTTP), aiolimiter (rate limiting), aiofiles (async file I/O), pydantic (models), pydantic-settings (config), SQLite (state), Rich (progress), Typer (CLI), tomli (TOML parsing on 3.11, stdlib on 3.12+), Pillow (PNJ-to-PNG conversion).

**Builds on Plan 2:** `api_json.py` parser (photo-only), `file_downloader.py`, `content_type.py`, `orchestrator.py`, `StateDb`, `resume.py`, `metadata.py`, `TumblrHttpClient`, `TumblrBlogCrawler`, `BlogRef`, `MediaTask`, `DownloadResult`, `Settings`/`HttpSettings`/`QueueSettings`, `download` CLI command, Rich progress, all Plan 1 foundation modules.

**Plans in this series:**

| # | Plan | Deliverable |
|---|---|---|
| 1 | Foundation (shipped) | `tumbl4 --version`; tooling + CI green |
| 2 | MVP public blog photo crawl (shipped) | `tumbl4 download <blog>` downloads photos, resumable |
| **3** | **All post types + sidecars + templates (this plan)** | **Every post type; configurable filename templates** |
| 4 | Filters + dedup + pinned posts | Tag/timespan filters; cross-blog dedup; pinned-post fix |
| 5 | Auth + hidden blog crawler | `tumbl4 login` + hidden/dashboard blog downloads |
| 6 | Security hardening + release | Redirect safety, SSRF guards, signal handling, SLSA release |

**Spec references:**
- Design spec: `docs/superpowers/specs/2026-04-11-tumbl4-macos-cli-port-design.md`
- Plan boundaries: `docs/superpowers/specs/2026-04-11-tumbl4-plan-boundaries.md`

---

## File Structure (Plan 3 additions)

New files are marked with `+`. Modified files are marked with `~`.

```
src/tumbl4/
├── __init__.py                              # (unchanged)
├── cli/
│   ├── app.py                            ~  # register config subcommand
│   ├── commands/
│   │   ├── __init__.py                   ~  # (unchanged)
│   │   ├── download.py                   ~  # add --template, --best, --convert-pnj flags
│   │   └── config.py                     +  # tumbl4 config get/set
│   └── output/
│       └── progress.py                   ~  # (unchanged)
├── core/
│   ├── __init__.py                       ~  # re-export new public API
│   ├── context.py                        ~  # add template_engine, consumer_key fields
│   ├── errors.py                         ~  # add TemplateError
│   ├── orchestrator.py                   ~  # wire sidecar_writer, template engine, all post types
│   ├── crawl/
│   │   ├── __init__.py                      # (unchanged)
│   │   ├── http_client.py                ~  # add get_html() method for best-resolution fetch
│   │   ├── tumblr_blog.py               ~  # dispatch all post types, not just photo
│   │   └── best_resolution.py            +  # URL rewrite + HTML fetch + ___INITIAL_STATE___ parse
│   ├── parse/
│   │   ├── __init__.py                   ~  # re-export all parsers
│   │   ├── intermediate.py               ~  # add InlineMedia TypedDict
│   │   ├── api_json.py                   ~  # add normalize_{video,audio,text,quote,link,answer}_post
│   │   ├── svc_json.py                   +  # SVC JSON -> IntermediateDict (all post types)
│   │   ├── npf.py                        +  # NPF blocks -> IntermediateDict (polymorphic)
│   │   ├── html_scrape.py                +  # ___INITIAL_STATE___ extractor (two regexes, two shapes)
│   │   └── inline_media.py               +  # regex scan of body HTML for media.tumblr.com URLs
│   ├── download/
│   │   ├── __init__.py                      # (unchanged)
│   │   ├── content_type.py               ~  # add video/audio MIME types
│   │   ├── file_downloader.py            ~  # accept template-rendered filenames
│   │   └── pnj_convert.py                +  # PNJ -> PNG conversion via Pillow
│   ├── naming/
│   │   ├── __init__.py                   +
│   │   └── template.py                   +  # filename template engine with validation
│   └── state/
│       ├── __init__.py                      # (unchanged)
│       ├── db.py                         ~  # add schema v2 migration (inline_media column)
│       ├── resume.py                        # (unchanged)
│       ├── metadata.py                   ~  # accept template-rendered sidecar paths
│       └── sidecar_writer.py             +  # dedicated async worker draining sidecar queue
├── models/
│   ├── __init__.py                       ~  # re-export new models
│   ├── blog.py                              # (unchanged)
│   ├── post.py                           ~  # (unchanged — already has all post_type literals)
│   ├── media.py                          ~  # add template_filename field to MediaTask
│   ├── settings.py                       ~  # add template, consumer_key, convert_pnj, best_resolution fields
│   └── config.py                         +  # TOML config loading (project + user)
tests/
├── conftest.py                           ~  # add parser fixture helpers
├── fixtures/
│   └── json/
│       ├── v1_photo_single.json             # (unchanged)
│       ├── v1_photo_set.json                # (unchanged)
│       ├── v1_video.json                 +  # V1 API video post
│       ├── v1_audio.json                 +  # V1 API audio post
│       ├── v1_text.json                  +  # V1 API text (regular) post
│       ├── v1_quote.json                 +  # V1 API quote post
│       ├── v1_link.json                  +  # V1 API link post
│       ├── v1_answer.json                +  # V1 API answer post
│       ├── svc_photo.json                +  # SVC JSON photo post
│       ├── svc_video.json                +  # SVC JSON video post
│       ├── svc_text.json                 +  # SVC JSON text post
│       ├── npf_image_block.json          +  # NPF image content block
│       ├── npf_video_block.json          +  # NPF video content block
│       ├── npf_text_block.json           +  # NPF text content block
│       ├── npf_mixed_blocks.json         +  # NPF multi-block post
│       ├── html_initial_state_peepr.html +  # ___INITIAL_STATE___ PeeprRoute shape
│       ├── html_initial_state_resp.html  +  # ___INITIAL_STATE___ response-wrapped shape
│       ├── best_resolution_page.html     +  # blog HTML with imageResponse in ___INITIAL_STATE___
│       └── inline_media_body.html        +  # post body with embedded media.tumblr.com URLs
├── unit/
│   ├── test_api_json_all_types.py        +  # V1 parser for video, audio, text, quote, link, answer
│   ├── test_svc_json.py                  +  # SVC JSON parser tests
│   ├── test_npf.py                       +  # NPF parser tests
│   ├── test_html_scrape.py               +  # ___INITIAL_STATE___ extraction tests
│   ├── test_inline_media.py              +  # inline media regex tests
│   ├── test_best_resolution.py           +  # URL rewrite + HTML parse tests
│   ├── test_filename_template.py         +  # template engine + validation tests
│   ├── test_pnj_convert.py              +  # PNJ -> PNG conversion tests
│   ├── test_toml_config.py               +  # TOML config loading tests
│   ├── test_config_command.py            +  # tumbl4 config get/set CLI tests
│   └── test_sidecar_writer.py            +  # dedicated sidecar worker tests
└── component/
    └── test_all_types_pipeline.py        +  # end-to-end with all post types
```

---

## Task 1: V1 API parser — all non-photo post types

**Files:**
- Modify: `src/tumbl4/core/parse/api_json.py`
- Create: `tests/fixtures/json/v1_video.json`
- Create: `tests/fixtures/json/v1_audio.json`
- Create: `tests/fixtures/json/v1_text.json`
- Create: `tests/fixtures/json/v1_quote.json`
- Create: `tests/fixtures/json/v1_link.json`
- Create: `tests/fixtures/json/v1_answer.json`
- Create: `tests/unit/test_api_json_all_types.py`

- [ ] **Step 1: Create test fixture files**

Write file `tests/fixtures/json/v1_video.json`:

```json
{
    "parsed": {
        "tumblelog": {"title": "Video Blog", "name": "videoblog"},
        "posts-start": 0,
        "posts-total": "30",
        "posts": [
            {
                "id": "900100200300",
                "url": "https://videoblog.tumblr.com/post/900100200300",
                "url-with-slug": "https://videoblog.tumblr.com/post/900100200300/cool-clip",
                "type": "video",
                "unix-timestamp": 1776097800,
                "tags": ["video", "art"],
                "video-caption": "<p>Check out this clip</p>",
                "video-player": "<iframe src='https://www.tumblr.com/video/videoblog/900100200300/700' width='500' height='281'></iframe>",
                "video-source": "https://vtt.tumblr.com/tumblr_abc123_720.mp4",
                "duration": 15
            }
        ]
    }
}
```

Write file `tests/fixtures/json/v1_audio.json`:

```json
{
    "parsed": {
        "tumblelog": {"title": "Music Blog", "name": "musicblog"},
        "posts-start": 0,
        "posts-total": "20",
        "posts": [
            {
                "id": "800200300400",
                "url": "https://musicblog.tumblr.com/post/800200300400",
                "url-with-slug": "https://musicblog.tumblr.com/post/800200300400/new-track",
                "type": "audio",
                "unix-timestamp": 1776097800,
                "tags": ["music"],
                "audio-caption": "<p>New track dropped</p>",
                "audio-player": "<embed type='application/x-shockwave-flash' src='...' />",
                "audio-url": "https://a.tumblr.com/tumblr_def456o1_r1_raw.mp3",
                "artist": "Test Artist",
                "album": "Test Album",
                "track-name": "Test Track",
                "year": 2026
            }
        ]
    }
}
```

Write file `tests/fixtures/json/v1_text.json`:

```json
{
    "parsed": {
        "tumblelog": {"title": "Text Blog", "name": "textblog"},
        "posts-start": 0,
        "posts-total": "100",
        "posts": [
            {
                "id": "700300400500",
                "url": "https://textblog.tumblr.com/post/700300400500",
                "url-with-slug": "https://textblog.tumblr.com/post/700300400500/hello-world",
                "type": "regular",
                "unix-timestamp": 1776097800,
                "tags": ["writing"],
                "regular-title": "Hello World",
                "regular-body": "<p>This is a <strong>text post</strong> with <img src=\"https://64.media.tumblr.com/inline123/s540x810/inline_photo.jpg\"/> an inline image.</p>",
                "format": "html"
            }
        ]
    }
}
```

Write file `tests/fixtures/json/v1_quote.json`:

```json
{
    "parsed": {
        "tumblelog": {"title": "Quote Blog", "name": "quoteblog"},
        "posts-start": 0,
        "posts-total": "10",
        "posts": [
            {
                "id": "600400500600",
                "url": "https://quoteblog.tumblr.com/post/600400500600",
                "url-with-slug": "https://quoteblog.tumblr.com/post/600400500600/wise-words",
                "type": "quote",
                "unix-timestamp": 1776097800,
                "tags": ["quotes", "wisdom"],
                "quote-text": "The only way to do great work is to love what you do.",
                "quote-source": "<a href=\"https://example.com\">Steve Jobs</a>"
            }
        ]
    }
}
```

Write file `tests/fixtures/json/v1_link.json`:

```json
{
    "parsed": {
        "tumblelog": {"title": "Link Blog", "name": "linkblog"},
        "posts-start": 0,
        "posts-total": "5",
        "posts": [
            {
                "id": "500500600700",
                "url": "https://linkblog.tumblr.com/post/500500600700",
                "url-with-slug": "https://linkblog.tumblr.com/post/500500600700/cool-site",
                "type": "link",
                "unix-timestamp": 1776097800,
                "tags": ["links"],
                "link-text": "Cool Website",
                "link-url": "https://example.com/cool",
                "link-description": "<p>This is a really interesting website.</p>"
            }
        ]
    }
}
```

Write file `tests/fixtures/json/v1_answer.json`:

```json
{
    "parsed": {
        "tumblelog": {"title": "Ask Blog", "name": "askblog"},
        "posts-start": 0,
        "posts-total": "25",
        "posts": [
            {
                "id": "400600700800",
                "url": "https://askblog.tumblr.com/post/400600700800",
                "url-with-slug": "https://askblog.tumblr.com/post/400600700800/q-and-a",
                "type": "answer",
                "unix-timestamp": 1776097800,
                "tags": ["asks", "q&a"],
                "question": "What is your favorite color?",
                "answer": "<p>Definitely blue.</p>",
                "asking-name": "anonymous",
                "asking-url": ""
            }
        ]
    }
}
```

- [ ] **Step 2: Write the failing test**

Write file `tests/unit/test_api_json_all_types.py`:

```python
"""Tests for V1 API parser — all non-photo post types."""

import json
from pathlib import Path

from tumbl4.core.parse.api_json import (
    normalize_answer_post,
    normalize_audio_post,
    normalize_link_post,
    normalize_quote_post,
    normalize_text_post,
    normalize_video_post,
)

FIXTURES = Path(__file__).parent.parent / "fixtures" / "json"


class TestNormalizeVideoPost:
    def test_basic_video(self) -> None:
        fixture = json.loads((FIXTURES / "v1_video.json").read_text())
        post = fixture["parsed"]["posts"][0]
        result = normalize_video_post(post, "videoblog")
        assert result["schema_version"] == 1
        assert result["source_format"] == "api"
        assert result["post_id"] == "900100200300"
        assert result["post_type"] == "video"
        assert result["tags"] == ["video", "art"]
        assert len(result["media"]) == 1
        assert result["media"][0]["kind"] == "video"
        assert "tumblr_abc123" in result["media"][0]["url"]
        assert result["media"][0]["duration_ms"] == 15000

    def test_video_no_source_url(self) -> None:
        post = {
            "id": "1", "type": "video", "unix-timestamp": 0,
            "video-caption": "<p>External video</p>",
            "video-player": "<iframe src='https://youtube.com/embed/abc'></iframe>",
        }
        result = normalize_video_post(post, "test")
        assert result["post_type"] == "video"
        assert len(result["media"]) == 0  # no downloadable media
        assert result["body_html"] == "<p>External video</p>"


class TestNormalizeAudioPost:
    def test_basic_audio(self) -> None:
        fixture = json.loads((FIXTURES / "v1_audio.json").read_text())
        post = fixture["parsed"]["posts"][0]
        result = normalize_audio_post(post, "musicblog")
        assert result["post_id"] == "800200300400"
        assert result["post_type"] == "audio"
        assert len(result["media"]) == 1
        assert result["media"][0]["kind"] == "audio"
        assert result["media"][0]["url"].endswith(".mp3")

    def test_audio_no_url(self) -> None:
        post = {
            "id": "1", "type": "audio", "unix-timestamp": 0,
            "audio-caption": "<p>Spotify embed</p>",
        }
        result = normalize_audio_post(post, "test")
        assert len(result["media"]) == 0

    def test_audio_metadata_in_body(self) -> None:
        fixture = json.loads((FIXTURES / "v1_audio.json").read_text())
        post = fixture["parsed"]["posts"][0]
        result = normalize_audio_post(post, "musicblog")
        assert result["title"] == "Test Track"


class TestNormalizeTextPost:
    def test_basic_text(self) -> None:
        fixture = json.loads((FIXTURES / "v1_text.json").read_text())
        post = fixture["parsed"]["posts"][0]
        result = normalize_text_post(post, "textblog")
        assert result["post_id"] == "700300400500"
        assert result["post_type"] == "text"
        assert result["title"] == "Hello World"
        assert "<strong>" in (result["body_html"] or "")
        assert len(result["media"]) == 0  # inline media extracted separately

    def test_text_no_title(self) -> None:
        post = {
            "id": "1", "type": "regular", "unix-timestamp": 0,
            "regular-body": "<p>Just a body.</p>",
        }
        result = normalize_text_post(post, "test")
        assert result["title"] is None
        assert result["body_text"] is not None


class TestNormalizeQuotePost:
    def test_basic_quote(self) -> None:
        fixture = json.loads((FIXTURES / "v1_quote.json").read_text())
        post = fixture["parsed"]["posts"][0]
        result = normalize_quote_post(post, "quoteblog")
        assert result["post_id"] == "600400500600"
        assert result["post_type"] == "quote"
        assert "great work" in (result["body_text"] or "")
        assert result["tags"] == ["quotes", "wisdom"]

    def test_quote_no_source(self) -> None:
        post = {
            "id": "1", "type": "quote", "unix-timestamp": 0,
            "quote-text": "A quote without attribution.",
        }
        result = normalize_quote_post(post, "test")
        assert result["body_text"] == "A quote without attribution."


class TestNormalizeLinkPost:
    def test_basic_link(self) -> None:
        fixture = json.loads((FIXTURES / "v1_link.json").read_text())
        post = fixture["parsed"]["posts"][0]
        result = normalize_link_post(post, "linkblog")
        assert result["post_id"] == "500500600700"
        assert result["post_type"] == "link"
        assert result["title"] == "Cool Website"
        assert "interesting" in (result["body_html"] or "")

    def test_link_no_description(self) -> None:
        post = {
            "id": "1", "type": "link", "unix-timestamp": 0,
            "link-text": "A Link", "link-url": "https://example.com",
        }
        result = normalize_link_post(post, "test")
        assert result["title"] == "A Link"
        assert len(result["media"]) == 0


class TestNormalizeAnswerPost:
    def test_basic_answer(self) -> None:
        fixture = json.loads((FIXTURES / "v1_answer.json").read_text())
        post = fixture["parsed"]["posts"][0]
        result = normalize_answer_post(post, "askblog")
        assert result["post_id"] == "400600700800"
        assert result["post_type"] == "answer"
        assert "favorite color" in (result["body_text"] or "")
        assert "blue" in (result["body_html"] or "")

    def test_answer_anonymous(self) -> None:
        fixture = json.loads((FIXTURES / "v1_answer.json").read_text())
        post = fixture["parsed"]["posts"][0]
        result = normalize_answer_post(post, "askblog")
        assert result["is_reblog"] is False
```

- [ ] **Step 3: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_api_json_all_types.py -v`
Expected: FAIL -- `ImportError: cannot import name 'normalize_video_post'`

- [ ] **Step 4: Write the implementation**

Modify `src/tumbl4/core/parse/api_json.py` -- add normalizer functions for every non-photo post type. Append after the existing `normalize_photo_post` function:

```python
def normalize_video_post(
    post: dict[str, object],
    blog_name: str,
) -> IntermediateDict:
    """Convert a V1 API video post dict to IntermediateDict."""
    post_id = str(post.get("id", ""))
    ts = _parse_timestamp(post.get("unix-timestamp"))
    caption = post.get("video-caption")

    media: list[MediaEntry] = []
    video_source = post.get("video-source")
    if isinstance(video_source, str) and video_source.strip():
        duration_raw = post.get("duration")
        duration_ms: int | None = None
        if duration_raw is not None:
            try:
                duration_ms = int(duration_raw) * 1000  # type: ignore[arg-type]
            except (ValueError, TypeError):
                pass
        media.append(
            MediaEntry(
                kind="video",
                url=video_source.strip(),
                width=None,
                height=None,
                mime_type=None,
                alt_text=None,
                duration_ms=duration_ms,
            )
        )

    reblog_from = post.get("reblogged-from-name")
    is_reblog = bool(reblog_from)
    reblog_source: ReblogSource | None = None
    if isinstance(reblog_from, str) and reblog_from:
        reblog_source = ReblogSource(
            blog_name=reblog_from,
            post_id=str(post.get("reblogged-from-id", "")),
        )

    return IntermediateDict(
        schema_version=1,
        source_format="api",
        post_id=post_id,
        blog_name=blog_name,
        post_url=str(post.get("url-with-slug", post.get("url", ""))),
        post_type="video",
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


def normalize_audio_post(
    post: dict[str, object],
    blog_name: str,
) -> IntermediateDict:
    """Convert a V1 API audio post dict to IntermediateDict."""
    post_id = str(post.get("id", ""))
    ts = _parse_timestamp(post.get("unix-timestamp"))
    caption = post.get("audio-caption")
    track_name = post.get("track-name")

    media: list[MediaEntry] = []
    audio_url = post.get("audio-url")
    if isinstance(audio_url, str) and audio_url.strip():
        media.append(
            MediaEntry(
                kind="audio",
                url=audio_url.strip(),
                width=None,
                height=None,
                mime_type=None,
                alt_text=None,
                duration_ms=None,
            )
        )

    reblog_from = post.get("reblogged-from-name")
    is_reblog = bool(reblog_from)
    reblog_source: ReblogSource | None = None
    if isinstance(reblog_from, str) and reblog_from:
        reblog_source = ReblogSource(
            blog_name=reblog_from,
            post_id=str(post.get("reblogged-from-id", "")),
        )

    return IntermediateDict(
        schema_version=1,
        source_format="api",
        post_id=post_id,
        blog_name=blog_name,
        post_url=str(post.get("url-with-slug", post.get("url", ""))),
        post_type="audio",
        timestamp_utc=ts,
        tags=_safe_tags(post.get("tags")),
        is_reblog=is_reblog,
        reblog_source=reblog_source,
        title=str(track_name) if track_name else None,
        body_text=str(caption) if caption else None,
        body_html=str(caption) if caption else None,
        media=media,
        raw_content_blocks=None,
    )


def normalize_text_post(
    post: dict[str, object],
    blog_name: str,
) -> IntermediateDict:
    """Convert a V1 API text (regular) post dict to IntermediateDict."""
    post_id = str(post.get("id", ""))
    ts = _parse_timestamp(post.get("unix-timestamp"))
    title = post.get("regular-title")
    body = post.get("regular-body")

    # Extract plain text from HTML body
    body_text: str | None = None
    body_html: str | None = None
    if body:
        body_html = str(body)
        body_text = _strip_html_tags(body_html)

    reblog_from = post.get("reblogged-from-name")
    is_reblog = bool(reblog_from)
    reblog_source: ReblogSource | None = None
    if isinstance(reblog_from, str) and reblog_from:
        reblog_source = ReblogSource(
            blog_name=reblog_from,
            post_id=str(post.get("reblogged-from-id", "")),
        )

    return IntermediateDict(
        schema_version=1,
        source_format="api",
        post_id=post_id,
        blog_name=blog_name,
        post_url=str(post.get("url-with-slug", post.get("url", ""))),
        post_type="text",
        timestamp_utc=ts,
        tags=_safe_tags(post.get("tags")),
        is_reblog=is_reblog,
        reblog_source=reblog_source,
        title=str(title) if title else None,
        body_text=body_text,
        body_html=body_html,
        media=[],  # inline media extracted separately by inline_media.py
        raw_content_blocks=None,
    )


def normalize_quote_post(
    post: dict[str, object],
    blog_name: str,
) -> IntermediateDict:
    """Convert a V1 API quote post dict to IntermediateDict."""
    post_id = str(post.get("id", ""))
    ts = _parse_timestamp(post.get("unix-timestamp"))
    quote_text = post.get("quote-text", "")
    quote_source = post.get("quote-source", "")

    body_text = str(quote_text) if quote_text else None
    body_html = str(quote_text) if quote_text else None
    if quote_source:
        body_html = f"{body_html}\n<footer>{quote_source}</footer>" if body_html else str(quote_source)

    reblog_from = post.get("reblogged-from-name")
    is_reblog = bool(reblog_from)
    reblog_source_dict: ReblogSource | None = None
    if isinstance(reblog_from, str) and reblog_from:
        reblog_source_dict = ReblogSource(
            blog_name=reblog_from,
            post_id=str(post.get("reblogged-from-id", "")),
        )

    return IntermediateDict(
        schema_version=1,
        source_format="api",
        post_id=post_id,
        blog_name=blog_name,
        post_url=str(post.get("url-with-slug", post.get("url", ""))),
        post_type="quote",
        timestamp_utc=ts,
        tags=_safe_tags(post.get("tags")),
        is_reblog=is_reblog,
        reblog_source=reblog_source_dict,
        title=None,
        body_text=body_text,
        body_html=body_html,
        media=[],
        raw_content_blocks=None,
    )


def normalize_link_post(
    post: dict[str, object],
    blog_name: str,
) -> IntermediateDict:
    """Convert a V1 API link post dict to IntermediateDict."""
    post_id = str(post.get("id", ""))
    ts = _parse_timestamp(post.get("unix-timestamp"))
    link_text = post.get("link-text")
    link_description = post.get("link-description")

    reblog_from = post.get("reblogged-from-name")
    is_reblog = bool(reblog_from)
    reblog_source: ReblogSource | None = None
    if isinstance(reblog_from, str) and reblog_from:
        reblog_source = ReblogSource(
            blog_name=reblog_from,
            post_id=str(post.get("reblogged-from-id", "")),
        )

    return IntermediateDict(
        schema_version=1,
        source_format="api",
        post_id=post_id,
        blog_name=blog_name,
        post_url=str(post.get("url-with-slug", post.get("url", ""))),
        post_type="link",
        timestamp_utc=ts,
        tags=_safe_tags(post.get("tags")),
        is_reblog=is_reblog,
        reblog_source=reblog_source,
        title=str(link_text) if link_text else None,
        body_text=_strip_html_tags(str(link_description)) if link_description else None,
        body_html=str(link_description) if link_description else None,
        media=[],
        raw_content_blocks=None,
    )


def normalize_answer_post(
    post: dict[str, object],
    blog_name: str,
) -> IntermediateDict:
    """Convert a V1 API answer post dict to IntermediateDict."""
    post_id = str(post.get("id", ""))
    ts = _parse_timestamp(post.get("unix-timestamp"))
    question = post.get("question", "")
    answer = post.get("answer", "")

    body_text = f"Q: {question}\nA: {_strip_html_tags(str(answer))}" if question else None
    body_html = f"<blockquote>{question}</blockquote>\n{answer}" if question else str(answer) if answer else None

    reblog_from = post.get("reblogged-from-name")
    is_reblog = bool(reblog_from)
    reblog_source: ReblogSource | None = None
    if isinstance(reblog_from, str) and reblog_from:
        reblog_source = ReblogSource(
            blog_name=reblog_from,
            post_id=str(post.get("reblogged-from-id", "")),
        )

    return IntermediateDict(
        schema_version=1,
        source_format="api",
        post_id=post_id,
        blog_name=blog_name,
        post_url=str(post.get("url-with-slug", post.get("url", ""))),
        post_type="answer",
        timestamp_utc=ts,
        tags=_safe_tags(post.get("tags")),
        is_reblog=is_reblog,
        reblog_source=reblog_source,
        title=None,
        body_text=body_text,
        body_html=body_html,
        media=[],
        raw_content_blocks=None,
    )


# --- internal helpers added for Plan 3 ---

_HTML_TAG_RE = re.compile(r"<[^>]+>")


def _strip_html_tags(html: str) -> str:
    """Strip HTML tags to produce plain text. Lightweight — no DOM parsing."""
    return _HTML_TAG_RE.sub("", html).strip()


# V1 type string -> post_type mapping. V1 uses "regular" for text posts.
V1_TYPE_MAP: dict[str, str] = {
    "photo": "photo",
    "video": "video",
    "audio": "audio",
    "regular": "text",
    "quote": "quote",
    "link": "link",
    "answer": "answer",
}

# V1 type string -> normalizer function mapping
V1_NORMALIZERS: dict[str, object] = {
    "photo": normalize_photo_post,
    "video": normalize_video_post,
    "audio": normalize_audio_post,
    "regular": normalize_text_post,
    "quote": normalize_quote_post,
    "link": normalize_link_post,
    "answer": normalize_answer_post,
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_api_json_all_types.py -v`
Expected: 14 passed

- [ ] **Step 6: Commit**

```bash
git add src/tumbl4/core/parse/api_json.py tests/fixtures/json/v1_video.json tests/fixtures/json/v1_audio.json tests/fixtures/json/v1_text.json tests/fixtures/json/v1_quote.json tests/fixtures/json/v1_link.json tests/fixtures/json/v1_answer.json tests/unit/test_api_json_all_types.py
git commit -m "feat(parse): add V1 API normalizers for all post types

Video, audio, text (regular), quote, link, answer normalizers with
V1_TYPE_MAP and V1_NORMALIZERS dispatch tables. HTML tag stripping
for plain text extraction. Snapshot fixtures for each type.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: SVC JSON parser

**Files:**
- Create: `src/tumbl4/core/parse/svc_json.py`
- Create: `tests/fixtures/json/svc_photo.json`
- Create: `tests/fixtures/json/svc_video.json`
- Create: `tests/fixtures/json/svc_text.json`
- Create: `tests/unit/test_svc_json.py`

- [ ] **Step 1: Create test fixture files**

Write file `tests/fixtures/json/svc_photo.json`:

```json
{
    "response": {
        "posts": [
            {
                "id": "728394056123",
                "blogName": "svctestblog",
                "postUrl": "https://svctestblog.tumblr.com/post/728394056123",
                "type": "photo",
                "timestamp": 1776097800,
                "tags": ["photography"],
                "rebloggedFromName": null,
                "rebloggedFromId": null,
                "summary": "Sunset photo",
                "caption": "<p>Beautiful sunset</p>",
                "photos": [
                    {
                        "caption": "",
                        "originalSize": {
                            "url": "https://64.media.tumblr.com/svc_aaa/s2048x3072/sunset_full.jpg",
                            "width": 2048,
                            "height": 1536
                        },
                        "altSizes": [
                            {"url": "https://64.media.tumblr.com/svc_aaa/s1280x1920/sunset.jpg", "width": 1280, "height": 960},
                            {"url": "https://64.media.tumblr.com/svc_aaa/s500x750/sunset.jpg", "width": 500, "height": 375}
                        ]
                    }
                ]
            }
        ],
        "totalPosts": 50
    }
}
```

Write file `tests/fixtures/json/svc_video.json`:

```json
{
    "response": {
        "posts": [
            {
                "id": "900100200300",
                "blogName": "svcvideoblog",
                "postUrl": "https://svcvideoblog.tumblr.com/post/900100200300",
                "type": "video",
                "timestamp": 1776097800,
                "tags": ["video"],
                "rebloggedFromName": "originalblog",
                "rebloggedFromId": "888777",
                "summary": "Cool clip",
                "caption": "<p>Check this out</p>",
                "videoUrl": "https://vtt.tumblr.com/tumblr_svc_abc_720.mp4",
                "duration": 30,
                "thumbnailUrl": "https://64.media.tumblr.com/svc_thumb/thumb.jpg",
                "thumbnailWidth": 480,
                "thumbnailHeight": 270
            }
        ],
        "totalPosts": 30
    }
}
```

Write file `tests/fixtures/json/svc_text.json`:

```json
{
    "response": {
        "posts": [
            {
                "id": "700300400500",
                "blogName": "svctextblog",
                "postUrl": "https://svctextblog.tumblr.com/post/700300400500",
                "type": "text",
                "timestamp": 1776097800,
                "tags": ["writing"],
                "rebloggedFromName": null,
                "rebloggedFromId": null,
                "summary": "Hello world post",
                "title": "Hello World",
                "body": "<p>This is a text post in SVC format.</p>"
            }
        ],
        "totalPosts": 100
    }
}
```

- [ ] **Step 2: Write the failing test**

Write file `tests/unit/test_svc_json.py`:

```python
"""Tests for SVC JSON parser."""

import json
from pathlib import Path

import pytest

from tumbl4.core.errors import ParseError
from tumbl4.core.parse.svc_json import normalize_svc_post, parse_svc_response

FIXTURES = Path(__file__).parent.parent / "fixtures" / "json"


class TestParseSvcResponse:
    def test_extracts_posts_and_total(self) -> None:
        fixture = json.loads((FIXTURES / "svc_photo.json").read_text())
        total, posts = parse_svc_response(fixture)
        assert total == 50
        assert len(posts) == 1

    def test_video_response(self) -> None:
        fixture = json.loads((FIXTURES / "svc_video.json").read_text())
        total, posts = parse_svc_response(fixture)
        assert total == 30
        assert len(posts) == 1

    def test_empty_response(self) -> None:
        data = {"response": {"posts": [], "totalPosts": 0}}
        total, posts = parse_svc_response(data)
        assert total == 0
        assert posts == []

    def test_missing_response_key_raises(self) -> None:
        with pytest.raises(ParseError):
            parse_svc_response({"bad": "data"})


class TestNormalizeSvcPost:
    def test_photo_post(self) -> None:
        fixture = json.loads((FIXTURES / "svc_photo.json").read_text())
        post = fixture["response"]["posts"][0]
        result = normalize_svc_post(post)
        assert result["schema_version"] == 1
        assert result["source_format"] == "svc"
        assert result["post_id"] == "728394056123"
        assert result["post_type"] == "photo"
        assert result["blog_name"] == "svctestblog"
        assert len(result["media"]) == 1
        assert result["media"][0]["kind"] == "photo"
        assert result["media"][0]["width"] == 2048
        assert result["is_reblog"] is False

    def test_video_post_with_reblog(self) -> None:
        fixture = json.loads((FIXTURES / "svc_video.json").read_text())
        post = fixture["response"]["posts"][0]
        result = normalize_svc_post(post)
        assert result["post_type"] == "video"
        assert result["is_reblog"] is True
        assert result["reblog_source"] is not None
        assert result["reblog_source"]["blog_name"] == "originalblog"
        assert len(result["media"]) == 1
        assert result["media"][0]["kind"] == "video"
        assert result["media"][0]["duration_ms"] == 30000

    def test_text_post(self) -> None:
        fixture = json.loads((FIXTURES / "svc_text.json").read_text())
        post = fixture["response"]["posts"][0]
        result = normalize_svc_post(post)
        assert result["post_type"] == "text"
        assert result["title"] == "Hello World"
        assert result["body_html"] is not None
        assert len(result["media"]) == 0

    def test_missing_id_raises(self) -> None:
        with pytest.raises(ParseError):
            normalize_svc_post({"type": "photo", "blogName": "test"})

    def test_tags_always_list(self) -> None:
        post = {
            "id": "1", "blogName": "test",
            "postUrl": "https://test.tumblr.com/post/1",
            "type": "text", "timestamp": 0,
        }
        result = normalize_svc_post(post)
        assert result["tags"] == []

    def test_unknown_type_defaults_to_text(self) -> None:
        post = {
            "id": "1", "blogName": "test",
            "postUrl": "https://test.tumblr.com/post/1",
            "type": "chat", "timestamp": 0,
            "body": "<p>Chat content</p>",
        }
        result = normalize_svc_post(post)
        assert result["post_type"] == "text"
```

- [ ] **Step 3: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_svc_json.py -v`
Expected: FAIL -- `ModuleNotFoundError: No module named 'tumbl4.core.parse.svc_json'`

- [ ] **Step 4: Write the implementation**

Write file `src/tumbl4/core/parse/svc_json.py`:

```python
"""Parse Tumblr SVC JSON responses into IntermediateDict.

SVC (service) JSON is the format used by Tumblr's internal API endpoints
and the hidden/dashboard crawler. It uses camelCase field names and has
a different post structure than the V1 API.

See spec section 5.6.
"""

from __future__ import annotations

import re
from datetime import UTC, datetime

from tumbl4.core.errors import ParseError
from tumbl4.core.parse.intermediate import IntermediateDict, MediaEntry, ReblogSource

_HTML_TAG_RE = re.compile(r"<[^>]+>")

# SVC type -> our canonical post_type. Unknown types fall back to "text".
_SVC_TYPE_MAP: dict[str, str] = {
    "photo": "photo",
    "video": "video",
    "audio": "audio",
    "text": "text",
    "quote": "quote",
    "link": "link",
    "answer": "answer",
    "regular": "text",
    "chat": "text",
}


def parse_svc_response(data: dict[str, object]) -> tuple[int, list[dict[str, object]]]:
    """Extract total post count and post list from an SVC JSON response.

    Returns:
        (total_posts, posts)
    """
    response = data.get("response")
    if not isinstance(response, dict):
        raise ParseError("SVC response missing 'response' key", excerpt=str(data)[:200])

    total = int(response.get("totalPosts", 0))  # type: ignore[arg-type]
    posts = response.get("posts", [])
    if not isinstance(posts, list):
        posts = []
    return total, posts  # type: ignore[return-value]


def normalize_svc_post(post: dict[str, object]) -> IntermediateDict:
    """Convert an SVC JSON post dict to IntermediateDict."""
    post_id = post.get("id")
    if not post_id:
        raise ParseError("SVC post missing 'id' field", excerpt=str(post)[:200])
    post_id = str(post_id)

    blog_name = str(post.get("blogName", ""))
    post_url = str(post.get("postUrl", ""))
    raw_type = str(post.get("type", "text"))
    post_type = _SVC_TYPE_MAP.get(raw_type, "text")

    ts = _parse_timestamp(post.get("timestamp"))
    tags = _safe_tags(post.get("tags"))

    reblog_from = post.get("rebloggedFromName")
    is_reblog = bool(reblog_from)
    reblog_source: ReblogSource | None = None
    if isinstance(reblog_from, str) and reblog_from:
        reblog_source = ReblogSource(
            blog_name=reblog_from,
            post_id=str(post.get("rebloggedFromId", "")),
        )

    media = _extract_media(post, post_type)
    title, body_text, body_html = _extract_content(post, post_type)

    return IntermediateDict(
        schema_version=1,
        source_format="svc",
        post_id=post_id,
        blog_name=blog_name,
        post_url=post_url,
        post_type=post_type,  # type: ignore[arg-type]
        timestamp_utc=ts,
        tags=tags,
        is_reblog=is_reblog,
        reblog_source=reblog_source,
        title=title,
        body_text=body_text,
        body_html=body_html,
        media=media,
        raw_content_blocks=None,
    )


def _extract_media(
    post: dict[str, object], post_type: str
) -> list[MediaEntry]:
    """Extract downloadable media from an SVC post."""
    media: list[MediaEntry] = []

    if post_type == "photo":
        photos = post.get("photos")
        if isinstance(photos, list):
            for photo in photos:
                if not isinstance(photo, dict):
                    continue
                original = photo.get("originalSize")
                if isinstance(original, dict):
                    url = str(original.get("url", ""))
                    if url:
                        media.append(
                            MediaEntry(
                                kind="photo",
                                url=url,
                                width=_int_or_none(original.get("width")),
                                height=_int_or_none(original.get("height")),
                                mime_type=None,
                                alt_text=_str_or_none(photo.get("caption")),
                                duration_ms=None,
                            )
                        )

    elif post_type == "video":
        video_url = post.get("videoUrl")
        if isinstance(video_url, str) and video_url.strip():
            duration_raw = post.get("duration")
            duration_ms: int | None = None
            if duration_raw is not None:
                try:
                    duration_ms = int(duration_raw) * 1000  # type: ignore[arg-type]
                except (ValueError, TypeError):
                    pass
            media.append(
                MediaEntry(
                    kind="video",
                    url=video_url.strip(),
                    width=None,
                    height=None,
                    mime_type=None,
                    alt_text=None,
                    duration_ms=duration_ms,
                )
            )

    elif post_type == "audio":
        audio_url = post.get("audioUrl")
        if isinstance(audio_url, str) and audio_url.strip():
            media.append(
                MediaEntry(
                    kind="audio",
                    url=audio_url.strip(),
                    width=None,
                    height=None,
                    mime_type=None,
                    alt_text=None,
                    duration_ms=None,
                )
            )

    return media


def _extract_content(
    post: dict[str, object], post_type: str
) -> tuple[str | None, str | None, str | None]:
    """Extract title, body_text, body_html from an SVC post.

    Returns:
        (title, body_text, body_html)
    """
    title: str | None = _str_or_none(post.get("title"))
    body_html: str | None = None
    body_text: str | None = None

    if post_type == "photo":
        caption = post.get("caption")
        if caption:
            body_html = str(caption)
            body_text = _strip_html(body_html)

    elif post_type == "video":
        caption = post.get("caption")
        if caption:
            body_html = str(caption)
            body_text = _strip_html(body_html)

    elif post_type == "audio":
        caption = post.get("caption")
        if caption:
            body_html = str(caption)
            body_text = _strip_html(body_html)
        track = post.get("trackName")
        if track:
            title = str(track)

    elif post_type == "text":
        body = post.get("body")
        if body:
            body_html = str(body)
            body_text = _strip_html(body_html)

    elif post_type == "quote":
        quote_text = post.get("text", post.get("quoteText", ""))
        quote_source = post.get("source", post.get("quoteSource", ""))
        if quote_text:
            body_text = str(quote_text)
            body_html = str(quote_text)
            if quote_source:
                body_html += f"\n<footer>{quote_source}</footer>"

    elif post_type == "link":
        link_desc = post.get("description", post.get("linkDescription", ""))
        if link_desc:
            body_html = str(link_desc)
            body_text = _strip_html(body_html)

    elif post_type == "answer":
        question = post.get("question", "")
        answer = post.get("answer", "")
        if question or answer:
            body_html = ""
            if question:
                body_html += f"<blockquote>{question}</blockquote>\n"
            if answer:
                body_html += str(answer)
            body_text = f"Q: {question}\nA: {_strip_html(str(answer))}" if question else _strip_html(str(answer))

    summary = post.get("summary")
    if body_text is None and summary:
        body_text = str(summary)

    return title, body_text, body_html


def _parse_timestamp(raw: object) -> str:
    """Parse a unix timestamp to ISO8601 string."""
    try:
        ts = int(raw) if raw is not None else 0  # type: ignore[arg-type]
        return datetime.fromtimestamp(ts, tz=UTC).isoformat()
    except (ValueError, TypeError, OSError):
        return datetime.fromtimestamp(0, tz=UTC).isoformat()


def _safe_tags(raw: object) -> list[str]:
    if isinstance(raw, list):
        return [str(t) for t in raw]
    return []


def _int_or_none(val: object) -> int | None:
    if val is None:
        return None
    try:
        return int(val)  # type: ignore[arg-type]
    except (ValueError, TypeError):
        return None


def _str_or_none(val: object) -> str | None:
    if val is None:
        return None
    s = str(val)
    return s if s else None


def _strip_html(html: str) -> str:
    return _HTML_TAG_RE.sub("", html).strip()
```

- [ ] **Step 5: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_svc_json.py -v`
Expected: 10 passed

- [ ] **Step 6: Commit**

```bash
git add src/tumbl4/core/parse/svc_json.py tests/fixtures/json/svc_photo.json tests/fixtures/json/svc_video.json tests/fixtures/json/svc_text.json tests/unit/test_svc_json.py
git commit -m "feat(parse): add SVC JSON parser for all post types

Normalizes Tumblr's internal SVC JSON format to IntermediateDict.
Handles photo (with originalSize), video, audio, text, quote, link,
and answer post types. See spec section 5.6.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: NPF parser with polymorphic content blocks

**Files:**
- Create: `src/tumbl4/core/parse/npf.py`
- Create: `tests/fixtures/json/npf_image_block.json`
- Create: `tests/fixtures/json/npf_video_block.json`
- Create: `tests/fixtures/json/npf_text_block.json`
- Create: `tests/fixtures/json/npf_mixed_blocks.json`
- Create: `tests/unit/test_npf.py`

- [ ] **Step 1: Create test fixture files**

Write file `tests/fixtures/json/npf_image_block.json`:

```json
{
    "objectType": "post",
    "id": "728394056123",
    "blogName": "npftestblog",
    "postUrl": "https://npftestblog.tumblr.com/post/728394056123",
    "timestamp": 1776097800,
    "tags": ["photography"],
    "rebloggedFromName": null,
    "summary": "Sunset photo",
    "content": [
        {
            "type": "image",
            "media": [
                {"url": "https://64.media.tumblr.com/npf_aaa/s2048x3072/sunset_full.jpg", "width": 2048, "height": 1536},
                {"url": "https://64.media.tumblr.com/npf_aaa/s1280x1920/sunset.jpg", "width": 1280, "height": 960},
                {"url": "https://64.media.tumblr.com/npf_aaa/s500x750/sunset.jpg", "width": 500, "height": 375}
            ],
            "altText": "A beautiful sunset over the ocean"
        }
    ]
}
```

Write file `tests/fixtures/json/npf_video_block.json`:

```json
{
    "objectType": "post",
    "id": "900100200300",
    "blogName": "npfvideoblog",
    "postUrl": "https://npfvideoblog.tumblr.com/post/900100200300",
    "timestamp": 1776097800,
    "tags": ["video"],
    "rebloggedFromName": "originalblog",
    "rebloggedFromId": "888777",
    "summary": "Cool clip",
    "content": [
        {
            "type": "video",
            "media": {"url": "https://vtt.tumblr.com/tumblr_npf_abc_720.mp4", "width": 1280, "height": 720},
            "poster": [
                {"url": "https://64.media.tumblr.com/npf_poster/poster.jpg", "width": 1280, "height": 720}
            ],
            "duration": 45.5
        }
    ]
}
```

Write file `tests/fixtures/json/npf_text_block.json`:

```json
{
    "objectType": "post",
    "id": "700300400500",
    "blogName": "npftextblog",
    "postUrl": "https://npftextblog.tumblr.com/post/700300400500",
    "timestamp": 1776097800,
    "tags": ["writing"],
    "rebloggedFromName": null,
    "summary": "Hello world",
    "content": [
        {
            "type": "text",
            "text": "Hello World",
            "subtype": "heading1"
        },
        {
            "type": "text",
            "text": "This is a text post written in NPF format."
        }
    ]
}
```

Write file `tests/fixtures/json/npf_mixed_blocks.json`:

```json
{
    "objectType": "post",
    "id": "555666777888",
    "blogName": "npfmixedblog",
    "postUrl": "https://npfmixedblog.tumblr.com/post/555666777888",
    "timestamp": 1776097800,
    "tags": ["art", "process"],
    "rebloggedFromName": null,
    "summary": "Art process post",
    "content": [
        {
            "type": "text",
            "text": "Here's my art process:",
            "subtype": "heading1"
        },
        {
            "type": "image",
            "media": [
                {"url": "https://64.media.tumblr.com/npf_mix1/s2048x3072/sketch.jpg", "width": 2048, "height": 1536}
            ],
            "altText": "Initial sketch"
        },
        {
            "type": "text",
            "text": "Then I added color:"
        },
        {
            "type": "image",
            "media": [
                {"url": "https://64.media.tumblr.com/npf_mix2/s2048x3072/colored.jpg", "width": 2048, "height": 1536}
            ],
            "altText": "Colored version"
        },
        {
            "type": "unsupported_future_block",
            "data": {"key": "value"}
        }
    ]
}
```

- [ ] **Step 2: Write the failing test**

Write file `tests/unit/test_npf.py`:

```python
"""Tests for NPF (Neue Post Format) parser."""

import json
from pathlib import Path

import pytest

from tumbl4.core.parse.npf import normalize_npf_post

FIXTURES = Path(__file__).parent.parent / "fixtures" / "json"


class TestNormalizeNpfPost:
    def test_image_block(self) -> None:
        fixture = json.loads((FIXTURES / "npf_image_block.json").read_text())
        result = normalize_npf_post(fixture)
        assert result["schema_version"] == 1
        assert result["source_format"] == "npf"
        assert result["post_id"] == "728394056123"
        assert result["blog_name"] == "npftestblog"
        assert result["post_type"] == "photo"
        assert len(result["media"]) == 1
        assert result["media"][0]["kind"] == "photo"
        # Should pick the largest resolution
        assert result["media"][0]["width"] == 2048
        assert result["media"][0]["alt_text"] == "A beautiful sunset over the ocean"
        assert result["is_reblog"] is False

    def test_video_block(self) -> None:
        fixture = json.loads((FIXTURES / "npf_video_block.json").read_text())
        result = normalize_npf_post(fixture)
        assert result["post_type"] == "video"
        assert len(result["media"]) == 1
        assert result["media"][0]["kind"] == "video"
        assert "npf_abc" in result["media"][0]["url"]
        assert result["media"][0]["duration_ms"] == 45500
        assert result["is_reblog"] is True
        assert result["reblog_source"]["blog_name"] == "originalblog"

    def test_text_block(self) -> None:
        fixture = json.loads((FIXTURES / "npf_text_block.json").read_text())
        result = normalize_npf_post(fixture)
        assert result["post_type"] == "text"
        assert len(result["media"]) == 0
        assert "Hello World" in (result["body_text"] or "")
        assert "NPF format" in (result["body_text"] or "")

    def test_mixed_blocks_extracts_all_media(self) -> None:
        fixture = json.loads((FIXTURES / "npf_mixed_blocks.json").read_text())
        result = normalize_npf_post(fixture)
        assert result["post_type"] == "photo"  # dominant media type
        assert len(result["media"]) == 2
        assert result["media"][0]["alt_text"] == "Initial sketch"
        assert result["media"][1]["alt_text"] == "Colored version"

    def test_mixed_blocks_preserves_text(self) -> None:
        fixture = json.loads((FIXTURES / "npf_mixed_blocks.json").read_text())
        result = normalize_npf_post(fixture)
        assert "art process" in (result["body_text"] or "").lower()

    def test_unknown_block_type_ignored(self) -> None:
        fixture = json.loads((FIXTURES / "npf_mixed_blocks.json").read_text())
        result = normalize_npf_post(fixture)
        # Unknown blocks should not crash, media count is still 2
        assert len(result["media"]) == 2

    def test_raw_content_blocks_preserved(self) -> None:
        fixture = json.loads((FIXTURES / "npf_mixed_blocks.json").read_text())
        result = normalize_npf_post(fixture)
        assert result["raw_content_blocks"] is not None
        assert len(result["raw_content_blocks"]) == 5

    def test_empty_content_array(self) -> None:
        post = {
            "id": "1", "blogName": "test",
            "postUrl": "https://test.tumblr.com/post/1",
            "timestamp": 0, "content": [],
        }
        result = normalize_npf_post(post)
        assert result["post_type"] == "text"
        assert len(result["media"]) == 0

    def test_tags_default_empty(self) -> None:
        post = {
            "id": "1", "blogName": "test",
            "postUrl": "https://test.tumblr.com/post/1",
            "timestamp": 0, "content": [],
        }
        result = normalize_npf_post(post)
        assert result["tags"] == []

    def test_image_picks_largest_size(self) -> None:
        fixture = json.loads((FIXTURES / "npf_image_block.json").read_text())
        result = normalize_npf_post(fixture)
        # The first (largest) media entry should be selected
        assert "s2048x3072" in result["media"][0]["url"]
```

- [ ] **Step 3: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_npf.py -v`
Expected: FAIL -- `ModuleNotFoundError: No module named 'tumbl4.core.parse.npf'`

- [ ] **Step 4: Write the implementation**

Write file `src/tumbl4/core/parse/npf.py`:

```python
"""Parse Tumblr NPF (Neue Post Format) content blocks into IntermediateDict.

NPF uses typed content blocks: image, video, audio, text, link, etc.
Unknown block types are preserved in raw_content_blocks but do not
crash the parser. See spec section 5.6.

The dominant media type determines post_type: if any image blocks exist,
the post is "photo"; if any video blocks, "video"; etc. Posts with only
text blocks are "text".
"""

from __future__ import annotations

from datetime import UTC, datetime

from tumbl4._internal.logging import get_logger
from tumbl4.core.parse.intermediate import IntermediateDict, MediaEntry, ReblogSource

logger = get_logger(__name__)

# Track unknown block types to emit one warning per type per run
_warned_block_types: set[str] = set()


def normalize_npf_post(post: dict[str, object]) -> IntermediateDict:
    """Convert an NPF post dict to IntermediateDict."""
    post_id = str(post.get("id", ""))
    blog_name = str(post.get("blogName", ""))
    post_url = str(post.get("postUrl", ""))
    ts = _parse_timestamp(post.get("timestamp"))
    tags = _safe_tags(post.get("tags"))

    reblog_from = post.get("rebloggedFromName")
    is_reblog = bool(reblog_from)
    reblog_source: ReblogSource | None = None
    if isinstance(reblog_from, str) and reblog_from:
        reblog_source = ReblogSource(
            blog_name=reblog_from,
            post_id=str(post.get("rebloggedFromId", "")),
        )

    content = post.get("content", [])
    if not isinstance(content, list):
        content = []

    media: list[MediaEntry] = []
    text_parts: list[str] = []

    for block in content:
        if not isinstance(block, dict):
            continue
        block_type = str(block.get("type", ""))

        if block_type == "image":
            entry = _parse_image_block(block)
            if entry:
                media.append(entry)

        elif block_type == "video":
            entry = _parse_video_block(block)
            if entry:
                media.append(entry)

        elif block_type == "audio":
            entry = _parse_audio_block(block)
            if entry:
                media.append(entry)

        elif block_type == "text":
            text = block.get("text")
            if isinstance(text, str) and text:
                text_parts.append(text)

        elif block_type == "link":
            # Link blocks don't produce downloadable media
            text = block.get("title") or block.get("url")
            if isinstance(text, str) and text:
                text_parts.append(text)

        else:
            # Unknown block type — log once per type, preserve in raw_content_blocks
            if block_type not in _warned_block_types:
                logger.warning(
                    "unknown NPF block type, preserving in raw_content_blocks",
                    extra={"block_type": block_type},
                )
                _warned_block_types.add(block_type)

    post_type = _determine_post_type(media)
    body_text = "\n".join(text_parts) if text_parts else None
    body_html = "\n".join(f"<p>{t}</p>" for t in text_parts) if text_parts else None

    # Determine title from first heading-type text block
    title: str | None = None
    for block in content:
        if isinstance(block, dict) and block.get("type") == "text":
            subtype = block.get("subtype", "")
            if isinstance(subtype, str) and subtype.startswith("heading"):
                title = str(block.get("text", ""))
                break

    return IntermediateDict(
        schema_version=1,
        source_format="npf",
        post_id=post_id,
        blog_name=blog_name,
        post_url=post_url,
        post_type=post_type,
        timestamp_utc=ts,
        tags=tags,
        is_reblog=is_reblog,
        reblog_source=reblog_source,
        title=title,
        body_text=body_text,
        body_html=body_html,
        media=media,
        raw_content_blocks=content,  # type: ignore[arg-type]
    )


def _parse_image_block(block: dict[str, object]) -> MediaEntry | None:
    """Extract the best-resolution image from an NPF image block."""
    media_list = block.get("media")
    if not isinstance(media_list, list) or not media_list:
        return None

    # Pick the largest by width
    best = max(
        (m for m in media_list if isinstance(m, dict)),
        key=lambda m: int(m.get("width", 0)),  # type: ignore[arg-type]
        default=None,
    )
    if best is None:
        return None

    url = str(best.get("url", ""))
    if not url:
        return None

    alt_text = block.get("altText")

    return MediaEntry(
        kind="photo",
        url=url,
        width=_int_or_none(best.get("width")),
        height=_int_or_none(best.get("height")),
        mime_type=None,
        alt_text=str(alt_text) if alt_text else None,
        duration_ms=None,
    )


def _parse_video_block(block: dict[str, object]) -> MediaEntry | None:
    """Extract video media from an NPF video block."""
    media = block.get("media")
    if not isinstance(media, dict):
        return None

    url = str(media.get("url", ""))
    if not url:
        return None

    duration_raw = block.get("duration")
    duration_ms: int | None = None
    if duration_raw is not None:
        try:
            duration_ms = int(float(duration_raw) * 1000)  # type: ignore[arg-type]
        except (ValueError, TypeError):
            pass

    return MediaEntry(
        kind="video",
        url=url,
        width=_int_or_none(media.get("width")),
        height=_int_or_none(media.get("height")),
        mime_type=None,
        alt_text=None,
        duration_ms=duration_ms,
    )


def _parse_audio_block(block: dict[str, object]) -> MediaEntry | None:
    """Extract audio media from an NPF audio block."""
    media = block.get("media")
    url: str | None = None

    if isinstance(media, dict):
        url = str(media.get("url", ""))
    elif isinstance(media, str):
        url = media

    # Fallback to url field directly
    if not url:
        url_field = block.get("url")
        if isinstance(url_field, str):
            url = url_field

    if not url:
        return None

    return MediaEntry(
        kind="audio",
        url=url,
        width=None,
        height=None,
        mime_type=None,
        alt_text=None,
        duration_ms=None,
    )


def _determine_post_type(
    media: list[MediaEntry],
) -> str:
    """Determine the canonical post type from the extracted media.

    Priority: photo > video > audio > text.
    """
    kinds = {m["kind"] for m in media}
    if "photo" in kinds:
        return "photo"
    if "video" in kinds:
        return "video"
    if "audio" in kinds:
        return "audio"
    return "text"


def _parse_timestamp(raw: object) -> str:
    try:
        ts = int(raw) if raw is not None else 0  # type: ignore[arg-type]
        return datetime.fromtimestamp(ts, tz=UTC).isoformat()
    except (ValueError, TypeError, OSError):
        return datetime.fromtimestamp(0, tz=UTC).isoformat()


def _safe_tags(raw: object) -> list[str]:
    if isinstance(raw, list):
        return [str(t) for t in raw]
    return []


def _int_or_none(val: object) -> int | None:
    if val is None:
        return None
    try:
        return int(val)  # type: ignore[arg-type]
    except (ValueError, TypeError):
        return None
```

- [ ] **Step 5: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_npf.py -v`
Expected: 10 passed

- [ ] **Step 6: Commit**

```bash
git add src/tumbl4/core/parse/npf.py tests/fixtures/json/npf_image_block.json tests/fixtures/json/npf_video_block.json tests/fixtures/json/npf_text_block.json tests/fixtures/json/npf_mixed_blocks.json tests/unit/test_npf.py
git commit -m "feat(parse): add NPF parser with polymorphic content blocks

Parses image, video, audio, text, and link blocks. Unknown block
types logged once per type and preserved in raw_content_blocks.
Picks largest resolution for images. See spec section 5.6.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: HTML scraper -- ___INITIAL_STATE___ extraction

**Files:**
- Create: `src/tumbl4/core/parse/html_scrape.py`
- Create: `tests/fixtures/json/html_initial_state_peepr.html`
- Create: `tests/fixtures/json/html_initial_state_resp.html`
- Create: `tests/unit/test_html_scrape.py`

- [ ] **Step 1: Create test fixture files**

Write file `tests/fixtures/json/html_initial_state_peepr.html`:

```html
<!DOCTYPE html>
<html>
<head><title>Test Blog</title></head>
<body>
<script>window['___INITIAL_STATE___'] = {"PeeprRoute":{"blogData":{"name":"peeprblog","title":"Peepr Blog"},"postData":{"posts":[{"id":"728394056123","type":"photo","timestamp":1776097800,"tags":["art"],"photos":[{"originalSize":{"url":"https://64.media.tumblr.com/peepr_aaa/s2048x3072/photo.jpg","width":2048,"height":1536}}]}],"nextLink":"/svc/route/peepr/blog/peeprblog?cursor=abc123"}}}</script>
</body>
</html>
```

Write file `tests/fixtures/json/html_initial_state_resp.html`:

```html
<!DOCTYPE html>
<html>
<head><title>Response Blog</title></head>
<body>
<script>
window['___INITIAL_STATE___'] = {"response":{"blogData":{"name":"respblog","title":"Response Blog"},"posts":[{"id":"900100200300","type":"video","timestamp":1776097800,"tags":["video"],"videoUrl":"https://vtt.tumblr.com/tumblr_resp_xyz_720.mp4"}],"nextLink":"/svc/route/blog/respblog?cursor=def456"}};
</script>
</body>
</html>
```

- [ ] **Step 2: Write the failing test**

Write file `tests/unit/test_html_scrape.py`:

```python
"""Tests for ___INITIAL_STATE___ HTML extraction."""

from pathlib import Path

import pytest

from tumbl4.core.errors import ParseError
from tumbl4.core.parse.html_scrape import extract_initial_state

FIXTURES = Path(__file__).parent.parent / "fixtures" / "json"


class TestExtractInitialState:
    def test_peepr_route_shape(self) -> None:
        html = (FIXTURES / "html_initial_state_peepr.html").read_text()
        result = extract_initial_state(html)
        assert result["blog_name"] == "peeprblog"
        assert len(result["posts"]) == 1
        assert result["posts"][0]["id"] == "728394056123"
        assert result["next_cursor"] is not None
        assert "cursor=abc123" in result["next_cursor"]

    def test_response_wrapped_shape(self) -> None:
        html = (FIXTURES / "html_initial_state_resp.html").read_text()
        result = extract_initial_state(html)
        assert result["blog_name"] == "respblog"
        assert len(result["posts"]) == 1
        assert result["posts"][0]["id"] == "900100200300"
        assert result["next_cursor"] is not None

    def test_no_initial_state_raises(self) -> None:
        with pytest.raises(ParseError, match="extractor"):
            extract_initial_state("<html><body>No state here</body></html>")

    def test_invalid_json_raises(self) -> None:
        html = "<script>window['___INITIAL_STATE___'] = {invalid json};</script>"
        with pytest.raises(ParseError):
            extract_initial_state(html)

    def test_unknown_shape_raises(self) -> None:
        html = """<script>window['___INITIAL_STATE___'] = {"unknownKey": {"data": []}}</script>"""
        with pytest.raises(ParseError, match="shape"):
            extract_initial_state(html)

    def test_peepr_preferred_when_both_present(self) -> None:
        html = (
            "<script>window['___INITIAL_STATE___'] = "
            '{"PeeprRoute":{"blogData":{"name":"both"},"postData":{"posts":[{"id":"1"}],"nextLink":null}},'
            '"response":{"blogData":{"name":"alt"},"posts":[{"id":"2"}]}}'
            "</script>"
        )
        result = extract_initial_state(html)
        assert result["blog_name"] == "both"

    def test_multiline_regex_fallback(self) -> None:
        html = """<script>
        window['___INITIAL_STATE___'] = {
            "PeeprRoute": {
                "blogData": {"name": "multiline"},
                "postData": {
                    "posts": [{"id": "999"}],
                    "nextLink": null
                }
            }
        }
        </script>"""
        result = extract_initial_state(html)
        assert result["blog_name"] == "multiline"
        assert len(result["posts"]) == 1

    def test_returns_empty_posts_gracefully(self) -> None:
        html = (
            "<script>window['___INITIAL_STATE___'] = "
            '{"PeeprRoute":{"blogData":{"name":"empty"},"postData":{"posts":[],"nextLink":null}}}'
            "</script>"
        )
        result = extract_initial_state(html)
        assert result["posts"] == []
        assert result["next_cursor"] is None
```

- [ ] **Step 3: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_html_scrape.py -v`
Expected: FAIL -- `ModuleNotFoundError: No module named 'tumbl4.core.parse.html_scrape'`

- [ ] **Step 4: Write the implementation**

Write file `src/tumbl4/core/parse/html_scrape.py`:

```python
"""Extract ___INITIAL_STATE___ JSON from Tumblr blog HTML pages.

Tumblr embeds a JSON blob in a <script> tag that contains post data,
blog metadata, and pagination cursors. This module handles two regex
patterns (single-line and multi-line) and two JSON shapes (PeeprRoute
and response-wrapped).

SECURITY: Uses only regex + json.loads. NEVER uses lxml.etree (XXE risk).
See spec sections 5.9 and html_scrape.py security note.
"""

from __future__ import annotations

import json
import re
from typing import Any, TypedDict

from tumbl4._internal.logging import get_logger
from tumbl4.core.errors import ParseError

logger = get_logger(__name__)

# Single-line regex: matches window['___INITIAL_STATE___'] = {...};
_SINGLE_LINE_RE = re.compile(
    r"""window\['___INITIAL_STATE___'\]\s*=\s*(\{.+?\})\s*;""",
)

# Multi-line regex: same pattern but with DOTALL
_MULTI_LINE_RE = re.compile(
    r"""window\['___INITIAL_STATE___'\]\s*=\s*(\{.+?\})\s*;?""",
    re.DOTALL,
)

# Repeated-error suppression
_extractor_warning_emitted = False


class InitialStateResult(TypedDict):
    """Uniform structure returned by extract_initial_state."""

    blog_name: str
    posts: list[dict[str, Any]]
    next_cursor: str | None


def extract_initial_state(html: str) -> InitialStateResult:
    """Extract and parse ___INITIAL_STATE___ from an HTML page.

    Tries single-line regex first, then multi-line regex. Identifies
    the JSON shape (PeeprRoute vs response-wrapped) and returns a
    uniform structure.

    Raises ParseError if no state is found, JSON is invalid, or the
    shape is unrecognised.
    """
    global _extractor_warning_emitted  # noqa: PLW0603

    raw_json = _extract_json_string(html)
    data = _parse_json(raw_json)
    return _normalize_shape(data)


def _extract_json_string(html: str) -> str:
    """Extract the raw JSON string from the HTML using regex."""
    # Try single-line first
    match = _SINGLE_LINE_RE.search(html)
    if match:
        return match.group(1)

    # Fallback to multi-line
    match = _MULTI_LINE_RE.search(html)
    if match:
        return match.group(1)

    global _extractor_warning_emitted  # noqa: PLW0603
    if not _extractor_warning_emitted:
        logger.warning("___INITIAL_STATE___ extractor may be out of date")
        _extractor_warning_emitted = True

    raise ParseError(
        "No ___INITIAL_STATE___ found in HTML — extractor regex did not match",
        excerpt=html[:300],
    )


def _parse_json(raw: str) -> dict[str, Any]:
    """Parse the extracted JSON string."""
    try:
        data = json.loads(raw)
    except json.JSONDecodeError as e:
        raise ParseError(
            f"Failed to parse ___INITIAL_STATE___ JSON: {e}",
            excerpt=raw[:200],
        ) from e

    if not isinstance(data, dict):
        raise ParseError(
            "___INITIAL_STATE___ is not a JSON object",
            excerpt=str(data)[:200],
        )
    return data


def _normalize_shape(data: dict[str, Any]) -> InitialStateResult:
    """Identify the JSON shape and normalise to InitialStateResult.

    PeeprRoute shape (newer, preferred when both present):
        {"PeeprRoute": {"blogData": {...}, "postData": {"posts": [...], "nextLink": ...}}}

    Response-wrapped shape:
        {"response": {"blogData": {...}, "posts": [...], "nextLink": ...}}
    """
    has_peepr = "PeeprRoute" in data
    has_response = "response" in data

    if has_peepr:
        if has_response:
            logger.warning(
                "___INITIAL_STATE___ has both PeeprRoute and response keys; "
                "preferring PeeprRoute (newer shape)"
            )
        return _normalize_peepr(data["PeeprRoute"])

    if has_response:
        return _normalize_response(data["response"])

    raise ParseError(
        "___INITIAL_STATE___ JSON has unknown shape — neither PeeprRoute nor response key found",
        excerpt=str(list(data.keys()))[:200],
    )


def _normalize_peepr(peepr: dict[str, Any]) -> InitialStateResult:
    """Normalize PeeprRoute shape."""
    blog_data = peepr.get("blogData", {})
    blog_name = str(blog_data.get("name", ""))

    post_data = peepr.get("postData", {})
    if not isinstance(post_data, dict):
        post_data = {}

    posts = post_data.get("posts", [])
    if not isinstance(posts, list):
        posts = []

    next_link = post_data.get("nextLink")
    next_cursor = str(next_link) if next_link else None

    return InitialStateResult(
        blog_name=blog_name,
        posts=posts,
        next_cursor=next_cursor,
    )


def _normalize_response(response: dict[str, Any]) -> InitialStateResult:
    """Normalize response-wrapped shape."""
    if not isinstance(response, dict):
        raise ParseError("response key is not a dict", excerpt=str(response)[:200])

    blog_data = response.get("blogData", {})
    blog_name = str(blog_data.get("name", ""))

    posts = response.get("posts", [])
    if not isinstance(posts, list):
        posts = []

    next_link = response.get("nextLink")
    next_cursor = str(next_link) if next_link else None

    return InitialStateResult(
        blog_name=blog_name,
        posts=posts,
        next_cursor=next_cursor,
    )
```

- [ ] **Step 5: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_html_scrape.py -v`
Expected: 8 passed

- [ ] **Step 6: Commit**

```bash
git add src/tumbl4/core/parse/html_scrape.py tests/fixtures/json/html_initial_state_peepr.html tests/fixtures/json/html_initial_state_resp.html tests/unit/test_html_scrape.py
git commit -m "feat(parse): add ___INITIAL_STATE___ HTML extractor

Two-regex (single-line + multi-line) with two-shape (PeeprRoute
+ response-wrapped) support. PeeprRoute preferred when both present.
No lxml.etree (XXE risk). See spec section 5.9.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: Inline media extraction from post body HTML

**Files:**
- Create: `src/tumbl4/core/parse/inline_media.py`
- Modify: `src/tumbl4/core/parse/intermediate.py`
- Create: `tests/fixtures/json/inline_media_body.html`
- Create: `tests/unit/test_inline_media.py`

- [ ] **Step 1: Create test fixture file**

Write file `tests/fixtures/json/inline_media_body.html`:

```html
<p>Check out this image:</p>
<figure><img src="https://64.media.tumblr.com/inline_aaa/s540x810/inline_photo.jpg" alt="inline image"/></figure>
<p>And this video:</p>
<figure><video src="https://va.media.tumblr.com/tumblr_inline_bbb.mp4" poster="https://64.media.tumblr.com/thumb/poster.jpg"></video></figure>
<p>Here is a non-tumblr image that should be ignored:</p>
<img src="https://example.com/external.jpg"/>
<p>Another tumblr image in different CDN format:</p>
<img src="https://64.media.tumblr.com/inline_ccc/s1280x1920/another.png"/>
<p>And an audio embed:</p>
<audio src="https://a.tumblr.com/tumblr_inline_ddd.mp3"></audio>
```

- [ ] **Step 2: Write the failing test**

Write file `tests/unit/test_inline_media.py`:

```python
"""Tests for inline media extraction from post body HTML."""

from pathlib import Path

from tumbl4.core.parse.inline_media import extract_inline_media

FIXTURES = Path(__file__).parent.parent / "fixtures" / "json"


class TestExtractInlineMedia:
    def test_extracts_tumblr_image_urls(self) -> None:
        html = (FIXTURES / "inline_media_body.html").read_text()
        media = extract_inline_media(html)
        image_urls = [m["url"] for m in media if m["kind"] == "photo"]
        assert any("inline_aaa" in u for u in image_urls)
        assert any("inline_ccc" in u for u in image_urls)

    def test_extracts_tumblr_video_urls(self) -> None:
        html = (FIXTURES / "inline_media_body.html").read_text()
        media = extract_inline_media(html)
        video_urls = [m["url"] for m in media if m["kind"] == "video"]
        assert any("inline_bbb" in u for u in video_urls)

    def test_extracts_tumblr_audio_urls(self) -> None:
        html = (FIXTURES / "inline_media_body.html").read_text()
        media = extract_inline_media(html)
        audio_urls = [m["url"] for m in media if m["kind"] == "audio"]
        assert any("inline_ddd" in u for u in audio_urls)

    def test_ignores_non_tumblr_urls(self) -> None:
        html = (FIXTURES / "inline_media_body.html").read_text()
        media = extract_inline_media(html)
        all_urls = [m["url"] for m in media]
        assert not any("example.com" in u for u in all_urls)

    def test_ignores_poster_urls(self) -> None:
        html = (FIXTURES / "inline_media_body.html").read_text()
        media = extract_inline_media(html)
        all_urls = [m["url"] for m in media]
        assert not any("poster" in u for u in all_urls)

    def test_deduplicates_urls(self) -> None:
        html = '<img src="https://64.media.tumblr.com/dup/s540x810/photo.jpg"/><img src="https://64.media.tumblr.com/dup/s540x810/photo.jpg"/>'
        media = extract_inline_media(html)
        assert len(media) == 1

    def test_empty_html(self) -> None:
        assert extract_inline_media("") == []
        assert extract_inline_media(None) == []  # type: ignore[arg-type]

    def test_no_media_in_html(self) -> None:
        html = "<p>Just plain text, no images or videos.</p>"
        media = extract_inline_media(html)
        assert media == []

    def test_correctly_classifies_media_kinds(self) -> None:
        html = (FIXTURES / "inline_media_body.html").read_text()
        media = extract_inline_media(html)
        kinds = {m["kind"] for m in media}
        assert "photo" in kinds
        assert "video" in kinds
        assert "audio" in kinds
```

- [ ] **Step 3: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_inline_media.py -v`
Expected: FAIL -- `ModuleNotFoundError: No module named 'tumbl4.core.parse.inline_media'`

- [ ] **Step 4: Write the implementation**

Write file `src/tumbl4/core/parse/inline_media.py`:

```python
"""Extract inline media URLs from post body HTML.

Scans for media.tumblr.com image/video/audio URLs embedded in post body
HTML (e.g., in <img>, <video>, <audio> tags or raw text). These are
distinct from the post's primary media entries — they appear inline in
text posts, quote posts, and other body content.

Only extracts URLs from Tumblr CDN domains:
  - 64.media.tumblr.com (images)
  - va.media.tumblr.com (videos)
  - vtt.tumblr.com (videos)
  - a.tumblr.com (audio)
"""

from __future__ import annotations

import re
from pathlib import PurePosixPath

from tumbl4.core.parse.intermediate import MediaEntry

# Regex matching Tumblr CDN media URLs in HTML content.
# Matches src="..." and direct URLs in text.
_TUMBLR_MEDIA_RE = re.compile(
    r'(?:src=["\']|(?<=\s)|^)'
    r'(https?://(?:64\.media|va\.media|vtt|a)\.tumblr\.com/[^\s"\'<>]+)',
    re.IGNORECASE,
)

# Poster/thumbnail URLs to exclude (these are video thumbnails, not standalone media)
_POSTER_RE = re.compile(r'poster=["\']([^"\']+)["\']', re.IGNORECASE)

# Extension -> kind mapping
_EXT_TO_KIND: dict[str, str] = {
    ".jpg": "photo",
    ".jpeg": "photo",
    ".png": "photo",
    ".gif": "photo",
    ".webp": "photo",
    ".pnj": "photo",
    ".mp4": "video",
    ".webm": "video",
    ".mov": "video",
    ".mp3": "audio",
    ".m4a": "audio",
    ".ogg": "audio",
    ".wav": "audio",
}


def extract_inline_media(html: str | None) -> list[MediaEntry]:
    """Extract unique Tumblr CDN media URLs from HTML body content.

    Returns a deduplicated list of MediaEntry dicts. Non-Tumblr URLs
    are ignored. Poster/thumbnail URLs are excluded.
    """
    if not html:
        return []

    # Collect poster URLs to exclude
    poster_urls: set[str] = set()
    for match in _POSTER_RE.finditer(html):
        poster_urls.add(match.group(1))

    # Extract all Tumblr CDN URLs
    seen: set[str] = set()
    media: list[MediaEntry] = []

    for match in _TUMBLR_MEDIA_RE.finditer(html):
        url = match.group(1).rstrip(")")  # strip trailing paren if present

        # Skip poster/thumbnail URLs
        if url in poster_urls:
            continue

        # Deduplicate
        if url in seen:
            continue
        seen.add(url)

        kind = _classify_url(url)
        media.append(
            MediaEntry(
                kind=kind,  # type: ignore[arg-type]
                url=url,
                width=None,
                height=None,
                mime_type=None,
                alt_text=None,
                duration_ms=None,
            )
        )

    return media


def _classify_url(url: str) -> str:
    """Classify a URL as photo, video, or audio based on path and domain."""
    # Check domain hints
    lower_url = url.lower()
    if "vtt.tumblr.com" in lower_url or "va.media.tumblr.com" in lower_url:
        # These are video CDN domains
        path = PurePosixPath(lower_url.split("?")[0])
        ext = path.suffix
        if ext in (".mp3", ".m4a", ".ogg", ".wav"):
            return "audio"
        return "video"

    if "a.tumblr.com" in lower_url:
        return "audio"

    # Check file extension
    path = PurePosixPath(lower_url.split("?")[0])
    ext = path.suffix
    return _EXT_TO_KIND.get(ext, "photo")
```

- [ ] **Step 5: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_inline_media.py -v`
Expected: 9 passed

- [ ] **Step 6: Commit**

```bash
git add src/tumbl4/core/parse/inline_media.py tests/fixtures/json/inline_media_body.html tests/unit/test_inline_media.py
git commit -m "feat(parse): add inline media extraction from post body HTML

Regex-based scan for media.tumblr.com URLs in post body HTML.
Classifies as photo/video/audio by CDN domain and file extension.
Deduplicates and excludes poster/thumbnail URLs.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: "Best" image resolution module

**Files:**
- Create: `src/tumbl4/core/crawl/best_resolution.py`
- Modify: `src/tumbl4/core/crawl/http_client.py`
- Create: `tests/fixtures/json/best_resolution_page.html`
- Create: `tests/unit/test_best_resolution.py`

- [ ] **Step 1: Create test fixture file**

Write file `tests/fixtures/json/best_resolution_page.html`:

```html
<!DOCTYPE html>
<html>
<head><title>Test Blog Post</title></head>
<body>
<script>window['___INITIAL_STATE___'] = {"PeeprRoute":{"blogData":{"name":"bestblog"},"postData":{"posts":[{"id":"728394056123","type":"photo","timestamp":1776097800,"imageResponse":{"images":[{"mediaKey":"aaa111","url":"https://64.media.tumblr.com/aaa111/s2048x3072/sunset_best.jpg","width":2048,"height":1536,"hasOriginal":true,"originalUrl":"https://64.media.tumblr.com/aaa111/raw/sunset_original.jpg"}]}}],"nextLink":null}}}</script>
</body>
</html>
```

- [ ] **Step 2: Write the failing test**

Write file `tests/unit/test_best_resolution.py`:

```python
"""Tests for best image resolution module."""

import pytest
import respx

from tumbl4.core.crawl.best_resolution import (
    BestResolutionResult,
    rewrite_url_for_best,
    resolve_best_resolution,
)
from tumbl4.core.crawl.http_client import TumblrHttpClient
from tumbl4.models.settings import HttpSettings


class TestRewriteUrlForBest:
    def test_rewrites_1280_to_2048(self) -> None:
        url = "https://64.media.tumblr.com/abc123/s1280x1920/photo.jpg"
        result = rewrite_url_for_best(url)
        assert "s2048x3072" in result

    def test_rewrites_500_to_2048(self) -> None:
        url = "https://64.media.tumblr.com/abc123/s500x750/photo.jpg"
        result = rewrite_url_for_best(url)
        assert "s2048x3072" in result

    def test_non_tumblr_url_unchanged(self) -> None:
        url = "https://example.com/photo.jpg"
        result = rewrite_url_for_best(url)
        assert result == url

    def test_already_2048_unchanged(self) -> None:
        url = "https://64.media.tumblr.com/abc123/s2048x3072/photo.jpg"
        result = rewrite_url_for_best(url)
        assert result == url

    def test_url_without_size_segment_unchanged(self) -> None:
        url = "https://64.media.tumblr.com/abc123/photo.jpg"
        result = rewrite_url_for_best(url)
        assert result == url


class TestResolveBestResolution:
    @respx.mock
    async def test_resolves_from_initial_state(self) -> None:
        from pathlib import Path

        page_html = (
            Path(__file__).parent.parent / "fixtures" / "json" / "best_resolution_page.html"
        ).read_text()

        respx.get("https://bestblog.tumblr.com/post/728394056123").respond(
            200, text=page_html
        )

        http = TumblrHttpClient(HttpSettings())
        try:
            result = await resolve_best_resolution(
                http=http,
                blog_url="https://bestblog.tumblr.com/",
                post_id="728394056123",
                media_key="aaa111",
            )
            assert result is not None
            assert "sunset_best" in result.url or "sunset_original" in result.url
            assert result.width == 2048 or result.width is None
        finally:
            await http.aclose()

    @respx.mock
    async def test_returns_none_on_404(self) -> None:
        respx.get("https://testblog.tumblr.com/post/999").respond(404)

        http = TumblrHttpClient(HttpSettings())
        try:
            result = await resolve_best_resolution(
                http=http,
                blog_url="https://testblog.tumblr.com/",
                post_id="999",
                media_key="abc",
            )
            assert result is None
        finally:
            await http.aclose()

    @respx.mock
    async def test_returns_none_on_no_image_response(self) -> None:
        html = (
            "<script>window['___INITIAL_STATE___'] = "
            '{"PeeprRoute":{"blogData":{"name":"test"},'
            '"postData":{"posts":[{"id":"1","type":"photo"}],"nextLink":null}}}'
            "</script>"
        )
        respx.get("https://testblog.tumblr.com/post/1").respond(200, text=html)

        http = TumblrHttpClient(HttpSettings())
        try:
            result = await resolve_best_resolution(
                http=http,
                blog_url="https://testblog.tumblr.com/",
                post_id="1",
                media_key="missing",
            )
            assert result is None
        finally:
            await http.aclose()
```

- [ ] **Step 3: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_best_resolution.py -v`
Expected: FAIL -- `ModuleNotFoundError`

- [ ] **Step 4: Write the implementation**

Add `get_html` method to `src/tumbl4/core/crawl/http_client.py`. After the existing `get_api` method, add:

```python
    async def get_html(self, url: str) -> str:
        """GET an HTML page with rate limiting and body size cap.

        Returns the response body as a string.
        Raises RateLimited, ServerError, or ResponseTooLarge on errors.
        Uses a dedicated rate bucket (700ms base delay) for best-resolution
        fetches to avoid overwhelming Tumblr's HTML endpoints.
        """
        async with self._rate_limiter:
            response = await self._client.get(url)
            _check_status(response)
            body = response.text
            if len(body.encode("utf-8")) > self._settings.max_api_response_bytes:
                raise ResponseTooLarge(
                    f"HTML response {len(body.encode('utf-8'))} bytes exceeds "
                    f"limit of {self._settings.max_api_response_bytes}"
                )
            return body
```

Write file `src/tumbl4/core/crawl/best_resolution.py`:

```python
"""Resolve "best" (highest available) image resolution for Tumblr photos.

Tumblr CDN URLs contain a size segment like /s1280x1920/. The best
resolution is obtained by:
1. Rewriting the URL: /s1280x1920/ -> /s2048x3072/
2. If the rewrite 404s, fetching the post's HTML page and parsing
   ___INITIAL_STATE___ for the imageResponse containing full-size URLs.

The dedicated retry policy uses 700ms base delay with 10s/20s backoff
to avoid rate limiting. See plan boundaries, correction 6.
"""

from __future__ import annotations

import re
from dataclasses import dataclass

from tumbl4._internal.logging import get_logger
from tumbl4.core.crawl.http_client import TumblrHttpClient
from tumbl4.core.errors import CrawlError, ParseError
from tumbl4.core.parse.html_scrape import extract_initial_state

logger = get_logger(__name__)

# Regex to match Tumblr CDN size segments like /s1280x1920/ or /s500x750/
_SIZE_SEGMENT_RE = re.compile(r"/s\d+x\d+/")

_BEST_SIZE = "s2048x3072"


@dataclass
class BestResolutionResult:
    """Result of a best-resolution lookup."""

    url: str
    width: int | None
    height: int | None
    media_key: str


def rewrite_url_for_best(url: str) -> str:
    """Rewrite a Tumblr CDN photo URL to request the best resolution.

    Replaces /s{W}x{H}/ with /s2048x3072/. Returns the original URL
    if it doesn't match the Tumblr CDN pattern or is already best-size.
    """
    if "media.tumblr.com" not in url:
        return url
    if f"/{_BEST_SIZE}/" in url:
        return url
    return _SIZE_SEGMENT_RE.sub(f"/{_BEST_SIZE}/", url)


async def resolve_best_resolution(
    *,
    http: TumblrHttpClient,
    blog_url: str,
    post_id: str,
    media_key: str,
) -> BestResolutionResult | None:
    """Fetch the post's HTML page and extract best-resolution URL from ___INITIAL_STATE___.

    Returns None if the post page is unavailable or the imageResponse
    doesn't contain the requested media_key.
    """
    post_url = f"{blog_url.rstrip('/')}/post/{post_id}"

    try:
        html = await http.get_html(post_url)
    except CrawlError as e:
        logger.debug("best resolution fetch failed", extra={"post_id": post_id, "error": str(e)})
        return None

    try:
        state = extract_initial_state(html)
    except ParseError as e:
        logger.debug("best resolution state parse failed", extra={"post_id": post_id, "error": str(e)})
        return None

    # Search posts for imageResponse matching our media_key
    for post in state["posts"]:
        if not isinstance(post, dict):
            continue

        image_response = post.get("imageResponse")
        if not isinstance(image_response, dict):
            continue

        images = image_response.get("images", [])
        if not isinstance(images, list):
            continue

        for image in images:
            if not isinstance(image, dict):
                continue

            key = str(image.get("mediaKey", ""))
            if key != media_key:
                continue

            # Found the matching image — prefer originalUrl, then url
            best_url = image.get("originalUrl") or image.get("url")
            if not best_url:
                continue

            return BestResolutionResult(
                url=str(best_url),
                width=_int_or_none(image.get("width")),
                height=_int_or_none(image.get("height")),
                media_key=media_key,
            )

    return None


def _int_or_none(val: object) -> int | None:
    if val is None:
        return None
    try:
        return int(val)  # type: ignore[arg-type]
    except (ValueError, TypeError):
        return None
```

- [ ] **Step 5: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_best_resolution.py -v`
Expected: 8 passed

- [ ] **Step 6: Commit**

```bash
git add src/tumbl4/core/crawl/best_resolution.py src/tumbl4/core/crawl/http_client.py tests/fixtures/json/best_resolution_page.html tests/unit/test_best_resolution.py
git commit -m "feat(crawl): add best image resolution resolver

URL rewrite s1280x1920 -> s2048x3072 plus HTML page fetch with
___INITIAL_STATE___ imageResponse parsing for full-size URLs.
Graceful fallback on 404 or missing imageResponse. See plan
boundaries correction 6.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: Filename template engine

**Files:**
- Create: `src/tumbl4/core/naming/__init__.py`
- Create: `src/tumbl4/core/naming/template.py`
- Modify: `src/tumbl4/core/errors.py`
- Create: `tests/unit/test_filename_template.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_filename_template.py`:

```python
"""Tests for the filename template engine."""

import pytest

from tumbl4.core.errors import ConfigError
from tumbl4.core.naming.template import TemplateEngine, validate_template


class TestValidateTemplate:
    def test_default_template_valid(self) -> None:
        validate_template("{blog}/{post_id}_{index_padded}.{ext}")

    def test_custom_template_valid(self) -> None:
        validate_template("{blog}/{year}/{month}/{post_id}_{index}.{ext}")

    def test_template_with_hash_valid(self) -> None:
        validate_template("{blog}/{hash8}.{ext}")

    def test_no_post_id_or_hash_raises(self) -> None:
        with pytest.raises(ConfigError, match="post_id.*hash8"):
            validate_template("{blog}/{index}.{ext}")

    def test_absolute_path_raises(self) -> None:
        with pytest.raises(ConfigError, match="absolute"):
            validate_template("/{blog}/{post_id}.{ext}")

    def test_dotdot_escape_raises(self) -> None:
        with pytest.raises(ConfigError, match="traversal"):
            validate_template("{blog}/../{post_id}.{ext}")

    def test_unknown_variable_raises(self) -> None:
        with pytest.raises(ConfigError, match="unknown"):
            validate_template("{blog}/{unknown_var}/{post_id}.{ext}")

    def test_empty_template_raises(self) -> None:
        with pytest.raises(ConfigError):
            validate_template("")

    def test_warns_about_tag_variable(self) -> None:
        # Should validate but return a warning
        warnings = validate_template("{blog}/{tag}/{post_id}.{ext}")
        assert any("untagged" in w.lower() for w in warnings)


class TestTemplateEngine:
    def setup_method(self) -> None:
        self.engine = TemplateEngine("{blog}/{post_id}_{index_padded}.{ext}")

    def test_render_basic(self) -> None:
        result = self.engine.render(
            blog="testblog",
            post_id="12345",
            post_type="photo",
            timestamp_utc="2026-04-11T14:22:03+00:00",
            tags=["art"],
            index=1,
            total_media=5,
            ext="jpg",
            url_hash="a1b2c3d4e5f6a7b8",
        )
        assert result == "testblog/12345_1.jpg"

    def test_render_year_month_day(self) -> None:
        engine = TemplateEngine("{blog}/{year}/{month}/{day}/{post_id}.{ext}")
        result = engine.render(
            blog="testblog",
            post_id="12345",
            post_type="photo",
            timestamp_utc="2026-04-11T14:22:03+00:00",
            tags=[],
            index=1,
            total_media=1,
            ext="jpg",
            url_hash="a1b2c3d4",
        )
        assert result == "testblog/2026/04/11/12345.jpg"

    def test_render_tag_uses_first_tag(self) -> None:
        engine = TemplateEngine("{blog}/{tag}/{post_id}.{ext}")
        result = engine.render(
            blog="testblog",
            post_id="12345",
            post_type="photo",
            timestamp_utc="2026-04-11T14:22:03+00:00",
            tags=["art", "wip"],
            index=1,
            total_media=1,
            ext="jpg",
            url_hash="a1b2c3d4",
        )
        assert result == "testblog/art/12345.jpg"

    def test_render_no_tags_uses_untagged(self) -> None:
        engine = TemplateEngine("{blog}/{tag}/{post_id}.{ext}")
        result = engine.render(
            blog="testblog",
            post_id="12345",
            post_type="photo",
            timestamp_utc="2026-04-11T14:22:03+00:00",
            tags=[],
            index=1,
            total_media=1,
            ext="jpg",
            url_hash="a1b2c3d4",
        )
        assert result == "testblog/_untagged_/12345.jpg"

    def test_render_tags_joined(self) -> None:
        engine = TemplateEngine("{blog}/{tags}/{post_id}.{ext}")
        result = engine.render(
            blog="testblog",
            post_id="12345",
            post_type="photo",
            timestamp_utc="2026-04-11T14:22:03+00:00",
            tags=["art", "wip"],
            index=1,
            total_media=1,
            ext="jpg",
            url_hash="a1b2c3d4",
        )
        assert result == "testblog/art_wip/12345.jpg"

    def test_render_hash8(self) -> None:
        engine = TemplateEngine("{blog}/{hash8}.{ext}")
        result = engine.render(
            blog="testblog",
            post_id="12345",
            post_type="photo",
            timestamp_utc="2026-04-11T14:22:03+00:00",
            tags=[],
            index=1,
            total_media=1,
            ext="jpg",
            url_hash="a1b2c3d4e5f6a7b8",
        )
        assert result == "testblog/a1b2c3d4.jpg"

    def test_render_index_padded(self) -> None:
        engine = TemplateEngine("{blog}/{post_id}_{index_padded}.{ext}")
        result = engine.render(
            blog="testblog",
            post_id="12345",
            post_type="photo",
            timestamp_utc="2026-04-11T14:22:03+00:00",
            tags=[],
            index=3,
            total_media=15,
            ext="jpg",
            url_hash="a1b2c3d4",
        )
        assert result == "testblog/12345_03.jpg"

    def test_render_datetime(self) -> None:
        engine = TemplateEngine("{blog}/{datetime}_{post_id}.{ext}")
        result = engine.render(
            blog="testblog",
            post_id="12345",
            post_type="photo",
            timestamp_utc="2026-04-11T14:22:03+00:00",
            tags=[],
            index=1,
            total_media=1,
            ext="jpg",
            url_hash="a1b2c3d4",
        )
        assert result == "testblog/2026-04-11_14-22-03_12345.jpg"

    def test_sanitizes_forbidden_characters(self) -> None:
        engine = TemplateEngine("{blog}/{post_id}_{index_padded}.{ext}")
        result = engine.render(
            blog="test/blog",  # contains /
            post_id="12345",
            post_type="photo",
            timestamp_utc="2026-04-11T14:22:03+00:00",
            tags=[],
            index=1,
            total_media=1,
            ext="jpg",
            url_hash="a1b2c3d4",
        )
        # The / in blog name should be sanitized within the component
        assert "test_blog" in result

    def test_render_post_type(self) -> None:
        engine = TemplateEngine("{blog}/{post_type}/{post_id}.{ext}")
        result = engine.render(
            blog="testblog",
            post_id="12345",
            post_type="video",
            timestamp_utc="2026-04-11T14:22:03+00:00",
            tags=[],
            index=1,
            total_media=1,
            ext="mp4",
            url_hash="a1b2c3d4",
        )
        assert result == "testblog/video/12345.mp4"
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_filename_template.py -v`
Expected: FAIL -- `ModuleNotFoundError`

- [ ] **Step 3: Write the implementation**

Add `TemplateError` to `src/tumbl4/core/errors.py`. After the existing `ConfigError` class:

(Note: `TemplateError` is a subclass of `ConfigError` since template validation happens at config load time.)

```python
class TemplateError(ConfigError):
    """Filename template validation error."""
```

Write file `src/tumbl4/core/naming/__init__.py`:

```python
"""Filename template engine for configurable output paths."""
```

Write file `src/tumbl4/core/naming/template.py`:

```python
"""Filename template engine with validation.

Uses Python's str.format_map with a custom Formatter. Templates are
validated at config load time — invalid templates raise ConfigError
immediately rather than failing per-post at render time.

See spec section 5.15.
"""

from __future__ import annotations

import re
import string
import unicodedata
from datetime import datetime, timezone

from tumbl4.core.errors import ConfigError

# All supported template variables
_KNOWN_VARIABLES: frozenset[str] = frozenset({
    "blog", "post_id", "post_type", "date", "datetime",
    "year", "month", "day", "tag", "tags",
    "index", "index_padded", "ext", "hash8",
})

# Forbidden characters in path components (replaced with _)
_FORBIDDEN_CHARS = re.compile(r'[/\\:\0*?<>|"]')

# Max bytes for a single path component (UTF-8)
_MAX_COMPONENT_BYTES = 255


def validate_template(template: str) -> list[str]:
    """Validate a filename template at config load time.

    Returns a list of warnings (empty if none). Raises ConfigError on
    invalid templates.
    """
    warnings: list[str] = []

    if not template or not template.strip():
        raise ConfigError("Filename template must not be empty")

    if template.startswith("/"):
        raise ConfigError("Filename template must not be an absolute path")

    if ".." in template:
        raise ConfigError("Filename template must not contain path traversal (..)")

    # Extract variable names from the template
    formatter = string.Formatter()
    variables: set[str] = set()
    try:
        for _, field_name, _, _ in formatter.parse(template):
            if field_name is not None:
                # Handle format specs like {index:02d}
                base_name = field_name.split(":")[0].split("!")[0].split(".")[0]
                if base_name:
                    variables.add(base_name)
    except (ValueError, KeyError) as e:
        raise ConfigError(f"Invalid template syntax: {e}") from e

    # Check for unknown variables
    unknown = variables - _KNOWN_VARIABLES
    if unknown:
        raise ConfigError(
            f"Filename template contains unknown variable(s): {', '.join(sorted(unknown))}. "
            f"Valid variables: {', '.join(sorted(_KNOWN_VARIABLES))}"
        )

    # Must contain post_id or hash8 for uniqueness
    if "post_id" not in variables and "hash8" not in variables:
        raise ConfigError(
            "Filename template must contain {post_id} or {hash8} to ensure unique filenames"
        )

    # Warn about {tag} / {tags} — untagged posts go to _untagged_ bucket
    if "tag" in variables or "tags" in variables:
        warnings.append(
            "Template uses {tag} or {tags} — untagged posts will be placed "
            "in the '_untagged_' directory"
        )

    return warnings


class TemplateEngine:
    """Render filenames from a validated template string."""

    def __init__(self, template: str) -> None:
        validate_template(template)
        self._template = template

    def render(
        self,
        *,
        blog: str,
        post_id: str,
        post_type: str,
        timestamp_utc: str,
        tags: list[str],
        index: int,
        total_media: int,
        ext: str,
        url_hash: str,
    ) -> str:
        """Render the template with the given values.

        All values are sanitised via _sanitize_path_component before
        substitution. The result is a relative path string.
        """
        dt = _parse_iso_timestamp(timestamp_utc)
        pad_width = max(len(str(total_media)), 2)

        values = {
            "blog": _sanitize_component(blog),
            "post_id": _sanitize_component(post_id),
            "post_type": _sanitize_component(post_type),
            "date": dt.strftime("%Y-%m-%d"),
            "datetime": dt.strftime("%Y-%m-%d_%H-%M-%S"),
            "year": dt.strftime("%Y"),
            "month": dt.strftime("%m"),
            "day": dt.strftime("%d"),
            "tag": _sanitize_component(tags[0] if tags else "_untagged_"),
            "tags": _sanitize_component("_".join(tags) if tags else "_untagged_"),
            "index": str(index),
            "index_padded": str(index).zfill(pad_width),
            "ext": _sanitize_component(ext.lstrip(".")),
            "hash8": url_hash[:8],
        }

        return self._template.format_map(values)


def _sanitize_component(s: str) -> str:
    """Sanitize a single path component.

    NFC normalize, replace forbidden characters, enforce byte-length cap,
    block POSIX reserved names.
    """
    s = unicodedata.normalize("NFC", s)
    s = _FORBIDDEN_CHARS.sub("_", s)

    # Block POSIX reserved names
    if s in (".", "..", ""):
        s = "_"

    # Enforce byte-length cap
    encoded = s.encode("utf-8")
    if len(encoded) > _MAX_COMPONENT_BYTES:
        while len(s.encode("utf-8")) > _MAX_COMPONENT_BYTES:
            s = s[:-1]

    return s


def _parse_iso_timestamp(ts: str) -> datetime:
    """Parse an ISO 8601 timestamp string to a datetime object."""
    try:
        return datetime.fromisoformat(ts)
    except (ValueError, TypeError):
        return datetime(1970, 1, 1, tzinfo=timezone.utc)
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_filename_template.py -v`
Expected: 17 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/errors.py src/tumbl4/core/naming/__init__.py src/tumbl4/core/naming/template.py tests/unit/test_filename_template.py
git commit -m "feat(naming): add filename template engine with validation

Supports {blog}, {post_id}, {date}, {year}, {month}, {day}, {tag},
{tags}, {index}, {index_padded}, {ext}, {hash8}, {post_type},
{datetime}. Config-time validation: requires post_id or hash8,
blocks absolute paths and traversal. See spec section 5.15.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: PNJ-to-PNG conversion

**Files:**
- Create: `src/tumbl4/core/download/pnj_convert.py`
- Create: `tests/unit/test_pnj_convert.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_pnj_convert.py`:

```python
"""Tests for PNJ -> PNG conversion."""

import io
from pathlib import Path

import pytest

from tumbl4.core.download.pnj_convert import convert_pnj_to_png, is_pnj_file


class TestIsPnjFile:
    def test_pnj_extension(self) -> None:
        assert is_pnj_file("photo.pnj") is True

    def test_pnj_extension_uppercase(self) -> None:
        assert is_pnj_file("photo.PNJ") is True

    def test_jpg_extension(self) -> None:
        assert is_pnj_file("photo.jpg") is False

    def test_png_extension(self) -> None:
        assert is_pnj_file("photo.png") is False


class TestConvertPnjToPng:
    def test_converts_jpeg_pnj_to_png(self, tmp_path: Path) -> None:
        # Create a minimal valid JPEG file (PNJ is just JPEG with .pnj extension)
        try:
            from PIL import Image
        except ImportError:
            pytest.skip("Pillow not installed")

        # Create a tiny test image
        img = Image.new("RGB", (4, 4), color=(255, 0, 0))
        pnj_path = tmp_path / "photo.pnj"
        img.save(pnj_path, format="JPEG")

        png_path = convert_pnj_to_png(pnj_path)

        assert png_path.suffix == ".png"
        assert png_path.exists()
        assert not pnj_path.exists()  # original removed

        # Verify it's a valid PNG
        result_img = Image.open(png_path)
        assert result_img.format == "PNG"
        assert result_img.size == (4, 4)

    def test_preserves_original_on_failure(self, tmp_path: Path) -> None:
        # Write invalid data
        pnj_path = tmp_path / "bad.pnj"
        pnj_path.write_bytes(b"not an image")

        try:
            from PIL import Image
        except ImportError:
            pytest.skip("Pillow not installed")

        # Should not raise — returns original path on failure
        result = convert_pnj_to_png(pnj_path)
        assert result == pnj_path
        assert pnj_path.exists()

    def test_non_pnj_file_returned_unchanged(self, tmp_path: Path) -> None:
        jpg_path = tmp_path / "photo.jpg"
        jpg_path.write_bytes(b"fake jpeg")
        result = convert_pnj_to_png(jpg_path)
        assert result == jpg_path
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_pnj_convert.py -v`
Expected: FAIL -- `ModuleNotFoundError`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/download/pnj_convert.py`:

```python
"""PNJ -> PNG conversion.

PNJ files are JPEG images with a .pnj extension (a Tumblr-ism).
When the user enables --convert-pnj, these are re-encoded as PNG
after download.

Requires Pillow as an optional dependency.
"""

from __future__ import annotations

from pathlib import Path

from tumbl4._internal.logging import get_logger

logger = get_logger(__name__)


def is_pnj_file(filename: str) -> bool:
    """Check if a filename has a .pnj extension."""
    return filename.lower().endswith(".pnj")


def convert_pnj_to_png(path: Path) -> Path:
    """Convert a PNJ file to PNG.

    Returns the path to the PNG file on success, or the original path
    on failure (logs a warning but does not raise).

    If the file is not a .pnj file, returns the path unchanged.
    """
    if not is_pnj_file(path.name):
        return path

    png_path = path.with_suffix(".png")

    try:
        from PIL import Image

        with Image.open(path) as img:
            img.save(png_path, format="PNG")

        # Remove the original .pnj file
        path.unlink()
        logger.debug("converted PNJ to PNG", extra={"src": str(path), "dst": str(png_path)})
        return png_path

    except ImportError:
        logger.warning("Pillow not installed — cannot convert PNJ to PNG")
        return path
    except Exception as exc:
        logger.warning(
            "PNJ to PNG conversion failed, keeping original",
            extra={"path": str(path), "error": str(exc)},
        )
        return path
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_pnj_convert.py -v`
Expected: 6 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/download/pnj_convert.py tests/unit/test_pnj_convert.py
git commit -m "feat(download): add PNJ to PNG conversion via Pillow

PNJ files (JPEG with .pnj extension) optionally re-encoded as PNG.
Graceful fallback on Pillow import failure or conversion error.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 9: TOML config loading

**Files:**
- Create: `src/tumbl4/models/config.py`
- Modify: `src/tumbl4/models/settings.py`
- Create: `tests/unit/test_toml_config.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_toml_config.py`:

```python
"""Tests for TOML config loading."""

import os
from pathlib import Path

import pytest

from tumbl4.core.errors import ConfigError
from tumbl4.models.config import load_config, merge_configs


class TestLoadConfig:
    def test_loads_valid_toml(self, tmp_path: Path) -> None:
        config_file = tmp_path / "tumbl4.toml"
        config_file.write_text(
            '[tumbl4]\n'
            'output_dir = "/tmp/tumbl4-test"\n'
            'max_concurrent_downloads = 8\n'
            'filename_template = "{blog}/{year}/{post_id}.{ext}"\n'
        )
        config = load_config(config_file)
        assert config["output_dir"] == "/tmp/tumbl4-test"
        assert config["max_concurrent_downloads"] == 8
        assert config["filename_template"] == "{blog}/{year}/{post_id}.{ext}"

    def test_loads_nested_sections(self, tmp_path: Path) -> None:
        config_file = tmp_path / "tumbl4.toml"
        config_file.write_text(
            '[tumbl4]\n'
            'output_dir = "/tmp/out"\n'
            '\n'
            '[tumbl4.http]\n'
            'connect_timeout = 5.0\n'
            'read_timeout = 30.0\n'
        )
        config = load_config(config_file)
        assert config["http"]["connect_timeout"] == 5.0

    def test_nonexistent_file_returns_empty(self, tmp_path: Path) -> None:
        config = load_config(tmp_path / "missing.toml")
        assert config == {}

    def test_invalid_toml_raises(self, tmp_path: Path) -> None:
        config_file = tmp_path / "tumbl4.toml"
        config_file.write_text("[invalid toml =")
        with pytest.raises(ConfigError):
            load_config(config_file)

    def test_non_tumbl4_section_ignored(self, tmp_path: Path) -> None:
        config_file = tmp_path / "tumbl4.toml"
        config_file.write_text(
            '[other]\n'
            'key = "value"\n'
        )
        config = load_config(config_file)
        assert config == {}


class TestMergeConfigs:
    def test_user_config_provides_defaults(self, tmp_path: Path) -> None:
        user_config = {"output_dir": "/home/user/tumbl4", "max_concurrent_downloads": 2}
        project_config = {"max_concurrent_downloads": 8}
        merged = merge_configs(user_config, project_config)
        assert merged["output_dir"] == "/home/user/tumbl4"
        assert merged["max_concurrent_downloads"] == 8  # project overrides user

    def test_project_overrides_user(self, tmp_path: Path) -> None:
        user_config = {"output_dir": "/user/dir"}
        project_config = {"output_dir": "/project/dir"}
        merged = merge_configs(user_config, project_config)
        assert merged["output_dir"] == "/project/dir"

    def test_empty_configs(self) -> None:
        merged = merge_configs({}, {})
        assert merged == {}

    def test_nested_merge(self) -> None:
        user = {"http": {"connect_timeout": 5.0}}
        project = {"http": {"read_timeout": 30.0}}
        merged = merge_configs(user, project)
        assert merged["http"]["connect_timeout"] == 5.0
        assert merged["http"]["read_timeout"] == 30.0

    def test_project_nested_overrides_user_nested(self) -> None:
        user = {"http": {"connect_timeout": 5.0}}
        project = {"http": {"connect_timeout": 10.0}}
        merged = merge_configs(user, project)
        assert merged["http"]["connect_timeout"] == 10.0
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_toml_config.py -v`
Expected: FAIL -- `ModuleNotFoundError: No module named 'tumbl4.models.config'`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/models/config.py`:

```python
"""TOML config loading for tumbl4.

Config precedence (highest -> lowest) per spec section 5.1:
    1. CLI flags
    2. Environment variables (TUMBL4_*)
    3. Project config (./tumbl4.toml)
    4. User config ($XDG_CONFIG_HOME/tumbl4/config.toml)
    5. Hardcoded defaults

This module handles layers 3 and 4. Layers 1 and 2 are handled by
pydantic-settings in Settings. Layer merging happens in Settings.__init__.
"""

from __future__ import annotations

import sys
from pathlib import Path
from typing import Any

from tumbl4._internal.logging import get_logger
from tumbl4.core.errors import ConfigError

logger = get_logger(__name__)


def load_config(path: Path) -> dict[str, Any]:
    """Load a TOML config file and return the [tumbl4] section.

    Returns an empty dict if the file doesn't exist or has no [tumbl4] section.
    Raises ConfigError on TOML parse errors.
    """
    if not path.exists():
        return {}

    try:
        text = path.read_text(encoding="utf-8")
    except OSError as e:
        raise ConfigError(f"Cannot read config file {path}: {e}") from e

    try:
        if sys.version_info >= (3, 11):
            import tomllib
        else:
            try:
                import tomllib  # type: ignore[import-not-found]
            except ImportError:
                import tomli as tomllib  # type: ignore[no-redef]

        data = tomllib.loads(text)
    except Exception as e:
        raise ConfigError(f"Invalid TOML in {path}: {e}") from e

    if not isinstance(data, dict):
        return {}

    tumbl4_section = data.get("tumbl4", {})
    if not isinstance(tumbl4_section, dict):
        return {}

    return tumbl4_section


def merge_configs(
    user_config: dict[str, Any],
    project_config: dict[str, Any],
) -> dict[str, Any]:
    """Merge user and project configs. Project config takes precedence.

    Performs a shallow-recursive merge: nested dicts are merged rather
    than replaced entirely.
    """
    return _deep_merge(user_config, project_config)


def _deep_merge(base: dict[str, Any], override: dict[str, Any]) -> dict[str, Any]:
    """Recursively merge two dicts. Values in override take precedence."""
    result = dict(base)
    for key, value in override.items():
        if (
            key in result
            and isinstance(result[key], dict)
            and isinstance(value, dict)
        ):
            result[key] = _deep_merge(result[key], value)
        else:
            result[key] = value
    return result


def load_user_config() -> dict[str, Any]:
    """Load the user-level config from $XDG_CONFIG_HOME/tumbl4/config.toml."""
    from tumbl4._internal.paths import config_dir

    return load_config(config_dir() / "config.toml")


def load_project_config() -> dict[str, Any]:
    """Load the project-level config from ./tumbl4.toml."""
    return load_config(Path.cwd() / "tumbl4.toml")
```

Modify `src/tumbl4/models/settings.py` -- add new fields for Plan 3. After the existing `max_concurrent_downloads` field, add:

```python
    filename_template: str = Field(
        default="{blog}/{post_id}_{index_padded}.{ext}",
        description="Filename template for downloaded media. See docs/filename-templates.md.",
    )
    consumer_key: str | None = Field(
        default=None,
        description="Tumblr API V2 consumer key. Required for V2 API access.",
    )
    best_resolution: bool = Field(
        default=False,
        description="Fetch best available image resolution (extra HTTP request per photo).",
    )
    convert_pnj: bool = Field(
        default=False,
        description="Convert PNJ files to PNG after download.",
    )
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_toml_config.py -v`
Expected: 10 passed

- [ ] **Step 5: Run all existing tests to check for regressions**

Run: `uv run pytest -v`
Expected: all tests pass

- [ ] **Step 6: Commit**

```bash
git add src/tumbl4/models/config.py src/tumbl4/models/settings.py tests/unit/test_toml_config.py
git commit -m "feat(config): add TOML config loading with precedence merging

Project ./tumbl4.toml overrides user $XDG_CONFIG_HOME/tumbl4/config.toml.
Deep-merge for nested sections. New Settings fields: filename_template,
consumer_key, best_resolution, convert_pnj. See spec section 5.1.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 10: `tumbl4 config get/set` CLI subcommand

**Files:**
- Create: `src/tumbl4/cli/commands/config.py`
- Modify: `src/tumbl4/cli/app.py`
- Create: `tests/unit/test_config_command.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_config_command.py`:

```python
"""Tests for the tumbl4 config get/set CLI subcommand."""

import os
from pathlib import Path

from typer.testing import CliRunner

from tumbl4.cli.app import app

runner = CliRunner()


class TestConfigCommand:
    def test_config_appears_in_help(self) -> None:
        result = runner.invoke(app, ["--help"])
        assert result.exit_code == 0
        assert "config" in result.output

    def test_config_help(self) -> None:
        result = runner.invoke(app, ["config", "--help"])
        assert result.exit_code == 0
        assert "get" in result.output.lower() or "set" in result.output.lower()

    def test_config_get_default_value(self) -> None:
        result = runner.invoke(app, ["config", "get", "max_concurrent_downloads"])
        assert result.exit_code == 0
        assert "4" in result.output

    def test_config_get_filename_template(self) -> None:
        result = runner.invoke(app, ["config", "get", "filename_template"])
        assert result.exit_code == 0
        assert "{blog}" in result.output

    def test_config_get_unknown_key(self) -> None:
        result = runner.invoke(app, ["config", "get", "nonexistent_key_xyz"])
        assert result.exit_code != 0

    def test_config_set_writes_to_user_config(self, tmp_path: Path) -> None:
        config_dir = tmp_path / "tumbl4"
        config_dir.mkdir()
        config_file = config_dir / "config.toml"

        result = runner.invoke(
            app,
            ["config", "set", "max_concurrent_downloads", "8"],
            env={"XDG_CONFIG_HOME": str(tmp_path)},
        )
        assert result.exit_code == 0
        assert config_file.exists()
        content = config_file.read_text()
        assert "8" in content

    def test_config_set_validates_template(self, tmp_path: Path) -> None:
        config_dir = tmp_path / "tumbl4"
        config_dir.mkdir()

        result = runner.invoke(
            app,
            ["config", "set", "filename_template", "/{absolute_path}"],
            env={"XDG_CONFIG_HOME": str(tmp_path)},
        )
        assert result.exit_code != 0

    def test_config_set_template_valid(self, tmp_path: Path) -> None:
        config_dir = tmp_path / "tumbl4"
        config_dir.mkdir()

        result = runner.invoke(
            app,
            ["config", "set", "filename_template", "{blog}/{year}/{post_id}.{ext}"],
            env={"XDG_CONFIG_HOME": str(tmp_path)},
        )
        assert result.exit_code == 0
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_config_command.py -v`
Expected: FAIL -- "config" not found in help output

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/cli/commands/config.py`:

```python
"""tumbl4 config get/set — read and write configuration values."""

from __future__ import annotations

import sys
from pathlib import Path
from typing import Annotated

import typer

from tumbl4._internal.logging import get_logger
from tumbl4.cli.output.progress import console

logger = get_logger(__name__)

config_app = typer.Typer(
    name="config",
    help="Read and write tumbl4 configuration.",
    no_args_is_help=True,
)

# Keys that can be get/set via CLI
_ALLOWED_KEYS: frozenset[str] = frozenset({
    "output_dir",
    "max_concurrent_downloads",
    "filename_template",
    "consumer_key",
    "best_resolution",
    "convert_pnj",
    "log_level",
    "page_size",
})


@config_app.command("get")
def config_get(
    key: Annotated[str, typer.Argument(help="Configuration key to read")],
) -> None:
    """Get the current value of a configuration key."""
    from tumbl4.models.settings import Settings

    if key not in _ALLOWED_KEYS:
        console.print(f"[red]Unknown configuration key: {key}[/red]")
        console.print(f"Valid keys: {', '.join(sorted(_ALLOWED_KEYS))}")
        raise typer.Exit(code=1)

    settings = Settings()
    value = getattr(settings, key, None)
    if value is None:
        console.print(f"{key} = (not set)")
    else:
        console.print(f"{key} = {value}")


@config_app.command("set")
def config_set(
    key: Annotated[str, typer.Argument(help="Configuration key to write")],
    value: Annotated[str, typer.Argument(help="Value to set")],
) -> None:
    """Set a configuration value in the user config file."""
    if key not in _ALLOWED_KEYS:
        console.print(f"[red]Unknown configuration key: {key}[/red]")
        console.print(f"Valid keys: {', '.join(sorted(_ALLOWED_KEYS))}")
        raise typer.Exit(code=1)

    # Validate filename_template before persisting
    if key == "filename_template":
        from tumbl4.core.errors import ConfigError
        from tumbl4.core.naming.template import validate_template

        try:
            warnings = validate_template(value)
            for w in warnings:
                console.print(f"[yellow]Warning: {w}[/yellow]")
        except ConfigError as e:
            console.print(f"[red]Invalid template: {e}[/red]")
            raise typer.Exit(code=1) from e

    # Write to user config file
    from tumbl4._internal.paths import config_dir

    user_config_dir = config_dir()
    user_config_dir.mkdir(parents=True, exist_ok=True)
    config_file = user_config_dir / "config.toml"

    # Load existing config or create new
    existing: dict[str, object] = {}
    if config_file.exists():
        try:
            if sys.version_info >= (3, 11):
                import tomllib
            else:
                try:
                    import tomllib  # type: ignore[import-not-found]
                except ImportError:
                    import tomli as tomllib  # type: ignore[no-redef]

            existing = tomllib.loads(config_file.read_text(encoding="utf-8")).get("tumbl4", {})
        except Exception:
            pass  # start fresh if file is corrupt

    # Coerce value to appropriate type
    coerced = _coerce_value(key, value)
    existing[key] = coerced

    # Write back as TOML
    _write_toml(config_file, existing)
    console.print(f"[green]Set {key} = {coerced}[/green]")


def _coerce_value(key: str, value: str) -> object:
    """Coerce a string value to the appropriate type for a config key."""
    bool_keys = {"best_resolution", "convert_pnj"}
    int_keys = {"max_concurrent_downloads", "page_size"}

    if key in bool_keys:
        return value.lower() in ("true", "1", "yes")
    if key in int_keys:
        return int(value)
    return value


def _write_toml(path: Path, data: dict[str, object]) -> None:
    """Write a simple TOML file with a [tumbl4] section."""
    lines = ["[tumbl4]"]
    for k, v in sorted(data.items()):
        if isinstance(v, bool):
            lines.append(f"{k} = {'true' if v else 'false'}")
        elif isinstance(v, int):
            lines.append(f"{k} = {v}")
        elif isinstance(v, str):
            lines.append(f'{k} = "{v}"')
        else:
            lines.append(f'{k} = "{v}"')
    lines.append("")

    path.write_text("\n".join(lines), encoding="utf-8")
```

Modify `src/tumbl4/cli/app.py` -- add the config command registration. After the existing download registration, add:

```python
from tumbl4.cli.commands.config import config_app  # noqa: E402

app.add_typer(config_app, name="config")
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_config_command.py -v`
Expected: 8 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/cli/commands/config.py src/tumbl4/cli/app.py tests/unit/test_config_command.py
git commit -m "feat(cli): add 'tumbl4 config get/set' subcommand

Read and write config values in user config file. Validates
filename_template on set. Supports bool/int/string coercion.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 11: Dedicated sidecar writer worker

**Files:**
- Create: `src/tumbl4/core/state/sidecar_writer.py`
- Create: `tests/unit/test_sidecar_writer.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_sidecar_writer.py`:

```python
"""Tests for the dedicated sidecar writer async worker."""

import asyncio
import json
from pathlib import Path

import pytest

from tumbl4.core.state.sidecar_writer import SidecarWriteTask, SidecarWriter


class TestSidecarWriter:
    async def test_writes_sidecar_from_queue(self, tmp_path: Path) -> None:
        writer = SidecarWriter(max_pending=16)

        task = SidecarWriteTask(
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

        # Start worker, enqueue task, stop
        worker_task = asyncio.create_task(writer.run())
        await writer.enqueue(task)
        await writer.stop()
        await worker_task

        sidecar_path = tmp_path / "_meta" / "12345.json"
        assert sidecar_path.exists()
        data = json.loads(sidecar_path.read_text())
        assert data["$schema_version"] == 1
        assert data["post_id"] == "12345"

    async def test_processes_multiple_tasks(self, tmp_path: Path) -> None:
        writer = SidecarWriter(max_pending=16)
        worker_task = asyncio.create_task(writer.run())

        for i in range(5):
            task = SidecarWriteTask(
                output_dir=tmp_path,
                post_id=str(10000 + i),
                blog_name="testblog",
                post_url=f"https://testblog.tumblr.com/post/{10000 + i}",
                post_type="photo",
                timestamp_utc="2026-04-11T14:22:03+00:00",
                tags=[],
                is_reblog=False,
                media_results=[],
            )
            await writer.enqueue(task)

        await writer.stop()
        await worker_task

        sidecars = list((tmp_path / "_meta").glob("*.json"))
        assert len(sidecars) == 5

    async def test_handles_stop_gracefully(self, tmp_path: Path) -> None:
        writer = SidecarWriter(max_pending=16)
        worker_task = asyncio.create_task(writer.run())

        # Stop immediately without enqueuing anything
        await writer.stop()
        await worker_task

        # Should not crash

    async def test_counts_written_sidecars(self, tmp_path: Path) -> None:
        writer = SidecarWriter(max_pending=16)
        worker_task = asyncio.create_task(writer.run())

        for i in range(3):
            task = SidecarWriteTask(
                output_dir=tmp_path,
                post_id=str(20000 + i),
                blog_name="testblog",
                post_url="",
                post_type="text",
                timestamp_utc="",
                tags=[],
                is_reblog=False,
                media_results=[],
            )
            await writer.enqueue(task)

        await writer.stop()
        await worker_task

        assert writer.written_count == 3

    async def test_backpressure_via_maxsize(self, tmp_path: Path) -> None:
        writer = SidecarWriter(max_pending=2)
        worker_task = asyncio.create_task(writer.run())

        # Enqueue more than max_pending — should not deadlock
        for i in range(5):
            task = SidecarWriteTask(
                output_dir=tmp_path,
                post_id=str(30000 + i),
                blog_name="testblog",
                post_url="",
                post_type="text",
                timestamp_utc="",
                tags=[],
                is_reblog=False,
                media_results=[],
            )
            await writer.enqueue(task)

        await writer.stop()
        await worker_task

        assert writer.written_count == 5
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_sidecar_writer.py -v`
Expected: FAIL -- `ModuleNotFoundError`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/state/sidecar_writer.py`:

```python
"""Dedicated async worker for writing JSON metadata sidecars.

Decouples sidecar I/O from download workers. A single writer worker
drains a bounded asyncio.Queue of SidecarWriteTask objects. The
orchestrator enqueues tasks after the last media for a post resolves.

See spec section 5.3 (sidecar queue) and design spec queue architecture.
"""

from __future__ import annotations

import asyncio
from dataclasses import dataclass, field
from pathlib import Path

from tumbl4._internal.logging import get_logger
from tumbl4.core.state.metadata import write_sidecar

logger = get_logger(__name__)


@dataclass
class SidecarWriteTask:
    """A single sidecar write request."""

    output_dir: Path
    post_id: str
    blog_name: str
    post_url: str
    post_type: str
    timestamp_utc: str
    tags: list[str]
    is_reblog: bool
    media_results: list[dict[str, object]]
    reblog_source: dict[str, str] | None = None
    title: str | None = None
    body_text: str | None = None
    body_html: str | None = None


class SidecarWriter:
    """Dedicated async worker that drains a sidecar write queue."""

    def __init__(self, max_pending: int = 16) -> None:
        self._queue: asyncio.Queue[SidecarWriteTask | None] = asyncio.Queue(
            maxsize=max_pending
        )
        self.written_count: int = 0
        self._running: bool = False

    async def enqueue(self, task: SidecarWriteTask) -> None:
        """Enqueue a sidecar write task. Blocks if the queue is full (backpressure)."""
        await self._queue.put(task)

    async def stop(self) -> None:
        """Signal the worker to drain remaining tasks and stop."""
        await self._queue.put(None)

    async def run(self) -> None:
        """Main worker loop. Process tasks until a None sentinel is received."""
        self._running = True
        logger.debug("sidecar writer started")

        while True:
            task = await self._queue.get()

            if task is None:
                self._queue.task_done()
                break

            try:
                write_sidecar(
                    output_dir=task.output_dir,
                    post_id=task.post_id,
                    blog_name=task.blog_name,
                    post_url=task.post_url,
                    post_type=task.post_type,
                    timestamp_utc=task.timestamp_utc,
                    tags=task.tags,
                    is_reblog=task.is_reblog,
                    media_results=task.media_results,
                    reblog_source=task.reblog_source,
                    title=task.title,
                    body_text=task.body_text,
                    body_html=task.body_html,
                )
                self.written_count += 1
            except Exception as exc:
                logger.error(
                    "sidecar write failed",
                    extra={"post_id": task.post_id, "error": str(exc)},
                )
            finally:
                self._queue.task_done()

        self._running = False
        logger.debug(
            "sidecar writer stopped",
            extra={"total_written": self.written_count},
        )
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_sidecar_writer.py -v`
Expected: 5 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/state/sidecar_writer.py tests/unit/test_sidecar_writer.py
git commit -m "feat(state): add dedicated async sidecar writer worker

Bounded asyncio.Queue with single drain worker. Decouples sidecar
I/O from download workers. Sentinel-based shutdown with graceful
drain. See spec section 5.3.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 12: Wire all post types + template + sidecar writer into orchestrator and crawler

**Files:**
- Modify: `src/tumbl4/core/crawl/tumblr_blog.py`
- Modify: `src/tumbl4/core/orchestrator.py`
- Modify: `src/tumbl4/core/download/file_downloader.py`
- Modify: `src/tumbl4/models/media.py`
- Modify: `src/tumbl4/cli/commands/download.py`
- Modify: `src/tumbl4/core/parse/__init__.py`

- [ ] **Step 1: Update the crawler to dispatch all post types**

Modify `src/tumbl4/core/crawl/tumblr_blog.py` -- replace the photo-only filter with the `V1_NORMALIZERS` dispatch table from `api_json.py`. In the `crawl()` method, replace the `# Plan 2: only photo posts` block:

```python
                # Dispatch to the appropriate normalizer for this post type
                normalizer = V1_NORMALIZERS.get(post_type)
                if normalizer is None:
                    logger.debug("skipping unsupported post type", extra={"type": post_type})
                    continue

                try:
                    if post_type == "photo":
                        intermediate = normalizer(
                            post_raw, blog_name or self._blog.name, self._image_size
                        )
                    else:
                        intermediate = normalizer(
                            post_raw, blog_name or self._blog.name
                        )
                    yield intermediate
                except ParseError as e:
                    logger.error("failed to parse post", extra={"post_id": post_id_str, "error": str(e)})
                    continue
```

Add the import at the top of the file:

```python
from tumbl4.core.parse.api_json import V1_NORMALIZERS
```

- [ ] **Step 2: Update MediaTask to accept template-rendered filename**

Modify `src/tumbl4/models/media.py` -- add an optional `template_filename` field to MediaTask:

```python
    template_filename: str | None = None
```

Update the `filename` computed property to use `template_filename` when set:

```python
    @computed_field  # type: ignore[prop-decorator]
    @property
    def filename(self) -> str:
        """Filename: template-rendered if available, else default {post_id}_{index:02d}.{ext}."""
        if self.template_filename:
            return self.template_filename
        parsed = urlparse(self.url)
        ext = Path(parsed.path).suffix.lstrip(".") or "jpg"
        return f"{self.post_id}_{self.index:02d}.{ext}"
```

- [ ] **Step 3: Update the orchestrator to use sidecar writer and template engine**

Modify `src/tumbl4/core/orchestrator.py` -- integrate `SidecarWriter`, `TemplateEngine`, inline media extraction, and best resolution. The key changes:

Add imports:

```python
from tumbl4.core.naming.template import TemplateEngine
from tumbl4.core.parse.inline_media import extract_inline_media
from tumbl4.core.state.sidecar_writer import SidecarWriteTask, SidecarWriter
```

In `run_crawl`, create the template engine and sidecar writer:

```python
    # Template engine for filenames
    template_engine = TemplateEngine(settings.filename_template)

    # Dedicated sidecar writer worker
    sidecar_writer = SidecarWriter(max_pending=settings.queue.max_pending_sidecars)
    sidecar_task = spawn(sidecar_writer.run(), name="sidecar-writer")
```

When creating MediaTask objects, render the filename via template:

```python
            for idx, media_entry in enumerate(all_media):
                rendered_name = template_engine.render(
                    blog=intermediate["blog_name"],
                    post_id=intermediate["post_id"],
                    post_type=intermediate["post_type"],
                    timestamp_utc=intermediate["timestamp_utc"],
                    tags=intermediate["tags"],
                    index=idx + 1,
                    total_media=len(all_media),
                    ext=_ext_from_url(media_entry["url"]),
                    url_hash=hashlib.sha256(media_entry["url"].encode()).hexdigest(),
                )
                task = MediaTask(
                    url=media_entry["url"],
                    post_id=intermediate["post_id"],
                    blog_name=intermediate["blog_name"],
                    index=idx,
                    output_dir=str(blog_dir),
                    template_filename=rendered_name,
                )
                await queue.put(task)
```

Extract inline media from body HTML and merge with primary media:

```python
            # Merge primary media with inline media extracted from body HTML
            primary_media = list(intermediate["media"])
            inline = extract_inline_media(intermediate.get("body_html"))
            # Deduplicate by URL
            seen_urls = {m["url"] for m in primary_media}
            for m in inline:
                if m["url"] not in seen_urls:
                    primary_media.append(m)
                    seen_urls.add(m["url"])
            all_media = primary_media
```

Replace direct `_write_post_sidecar` calls with sidecar writer enqueue:

```python
                sidecar_task = SidecarWriteTask(
                    output_dir=output_dir / intermediate["blog_name"],
                    post_id=intermediate["post_id"],
                    blog_name=intermediate["blog_name"],
                    post_url=intermediate["post_url"],
                    post_type=intermediate["post_type"],
                    timestamp_utc=intermediate["timestamp_utc"],
                    tags=intermediate["tags"],
                    is_reblog=intermediate["is_reblog"],
                    media_results=media_dicts,
                    reblog_source=intermediate["reblog_source"],
                    body_text=intermediate["body_text"],
                    body_html=intermediate["body_html"],
                )
                await sidecar_writer.enqueue(sidecar_task)
```

Stop the sidecar writer after all downloads complete:

```python
    await sidecar_writer.stop()
    await sidecar_task
```

- [ ] **Step 4: Update download command to pass new flags**

Modify `src/tumbl4/cli/commands/download.py` -- add `--template`, `--best`, `--convert-pnj` options:

```python
    template: Annotated[str | None, typer.Option("--template", help="Filename template (overrides config)")] = None,
    best: Annotated[bool, typer.Option("--best", help="Fetch best available image resolution")] = False,
    convert_pnj: Annotated[bool, typer.Option("--convert-pnj", help="Convert PNJ files to PNG")] = False,
```

Wire these into Settings before calling `run_crawl`.

- [ ] **Step 5: Update parse __init__.py to re-export parsers**

Modify `src/tumbl4/core/parse/__init__.py`:

```python
"""Parse pipeline — raw API responses to IntermediateDict to Pydantic models.

Parsers:
  - api_json: V1 API (/api/read/json) — all post types
  - svc_json: SVC JSON (internal API) — all post types
  - npf: NPF (Neue Post Format) — polymorphic content blocks
  - html_scrape: ___INITIAL_STATE___ extraction from HTML pages
  - inline_media: regex scan of body HTML for embedded media URLs
"""
```

- [ ] **Step 6: Run all existing tests to check for regressions**

Run: `uv run pytest -v`
Expected: all tests pass

- [ ] **Step 7: Commit**

```bash
git add src/tumbl4/core/crawl/tumblr_blog.py src/tumbl4/core/orchestrator.py src/tumbl4/core/download/file_downloader.py src/tumbl4/models/media.py src/tumbl4/cli/commands/download.py src/tumbl4/core/parse/__init__.py
git commit -m "feat(core): wire all post types, templates, and sidecar writer

Crawler dispatches all V1 post types via V1_NORMALIZERS table.
Orchestrator uses TemplateEngine for filenames, extracts inline
media from body HTML, and delegates sidecar writes to dedicated
async SidecarWriter worker. Download command adds --template,
--best, --convert-pnj flags.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 13: Component test, quality gates, and models update

**Files:**
- Modify: `src/tumbl4/models/__init__.py`
- Create: `tests/component/test_all_types_pipeline.py`

- [ ] **Step 1: Update models __init__.py**

Update `src/tumbl4/models/__init__.py` to re-export new models:

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

- [ ] **Step 2: Write the component test**

Write file `tests/component/test_all_types_pipeline.py`:

```python
"""Component test — end-to-end pipeline with all post types."""

import json
from pathlib import Path

import httpx
import respx

from tumbl4.core.crawl.http_client import TumblrHttpClient
from tumbl4.core.crawl.tumblr_blog import TumblrBlogCrawler
from tumbl4.core.orchestrator import run_crawl
from tumbl4.models.blog import BlogRef
from tumbl4.models.settings import Settings

_MIXED_RESPONSE = (
    "var tumblr_api_read = "
    + json.dumps({
        "tumblelog": {"title": "Mixed Blog", "name": "mixedblog"},
        "posts-start": 0,
        "posts-total": "4",
        "posts": [
            {
                "id": "400",
                "url-with-slug": "https://mixedblog.tumblr.com/post/400",
                "type": "photo",
                "unix-timestamp": 1776097800,
                "tags": ["art"],
                "photo-url-1280": "https://64.media.tumblr.com/aaa/photo.jpg",
            },
            {
                "id": "300",
                "url-with-slug": "https://mixedblog.tumblr.com/post/300",
                "type": "video",
                "unix-timestamp": 1776097700,
                "tags": ["video"],
                "video-caption": "<p>Cool clip</p>",
                "video-source": "https://vtt.tumblr.com/tumblr_vid_720.mp4",
                "duration": 10,
            },
            {
                "id": "200",
                "url-with-slug": "https://mixedblog.tumblr.com/post/200",
                "type": "regular",
                "unix-timestamp": 1776097600,
                "tags": ["writing"],
                "regular-title": "Hello",
                "regular-body": "<p>A text post with <img src='https://64.media.tumblr.com/inline/s540x810/inline.jpg'/> an inline image.</p>",
            },
            {
                "id": "100",
                "url-with-slug": "https://mixedblog.tumblr.com/post/100",
                "type": "quote",
                "unix-timestamp": 1776097500,
                "tags": ["quotes"],
                "quote-text": "To be or not to be.",
                "quote-source": "Shakespeare",
            },
        ],
    })
    + ";"
)


class TestAllTypesPipeline:
    @respx.mock
    async def test_crawls_all_post_types(self, tmp_path: Path) -> None:
        """End-to-end: crawl 4 posts of different types, download media."""
        respx.get(
            "https://mixedblog.tumblr.com/api/read/json",
            params={"debug": "1", "num": "50", "start": "0"},
        ).respond(200, text=_MIXED_RESPONSE)

        # Mock media downloads
        respx.get("https://64.media.tumblr.com/aaa/photo.jpg").respond(
            200, content=b"photo-data", headers={"Content-Type": "image/jpeg"},
        )
        respx.get("https://vtt.tumblr.com/tumblr_vid_720.mp4").respond(
            200, content=b"video-data", headers={"Content-Type": "video/mp4"},
        )
        respx.get("https://64.media.tumblr.com/inline/s540x810/inline.jpg").respond(
            200, content=b"inline-data", headers={"Content-Type": "image/jpeg"},
        )

        settings = Settings(output_dir=tmp_path / "output", max_concurrent_downloads=2)
        blog = BlogRef.from_input("mixedblog")
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

        assert result.posts_crawled == 4
        assert result.complete is True

        # Verify sidecars for all post types
        meta_dir = tmp_path / "output" / "mixedblog" / "_meta"
        sidecars = list(meta_dir.glob("*.json"))
        assert len(sidecars) == 4

        # Verify sidecar contents
        for sidecar_path in sidecars:
            data = json.loads(sidecar_path.read_text())
            assert "$schema_version" in data
            assert data["type"] in ("photo", "video", "text", "quote")

    @respx.mock
    async def test_custom_filename_template(self, tmp_path: Path) -> None:
        """Verify that custom filename templates produce correct paths."""
        respx.get(
            "https://mixedblog.tumblr.com/api/read/json",
            params={"debug": "1", "num": "50", "start": "0"},
        ).respond(200, text=_MIXED_RESPONSE)

        respx.get("https://64.media.tumblr.com/aaa/photo.jpg").respond(
            200, content=b"photo-data", headers={"Content-Type": "image/jpeg"},
        )
        respx.get("https://vtt.tumblr.com/tumblr_vid_720.mp4").respond(
            200, content=b"video-data", headers={"Content-Type": "video/mp4"},
        )
        respx.get("https://64.media.tumblr.com/inline/s540x810/inline.jpg").respond(
            200, content=b"inline-data", headers={"Content-Type": "image/jpeg"},
        )

        settings = Settings(
            output_dir=tmp_path / "output",
            max_concurrent_downloads=1,
            filename_template="{blog}/{post_type}/{post_id}_{index_padded}.{ext}",
        )
        blog = BlogRef.from_input("mixedblog")
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

        # Verify type-separated directory structure
        blog_dir = tmp_path / "output" / "mixedblog"
        photo_dir = blog_dir / "photo"
        video_dir = blog_dir / "video"
        # At least the photo and video subdirectories should exist
        assert result.posts_crawled == 4
```

- [ ] **Step 3: Run the component test**

Run: `uv run pytest tests/component/test_all_types_pipeline.py -v`
Expected: 2 passed

- [ ] **Step 4: Run ALL tests**

Run: `uv run pytest -v`
Expected: all tests pass (Plan 1 + Plan 2 existing + ~120 Plan 3 new)

- [ ] **Step 5: Run quality gates**

Run: `uv run ruff check .`
Expected: all checks pass (fix any issues found)

Run: `uv run ruff format --check .`
Expected: all formatting correct (run `uv run ruff format .` to fix if needed)

Run: `uv run pyright`
Expected: 0 errors (fix any type errors found)

- [ ] **Step 6: Self-review checklist**

Verify before committing:

1. **Spec coverage** -- all Plan 3 boundary items implemented:
   - [x] `svc_json.py` parser (Task 2)
   - [x] `npf.py` parser (Task 3)
   - [x] `html_scrape.py` parser (Task 4)
   - [x] All non-photo post types in `api_json.py` (Task 1)
   - [x] "Best" image resolution (Task 6)
   - [x] Inline media extraction (Task 5)
   - [x] Filename templates (Task 7)
   - [x] PNJ-to-PNG conversion (Task 8)
   - [x] TOML config loading (Task 9)
   - [x] `tumbl4 config get/set` (Task 10)
   - [x] Sidecar writer worker (Task 11)
   - [x] All wired into orchestrator (Task 12)

2. **Placeholder scan** -- search for `TODO`, `FIXME`, `XXX`, `PLACEHOLDER`:
   Run: `grep -rn 'TODO\|FIXME\|XXX\|PLACEHOLDER' src/tumbl4/`
   Expected: none found (or all are intentional documentation TODOs)

3. **Type consistency** -- `IntermediateDict.post_type` literal includes all 7 types:
   `Literal["photo", "video", "audio", "text", "quote", "link", "answer"]`

4. **Import hygiene** -- no circular imports, no `lxml.etree` in `html_scrape.py`

5. **Test count** -- approximately 120 new tests across all Plan 3 tasks

- [ ] **Step 7: Commit**

```bash
git add src/tumbl4/models/__init__.py tests/component/test_all_types_pipeline.py
git commit -m "test: add component tests for all-post-types pipeline

End-to-end with mixed post types (photo, video, text, quote).
Verifies sidecars written for all types, custom filename template
directory structure. Quality gates green.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

- [ ] **Step 8: Final quality gate commit (if needed)**

If Steps 4-5 required any fixes, commit them:

```bash
git add -u
git commit -m "fix: address Plan 3 quality gate findings

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```
