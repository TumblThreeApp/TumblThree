# tumbl4 Plan 5: Auth + Hidden Blog Crawler

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Playwright-driven interactive login for dashboard/hidden blogs. `tumbl4 login` and `tumbl4 logout` subcommands. Authenticated hidden blog crawling via the SVC/NPF API with cursor-based pagination.

**Architecture:** The auth layer sits between the CLI and the crawl layer. `tumbl4 login` launches a headed Chromium browser via Playwright, the user logs in interactively, and cookies + bearer token are extracted and persisted to `playwright_state.json` (0600 perms). On `tumbl4 download`, the orchestrator auto-detects whether a blog is public or hidden; hidden blogs use `TumblrHiddenCrawler` which authenticates via `AuthSession` (httpx cookie jar + bearer token), fetches the blog's HTML page, extracts posts from `___INITIAL_STATE___` (two-shape: PeeprRoute vs response-wrapped), and paginates via cursor-based `next_link`. The bearer token and SVC API URL are extracted from `___INITIAL_STATE___` using `html_scrape.py` (from Plan 3).

**Tech Stack:** Python 3.12+, Playwright (Chromium, headed mode), httpx (async HTTP with cookie jar), aiolimiter, pydantic, SQLite, Rich, Typer.

**Builds on:** Plan 1 foundation (`Settings`, `get_logger()`, `SecretFilter`, `state_dir()`, `playwright_state_file()`, `browser_profile_dir()`), Plan 2 (`StateDb`, `resume.py`, `TumblrHttpClient`, orchestrator, `CrawlerProtocol`, `MediaTask`, `BlogRef`, CLI `app`), Plan 3 (`html_scrape.py` for `___INITIAL_STATE___` extraction, `svc_json.py` parser), Plan 4 (`crawl_state` table with `crawler_type` column).

**Plans in this series:**

| # | Plan | Deliverable |
|---|---|---|
| 1 | Foundation (shipped) | `tumbl4 --version`; tooling + CI green |
| 2 | MVP public blog photo crawl (shipped) | `tumbl4 download <blog>` downloads photos, resumable |
| 3 | All post types + sidecars + templates (shipped) | Every post type; configurable filename templates |
| 4 | Filters + dedup + pinned posts (shipped) | Tag/timespan filters; cross-blog dedup; pinned-post fix |
| **5** | **Auth + hidden blog crawler (this plan)** | **`tumbl4 login` + hidden/dashboard blog downloads** |
| 6 | Security hardening + release | Redirect safety, SSRF guards, signal handling, SLSA release |

**Spec references:**
- Design spec: `docs/superpowers/specs/2026-04-11-tumbl4-macos-cli-port-design.md` (sections 5.9, 6.6-6.8, 6.10)
- Plan boundaries: `docs/superpowers/specs/2026-04-11-tumbl4-plan-boundaries.md` (Plan 5)

---

## File Structure (Plan 5 additions)

New files are marked with `+`. Modified files are marked with `~`.

```
src/tumbl4/
├── __init__.py                              # (unchanged)
├── cli/
│   ├── app.py                            ~  # register login, logout subcommands
│   └── commands/
│       ├── __init__.py                   ~  # (unchanged)
│       ├── download.py                   ~  # add --hidden/--public flags, blog type detection
│       ├── login.py                      +  # tumbl4 login
│       └── logout.py                     +  # tumbl4 logout
├── core/
│   ├── __init__.py                       ~  # re-export auth types
│   ├── auth/
│   │   ├── __init__.py                   +
│   │   ├── playwright_login.py           +  # headed Chromium interactive login
│   │   ├── cookie_store.py               +  # persist/load playwright_state.json
│   │   └── session.py                    +  # AuthSession with httpx cookie jar
│   ├── crawl/
│   │   ├── tumblr_hidden.py              +  # hidden/dashboard crawler (SVC/NPF)
│   │   └── blog_detector.py              +  # auto-detect public vs hidden
│   └── errors.py                         ~  # add NoDisplay, SessionExpired, AuthError
tests/
├── conftest.py                           ~  # add auth fixtures
├── fixtures/
│   └── json/
│       ├── initial_state_peepr.json      +  # PeeprRoute shape ___INITIAL_STATE___
│       ├── initial_state_response.json   +  # response-wrapped shape ___INITIAL_STATE___
│       └── playwright_state.json         +  # mock Playwright storage state
├── unit/
│   ├── test_cookie_store.py              +
│   ├── test_auth_session.py              +
│   ├── test_playwright_login.py          +
│   ├── test_hidden_crawler.py            +
│   ├── test_blog_detector.py             +
│   ├── test_login_command.py             +
│   └── test_logout_command.py            +
└── component/
    └── test_hidden_pipeline.py           +  # end-to-end hidden crawl with mocked HTTP
```

---

## Task 1: Auth error types

**Files:**
- Modify: `src/tumbl4/core/errors.py`
- Create: `tests/unit/test_auth_errors.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_auth_errors.py`:

```python
"""Tests for auth-related exception types."""

from tumbl4.core.errors import (
    AuthError,
    CrawlError,
    NoDisplay,
    SessionExpired,
    Tumbl4Error,
)


class TestAuthErrors:
    def test_all_inherit_from_tumbl4_error(self) -> None:
        for exc_cls in [AuthError, NoDisplay, SessionExpired]:
            assert issubclass(exc_cls, Tumbl4Error)

    def test_auth_error_is_own_branch(self) -> None:
        assert issubclass(AuthError, Tumbl4Error)
        assert not issubclass(AuthError, CrawlError)

    def test_no_display_inherits_from_auth_error(self) -> None:
        assert issubclass(NoDisplay, AuthError)

    def test_session_expired_inherits_from_auth_error(self) -> None:
        assert issubclass(SessionExpired, AuthError)

    def test_no_display_message(self) -> None:
        exc = NoDisplay()
        msg = str(exc)
        assert "graphical environment" in msg.lower() or "display" in msg.lower()

    def test_session_expired_message(self) -> None:
        exc = SessionExpired("cookies expired after 24h")
        assert "expired" in str(exc).lower()

    def test_no_display_includes_help_text(self) -> None:
        exc = NoDisplay()
        msg = str(exc)
        assert "tumbl4 login" in msg or "playwright_state.json" in msg
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_auth_errors.py -v`
Expected: FAIL — `ImportError: cannot import name 'AuthError' from 'tumbl4.core.errors'`

- [ ] **Step 3: Write the implementation**

Add to `src/tumbl4/core/errors.py` after the existing `StateError` class:

```python
class AuthError(Tumbl4Error):
    """Authentication or session management error."""


class NoDisplay(AuthError):
    """No graphical display available for Playwright login.

    Raised when $DISPLAY and $WAYLAND_DISPLAY are both unset.
    See spec section 6.7.
    """

    def __init__(self) -> None:
        super().__init__(
            "tumbl4 login requires a graphical environment. Options:\n"
            "  1. Log in on a local machine, then copy playwright_state.json\n"
            "     to $XDG_STATE_HOME/tumbl4/ (chmod 0600)\n"
            "  2. Use X11 forwarding over SSH: ssh -X, then tumbl4 login"
        )


class SessionExpired(AuthError):
    """Stored session is no longer valid — re-login required."""
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_auth_errors.py -v`
Expected: 7 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/errors.py tests/unit/test_auth_errors.py
git commit -m "feat(auth): add AuthError, NoDisplay, SessionExpired exceptions

AuthError branch under Tumbl4Error for auth-layer errors.
NoDisplay raised when no graphical display is available (spec section 6.7).
SessionExpired raised when stored cookies/bearer token are stale.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Cookie store — persist and load Playwright state

**Files:**
- Create: `src/tumbl4/core/auth/__init__.py`
- Create: `src/tumbl4/core/auth/cookie_store.py`
- Create: `tests/fixtures/json/playwright_state.json`
- Create: `tests/unit/test_cookie_store.py`

- [ ] **Step 1: Create test fixture**

Write file `tests/fixtures/json/playwright_state.json`:

```json
{
    "cookies": [
        {
            "name": "pfg",
            "value": "abc123def456",
            "domain": ".tumblr.com",
            "path": "/",
            "httpOnly": false,
            "secure": true,
            "sameSite": "None",
            "expires": 1807633800
        },
        {
            "name": "pfs",
            "value": "xyz789",
            "domain": ".tumblr.com",
            "path": "/",
            "httpOnly": true,
            "secure": true,
            "sameSite": "None",
            "expires": 1807633800
        },
        {
            "name": "pfe",
            "value": "1807633800",
            "domain": ".tumblr.com",
            "path": "/",
            "httpOnly": false,
            "secure": true,
            "sameSite": "None",
            "expires": 1807633800
        },
        {
            "name": "logged_in",
            "value": "1",
            "domain": ".tumblr.com",
            "path": "/",
            "httpOnly": false,
            "secure": true,
            "sameSite": "None",
            "expires": 1807633800
        }
    ],
    "origins": []
}
```

- [ ] **Step 2: Write the failing test**

Write file `tests/unit/test_cookie_store.py`:

```python
"""Tests for Playwright cookie store — persist and load."""

import json
import os
import stat
from pathlib import Path

import pytest

from tumbl4.core.auth.cookie_store import (
    CookieData,
    delete_state,
    has_stored_session,
    load_state,
    save_state,
)
from tumbl4.core.errors import AuthError

FIXTURES = Path(__file__).parent.parent / "fixtures" / "json"


class TestSaveState:
    def test_writes_valid_json(self, tmp_path: Path) -> None:
        state_file = tmp_path / "playwright_state.json"
        data = CookieData(
            cookies=[{"name": "pfg", "value": "abc", "domain": ".tumblr.com"}],
            origins=[],
        )
        save_state(state_file, data)
        assert state_file.exists()
        parsed = json.loads(state_file.read_text())
        assert parsed["cookies"][0]["name"] == "pfg"

    def test_file_permissions_0600(self, tmp_path: Path) -> None:
        state_file = tmp_path / "playwright_state.json"
        data = CookieData(cookies=[], origins=[])
        save_state(state_file, data)
        mode = stat.S_IMODE(os.stat(state_file).st_mode)
        assert mode == 0o600

    def test_creates_parent_directory(self, tmp_path: Path) -> None:
        state_file = tmp_path / "nested" / "dir" / "playwright_state.json"
        data = CookieData(cookies=[], origins=[])
        save_state(state_file, data)
        assert state_file.exists()

    def test_overwrites_existing_file(self, tmp_path: Path) -> None:
        state_file = tmp_path / "playwright_state.json"
        data1 = CookieData(cookies=[{"name": "old"}], origins=[])
        data2 = CookieData(cookies=[{"name": "new"}], origins=[])
        save_state(state_file, data1)
        save_state(state_file, data2)
        parsed = json.loads(state_file.read_text())
        assert parsed["cookies"][0]["name"] == "new"

    def test_atomic_write_no_part_file_remains(self, tmp_path: Path) -> None:
        state_file = tmp_path / "playwright_state.json"
        data = CookieData(cookies=[], origins=[])
        save_state(state_file, data)
        part_files = list(tmp_path.glob("*.part"))
        assert len(part_files) == 0


class TestLoadState:
    def test_loads_fixture(self) -> None:
        data = load_state(FIXTURES / "playwright_state.json")
        assert len(data.cookies) == 4
        assert data.cookies[0]["name"] == "pfg"

    def test_loads_saved_state(self, tmp_path: Path) -> None:
        state_file = tmp_path / "playwright_state.json"
        original = CookieData(
            cookies=[{"name": "pfg", "value": "abc", "domain": ".tumblr.com"}],
            origins=[],
        )
        save_state(state_file, original)
        loaded = load_state(state_file)
        assert len(loaded.cookies) == 1
        assert loaded.cookies[0]["value"] == "abc"

    def test_missing_file_raises_auth_error(self, tmp_path: Path) -> None:
        with pytest.raises(AuthError, match="No stored session"):
            load_state(tmp_path / "nonexistent.json")

    def test_refuses_world_readable_file(self, tmp_path: Path) -> None:
        state_file = tmp_path / "playwright_state.json"
        data = CookieData(cookies=[], origins=[])
        save_state(state_file, data)
        # Broaden permissions
        os.chmod(state_file, 0o644)
        with pytest.raises(AuthError, match="permissions"):
            load_state(state_file)

    def test_refuses_group_readable_file(self, tmp_path: Path) -> None:
        state_file = tmp_path / "playwright_state.json"
        data = CookieData(cookies=[], origins=[])
        save_state(state_file, data)
        os.chmod(state_file, 0o640)
        with pytest.raises(AuthError, match="permissions"):
            load_state(state_file)

    def test_corrupt_json_raises_auth_error(self, tmp_path: Path) -> None:
        state_file = tmp_path / "playwright_state.json"
        state_file.write_text("{invalid json")
        os.chmod(state_file, 0o600)
        with pytest.raises(AuthError, match="corrupt"):
            load_state(state_file)


class TestHasStoredSession:
    def test_true_when_file_exists(self, tmp_path: Path) -> None:
        state_file = tmp_path / "playwright_state.json"
        data = CookieData(cookies=[{"name": "pfg"}], origins=[])
        save_state(state_file, data)
        assert has_stored_session(state_file) is True

    def test_false_when_no_file(self, tmp_path: Path) -> None:
        assert has_stored_session(tmp_path / "nonexistent.json") is False

    def test_false_when_empty_cookies(self, tmp_path: Path) -> None:
        state_file = tmp_path / "playwright_state.json"
        data = CookieData(cookies=[], origins=[])
        save_state(state_file, data)
        assert has_stored_session(state_file) is False


class TestDeleteState:
    def test_deletes_file(self, tmp_path: Path) -> None:
        state_file = tmp_path / "playwright_state.json"
        data = CookieData(cookies=[], origins=[])
        save_state(state_file, data)
        delete_state(state_file)
        assert not state_file.exists()

    def test_no_error_on_missing_file(self, tmp_path: Path) -> None:
        delete_state(tmp_path / "nonexistent.json")  # should not raise
```

