# tumbl4 — macOS/Linux CLI port of TumblThree (design)

**Status:** Draft v2 — incorporates security + implementability review
**Author:** Claire (with Claude assistance)
**Last updated:** 2026-04-11

## 1. Problem statement

TumblThree is a Windows WPF application for backing up Tumblr, Twitter, Bluesky, and NewTumbl blogs. It's ~52k LoC of C# targeting .NET Framework 4.7.2, with a tightly WPF-coupled UI layer and several Windows-only dependencies (WPF, WAF, `Microsoft.Web.WebView2`, `System.Windows.Forms`, `Autoupdater.NET.Official`). It cannot run on macOS or Linux today.

This project is a fork that delivers a **Python 3.12+ command-line tool** (`tumbl4`) running on macOS and Linux. It preserves the **behavior** of TumblThree's Tumblr crawler — the API quirks, parsing rules, resume semantics, filter logic — but replaces the implementation entirely and drops the GUI.

The v1 target is: **open-source, fully-functional, secure**. "Fully functional" means feature-complete for the committed v1 scope (not a toy). "Secure" is a first-class quality bar, not an afterthought.

## 2. Goals and non-goals

### v1 goals
- A CLI (`tumbl4`) installable on macOS and Linux via `uv tool install tumbl4` / `pipx install tumbl4` / `pip install tumbl4`
- Backup of public and hidden (dashboard/login-required) Tumblr blogs
- All post types: photo, video, audio, text, quote, link, answer
- Filters: original-vs-reblog, tag include/exclude, timespan
- Per-post metadata JSON sidecars
- Resume across runs with no duplicate downloads
- Configurable filename templates
- Cross-blog dedup via a shared index
- Playwright-driven interactive login (`tumbl4 login`) for hidden blogs
- Open-source under MIT, with CI, tests, contribution docs, versioned releases
- Signed release artifacts with SLSA provenance attestation

### Explicit non-goals for v1
- **No GUI** — may revisit in v2 if there's demand
- **No Twitter, Bluesky, or NewTumbl** — may add later
- **No likes / liked-by / search / tag-search crawlers** — defer to v2
- **No external-link downloaders** (imgur, gfycat, webmshare, uguu, catbox) — most are dead or dying
- **No password-protected (non-hidden) blog support** — defer
- **No Windows support in this fork** — upstream TumblThree still serves Windows users
- **No auto-updater** — users update via `uv tool upgrade tumbl4` or equivalent
- **No clipboard monitor, image viewer, scheduled start** — GUI-specific or out of scope
- **No bandwidth throttling in v1**, but the download implementation must use a streaming chunk loop that accommodates a future throttle wrapper without a rewrite
- **No localization / i18n** — English error messages for v1
- **No PyInstaller single-file binary for v1** — Playwright + PyInstaller is a known pain point; broken binaries hurt trust more than missing ones. Revisit in v2.

## 3. High-level architecture

```
                  ┌──────────────────────┐
                  │   tumbl4 CLI         │   (Typer + Rich)
                  └──────────┬───────────┘
                             │
                  ┌──────────▼───────────┐
                  │   tumbl4.core        │
                  │                      │
                  │  orchestrator        │   # thin state machine
                  │     │                │
                  │     ├── auth         │   # Playwright login + session
                  │     ├── crawl        │   # base + tumblr_blog + tumblr_hidden
                  │     │    └── http    │   # httpx client + per-endpoint rate limiters
                  │     ├── parse        │   # api/svc/npf: raw dict → IntermediateDict
                  │     ├── filter       │   # reblog, tag, timespan
                  │     ├── download     │   # file transfer + content-type reconcile + retries
                  │     ├── naming       │   # filename template engine
                  │     └── state        │   # SQLite, resume, dedup, metadata sidecars
                  └──────────────────────┘
                             │ uses
                  ┌──────────▼───────────┐
                  │   tumbl4.models      │   # Pydantic: Blog, Post, Media, Settings
                  └──────────────────────┘
```

### Core properties

1. **Everything async.** `httpx.AsyncClient`, `asyncio`, `aiofiles`. The workload is 95% I/O wait; async is the right shape.
2. **Orchestrator is a thin state machine.** It calls `auth → crawl → filter → download → state` as an ordered pipeline. It *never* touches raw HTTP responses, parses JSON, or constructs filenames. If it's tempted to, that logic belongs in the owning module.
3. **No global state.** All state passed explicitly via `CrawlContext` (defined below).
4. **Unidirectional dependencies** enforced by a ruff import rule: `cli → core.orchestrator → core.* → models`. Nothing imports upward.
5. **Pydantic for domain models, plain functions for wire parsing.** Raw `dict[str, Any]` exists only inside `parse/`; everything downstream sees typed models.
6. **CLI is the stable public API** (semver-gated from v1.0.0). `tumbl4.core` is documented but "unstable — may change" until third-party demand proves otherwise.

## 4. Project structure

```
TumblThreeMac/                           # repo root (may rename to tumbl4 later)
├── README.md                            # quickstart, install, basic usage
├── LICENSE                              # MIT (inherited from upstream)
├── CHANGELOG.md                         # Keep-a-Changelog format
├── CONTRIBUTING.md                      # dev setup, style, PR checklist, cassette workflow
├── CODE_OF_CONDUCT.md
├── pyproject.toml                       # hatchling, uv, ruff, pyright config
├── uv.lock                              # committed lockfile
├── .python-version                      # 3.12
├── .gitignore
├── .github/
│   ├── workflows/
│   │   ├── ci.yml                       # lint + type + test, macOS + Linux, py 3.11/3.12/3.13
│   │   └── release.yml                  # on tag v*: build + attest + PyPI + GH release
│   ├── ISSUE_TEMPLATE/{bug_report,feature_request}.md
│   └── pull_request_template.md
├── scripts/
│   └── scrub_cassettes.py               # fixture scrubber (strips auth before commit)
├── src/
│   └── tumbl4/
│       ├── __init__.py, __main__.py, py.typed
│       ├── cli/
│       │   ├── app.py                    # Typer app root + signal handler
│       │   ├── commands/
│       │   │   ├── download.py           # tumbl4 download <blog>
│       │   │   ├── login.py              # tumbl4 login (Playwright flow)
│       │   │   ├── logout.py             # tumbl4 logout (delete state + profile)
│       │   │   ├── list.py               # tumbl4 list (managed blogs)
│       │   │   ├── config.py             # tumbl4 config get/set
│       │   │   ├── status.py             # tumbl4 status <blog> (resume info)
│       │   │   └── sweep.py              # tumbl4 sweep <blog> (manual orphan cleanup)
│       │   └── output/
│       │       ├── progress.py           # Rich progress bars
│       │       ├── tables.py             # Rich tables
│       │       └── errors.py             # human-readable error formatting
│       ├── core/
│       │   ├── __init__.py               # public re-exports (marked unstable)
│       │   ├── context.py                # CrawlContext, CancelToken
│       │   ├── orchestrator.py           # state machine
│       │   ├── errors.py                 # exception taxonomy
│       │   ├── auth/
│       │   │   ├── playwright_login.py   # headed interactive login
│       │   │   ├── cookie_store.py       # persist/load state.json
│       │   │   └── session.py            # AuthSession, httpx cookie jar
│       │   ├── crawl/
│       │   │   ├── base.py               # BaseCrawler abstract
│       │   │   ├── tumblr_blog.py        # public crawl loop
│       │   │   ├── tumblr_hidden.py      # hidden/dashboard crawl loop (sibling)
│       │   │   ├── pinned_resolver.py    # dedicated upfront HTML fetch for pinned posts
│       │   │   └── http_client.py        # httpx + RateLimiters + redirect safety
│       │   ├── parse/
│       │   │   ├── intermediate.py       # IntermediateDict TypedDict + schema version
│       │   │   ├── api_json.py           # old API → IntermediateDict
│       │   │   ├── svc_json.py           # SVC → IntermediateDict
│       │   │   ├── npf.py                # NPF → IntermediateDict (polymorphic blocks)
│       │   │   └── html_scrape.py        # ___INITIAL_STATE___ extractor (HTML mode only)
│       │   ├── filter/
│       │   │   ├── reblog.py, tag.py, timespan.py
│       │   ├── download/
│       │   │   ├── file_downloader.py    # streaming chunk loop, throttle-ready
│       │   │   └── content_type.py       # mid-stream content-type reconciliation
│       │   ├── naming/
│       │   │   └── template.py           # filename template engine
│       │   └── state/
│       │       ├── db.py                 # SQLite schema + hand-rolled user_version migrations
│       │       ├── resume.py             # per-crawler-type cursor persistence + queries
│       │       ├── dedup.py              # cross-blog dedup index
│       │       ├── metadata.py           # JSON sidecar writer (atomic, path-guarded)
│       │       ├── sidecar_writer.py     # dedicated async worker draining the sidecar queue
│       │       ├── pending_posts.py      # in-flight post→media tracker (lock-guarded)
│       │       └── orphan_sweep.py       # .part cleanup (runs in to_thread)
│       ├── models/
│       │   ├── blog.py                   # Blog, BlogRef
│       │   ├── post.py                   # Post + post type variants
│       │   ├── media.py                  # Photo, Video, Audio
│       │   ├── settings.py               # Settings + nested configs
│       │   └── crawl_result.py           # CrawlResult, DownloadResult
│       └── _internal/
│           ├── logging.py                # structured logging + SecretFilter
│           ├── paths.py                  # XDG base dirs; state/config/cache locations
│           ├── sanitize.py               # control-char + bidi stripper for log output
│           ├── signal_handling.py        # SIGINT wiring
│           └── tasks.py                  # spawn() helper for supervised create_task
├── tests/
│   ├── unit/
│   │   ├── parse/                        # snapshot tests per format
│   │   ├── filter/                       # table-driven
│   │   ├── naming/                       # property-based (hypothesis)
│   │   ├── state/                        # in-memory SQLite
│   │   └── download/                     # respx HTTP mocks
│   ├── component/                        # orchestrator w/ fakes
│   ├── integration/                      # cassette-based end-to-end
│   │   ├── test_download_public_blog.py
│   │   ├── test_resume.py
│   │   ├── test_hidden_blog.py           # scrubbed logged-in cassette
│   │   ├── test_signal_handling.py
│   │   ├── test_redirect_safety.py       # ensures allowlist applies on every hop
│   │   └── test_pinned_post.py           # pinned post downloaded but excluded from highest_id
│   └── fixtures/
│       ├── cassettes/                    # pytest-recording, auth-scrubbed
│       └── json/                         # sample Tumblr responses per format
└── docs/
    ├── index.md                          # docs site root
    ├── installation.md
    ├── getting-started.md
    ├── authentication.md                 # Playwright login explained + security notes + headless workflow
    ├── configuration.md                  # config schema + precedence chain + env vars
    ├── filename-templates.md             # template syntax + variables + examples
    ├── commands/                         # command reference (generatable from Typer)
    ├── architecture.md                   # layer diagram for contributors
    ├── contributing.md                   # includes cassette recording workflow
    ├── security.md                       # threat model + accepted limitations
    └── superpowers/
        └── specs/                        # design docs (this file lives here)
```

