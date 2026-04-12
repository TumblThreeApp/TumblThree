# Changelog

All notable changes to tumbl4 are documented here.

This project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html) from v1.0.0 onward. Pre-v1 releases (`0.x.y`) may include breaking CLI changes between minor versions.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- Initial Python project scaffolding: `pyproject.toml` (hatchling build), `src/tumbl4/` package layout, `py.typed` marker for PEP 561
- Settings model (`tumbl4.models.settings.Settings`) with Pydantic BaseSettings, env-var overrides via `TUMBL4_*` prefix, nested `QueueSettings` and `HttpSettings`
- XDG-compliant paths module (`tumbl4._internal.paths`) with macOS and Linux fallbacks
- Supervised task helper (`tumbl4._internal.tasks.spawn`) wrapping `asyncio.create_task` with tracking and done-callback error logging
- `SecretFilter` + `get_logger` in `tumbl4._internal.logging` — scrubs credentials from formatted messages, structured extras, and exception tracebacks
- Typer-based CLI skeleton (`tumbl4.cli.app`) with `--version` / `-V` flag
- Test suite with 31 tests across `tests/unit/` covering version, settings, paths, tasks, logging, CLI version, and import-boundary structural checks
- GitHub Actions CI workflow: `{macos-latest, ubuntu-latest}` × `{3.11, 3.12, 3.13}` (excluding macOS × 3.11), lint + format + type + test with coverage
- MIT license preserved from upstream TumblThree; upstream C# changelog archived at `docs/CHANGELOG-upstream.md`

### Removed

- Entire upstream C# codebase (`src/TumblThree/`, `lib/`, Windows-only `scripts/` and `appveyor.yml`)
- Old `Contributing.md` and `.gitattributes` (will be rewritten for Python)