- [ ] **Step 3: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_cookie_store.py -v`
Expected: FAIL — `ModuleNotFoundError: No module named 'tumbl4.core.auth'`

- [ ] **Step 4: Write the implementation**

Write file `src/tumbl4/core/auth/__init__.py`:

```python
"""Authentication layer — Playwright login, cookie persistence, session management."""
```

Write file `src/tumbl4/core/auth/cookie_store.py`:

```python
"""Persist and load Playwright storage state (cookies + origins).

The state file is written with 0600 permissions and verified on load.
If the file has broader permissions, loading is refused — the user must
fix permissions before tumbl4 will use the session.

See spec section 6.6.
"""

from __future__ import annotations

import json
import os
import stat
from dataclasses import dataclass, field
from pathlib import Path

from tumbl4._internal.logging import get_logger
from tumbl4.core.errors import AuthError

logger = get_logger(__name__)

_REQUIRED_PERMS = 0o600


@dataclass
class CookieData:
    """Parsed Playwright storage state."""

    cookies: list[dict[str, object]] = field(default_factory=list)
    origins: list[dict[str, object]] = field(default_factory=list)

    def to_dict(self) -> dict[str, object]:
        return {"cookies": self.cookies, "origins": self.origins}


def save_state(path: Path, data: CookieData) -> None:
    """Write Playwright state to disk atomically with 0600 permissions.

    Creates parent directories if needed.
    """
    path.parent.mkdir(parents=True, exist_ok=True)

    part_path = path.with_suffix(".json.part")

    fd = os.open(str(part_path), os.O_WRONLY | os.O_CREAT | os.O_TRUNC, _REQUIRED_PERMS)
    try:
        with os.fdopen(fd, "w") as f:
            json.dump(data.to_dict(), f, indent=2, ensure_ascii=False)
            f.write("\n")
    except BaseException:
        part_path.unlink(missing_ok=True)
        raise

    os.rename(part_path, path)
    logger.debug("saved playwright state", extra={"path": str(path)})


def load_state(path: Path) -> CookieData:
    """Load Playwright state from disk.

    Raises AuthError if the file is missing, has wrong permissions, or is corrupt.
    """
    if not path.exists():
        raise AuthError(f"No stored session found at {path}. Run `tumbl4 login` first.")

    # Verify permissions are not broader than 0600
    mode = stat.S_IMODE(os.stat(path).st_mode)
    if mode & (stat.S_IRGRP | stat.S_IWGRP | stat.S_IROTH | stat.S_IWOTH):
        raise AuthError(
            f"Session file {path} has permissions {oct(mode)} — "
            f"expected {oct(_REQUIRED_PERMS)}. Fix with: chmod 600 {path}"
        )

    try:
        raw = json.loads(path.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, UnicodeDecodeError) as e:
        raise AuthError(f"Session file {path} is corrupt: {e}") from e

    cookies = raw.get("cookies", [])
    origins = raw.get("origins", [])

    if not isinstance(cookies, list):
        raise AuthError(f"Session file {path} is corrupt: 'cookies' is not a list")

    return CookieData(cookies=cookies, origins=origins)


def has_stored_session(path: Path) -> bool:
    """Check if a stored session exists and has at least one cookie.

    Does not verify permissions — use load_state for full validation.
    """
    if not path.exists():
        return False
    try:
        raw = json.loads(path.read_text(encoding="utf-8"))
        cookies = raw.get("cookies", [])
        return isinstance(cookies, list) and len(cookies) > 0
    except (json.JSONDecodeError, UnicodeDecodeError, OSError):
        return False


def delete_state(path: Path) -> None:
    """Delete the stored session file. No-op if missing."""
    path.unlink(missing_ok=True)
    logger.debug("deleted playwright state", extra={"path": str(path)})
```

- [ ] **Step 5: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_cookie_store.py -v`
Expected: 15 passed

- [ ] **Step 6: Commit**

```bash
git add src/tumbl4/core/auth/__init__.py src/tumbl4/core/auth/cookie_store.py tests/fixtures/json/playwright_state.json tests/unit/test_cookie_store.py
git commit -m "feat(auth): add cookie store with 0600 permission enforcement

Atomic .part + rename writes, permission check on load (refuses
group/world-readable files), CookieData dataclass for type safety.
See spec section 6.6.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: AuthSession — httpx cookie jar + bearer token

**Files:**
- Create: `src/tumbl4/core/auth/session.py`
- Create: `tests/unit/test_auth_session.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_auth_session.py`:

```python
"""Tests for AuthSession — httpx cookie jar and bearer token management."""

import json
from pathlib import Path

import httpx
import pytest
import respx

from tumbl4.core.auth.cookie_store import CookieData, save_state
from tumbl4.core.auth.session import AuthSession, load_session
from tumbl4.core.errors import AuthError, SessionExpired


def _make_cookie_data() -> CookieData:
    return CookieData(
        cookies=[
            {
                "name": "pfg",
                "value": "abc123",
                "domain": ".tumblr.com",
                "path": "/",
                "httpOnly": False,
                "secure": True,
                "sameSite": "None",
                "expires": 1807633800,
            },
            {
                "name": "logged_in",
                "value": "1",
                "domain": ".tumblr.com",
                "path": "/",
                "httpOnly": False,
                "secure": True,
                "sameSite": "None",
                "expires": 1807633800,
            },
        ],
        origins=[],
    )


class TestAuthSession:
    def test_construction_with_cookies_and_token(self) -> None:
        session = AuthSession(
            cookies=_make_cookie_data(),
            bearer_token="test-bearer-token-123",
        )
        assert session.bearer_token == "test-bearer-token-123"
        assert session.is_authenticated is True

    def test_unauthenticated_session(self) -> None:
        session = AuthSession.unauthenticated()
        assert session.is_authenticated is False
        assert session.bearer_token is None

    def test_httpx_cookies_populated(self) -> None:
        session = AuthSession(
            cookies=_make_cookie_data(),
            bearer_token="token",
        )
        jar = session.httpx_cookies()
        # httpx.Cookies is a MutableMapping
        assert "pfg" in jar
        assert jar["pfg"] == "abc123"

    def test_auth_headers_include_bearer(self) -> None:
        session = AuthSession(
            cookies=_make_cookie_data(),
            bearer_token="my-bearer-token",
        )
        headers = session.auth_headers()
        assert headers["Authorization"] == "Bearer my-bearer-token"

    def test_auth_headers_unauthenticated_is_empty(self) -> None:
        session = AuthSession.unauthenticated()
        headers = session.auth_headers()
        assert "Authorization" not in headers

    def test_has_logged_in_cookie(self) -> None:
        session = AuthSession(
            cookies=_make_cookie_data(),
            bearer_token="token",
        )
        assert session.has_logged_in_cookie() is True

    def test_no_logged_in_cookie(self) -> None:
        data = CookieData(
            cookies=[{"name": "pfg", "value": "abc", "domain": ".tumblr.com"}],
            origins=[],
        )
        session = AuthSession(cookies=data, bearer_token="token")
        assert session.has_logged_in_cookie() is False


class TestLoadSession:
    def test_loads_from_cookie_store(self, tmp_path: Path) -> None:
        state_file = tmp_path / "playwright_state.json"
        save_state(state_file, _make_cookie_data())
        session = load_session(state_file)
        assert session.is_authenticated is True
        assert "pfg" in session.httpx_cookies()

    def test_missing_file_raises_auth_error(self, tmp_path: Path) -> None:
        with pytest.raises(AuthError, match="No stored session"):
            load_session(tmp_path / "nonexistent.json")

    def test_bearer_token_none_when_not_extracted(self, tmp_path: Path) -> None:
        state_file = tmp_path / "playwright_state.json"
        save_state(state_file, _make_cookie_data())
        session = load_session(state_file)
        # Bearer token is extracted later from ___INITIAL_STATE___, not from cookies
        assert session.bearer_token is None

    def test_set_bearer_token_after_load(self, tmp_path: Path) -> None:
        state_file = tmp_path / "playwright_state.json"
        save_state(state_file, _make_cookie_data())
        session = load_session(state_file)
        session.set_bearer_token("extracted-from-initial-state")
        assert session.bearer_token == "extracted-from-initial-state"
        assert session.auth_headers()["Authorization"] == "Bearer extracted-from-initial-state"
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_auth_session.py -v`
Expected: FAIL — `ModuleNotFoundError`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/auth/session.py`:

```python
"""AuthSession — authenticated session for hidden blog crawling.

Wraps Playwright cookies into an httpx-compatible cookie jar and manages
the bearer token extracted from ___INITIAL_STATE___. The bearer token is
NOT stored in playwright_state.json — it's extracted fresh from the blog's
HTML page on each crawl run.

See spec section 5.2 (CrawlContext.auth) and section 6.6.
"""

from __future__ import annotations

import httpx

from tumbl4._internal.logging import get_logger
from tumbl4.core.auth.cookie_store import CookieData, load_state
from tumbl4.core.errors import AuthError

logger = get_logger(__name__)

from pathlib import Path


class AuthSession:
    """Authenticated session carrying cookies and an optional bearer token."""

    def __init__(
        self,
        *,
        cookies: CookieData,
        bearer_token: str | None = None,
    ) -> None:
        self._cookies = cookies
        self._bearer_token = bearer_token

    @classmethod
    def unauthenticated(cls) -> AuthSession:
        """Create an unauthenticated session (no cookies, no token)."""
        return cls(cookies=CookieData(cookies=[], origins=[]))

    @property
    def is_authenticated(self) -> bool:
        """True if the session has at least one cookie."""
        return len(self._cookies.cookies) > 0

    @property
    def bearer_token(self) -> str | None:
        return self._bearer_token

    def set_bearer_token(self, token: str) -> None:
        """Set the bearer token after extraction from ___INITIAL_STATE___."""
        self._bearer_token = token
        logger.debug("bearer token set")

    def httpx_cookies(self) -> httpx.Cookies:
        """Convert Playwright cookies to an httpx.Cookies jar.

        Only includes cookies for .tumblr.com domains.
        """
        jar = httpx.Cookies()
        for cookie in self._cookies.cookies:
            name = str(cookie.get("name", ""))
            value = str(cookie.get("value", ""))
            domain = str(cookie.get("domain", ""))
            if name and value:
                jar.set(name, value, domain=domain)
        return jar

    def auth_headers(self) -> dict[str, str]:
        """Return headers dict with Authorization bearer if available."""
        headers: dict[str, str] = {}
        if self._bearer_token:
            headers["Authorization"] = f"Bearer {self._bearer_token}"
        return headers

    def has_logged_in_cookie(self) -> bool:
        """Check if the session has a 'logged_in' cookie from Tumblr."""
        return any(
            str(c.get("name", "")) == "logged_in"
            for c in self._cookies.cookies
        )


def load_session(state_file: Path) -> AuthSession:
    """Load a session from the Playwright state file.

    Bearer token is NOT loaded from the file — it is extracted from
    ___INITIAL_STATE___ during crawl and set via set_bearer_token().

    Raises:
        AuthError: if the state file is missing, has wrong permissions,
            or is corrupt.
    """
    data = load_state(state_file)
    logger.debug(
        "loaded session",
        extra={"cookie_count": len(data.cookies), "path": str(state_file)},
    )
    return AuthSession(cookies=data)
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_auth_session.py -v`
Expected: 11 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/auth/session.py tests/unit/test_auth_session.py
git commit -m "feat(auth): add AuthSession with httpx cookie jar and bearer token