**Notes on layout decisions:**

- **`src/` layout, not flat** — prevents accidental "imported the source tree instead of the installed package" bugs; standard with `hatchling` and `uv init --lib`.
- **`py.typed` marker** — we ship type hints; downstream users get IDE completion and type-checker support.
- **`_internal/`** — truly private helpers. Leading underscore signals "don't import from outside the package."
- **`tests/` is a sibling of `src/`**, not under it — standard src-layout convention, lets tests run against the installed package.
- **Existing C# codebase is deleted in this fork** — upstream `TumblThreeApp/TumblThree` still hosts the Windows app. Git history preserves everything for archaeology.

## 5. Data flow and runtime

### 5.1 Config precedence

Highest → lowest, merged at each layer (not last-write-wins):

1. CLI flags (`--output-dir`, `--tag`, `--timespan`, ...)
2. Environment variables (`TUMBL4_OUTPUT_DIR`, `TUMBL4_STATE_DIR`, `TUMBL4_LOG_LEVEL`, ...)
3. Project config: `./tumbl4.toml` if present in current working directory
4. User config: `$XDG_CONFIG_HOME/tumbl4/config.toml` (defaults to `~/.config/tumbl4/config.toml` on Linux, `~/Library/Application Support/tumbl4/config.toml` on macOS if the user hasn't set XDG)
5. Hardcoded defaults

`Settings` is a Pydantic `BaseSettings` model (from `pydantic-settings`). Each layer validates and merges via explicit precedence. A flag with a default value does NOT override a non-default env var — only an explicitly-passed flag does.

### 5.2 CrawlContext

Immutable frozen dataclass, threaded explicitly through the pipeline:

```python
@dataclass(frozen=True)
class CrawlContext:
    blog_ref: BlogRef                 # normalized target (name, type)
    settings: Settings                # fully merged config
    http: httpx.AsyncClient           # single shared client per crawl run
    rate_limiters: RateLimiters       # named buckets: api, svc, search, media
    auth: AuthSession                 # cookies + bearer token + metadata
    db: StateDb                       # per-blog SQLite connection
    dedup: DedupIndex                 # cross-blog dedup (details below)
    progress: ProgressReporter        # Rich-backed, disabled in --quiet
    cancel: CancelToken               # cooperative cancellation
```

### 5.3 Orchestrator state machine

```
START
  │
  ▼
authenticate()                    # load session, verify not-expired
  │
  ▼
(public crawler only) pinned_resolver.resolve_highest_id()
                                  # upfront HTML fetch to identify + skip pinned post
  │
  ▼
crawl.iterate_posts()             # async generator yielding Post
  │
  ▼
for each post:
  ├── filter.apply(post)          # drop if filters reject
  ├── for each media in post:
  │     └── media_queue.put(task) # bounded asyncio.Queue(maxsize=200)
  └── (do NOT write sidecar yet — sidecar is written on post completion)
  │
  ▼
(in parallel with crawl)
download_worker_pool (N workers, default 4)
  │
  ▼
for each MediaTask from queue:
  ├── rate_limiters.media.acquire()  # wrapped in shielded-cancellable wait
  ├── http.stream(GET url)           # via safe_redirect_client; every hop allowlist-checked
  ├── content_type.reconcile(response, task)  # may rename task.final_filename mid-stream
  ├── stream to {filename}.part     # chunk loop, throttle-ready
  ├── fsync + atomic rename to {filename}
  ├── state.record_download(url, hash, path)   # single SQLite txn
  └── progress.update()
  │
  ▼
for each post whose media are all resolved (success OR retries-exhausted):
  └── state.write_sidecar(post, media_results)
         # single txn: writes sidecar JSON atomically + marks post complete
  │
  ▼
crawl_done → drain_queue → persist_resume_cursor → END
```

**Queue semantics (was ambiguous in draft v1):**

- The **media queue** holds `MediaTask` objects (individual file download units), **not** `Post` objects. One post with 10 images yields 10 MediaTasks. This is the correct granularity for the worker pool.
- `maxsize=200` is a default; configurable via `settings.queue.max_pending_media`. At 200 items with 4 workers, the crawler gets at most ~50 items ahead of the download workers, which is enough to hide crawl latency without unbounded memory growth.
- A separate, smaller **sidecar queue** (`asyncio.Queue(maxsize=16)`) holds post-level `SidecarWriteTask` objects and is drained by **one dedicated sidecar writer worker**. This keeps media and metadata concurrency independent.
- **Sidecar writer worker lives at** `state/sidecar_writer.py`. It is started by the orchestrator via `tasks.spawn()`, tracked in the same supervised task set as download workers, and drains identically on cancel. A `SidecarWriteTask` is enqueued by a download worker after the last media in a post has resolved (success or retries-exhausted); the sidecar writer reads the task, writes the JSON sidecar atomically, and commits the post-complete marker in a single SQLite transaction.
- **Enqueue ordering note:** because sidecar writes depend on all media for a post being resolved, the orchestrator maintains a small `dict[post_id, set[MediaTask]]` of in-flight post media. When the last media for a post completes (from any download worker), that worker enqueues the sidecar task. This `dict` is the only shared state outside the context; it's guarded by an `asyncio.Lock` and lives in `state/pending_posts.py`.

### 5.4 HTTP client configuration

**Explicit timeouts** (was a real gap in draft v1):

```python
httpx.Timeout(
    connect=10.0,      # fast fail on dead hosts
    read=60.0,         # Tumblr media CDN can be slow
    write=30.0,
    pool=5.0,          # pool acquisition
)
```

With `max_connections=32`, `max_keepalive_connections=16`. `User-Agent` is explicit: `tumbl4/{version} (+https://github.com/<repo>)` — never blank, never the httpx default.

**Redirect safety (security-critical, see §6.3):** `httpx.AsyncClient(follow_redirects=False)`. Redirects are followed manually in `http_client.safe_follow_redirects()` which:
1. Re-checks the allowlist on every hop (not just the final URL)
2. Applies `max_redirects=5` (hard cap)
3. Performs DNS-resolved IP validation on each hop to block RFC-1918 / link-local / loopback (§6.3)
4. Raises `AllowlistViolation` on any failing hop, halting the crawl loudly

**Response body size cap:** API/SVC/NPF responses are read with `response.aread()` capped at `settings.http.max_api_response_bytes` (default: 32 MB). Over-cap responses raise `ResponseTooLarge` and skip the post. Media downloads use streaming and are NOT capped (large files are expected).

**Memory profile note:** the 32 MB cap is on the raw HTTP response body. After parsing, a large NPF response becomes a large `IntermediateDict`, including the `raw_content_blocks` field which preserves the full NPF block array for the sidecar. With 4 concurrent download workers each potentially holding an intermediate dict during per-post processing, the expected working-set is roughly `max_api_response_bytes × (worker_count + 1)` ≈ 160 MB in the worst case. This is the expected resident memory ceiling for v1; document it in `docs/architecture.md` so contributors aren't surprised by the memory profile on large blogs.

**Error handling discipline:** `httpx.HTTPStatusError` is never propagated as-is into logs — its `response.text` can carry session tokens in error bodies. The HTTP client layer catches `HTTPStatusError`, constructs an internal `CrawlError` subtype with only the status code and a `SecretFilter`-scrubbed body excerpt, and re-raises that. Raw `HTTPStatusError` is not allowed past `http_client.py`.

### 5.5 Per-endpoint rate limiters

```python
class RateLimiters:
    api: AsyncLimiter        # old Tumblr API     (default: 20 req / 10s)
    svc: AsyncLimiter        # SVC / mobile       (default: 30 req / 10s)
    search: AsyncLimiter     # search endpoints   (stub for v2)
    media: AsyncLimiter      # file fetches       (default: 8 concurrent)
```

All defaults configurable via `settings.rate_limits.*`. Implementation: `aiolimiter`.

**Cancellation + rate limiter interaction.** The earlier draft proposed cancelling a mid-flight `limiter.acquire()` via `asyncio.wait(FIRST_COMPLETED)`. The peer reviewer correctly flagged that this leaks limiter state — `aiolimiter` does not expose a clean cancellation path for a pending acquire, and cancelling mid-acquisition can leave a waiter permanently enqueued, reducing effective capacity on subsequent runs.

**Chosen pattern:** do not cancel mid-acquire. Workers check `cancel.is_cancelled()` **before** and **after** acquiring, but let the acquire itself complete uninterrupted:

```python
async def worker_step(task, limiter, cancel):
    if cancel.is_cancelled():
        return
    async with limiter:              # completes naturally; bounded by token replenish window
        if cancel.is_cancelled():
            return
        await do_download(task)
```

**Tradeoff acknowledged:** during SIGINT, a worker blocked on a heavily-throttled limiter may take up to one token-replenishment window (~10s in the worst default configuration) before it observes cancellation. This is an acceptable SIGINT latency ceiling given the correctness guarantee it preserves. A second SIGINT still triggers the immediate ungraceful exit path (§6.10), so users frustrated by the 10s wait have an escape hatch.

**Why not `asyncio.shield`?** `shield` protects an awaitable from cancellation but doesn't help here — the worker's cancellation-awareness is already cooperative via the `cancel.is_cancelled()` checks. `shield` would only matter if something above the worker was calling `task.cancel()`, which we explicitly do not do.

### 5.6 Parse pipeline (raw → IntermediateDict → Pydantic)

```
raw dict[str, Any]                                (from httpx)
         │
         ▼
  crawler passes (raw_dict, source_format)         # source hint, not sniffed
         │
         ▼
  parse.{api_json|svc_json|npf}.normalize()
         │                                         → IntermediateDict (§5.7)
         ▼
  models.Post.model_validate(intermediate)
         │                                         → Post subtype
         ▼
  orchestrator                                     (typed only)
```

**`format_detector` is removed.** The implementability review correctly pointed out that format is known at the call site — `TumblrBlogCrawler` calls the API endpoint (api_json); `TumblrHiddenCrawler` scrapes `___INITIAL_STATE___` (svc_json). The Python port uses the same knowledge: each crawler passes an explicit `source_format: Literal["api", "svc", "npf"]` to the parser. No content sniffing.

**NPF polymorphic content blocks:** modeled as `Annotated[Union[ImageBlock, TextBlock, VideoBlock, AudioBlock, LinkBlock, UnknownBlock], Discriminator("type")]`. The `UnknownBlock` variant is a catch-all — when Tumblr ships a new block type, we log a warning once per type per run and pass it through as unknown rather than crashing. Pydantic v2's `Discriminator` with a callable supports this pattern.

**On parse failure:** raise `ParseError` with a *control-character-stripped, bidi-stripped, truncated, `SecretFilter`-scrubbed* excerpt of the raw JSON for debugging (see §6.2 for the sanitize chain). Log at ERROR. Skip the current post. Do not halt the crawl.

### 5.7 IntermediateDict contract (was undefined — critical gap)

All parsers emit a dict conforming to this shape. `intermediate.py` defines it as a TypedDict with a schema version:

```python
class IntermediateDict(TypedDict):
    schema_version: int                   # currently 1; incremented on breaking shape changes
    source_format: Literal["api", "svc", "npf"]
    post_id: str                          # always string; upstream may return int
    blog_name: str                        # "example" (no .tumblr.com)
    post_url: str                         # canonical URL
    post_type: Literal["photo", "video", "audio", "text", "quote", "link", "answer"]
    timestamp_utc: str                    # ISO8601 Z
    tags: list[str]                       # always list; upstream may return null/missing
    is_reblog: bool
    reblog_source: ReblogSource | None    # {"blog_name": str, "post_id": str} | None
    title: str | None
    body_text: str | None                 # plain text extracted from NPF/HTML/markdown
    body_html: str | None                 # original HTML if the format provides it
    media: list[MediaEntry]               # normalized media list
    raw_content_blocks: list[dict] | None # NPF-only: preserved block array for sidecar
```

```python
class MediaEntry(TypedDict):
    kind: Literal["photo", "video", "audio"]
    url: str                              # direct media URL (pre-redirect)
    width: int | None
    height: int | None
    mime_type: str | None                 # if the format provides it
    alt_text: str | None                  # for photos
    duration_ms: int | None               # for video/audio
```

```python
class ReblogSource(TypedDict):
    blog_name: str
    post_id: str
```

**Parser responsibilities:** convert whatever the wire format gives you into this shape. If a field is missing from a particular format, set it to `None` (for Optional fields) or the neutral empty value (`[]` for lists, `False` for booleans). Never leak format-specific field names past the parser boundary.

**Test contract:** each parser has at least one snapshot test per post type that asserts the emitted `IntermediateDict` matches a committed fixture. A parser change that breaks an existing snapshot fails CI until the snapshot is explicitly updated.

### 5.8 Public crawler pagination and pinned posts

The C# `TumblrBlogCrawler.GetHighestPostIdCoreAsync` does a separate upfront HTML fetch, extracts `___INITIAL_STATE___` to identify the pinned post ID, then **skips that pinned post** when determining `highest_post_id`. Pinned posts can be months or years older than the blog's newest post; without this step, the resume cursor would be set to the pinned post's ancient ID, causing a full re-crawl on every run.

The Python port preserves this:

- `crawl/pinned_resolver.py`: on first use of a blog in a session, fetches the blog's HTML front page, extracts `___INITIAL_STATE___` (see §5.10), identifies the pinned post ID (if any), and stores it in `CrawlContext` as `blog_ref.pinned_post_id`.
- During pagination, the crawler skips the pinned post when it appears in API results for the purpose of `highest_post_id` calculation. The pinned post itself is still downloaded — it's only excluded from the "newest post" computation.
- **Public crawler's resume cursor is `(highest_post_id, before_id)`** — not an offset. `highest_post_id` is the newest-post fence for this blog (what we've seen before); `before_id` is the current pagination fence for the in-progress crawl. On resume:
  1. Re-fetch the blog's HTML front page via `pinned_resolver`, get the current real newest post ID
  2. Compare to stored `highest_post_id` — any gap is new content since last run
  3. Restart the API walk from the current newest post with `before_id = stored_highest_post_id` as the stop fence, and walk backward via the API's native `before_id` parameter (not an integer offset). **Never replay a stored offset** — a stored offset is invalid if the blog published new posts between runs, since the same offset now points to different posts and would cause duplicate/missed work.
  4. On crawl completion, update `highest_post_id` to the newly-resolved current newest post ID
