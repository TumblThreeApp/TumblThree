# tumbl4 Plan 1: Foundation — Project Setup, Tooling, CI, CLI Skeleton

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Get tumbl4 installable with modern Python tooling, tests green, CI running on macOS + Linux, and `tumbl4 --version` working from a clean-rewrite Python project. No crawlers yet.

**Architecture:** Replace the existing C# codebase with a Python 3.12+ project using `uv` + `hatchling` build backend, `ruff` + `pyright` for quality gates, `pytest` + `hypothesis` + `respx` for testing, and `Typer` for the CLI. All modules are placed under `src/tumbl4/` following the `src/` layout convention. Strict import boundaries enforced by ruff: `tumbl4.cli` may not import from `tumbl4.core`.

**Tech Stack:** Python 3.12+, uv (package manager), hatchling (build), ruff (lint + format), pyright (type check), pytest + pytest-cov + hypothesis + respx (test), Typer (CLI), Rich (output), structlog (logging), pydantic + pydantic-settings (models + config).

**Deliverable at plan completion:**
- Running `uv tool install --from . tumbl4` from the repo root installs the package
- `tumbl4 --version` prints `tumbl4 0.1.0`
- `uv run pytest` passes green with ~10 tests (foundation modules only)
- `uv run ruff check` + `uv run ruff format --check` pass
- `uv run pyright` passes (strict on core/parse, basic on cli)
- GitHub Actions CI workflow runs on `macos-latest` + `ubuntu-latest` × Python 3.11/3.12/3.13 and all jobs pass
- README.md has a quickstart
- Repo directory renamed from `TumblThreeMac` to `tumbl4` (last task — so the plan file itself is authored before the rename)

**Plans in this series (roadmap for tumbl4 v1):**

| # | Plan | Deliverable |
|---|---|---|
| **1** | **Foundation** (this plan) | `tumbl4 --version` works; tooling + CI green; repo renamed |
| 2 | MVP public blog photo crawl | `tumbl4 download <public-blog>` downloads photos, resumable |
| 3 | All post types + sidecars + templates | Every post type downloadable with metadata; configurable filename templates |
| 4 | Filters + dedup + pinned posts | original/tag/timespan filters; cross-blog dedup; pinned-post handling |
| 5 | Auth + hidden blog crawler | `tumbl4 login` + hidden/dashboard blog downloads |
| 6 | Security hardening + release | Redirect safety, SSRF guards, signal handling, SLSA release workflow, PyPI publish |

**Spec reference:** `docs/superpowers/specs/2026-04-11-tumbl4-macos-cli-port-design.md`

---

## File Structure (Plan 1 only)

This plan creates these files. Later plans populate the rest of the tree defined in spec §4.

```
tumbl4/                                    # renamed from TumblThreeMac in final task
├── .github/
│   └── workflows/
│       └── ci.yml                          # lint + type + test matrix
├── .gitignore                              # Python-oriented
├── .python-version                         # 3.12
├── CHANGELOG.md                            # Keep-a-Changelog skeleton
├── LICENSE                                 # preserved from upstream (MIT)
├── README.md                               # new, tumbl4-focused
├── pyproject.toml                          # hatchling build + ruff + pyright + deps
├── src/
│   └── tumbl4/
│       ├── __init__.py                     # exports __version__
│       ├── __main__.py                     # enables `python -m tumbl4`
│       ├── py.typed                        # PEP 561 marker
│       ├── cli/
│       │   ├── __init__.py
│       │   └── app.py                      # Typer app + --version
│       ├── core/
│       │   └── __init__.py                 # marker only; populated in later plans
│       ├── models/
│       │   ├── __init__.py
│       │   └── settings.py                 # Pydantic Settings skeleton
│       └── _internal/
│           ├── __init__.py
│           ├── logging.py                  # SecretFilter + logger factory
│           ├── paths.py                    # XDG base dirs
│           └── tasks.py                    # spawn() helper for supervised create_task
├── tests/
│   ├── __init__.py
│   ├── conftest.py                         # shared pytest fixtures
│   └── unit/
│       ├── __init__.py
│       ├── test_version.py
│       ├── test_cli_version.py
│       ├── test_settings.py
│       ├── test_paths.py
│       ├── test_logging.py
│       ├── test_tasks.py
│       └── test_import_boundaries.py       # sanity check on import discipline
└── docs/
    └── superpowers/
        ├── specs/2026-04-11-tumbl4-macos-cli-port-design.md       # already committed
        └── plans/2026-04-11-tumbl4-plan-01-foundation.md          # this file
```