Wraps Playwright cookies into httpx.Cookies, manages bearer token
(set after extraction from ___INITIAL_STATE___, not from stored state).
Unauthenticated factory for public-only crawls. See spec section 5.2.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: Playwright login flow (mocked)

**Files:**
- Create: `src/tumbl4/core/auth/playwright_login.py`
- Create: `tests/unit/test_playwright_login.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_playwright_login.py`:

```python
"""Tests for Playwright login flow — all browser interactions mocked.

These tests verify the control flow and error handling of the login flow
without requiring an actual browser. Playwright is never launched.
"""

import os
import stat
from pathlib import Path
from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from tumbl4.core.auth.cookie_store import CookieData, load_state
from tumbl4.core.auth.playwright_login import (
    _check_display_available,
    _prepare_browser_profile,
    run_login_flow,
)
from tumbl4.core.errors import AuthError, NoDisplay


class TestCheckDisplayAvailable:
    def test_passes_when_display_set(self, monkeypatch: pytest.MonkeyPatch) -> None:
        monkeypatch.setenv("DISPLAY", ":0")
        monkeypatch.delenv("WAYLAND_DISPLAY", raising=False)
        _check_display_available()  # should not raise

    def test_passes_when_wayland_set(self, monkeypatch: pytest.MonkeyPatch) -> None:
        monkeypatch.delenv("DISPLAY", raising=False)
        monkeypatch.setenv("WAYLAND_DISPLAY", "wayland-0")
        _check_display_available()  # should not raise

    def test_passes_when_both_set(self, monkeypatch: pytest.MonkeyPatch) -> None:
        monkeypatch.setenv("DISPLAY", ":0")
        monkeypatch.setenv("WAYLAND_DISPLAY", "wayland-0")
        _check_display_available()  # should not raise

    def test_raises_no_display_when_neither_set(self, monkeypatch: pytest.MonkeyPatch) -> None:
        monkeypatch.delenv("DISPLAY", raising=False)
        monkeypatch.delenv("WAYLAND_DISPLAY", raising=False)
        with pytest.raises(NoDisplay, match="graphical environment"):
            _check_display_available()

    def test_skipped_on_macos(self, monkeypatch: pytest.MonkeyPatch) -> None:
        monkeypatch.delenv("DISPLAY", raising=False)
        monkeypatch.delenv("WAYLAND_DISPLAY", raising=False)
        monkeypatch.setattr("sys.platform", "darwin")
        # macOS always has a display server, so the check should pass
        _check_display_available()


class TestPrepareBrowserProfile:
    def test_creates_fresh_profile_dir(self, tmp_path: Path) -> None:
        profile_dir = tmp_path / "browser_profile"
        _prepare_browser_profile(profile_dir)
        assert profile_dir.exists()
        assert profile_dir.is_dir()

    def test_directory_permissions_0700(self, tmp_path: Path) -> None:
        profile_dir = tmp_path / "browser_profile"
        _prepare_browser_profile(profile_dir)
        mode = stat.S_IMODE(os.stat(profile_dir).st_mode)
        assert mode == 0o700

    def test_deletes_existing_profile(self, tmp_path: Path) -> None:
        profile_dir = tmp_path / "browser_profile"
        profile_dir.mkdir()
        stale_file = profile_dir / "stale_cookie.txt"
        stale_file.write_text("old data")
        _prepare_browser_profile(profile_dir)
        assert not stale_file.exists()
        assert profile_dir.exists()

    def test_creates_parent_directories(self, tmp_path: Path) -> None:
        profile_dir = tmp_path / "nested" / "browser_profile"
        _prepare_browser_profile(profile_dir)
        assert profile_dir.exists()


class TestRunLoginFlow:
    async def test_login_flow_saves_state(self, tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
        monkeypatch.setenv("DISPLAY", ":0")

        state_file = tmp_path / "playwright_state.json"
        profile_dir = tmp_path / "browser_profile"

        # Mock Playwright entirely
        mock_storage_state = {
            "cookies": [
                {
                    "name": "pfg",
                    "value": "extracted-cookie",
                    "domain": ".tumblr.com",
                    "path": "/",
                },
                {
                    "name": "logged_in",
                    "value": "1",
                    "domain": ".tumblr.com",
                    "path": "/",
                },
            ],
            "origins": [],
        }

        mock_page = AsyncMock()
        mock_page.url = "https://www.tumblr.com/dashboard"

        mock_context = AsyncMock()
        mock_context.storage_state.return_value = mock_storage_state
        mock_context.new_page.return_value = mock_page

        mock_browser = AsyncMock()
        mock_browser.new_context.return_value = mock_context

        mock_playwright = AsyncMock()
        mock_playwright.chromium.launch.return_value = mock_browser

        mock_pw_context = AsyncMock()
        mock_pw_context.__aenter__ = AsyncMock(return_value=mock_playwright)
        mock_pw_context.__aexit__ = AsyncMock(return_value=False)

        with patch(
            "tumbl4.core.auth.playwright_login.async_playwright",
            return_value=mock_pw_context,
        ):
            await run_login_flow(
                state_file=state_file,
                profile_dir=profile_dir,
                timeout_seconds=1,
            )

        # Verify state was saved
        assert state_file.exists()
        loaded = load_state(state_file)
        assert len(loaded.cookies) == 2
        assert loaded.cookies[0]["name"] == "pfg"

    async def test_login_raises_no_display(
        self, tmp_path: Path, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.delenv("DISPLAY", raising=False)
        monkeypatch.delenv("WAYLAND_DISPLAY", raising=False)
        monkeypatch.setattr("sys.platform", "linux")

        with pytest.raises(NoDisplay):
            await run_login_flow(
                state_file=tmp_path / "state.json",
                profile_dir=tmp_path / "profile",
            )

    async def test_fresh_profile_on_each_login(
        self, tmp_path: Path, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("DISPLAY", ":0")

        profile_dir = tmp_path / "browser_profile"
        profile_dir.mkdir()
        stale = profile_dir / "old_data.txt"
        stale.write_text("stale")

        mock_storage_state = {"cookies": [{"name": "logged_in", "value": "1", "domain": ".tumblr.com"}], "origins": []}
        mock_page = AsyncMock()
        mock_page.url = "https://www.tumblr.com/dashboard"
        mock_context = AsyncMock()
        mock_context.storage_state.return_value = mock_storage_state
        mock_context.new_page.return_value = mock_page
        mock_browser = AsyncMock()
        mock_browser.new_context.return_value = mock_context
        mock_playwright = AsyncMock()
        mock_playwright.chromium.launch.return_value = mock_browser
        mock_pw_context = AsyncMock()
        mock_pw_context.__aenter__ = AsyncMock(return_value=mock_playwright)
        mock_pw_context.__aexit__ = AsyncMock(return_value=False)

        with patch(
            "tumbl4.core.auth.playwright_login.async_playwright",
            return_value=mock_pw_context,
        ):
            await run_login_flow(
                state_file=tmp_path / "state.json",
                profile_dir=profile_dir,
            )

        # Stale data should be gone
        assert not stale.exists()

    async def test_login_raises_on_no_logged_in_cookie(
        self, tmp_path: Path, monkeypatch: pytest.MonkeyPatch
    ) -> None:
        monkeypatch.setenv("DISPLAY", ":0")

        mock_storage_state = {
            "cookies": [{"name": "pfg", "value": "abc", "domain": ".tumblr.com"}],
            "origins": [],
        }
        mock_page = AsyncMock()
        mock_page.url = "https://www.tumblr.com/login"  # still on login page
        mock_context = AsyncMock()
        mock_context.storage_state.return_value = mock_storage_state
        mock_context.new_page.return_value = mock_page
        mock_browser = AsyncMock()
        mock_browser.new_context.return_value = mock_context
        mock_playwright = AsyncMock()
        mock_playwright.chromium.launch.return_value = mock_browser
        mock_pw_context = AsyncMock()
        mock_pw_context.__aenter__ = AsyncMock(return_value=mock_playwright)
        mock_pw_context.__aexit__ = AsyncMock(return_value=False)

        with patch(
            "tumbl4.core.auth.playwright_login.async_playwright",
            return_value=mock_pw_context,
        ):
            with pytest.raises(AuthError, match="[Ll]ogin.*not.*complete|no.*logged_in"):
                await run_login_flow(
                    state_file=tmp_path / "state.json",
                    profile_dir=tmp_path / "profile",
                    timeout_seconds=1,
                )
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_playwright_login.py -v`
Expected: FAIL — `ModuleNotFoundError`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/auth/playwright_login.py`:

```python
"""Playwright-driven interactive Tumblr login.

Launches a headed Chromium browser with a fresh profile, navigates to
Tumblr's login page, and waits for the user to authenticate. Once
the logged_in cookie appears, the storage state is extracted and persisted.

The browser profile is deleted and recreated on every login to prevent
stale session state. See spec sections 6.6, 6.7, 6.8.
"""

from __future__ import annotations

import os
import shutil
import stat
import sys
from pathlib import Path

from tumbl4._internal.logging import get_logger
from tumbl4.core.auth.cookie_store import CookieData, save_state
from tumbl4.core.errors import AuthError, NoDisplay

logger = get_logger(__name__)

_TUMBLR_LOGIN_URL = "https://www.tumblr.com/login"
_TUMBLR_DASHBOARD_URL = "https://www.tumblr.com/dashboard"
_DEFAULT_TIMEOUT = 300  # 5 minutes for user to complete login + 2FA


def _check_display_available() -> None:
    """Verify a graphical display is available. Raises NoDisplay on failure.

    On macOS the display server is always available (Quartz), so the check
    is skipped. On Linux, both $DISPLAY (X11) and $WAYLAND_DISPLAY are
    checked. See spec section 6.7.
    """
    if sys.platform == "darwin":
        return

    display = os.environ.get("DISPLAY")
    wayland = os.environ.get("WAYLAND_DISPLAY")
    if not display and not wayland:
        raise NoDisplay()


def _prepare_browser_profile(profile_dir: Path) -> None:
    """Delete and recreate the browser profile directory.

    Fresh on every login — no stale session state. Directory permissions 0700.
    See spec section 6.6.
    """
    if profile_dir.exists():
        shutil.rmtree(profile_dir)
        logger.debug("deleted stale browser profile", extra={"path": str(profile_dir)})

    profile_dir.mkdir(parents=True, exist_ok=True)
    os.chmod(profile_dir, 0o700)
    logger.debug("created fresh browser profile", extra={"path": str(profile_dir)})


