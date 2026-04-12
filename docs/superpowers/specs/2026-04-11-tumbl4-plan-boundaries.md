# tumbl4 — Plan Boundaries (Plans 2–6)

**Status:** Approved
**Author:** Claire (with Claude assistance)
**Last updated:** 2026-04-11
**Parent spec:** [tumbl4 macOS/Linux CLI port design](2026-04-11-tumbl4-macos-cli-port-design.md)

This document defines the exact scope boundary for each implementation plan.
The parent design spec describes the full v1 architecture; this document says
what ships *when*.

## Corrections to the parent spec

These were identified during upstream TumblThree code review (2026-04-11):

1. **V1 API pagination is offset-based, not `before_id`-based.** Section 5.8
   describes the public crawler's resume cursor as `(highest_post_id, before_id)`
   with timestamp-based walking. The V1 API (`/api/read/json`) uses
   `start=N` offset pagination. The correct resume model for the public crawler
   is: `LastId` (highest non-pinned post ID seen on last *complete* crawl) as
   a stop-fence — skip posts with `id < LastId`. Offset walks forward from
   page 0; `LastId` determines when to stop. Only updated on complete crawl
   (matching TumblThree's `Blog.LastId` semantics).

2. **V1 response is JSONP-wrapped.** The endpoint returns
   `var tumblr_api_read = {...};` — strip the wrapper before JSON parsing.
   The `api_json` parser must handle this.

3. **Photo URL fields are V1-specific.** V1 uses `photo-url-1280`,
   `photo-url-500`, etc. Photosets use a `photos[]` array where each entry
   has the same multi-size URL fields plus `width`, `height`, `caption`,
   `offset`. This is distinct from NPF's `ImageBlock` structure.

4. **TumblThree uses no API key for public blogs.** The hardcoded consumer key
   (`x8pd1...`) is only used by the hidden/search crawlers with V2. The V1
   endpoint is fully unauthenticated.

5. **TumblThree does not use `.part` files.** It writes directly to the final
   path and uses HTTP Range requests for resume. Our `.part` + atomic rename
   approach is a deliberate improvement — safer crash recovery at the cost of
   not supporting mid-file resume. This divergence is intentional.

6. **"Best" resolution is non-trivial.** It requires an extra HTTP request per
   photo, its own retry policy (700ms + 10s/20s backoff), and resolution-aware
   dedup by media key. Deferred to Plan 3.

7. **Pinned post handling deferred to Plan 4.** Plan 2 accepts that blogs with
   pinned posts may trigger unnecessary full re-crawl on first resume.

## Plan 2 — MVP Public Blog Photo Crawl

**Goal:** `tumbl4 download <blog>` crawls a public Tumblr blog's photo posts
and saves them to disk, with resume and per-blog dedup.

### In scope

- **CLI:** `tumbl4 download <blog>` subcommand
  - `--output-dir PATH`
  - `--page-size N` (1–50, default 50)
  - `--image-size SIZE` (1280/500/400/250/100/75, default 1280)
  - `--no-resume` (ignore saved cursor, full re-crawl)
  - `--quiet` / `--verbose`
- **Crawl:** V1 API (`/api/read/json`), offset-based pagination, photo posts only
- **Parse:** `api_json` parser — JSONP unwrapping, photo post extraction
  (single + photoset), `IntermediateDict` + `MediaEntry` contracts
- **Download:** streaming chunk loop via httpx, `.part` + atomic rename,
  content-type reconciliation (PNG URL → JPG file when Content-Type disagrees)
- **State:** per-blog SQLite database, WAL mode, `LastId` resume cursor,
  per-blog URL dedup (SHA-256 of initial-request URL), schema migrations runner
- **Sidecars:** JSON metadata sidecar per post (atomic write, 0600 perms)
- **Models:** `Blog`, `BlogRef`, `Post` (photo variant), `Photo`, `MediaEntry`,
  `CrawlResult`, `DownloadResult`, `IntermediateDict`
- **Context:** `CrawlContext` frozen dataclass (simplified — no auth, no cross-blog dedup)
- **Errors:** `Tumbl4Error` exception hierarchy (subset: `CrawlError`,
  `DownloadError`, `StateError`, `ConfigError`)
- **HTTP:** httpx AsyncClient wrapper, rate limiter (aiolimiter), User-Agent
  header, response body size cap, explicit timeouts from `HttpSettings`
- **Progress:** Rich progress bars during crawl + download
- **Retry:** per-MediaTask exponential backoff (max 5 retries, base 1s,
  factor 2, max 60s). Respect `Retry-After` on 429.
- **Queue:** `asyncio.Queue` producer-consumer — crawler enqueues `MediaTask`
  objects, N download workers consume (default 4 from
  `settings.max_concurrent_downloads`)

### Out of scope (deferred)

- ~~Pinned post resolver~~ → Plan 4
- ~~"Best" image resolution~~ → Plan 3
- ~~Inline photo extraction from post body~~ → Plan 3
- ~~V2 API / NPF / SVC parsers~~ → Plan 3
- ~~Non-photo post types~~ → Plan 3
- ~~Filename templates~~ → Plan 3 (use hardcoded default `{blog}/{post_id}_{index_padded}.{ext}`)
- ~~TOML config files~~ → Plan 3
- ~~Filters (reblog/tag/timespan)~~ → Plan 4
- ~~Cross-blog dedup~~ → Plan 4
- ~~Auth / hidden blogs~~ → Plan 5
- ~~SSRF guards / redirect safety~~ → Plan 6
- ~~Signal handling (SIGINT)~~ → Plan 6
- ~~Orphan sweep~~ → Plan 6
- ~~PNJ→PNG conversion~~ → Plan 3

### Known limitations

- Blogs with pinned posts may cause unnecessary full re-crawl on first resume
  (pinned post inflates `LastId`). Fixed in Plan 4.
- Photos limited to 1280px max. "Best" resolution in Plan 3.
- Only photo posts downloaded. Other types silently skipped.
- No redirect safety (follows httpx defaults). Hardened in Plan 6.

### Testing target

~60 new tests:
- Parser snapshot fixtures (V1 photo single + photoset + edge cases): ~10
- HTTP client (respx mocks — rate limit, retry, timeout, size cap): ~12
- Download (streaming, content-type reconciliation, .part handling): ~10
- State (SQLite resume, dedup, migrations, sidecar writes): ~15
- Orchestrator component test (fake crawler + fake downloader): ~8
- CLI integration (CliRunner, download subcommand): ~5

## Plan 3 — All Post Types + Metadata + Filename Templates

**Goal:** Every post type downloadable. "Best" resolution for photos. Inline
media extraction. Configurable output paths. Full sidecar metadata.

### In scope

- **Parsers:** `svc_json.py`, `npf.py`, `html_scrape.py` — all three wire formats
- **Post types:** video, audio, text, quote, link, answer (add to models + parsers)
- **"Best" image resolution:** URL rewrite (`/s1280x1920/` → `/s2048x3072/`),
  HTML page fetch, `___INITIAL_STATE___` parsing for `imageResponse`, dedicated
  retry policy, resolution-aware dedup by media key
- **Inline media extraction:** regex scan of post body HTML for
  `media.tumblr.com` image/video URLs
- **Filename templates:** `naming/template.py` with `{blog}`, `{post_id}`,
  `{date}`, `{year}`, `{month}`, `{day}`, `{tag}`, `{tags}`, `{index}`,
  `{index_padded}`, `{ext}`, `{hash8}`, `{post_type}`, `{datetime}`.
  Validation at config load time.
- **PNJ→PNG conversion** option
- **V2 API** support as alternative (with consumer key)
- **TOML config:** project `./tumbl4.toml` + user `$XDG_CONFIG_HOME/tumbl4/config.toml`
- **`tumbl4 config get/set`** subcommand
- **Sidecar queue:** `state/sidecar_writer.py` — dedicated async worker

### Testing target

~120 new tests (parser fixture matrix is the bulk)

## Plan 4 — Filters + Cross-Blog Dedup + Pinned Posts

**Goal:** Filter posts. Cross-blog dedup. Correct resume for blogs with
pinned posts.

### In scope

- **Filters:** `filter/reblog.py`, `filter/tag.py`, `filter/timespan.py` —
  pure functions, table-driven tests
- **Cross-blog dedup:** `state/dedup.py` — shared SQLite at
  `$XDG_DATA_HOME/tumbl4/dedup.db`, URL-indexed, `--no-dedup` flag for
  cross-blog only (per-blog always active)
- **Pinned post resolver:** `crawl/pinned_resolver.py` — HTML fetch +
  `___INITIAL_STATE___` extraction (reuses `html_scrape.py` from Plan 3),
  skip pinned post for `highest_id` computation
- **CLI flags:** `--tag`, `--exclude-tag`, `--since`, `--until`,
  `--originals-only`, `--reblogs-only`, `--no-dedup`
- **`tumbl4 list`** — show managed blogs from state
- **`tumbl4 status <blog>`** — resume info, download counts, last crawl time

### Testing target

~80 new tests (filters: ~30, dedup: ~25, pinned: ~15, CLI: ~10)

## Plan 5 — Auth + Hidden Blog Crawler

**Goal:** Playwright-driven interactive login for dashboard/hidden blogs.

### In scope

- **Auth flow:** `auth/playwright_login.py` — headed Chromium, fresh profile
  each login, extract cookies + bearer token
- **Cookie persistence:** `auth/cookie_store.py` — read/write
  `playwright_state.json` (0600 perms, refuse to run if broader)
- **Session management:** `auth/session.py` — `AuthSession` with httpx cookie
  jar, bearer token from `___INITIAL_STATE___`
- **Hidden crawler:** `crawl/tumblr_hidden.py` — SVC/NPF format, cursor-based
  pagination via `next_link`, two-shape `___INITIAL_STATE___` handling
  (PeeprRoute vs response-wrapped)
- **CLI:** `tumbl4 login`, `tumbl4 logout`
- **Headless detection:** `$DISPLAY`/`$WAYLAND_DISPLAY` check, `NoDisplay` error
  with helpful message
- **Blog type detection:** auto-detect public vs hidden, prompt to login if needed
- **Browser profile:** fresh on each login, pinned under
  `$XDG_STATE_HOME/tumbl4/browser_profile/` (0700)

### Testing target

~50 new tests (auth flow mocked, cookie store, session, hidden crawler with
respx, blog detection)

## Plan 6 — Security Hardening + Release

**Goal:** Production-ready security. Signed PyPI release.

### In scope

- **Redirect safety:** `http_client.safe_follow_redirects()` — manual redirect
  following, per-hop allowlist (`*.tumblr.com`), SSRF IP validation
  (RFC 1918/loopback/link-local/IMDS blocking)
- **Terminal sanitization:** `_internal/sanitize.py` — strip Cc/Cf/Cs Unicode
  categories from external content before display
- **Signal handling:** `_internal/signal_handling.py` — SIGINT → `CancelToken`,
  cooperative shutdown (drain queue, persist cursor, WAL checkpoint),
  double-SIGINT immediate exit
- **CancelToken:** `threading.Event`-based, safe from signal handler context
- **Orphan sweep:** `state/orphan_sweep.py` — `.part` cleanup on startup
  (bounded scan, `asyncio.to_thread`)
- **Pending posts tracker:** `state/pending_posts.py` — in-flight post→media
  map, `asyncio.Lock`-guarded
- **Path traversal guards:** `is_relative_to(output_root.resolve())` before
  every `open()` in `file_downloader.py` and `metadata.py`
- **SQL injection prevention:** ruff ban on `execute(f"...")` patterns
- **XXE prevention:** ruff ban on `lxml.etree` in `html_scrape.py`
- **`tumbl4 sweep <blog>`** — manual orphan cleanup subcommand
- **CI hardening:** `pip-audit`, Dependabot, cassette-scrubber re-scan
- **Release workflow:** `release.yml` — build + SLSA attestation +
  PyPI OIDC publish + GitHub release
- **Docs site:** `docs/` — installation, getting-started, configuration,
  authentication, filename-templates, architecture, security, commands

### Testing target

~70 new tests (redirect safety: ~15, signal handling: ~10, sanitize: ~8,
orphan sweep: ~8, path traversal: ~10, integration: ~15, SQL ban: structural)