**Files being deleted** (upstream C# artifacts and obsolete tooling — cleaned out in Task 1):
- `src/TumblThree/` (entire directory — 3 C# projects)
- `lib/` (RateLimiter, WpfApplicationFramework, WpfImageViewer)
- `scripts/` (appveyor PowerShell scripts)
- `appveyor.yml` (Windows-only CI config)
- `LICENSE-3RD-PARTY` (was for C# dependencies; will recreate if needed for Python deps)
- `Contributing.md` (pre-existing; will rewrite in a later plan)
- `.gitattributes` (replaced with Python-appropriate version)

**Files being preserved:** `LICENSE`, `docs/Contributors.md`, `docs/code_of_conduct.md`, `docs/New-to-OSS.md`, `docs/CHANGELOG.md` (old C# changelog — renamed to `docs/CHANGELOG-upstream.md` for historical reference), all files under `docs/superpowers/`, `.git/`.

---

## Task 1: Remove upstream C# codebase and obsolete tooling

**Files:**
- Delete: `src/TumblThree/` (whole tree)
- Delete: `lib/`
- Delete: `scripts/`
- Delete: `appveyor.yml`
- Delete: `LICENSE-3RD-PARTY`
- Delete: `Contributing.md`
- Delete: `.gitattributes`
- Rename: `docs/CHANGELOG.md` → `docs/CHANGELOG-upstream.md`

- [ ] **Step 1: Verify the files that will be deleted and clean up untracked junk**

Run:
```bash
ls src/TumblThree/ lib/ scripts/ appveyor.yml LICENSE-3RD-PARTY Contributing.md .gitattributes
```

Expected: all listed items exist. If any are missing, reconcile against `git status` before proceeding (a prior plan may have deleted some).

Also remove any untracked `.DS_Store` files from the filesystem so they don't clutter the `git status` check in Step 4. These are untracked (macOS creates them automatically), not in the git index, so this is a filesystem-level cleanup — nothing to commit:

```bash
find . -name '.DS_Store' -type f -delete
```

Expected: no output. If `.DS_Store` files did exist, they are now gone from the working tree.

- [ ] **Step 2: Delete C# source tree, libraries, and scripts**

Run:
```bash
git rm -rf src/TumblThree lib scripts
git rm appveyor.yml LICENSE-3RD-PARTY Contributing.md .gitattributes
```

Expected: all listed paths are removed from the index.

- [ ] **Step 3: Preserve upstream C# changelog for historical reference**

Run:
```bash
git mv docs/CHANGELOG.md docs/CHANGELOG-upstream.md
```

Expected: file is moved in the index.

- [ ] **Step 4: Verify working tree cleanliness**

Run:
```bash
git status --short
```

Expected output (order may vary):
```
 D Contributing.md
 D LICENSE-3RD-PARTY
 D appveyor.yml
 D .gitattributes
R  docs/CHANGELOG.md -> docs/CHANGELOG-upstream.md
 D lib/RateLimiter/...           (many lines)
 D scripts/...                    (several lines)
 D src/TumblThree/...              (many lines)
```

Nothing else should appear. If there are unexpected items (e.g., `.DS_Store` files), do NOT stage them.

- [ ] **Step 5: Commit the cleanout**

Run:
```bash
git commit -m "$(cat <<'EOF'
chore: remove upstream C# codebase and Windows-only tooling

Clean slate for the Python rewrite. Removes src/TumblThree, lib/ (WAF and
friends), scripts/ (appveyor PowerShell), appveyor.yml, LICENSE-3RD-PARTY,
Contributing.md, and .gitattributes. The upstream C# changelog is preserved
under docs/CHANGELOG-upstream.md for historical reference.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

Expected: commit succeeds with a single commit containing all removals.

---

## Task 2: Write Python .gitignore and .python-version

**Files:**
- Create: `.gitignore`
- Create: `.python-version`

- [ ] **Step 1: Create .gitignore**

Write file `.gitignore`:

```gitignore
# Byte-compiled / optimized / DLL files
__pycache__/
*.py[cod]
*$py.class

# Distribution / packaging
.Python
build/
dist/
*.egg-info/
*.egg
wheels/
.eggs/

# Virtual environments
.venv/
venv/
env/
.env

# uv
.uv/

# Testing / coverage
.pytest_cache/
.coverage
.coverage.*
htmlcov/
coverage.xml
.hypothesis/

# Type checkers
.mypy_cache/
.pyright/
.ruff_cache/

# IDEs / editors
.vscode/
.idea/
*.swp
*.swo

# OS
.DS_Store
Thumbs.db

# Playwright (cached browsers)
.playwright/

# tumbl4 runtime state (created during manual testing)
.tumbl4/
playwright_state.json
browser_profile/

# Local working output directories
output/
downloads/
```

- [ ] **Step 2: Create .python-version**

Write file `.python-version`:

```
3.12
```

(A single line, no trailing content.)

- [ ] **Step 3: Stage and commit**

Run:
```bash
git add .gitignore .python-version
git commit -m "$(cat <<'EOF'
chore: add Python .gitignore and .python-version

Pin Python 3.12 for local dev. Ignore typical Python build artifacts,
virtual envs, tool caches, and tumbl4 runtime state (Playwright profile,
output directories).

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Write pyproject.toml with hatchling + dev dependencies

**Files:**
- Create: `pyproject.toml`

- [ ] **Step 1: Write pyproject.toml**

Write file `pyproject.toml`:

```toml
[build-system]
requires = ["hatchling>=1.25"]
build-backend = "hatchling.build"

[project]
name = "tumbl4"
version = "0.1.0"
description = "Command-line Tumblr blog backup tool for macOS and Linux"
readme = "README.md"
license = { file = "LICENSE" }
requires-python = ">=3.12"
authors = [
    { name = "Claire" },
]
keywords = ["tumblr", "backup", "archive", "cli", "crawler"]
classifiers = [
    "Development Status :: 3 - Alpha",
    "Environment :: Console",
    "Intended Audience :: End Users/Desktop",
    "License :: OSI Approved :: MIT License",
    "Operating System :: MacOS",
    "Operating System :: POSIX :: Linux",
    "Programming Language :: Python :: 3",
    "Programming Language :: Python :: 3.12",
    "Programming Language :: Python :: 3.13",
    "Topic :: Internet :: WWW/HTTP",
    "Topic :: System :: Archiving :: Backup",
    "Typing :: Typed",
]
dependencies = [
    "httpx>=0.27",
    "pydantic>=2.8",
    "pydantic-settings>=2.5",
    "typer>=0.12",
    "rich>=13.7",
    "structlog>=24.1",
    "aiofiles>=24.1",
    "aiolimiter>=1.1",
]

[project.optional-dependencies]

[project.scripts]
tumbl4 = "tumbl4.cli.app:main"

[project.urls]
Repository = "https://github.com/claire/tumbl4"
Issues = "https://github.com/claire/tumbl4/issues"

[dependency-groups]
dev = [
    "pytest>=8.3",
    "pytest-asyncio>=0.24",
    "pytest-cov>=5.0",
    "pytest-recording>=0.13",
    "respx>=0.21",
    "hypothesis>=6.112",
    "ruff>=0.6",
    "pyright>=1.1.380",
    "pre-commit>=3.8",
]

[tool.hatch.build.targets.wheel]
packages = ["src/tumbl4"]

[tool.hatch.build.targets.sdist]
include = [
    "src/tumbl4",
    "README.md",
    "LICENSE",
    "CHANGELOG.md",
]

[tool.ruff]
line-length = 100
target-version = "py312"
src = ["src"]
extend-exclude = [
    "docs",
]

[tool.ruff.lint]
select = [
    "E",     # pycodestyle errors
    "W",     # pycodestyle warnings
    "F",     # pyflakes
    "I",     # isort
    "B",     # flake8-bugbear
    "C4",    # flake8-comprehensions
    "UP",    # pyupgrade
    "RUF",   # ruff-specific
    "TID",   # flake8-tidy-imports — used for cli/core boundary
    "SIM",   # flake8-simplify
    "PL",    # pylint subset
    "S",     # flake8-bandit — security
]
ignore = [
    "S101",  # assert is fine in tests
    "PLR0913",  # allow many args on core functions; revisit after slice 2
]

[tool.ruff.lint.per-file-ignores]
"tests/**/*" = ["S", "PLR2004"]  # tests can use magic values + no bandit

[tool.ruff.lint.flake8-tidy-imports.banned-api]
"tumbl4.cli".msg = "core modules must not import from cli; see spec §3."

[tool.ruff.format]
quote-style = "double"
indent-style = "space"

[tool.pyright]
include = ["src/tumbl4", "tests"]
exclude = ["**/__pycache__", "**/.venv"]
pythonVersion = "3.12"
typeCheckingMode = "basic"

# Strict on correctness-critical subpackages
[[tool.pyright.executionEnvironments]]
root = "src/tumbl4/core"
typeCheckingMode = "strict"

[[tool.pyright.executionEnvironments]]
root = "src/tumbl4/models"
typeCheckingMode = "strict"

[tool.pytest.ini_options]
minversion = "8.0"
testpaths = ["tests"]
asyncio_mode = "auto"
addopts = [
    "-ra",
    "--strict-markers",
    "--strict-config",
]
markers = [
    "slow: marks tests as slow (deselect with '-m \"not slow\"')",
]

[tool.coverage.run]
source = ["src/tumbl4"]
branch = true

[tool.coverage.report]
exclude_lines = [
    "pragma: no cover",
    "raise NotImplementedError",
    "if TYPE_CHECKING:",
    "if __name__ == .__main__.:",
]
```

- [ ] **Step 2: Stage and commit**

Run:
```bash
git add pyproject.toml
git commit -m "$(cat <<'EOF'
chore: add pyproject.toml with hatchling build + dev tooling

Configures hatchling as the build backend, declares runtime dependencies
(httpx, pydantic, typer, rich, structlog, aiofiles, aiolimiter), dev deps
(pytest + plugins, ruff, pyright, hypothesis, respx, pre-commit), and
tool settings for ruff (strict lint with tidy-imports ban on cli→core),
pyright (strict on core + models, basic on cli), and pytest (asyncio_mode
= auto, strict markers).

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Create tumbl4 package skeleton (__init__.py, py.typed, __main__.py)

**Files:**
- Create: `src/tumbl4/__init__.py`
- Create: `src/tumbl4/py.typed` (empty)
- Create: `src/tumbl4/__main__.py`
- Create: `src/tumbl4/core/__init__.py` (marker)
- Create: `src/tumbl4/cli/__init__.py` (marker)
- Create: `src/tumbl4/models/__init__.py` (marker)
- Create: `src/tumbl4/_internal/__init__.py` (marker)

- [ ] **Step 1: Create the package directory tree**

Run:
```bash
mkdir -p src/tumbl4/{cli,core,models,_internal}
```

- [ ] **Step 2: Create src/tumbl4/__init__.py**

Write file `src/tumbl4/__init__.py`:

```python
"""tumbl4 — command-line Tumblr blog backup tool for macOS and Linux."""

from __future__ import annotations

__version__ = "0.1.0"

__all__ = ["__version__"]
```

- [ ] **Step 3: Create src/tumbl4/py.typed**

Write file `src/tumbl4/py.typed` (empty file — the PEP 561 marker):

(Empty content — zero bytes.)

- [ ] **Step 4: Create src/tumbl4/__main__.py**

Write file `src/tumbl4/__main__.py`:

```python
"""Enable `python -m tumbl4` as an alternative entry point."""

from __future__ import annotations

from tumbl4.cli.app import main

if __name__ == "__main__":
    main()
```

- [ ] **Step 5: Create empty __init__.py markers for subpackages**

Write file `src/tumbl4/cli/__init__.py`:

```python
"""CLI surface for tumbl4. Typer-based commands and Rich output helpers."""
```

Write file `src/tumbl4/core/__init__.py`:

```python
"""Core orchestration, crawling, parsing, download, and state modules.

Public API is declared in submodules; `tumbl4.core` itself is documented
as "unstable — may change" until third-party demand proves otherwise
(see design spec §3).
"""
```

Write file `src/tumbl4/models/__init__.py`:

```python
"""Pydantic domain models shared across core and cli layers."""
```

Write file `src/tumbl4/_internal/__init__.py`:

```python
"""Internal helpers — do not import from outside the tumbl4 package.

Leading-underscore convention: anything under `tumbl4._internal` is not
part of the stable public API regardless of what `__all__` lists.
"""
```

- [ ] **Step 6: Sync uv environment and verify import**

Run:
```bash
uv sync --dev
```

Expected: uv creates `.venv/`, installs all dependencies and dev deps, `uv.lock` is created.

Then run:
```bash
uv run python -c "import tumbl4; print(tumbl4.__version__)"
```

Expected output: `0.1.0`

- [ ] **Step 7: Stage and commit**

Run:
```bash
git add src/tumbl4 uv.lock
git commit -m "$(cat <<'EOF'
feat: create tumbl4 package skeleton with __version__

Initial src/tumbl4/ package layout: __init__.py with __version__,
__main__.py entry point, py.typed PEP 561 marker, and empty markers
for cli/, core/, models/, and _internal/ subpackages. Running
`python -c "import tumbl4"` now succeeds and reports version 0.1.0.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Create test harness and first test (version)

**Files:**
- Create: `tests/__init__.py`
- Create: `tests/conftest.py`
- Create: `tests/unit/__init__.py`
- Create: `tests/unit/test_version.py`

- [ ] **Step 1: Create tests/__init__.py and tests/unit/__init__.py**

Write file `tests/__init__.py`:

```python
"""Test suite for tumbl4."""
```

Write file `tests/unit/__init__.py`:

```python
"""Unit tests for tumbl4 modules."""
```

- [ ] **Step 2: Create tests/conftest.py**

Write file `tests/conftest.py`:

```python
"""Shared pytest fixtures for tumbl4 tests."""

from __future__ import annotations

from collections.abc import Iterator
from pathlib import Path

import pytest


@pytest.fixture
def tmp_output_dir(tmp_path: Path) -> Iterator[Path]:
    """Return a temporary output directory for tests that write files."""
    out = tmp_path / "output"
    out.mkdir()
    yield out
```

- [ ] **Step 3: Write tests/unit/test_version.py (the failing test)**

Write file `tests/unit/test_version.py`:

```python
"""Smoke test that the package exposes a version string."""

from __future__ import annotations

import re

import tumbl4


def test_version_is_semver_string() -> None:
    assert hasattr(tumbl4, "__version__")
    assert isinstance(tumbl4.__version__, str)
    # SemVer 2.0 regex (without build metadata) — matches MAJOR.MINOR.PATCH[-PRERELEASE]
    pattern = r"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$"
    assert re.match(pattern, tumbl4.__version__), (
        f"__version__ {tumbl4.__version__!r} is not valid SemVer"
    )


def test_version_matches_pyproject() -> None:
    # Ensures the __init__.py version and the pyproject.toml version do not drift.
    # We intentionally parse pyproject.toml directly rather than importing
    # build-time metadata, so this test catches the case where someone bumps
    # one but forgets the other.
    import tomllib
    from pathlib import Path

    pyproject = Path(__file__).resolve().parents[2] / "pyproject.toml"
    data = tomllib.loads(pyproject.read_text(encoding="utf-8"))
    assert data["project"]["version"] == tumbl4.__version__
```

- [ ] **Step 4: Run the test to confirm it passes**

Run:
```bash
uv run pytest tests/unit/test_version.py -v
```

Expected output: both tests PASS. Sample:
```
tests/unit/test_version.py::test_version_is_semver_string PASSED
tests/unit/test_version.py::test_version_matches_pyproject PASSED
```

(This is a pass-on-first-run test because we wrote `__version__` in Task 4. TDD is about making sure tests drive the code — but for test-the-scaffolding cases like a version smoke test, a pass-on-first-run check is fine. The next task does a more traditional fail-then-pass flow.)

- [ ] **Step 5: Stage and commit**

Run:
```bash
git add tests
git commit -m "$(cat <<'EOF'
test: add pytest harness + version smoke test

tests/conftest.py with a tmp_output_dir fixture shared across the suite,
and tests/unit/test_version.py that asserts tumbl4.__version__ is valid
SemVer and matches the version in pyproject.toml. Running
`uv run pytest` now passes green.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: Settings model (Pydantic BaseSettings)

**Files:**
- Test: `tests/unit/test_settings.py`
- Create: `src/tumbl4/models/settings.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_settings.py`:

```python
"""Tests for the Settings model (Pydantic BaseSettings)."""

from __future__ import annotations

from pathlib import Path

import pytest

from tumbl4.models.settings import Settings


def test_settings_defaults_construct_without_error() -> None:
    s = Settings()
    assert s.output_dir is not None
    assert s.log_level == "INFO"
    assert s.max_concurrent_downloads == 4
    assert s.queue.max_pending_media == 200
    assert s.queue.max_pending_sidecars == 16
    assert s.http.connect_timeout == 10.0
    assert s.http.read_timeout == 60.0
    assert s.http.max_api_response_bytes == 32 * 1024 * 1024


def test_settings_accepts_env_overrides(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("TUMBL4_LOG_LEVEL", "DEBUG")
    monkeypatch.setenv("TUMBL4_MAX_CONCURRENT_DOWNLOADS", "8")
    s = Settings()
    assert s.log_level == "DEBUG"
    assert s.max_concurrent_downloads == 8


def test_settings_output_dir_coerces_to_path(tmp_path: Path) -> None:
    s = Settings(output_dir=str(tmp_path))
    assert s.output_dir == tmp_path
    assert isinstance(s.output_dir, Path)


def test_settings_rejects_invalid_log_level() -> None:
    from pydantic import ValidationError

    with pytest.raises(ValidationError):
        Settings(log_level="NONSENSE")
```

- [ ] **Step 2: Run the test to verify it fails**

Run:
```bash
uv run pytest tests/unit/test_settings.py -v
```

Expected: all tests FAIL with `ModuleNotFoundError: No module named 'tumbl4.models.settings'`.

- [ ] **Step 3: Write the minimal Settings implementation**

Write file `src/tumbl4/models/settings.py`:

```python
"""tumbl4 Settings model — Pydantic BaseSettings with env-var and config-file support.

Config precedence (highest → lowest) per spec §5.1:
    1. CLI flags
    2. Environment variables (TUMBL4_*)
    3. Project config (./tumbl4.toml) — wired in a later plan
    4. User config ($XDG_CONFIG_HOME/tumbl4/config.toml) — wired in a later plan
    5. Hardcoded defaults (this file)

In Plan 1 we only implement defaults + env-var overrides. TOML layers are
added in Plan 2 once we have a real config surface to validate.
"""

from __future__ import annotations

from pathlib import Path
from typing import Literal

from pydantic import BaseModel, Field
from pydantic_settings import BaseSettings, SettingsConfigDict


LogLevel = Literal["DEBUG", "INFO", "WARNING", "ERROR", "CRITICAL"]


class QueueSettings(BaseModel):
    max_pending_media: int = Field(default=200, ge=1, le=10_000)
    max_pending_sidecars: int = Field(default=16, ge=1, le=1_000)


class HttpSettings(BaseModel):
    connect_timeout: float = Field(default=10.0, gt=0.0)
    read_timeout: float = Field(default=60.0, gt=0.0)
    write_timeout: float = Field(default=30.0, gt=0.0)
    pool_timeout: float = Field(default=5.0, gt=0.0)
    max_connections: int = Field(default=32, ge=1)
    max_keepalive_connections: int = Field(default=16, ge=1)
    max_redirects: int = Field(default=5, ge=0, le=20)
    max_api_response_bytes: int = Field(default=32 * 1024 * 1024, ge=1024)
    user_agent_suffix: str = Field(
        default="+https://github.com/claire/tumbl4",
        description="Appended to User-Agent header after `tumbl4/{version}`.",
    )


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_prefix="TUMBL4_",
        env_nested_delimiter="__",
        env_file=None,
        extra="forbid",
    )

    output_dir: Path = Field(
        default_factory=lambda: Path.cwd() / "tumbl4-output",
        description="Where downloaded media and sidecars are written.",
    )
    log_level: LogLevel = "INFO"
    max_concurrent_downloads: int = Field(default=4, ge=1, le=32)
    queue: QueueSettings = Field(default_factory=QueueSettings)
    http: HttpSettings = Field(default_factory=HttpSettings)
```

- [ ] **Step 4: Run the test to verify it passes**

Run:
```bash
uv run pytest tests/unit/test_settings.py -v
```

Expected: all four tests PASS.

- [ ] **Step 5: Stage and commit**

Run:
```bash
git add src/tumbl4/models/settings.py tests/unit/test_settings.py
git commit -m "$(cat <<'EOF'
feat(models): add Settings model with QueueSettings and HttpSettings

Pydantic BaseSettings-backed configuration with defaults matching spec
§5.1/§5.4: output_dir, log_level, max_concurrent_downloads, queue sizes,
and HTTP client timeouts + pool + response-body cap. Env var overrides
via TUMBL4_* prefix. TOML config-file layering is deferred to Plan 2
once we have a real config surface.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 7: XDG paths module

**Files:**
- Test: `tests/unit/test_paths.py`
- Create: `src/tumbl4/_internal/paths.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_paths.py`:

```python
"""Tests for tumbl4._internal.paths (XDG base directory resolution)."""

from __future__ import annotations

import sys
from pathlib import Path

import pytest

from tumbl4._internal import paths


def test_config_dir_respects_xdg_config_home(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    monkeypatch.setenv("XDG_CONFIG_HOME", str(tmp_path / "xdg-config"))
    result = paths.config_dir()
    assert result == tmp_path / "xdg-config" / "tumbl4"


def test_state_dir_respects_xdg_state_home(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    monkeypatch.setenv("XDG_STATE_HOME", str(tmp_path / "xdg-state"))
    result = paths.state_dir()
    assert result == tmp_path / "xdg-state" / "tumbl4"


def test_data_dir_respects_xdg_data_home(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    monkeypatch.setenv("XDG_DATA_HOME", str(tmp_path / "xdg-data"))
    result = paths.data_dir()
    assert result == tmp_path / "xdg-data" / "tumbl4"


def test_config_dir_linux_default(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    monkeypatch.delenv("XDG_CONFIG_HOME", raising=False)
    monkeypatch.setenv("HOME", str(tmp_path))
    if sys.platform == "darwin":
        # On macOS the platform-specific fallback is used
        result = paths.config_dir()
        assert "tumbl4" in str(result)
    else:
        result = paths.config_dir()
        assert result == tmp_path / ".config" / "tumbl4"


def test_playwright_state_file_is_under_state_dir(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    monkeypatch.setenv("XDG_STATE_HOME", str(tmp_path / "xdg-state"))
    result = paths.playwright_state_file()
    assert result == tmp_path / "xdg-state" / "tumbl4" / "playwright_state.json"


def test_browser_profile_dir_is_under_state_dir(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    monkeypatch.setenv("XDG_STATE_HOME", str(tmp_path / "xdg-state"))
    result = paths.browser_profile_dir()
    assert result == tmp_path / "xdg-state" / "tumbl4" / "browser_profile"


def test_dedup_db_is_under_data_dir(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    monkeypatch.setenv("XDG_DATA_HOME", str(tmp_path / "xdg-data"))
    result = paths.dedup_db()
    assert result == tmp_path / "xdg-data" / "tumbl4" / "dedup.db"
```

- [ ] **Step 2: Run the test to verify it fails**

Run:
```bash
uv run pytest tests/unit/test_paths.py -v
```

Expected: all tests FAIL with `ModuleNotFoundError: No module named 'tumbl4._internal.paths'`.

- [ ] **Step 3: Write the paths module**

Write file `src/tumbl4/_internal/paths.py`:

```python
"""XDG Base Directory Specification-compliant paths for tumbl4.

On Linux we follow XDG strictly. On macOS we also honor XDG env vars when set
(so a user can opt in to Linux-style locations), but the defaults fall back to
macOS-native `~/Library/...` conventions when no XDG env vars are present.

All functions return `Path` objects. Callers are responsible for creating
directories on first use — these functions only compute paths, they never
touch the filesystem.

See spec §5.1 (config precedence) and §6.6 (credential storage locations).
"""

from __future__ import annotations

import os
import sys
from pathlib import Path

_APP_NAME = "tumbl4"


def _home() -> Path:
    return Path(os.environ.get("HOME", "~")).expanduser()


def config_dir() -> Path:
    """Return the directory where tumbl4 looks for its user config file.

    Resolution order:
        1. $XDG_CONFIG_HOME/tumbl4
        2. (macOS only, if XDG unset) ~/Library/Application Support/tumbl4
        3. ~/.config/tumbl4
    """
    xdg = os.environ.get("XDG_CONFIG_HOME")
    if xdg:
        return Path(xdg) / _APP_NAME
    if sys.platform == "darwin":
        return _home() / "Library" / "Application Support" / _APP_NAME
    return _home() / ".config" / _APP_NAME


def state_dir() -> Path:
    """Return the directory where tumbl4 stores runtime state (sessions, cursors).

    Resolution order:
        1. $XDG_STATE_HOME/tumbl4
        2. (macOS only, if XDG unset) ~/Library/Application Support/tumbl4/state
        3. ~/.local/state/tumbl4
    """
    xdg = os.environ.get("XDG_STATE_HOME")
    if xdg:
        return Path(xdg) / _APP_NAME
    if sys.platform == "darwin":
        return _home() / "Library" / "Application Support" / _APP_NAME / "state"
    return _home() / ".local" / "state" / _APP_NAME


def data_dir() -> Path:
    """Return the directory where tumbl4 stores long-lived data (dedup DB).

    Resolution order:
        1. $XDG_DATA_HOME/tumbl4
        2. (macOS only, if XDG unset) ~/Library/Application Support/tumbl4/data
        3. ~/.local/share/tumbl4
    """
    xdg = os.environ.get("XDG_DATA_HOME")
    if xdg:
        return Path(xdg) / _APP_NAME
    if sys.platform == "darwin":
        return _home() / "Library" / "Application Support" / _APP_NAME / "data"
    return _home() / ".local" / "share" / _APP_NAME


def playwright_state_file() -> Path:
    """Return the path to the Playwright storage_state JSON (chmod 0600 on write)."""
    return state_dir() / "playwright_state.json"


def browser_profile_dir() -> Path:
    """Return the path to the Playwright browser profile directory (chmod 0700)."""
    return state_dir() / "browser_profile"


def dedup_db() -> Path:
    """Return the path to the shared cross-blog dedup SQLite database."""
    return data_dir() / "dedup.db"
```

- [ ] **Step 4: Run the test to verify it passes**

Run:
```bash
uv run pytest tests/unit/test_paths.py -v
```

Expected: all tests PASS (7 tests).

- [ ] **Step 5: Stage and commit**

Run:
```bash
git add src/tumbl4/_internal/paths.py tests/unit/test_paths.py
git commit -m "$(cat <<'EOF'
feat(_internal): add XDG base directory resolver

tumbl4._internal.paths provides config_dir(), state_dir(), data_dir(),
playwright_state_file(), browser_profile_dir(), and dedup_db() — all
pure computations returning Path objects. On Linux, strict XDG.
On macOS, XDG env vars are honored when set, else fall back to
~/Library/Application Support/tumbl4 for config and state.

Matches spec §6.6 for credential storage locations.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 8: Supervised task spawn helper

**Files:**
- Test: `tests/unit/test_tasks.py`
- Create: `src/tumbl4/_internal/tasks.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_tasks.py`:

```python
"""Tests for tumbl4._internal.tasks.spawn — supervised asyncio task creation."""

from __future__ import annotations

import asyncio
import logging

import pytest

from tumbl4._internal import tasks


async def _ok_coro() -> str:
    await asyncio.sleep(0)
    return "hello"


async def _raising_coro() -> None:
    await asyncio.sleep(0)
    raise RuntimeError("synthetic failure")


# No @pytest.mark.asyncio needed — pyproject.toml sets asyncio_mode = "auto",
# which auto-decorates every `async def test_*` in the suite.


async def test_spawn_returns_task_and_tracks_it() -> None:
    task = tasks.spawn(_ok_coro())
    assert isinstance(task, asyncio.Task)
    # The task should be in the tracked set while still running.
    assert task in tasks._live_tasks()
    result = await task
    assert result == "hello"
    # After completion, supervision removes it from the tracked set.
    await asyncio.sleep(0)  # let done-callback run
    assert task not in tasks._live_tasks()


async def test_spawn_logs_exception_from_failing_task(
    caplog: pytest.LogCaptureFixture,
) -> None:
    caplog.set_level(logging.ERROR, logger="tumbl4._internal.tasks")
    task = tasks.spawn(_raising_coro())
    with pytest.raises(RuntimeError, match="synthetic failure"):
        await task
    await asyncio.sleep(0)  # let done-callback run
    # Supervision should have logged the exception via the done-callback.
    assert any(
        "synthetic failure" in record.getMessage() for record in caplog.records
    ), f"expected error log containing 'synthetic failure', got {[r.getMessage() for r in caplog.records]}"


async def test_spawn_handles_cancellation_without_logging_as_error(
    caplog: pytest.LogCaptureFixture,
) -> None:
    caplog.set_level(logging.ERROR, logger="tumbl4._internal.tasks")

    async def long_coro() -> None:
        await asyncio.sleep(10)

    task = tasks.spawn(long_coro())
    task.cancel()
    with pytest.raises(asyncio.CancelledError):
        await task
    await asyncio.sleep(0)
    # Cancellation is expected, not a surprise; do not log as error.
    assert not any(
        record.levelno == logging.ERROR for record in caplog.records
    ), "cancellation should not produce an error log"
```

- [ ] **Step 2: Run the test to verify it fails**

Run:
```bash
uv run pytest tests/unit/test_tasks.py -v
```

Expected: all tests FAIL with `ModuleNotFoundError: No module named 'tumbl4._internal.tasks'`.

- [ ] **Step 3: Write the tasks helper**

Write file `src/tumbl4/_internal/tasks.py`:

```python
"""Supervised asyncio task spawning for tumbl4.

Spec §6.11 requires that every `asyncio.create_task()` call in tumbl4 goes
through this helper. The helper:

  1. Creates the task via `asyncio.create_task()`.
  2. Adds the task to a module-level tracked set so it is not garbage-collected
     mid-run (a classic asyncio footgun).
  3. Attaches a done-callback that removes the task from the set and logs any
     exception — except `CancelledError`, which is expected on cooperative
     cancel and must not be logged as an error.

Contributors: use `spawn()` instead of raw `asyncio.create_task()`. The ruff
rule `RUF006` enforces this at lint time.
"""

from __future__ import annotations

import asyncio
import logging
from collections.abc import Coroutine
from typing import Any, TypeVar

_LOG = logging.getLogger(__name__)

_T = TypeVar("_T")

_tracked: set[asyncio.Task[Any]] = set()


def spawn(coro: Coroutine[Any, Any, _T], *, name: str | None = None) -> asyncio.Task[_T]:
    """Create and supervise an asyncio task.

    Args:
        coro: The coroutine to run as a task.
        name: Optional task name (propagated to asyncio.Task for debug output).

    Returns:
        The created task. The task is tracked internally until it finishes;
        callers may still `await` it directly if they care about its result.
    """
    task: asyncio.Task[_T] = asyncio.create_task(coro, name=name)
    _tracked.add(task)
    task.add_done_callback(_on_task_done)
    return task


def _on_task_done(task: asyncio.Task[Any]) -> None:
    _tracked.discard(task)
    if task.cancelled():
        return
    exc = task.exception()
    if exc is None:
        return
    _LOG.error(
        "supervised task %r failed with unhandled exception: %s",
        task.get_name(),
        exc,
        exc_info=exc,
    )


def _live_tasks() -> frozenset[asyncio.Task[Any]]:
    """Test-only view of currently tracked tasks.

    Not part of the public API. Used by tests/unit/test_tasks.py to assert
    tracking behavior.
    """
    return frozenset(_tracked)
```

- [ ] **Step 4: Run the test to verify it passes**

Run:
```bash
uv run pytest tests/unit/test_tasks.py -v
```

Expected: all three tests PASS.

- [ ] **Step 5: Stage and commit**

Run:
```bash
git add src/tumbl4/_internal/tasks.py tests/unit/test_tasks.py
git commit -m "$(cat <<'EOF'
feat(_internal): add supervised task spawn helper

tumbl4._internal.tasks.spawn() wraps asyncio.create_task() with a
tracked set + done-callback that logs unhandled exceptions and removes
the task from the set on completion. Cancellation is not logged as an
error (cooperative cancel is expected). Contributors must use spawn()
instead of raw create_task; ruff rule RUF006 enforces at lint time
(wired up in a later task).

Matches spec §6.11 async exception hygiene.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 9: SecretFilter + logger factory

**Files:**
- Test: `tests/unit/test_logging.py`
- Create: `src/tumbl4/_internal/logging.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_logging.py`:

```python
"""Tests for tumbl4._internal.logging — SecretFilter and logger factory."""

from __future__ import annotations

import logging

import pytest

from tumbl4._internal import logging as tumbl4_logging


def _make_logger_with_capture() -> tuple[logging.Logger, list[logging.LogRecord]]:
    logger = logging.getLogger("tumbl4.test.secretfilter")
    logger.handlers.clear()
    logger.setLevel(logging.DEBUG)
    captured: list[logging.LogRecord] = []

    class CaptureHandler(logging.Handler):
        def emit(self, record: logging.LogRecord) -> None:
            captured.append(record)

    handler = CaptureHandler()
    handler.addFilter(tumbl4_logging.SecretFilter())
    logger.addHandler(handler)
    return logger, captured


def test_secret_filter_redacts_tumblr_cookie_in_formatted_message() -> None:
    logger, captured = _make_logger_with_capture()
    logger.info("got cookie tumblr_b=abcd1234ef")
    assert len(captured) == 1
    msg = captured[0].getMessage()
    assert "abcd1234ef" not in msg
    assert "[REDACTED]" in msg


def test_secret_filter_redacts_bearer_token_in_formatted_message() -> None:
    logger, captured = _make_logger_with_capture()
    logger.info("auth header: Bearer eyJhbGciOiJIUzI1NiJ9")
    msg = captured[0].getMessage()
    assert "eyJhbGciOiJIUzI1NiJ9" not in msg
    assert "[REDACTED]" in msg


def test_secret_filter_redacts_structured_extra_cookie_field() -> None:
    logger, captured = _make_logger_with_capture()
    logger.info("request sent", extra={"cookie": "tumblr_b=abcd1234ef"})
    record = captured[0]
    # The extra attribute should be redacted on the record.
    assert getattr(record, "cookie", None) == "[REDACTED]"


def test_secret_filter_redacts_nested_structured_extra() -> None:
    logger, captured = _make_logger_with_capture()
    logger.info(
        "request sent",
        extra={"headers": {"Cookie": "tumblr_b=sensitive", "User-Agent": "tumbl4/0.1"}},
    )
    record = captured[0]
    headers = getattr(record, "headers", None)
    assert headers is not None
    assert headers["Cookie"] == "[REDACTED]"
    # Non-sensitive keys are preserved.
    assert headers["User-Agent"] == "tumbl4/0.1"


def test_secret_filter_is_idempotent() -> None:
    logger, captured = _make_logger_with_capture()
    logger.info("Cookie: tumblr_b=first Cookie: tumblr_b=second")
    msg = captured[0].getMessage()
    assert "first" not in msg
    assert "second" not in msg
    assert msg.count("[REDACTED]") >= 2


def test_secret_filter_redacts_exception_traceback(
    caplog: pytest.LogCaptureFixture,
) -> None:
    # Use the pytest caplog fixture with our filter explicitly installed.
    caplog.handler.addFilter(tumbl4_logging.SecretFilter())
    logger = logging.getLogger("tumbl4.test.traceback")
    logger.setLevel(logging.DEBUG)
    logger.propagate = True

    cookie_value = "tumblr_b=super_secret_abc"  # noqa: S105 — test fixture
    try:
        raise RuntimeError(f"failed while handling {cookie_value}")
    except RuntimeError:
        logger.exception("something went wrong")

    records = [r for r in caplog.records if r.name == "tumbl4.test.traceback"]
    assert records, "expected a log record from tumbl4.test.traceback"
    rec = records[-1]
    # Message and exception text should both be scrubbed.
    assert "super_secret_abc" not in rec.getMessage()
    if rec.exc_text:
        assert "super_secret_abc" not in rec.exc_text


def test_get_logger_returns_a_tumbl4_child_logger() -> None:
    log = tumbl4_logging.get_logger("auth.session")
    assert log.name == "tumbl4.auth.session"
```

- [ ] **Step 2: Run the test to verify it fails**

Run:
```bash
uv run pytest tests/unit/test_logging.py -v
```

Expected: all tests FAIL with `ModuleNotFoundError: No module named 'tumbl4._internal.logging'`.

- [ ] **Step 3: Write the logging module**

Write file `src/tumbl4/_internal/logging.py`:

```python
"""Logging setup for tumbl4 with a SecretFilter that scrubs credentials.

Per spec §6.2, the SecretFilter intercepts THREE content paths:
    1. Formatted message strings (regex match on known credential patterns)
    2. Structured `extra` dict fields whose keys match a sensitive-key set
    3. Exception traceback bodies attached to LogRecord.exc_text

The filter is installed on every logger returned by `get_logger()`. For the
third path, we also scrub `record.exc_text` after Python formats the traceback.
"""

from __future__ import annotations

import logging
import re
from typing import Any, Final

# Patterns that indicate a secret on the wire. Each pattern is a regex that
# captures the value-portion in group 1; the filter substitutes group 1 with
# [REDACTED] while preserving the surrounding context for debuggability.
_SECRET_PATTERNS: Final[tuple[re.Pattern[str], ...]] = (
    re.compile(r"(tumblr_[a-zA-Z0-9_]+=[^\s;,&]+)"),
    re.compile(r"(Bearer\s+[A-Za-z0-9._\-~+/]+=*)", re.IGNORECASE),
    re.compile(r"(Cookie:\s*[^\r\n]+)", re.IGNORECASE),
    re.compile(r"(Authorization:\s*[^\r\n]+)", re.IGNORECASE),
    re.compile(r"(Set-Cookie:\s*[^\r\n]+)", re.IGNORECASE),
)

# Key names in structured `extra` dicts whose values should be redacted
# regardless of content. Matched case-insensitively.
_SENSITIVE_EXTRA_KEYS: Final[frozenset[str]] = frozenset({
    "cookie",
    "cookies",
    "token",
    "session",
    "authorization",
    "auth",
    "secret",
    "password",
    "bearer",
    "api_key",
    "apikey",
    "headers",  # redact known-sensitive sub-keys inside a headers dict
})

_REDACTED: Final[str] = "[REDACTED]"


# The fields Python's logging module sets on every LogRecord. Anything outside
# this set is user-supplied `extra` data that we scan for secrets. Defined
# before SecretFilter so pyright strict doesn't flag a forward reference.
_STANDARD_LOGRECORD_ATTRS: Final[frozenset[str]] = frozenset({
    "args",
    "asctime",
    "created",
    "exc_info",
    "exc_text",
    "filename",
    "funcName",
    "levelname",
    "levelno",
    "lineno",
    "message",
    "module",
    "msecs",
    "msg",
    "name",
    "pathname",
    "process",
    "processName",
    "relativeCreated",
    "stack_info",
    "thread",
    "threadName",
    "taskName",
})


def _redact_string(text: str) -> str:
    """Apply all secret regexes to a string, substituting matches with [REDACTED]."""
    for pattern in _SECRET_PATTERNS:
        text = pattern.sub(_REDACTED, text)
    return text


def _redact_value(key: str, value: Any) -> Any:
    """Redact a single extra-field value based on its key name and shape."""
    if isinstance(value, dict):
        return {k: _redact_value(k, v) for k, v in value.items()}
    if isinstance(value, (list, tuple)):
        return type(value)(_redact_value(key, v) for v in value)
    if key.lower() in _SENSITIVE_EXTRA_KEYS:
        return _REDACTED
    if isinstance(value, str):
        return _redact_string(value)
    return value


class SecretFilter(logging.Filter):
    """Redact credentials from log records in all three surfaces (msg, extra, exc_text).

    Install on a handler (not a logger) so that every record flowing to that
    handler is scrubbed, regardless of which logger produced it.
    """

    def filter(self, record: logging.LogRecord) -> bool:  # noqa: A003 — logging API
        # 1. Scrub the formatted message. We do this by replacing `msg` so
        #    downstream getMessage() calls see the redacted text. When `args`
        #    are present we first format them, then blank args, so the final
        #    message string is what we've redacted.
        try:
            formatted = record.getMessage()
        except Exception:  # pragma: no cover — defensive
            formatted = str(record.msg)
        record.msg = _redact_string(formatted)
        record.args = None

        # 2. Scrub structured extra dict fields attached to the record.
        for key in list(record.__dict__.keys()):
            if key in _STANDARD_LOGRECORD_ATTRS:
                continue
            record.__dict__[key] = _redact_value(key, record.__dict__[key])

        # 3. Scrub any already-formatted exception text.
        if record.exc_text:
            record.exc_text = _redact_string(record.exc_text)

        return True


def get_logger(name: str) -> logging.Logger:
    """Return a logger named `tumbl4.{name}`, suitable for use in tumbl4 modules.

    Example:
        log = get_logger("auth.session")  # produces logger "tumbl4.auth.session"
    """
    return logging.getLogger(f"tumbl4.{name}")
```

- [ ] **Step 4: Run the test to verify it passes**

Run:
```bash
uv run pytest tests/unit/test_logging.py -v
```

Expected: all seven tests PASS.

- [ ] **Step 5: Stage and commit**

Run:
```bash
git add src/tumbl4/_internal/logging.py tests/unit/test_logging.py
git commit -m "$(cat <<'EOF'
feat(_internal): add SecretFilter + get_logger factory

SecretFilter scrubs credentials from three surfaces (spec §6.2):
  1. Formatted message strings via regex for tumblr_* cookies, Bearer
     tokens, and Cookie/Authorization/Set-Cookie headers
  2. Structured `extra` dict fields with sensitive key names, including
     nested dicts (e.g., extra={"headers": {"Cookie": ...}})
  3. Already-formatted exception traceback bodies (record.exc_text)

get_logger(name) returns a logger namespaced under "tumbl4.{name}".

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 10: CLI app.py with Typer and --version

**Files:**
- Test: `tests/unit/test_cli_version.py`
- Create: `src/tumbl4/cli/app.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_cli_version.py`:

```python
"""Tests for tumbl4 CLI --version flag."""

from __future__ import annotations

from typer.testing import CliRunner

import tumbl4
from tumbl4.cli.app import app


runner = CliRunner()


def test_version_flag_prints_version_and_exits_zero() -> None:
    result = runner.invoke(app, ["--version"])
    assert result.exit_code == 0
    assert tumbl4.__version__ in result.stdout


def test_version_flag_short_form_prints_version() -> None:
    result = runner.invoke(app, ["-V"])
    assert result.exit_code == 0
    assert tumbl4.__version__ in result.stdout


def test_help_flag_exits_zero() -> None:
    result = runner.invoke(app, ["--help"])
    assert result.exit_code == 0
    assert "Usage:" in result.stdout


def test_no_args_shows_help_without_crashing() -> None:
    # With no subcommands registered yet, running bare `tumbl4` should not crash.
    # Typer's `no_args_is_help=True` historically exits with code 0 on some
    # versions and code 2 on others (Click-ish semantics). Both are acceptable
    # outcomes for Plan 1 — what we care about is that the output contains
    # "tumbl4" and that the process exits cleanly (not a stack trace).
    result = runner.invoke(app, [])
    assert result.exit_code in (0, 2), (
        f"unexpected exit code {result.exit_code}; output: {result.output!r}"
    )
    assert "tumbl4" in result.output or "Usage:" in result.output
```

- [ ] **Step 2: Run the test to verify it fails**

Run:
```bash
uv run pytest tests/unit/test_cli_version.py -v
```

Expected: all tests FAIL with `ModuleNotFoundError: No module named 'tumbl4.cli.app'`.

- [ ] **Step 3: Write the CLI app**

Write file `src/tumbl4/cli/app.py`:

```python
"""Top-level Typer application for tumbl4.

This module exposes `app` (the Typer instance) and `main()` (the function
referenced by the `tumbl4` console script in pyproject.toml).

Later plans add subcommands (download, login, logout, list, config, status,
sweep) under `src/tumbl4/cli/commands/`. In Plan 1 we only wire up --version
and --help so the CLI entry point is installable and testable.
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
    # The callback body is only reached when no --version flag was given.
    # We currently have no global options beyond --version, so this is a no-op.
    return None


def main() -> None:
    """Console-script entry point referenced from pyproject.toml."""
    app()
```

- [ ] **Step 4: Run the test to verify it passes**

Run:
```bash
uv run pytest tests/unit/test_cli_version.py -v
```

Expected: all four tests PASS. If `test_no_args_exits_with_help_and_zero` fails because Typer exits with code 2 on missing command, adjust the `no_args_is_help=True` path and re-run until green.

- [ ] **Step 5: Verify the installed command works**

Run:
```bash
uv run tumbl4 --version
```

Expected output:
```
tumbl4 0.1.0
```

Run:
```bash
uv run python -m tumbl4 --version
```

Expected output:
```
tumbl4 0.1.0
```

- [ ] **Step 6: Stage and commit**

Run:
```bash
git add src/tumbl4/cli/app.py tests/unit/test_cli_version.py
git commit -m "$(cat <<'EOF'
feat(cli): add Typer app with --version/-V flag

tumbl4.cli.app.app is the root Typer instance; main() is the console-script
entry point referenced from pyproject.toml. The --version callback prints
"tumbl4 {version}" and exits cleanly. `python -m tumbl4 --version` also
works via src/tumbl4/__main__.py.

Subcommands (download, login, logout, list, config, status, sweep) are
added in later plans.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 11: Import discipline test — ban tumbl4.cli imports from tumbl4.core

**Files:**
- Test: `tests/unit/test_import_boundaries.py`

This task adds a structural test that complements the ruff `flake8-tidy-imports` rule already configured in `pyproject.toml`. The ruff rule catches violations at lint time; this test catches them at test time too, because some CI setups run tests but skip lint (we run both, but defense in depth is cheap).

- [ ] **Step 1: Write the test**

Write file `tests/unit/test_import_boundaries.py`:

```python
"""Structural test: tumbl4.core must not import from tumbl4.cli.

Spec §3 core property: "Unidirectional dependencies — cli → core.orchestrator
→ core.* → models. Nothing imports upward." Enforced by ruff's
flake8-tidy-imports at lint time; this test catches it at test time as a
defense-in-depth check.
"""

from __future__ import annotations

import ast
from pathlib import Path


def _repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def _iter_python_files(root: Path) -> list[Path]:
    return sorted(root.rglob("*.py"))


def _module_imports(source: str) -> set[str]:
    """Return every fully-qualified module name imported by this source file."""
    try:
        tree = ast.parse(source)
    except SyntaxError:
        return set()
    names: set[str] = set()
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            for alias in node.names:
                names.add(alias.name)
        elif isinstance(node, ast.ImportFrom) and node.module:
            names.add(node.module)
    return names


def test_core_does_not_import_from_cli() -> None:
    core_dir = _repo_root() / "src" / "tumbl4" / "core"
    if not core_dir.exists():
        # In Plan 1 core/ is just a marker __init__.py. The test still runs
        # and passes trivially. Later plans add modules under core/.
        return
    violations: list[tuple[Path, str]] = []
    for path in _iter_python_files(core_dir):
        source = path.read_text(encoding="utf-8")
        for imported in _module_imports(source):
            if imported.startswith("tumbl4.cli"):
                violations.append((path, imported))
    assert not violations, (
        "tumbl4.core modules must not import from tumbl4.cli; "
        f"violations: {violations}"
    )


def test_models_does_not_import_from_cli_or_core() -> None:
    """Models are the shared vocabulary — they import from nothing in tumbl4."""
    models_dir = _repo_root() / "src" / "tumbl4" / "models"
    if not models_dir.exists():
        return
    violations: list[tuple[Path, str]] = []
    for path in _iter_python_files(models_dir):
        source = path.read_text(encoding="utf-8")
        for imported in _module_imports(source):
            if imported.startswith("tumbl4.cli") or imported.startswith("tumbl4.core"):
                violations.append((path, imported))
    assert not violations, (
        "tumbl4.models must not import from cli or core; "
        f"violations: {violations}"
    )
```

- [ ] **Step 2: Run the test**

Run:
```bash
uv run pytest tests/unit/test_import_boundaries.py -v
```

Expected: both tests PASS (there are no violations because core/ is empty-ish and models/ only has the Settings module, which doesn't import cli/core).

- [ ] **Step 3: Stage and commit**

Run:
```bash
git add tests/unit/test_import_boundaries.py
git commit -m "$(cat <<'EOF'
test: add structural test for import boundaries

Defense-in-depth check that tumbl4.core modules do not import from
tumbl4.cli, and tumbl4.models does not import from cli or core.
Complements the ruff flake8-tidy-imports rule already in pyproject.toml,
so that CI setups running tests without lint still catch violations.

Matches spec §3 unidirectional-dependency property.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 12: Full test suite + quality gates run locally

**Files:**
- No new files. This is a verification task.

- [ ] **Step 1: Run the full test suite**

Run:
```bash
uv run pytest -v
```

Expected: all tests pass. Count should be roughly:
- `test_version.py`: 2
- `test_settings.py`: 4
- `test_paths.py`: 7
- `test_tasks.py`: 3
- `test_logging.py`: 7
- `test_cli_version.py`: 4
- `test_import_boundaries.py`: 2

Total: ~29 tests PASSED.

If any fail, fix them before proceeding. Do not commit a broken state.

- [ ] **Step 2: Run ruff check**

Run:
```bash
uv run ruff check .
```

Expected: no violations. If any appear, fix them (do not use `--fix` blindly — read each and decide) and re-run. Commit any fixes as a separate commit with message `style: fix ruff violations`.

- [ ] **Step 3: Run ruff format --check**

Run:
```bash
uv run ruff format --check .
```

Expected: no formatting issues. If any appear, run `uv run ruff format .` and commit with `style: ruff format`.

- [ ] **Step 4: Run pyright**

Run:
```bash
uv run pyright
```

Expected: 0 errors, 0 warnings. If warnings appear from pyright's default-loose scanning of tests, they're acceptable — errors are not.

If errors appear, fix them before proceeding.

- [ ] **Step 5: Run coverage report**

Run:
```bash
uv run pytest --cov=tumbl4 --cov-report=term-missing
```

Expected: coverage on `src/tumbl4/` modules that exist (models, _internal, cli). Rough target for Plan 1: ≥85% on the files we've touched. If a specific module is under-covered, add missing unit tests before moving on.

- [ ] **Step 6: Commit any quality-gate fixes**

If steps 2-4 produced any fixes, stage and commit them. If everything was already clean, skip this step.

---

## Task 13: GitHub Actions CI workflow

**Files:**
- Create: `.github/workflows/ci.yml`

- [ ] **Step 1: Write the CI workflow**

Write file `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [master, main]
  pull_request:
    branches: [master, main]

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

jobs:
  test:
    name: test (${{ matrix.os }}, py${{ matrix.python-version }})
    runs-on: ${{ matrix.os }}
    strategy:
      fail-fast: false
      matrix:
        os: [ubuntu-latest, macos-latest]
        python-version: ["3.11", "3.12", "3.13"]
        exclude:
          # Drop py3.11 × macOS to save CI time; we run it on Linux.
          - os: macos-latest
            python-version: "3.11"

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Install uv
        uses: astral-sh/setup-uv@v3
        with:
          enable-cache: true
          cache-dependency-glob: "uv.lock"

      - name: Set up Python
        run: uv python install ${{ matrix.python-version }}

      - name: Install dependencies
        run: uv sync --dev --frozen

      - name: Ruff lint
        run: uv run ruff check .

      - name: Ruff format check
        run: uv run ruff format --check .

      - name: Pyright type check
        run: uv run pyright

      - name: Pytest
        run: uv run pytest --cov=tumbl4 --cov-report=xml --cov-report=term -v

      - name: Upload coverage
        if: matrix.os == 'ubuntu-latest' && matrix.python-version == '3.12'
        uses: actions/upload-artifact@v4
        with:
          name: coverage-report
          path: coverage.xml
          retention-days: 14
```

- [ ] **Step 2: Verify YAML syntax locally**

Run:
```bash
uv run python -c "import yaml; yaml.safe_load(open('.github/workflows/ci.yml'))"
```

Expected: no output (successful parse). If a `ScannerError` or `ParserError` appears, fix the indentation or quoting and re-run.

- [ ] **Step 3: Stage and commit**

Run:
```bash
git add .github/workflows/ci.yml
git commit -m "$(cat <<'EOF'
ci: add GitHub Actions workflow for lint + type + test matrix

Runs on push and PR to master. Matrix: {ubuntu-latest, macos-latest} ×
{3.11, 3.12, 3.13} with macOS × 3.11 excluded to save minutes. Steps:
checkout, install uv, install Python, uv sync --dev --frozen, ruff
check + format check, pyright, pytest with coverage. Coverage XML is
uploaded as an artifact from the ubuntu × 3.12 job.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 14: README.md (quickstart)

**Files:**
- Create: `README.md` (overwriting the existing upstream README)

- [ ] **Step 1: Remove the old upstream README (it's still checked in as the TumblThree README)**

Run:
```bash
git rm README.md
```

Expected: README.md is removed from the index. (The file will be recreated in the next step.)

- [ ] **Step 2: Write the new README.md**

Write file `README.md`:

````markdown
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
````

- [ ] **Step 3: Stage and commit**

Run:
```bash
git add README.md
git commit -m "$(cat <<'EOF'
docs: rewrite README.md for tumbl4

Replaces the upstream TumblThree README (which described the Windows WPF
app) with a tumbl4-focused quickstart covering install, Plan 1 status,
upcoming plans, and dev setup. Links to the authoritative design spec.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 15: CHANGELOG.md skeleton

**Files:**
- Create: `CHANGELOG.md`

- [ ] **Step 1: Write CHANGELOG.md**

Write file `CHANGELOG.md`:

```markdown
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
- Test suite with ~29 tests across unit/ covering version, settings, paths, tasks, logging, CLI version, and import-boundary structural checks
- GitHub Actions CI workflow: {macos-latest, ubuntu-latest} × {3.11, 3.12, 3.13} (excluding macOS × 3.11), lint + format + type + test with coverage
- MIT license preserved from upstream TumblThree; upstream C# changelog archived at `docs/CHANGELOG-upstream.md`

### Removed

- Entire upstream C# codebase (`src/TumblThree/`, `lib/`, Windows-only `scripts/` and `appveyor.yml`)
- Old `Contributing.md` and `.gitattributes` (will be rewritten for Python)
```

- [ ] **Step 2: Stage and commit**

Run:
```bash
git add CHANGELOG.md
git commit -m "$(cat <<'EOF'
docs: add CHANGELOG.md with Plan 1 Unreleased entries

Keep-a-Changelog format. Documents the Plan 1 scope: scaffolding,
Settings model, paths, task supervision, SecretFilter, CLI --version,
test harness, and CI matrix. Notes the removal of upstream C# assets.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 16: Rename repo directory to `tumbl4` (final task)

This is the directory rename the user requested. It's last because all prior tasks need the old path to work. After this task, the working directory is `/Users/claire/Github/tumbl4`.

**Files:**
- Rename: `/Users/claire/Github/TumblThreeMac` → `/Users/claire/Github/tumbl4`
- Update: no files inside the repo change — only the parent directory name

- [ ] **Step 1: Ensure the working tree is clean**

Run:
```bash
git status --short
```

Expected: empty output (nothing staged, nothing modified). If not, commit or stash first.

- [ ] **Step 2: Move out of the working directory before the rename**

Run:
```bash
cd /Users/claire/Github
```

Expected: working directory is now `/Users/claire/Github`.

- [ ] **Step 3: Perform the rename**

Run:
```bash
mv TumblThreeMac tumbl4
```

Expected: no output. Verify:
```bash
ls -d tumbl4 && ls -d TumblThreeMac 2>&1
```

Expected: `tumbl4` exists, `TumblThreeMac` returns `No such file or directory`.

- [ ] **Step 4: Move into the renamed directory and verify the repo still works**

Run:
```bash
cd /Users/claire/Github/tumbl4
git status
uv run tumbl4 --version
```

Expected:
- `git status` reports a clean tree on the same branch
- `tumbl4 --version` prints `tumbl4 0.1.0`

- [ ] **Step 5: (Deferred to user) Rename the GitHub remote repo**

The local rename is complete. To rename the GitHub repo to match:

```bash
gh repo rename tumbl4
```

This is a user-level action that requires GitHub authentication. It is **not** part of the automatic plan execution — the user runs it at a convenient time. After it's done, the `origin` remote URL updates automatically on the next push/pull.

- [ ] **Step 6: (No commit)**

The directory rename is filesystem-level; git doesn't track the parent directory name. There is nothing to commit. Plan 1 is complete.

---

## Plan 1 self-review checklist

Before handing off to execution, verify:

- [ ] Every task's "Files" section lists exact paths
- [ ] Every task has TDD steps (write test, fail, implement, pass, commit) or is a structural / tooling task explicitly marked as such
- [ ] Every code block is complete — no `...`, `TODO`, or `TBD` markers
- [ ] Commit messages are specific, not `update stuff`
- [ ] Later tasks reference symbols that earlier tasks created (Settings, get_logger, spawn, app, etc.) consistently
- [ ] The deliverable stated at the top is achievable by running the tasks in order

## Spec coverage check

Each spec requirement in scope for Plan 1 is satisfied by a task:

| Spec section | Task |
|---|---|
| §3 public API discipline (cli→core boundary) | Task 3 (ruff config) + Task 11 (structural test) |
| §4 project structure (src/ layout, py.typed) | Task 3 + Task 4 |
| §5.1 config precedence (env vars layer) | Task 6 (Settings) |
| §5.4 HTTP timeouts + response body cap | Task 6 (HttpSettings) |
| §6.2 logging hygiene (SecretFilter) | Task 9 |
| §6.6 credential storage paths | Task 7 (paths module) |
| §6.11 supervised task creation | Task 8 (tasks.spawn) |
| §7.6 quality gates (ruff, pyright, pytest, coverage) | Task 12 (local) + Task 13 (CI) |
| §7.7 CI matrix | Task 13 |
| §10 repo rename | Task 16 |

Items **not** in Plan 1 scope (addressed in later plans):
- Real crawlers, downloaders, parsers, orchestrator, state — all of §5 beyond 5.1 and 5.4
- Playwright auth flow — §6.6 only covers path locations here; the flow itself is Plan 5
- SLSA release workflow — Plan 6
- Docs beyond README + CHANGELOG — later plans

## Execution handoff

**Plan 1 complete and saved to `docs/superpowers/plans/2026-04-11-tumbl4-plan-01-foundation.md`.**

**Two execution options:**

1. **Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, two-stage peer review. Matches your PM + parallel + main-dev workflow preference.

2. **Inline Execution** — Execute tasks in this session using the executing-plans skill, batched checkpoints for your review.

**Which approach do you want?**