async def run_login_flow(
    *,
    state_file: Path,
    profile_dir: Path,
    timeout_seconds: int = _DEFAULT_TIMEOUT,
) -> None:
    """Run the interactive Playwright login flow.

    1. Check display availability
    2. Prepare a fresh browser profile
    3. Launch headed Chromium, navigate to Tumblr login
    4. Wait for user to complete login (logged_in cookie appears)
    5. Extract and persist storage state

    Raises:
        NoDisplay: if no graphical display is available
        AuthError: if login was not completed (no logged_in cookie)
    """
    _check_display_available()
    _prepare_browser_profile(profile_dir)

    # Import Playwright at call time — it's an optional heavy dependency
    try:
        from playwright.async_api import async_playwright
    except ImportError as e:
        raise AuthError(
            "Playwright is not installed. Install it with:\n"
            "  pip install playwright && playwright install chromium"
        ) from e

    logger.info("launching Chromium for Tumblr login")

    async with async_playwright() as pw:
        browser = await pw.chromium.launch(
            headless=False,
            args=["--disable-blink-features=AutomationControlled"],
        )

        context = await browser.new_context(
            user_data_dir=None,  # fresh context, not persistent
            viewport={"width": 1280, "height": 900},
        )

        page = await context.new_page()
        await page.goto(_TUMBLR_LOGIN_URL, wait_until="domcontentloaded")

        logger.info("waiting for login (close the browser when done, or it will timeout)")

        # Wait for the logged_in cookie to appear by polling storage state.
        # The user interacts with the headed browser — we just observe.
        try:
            await page.wait_for_url(
                "**/dashboard**",
                timeout=timeout_seconds * 1000,
            )
        except Exception:
            # Timeout or browser closed — check if login was completed anyway
            pass

        # Extract storage state regardless of how we got here
        storage_state = await context.storage_state()
        await browser.close()

    # Validate that login was successful
    cookies = storage_state.get("cookies", [])
    has_logged_in = any(
        str(c.get("name", "")) == "logged_in"
        for c in cookies
        if isinstance(c, dict)
    )

    if not has_logged_in:
        raise AuthError(
            "Login was not completed — no logged_in cookie found. "
            "Please try again with `tumbl4 login`."
        )

    # Persist the state
    cookie_data = CookieData(
        cookies=cookies,
        origins=storage_state.get("origins", []),
    )
    save_state(state_file, cookie_data)

    cookie_count = len(cookies)
    logger.info("login successful", extra={"cookie_count": cookie_count})
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_playwright_login.py -v`
Expected: 8 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/auth/playwright_login.py tests/unit/test_playwright_login.py
git commit -m "feat(auth): add Playwright login flow with mocked tests

Headed Chromium, fresh profile each login (0700), display check
(DISPLAY/WAYLAND_DISPLAY), cookie extraction + persistence.
All tests mock Playwright — no real browser required.
See spec sections 6.6-6.8.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: Blog type detection — public vs hidden auto-detect

**Files:**
- Create: `src/tumbl4/core/crawl/blog_detector.py`
- Create: `tests/unit/test_blog_detector.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_blog_detector.py`:

```python
"""Tests for blog type auto-detection (public vs hidden)."""

import httpx
import pytest
import respx

from tumbl4.core.crawl.blog_detector import BlogType, detect_blog_type
from tumbl4.core.crawl.http_client import TumblrHttpClient
from tumbl4.models.blog import BlogRef
from tumbl4.models.settings import HttpSettings

_PUBLIC_V1_RESPONSE = (
    'var tumblr_api_read = '
    '{"tumblelog":{"name":"publicblog"},"posts-start":0,"posts-total":"5","posts":[]};'
)


class TestDetectBlogType:
    @respx.mock
    async def test_public_blog_detected(self) -> None:
        respx.get("https://publicblog.tumblr.com/api/read/json").respond(
            200, text=_PUBLIC_V1_RESPONSE
        )
        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("publicblog")
        try:
            result = await detect_blog_type(http, blog)
            assert result == BlogType.PUBLIC
        finally:
            await http.aclose()

    @respx.mock
    async def test_hidden_blog_detected_on_404(self) -> None:
        respx.get("https://hiddenblog.tumblr.com/api/read/json").respond(404)
        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("hiddenblog")
        try:
            result = await detect_blog_type(http, blog)
            assert result == BlogType.HIDDEN
        finally:
            await http.aclose()

    @respx.mock
    async def test_hidden_blog_detected_on_401(self) -> None:
        respx.get("https://privateblog.tumblr.com/api/read/json").respond(401)
        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("privateblog")
        try:
            result = await detect_blog_type(http, blog)
            assert result == BlogType.HIDDEN
        finally:
            await http.aclose()

    @respx.mock
    async def test_hidden_blog_detected_on_403(self) -> None:
        respx.get("https://dashblog.tumblr.com/api/read/json").respond(403)
        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("dashblog")
        try:
            result = await detect_blog_type(http, blog)
            assert result == BlogType.HIDDEN
        finally:
            await http.aclose()

    @respx.mock
    async def test_nonexistent_blog_v1_responds_with_meta_msg(self) -> None:
        """Tumblr sometimes returns 200 with error body for nonexistent blogs."""
        respx.get("https://reallygone.tumblr.com/api/read/json").respond(
            200, text='var tumblr_api_read = {"tumblelog":{},"posts-start":0,"posts-total":"0","posts":[]};'
        )
        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("reallygone")
        try:
            result = await detect_blog_type(http, blog)
            assert result == BlogType.PUBLIC  # empty blog is still public
        finally:
            await http.aclose()

    @respx.mock
    async def test_server_error_raises(self) -> None:
        from tumbl4.core.errors import ServerError

        respx.get("https://errorblog.tumblr.com/api/read/json").respond(502)
        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("errorblog")
        try:
            with pytest.raises(ServerError):
                await detect_blog_type(http, blog)
        finally:
            await http.aclose()

    @respx.mock
    async def test_explicit_override_public(self) -> None:
        """When force=PUBLIC, no HTTP request is made."""
        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("anyblog")
        try:
            result = await detect_blog_type(http, blog, force=BlogType.PUBLIC)
            assert result == BlogType.PUBLIC
        finally:
            await http.aclose()

    @respx.mock
    async def test_explicit_override_hidden(self) -> None:
        """When force=HIDDEN, no HTTP request is made."""
        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("anyblog")
        try:
            result = await detect_blog_type(http, blog, force=BlogType.HIDDEN)
            assert result == BlogType.HIDDEN
        finally:
            await http.aclose()
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_blog_detector.py -v`
Expected: FAIL — `ModuleNotFoundError`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/crawl/blog_detector.py`:

```python
"""Auto-detect whether a blog is public or hidden (login-required).

Probes the V1 public API endpoint. If it returns a successful response,
the blog is public. If it returns 401, 403, or 404, the blog is likely
hidden/dashboard-only and requires authentication.

See spec section 8.6 for auto-detection behavior.
"""

from __future__ import annotations

from enum import Enum

from tumbl4._internal.logging import get_logger
from tumbl4.core.crawl.http_client import TumblrHttpClient
from tumbl4.core.errors import RateLimited, ServerError
from tumbl4.models.blog import BlogRef

logger = get_logger(__name__)


class BlogType(Enum):
    """Detected blog access type."""

    PUBLIC = "public"
    HIDDEN = "hidden"


async def detect_blog_type(
    http: TumblrHttpClient,
    blog: BlogRef,
    *,
    force: BlogType | None = None,
) -> BlogType:
    """Detect whether a blog is public or hidden.

    Args:
        http: HTTP client for making the probe request.
        blog: Blog reference to check.
        force: If set, skip detection and return this value immediately.
            Used for --hidden/--public CLI overrides.

    Returns:
        BlogType.PUBLIC or BlogType.HIDDEN.

    Raises:
        ServerError: on 5xx responses (transient errors should be retried).
        RateLimited: on 429 (caller should back off).
    """
    if force is not None:
        logger.debug("blog type forced", extra={"blog": blog.name, "type": force.value})
        return force

    url = f"{blog.url}api/read/json?debug=1&num=1&start=0"

    try:
        await http.get_api(url)
        logger.debug("blog is public", extra={"blog": blog.name})
        return BlogType.PUBLIC
    except (RateLimited, ServerError):
        # These are transient — propagate so the caller can retry
        raise
    except Exception as exc:
        # 401, 403, 404, or other client errors → likely hidden
        logger.debug(
            "blog appears hidden",
            extra={"blog": blog.name, "error": str(exc)},
        )
        return BlogType.HIDDEN
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_blog_detector.py -v`
Expected: 8 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/crawl/blog_detector.py tests/unit/test_blog_detector.py
git commit -m "feat(crawl): add blog type auto-detection (public vs hidden)

Probes V1 public API — 200 means public, 401/403/404 means hidden.
Supports --hidden/--public force overrides. Server errors propagate
for retry. See spec section 8.6.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: Hidden blog crawler with SVC/NPF pagination

**Files:**
- Create: `src/tumbl4/core/crawl/tumblr_hidden.py`
- Create: `tests/fixtures/json/initial_state_peepr.json`
- Create: `tests/fixtures/json/initial_state_response.json`
- Create: `tests/unit/test_hidden_crawler.py`

- [ ] **Step 1: Create test fixtures**

Write file `tests/fixtures/json/initial_state_peepr.json` — PeeprRoute shape `___INITIAL_STATE___`:

```json
{
    "PeeprRoute": {
        "blogName": "hiddenblog",
        "postsView": {
            "posts": [
                {
                    "id": "73829405612300",
                    "blogName": "hiddenblog",
                    "postUrl": "https://www.tumblr.com/hiddenblog/73829405612300",
                    "type": "photo",
                    "timestamp": 1776097800,
                    "tags": ["photography", "art"],
                    "rebloggedFromName": null,
                    "content": [
                        {
                            "type": "image",
                            "media": [
                                {
                                    "url": "https://64.media.tumblr.com/aaa111/s2048x3072/hidden_photo1.jpg",
                                    "width": 2048,
                                    "height": 1536
                                }
                            ],
                            "altText": "A beautiful landscape"
                        }
                    ],
                    "trail": [],
                    "summary": "Hidden blog photo"
                },
                {
                    "id": "73829405612200",
                    "blogName": "hiddenblog",
                    "postUrl": "https://www.tumblr.com/hiddenblog/73829405612200",
                    "type": "photo",
                    "timestamp": 1776011400,
                    "tags": [],
                    "rebloggedFromName": "originalblog",
                    "rebloggedFromId": "99999999",
                    "content": [
                        {
                            "type": "image",
                            "media": [
                                {
                                    "url": "https://64.media.tumblr.com/bbb222/s2048x3072/hidden_photo2.png",
                                    "width": 1600,
                                    "height": 1200
                                }
                            ]
                        }
                    ],
                    "trail": [],
                    "summary": "Reblogged photo"
                }
            ],
            "nextLink": "/svc/indash_blog/hiddenblog?limit=20&offset=20&page_number=2"
        }
    },
    "apiFetchStore": {
        "API_TOKEN": "test-bearer-token-from-peepr"
    }
}
```

Write file `tests/fixtures/json/initial_state_response.json` — response-wrapped shape `___INITIAL_STATE___`:

```json
{
    "response": {
        "posts": {
            "data": [
                {
                    "id": "73829405612300",
                    "blogName": "hiddenblog",
                    "postUrl": "https://www.tumblr.com/hiddenblog/73829405612300",
                    "type": "photo",
                    "timestamp": 1776097800,
                    "tags": ["photography", "art"],
                    "rebloggedFromName": null,
                    "content": [
                        {
                            "type": "image",
                            "media": [
                                {
                                    "url": "https://64.media.tumblr.com/aaa111/s2048x3072/hidden_photo1.jpg",
                                    "width": 2048,
                                    "height": 1536
                                }
                            ],
                            "altText": "A beautiful landscape"
                        }
                    ],
                    "trail": [],
                    "summary": "Hidden blog photo"
                }
            ],
            "links": {
                "next": {
                    "href": "/svc/indash_blog/hiddenblog?limit=20&offset=20&page_number=2",
                    "method": "GET",
                    "queryParams": {
                        "limit": "20",
                        "offset": "20",
                        "page_number": "2"
                    }
                }
            }
        }
    },
    "apiFetchStore": {
        "API_TOKEN": "test-bearer-token-from-response"
    }
}
```

- [ ] **Step 2: Write the failing test**

Write file `tests/unit/test_hidden_crawler.py`:

```python
"""Tests for the hidden blog crawler — SVC/NPF format, cursor-based pagination."""

import json
from pathlib import Path
from unittest.mock import AsyncMock, MagicMock

import httpx
import pytest
import respx

from tumbl4.core.auth.session import AuthSession
from tumbl4.core.auth.cookie_store import CookieData
from tumbl4.core.crawl.http_client import TumblrHttpClient
from tumbl4.core.crawl.tumblr_hidden import (
    TumblrHiddenCrawler,
    extract_bearer_token,
    extract_posts_and_cursor,
)
from tumbl4.core.errors import AuthError, ParseError
from tumbl4.models.blog import BlogRef
from tumbl4.models.settings import HttpSettings

FIXTURES = Path(__file__).parent.parent / "fixtures" / "json"


class TestExtractBearerToken:
    def test_extracts_from_peepr_shape(self) -> None:
        data = json.loads((FIXTURES / "initial_state_peepr.json").read_text())
        token = extract_bearer_token(data)
        assert token == "test-bearer-token-from-peepr"

    def test_extracts_from_response_shape(self) -> None:
        data = json.loads((FIXTURES / "initial_state_response.json").read_text())
        token = extract_bearer_token(data)
        assert token == "test-bearer-token-from-response"

    def test_missing_token_raises_parse_error(self) -> None:
        with pytest.raises(ParseError, match="bearer.*token|API_TOKEN"):
            extract_bearer_token({"PeeprRoute": {}, "apiFetchStore": {}})

    def test_no_api_fetch_store_raises(self) -> None:
        with pytest.raises(ParseError):
            extract_bearer_token({"PeeprRoute": {}})


class TestExtractPostsAndCursor:
    def test_peepr_shape(self) -> None:
        data = json.loads((FIXTURES / "initial_state_peepr.json").read_text())
        posts, next_cursor = extract_posts_and_cursor(data)
        assert len(posts) == 2
        assert posts[0]["id"] == "73829405612300"
        assert next_cursor is not None
        assert "page_number=2" in next_cursor

    def test_response_shape(self) -> None:
        data = json.loads((FIXTURES / "initial_state_response.json").read_text())
        posts, next_cursor = extract_posts_and_cursor(data)
        assert len(posts) == 1
        assert posts[0]["id"] == "73829405612300"
        assert next_cursor is not None

    def test_peepr_preferred_when_both_present(self) -> None:
        """When both PeeprRoute and response keys exist, prefer PeeprRoute."""
        data = json.loads((FIXTURES / "initial_state_peepr.json").read_text())
        # Add a response key too
        data["response"] = {"posts": {"data": [], "links": {}}}
        posts, _ = extract_posts_and_cursor(data)
        assert len(posts) == 2  # PeeprRoute had 2 posts

    def test_no_next_link_returns_none(self) -> None:
        data = {
            "PeeprRoute": {
                "postsView": {
                    "posts": [{"id": "1", "type": "photo"}],
                }
            }
        }
        posts, next_cursor = extract_posts_and_cursor(data)
        assert len(posts) == 1
        assert next_cursor is None

    def test_neither_shape_raises_parse_error(self) -> None:
        with pytest.raises(ParseError, match="shape"):
            extract_posts_and_cursor({"unknown_key": {}})

    def test_empty_posts_array(self) -> None:
        data = {"PeeprRoute": {"postsView": {"posts": []}}}
        posts, next_cursor = extract_posts_and_cursor(data)
        assert len(posts) == 0
        assert next_cursor is None


class TestTumblrHiddenCrawler:
    @respx.mock
    async def test_crawl_first_page_from_html(self) -> None:
        """First page: fetch blog HTML, extract ___INITIAL_STATE___, yield posts."""
        peepr_data = json.loads((FIXTURES / "initial_state_peepr.json").read_text())
        initial_state_json = json.dumps(peepr_data)

        blog_html = (
            '<html><head></head><body>'
            '<script>window[\'___INITIAL_STATE___\'] = '
            + initial_state_json
            + ';</script></body></html>'
        )

        # Mock the blog HTML page
        respx.get("https://www.tumblr.com/hiddenblog").respond(200, text=blog_html)

        # Mock the SVC next-page request to return empty (stop pagination)
        respx.get(
            url__startswith="https://www.tumblr.com/svc/indash_blog/hiddenblog"
        ).respond(200, json={"response": {"posts": [], "links": {}}})

        session = AuthSession(
            cookies=CookieData(
                cookies=[{"name": "pfg", "value": "abc", "domain": ".tumblr.com"}],
                origins=[],
            ),
            bearer_token="test-bearer-token-from-peepr",
        )
        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("hiddenblog")
        crawler = TumblrHiddenCrawler(http, blog, session=session)

        try:
            posts = [p async for p in crawler.crawl()]
            assert len(posts) == 2
            assert posts[0]["post_id"] == "73829405612300"
            assert posts[0]["source_format"] == "svc"
            assert len(posts[0]["media"]) == 1
            assert posts[0]["media"][0]["url"].endswith("hidden_photo1.jpg")
        finally:
            await http.aclose()

    @respx.mock
    async def test_crawl_extracts_bearer_token_from_html(self) -> None:
        peepr_data = json.loads((FIXTURES / "initial_state_peepr.json").read_text())
        initial_state_json = json.dumps(peepr_data)

        blog_html = (
            '<html><script>window[\'___INITIAL_STATE___\'] = '
            + initial_state_json
            + ';</script></html>'
        )

        respx.get("https://www.tumblr.com/hiddenblog").respond(200, text=blog_html)
        respx.get(url__startswith="https://www.tumblr.com/svc/").respond(
            200, json={"response": {"posts": [], "links": {}}}
        )

        session = AuthSession(
            cookies=CookieData(
                cookies=[{"name": "pfg", "value": "abc", "domain": ".tumblr.com"}],
                origins=[],
            ),
        )
        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("hiddenblog")
        crawler = TumblrHiddenCrawler(http, blog, session=session)

        try:
            _ = [p async for p in crawler.crawl()]
            # Bearer token should have been extracted from ___INITIAL_STATE___
            assert session.bearer_token == "test-bearer-token-from-peepr"
        finally:
            await http.aclose()

    @respx.mock
    async def test_crawl_detects_reblog(self) -> None:
        peepr_data = json.loads((FIXTURES / "initial_state_peepr.json").read_text())
        initial_state_json = json.dumps(peepr_data)

        blog_html = (
            '<html><script>window[\'___INITIAL_STATE___\'] = '
            + initial_state_json
            + ';</script></html>'
        )

        respx.get("https://www.tumblr.com/hiddenblog").respond(200, text=blog_html)
        respx.get(url__startswith="https://www.tumblr.com/svc/").respond(
            200, json={"response": {"posts": [], "links": {}}}
        )

        session = AuthSession(
            cookies=CookieData(
                cookies=[{"name": "pfg", "value": "abc", "domain": ".tumblr.com"}],
                origins=[],
            ),
            bearer_token="token",
        )
        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("hiddenblog")
        crawler = TumblrHiddenCrawler(http, blog, session=session)

        try:
            posts = [p async for p in crawler.crawl()]
            # Second post is a reblog
            reblog_post = [p for p in posts if p["is_reblog"]]
            assert len(reblog_post) == 1
            assert reblog_post[0]["reblog_source"]["blog_name"] == "originalblog"
        finally:
            await http.aclose()

    @respx.mock
    async def test_tracks_highest_post_id(self) -> None:
        peepr_data = json.loads((FIXTURES / "initial_state_peepr.json").read_text())
        initial_state_json = json.dumps(peepr_data)

        blog_html = (
            '<html><script>window[\'___INITIAL_STATE___\'] = '
            + initial_state_json
            + ';</script></html>'
        )

        respx.get("https://www.tumblr.com/hiddenblog").respond(200, text=blog_html)
        respx.get(url__startswith="https://www.tumblr.com/svc/").respond(
            200, json={"response": {"posts": [], "links": {}}}
        )

        session = AuthSession(
            cookies=CookieData(
                cookies=[{"name": "pfg", "value": "abc", "domain": ".tumblr.com"}],
                origins=[],
            ),
            bearer_token="token",
        )
        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("hiddenblog")
        crawler = TumblrHiddenCrawler(http, blog, session=session)

        try:
            _ = [p async for p in crawler.crawl()]
            assert crawler.highest_post_id == 73829405612300
        finally:
            await http.aclose()

    async def test_requires_authenticated_session(self) -> None:
        session = AuthSession.unauthenticated()
        http = TumblrHttpClient(HttpSettings())
        blog = BlogRef.from_input("hiddenblog")

        with pytest.raises(AuthError, match="login"):
            crawler = TumblrHiddenCrawler(http, blog, session=session)
            _ = [p async for p in crawler.crawl()]

        await http.aclose()
```

- [ ] **Step 3: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_hidden_crawler.py -v`
Expected: FAIL — `ModuleNotFoundError`

- [ ] **Step 4: Write the implementation**

Write file `src/tumbl4/core/crawl/tumblr_hidden.py`:

```python
"""Hidden blog crawler using the SVC/NPF format with cursor-based pagination.

Hidden (login-required/dashboard) blogs use a completely different API from
the public V1 crawler. The flow is:

1. Fetch the blog's HTML page at https://www.tumblr.com/<blog>
2. Extract ___INITIAL_STATE___ JSON from the HTML
3. Parse the two possible shapes: PeeprRoute vs response-wrapped
4. Extract bearer token from apiFetchStore.API_TOKEN
5. Extract posts array and next_link cursor
6. For subsequent pages, fetch the SVC API directly with the bearer token
7. Continue until next_link is None (no more pages)

See spec section 5.9 for the two-shape extraction and section 5.8 for
cursor semantics.
"""

from __future__ import annotations

import json
import re
from collections.abc import AsyncIterator
from datetime import UTC, datetime

from tumbl4._internal.logging import get_logger
from tumbl4.core.auth.session import AuthSession
from tumbl4.core.crawl.http_client import TumblrHttpClient
from tumbl4.core.errors import AuthError, ParseError, RateLimited
from tumbl4.core.parse.intermediate import IntermediateDict, MediaEntry, ReblogSource
from tumbl4.models.blog import BlogRef

logger = get_logger(__name__)

# Regexes for ___INITIAL_STATE___ extraction — single-line first, multi-line fallback.
# See spec section 5.9.
_INITIAL_STATE_SINGLE = re.compile(
    r"""window\['___INITIAL_STATE___'\]\s*=\s*({.+?})\s*;""",
)
_INITIAL_STATE_MULTI = re.compile(
    r"""window\['___INITIAL_STATE___'\]\s*=\s*({.+?})\s*;\s*</script>""",
    re.DOTALL,
)

_TUMBLR_BASE = "https://www.tumblr.com"


def extract_initial_state(html: str) -> dict[str, object]:
    """Extract ___INITIAL_STATE___ JSON from blog HTML.

    Tries single-line regex first, then multi-line fallback.
    Raises ParseError if neither matches.
    """
    match = _INITIAL_STATE_SINGLE.search(html)
    if not match:
        match = _INITIAL_STATE_MULTI.search(html)

    if not match:
        excerpt = html[:500].replace("\n", " ").strip()
        raise ParseError(
            "Could not extract ___INITIAL_STATE___ from blog HTML. "
            "The page structure may have changed.",
            excerpt=excerpt,
        )

    try:
        return json.loads(match.group(1))  # type: ignore[no-any-return]
    except json.JSONDecodeError as e:
        raise ParseError(
            f"___INITIAL_STATE___ is not valid JSON: {e}",
            excerpt=match.group(1)[:200],
        ) from e


def extract_bearer_token(state: dict[str, object]) -> str:
    """Extract the bearer token from ___INITIAL_STATE___.

    The token lives at apiFetchStore.API_TOKEN in both shapes.
    """
    api_store = state.get("apiFetchStore")
    if not isinstance(api_store, dict):
        raise ParseError(
            "No apiFetchStore in ___INITIAL_STATE___ — cannot extract bearer token",
        )

    token = api_store.get("API_TOKEN")
    if not token or not isinstance(token, str):
        raise ParseError(
            "No API_TOKEN in apiFetchStore — bearer token not found",
        )

    return token