- **Hidden crawler's resume cursor is `(highest_id, latest_post_id, next_link)`** matching SVC's pagination model (`TumblrHiddenCrawler.cs`'s `highestId` + `latestPost` + internal `nextLink` cursor chain). The hidden crawler's `next_link` is an opaque token from the previous SVC response, not an offset.
- Cursors are opaque to the orchestrator — each crawler owns its own cursor encoding and (de)serialization via `resume.py` helpers keyed on `crawler_type`.

**Cursor schema version** in the `crawl_state` table allows breaking cursor-format changes; on mismatch, the cursor is reset and the blog re-crawls from scratch (safe because dedup catches duplicates).

The cursor is persisted every `settings.crawl.cursor_flush_interval` posts (default: 20). Worst case crash recovery re-crawls 20 posts — dedup ensures no duplicate writes.

### 5.9 Hidden crawler: two-shape `___INITIAL_STATE___` extraction

`AbstractTumblrCrawler.cs` uses two distinct regexes for `___INITIAL_STATE___` extraction (a single-line form and a multi-line form), in try-first-then-fallback order. The `___INITIAL_STATE___` JSON also has two possible shapes:

- **`PeeprRoute` shape:** used on `tumblr.com/dashboard/blog/<blog>` pages
- **Response-wrapped shape:** used on `tumblr.com/blog/<blog>` pages

Each shape has a different path to the posts array and a different `nextLink` cursor location.

`parse/html_scrape.py` encodes both regexes and both shapes. It tries the single-line regex first, falls back to the multi-line regex, then identifies the shape by presence of `PeeprRoute` vs. `response` root keys, and returns a uniform `{posts: [...], next_cursor: str | None}` structure. Unit tests cover all four combinations (2 regexes × 2 shapes).

**Error paths for shape detection:**
- **Neither regex matches:** raise `ParseError` with an excerpt of the HTML body (sanitized via §6.2's sanitize chain). Typical cause: Tumblr ships a redesign. Logged at ERROR, post skipped, crawl continues. Repeated identical errors triggers an "extractor may be out of date" warning once per run.
- **Both `PeeprRoute` and `response` keys present (version drift):** prefer `PeeprRoute` (the newer shape) and log a WARNING once per run. Do not treat as fatal — Tumblr's drift is graceful more often than not.
- **Neither shape key present** (the JSON parsed but structure is unknown): raise `ParseError`, same handling as neither-regex-matches.

`html_scrape.py` uses **`lxml.html.fromstring`** (HTML parser mode) or `html.parser` — never `lxml.etree.parse` (XML mode), which resolves external entities and is an XXE risk on attacker-controlled content. This is a hard rule enforced by a ruff ban on `lxml.etree` imports in `html_scrape.py`.

### 5.10 Dedup index

- **Per-blog dedup:** stored in the blog's own SQLite database. Key = SHA-256 of **initial-request URL** (the URL as parsed from the post, before any redirect following).
- **Cross-blog dedup:** stored in a shared SQLite database at `$XDG_DATA_HOME/tumbl4/dedup.db` (default: `~/.local/share/tumbl4/dedup.db` on Linux, `~/Library/Application Support/tumbl4/dedup.db` on macOS). Same URL key plus a `first_seen_blog_id` pointer.
- **Loading:** **not** loaded into a Python set at startup. Queries go directly to SQLite; the URL column is indexed; lookup is O(log n).
- **When dedup runs in the pipeline:** between filter and media-queue enqueue. A post that passes filters has its media URLs checked against dedup; already-downloaded media are skipped but the post's metadata sidecar is still written (reblog tracking).
- **Opt-out:** `--no-dedup` bypasses **cross-blog** dedup only. **Per-blog dedup always runs** — the per-blog case has no legitimate opt-out (you never want to re-download the same file inside the same blog's archive; it's always an error). Cross-blog dedup is opt-outable because some users want independent per-blog archives where every blog has its own copy of reblogged content. Documented in `docs/configuration.md`.

### 5.11 Download module: streaming chunk loop and content-type reconciliation

**Streaming chunk loop.** `file_downloader.py` reads the response body in chunks and writes each chunk to the `.part` file. The chunk-loop structure is required so that bandwidth throttling (v2) can be added as a wrapper around the chunk iterator without refactoring:

```python
async with http.stream("GET", url) as response:
    content_type.reconcile(response, task)  # may update task.final_filename
    async with aiofiles.open(task.part_path, "wb") as f:
        async for chunk in response.aiter_bytes(chunk_size=64 * 1024):
            if cancel.is_cancelled():
                raise asyncio.CancelledError()
            await f.write(chunk)
            # v2 throttle hook: await throttle.consume(len(chunk))
```

**Content-type reconciliation (was missing from the draft).** The C# `FileDownloader` renames the destination file mid-download if the response `Content-Type` doesn't match the URL's apparent extension (e.g., a `.png` URL that returns `Content-Type: image/jpeg`). The Python port does the same:

1. Before writing the first byte of the `.part` file, inspect `response.headers["content-type"]`.
2. If the declared MIME type maps to an extension different from the one implied by the URL, update `task.final_filename` to use the correct extension.
3. Record both the original URL extension and the resolved extension in the download metadata for debugging.

This must happen **before** any write — once the `.part` file is opened under a filename, renaming mid-write is more error-prone than delaying the open.

### 5.12 Resume semantics (definition of "post done")

A post is considered **complete** when its sidecar is written. The sidecar is written after **all** of the post's media have either succeeded or exhausted retries:

- Success: media downloaded and committed to state
- Retries exhausted: media recorded as `failed` in `downloads` table; sidecar still lists the media entry with `status: "failed"` and no local filename

**Resume cursor advances past a post only when the sidecar is committed.** This guarantees that a half-downloaded post is re-processed on the next run:

- Crash mid-download: `.part` file exists, cursor still points to the post's page, no sidecar. Orphan sweep deletes `.part`, dedup lets the not-yet-committed URL through, crawler re-visits the page, post is reprocessed.
- Crash after all media committed but before sidecar: cursor still points to the post's page. On rerun, dedup catches the already-downloaded media (they're in the downloads table), sidecar is written from state, post advances.

**Tradeoff acknowledged:** on a crash, the last post whose media were fully committed but whose sidecar wasn't yet written will have its media dedup-skipped and its sidecar written on the next run. This is correct. The cost is O(1) per crash.

### 5.13 Orphaned `.part` file cleanup

On crawl startup, before any downloads begin, `orphan_sweep.py` runs in `asyncio.to_thread()` (so it doesn't block the event loop on slow filesystems):

1. Scan `{output_dir}` recursively for `*.part` files.
2. Scan is bounded at `settings.startup_sweep.max_files` (default: 10_000). Above that, emit a warning and skip — user can run `tumbl4 sweep <blog>` explicitly.
3. For each `.part`, delete it unconditionally. The downloader will re-fetch on the next crawl run.
4. Sweep is also triggered after a completed crawl to clean up any in-flight `.part` files that got stranded.

### 5.14 Unicode and filename safety

- `unicodedata.normalize("NFC", s)` on every filename template output before path construction
- Component length enforced in **bytes** (UTF-8), 255 max — not characters
- Dual NFC-vs-NFD existence check on resume lookups (macOS APFS silently decomposes)
- Forbidden characters substituted with `_`: `/ \ : \0 * ? < > | "`
- POSIX-reserved names blocked: `.`, `..`, empty string
- **Filename template shape validated at config load time**, not per-post render time. `ConfigError` raised immediately if the template can produce invalid filenames (absolute paths, `..` escapes, reserved words)
- **Per-post path validation is a mandatory runtime assertion** (§6.4), not just a test invariant: every rendered path is passed through `is_relative_to(output_root.resolve())` before any `open()` call. A post whose data produces an escape raises `WriteFailed` at runtime, halting the crawl.

### 5.15 Filename template engine

`naming/template.py` uses Python's `str.format_map` with a custom `Formatter` that supports these variables:

| Variable | Type | Source | Example |
|---|---|---|---|
| `{blog}` | str | `IntermediateDict.blog_name` | `example` |
| `{post_id}` | str | `IntermediateDict.post_id` | `12345678` |
| `{post_type}` | str | `IntermediateDict.post_type` | `photo` |
| `{date}` | date | parsed `timestamp_utc` | `2026-04-11` |
| `{datetime}` | datetime | parsed `timestamp_utc` | `2026-04-11_14-22-03` |
| `{year}`, `{month}`, `{day}` | str | parsed `timestamp_utc` | `2026`, `04`, `11` |
| `{tag}` | str | first tag, or `_untagged_` if none | `art` |
| `{tags}` | str | all tags joined by `_`, or `_untagged_` if none | `art_wip` |
| `{index}` | int | media index within post (1-based) | `1`, `2`, `3` |
| `{index_padded}` | str | zero-padded to post's media count width | `01` of `15` |
| `{ext}` | str | resolved file extension (after content-type reconciliation) | `jpg` |
| `{hash8}` | str | first 8 hex chars of URL SHA-256 | `a1b2c3d4` |

All variables are sanitized via `_sanitize_path_component()` (NFC, forbidden-char replacement, byte-length truncation).

**Default template:** `{blog}/{post_id}_{index_padded}.{ext}`

**Example custom templates:**
- `{blog}/{year}/{month}/{post_id}_{index}.{ext}` — year/month folder hierarchy
- `{blog}/{post_type}/{date}_{post_id}_{hash8}.{ext}` — type-separated with hash suffix
- `{blog}/{tag}/{post_id}_{index}.{ext}` — tag-folder organization

Templates with no `{post_id}` **or** `{hash8}` fail config validation — without one of these, non-unique filenames are possible.

**Empty-value substitution:** `{tag}` and `{tags}` both produce the literal string `_untagged_` when a post has no tags (rather than leaving an empty path component, which is blocked by §5.14's reserved-name rule). Config validation warns if the user's template contains `{tag}` or `{tags}` — because any untagged post will land in the `_untagged_` bucket, which may surprise users expecting only tagged posts in the output.

### 5.16 Filter module contracts

Each filter is a pure function `(post: Post, config: FilterConfig) -> bool` returning True to keep, False to drop.

**`reblog.py`:**
- `include_reblogs: bool` (default: True) — if False, drop all reblogs
- `include_originals: bool` (default: True) — if False, drop all originals
- Both False is a `ConfigError` at load time
- Both True is a no-op

**`tag.py`:**
- `include_tags: list[str]` — post kept if ANY tag in the post's tag list matches (OR semantics); empty list = don't filter on tags
- `exclude_tags: list[str]` — post dropped if ANY tag in the post's tag list matches (OR semantics); empty list = no exclusions
- Include and exclude are applied independently and a post must satisfy BOTH (must have an included tag AND must not have an excluded tag)
- Tag matching is case-insensitive and NFC-normalized on both sides

**`timespan.py`:**
- `since: datetime | None` — post kept if its `timestamp_utc` >= since
- `until: datetime | None` — post kept if its `timestamp_utc` <= until
- Filters on **post creation date** (`timestamp_utc`), not reblog date
- Both None = no filter; both set = inclusive-inclusive range

### 5.17 Retry policy (unit: per-MediaTask)

- **Transient errors** (`RateLimited`, `ServerError`, `WriteFailed`, `HashMismatch`): exponential backoff with jitter. Max 5 retries per MediaTask. Base 1s, factor 2, max 60s.
- **429 specifically:** respect `Retry-After` header verbatim when present.
- **Retries are per-MediaTask**, not per-post or per-crawl. After a MediaTask exhausts retries, it's recorded as `failed` in the `downloads` table with no local filename. The post's sidecar is still written with a `"status": "failed"` entry for that media. The post advances normally.
- **Terminal errors:** logged, crawl skips the offending post or halts depending on severity. **Halt:** `DiskFull`, `WriteFailed`, `SessionExpired`, `AllowlistViolation`, `BlogRequiresLogin` (prompts the user to `tumbl4 login` and retry). **Skip-post and continue:** `ParseError`. **Halt-this-blog but continue any other queued blogs (v2):** `BlogNotFound`.

### 5.18 Metadata sidecar format

Each completed post writes a JSON sidecar:

- **Location:** `{output_dir}/_meta/{sanitized_post_id}.json` (underscored subdirectory so it doesn't mix with media files)
- **`sanitized_post_id`**: `post_id` is from `IntermediateDict` (Tumblr-controlled content) and is sanitized with the same rules as filename template components (§5.14) — NFC normalization, forbidden-char replacement, byte-length cap, POSIX-reserved-name block. Applied in `state/metadata.py` before path construction, independent of the template engine.
- **Runtime `is_relative_to` guard**: `metadata.py` performs the same `rendered_path.resolve().is_relative_to(output_root.resolve())` check as `file_downloader.py` before calling `open()`. Violation raises `WriteFailed`, halting the crawl. This is a hard runtime assertion, not just test coverage.
- **Permissions:** **`0600`** on creation (may contain sensitive post content like private dashboard posts)
- **Schema:**
  ```json
  {
    "$schema_version": 1,
    "blog": "example.tumblr.com",
    "post_id": "12345678",
    "post_url": "https://example.tumblr.com/post/12345678",
    "type": "photo",
    "timestamp_utc": "2026-04-11T14:22:03Z",
    "tags": ["art", "wip"],
    "is_reblog": false,
    "reblog_source": null,
    "title": "...",
    "body_text": "...",
    "body_html": "...",
    "media": [
      {
        "filename": "12345678_01.jpg",
        "url": "https://64.media.tumblr.com/...",
        "sha256": "...",
        "bytes": 234567,
        "status": "success"
      },
      {
        "filename": null,
        "url": "https://64.media.tumblr.com/...",
        "status": "failed",
        "error": "HTTP 404 after 5 retries"
      }
    ]
  }
  ```
- `$schema_version` field so future sidecar format changes can be migrated
- Written atomically via `.part` + rename, same as media files
- Written as a single SQLite transaction with the post-complete marker

## 6. Error handling and security

### 6.1 Exception taxonomy

All exceptions inherit from `tumbl4.core.errors.Tumbl4Error`:

```
Tumbl4Error
├── ConfigError                    # config file bad, filename template invalid
├── AuthError
│   ├── SessionExpired             # 401/403 on authenticated request
│   ├── LoginFailed                # Playwright flow failed
│   └── NoDisplay                  # headless env can't run interactive login
├── CrawlError
│   ├── RateLimited                # 429 — transient, back off
│   ├── ServerError                # 5xx — transient, back off
│   ├── BlogNotFound               # 404 — terminal for this blog
│   ├── BlogRequiresLogin          # public crawl hit a hidden blog — prompt to login
│   ├── ResponseTooLarge           # response body exceeded max_api_response_bytes
│   └── ParseError                 # parser failure — skip post
├── DownloadError
│   ├── DiskFull                   # ENOSPC — halt crawl
│   ├── WriteFailed                # other OS error on write — halt crawl
│   ├── HashMismatch               # content hash didn't match — retry
│   └── AllowlistViolation         # URL not in allowed domains — halt crawl
└── StateError                     # SQLite error, migration failure, schema mismatch
```

### 6.2 Logging hygiene (expanded to cover tracebacks and terminal injection)

Structured logging via `structlog` with a JSON renderer in CI and a human-readable renderer in the terminal.

**`SecretFilter`** is installed on the root logger. It intercepts *three* content paths:

1. **Formatted message strings:** regex matches on known Tumblr cookie patterns (`tumblr_b=...`, `tumblr_[a-z_]+=<value>`), bearer tokens (`Bearer <token>`), and generic `Cookie: ...` / `Authorization: ...` headers.
2. **Structured `extra` dict fields:** any key matching case-insensitive `{cookie, cookies, token, session, authorization, auth, secret, password, bearer, api_key, headers}` has its value replaced with `[REDACTED]`. Nested dicts are walked recursively.
3. **Exception traceback bodies** (`record.exc_text`): after `logging.Formatter.formatException` runs, `SecretFilter.filter()` scrubs the formatted traceback string with the same regex set. Additionally, before `logger.exception()` is called, the `tumbl4._internal.tasks.spawn()` helper (which supervises all `create_task` calls) clears locals from frames via `traceback.clear_frames(tb)` on exceptions, removing `AuthSession` objects from the traceback locals entirely.

**Terminal injection mitigation:** content that reaches the terminal (Rich progress messages, error messages, `ParseError` excerpts) is passed through `_internal.sanitize.for_terminal()` which:
- Strips all Unicode characters with `Cc` (control), `Cf` (format — includes bidi overrides `U+202E`, `U+202D`, etc.), and `Cs` (surrogate) general categories
- Replaces with `\u{hex}` printable escape notation so evidence is preserved but harmless
- Is applied to: log messages containing external content, `ParseError` excerpt snippets, filename displays, sidecar body_text previews in error output

**Test harness:** a `caplog` fixture re-runs the filter chain and asserts no known-secret pattern and no raw bidi/control char makes it to the output. Tests cover: formatted messages, structured extras, exception tracebacks with `AuthSession` in locals, and terminal output with crafted bidi-override inputs.

### 6.3 HTTPS, URL allowlist, redirect safety, SSRF

- `httpx.AsyncClient(verify=True, follow_redirects=False)` always. No `--insecure` flag in v1. Manual redirect follower in `http_client.safe_follow_redirects()`.
- **Allowlist is a suffix match on `*.tumblr.com`**, with explicit exceptions for any non-`tumblr.com` CDN hosts we discover (none are currently required).
- **Allowlist is checked on every hop of the redirect chain**, not just the final URL. A redirect off `*.tumblr.com` is an `AllowlistViolation` and halts the crawl loudly.
- **SSRF guard via IP-level validation:** a custom httpx transport resolves the hostname, and rejects the request if the resolved IP is:
  - RFC 1918 (`10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`)
  - Loopback (`127.0.0.0/8`, `::1`)
  - Link-local (`169.254.0.0/16`, `fe80::/10`) — blocks cloud IMDS endpoints
  - RFC 5735 special-use ranges
- IP validation applies to every redirect hop, not just the initial request.
- The crawler **never** silently drops URLs that fail the allowlist — every miss is an `AllowlistViolation` halt. Silent drop would be invisible data loss.
- **`test_redirect_safety.py`** covers: (a) redirect to off-domain attacker.com → allowlist violation; (b) redirect to `169.254.169.254` (IMDS) → IP violation; (c) redirect chain exceeding `max_redirects=5` → error; (d) legitimate Tumblr CDN redirect chain → success.

### 6.4 Path traversal and filename safety (hardened)

- Filename template **shape validation** at config load time, raising `ConfigError` before any crawl starts. Validator renders a dozen synthetic posts with unusual characters and verifies all outputs stay within the output root.
- **Per-post rendering includes a mandatory runtime guard:** `rendered_path.resolve().is_relative_to(output_root.resolve())` is checked before every `open()` call. Violation raises `WriteFailed` (treated as halt-class). This is enforced in `file_downloader.py` and `metadata.py` at the actual file-opening call sites, not just in tests.
- **Attacker-controlled input consideration:** template variables (`{title}`, `{tags}`, `{body_text}`) are populated from Tumblr-controlled content. A malicious post title of `../../../.bashrc` is neutralized by (a) sanitization of path separators at template render time, and (b) the runtime `is_relative_to` guard as a backstop. Both are active; either one alone is insufficient.
- **TOCTOU note:** the `.resolve()` check happens before `open()`, but between resolve and open, a symlink substitution is theoretically possible on a world-writable output directory. We document this as a known limitation — `tumbl4` warns on startup if the output directory is world-writable (`stat.S_IWOTH`) and recommends against it.

### 6.5 SQLite safety

- **Parameterized queries only.** No string-formatted SQL. Enforced by a ruff rule banning `execute(f"...")` and `execute("%s" % ...)` patterns.
- **SQLite file permissions: `0600`** (both per-blog and cross-blog dedup files).
- **WAL mode** for crash safety. `PRAGMA journal_mode=WAL` + `PRAGMA synchronous=NORMAL` — tradeoff: may lose the last ~few transactions on power loss. Acceptable because the resume cursor flushes at most every 20 posts, limiting lost work to the cursor-window.
- **Schema migrations** via a hand-rolled migration runner keyed on `PRAGMA user_version`. Alembic has significant setup cost and dependency weight for a single-table-dominant SQLite schema; a minimal runner (~50 LoC in `state/db.py`) handles the v1 migration needs cleanly. Each migration is a numbered SQL file in `src/tumbl4/core/state/migrations/NNNN_description.sql`; the runner compares `PRAGMA user_version` against the highest-numbered migration on load, applies pending migrations in order, and bumps `user_version`. Atomic per migration.

### 6.6 Credential and session storage

- **Playwright state.json:** `$XDG_STATE_HOME/tumbl4/playwright_state.json` (default: `~/.local/state/tumbl4/playwright_state.json` on Linux, `~/Library/Application Support/tumbl4/state/playwright_state.json` on macOS). File permissions **`0600`**. On load, if the file is found with broader permissions, `tumbl4` **refuses to run** until the user re-secures it (or passes `--i-know-its-insecure`, which logs a warning).
- **Playwright user_data_dir:** pinned under `$XDG_STATE_HOME/tumbl4/browser_profile/`, directory permissions **`0700`**. Never use system `/tmp` for the browser profile.
- **`tumbl4 login` always starts fresh:** deletes and recreates `browser_profile/` at the start of every login flow. A partial or killed-before-logout session does not leave a lingering profile with stale auth state. Unit-tested: `test_login_fresh_profile.py`.
- **`tumbl4 logout`:** deletes (not clears) both `playwright_state.json` and `browser_profile/`. After file deletion, it explicitly `del`s the in-memory `AuthSession` and calls `gc.collect()` to reduce the window during which credentials remain in heap memory.
- **Threat model: accepted limitations** (documented in `docs/security.md`):
  - Decrypted state is held in memory while the CLI runs; a user with `ptrace` access to the process can read cookies. Inherent to local CLI execution.
  - On macOS without FileVault 2 active, paged-out memory (swap) is unencrypted; cookies may be recoverable from a machine after power-off until swap overwrite. Documented; not a code fix.
  - Malicious code already running as the same user is out of scope.
- **Sidecar file permissions: `0600`** on creation. Sidecars contain post content (title, body, tags) that may be sensitive for private/dashboard posts.

### 6.7 Headless environment handling

- On `tumbl4 login`, if `$DISPLAY` and `$WAYLAND_DISPLAY` are both unset (SSH, headless server), raise `NoDisplay` with:
  ```
  tumbl4 login requires a graphical environment. Options:
    1. Log in on a local machine, then copy playwright_state.json
       to $XDG_STATE_HOME/tumbl4/ (chmod 0600)
    2. Use X11 forwarding over SSH: ssh -X, then tumbl4 login
  ```
- Do **not** silently fall back to `headless=True` — the CAPTCHA/2FA UX is worse that way, and users need explicit consent to that mode.

### 6.8 Playwright sandboxing

- Chromium runs with its default sandbox.
- **Never pass `--no-sandbox`** even if it seems convenient on Linux CI — use `xvfb-run` instead for display emulation.
- Playwright CI step installs browsers via `playwright install chromium --with-deps`, cached on `~/.cache/ms-playwright` keyed on the Playwright package version.

### 6.9 Dependency security and supply chain

- `pip-audit` runs in CI on every PR and on a scheduled weekly job.
- Dependabot enabled on `pyproject.toml` for version updates and security alerts.
- `uv.lock` is committed; lockfile diffs are part of code review.
- CI fails on any CVE above LOW severity in direct or transitive dependencies.
- **Release artifacts are signed with SLSA provenance attestations** via `actions/attest-build-provenance` in `release.yml`. Each published wheel and sdist has a verifiable attestation linking the artifact to the git commit that built it.
- **PyPI publishing uses OIDC trusted publisher** — no stored API tokens.

### 6.10 Cancellation and signal handling

**`CancelToken` implementation** — uses a `threading.Event` as the ground truth, with a small async bridge for coroutines that need to `await` cancellation. The prior draft had a two-field (`_loop` + `_event`) pattern with a race where `cancel_from_signal` could silently no-op if it arrived mid-initialization. This version eliminates that race because `threading.Event` is always safe to call from any thread including a signal handler, and the async bridge is one-way (async-side observes the threading event).

```python
import asyncio
import threading

class CancelToken:
    """Cooperative cancellation primitive safe across signal handlers and the asyncio loop.

    - `cancel()` and `cancel_from_signal()` are both safe from any thread
      (including signal handler context) because they operate on a plain
      threading.Event, which has no loop dependency.
    - `wait()` is safe to call from asyncio code; it bridges to the threading
      event via a polling loop that yields to the event loop.
    - `is_cancelled()` is a non-blocking check, safe from any context.
    """

    _POLL_INTERVAL_SECONDS = 0.1   # trade-off: 100ms cancel latency for simple impl

    def __init__(self) -> None:
        self._event = threading.Event()

    def cancel(self) -> None:
        """Set the cancel flag. Safe from any thread."""
        self._event.set()

    def cancel_from_signal(self) -> None:
        """Alias for cancel(). Explicit name for signal handler call sites."""
        self._event.set()

    def is_cancelled(self) -> bool:
        return self._event.is_set()

    async def wait(self) -> None:
        """Wait for cancellation from async code."""
        while not self._event.is_set():
            await asyncio.sleep(self._POLL_INTERVAL_SECONDS)
```

**Why polling instead of a fancier bridge:** the alternative is an `asyncio.Event` with a `threading.Thread` watcher or `loop.call_soon_threadsafe` — both add complexity and the original race. Polling at 100ms is simple, correct, and the cancellation latency is invisible in practice (SIGINT response is already bounded below by worker task granularity).

**`SIGINT` handler** (installed in `cli/app.py` via `signal.signal`) calls `cancel_token.cancel_from_signal()`. This is a simple `threading.Event.set()` call — no loop bridging required, no race.

**On cancel:**
1. Orchestrator stops enqueuing new crawl work
2. The crawler coroutine finishes parsing the page currently in flight, does not fetch the next page, and exits its generator
3. In-flight download workers:
   - If blocked on `rate_limiters.media.acquire()`, the acquire completes naturally (bounded by the rate-limiter's replenishment window, ~10s worst case — see §5.5 for the tradeoff). After acquire, the post-check observes cancel and returns without starting the download.
   - If mid-HTTP-stream, the chunk loop checks `cancel.is_cancelled()` between chunks and cancels the stream cleanly
4. Orchestrator drains the queue (no new work; remaining workers finish their current task or abort mid-chunk)
5. Final state commit: resume cursor, pending sidecar writes
6. SQLite WAL checkpoint + close
7. Exit code 130 (standard SIGINT)

Second `SIGINT` within 3 seconds → immediate ungraceful exit with warning that resume state may be inconsistent.

### 6.11 Async exception hygiene

- `loop.set_exception_handler(handler)` installed in `cli/app.py`. The handler logs unhandled task exceptions at ERROR with the full traceback (scrubbed by `SecretFilter`).
- **Every `asyncio.create_task(coro)` call goes through the helper `tumbl4._internal.tasks.spawn(coro)`** which:
  - Creates the task
  - Adds the task to a tracked `set[Task]` (so it's not garbage-collected mid-run)
  - Attaches a done-callback that removes it from the set and logs any exception
- Ruff rule `RUF006` enforces "asyncio-dangling-task" at lint time — raw `asyncio.create_task` is banned in favor of `spawn`.

### 6.12 Disk-full detection

Every file write uses `.part` + atomic `os.rename`:

```python
try:
    await aiofiles.os.rename(part_path, final_path)
except OSError as e:
    if e.errno == errno.ENOSPC:
        raise DiskFull() from e
    raise WriteFailed() from e
```

**On `DiskFull`:** the orchestrator halts cleanly. Halt sequence:
1. Download workers stop accepting new MediaTasks (stop draining the media queue)
2. Any in-flight sidecar writes in the sidecar queue are **attempted best-effort** — sidecars are small (typically <10 KB) and may succeed even when media writes fail. Successes are recorded in state; failures are logged and treated as "sidecar missing" on next resume.
3. Resume cursor is persisted with the last fully-completed post
4. SQLite WAL checkpoint + close
5. Report to user: how many files succeeded, how many MediaTasks remain, and the disk free space at the output path
6. Exit with error code 28 (standard `ENOSPC` exit convention from `sysexits.h` doesn't exist, but 28 is widely used for disk-full in Unix tools)

**On `WriteFailed`:** same halt sequence, different exit code (1 = general error). `WriteFailed` includes anything that isn't `ENOSPC` — EIO, EACCES, etc. Not retried, because these conditions rarely self-heal during a crawl.

## 7. Testing strategy

### 7.1 Test pyramid

```
         integration (cassette-based end-to-end)   ~15 tests
                   ▲
         component (orchestrator + fakes)           ~40 tests
                   ▲
         unit (parse, filter, naming, state, ...)  ~320 tests
```

### 7.2 Unit tests

**Parser fixture matrix** (corrected from draft v1's "15 × 3" undercount). The API format doesn't support NPF blocks, and NPF is organized by content-block type rather than post type:

- **`parse/api_json/`**: 7 post types × 2 fixtures each (normal + edge) = **14 fixtures**
  - photo, video, audio, text, quote, link, answer
  - Each post type has: one "standard" fixture and one "edge case" fixture (missing fields, null values, array-vs-scalar variance — see `SingleOrArrayConverter.cs` / `EmptyArrayOrDictionaryConverter.cs` in the C# code for motivation)
- **`parse/svc_json/`**: 7 post types × 2 fixtures each = **14 fixtures**
- **`parse/npf/`**: 6 content block types × 2 fixtures each + 4 mixed/multi-block post fixtures = **16 fixtures**
  - Content blocks: image, video, audio, text, link, unknown (new-block-type test)
  - Mixed fixtures: 2-block post, 5-block post, reblog-chain post, post with unknown-block fallback
- **`parse/html_scrape/`**: 4 fixtures (2 regex variants × 2 shape variants: PeeprRoute + response-wrapped)

**Total parser snapshots: ~48 committed fixtures, each producing a snapshot-tested `IntermediateDict` output = ~48 parser tests minimum.** Plus hypothesis-driven fuzzing of normalize functions (non-snapshot, ~15 property tests), totaling **~63 parser unit tests**. Budget room for **~80 parser tests** as edge cases accumulate.

**Filters:** table-driven. Each filter has a matrix of (input post, config options) → expected keep/drop result. Roughly 30 filter tests total.

**Naming:** property-based via `hypothesis`. Invariants: never produce invalid filename, never escape output root, byte-length always ≤255, NFC-normalized output, collision-free for same post+index. Unicode test suite runs on **both** macOS and Linux in CI to catch NFC/NFD divergence. Roughly 40 naming tests total.

**State (SQLite):** in-memory SQLite. Tests cover resume cursor semantics, dedup queries, transactional atomicity, schema migration, per-blog and cross-blog isolation. Roughly 50 state tests total.

**HTTP client:** `respx` mocks httpx responses. Tests cover rate-limit backoff, retries, 401 → `SessionExpired` mapping, timeout handling, **redirect safety (allowlist + IP validation on every hop)**, response body size cap enforcement. Roughly 45 HTTP tests total.

**Download module:** content-type reconciliation tests (URL says `.png`, response says `image/jpeg` → final filename uses `.jpg`), streaming chunk loop tests, cancellation-during-chunk tests. Roughly 25 download tests.

**Approximate unit test total: ~320.**

### 7.3 Component tests

Orchestrator tested with a **fake crawler** (yields synthetic posts) and a **fake downloader** (writes to `tmpfs`). Verifies:

- End-to-end state transitions
- Queue backpressure (slow downloader, fast crawler)
- Cancellation propagation
- **Concrete `test_signal_handling` assertions (was underspecified):**
  - After cancel: zero `.part` files remain in output directory
  - SQLite has no in-flight uncommitted transaction (`PRAGMA locking_mode; PRAGMA journal_mode;` + inspection)
  - Resume cursor is set to the last fully-completed post
  - All tracked tasks in `_internal.tasks` are done or cancelled
  - Exit code is 130
  - On second SIGINT within 3s: immediate exit with warning
- **`test_post_done_semantics`:** synthetic post with 3 media, one failing all retries. Assert sidecar is written with 2 success + 1 failed entries, cursor advances past the post.

### 7.4 Integration tests

End-to-end runs against recorded HTTP cassettes via `pytest-recording`. Cassettes stored under `tests/fixtures/cassettes/`, committed to the repo.

**Cassette scrubbing workflow (critical — was undocumented):**
- `scripts/scrub_cassettes.py` is the fixture scrubber. It reads a cassette YAML and redacts: `Set-Cookie`, `Cookie`, `Authorization`, `X-*` sensitive headers, and any body content matching the `SecretFilter` regex set.
- **Pre-commit hook:** `.pre-commit-config.yaml` runs the scrubber on any changed cassette file and fails the commit if anything was redacted (so the developer knows to re-stage the scrubbed version).
- **CI re-scan:** a CI step greps every committed cassette for known-secret patterns and fails the build on any match. Double defense against leaked secrets.
- Cassettes are authored by running tests with `--record-mode=new_episodes` (records missing interactions), then running the scrubber before committing. The workflow is documented in `docs/contributing.md`.

**Cassette list:**
- `test_download_public_blog.py` — crawl, filter, download, assert files on disk
- `test_resume.py` — crawl halfway, kill, resume, assert no duplicate downloads and exact file-count match
- `test_hidden_blog.py` — scrubbed logged-in cassette; verifies SVC flow + cookie handling
- `test_signal_handling.py` — synthetic mid-crawl SIGINT via `signal.raise_signal`, assert all drain invariants above
- `test_redirect_safety.py` — redirect to off-domain host + IMDS address; assert `AllowlistViolation`
- `test_pinned_post.py` — blog with a pinned post; assert pinned post is downloaded but excluded from `highest_post_id`

**Cassette refresh policy:** re-record on major Tumblr API changes, not per-release. Each cassette has a header comment with the date recorded and the tumblr-API-version assumption. Stale cassettes trigger regressions, which is the intended alarm.

### 7.5 What we explicitly do NOT test in CI

- Live Tumblr API (flaky, would break the build on their schedule, not ours)
- Full Playwright login end-to-end — gated behind `--run-auth-tests` pytest flag, runs only locally or on release-tag builds (not PRs)
- macOS System Integrity Protection edge cases (deferred to manual QA on release candidates)

### 7.6 Quality gates in CI

- `ruff check` + `ruff format --check`
- `pyright` — **`--strict` on `core/` and `parse/`, `--basic` on `cli/`** (strict pyright on CLI code fights Typer decorators and Pydantic v2 generics too hard)
- `pytest --cov=tumbl4 --cov-report=xml` with coverage targets: **core ≥80%**, **parse ≥90%** (correctness-critical), **cli ≥60%**
- `pip-audit` on direct + transitive deps
- Ruff rule banning `tumbl4.cli` imports from within `tumbl4.core` (`tidy-imports`)
- Ruff rule `RUF006` for dangling `create_task` detection
- Ruff rule banning `execute(f"...")` / `execute("%s" % ...)` for SQL injection protection
- Ruff rule banning `lxml.etree` imports inside `parse/html_scrape.py`
- Cassette-scrubber re-scan step that fails CI on any leaked secret in fixtures

### 7.7 CI matrix

- `{macos-latest, ubuntu-latest}` × `{python 3.11, 3.12, 3.13}` = 6 jobs
- Playwright browsers cached on `~/.cache/ms-playwright` keyed on `playwright` package version + runner OS
- Auth-gated tests skipped on fork-origin PRs (can't access repo secrets)

## 8. Distribution, CI/CD, delivery

### 8.1 GitHub Actions workflows

**`ci.yml`** — triggered on PR + push to main:
- Matrix: {macOS, Ubuntu} × {3.11, 3.12, 3.13}
- Steps: checkout → install uv (via `astral-sh/setup-uv`) → `uv sync --dev` → cache Playwright → `playwright install chromium --with-deps` (on cache miss only) → `ruff check` → `ruff format --check` → `pyright` → `pytest --cov` → `pip-audit` → cassette-scrubber re-scan

**`release.yml`** — triggered on tag `v*`:
1. Run `ci.yml` as a prerequisite
2. `uv build` → wheel + sdist
3. **`actions/attest-build-provenance`** → SLSA provenance attestation for each artifact
4. Publish to PyPI via **OIDC trusted publisher** (no stored API tokens)
5. Create GitHub release with changelog section + attached SLSA attestations

### 8.2 Versioning

- **SemVer.** `__version__` derived from git tags via `hatch-vcs`.
- **Pre-v1 (0.x.y):** CLI surface may break between minors
- **v1.0.0:** CLI surface frozen under SemVer contract; breaking changes require a major bump
- **`tumbl4 --version`** output:
  - From a PyPI install (no `.git`): `tumbl4 0.1.0`
  - From a git clone with `.git`: `tumbl4 0.1.0 (commit abc1234)`
  - Fallback: `tumbl4 0.1.0` without commit suffix — never error, never show a placeholder like `unknown`

### 8.3 Changelog

- `CHANGELOG.md` in "Keep a Changelog" format
- Every user-visible-change PR updates `## [Unreleased]`
- Release workflow moves `[Unreleased]` → `[vX.Y.Z]` section with the tag date

### 8.4 Documentation

- `README.md`: problem statement, quickstart, install, basic usage, link to docs
- `docs/*.md` as plain GitHub-rendered Markdown; MkDocs deferred until doc volume justifies it
- Content:
  - `installation.md` — uv tool install path, Playwright browser install, XDG paths
  - `getting-started.md` — first-run walkthrough
  - `authentication.md` — Playwright login flow, security notes, `tumbl4 logout` semantics, headless workflow (copy state file)
  - `configuration.md` — config schema, precedence chain, env vars, example config
  - `filename-templates.md` — template syntax, variables table (§5.15), examples
  - `commands/` — command reference (generatable from Typer via `typer-cli` or handwritten)
  - `architecture.md` — layer diagram + module overview for contributors
  - `security.md` — threat model, accepted limitations, file permissions reference, how to verify SLSA attestations
  - `contributing.md` — dev setup, how to run tests, PR checklist, **cassette recording workflow** (record mode, scrubber invocation, pre-commit expectations)

### 8.5 Install paths for users

- **Recommended:** `uv tool install tumbl4`
- `pipx install tumbl4`
- `pip install tumbl4` (in a user-managed venv)
- **Deferred to v2:** PyInstaller single-file binary, Homebrew formula

### 8.6 First-run UX

**Public vs hidden blog auto-detection (was a gap in draft v1 — the spec implied login was always required):**

- `tumbl4 download <blog>` **does not require prior login for public blogs**. The CLI attempts the public crawl path first.
- If the public API returns an authentication-required signal (Tumblr returns specific error codes for login-gated blogs), the CLI raises `BlogRequiresLogin`, which is presented to the user as: `"<blog>" requires login. Run \`tumbl4 login\` and try again.` Exit code 2 (auth-required).
- **Explicit override:** the user can pass `--hidden` to skip the public-API attempt and go straight to the authenticated/hidden crawler path. This is useful when the user already knows a blog is dashboard-only and wants to avoid the wasted round-trip.
- **Reverse override:** `--public` forces the public crawler path even if auto-detection would try the hidden path. Primarily a debugging flag.
- Both `--hidden` and `--public` without a prior `tumbl4 login` behave intuitively: `--public` needs no auth; `--hidden` without auth raises `BlogRequiresLogin` immediately.

**Other first-run surfaces:**
- `tumbl4 login` without Playwright Chromium installed → detects and prints: `` Chromium not installed. Run `playwright install chromium`. ``
- `tumbl4 login` with no display → `NoDisplay` error with headless workflow instructions (§6.7)
- First `tumbl4 download` against a brand new output directory → creates the directory (`0755`), the `_meta/` subdirectory, and the per-blog SQLite, with a one-line log at INFO describing each initial creation. No surprises.

## 9. Risks

Top risks going into implementation, in rough priority order:

1. **Hidden crawler complexity.** The C# `TumblrHiddenCrawler` uses SVC JSON, extracts `___INITIAL_STATE___` in two regex+shape variants, handles bearer-token refresh, and has a different `nextPage` cursor chain. It will share only a thin base with the public crawler. Plan for divergence from day 1, not a shared-parent-class refactor.
2. **Three coexisting Tumblr JSON formats in the parser layer** will accumulate bugs fastest. Parser snapshot tests *before* crawl integration tests — catching a parser regression from a fixture diff is dramatically cheaper than from a missing file.
3. **Content-type mid-stream reconciliation.** Tumblr CDN returns content-types that don't always match URL extensions. If the Python port skips this logic, files land with wrong extensions and OS tools (Quick Look, `file`) misidentify them. This was missed in draft v1; it's baked into the download module spec now.
4. **Pinned post handling.** Without the upfront HTML fetch + pinned-post exclusion from `highest_post_id`, resume runs would re-crawl entire blogs on every invocation. Load-bearing for correctness.
5. **Playwright session management.** v1 accepts re-prompt on session expiry (user reruns `tumbl4 login`). v2 may add `--keep-session-alive` for long crawls.
6. **Cassette bitrot.** Integration tests are only as current as the cassettes. Major Tumblr API changes silently pass tests but break on real blogs. Mitigation: manual smoke tests against a real public test blog on release candidates.
7. **Unicode filename edge cases on APFS vs ext4.** Designed for but unproven until the CI Unicode suite runs on both OSes.
8. **Playwright + PyInstaller incompatibility.** Users need Python. Acceptable for a technical audience.
9. **Supply chain compromise.** Mitigated by SLSA attestations, OIDC publishing, `uv.lock` commit, `pip-audit`, Dependabot. Residual risk = transitive dep vulnerability.
10. **Open-source contribution friction.** Playwright heavy install, cassette recording workflow is non-obvious, signal-handling tests are subtle. Good `CONTRIBUTING.md` matters.

## 10. Open questions / deferred decisions

- **Repo rename:** keep `TumblThreeMac` or rename to `tumbl4` before v1 release? (Low priority.)
- **`tumbl4` name:** placeholder, may change before release based on user experience using it day-to-day.
- **MkDocs site:** deferred until docs volume justifies it. If it doesn't, plain Markdown stays.
- **Homebrew formula:** deferred to post-v1. Once PyPI publishing is stable, add a tap.
- **v2 scope:** Twitter, Bluesky, NewTumbl, likes/liked-by/search/tag-search, external-link downloaders, GUI, `--keep-session-alive` mode, bandwidth throttling, PyInstaller binary. Prioritize based on v1 user feedback.

## 11. Glossary

- **IntermediateDict** — the uniform-shape `TypedDict` produced by `parse/` functions and consumed by `models/` Pydantic validation. Defined in §5.7.
- **NPF** — Neue Post Format. Tumblr's current native post format using typed content blocks.
- **SVC** — the internal "service" JSON format used by Tumblr's mobile app and authenticated endpoints.
- **API JSON** — the older Tumblr REST API v2 format (`/api/read/json`).
- **MediaTask** — the queue item enqueued from crawl → download workers; one per file to fetch.
- **`CrawlContext`** — the immutable frozen dataclass threaded through the orchestrator pipeline carrying all per-crawl state.
- **`CancelToken`** — cooperative cancellation primitive (§6.10); thread-safe bridge from signal handler to asyncio event loop.
- **`SecretFilter`** — logging filter that redacts credentials from formatted messages, structured extras, and exception tracebacks.
- **XDG** — XDG Base Directory Specification; standard locations for user config/state/cache/data files on Unix-like systems.
- **SLSA** — Supply-chain Levels for Software Artifacts; attestation framework for verifying that a published artifact was built from a specific source commit.
