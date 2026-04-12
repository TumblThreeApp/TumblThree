# tumbl4

A command-line Tumblr blog backup tool for macOS and Linux. Forked from [TumblThree](https://github.com/TumblThreeApp/TumblThree) (Windows WPF).

> **Status:** pre-release (foundation work in progress). The v1 milestone targets public + hidden/dashboard Tumblr blog backups with resume, filters, and metadata sidecars. See [the design spec](docs/superpowers/specs/2026-04-11-tumbl4-macos-cli-port-design.md) for the full scope.

## Install

```bash
uv tool install tumbl4
```

Or, for development from a checkout:

```bash
git clone https://github.com/claire/tumbl4
cd tumbl4
uv sync --dev
uv run tumbl4 --version
```

## What works today (Plan 1)

- `tumbl4 --version` prints the installed version
- Python package installable via `uv tool install`
- Project tooling: ruff (lint + format), pyright (type check, strict on core/models), pytest + hypothesis + respx
- CI matrix on macOS and Linux across Python 3.11/3.12/3.13

## What's coming next

Roughly in order:

1. **Plan 2 — MVP public blog photo crawl.** `tumbl4 download <public-blog>` downloads photos, resumable.
2. **Plan 3 — All post types + metadata sidecars + filename templates.** Every post type downloadable with JSON sidecars; configurable filename templates.
3. **Plan 4 — Filters + cross-blog dedup + pinned-post handling.** Original/tag/timespan filters; deduplication across blogs; correct resume across pinned posts.
4. **Plan 5 — Auth + hidden blog crawler.** Interactive Playwright login for dashboard-only blogs.
5. **Plan 6 — Security hardening + release.** Redirect safety, SSRF guards, signal handling, SLSA-attested PyPI releases.

## Development

```bash
# Install dev dependencies
uv sync --dev

# Run tests
uv run pytest

# Lint
uv run ruff check .

# Format
uv run ruff format .

# Type check
uv run pyright
```

## Contributing

`tumbl4` is in early development. Issues and PRs welcome — see [`CONTRIBUTING.md`](CONTRIBUTING.md) (added in a later plan). The authoritative design is [`docs/superpowers/specs/2026-04-11-tumbl4-macos-cli-port-design.md`](docs/superpowers/specs/2026-04-11-tumbl4-macos-cli-port-design.md).

## License

MIT — inherited from upstream TumblThree. See [`LICENSE`](LICENSE).