def extract_posts_and_cursor(
    state: dict[str, object],
) -> tuple[list[dict[str, object]], str | None]:
    """Extract posts array and next_link cursor from ___INITIAL_STATE___.

    Two shapes:
    - PeeprRoute: posts at PeeprRoute.postsView.posts, cursor at postsView.nextLink
    - Response-wrapped: posts at response.posts.data, cursor at response.posts.links.next.href

    When both are present, prefer PeeprRoute (the newer shape) per spec section 5.9.
    """
    has_peepr = "PeeprRoute" in state
    has_response = "response" in state

    if has_peepr:
        if has_response:
            logger.warning(
                "Both PeeprRoute and response keys present in ___INITIAL_STATE___; "
                "preferring PeeprRoute (newer shape)"
            )
        return _extract_peepr_shape(state)

    if has_response:
        return _extract_response_shape(state)

    raise ParseError(
        "Neither PeeprRoute nor response key found in ___INITIAL_STATE___ — "
        "unknown shape",
    )


def _extract_peepr_shape(
    state: dict[str, object],
) -> tuple[list[dict[str, object]], str | None]:
    """Extract from PeeprRoute shape."""
    peepr = state.get("PeeprRoute", {})
    if not isinstance(peepr, dict):
        raise ParseError("PeeprRoute is not a dict")

    posts_view = peepr.get("postsView", {})
    if not isinstance(posts_view, dict):
        posts_view = {}

    posts = posts_view.get("posts", [])
    if not isinstance(posts, list):
        posts = []

    next_link = posts_view.get("nextLink")
    if not isinstance(next_link, str) or not next_link:
        next_link = None

    return posts, next_link  # type: ignore[return-value]


def _extract_response_shape(
    state: dict[str, object],
) -> tuple[list[dict[str, object]], str | None]:
    """Extract from response-wrapped shape."""
    response = state.get("response", {})
    if not isinstance(response, dict):
        raise ParseError("response is not a dict")

    posts_section = response.get("posts", {})
    if not isinstance(posts_section, dict):
        posts_section = {}

    posts = posts_section.get("data", [])
    if not isinstance(posts, list):
        posts = []

    links = posts_section.get("links", {})
    next_link: str | None = None
    if isinstance(links, dict):
        next_obj = links.get("next", {})
        if isinstance(next_obj, dict):
            href = next_obj.get("href")
            if isinstance(href, str) and href:
                next_link = href

    return posts, next_link  # type: ignore[return-value]


def _normalize_svc_post(
    post: dict[str, object],
    blog_name: str,
) -> IntermediateDict:
    """Convert an SVC/NPF post dict to IntermediateDict.

    SVC posts use NPF-style content blocks (type: image, video, audio, text).
    """
    post_id = str(post.get("id", ""))
    post_type_raw = str(post.get("type", "text"))

    # Map SVC type to our canonical types
    type_map = {
        "photo": "photo",
        "video": "video",
        "audio": "audio",
        "text": "text",
        "quote": "quote",
        "link": "link",
        "answer": "answer",
        "chat": "text",
    }
    post_type = type_map.get(post_type_raw, "text")

    # Extract media from content blocks
    media: list[MediaEntry] = []
    content_blocks = post.get("content", [])
    if isinstance(content_blocks, list):
        for block in content_blocks:
            if not isinstance(block, dict):
                continue
            block_type = block.get("type", "")

            if block_type == "image":
                block_media = block.get("media", [])
                if isinstance(block_media, list):
                    for m in block_media:
                        if isinstance(m, dict) and m.get("url"):
                            media.append(
                                MediaEntry(
                                    kind="photo",
                                    url=str(m["url"]),
                                    width=_int_or_none(m.get("width")),
                                    height=_int_or_none(m.get("height")),
                                    mime_type=None,
                                    alt_text=_str_or_none(block.get("altText")),
                                    duration_ms=None,
                                )
                            )
            elif block_type == "video":
                video_url = block.get("url")
                if isinstance(video_url, str) and video_url:
                    media.append(
                        MediaEntry(
                            kind="video",
                            url=video_url,
                            width=_int_or_none(block.get("width")),
                            height=_int_or_none(block.get("height")),
                            mime_type=None,
                            alt_text=None,
                            duration_ms=_int_or_none(block.get("duration")),
                        )
                    )
            elif block_type == "audio":
                audio_url = block.get("url")
                if isinstance(audio_url, str) and audio_url:
                    media.append(
                        MediaEntry(
                            kind="audio",
                            url=audio_url,
                            width=None,
                            height=None,
                            mime_type=None,
                            alt_text=None,
                            duration_ms=_int_or_none(block.get("duration")),
                        )
                    )

    # Timestamp
    ts_raw = post.get("timestamp")
    ts = _parse_timestamp(ts_raw)

    # Tags
    tags = post.get("tags", [])
    if not isinstance(tags, list):
        tags = []
    tags = [str(t) for t in tags]

    # Reblog detection
    reblog_from = post.get("rebloggedFromName")
    is_reblog = bool(reblog_from)
    reblog_source: ReblogSource | None = None
    if isinstance(reblog_from, str) and reblog_from:
        reblog_source = ReblogSource(
            blog_name=reblog_from,
            post_id=str(post.get("rebloggedFromId", "")),
        )

    # Summary / body
    summary = post.get("summary")
    body_text = str(summary) if summary else None

    # Raw content blocks for NPF sidecar
    raw_blocks: list[dict[str, object]] | None = None
    if isinstance(content_blocks, list) and content_blocks:
        raw_blocks = content_blocks  # type: ignore[assignment]

    return IntermediateDict(
        schema_version=1,
        source_format="svc",
        post_id=post_id,
        blog_name=blog_name,
        post_url=str(post.get("postUrl", "")),
        post_type=post_type,  # type: ignore[arg-type]
        timestamp_utc=ts,
        tags=tags,
        is_reblog=is_reblog,
        reblog_source=reblog_source,
        title=None,
        body_text=body_text,
        body_html=None,
        media=media,
        raw_content_blocks=raw_blocks,
    )


class TumblrHiddenCrawler:
    """Crawl a hidden/dashboard Tumblr blog via SVC/NPF.

    Uses cursor-based pagination via next_link. Bearer token and initial
    posts are extracted from ___INITIAL_STATE___ on the first page.
    Subsequent pages use the SVC API directly.
    """

    def __init__(
        self,
        http: TumblrHttpClient,
        blog: BlogRef,
        *,
        session: AuthSession,
        last_id: int = 0,
    ) -> None:
        self._http = http
        self._blog = blog
        self._session = session
        self._last_id = last_id
        self.highest_post_id: int = 0
        self.total_posts: int = 0
        self.rate_limited: bool = False

    async def crawl(self) -> AsyncIterator[IntermediateDict]:
        """Async generator yielding IntermediateDict for each post.

        First page: fetch blog HTML, extract ___INITIAL_STATE___.
        Subsequent pages: fetch SVC API with bearer token and next_link cursor.
        """
        if not self._session.is_authenticated:
            raise AuthError(
                f'"{self._blog.name}" requires login. Run `tumbl4 login` first.'
            )

        # First page: fetch the blog's HTML page
        blog_url = f"{_TUMBLR_BASE}/{self._blog.name}"
        try:
            html = await self._http.get_api(blog_url)
        except RateLimited:
            self.rate_limited = True
            return

        state = extract_initial_state(html)

        # Extract bearer token if not already set
        if not self._session.bearer_token:
            try:
                token = extract_bearer_token(state)
                self._session.set_bearer_token(token)
            except ParseError:
                logger.warning("could not extract bearer token from ___INITIAL_STATE___")

        # Extract posts and cursor from first page
        posts, next_cursor = extract_posts_and_cursor(state)

        # Yield posts from first page
        for post_raw in posts:
            intermediate = self._process_post(post_raw)
            if intermediate is not None:
                yield intermediate

        # Paginate via next_link
        while next_cursor:
            try:
                svc_posts, next_cursor = await self._fetch_next_page(next_cursor)
            except RateLimited:
                self.rate_limited = True
                return

            if not svc_posts:
                return

            all_below_fence = True
            for post_raw in svc_posts:
                intermediate = self._process_post(post_raw)
                if intermediate is not None:
                    all_below_fence = False
                    yield intermediate
                elif self._last_id > 0:
                    # Post was below fence — check if we should stop
                    pass

            if all_below_fence and self._last_id > 0:
                return

    def _process_post(self, post_raw: dict[str, object]) -> IntermediateDict | None:
        """Normalize a single post. Returns None if the post should be skipped."""
        post_id_str = str(post_raw.get("id", "0"))
        try:
            post_id_int = int(post_id_str)
        except ValueError:
            post_id_int = 0

        # Track highest post ID
        if post_id_int > self.highest_post_id:
            self.highest_post_id = post_id_int

        self.total_posts += 1

        # Skip posts at or below resume fence
        if self._last_id > 0 and post_id_int <= self._last_id:
            return None

        try:
            return _normalize_svc_post(post_raw, self._blog.name)
        except ParseError as e:
            logger.error(
                "failed to parse hidden post",
                extra={"post_id": post_id_str, "error": str(e)},
            )
            return None

    async def _fetch_next_page(
        self, next_link: str,
    ) -> tuple[list[dict[str, object]], str | None]:
        """Fetch the next page via the SVC API.

        Uses the bearer token for authentication. Returns (posts, next_cursor).
        """
        # Build full URL from relative next_link
        if next_link.startswith("/"):
            url = f"{_TUMBLR_BASE}{next_link}"
        else:
            url = next_link

        # Make authenticated request
        headers = self._session.auth_headers()
        headers["Accept"] = "application/json"

        async with self._http.rate_limiter:
            response = await self._http.client.get(
                url,
                headers=headers,
                cookies=self._session.httpx_cookies(),
            )

        if response.status_code == 429:
            raise RateLimited()
        if response.status_code >= 400:
            logger.warning(
                "SVC API error",
                extra={"status": response.status_code, "url": url},
            )
            return [], None

        try:
            data = response.json()
        except Exception as e:
            logger.error("SVC response is not valid JSON", extra={"error": str(e)})
            return [], None

        # SVC pagination response has the same structure
        response_data = data.get("response", {})
        if not isinstance(response_data, dict):
            return [], None

        posts = response_data.get("posts", [])
        if not isinstance(posts, list):
            posts = []

        # Extract next_link from response
        links = response_data.get("links", {})
        next_cursor: str | None = None
        if isinstance(links, dict):
            next_obj = links.get("next", {})
            if isinstance(next_obj, dict):
                href = next_obj.get("href")
                if isinstance(href, str) and href:
                    next_cursor = href

        return posts, next_cursor  # type: ignore[return-value]


def _parse_timestamp(raw: object) -> str:
    """Parse a unix timestamp to ISO8601 string."""
    try:
        ts = int(raw) if raw is not None else 0  # type: ignore[arg-type]
        return datetime.fromtimestamp(ts, tz=UTC).isoformat()
    except (ValueError, TypeError, OSError):
        return datetime.fromtimestamp(0, tz=UTC).isoformat()


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
```

- [ ] **Step 5: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_hidden_crawler.py -v`
Expected: 12 passed

- [ ] **Step 6: Commit**

```bash
git add src/tumbl4/core/crawl/tumblr_hidden.py tests/fixtures/json/initial_state_peepr.json tests/fixtures/json/initial_state_response.json tests/unit/test_hidden_crawler.py
git commit -m "feat(crawl): add hidden blog crawler with SVC/NPF pagination

Two-shape ___INITIAL_STATE___ extraction (PeeprRoute vs response-wrapped),
bearer token from apiFetchStore, cursor-based next_link pagination,
SVC post normalization to IntermediateDict. See spec section 5.9.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: CLI login and logout commands

**Files:**
- Create: `src/tumbl4/cli/commands/login.py`
- Create: `src/tumbl4/cli/commands/logout.py`
- Modify: `src/tumbl4/cli/app.py`
- Create: `tests/unit/test_login_command.py`
- Create: `tests/unit/test_logout_command.py`

- [ ] **Step 1: Write the failing tests**

Write file `tests/unit/test_login_command.py`:

```python
"""Tests for the tumbl4 login CLI command."""

from typer.testing import CliRunner

from tumbl4.cli.app import app

runner = CliRunner()


class TestLoginCommand:
    def test_login_appears_in_help(self) -> None:
        result = runner.invoke(app, ["--help"])
        assert result.exit_code == 0
        assert "login" in result.output

    def test_login_help(self) -> None:
        result = runner.invoke(app, ["login", "--help"])
        assert result.exit_code == 0
        assert "login" in result.output.lower() or "tumblr" in result.output.lower()
```

Write file `tests/unit/test_logout_command.py`:

```python
"""Tests for the tumbl4 logout CLI command."""

import json
import os
from pathlib import Path
from unittest.mock import patch

from typer.testing import CliRunner

from tumbl4.cli.app import app

runner = CliRunner()


class TestLogoutCommand:
    def test_logout_appears_in_help(self) -> None:
        result = runner.invoke(app, ["--help"])
        assert result.exit_code == 0
        assert "logout" in result.output

    def test_logout_help(self) -> None:
        result = runner.invoke(app, ["logout", "--help"])
        assert result.exit_code == 0

    def test_logout_deletes_state_file(self, tmp_path: Path) -> None:
        state_file = tmp_path / "playwright_state.json"
        state_file.write_text('{"cookies": [], "origins": []}')
        os.chmod(state_file, 0o600)

        profile_dir = tmp_path / "browser_profile"
        profile_dir.mkdir()
        (profile_dir / "data.txt").write_text("browser data")

        with (
            patch("tumbl4.cli.commands.logout.playwright_state_file", return_value=state_file),
            patch("tumbl4.cli.commands.logout.browser_profile_dir", return_value=profile_dir),
        ):
            result = runner.invoke(app, ["logout"])

        assert result.exit_code == 0
        assert not state_file.exists()
        assert not profile_dir.exists()

    def test_logout_succeeds_when_no_session(self, tmp_path: Path) -> None:
        state_file = tmp_path / "nonexistent.json"
        profile_dir = tmp_path / "nonexistent_profile"

        with (
            patch("tumbl4.cli.commands.logout.playwright_state_file", return_value=state_file),
            patch("tumbl4.cli.commands.logout.browser_profile_dir", return_value=profile_dir),
        ):
            result = runner.invoke(app, ["logout"])

        assert result.exit_code == 0
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `uv run pytest tests/unit/test_login_command.py tests/unit/test_logout_command.py -v`
Expected: FAIL — "login" not in help output

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/cli/commands/login.py`:

```python
"""tumbl4 login — interactive Playwright-driven Tumblr login."""

from __future__ import annotations

import asyncio
from typing import Annotated

import typer

from tumbl4._internal.logging import get_logger
from tumbl4._internal.paths import browser_profile_dir, playwright_state_file
from tumbl4.cli.output.progress import console

logger = get_logger(__name__)


def login(
    timeout: Annotated[
        int,
        typer.Option("--timeout", help="Login timeout in seconds"),
    ] = 300,
) -> None:
    """Log in to Tumblr via interactive browser (required for hidden blogs)."""
    asyncio.run(_login_async(timeout=timeout))


async def _login_async(*, timeout: int) -> None:
    """Async implementation of the login command."""
    from tumbl4.core.auth.playwright_login import run_login_flow
    from tumbl4.core.errors import AuthError, NoDisplay

    state_file = playwright_state_file()
    profile_dir = browser_profile_dir()

    console.print("[bold]tumbl4 login[/bold]")
    console.print("A Chromium browser will open. Log in to Tumblr, then the browser will close.")
    console.print(f"Timeout: {timeout}s\n")

    try:
        await run_login_flow(
            state_file=state_file,
            profile_dir=profile_dir,
            timeout_seconds=timeout,
        )
        console.print("[bold green]Login successful![/bold green]")
        console.print(f"Session saved to {state_file}")
    except NoDisplay as exc:
        console.print(f"[bold red]Error:[/bold red] {exc}")
        raise typer.Exit(code=1) from exc
    except AuthError as exc:
        console.print(f"[bold red]Login failed:[/bold red] {exc}")
        raise typer.Exit(code=1) from exc
```

Write file `src/tumbl4/cli/commands/logout.py`:

```python
"""tumbl4 logout — delete stored Tumblr session and browser profile."""

from __future__ import annotations

import gc
import shutil

import typer

from tumbl4._internal.logging import get_logger
from tumbl4._internal.paths import browser_profile_dir, playwright_state_file
from tumbl4.cli.output.progress import console
from tumbl4.core.auth.cookie_store import delete_state

logger = get_logger(__name__)


def logout() -> None:
    """Delete stored Tumblr login session and browser profile."""
    state_file = playwright_state_file()
    profile_dir = browser_profile_dir()

    deleted_anything = False

    # Delete session file
    if state_file.exists():
        delete_state(state_file)
        console.print(f"Deleted session: {state_file}")
        deleted_anything = True

    # Delete browser profile directory
    if profile_dir.exists():
        shutil.rmtree(profile_dir)
        console.print(f"Deleted browser profile: {profile_dir}")
        deleted_anything = True

    if not deleted_anything:
        console.print("No stored session found — already logged out.")
    else:
        console.print("[bold green]Logged out.[/bold green]")

    # Explicit gc to clear any in-memory credentials. See spec section 6.6.
    gc.collect()
```

Modify `src/tumbl4/cli/app.py` to register login and logout commands. The full file should be:

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
from tumbl4.cli.commands.login import login  # noqa: E402
from tumbl4.cli.commands.logout import logout  # noqa: E402

app.command()(download)
app.command()(login)
app.command()(logout)


def main() -> None:
    """Console-script entry point referenced from pyproject.toml."""
    app()
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `uv run pytest tests/unit/test_login_command.py tests/unit/test_logout_command.py -v`
Expected: 6 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/cli/commands/login.py src/tumbl4/cli/commands/logout.py src/tumbl4/cli/app.py tests/unit/test_login_command.py tests/unit/test_logout_command.py
git commit -m "feat(cli): add 'tumbl4 login' and 'tumbl4 logout' commands

Login launches headed Chromium via Playwright for interactive auth.
Logout deletes playwright_state.json and browser profile, then gc.collect()
to clear in-memory credentials. See spec section 6.6.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: Wire hidden crawler into download command

**Files:**
- Modify: `src/tumbl4/cli/commands/download.py`
- Modify: `src/tumbl4/core/__init__.py`

- [ ] **Step 1: Write the failing test**

Modify the existing download command tests (or create new ones). Write file `tests/unit/test_download_hidden.py`:

```python
"""Tests for download command with --hidden/--public flags and blog detection."""

from typer.testing import CliRunner

from tumbl4.cli.app import app

runner = CliRunner()


class TestDownloadHiddenFlags:
    def test_hidden_flag_in_help(self) -> None:
        result = runner.invoke(app, ["download", "--help"])
        assert result.exit_code == 0
        assert "--hidden" in result.output

    def test_public_flag_in_help(self) -> None:
        result = runner.invoke(app, ["download", "--help"])
        assert result.exit_code == 0
        assert "--public" in result.output

    def test_hidden_and_public_mutually_exclusive(self) -> None:
        result = runner.invoke(app, ["download", "testblog", "--hidden", "--public"])
        assert result.exit_code != 0

    def test_hidden_without_login_fails(self) -> None:
        """--hidden without a stored session should fail with auth error."""
        result = runner.invoke(app, ["download", "testblog", "--hidden"])
        assert result.exit_code != 0
        # Should mention login
        output = result.output.lower()
        assert "login" in output or "session" in output or "auth" in output
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_download_hidden.py -v`
Expected: FAIL — "--hidden" not in help output

- [ ] **Step 3: Write the implementation**

Modify `src/tumbl4/cli/commands/download.py` to add --hidden/--public flags and wire in blog type detection:

```python
"""tumbl4 download — crawl a Tumblr blog and download media.

Supports both public blogs (V1 API) and hidden/dashboard blogs (SVC/NPF).
Auto-detects blog type unless --hidden or --public is specified.
"""

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
    hidden: Annotated[bool, typer.Option("--hidden", help="Force hidden/authenticated crawl path")] = False,
    public: Annotated[bool, typer.Option("--public", help="Force public crawl path (skip detection)")] = False,
    quiet: Annotated[bool, typer.Option("--quiet", "-q", help="Suppress progress output")] = False,
    verbose: Annotated[bool, typer.Option("--verbose", "-v", help="Enable debug logging")] = False,
) -> None:
    """Download media from a Tumblr blog (public or hidden)."""
    if hidden and public:
        console.print("[bold red]Error:[/bold red] --hidden and --public are mutually exclusive")
        raise typer.Exit(code=1)

    asyncio.run(_download_async(
        blog=blog,
        output_dir=output_dir,
        page_size=page_size,
        image_size=image_size,
        no_resume=no_resume,
        hidden=hidden,
        public=public,
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
    hidden: bool,
    public: bool,
    quiet: bool,
    verbose: bool,
) -> None:
    """Async implementation of the download command."""
    import logging

    from tumbl4._internal.paths import data_dir, playwright_state_file
    from tumbl4.core.auth.cookie_store import has_stored_session
    from tumbl4.core.auth.session import AuthSession, load_session
    from tumbl4.core.crawl.blog_detector import BlogType, detect_blog_type
    from tumbl4.core.crawl.http_client import TumblrHttpClient
    from tumbl4.core.crawl.tumblr_blog import TumblrBlogCrawler
    from tumbl4.core.crawl.tumblr_hidden import TumblrHiddenCrawler
    from tumbl4.core.errors import AuthError, BlogRequiresLogin
    from tumbl4.core.orchestrator import CrawlerProtocol, run_crawl
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

    http = TumblrHttpClient(settings.http)

    try:
        # Determine blog type
        force_type: BlogType | None = None
        if hidden:
            force_type = BlogType.HIDDEN
        elif public:
            force_type = BlogType.PUBLIC

        blog_type = await detect_blog_type(http, blog_ref, force=force_type)

        if not quiet:
            console.print(f"  Blog type: {blog_type.value}")

        # Load session if needed
        session = AuthSession.unauthenticated()
        state_file = playwright_state_file()

        if blog_type == BlogType.HIDDEN:
            if not has_stored_session(state_file):
                console.print(
                    f'[bold red]"{blog_ref.name}" requires login.[/bold red] '
                    "Run `tumbl4 login` and try again."
                )
                raise typer.Exit(code=2)

            try:
                session = load_session(state_file)
            except AuthError as exc:
                console.print(f"[bold red]Auth error:[/bold red] {exc}")
                raise typer.Exit(code=2) from exc

        # Load resume cursor
        db_dir = data_dir()
        db_dir.mkdir(parents=True, exist_ok=True)
        db_path = db_dir / f"{blog_ref.name}.db"

        last_id = 0
        crawler_type = "hidden" if blog_type == BlogType.HIDDEN else "public"
        if not no_resume and db_path.exists():
            db = StateDb(str(db_path))
            last_id = load_cursor(db, blog_ref.name, crawler_type)
            db.close()
            if last_id > 0 and not quiet:
                console.print(f"  Resuming from post ID {last_id}")

        # Create the appropriate crawler
        crawler: CrawlerProtocol
        if blog_type == BlogType.HIDDEN:
            crawler = TumblrHiddenCrawler(
                http, blog_ref,
                session=session,
                last_id=last_id,
            )
        else:
            crawler = TumblrBlogCrawler(
                http, blog_ref,
                page_size=page_size,
                last_id=last_id,
                image_size=image_size,
            )

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

Update `src/tumbl4/core/__init__.py`:

```python
"""Core modules — orchestrator, crawlers, parsers, downloaders, state.

Unstable public API — may change between minor versions until v1.0.0.
"""

from tumbl4.core.orchestrator import CrawlResult, run_crawl

# Preserve Plan 4 exports (FilterConfig, apply_filters) — only add, never drop
__all__ = ["CrawlResult", "run_crawl"]  # noqa: Plan 4 adds filter exports here too
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_download_hidden.py -v`
Expected: 4 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/cli/commands/download.py src/tumbl4/core/__init__.py tests/unit/test_download_hidden.py
git commit -m "feat(cli): wire hidden crawler into download command

Add --hidden/--public flags (mutually exclusive), auto-detect blog type,
load AuthSession from playwright_state.json for hidden blogs, select
appropriate crawler (TumblrBlogCrawler vs TumblrHiddenCrawler).

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 9: Component test — hidden blog end-to-end pipeline

**Files:**
- Create: `tests/component/test_hidden_pipeline.py`

- [ ] **Step 1: Write the component test**

Write file `tests/component/test_hidden_pipeline.py`:

```python
"""Component test — end-to-end hidden blog pipeline with mocked HTTP."""

import json
from pathlib import Path
from unittest.mock import patch

import respx

from tumbl4.core.auth.cookie_store import CookieData
from tumbl4.core.auth.session import AuthSession
from tumbl4.core.crawl.http_client import TumblrHttpClient
from tumbl4.core.crawl.tumblr_hidden import TumblrHiddenCrawler
from tumbl4.core.orchestrator import run_crawl
from tumbl4.models.blog import BlogRef
from tumbl4.models.settings import Settings

FIXTURES = Path(__file__).parent.parent / "fixtures" / "json"


def _build_blog_html(initial_state: dict[str, object]) -> str:
    """Wrap ___INITIAL_STATE___ JSON in an HTML page."""
    return (
        "<html><head></head><body>"
        "<script>window['___INITIAL_STATE___'] = "
        + json.dumps(initial_state)
        + ";</script></body></html>"
    )


class TestHiddenPipeline:
    @respx.mock
    async def test_full_hidden_crawl_pipeline(self, tmp_path: Path) -> None:
        """End-to-end: crawl hidden blog (2 posts), download 2 photos."""
        peepr_data = json.loads((FIXTURES / "initial_state_peepr.json").read_text())
        blog_html = _build_blog_html(peepr_data)

        # Mock the blog HTML page
        respx.get("https://www.tumblr.com/hiddenblog").respond(200, text=blog_html)

        # Mock SVC next-page to return empty (single page blog)
        respx.get(url__startswith="https://www.tumblr.com/svc/").respond(
            200, json={"response": {"posts": [], "links": {}}}
        )

        # Mock media downloads
        respx.get("https://64.media.tumblr.com/aaa111/s2048x3072/hidden_photo1.jpg").respond(
            200, content=b"photo1-hidden-data", headers={"Content-Type": "image/jpeg"},
        )
        respx.get("https://64.media.tumblr.com/bbb222/s2048x3072/hidden_photo2.png").respond(
            200, content=b"photo2-hidden-data", headers={"Content-Type": "image/png"},
        )

        settings = Settings(output_dir=tmp_path / "output", max_concurrent_downloads=2)
        blog = BlogRef.from_input("hiddenblog")
        session = AuthSession(
            cookies=CookieData(
                cookies=[
                    {"name": "pfg", "value": "abc", "domain": ".tumblr.com"},
                    {"name": "logged_in", "value": "1", "domain": ".tumblr.com"},
                ],
                origins=[],
            ),
            bearer_token="test-token",
        )
        http = TumblrHttpClient(settings.http)
        crawler = TumblrHiddenCrawler(http, blog, session=session)

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
        assert result.downloads_success == 2
        assert result.complete is True

        # Verify files on disk
        blog_dir = tmp_path / "output" / "hiddenblog"
        downloaded_files = list(blog_dir.glob("*.*"))
        # Filter out .part files and _meta directory
        media_files = [f for f in downloaded_files if not f.name.endswith(".part") and f.is_file()]
        assert len(media_files) == 2

        # Verify sidecars
        meta_dir = blog_dir / "_meta"
        sidecars = list(meta_dir.glob("*.json"))
        assert len(sidecars) == 2

    @respx.mock
    async def test_hidden_crawl_with_pagination(self, tmp_path: Path) -> None:
        """Two pages: first from ___INITIAL_STATE___, second from SVC API."""
        peepr_data = json.loads((FIXTURES / "initial_state_peepr.json").read_text())
        blog_html = _build_blog_html(peepr_data)

        # First page: blog HTML
        respx.get("https://www.tumblr.com/hiddenblog").respond(200, text=blog_html)

        # Second page: SVC API returns one more post
        second_page = {
            "response": {
                "posts": [
                    {
                        "id": "73829405612100",
                        "blogName": "hiddenblog",
                        "postUrl": "https://www.tumblr.com/hiddenblog/73829405612100",
                        "type": "photo",
                        "timestamp": 1775925000,
                        "tags": [],
                        "content": [
                            {
                                "type": "image",
                                "media": [
                                    {
                                        "url": "https://64.media.tumblr.com/eee555/s2048x3072/page2_photo.jpg",
                                        "width": 1024,
                                        "height": 768,
                                    }
                                ],
                            }
                        ],
                    }
                ],
                "links": {},
            }
        }
        respx.get(url__startswith="https://www.tumblr.com/svc/").respond(200, json=second_page)

        # Mock all media downloads
        respx.get("https://64.media.tumblr.com/aaa111/s2048x3072/hidden_photo1.jpg").respond(
            200, content=b"p1", headers={"Content-Type": "image/jpeg"},
        )
        respx.get("https://64.media.tumblr.com/bbb222/s2048x3072/hidden_photo2.png").respond(
            200, content=b"p2", headers={"Content-Type": "image/png"},
        )
        respx.get("https://64.media.tumblr.com/eee555/s2048x3072/page2_photo.jpg").respond(
            200, content=b"p3", headers={"Content-Type": "image/jpeg"},
        )

        settings = Settings(output_dir=tmp_path / "output", max_concurrent_downloads=2)
        blog = BlogRef.from_input("hiddenblog")
        session = AuthSession(
            cookies=CookieData(
                cookies=[{"name": "pfg", "value": "abc", "domain": ".tumblr.com"}],
                origins=[],
            ),
            bearer_token="test-token",
        )
        http = TumblrHttpClient(settings.http)
        crawler = TumblrHiddenCrawler(http, blog, session=session)

        try:
            result = await run_crawl(
                settings=settings,
                blog=blog,
                crawler=crawler,
                no_resume=True,
            )
        finally:
            await http.aclose()

        # 2 posts from first page + 1 from second page = 3 total
        assert result.posts_crawled == 3
        assert result.downloads_success == 3

    @respx.mock
    async def test_hidden_crawl_dedup_on_second_run(self, tmp_path: Path) -> None:
        """Run twice — second run skips via dedup."""
        peepr_data = json.loads((FIXTURES / "initial_state_peepr.json").read_text())
        blog_html = _build_blog_html(peepr_data)

        respx.get("https://www.tumblr.com/hiddenblog").respond(200, text=blog_html)
        respx.get(url__startswith="https://www.tumblr.com/svc/").respond(
            200, json={"response": {"posts": [], "links": {}}}
        )
        respx.get("https://64.media.tumblr.com/aaa111/s2048x3072/hidden_photo1.jpg").respond(
            200, content=b"p1", headers={"Content-Type": "image/jpeg"},
        )
        respx.get("https://64.media.tumblr.com/bbb222/s2048x3072/hidden_photo2.png").respond(
            200, content=b"p2", headers={"Content-Type": "image/png"},
        )

        settings = Settings(output_dir=tmp_path / "output", max_concurrent_downloads=1)
        blog = BlogRef.from_input("hiddenblog")

        def make_session() -> AuthSession:
            return AuthSession(
                cookies=CookieData(
                    cookies=[{"name": "pfg", "value": "abc", "domain": ".tumblr.com"}],
                    origins=[],
                ),
                bearer_token="test-token",
            )

        # First run
        http = TumblrHttpClient(settings.http)
        crawler = TumblrHiddenCrawler(http, blog, session=make_session())
        try:
            result1 = await run_crawl(settings=settings, blog=blog, crawler=crawler, no_resume=True)
        finally:
            await http.aclose()

        assert result1.downloads_success == 2

        # Second run — should skip all via dedup
        http = TumblrHttpClient(settings.http)
        crawler = TumblrHiddenCrawler(http, blog, session=make_session())
        try:
            result2 = await run_crawl(settings=settings, blog=blog, crawler=crawler, no_resume=True)
        finally:
            await http.aclose()

        assert result2.downloads_skipped == 2
        assert result2.downloads_success == 0
```

- [ ] **Step 2: Run the component test**

Run: `uv run pytest tests/component/test_hidden_pipeline.py -v`
Expected: 3 passed

- [ ] **Step 3: Commit**

```bash
git add tests/component/test_hidden_pipeline.py
git commit -m "test: add component tests for hidden blog crawl pipeline

End-to-end with mocked HTTP: single page, paginated, and dedup.
Verifies files and sidecars on disk, bearer token extraction,
and cursor-based SVC pagination.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 10: Quality gates, full test suite, and final cleanup

**Files:**
- Modify: `tests/conftest.py` (add auth fixtures if needed)

- [ ] **Step 1: Run ALL tests**

Run: `uv run pytest -v`
Expected: all tests pass (existing Plan 1-4 tests + ~50 new Plan 5 tests)

- [ ] **Step 2: Run quality gates**

Run: `uv run ruff check .`
Expected: all checks pass (fix any issues found)

Run: `uv run ruff format --check .`
Expected: all formatting correct (run `uv run ruff format .` to fix if needed)

Run: `uv run pyright`
Expected: 0 errors (fix any type errors found)

- [ ] **Step 3: Verify import boundaries**

Verify that auth modules do not import from CLI:

Run: `uv run ruff check src/tumbl4/core/auth/ --select TID`
Expected: 0 violations

- [ ] **Step 4: Commit any fixes**

If Steps 1-3 required any fixes, commit them:

```bash
git add -u
git commit -m "fix: address Plan 5 quality gate findings

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

- [ ] **Step 5: Final summary commit (if all clean)**

```bash
git add -u
git commit -m "chore: Plan 5 complete — auth + hidden blog crawler

Playwright login, cookie persistence, AuthSession, blog type detection,
SVC/NPF hidden crawler with cursor-based pagination. ~50 new tests.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Self-review checklist

| # | Check | Status |
|---|---|---|
| 1 | **Cookie store uses 0600 permissions and refuses broader** | Task 2 — `save_state` writes via `os.open` with `0o600`; `load_state` checks `S_IRGRP \| S_IWGRP \| S_IROTH \| S_IWOTH` and refuses |
| 2 | **Browser profile is fresh on every login (0700)** | Task 4 — `_prepare_browser_profile` does `shutil.rmtree` + `mkdir` + `chmod 0700` |
| 3 | **Headless detection checks both $DISPLAY and $WAYLAND_DISPLAY** | Task 4 — `_check_display_available` checks both env vars, skips on macOS |
| 4 | **Bearer token extracted from ___INITIAL_STATE___, not stored** | Task 6 — `extract_bearer_token` reads `apiFetchStore.API_TOKEN`; `session.py` sets it at crawl time |
| 5 | **Two-shape handling: PeeprRoute preferred when both present** | Task 6 — `extract_posts_and_cursor` checks `has_peepr` first, logs WARNING when both present |
| 6 | **Hidden crawler uses cursor-based pagination (next_link)** | Task 6 — `_fetch_next_page` follows `next_link` from SVC response `links.next.href` |
| 7 | **Blog type auto-detection with --hidden/--public overrides** | Task 5 (detector) + Task 8 (CLI wiring) — probes V1 API, respects force parameter |
| 8 | **Playwright tests are fully mocked (no real browser)** | Task 4 — all tests use `unittest.mock.patch` on `async_playwright` |
| 9 | **Logout deletes both state file and browser profile, then gc.collect()** | Task 7 — `logout.py` calls `delete_state`, `shutil.rmtree`, `gc.collect()` |
| 10 | **All new modules respect import boundaries (core does not import cli)** | Task 10 — verified via `ruff check --select TID` |
| 11 | **TumblrHiddenCrawler conforms to CrawlerProtocol** | Task 6 — has `highest_post_id`, `total_posts`, `rate_limited`, `crawl()` matching protocol |
| 12 | **Resume cursor uses `crawler_type="hidden"` to separate from public** | Task 8 — download command passes `crawler_type` based on `BlogType` |
| 13 | **Component tests verify files and sidecars on disk** | Task 9 — asserts file counts, sidecar counts, and dedup behavior |
| 14 | **Error messages guide user to `tumbl4 login` when auth is needed** | Tasks 1, 6, 8 — `BlogRequiresLogin`, `AuthError`, CLI exit messages all reference login |
| 15 | **~50 new tests as specified in plan boundaries** | Tasks 1-9 produce approximately 50 tests across unit and component |
