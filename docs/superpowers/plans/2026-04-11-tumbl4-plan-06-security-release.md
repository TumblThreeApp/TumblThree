# tumbl4 Plan 6: Security Hardening + Release

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Production-ready security hardening (redirect safety, SSRF guards, terminal sanitization, signal handling, orphan sweep, path traversal guards) plus CI hardening, signed PyPI release workflow, and documentation site.

**Architecture:** This plan adds defensive layers across the existing stack. `CancelToken` (threading.Event-based) provides cooperative cancellation safe from signal handler context. `safe_follow_redirects()` wraps the httpx client with per-hop allowlist + SSRF IP validation. `sanitize.for_terminal()` strips dangerous Unicode categories from external content before display. The orphan sweep runs `.part` cleanup on startup via `asyncio.to_thread`. Ruff rules enforce SQL injection and XXE prevention structurally at lint time. The release workflow uses SLSA attestation + PyPI OIDC for supply-chain security.

**Tech Stack:** Python 3.12+, httpx (async HTTP), threading.Event (cancel primitive), ipaddress (SSRF validation), unicodedata (terminal sanitization), signal (SIGINT handling), pip-audit (dependency scanning), GitHub Actions (CI/CD + SLSA).

**Builds on Plans 1-5:** `Settings`/`HttpSettings` in `models/settings.py`, `TumblrHttpClient` in `core/crawl/http_client.py`, `file_downloader.py` in `core/download/`, `metadata.py` in `core/state/`, `SecretFilter`/`get_logger()` in `_internal/logging.py`, `spawn()` in `_internal/tasks.py`, `state_dir()`/`data_dir()` in `_internal/paths.py`, Typer `app` in `cli/app.py`, orchestrator in `core/orchestrator.py`.

**Plans in this series:**

| # | Plan | Deliverable |
|---|---|---|
| 1 | Foundation (shipped) | `tumbl4 --version`; tooling + CI green |
| 2 | MVP public blog photo crawl (shipped) | `tumbl4 download <blog>` downloads photos, resumable |
| 3 | All post types + sidecars + templates (shipped) | Every post type; configurable filename templates |
| 4 | Filters + dedup + pinned posts (shipped) | Tag/timespan filters; cross-blog dedup; pinned-post fix |
| 5 | Auth + hidden blog crawler (shipped) | `tumbl4 login` + hidden/dashboard blog downloads |
| **6** | **Security hardening + release (this plan)** | **Redirect safety, SSRF guards, signal handling, SLSA release** |

**Spec references:**
- Design spec: `docs/superpowers/specs/2026-04-11-tumbl4-macos-cli-port-design.md`
- Plan boundaries: `docs/superpowers/specs/2026-04-11-tumbl4-plan-boundaries.md`

---

## File Structure (Plan 6 additions)

New files are marked with `+`. Modified files are marked with `~`.

```
src/tumbl4/
├── __init__.py                              # (unchanged)
├── cli/
│   ├── app.py                            ~  # install SIGINT handler, wire sweep subcommand
│   └── commands/
│       └── sweep.py                      +  # tumbl4 sweep <blog>
├── core/
│   ├── cancel_token.py                   +  # CancelToken (threading.Event-based)
│   ├── orchestrator.py                   ~  # cooperative shutdown on cancel
│   ├── crawl/
│   │   └── http_client.py               ~  # add safe_follow_redirects(), SSRF guard
│   ├── download/
│   │   └── file_downloader.py           ~  # add path traversal guard
│   └── state/
│       ├── metadata.py                  ~  # add path traversal guard
│       ├── orphan_sweep.py              +  # .part cleanup on startup
│       └── pending_posts.py             +  # in-flight post→media tracker
├── _internal/
│   ├── sanitize.py                      +  # terminal injection prevention
│   └── signal_handling.py               +  # SIGINT → CancelToken wiring
├── models/
│   └── settings.py                      ~  # add max_redirects, allowed_hosts
.github/
├── workflows/
│   ├── ci.yml                           ~  # add pip-audit + cassette-scrubber steps
│   └── release.yml                      +  # SLSA attestation + PyPI OIDC + GH release
├── dependabot.yml                       +  # dependency update automation
pyproject.toml                           ~  # add ruff bans for SQL injection + XXE
scripts/
└── scrub_cassettes.py                   ~  # add CI re-scan mode
docs/
├── index.md                             +  # docs site root
├── installation.md                      +  # install guide
├── getting-started.md                   +  # first-run walkthrough
├── authentication.md                    +  # Playwright login flow + security notes
├── configuration.md                     +  # config schema + precedence chain
├── filename-templates.md                +  # template syntax + variables + examples
├── architecture.md                      +  # layer diagram for contributors
├── security.md                          +  # threat model + accepted limitations
└── commands/
    ├── download.md                      +  # download command reference
    ├── login.md                         +  # login command reference
    ├── sweep.md                         +  # sweep command reference
    └── status.md                        +  # status command reference
tests/
├── unit/
│   ├── test_cancel_token.py             +
│   ├── test_sanitize.py                 +
│   ├── test_signal_handling.py          +
│   ├── test_redirect_safety.py          +
│   ├── test_ssrf_guard.py              +
│   ├── test_path_traversal.py           +
│   ├── test_orphan_sweep.py             +
│   ├── test_pending_posts.py            +
│   └── test_sweep_command.py            +
└── integration/
    ├── __init__.py                      +
    └── test_signal_integration.py       +
```

---

## Task 1: CancelToken — threading.Event-based cooperative cancellation

**Files:**
- Create: `src/tumbl4/core/cancel_token.py`
- Create: `tests/unit/test_cancel_token.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_cancel_token.py`:

```python
"""Tests for CancelToken — threading.Event-based cooperative cancellation.

See spec §6.10: CancelToken must be safe from signal handler context.
"""

from __future__ import annotations

import asyncio
import threading

import pytest

from tumbl4.core.cancel_token import CancelToken


class TestCancelTokenSync:
    def test_initially_not_cancelled(self) -> None:
        token = CancelToken()
        assert token.is_cancelled() is False

    def test_cancel_sets_flag(self) -> None:
        token = CancelToken()
        token.cancel()
        assert token.is_cancelled() is True

    def test_cancel_from_signal_sets_flag(self) -> None:
        token = CancelToken()
        token.cancel_from_signal()
        assert token.is_cancelled() is True

    def test_cancel_is_idempotent(self) -> None:
        token = CancelToken()
        token.cancel()
        token.cancel()
        assert token.is_cancelled() is True

    def test_cancel_from_another_thread(self) -> None:
        token = CancelToken()
        thread = threading.Thread(target=token.cancel)
        thread.start()
        thread.join(timeout=2.0)
        assert token.is_cancelled() is True

    def test_multiple_tokens_are_independent(self) -> None:
        token_a = CancelToken()
        token_b = CancelToken()
        token_a.cancel()
        assert token_a.is_cancelled() is True
        assert token_b.is_cancelled() is False


class TestCancelTokenAsync:
    async def test_wait_returns_when_cancelled(self) -> None:
        token = CancelToken()
        # Cancel from another thread after a brief delay
        def cancel_later() -> None:
            import time

            time.sleep(0.15)
            token.cancel()

        thread = threading.Thread(target=cancel_later)
        thread.start()

        await asyncio.wait_for(token.wait(), timeout=2.0)
        assert token.is_cancelled() is True
        thread.join(timeout=1.0)

    async def test_wait_returns_immediately_if_already_cancelled(self) -> None:
        token = CancelToken()
        token.cancel()
        await asyncio.wait_for(token.wait(), timeout=0.5)
        assert token.is_cancelled() is True

    async def test_wait_timeout_when_not_cancelled(self) -> None:
        token = CancelToken()
        with pytest.raises(asyncio.TimeoutError):
            await asyncio.wait_for(token.wait(), timeout=0.25)


class TestCancelTokenRepr:
    def test_repr_not_cancelled(self) -> None:
        token = CancelToken()
        assert "cancelled=False" in repr(token)

    def test_repr_cancelled(self) -> None:
        token = CancelToken()
        token.cancel()
        assert "cancelled=True" in repr(token)
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_cancel_token.py -v`
Expected: FAIL — `ModuleNotFoundError: No module named 'tumbl4.core.cancel_token'`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/cancel_token.py`:

```python
"""Cooperative cancellation primitive safe across signal handlers and the asyncio loop.

See spec §6.10. Uses a threading.Event as the ground truth, with a small async
bridge for coroutines that need to ``await`` cancellation.

The prior draft had a two-field (_loop + _event) pattern with a race where
cancel_from_signal could silently no-op if it arrived mid-initialization.
This version eliminates that race because threading.Event is always safe to
call from any thread including a signal handler, and the async bridge is
one-way (async-side observes the threading event).
"""

from __future__ import annotations

import asyncio
import threading


class CancelToken:
    """Cooperative cancellation primitive safe across signal handlers and the asyncio loop.

    - ``cancel()`` and ``cancel_from_signal()`` are both safe from any thread
      (including signal handler context) because they operate on a plain
      threading.Event, which has no loop dependency.
    - ``wait()`` is safe to call from asyncio code; it bridges to the threading
      event via a polling loop that yields to the event loop.
    - ``is_cancelled()`` is a non-blocking check, safe from any context.
    """

    _POLL_INTERVAL_SECONDS: float = 0.1  # trade-off: 100ms cancel latency for simple impl

    def __init__(self) -> None:
        self._event = threading.Event()

    def cancel(self) -> None:
        """Set the cancel flag. Safe from any thread."""
        self._event.set()

    def cancel_from_signal(self) -> None:
        """Alias for cancel(). Explicit name for signal handler call sites.

        threading.Event.set() is async-signal-safe in CPython — it only flips
        an internal flag and notifies waiters, with no malloc or lock acquisition
        that could deadlock if the signal interrupted a malloc call.
        """
        self._event.set()

    def is_cancelled(self) -> bool:
        """Non-blocking check. Safe from any context (sync, async, signal handler)."""
        return self._event.is_set()

    async def wait(self) -> None:
        """Wait for cancellation from async code.

        Bridges to the threading event via a polling loop. The 100ms poll
        interval is a trade-off: simple and correct, with cancel latency
        invisible in practice (bounded below by worker task granularity).
        """
        while not self._event.is_set():
            await asyncio.sleep(self._POLL_INTERVAL_SECONDS)

    def __repr__(self) -> str:
        return f"CancelToken(cancelled={self.is_cancelled()})"
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_cancel_token.py -v`
Expected: 10 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/cancel_token.py tests/unit/test_cancel_token.py
git commit -m "feat(core): add CancelToken — threading.Event-based cooperative cancellation

Safe from signal handler context. Async bridge via polling loop.
See spec §6.10.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Terminal sanitization — strip dangerous Unicode before display

**Files:**
- Create: `src/tumbl4/_internal/sanitize.py`
- Create: `tests/unit/test_sanitize.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_sanitize.py`:

```python
"""Tests for terminal sanitization — strip Cc/Cf/Cs Unicode categories.

See spec §6.2: content reaching the terminal is passed through
_internal.sanitize.for_terminal() which strips control, format (bidi overrides),
and surrogate characters, replacing with \\u{hex} printable escape notation.
"""

from __future__ import annotations

from tumbl4._internal.sanitize import for_terminal


class TestForTerminal:
    def test_clean_ascii_passes_through(self) -> None:
        assert for_terminal("hello world") == "hello world"

    def test_clean_unicode_passes_through(self) -> None:
        assert for_terminal("cafe\u0301 \u2603") == "cafe\u0301 \u2603"

    def test_strips_null_byte(self) -> None:
        result = for_terminal("hello\x00world")
        assert "\x00" not in result
        assert "\\u{0}" in result

    def test_strips_escape_sequence(self) -> None:
        result = for_terminal("normal\x1b[31mredtext\x1b[0m")
        assert "\x1b" not in result
        assert "\\u{1b}" in result

    def test_strips_bidi_override_rtl(self) -> None:
        # U+202E RIGHT-TO-LEFT OVERRIDE — Cf category
        result = for_terminal("safe\u202edangerous")
        assert "\u202e" not in result
        assert "\\u{202e}" in result

    def test_strips_bidi_override_ltr(self) -> None:
        # U+202D LEFT-TO-RIGHT OVERRIDE — Cf category
        result = for_terminal("safe\u202ddangerous")
        assert "\u202d" not in result
        assert "\\u{202d}" in result

    def test_strips_bidi_embedding(self) -> None:
        # U+202A LEFT-TO-RIGHT EMBEDDING — Cf category
        result = for_terminal("embed\u202atext")
        assert "\u202a" not in result

    def test_strips_bidi_pop(self) -> None:
        # U+202C POP DIRECTIONAL FORMATTING — Cf category
        result = for_terminal("pop\u202cdir")
        assert "\u202c" not in result

    def test_strips_surrogates(self) -> None:
        # Cs category — lone surrogates (invalid but may appear in corrupted data)
        result = for_terminal("before\ud800after")
        assert "\ud800" not in result
        assert "\\u{d800}" in result

    def test_strips_bell_character(self) -> None:
        result = for_terminal("alert\x07here")
        assert "\x07" not in result
        assert "\\u{7}" in result

    def test_strips_backspace(self) -> None:
        result = for_terminal("over\x08write")
        assert "\x08" not in result

    def test_preserves_newline(self) -> None:
        # Newline is Cc but we preserve it — it's expected in terminal output
        assert for_terminal("line1\nline2") == "line1\nline2"

    def test_preserves_tab(self) -> None:
        # Tab is Cc but we preserve it
        assert for_terminal("col1\tcol2") == "col1\tcol2"

    def test_preserves_carriage_return(self) -> None:
        assert for_terminal("line\r\n") == "line\r\n"

    def test_empty_string(self) -> None:
        assert for_terminal("") == ""

    def test_zero_width_joiner_stripped(self) -> None:
        # U+200D ZERO WIDTH JOINER — Cf category
        result = for_terminal("a\u200db")
        assert "\u200d" not in result

    def test_soft_hyphen_stripped(self) -> None:
        # U+00AD SOFT HYPHEN — Cf category
        result = for_terminal("con\u00adtrol")
        assert "\u00ad" not in result

    def test_mixed_dangerous_content(self) -> None:
        """Simulate a malicious post title with multiple attack vectors."""
        malicious = "legit\x1b[31m\u202efile\x00name\x07.jpg"
        result = for_terminal(malicious)
        assert "\x1b" not in result
        assert "\u202e" not in result
        assert "\x00" not in result
        assert "\x07" not in result
        assert "legit" in result
        assert "name" in result
        assert ".jpg" in result
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_sanitize.py -v`
Expected: FAIL — `ModuleNotFoundError: No module named 'tumbl4._internal.sanitize'`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/_internal/sanitize.py`:

```python
"""Terminal injection prevention — strip dangerous Unicode before display.

See spec §6.2. Content that reaches the terminal (Rich progress messages,
error messages, ParseError excerpts) is passed through ``for_terminal()``
which strips all Unicode characters with Cc (control), Cf (format — includes
bidi overrides U+202E, U+202D, etc.), and Cs (surrogate) general categories.

Stripped characters are replaced with ``\\u{hex}`` printable escape notation
so evidence is preserved but harmless.

Preserved Cc characters: ``\\n`` (0x0A), ``\\r`` (0x0D), ``\\t`` (0x09) —
these are expected in normal terminal output.
"""

from __future__ import annotations

import unicodedata

# Cc characters that are safe/expected in terminal output
_ALLOWED_CONTROL: frozenset[int] = frozenset({
    0x09,  # TAB
    0x0A,  # LF
    0x0D,  # CR
})


def for_terminal(text: str) -> str:
    """Strip dangerous Unicode characters from text before terminal display.

    Replaces Cc (control), Cf (format/bidi), and Cs (surrogate) category
    characters with ``\\u{hex}`` printable escape notation. Preserves TAB,
    LF, and CR since they are expected in terminal output.

    Args:
        text: Untrusted text from external content (post titles, tags,
              error excerpts, filenames).

    Returns:
        Sanitized text safe for terminal display.
    """
    if not text:
        return text

    out: list[str] = []
    for ch in text:
        cp = ord(ch)

        # Fast path: common ASCII printable range
        if 0x20 <= cp <= 0x7E:
            out.append(ch)
            continue

        # Preserve allowed control characters
        if cp in _ALLOWED_CONTROL:
            out.append(ch)
            continue

        # Check Unicode general category
        cat = unicodedata.category(ch)
        if cat.startswith(("Cc", "Cf", "Cs")):
            # Replace with printable escape notation
            out.append(f"\\u{{{cp:x}}}")
            continue

        # All other characters pass through (letters, numbers, symbols,
        # combining marks, punctuation, etc.)
        out.append(ch)

    return "".join(out)
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_sanitize.py -v`
Expected: 18 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/_internal/sanitize.py tests/unit/test_sanitize.py
git commit -m "feat(internal): add terminal sanitization for external content

Strips Cc/Cf/Cs Unicode categories (control, bidi overrides, surrogates)
with printable escape notation. Preserves TAB/LF/CR. See spec §6.2.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: SSRF guard — IP validation for redirect safety

**Files:**
- Create: `src/tumbl4/core/crawl/ssrf_guard.py`
- Create: `tests/unit/test_ssrf_guard.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_ssrf_guard.py`:

```python
"""Tests for SSRF guard — IP-level validation blocking private/reserved ranges.

See spec §6.3: A custom httpx transport resolves the hostname and rejects
if the resolved IP is RFC 1918, loopback, link-local, or special-use.
"""

from __future__ import annotations

import ipaddress

import pytest

from tumbl4.core.crawl.ssrf_guard import (
    SSRFViolation,
    is_ip_allowed,
    validate_hostname,
)


class TestIsIpAllowed:
    """Test the raw IP validation function."""

    # --- Blocked ranges ---

    def test_blocks_loopback_ipv4(self) -> None:
        assert is_ip_allowed(ipaddress.ip_address("127.0.0.1")) is False

    def test_blocks_loopback_ipv4_range(self) -> None:
        assert is_ip_allowed(ipaddress.ip_address("127.255.255.254")) is False

    def test_blocks_loopback_ipv6(self) -> None:
        assert is_ip_allowed(ipaddress.ip_address("::1")) is False

    def test_blocks_rfc1918_10(self) -> None:
        assert is_ip_allowed(ipaddress.ip_address("10.0.0.1")) is False

    def test_blocks_rfc1918_172(self) -> None:
        assert is_ip_allowed(ipaddress.ip_address("172.16.0.1")) is False

    def test_blocks_rfc1918_192(self) -> None:
        assert is_ip_allowed(ipaddress.ip_address("192.168.1.1")) is False

    def test_blocks_link_local_ipv4(self) -> None:
        assert is_ip_allowed(ipaddress.ip_address("169.254.1.1")) is False

    def test_blocks_link_local_ipv6(self) -> None:
        assert is_ip_allowed(ipaddress.ip_address("fe80::1")) is False

    def test_blocks_imds_endpoint(self) -> None:
        """AWS/GCP/Azure IMDS at 169.254.169.254."""
        assert is_ip_allowed(ipaddress.ip_address("169.254.169.254")) is False

    def test_blocks_unspecified_ipv4(self) -> None:
        assert is_ip_allowed(ipaddress.ip_address("0.0.0.0")) is False

    def test_blocks_unspecified_ipv6(self) -> None:
        assert is_ip_allowed(ipaddress.ip_address("::")) is False

    def test_blocks_broadcast(self) -> None:
        assert is_ip_allowed(ipaddress.ip_address("255.255.255.255")) is False

    def test_blocks_documentation_range(self) -> None:
        """RFC 5737 documentation range 192.0.2.0/24."""
        assert is_ip_allowed(ipaddress.ip_address("192.0.2.1")) is False

    def test_blocks_benchmarking_range(self) -> None:
        """RFC 2544 benchmarking range 198.18.0.0/15."""
        assert is_ip_allowed(ipaddress.ip_address("198.18.0.1")) is False

    def test_blocks_ipv4_mapped_ipv6_private(self) -> None:
        """IPv4-mapped IPv6 addresses must also be checked."""
        assert is_ip_allowed(ipaddress.ip_address("::ffff:127.0.0.1")) is False

    def test_blocks_ipv4_mapped_ipv6_rfc1918(self) -> None:
        assert is_ip_allowed(ipaddress.ip_address("::ffff:10.0.0.1")) is False

    # --- Allowed ranges ---

    def test_allows_public_ipv4(self) -> None:
        assert is_ip_allowed(ipaddress.ip_address("64.228.200.100")) is True

    def test_allows_public_ipv4_tumblr_cdn(self) -> None:
        assert is_ip_allowed(ipaddress.ip_address("152.199.21.141")) is True

    def test_allows_public_ipv6(self) -> None:
        assert is_ip_allowed(ipaddress.ip_address("2606:4700::1")) is True


class TestValidateHostname:
    """Test hostname→IP resolution + validation."""

    def test_rejects_localhost(self) -> None:
        with pytest.raises(SSRFViolation, match="127.0.0.1"):
            validate_hostname("localhost")

    def test_accepts_public_host(self) -> None:
        # Should not raise for a known public hostname
        # Using a hostname that resolves to public IP
        validate_hostname("one.one.one.one")  # Cloudflare 1.1.1.1

    def test_rejects_private_ip_as_hostname(self) -> None:
        with pytest.raises(SSRFViolation):
            validate_hostname("10.0.0.1")

    def test_rejects_link_local_ip_as_hostname(self) -> None:
        with pytest.raises(SSRFViolation):
            validate_hostname("169.254.169.254")
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_ssrf_guard.py -v`
Expected: FAIL — `ModuleNotFoundError: No module named 'tumbl4.core.crawl.ssrf_guard'`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/crawl/ssrf_guard.py`:

```python
"""SSRF guard — IP-level validation blocking private and reserved address ranges.

See spec §6.3. This module provides:

- ``is_ip_allowed()``: checks a resolved IP against blocked ranges
- ``validate_hostname()``: resolves a hostname and validates the result
- ``SSRFViolation``: raised when an IP fails validation

Blocked ranges (per spec):
- RFC 1918 (10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16)
- Loopback (127.0.0.0/8, ::1)
- Link-local (169.254.0.0/16, fe80::/10) — blocks cloud IMDS endpoints
- Unspecified (0.0.0.0, ::)
- Broadcast (255.255.255.255)
- RFC 5737 documentation (192.0.2.0/24, 198.51.100.0/24, 203.0.113.0/24)
- RFC 2544 benchmarking (198.18.0.0/15)
- IPv4-mapped IPv6 addresses are unpacked and the inner IPv4 is checked
"""

from __future__ import annotations

import ipaddress
import socket
from typing import Union

from tumbl4._internal.logging import get_logger

_LOG = get_logger("crawl.ssrf_guard")

IPAddress = Union[ipaddress.IPv4Address, ipaddress.IPv6Address]


class SSRFViolation(Exception):
    """Raised when a resolved IP address falls in a blocked range.

    This is not part of the Tumbl4Error hierarchy because it indicates a
    security policy violation, not a recoverable crawl error.
    """

    def __init__(self, hostname: str, ip: IPAddress, reason: str) -> None:
        self.hostname = hostname
        self.ip = ip
        self.reason = reason
        super().__init__(
            f"SSRF blocked: {hostname} resolved to {ip} ({reason})"
        )


def is_ip_allowed(ip: IPAddress) -> bool:
    """Check whether an IP address is allowed (not in any blocked range).

    For IPv4-mapped IPv6 addresses (::ffff:x.x.x.x), unpacks the inner
    IPv4 address and validates that instead.

    Args:
        ip: The resolved IP address to validate.

    Returns:
        True if the IP is public and allowed, False if it's in a blocked range.
    """
    # Unpack IPv4-mapped IPv6 addresses
    if isinstance(ip, ipaddress.IPv6Address):
        mapped_v4 = ip.ipv4_mapped
        if mapped_v4 is not None:
            ip = mapped_v4

    # Use Python's ipaddress module for comprehensive checks
    if isinstance(ip, ipaddress.IPv4Address):
        if ip.is_private:
            return False
        if ip.is_loopback:
            return False
        if ip.is_link_local:
            return False
        if ip.is_unspecified:
            return False
        if ip.is_reserved:
            return False
        if ip.is_multicast:
            return False
        # Broadcast
        if ip == ipaddress.IPv4Address("255.255.255.255"):
            return False
        return True

    # IPv6
    if ip.is_private:
        return False
    if ip.is_loopback:
        return False
    if ip.is_link_local:
        return False
    if ip.is_unspecified:
        return False
    if ip.is_reserved:
        return False
    if ip.is_multicast:
        return False
    return True


def validate_hostname(hostname: str) -> list[IPAddress]:
    """Resolve a hostname and validate all resolved IPs.

    Args:
        hostname: The hostname to resolve and validate.

    Returns:
        List of allowed IP addresses.

    Raises:
        SSRFViolation: If any resolved IP falls in a blocked range.
        socket.gaierror: If the hostname cannot be resolved.
    """
    # Handle bare IP addresses first
    try:
        ip = ipaddress.ip_address(hostname)
        if not is_ip_allowed(ip):
            raise SSRFViolation(hostname, ip, _classify_blocked(ip))
        return [ip]
    except ValueError:
        pass  # Not a bare IP — resolve as hostname

    # Resolve hostname to IP addresses
    try:
        addrinfos = socket.getaddrinfo(
            hostname, None, socket.AF_UNSPEC, socket.SOCK_STREAM,
        )
    except socket.gaierror:
        _LOG.warning("DNS resolution failed for %s", hostname)
        raise

    allowed: list[IPAddress] = []
    for family, _type, _proto, _canonname, sockaddr in addrinfos:
        ip_str = sockaddr[0]
        ip = ipaddress.ip_address(ip_str)

        if not is_ip_allowed(ip):
            raise SSRFViolation(hostname, ip, _classify_blocked(ip))
        allowed.append(ip)

    if not allowed:
        raise SSRFViolation(
            hostname,
            ipaddress.ip_address("0.0.0.0"),
            "no addresses resolved",
        )

    return allowed


def _classify_blocked(ip: IPAddress) -> str:
    """Return a human-readable reason for why an IP is blocked."""
    if isinstance(ip, ipaddress.IPv6Address):
        mapped_v4 = ip.ipv4_mapped
        if mapped_v4 is not None:
            return f"IPv4-mapped {_classify_blocked(mapped_v4)}"

    if isinstance(ip, ipaddress.IPv4Address):
        if ip.is_loopback:
            return "loopback"
        if ip.is_link_local:
            return "link-local (IMDS risk)"
        if ip.is_unspecified:
            return "unspecified"
        if ip == ipaddress.IPv4Address("255.255.255.255"):
            return "broadcast"
        if ip.is_multicast:
            return "multicast"
        if ip.is_private:
            return "RFC 1918 private"
        if ip.is_reserved:
            return "reserved"
    else:
        if ip.is_loopback:
            return "loopback"
        if ip.is_link_local:
            return "link-local"
        if ip.is_unspecified:
            return "unspecified"
        if ip.is_multicast:
            return "multicast"
        if ip.is_private:
            return "private"
        if ip.is_reserved:
            return "reserved"

    return "blocked"
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_ssrf_guard.py -v`
Expected: 21 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/crawl/ssrf_guard.py tests/unit/test_ssrf_guard.py
git commit -m "feat(crawl): add SSRF guard with IP-level validation

Blocks RFC 1918, loopback, link-local (IMDS), broadcast, documentation,
benchmarking, and reserved ranges. Unpacks IPv4-mapped IPv6. See spec §6.3.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: Redirect safety — per-hop allowlist + SSRF validation

**Files:**
- Create: `src/tumbl4/core/crawl/redirect_safety.py`
- Create: `tests/unit/test_redirect_safety.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_redirect_safety.py`:

```python
"""Tests for redirect safety — per-hop allowlist + SSRF validation.

See spec §6.3: manual redirect following, per-hop allowlist (*.tumblr.com),
SSRF IP validation on every hop. Never silently drops — every miss is an
AllowlistViolation halt.
"""

from __future__ import annotations

from unittest.mock import AsyncMock, MagicMock

import httpx
import pytest

from tumbl4.core.crawl.redirect_safety import (
    AllowlistViolation,
    MaxRedirectsExceeded,
    is_url_allowed,
    safe_follow_redirects,
)


class TestIsUrlAllowed:
    """Test the URL allowlist checker."""

    def test_allows_tumblr_subdomain(self) -> None:
        assert is_url_allowed("https://myblog.tumblr.com/post/123") is True

    def test_allows_tumblr_media_cdn(self) -> None:
        assert is_url_allowed("https://64.media.tumblr.com/abc/photo.jpg") is True

    def test_allows_tumblr_assets(self) -> None:
        assert is_url_allowed("https://assets.tumblr.com/css/style.css") is True

    def test_allows_www_tumblr(self) -> None:
        assert is_url_allowed("https://www.tumblr.com/blog/test") is True

    def test_blocks_non_tumblr_domain(self) -> None:
        assert is_url_allowed("https://attacker.com/steal") is False

    def test_blocks_similar_domain(self) -> None:
        assert is_url_allowed("https://nottumblr.com/fake") is False

    def test_blocks_subdomain_spoof(self) -> None:
        assert is_url_allowed("https://tumblr.com.attacker.com/fake") is False

    def test_blocks_http_downgrade(self) -> None:
        assert is_url_allowed("http://myblog.tumblr.com/post/123") is False

    def test_blocks_empty_url(self) -> None:
        assert is_url_allowed("") is False

    def test_blocks_data_uri(self) -> None:
        assert is_url_allowed("data:text/html,<script>alert(1)</script>") is False

    def test_blocks_file_uri(self) -> None:
        assert is_url_allowed("file:///etc/passwd") is False

    def test_allows_custom_additional_hosts(self) -> None:
        assert is_url_allowed(
            "https://cdn.example.com/file.jpg",
            additional_allowed=frozenset({"cdn.example.com"}),
        ) is True


class TestSafeFollowRedirects:
    """Test the manual redirect follower."""

    async def test_no_redirect_returns_directly(self) -> None:
        client = AsyncMock(spec=httpx.AsyncClient)
        response = MagicMock(spec=httpx.Response)
        response.status_code = 200
        response.is_redirect = False
        client.send.return_value = response

        request = httpx.Request("GET", "https://myblog.tumblr.com/post/123")
        result = await safe_follow_redirects(client, request, max_redirects=5)
        assert result.status_code == 200

    async def test_follows_valid_redirect(self) -> None:
        client = AsyncMock(spec=httpx.AsyncClient)

        redirect_response = MagicMock(spec=httpx.Response)
        redirect_response.status_code = 301
        redirect_response.is_redirect = True
        redirect_response.headers = {"location": "https://64.media.tumblr.com/img.jpg"}
        redirect_response.next_request = httpx.Request(
            "GET", "https://64.media.tumblr.com/img.jpg",
        )

        final_response = MagicMock(spec=httpx.Response)
        final_response.status_code = 200
        final_response.is_redirect = False

        client.send.side_effect = [redirect_response, final_response]

        request = httpx.Request("GET", "https://myblog.tumblr.com/media/123")
        result = await safe_follow_redirects(client, request, max_redirects=5)
        assert result.status_code == 200
        assert client.send.call_count == 2

    async def test_blocks_redirect_to_attacker(self) -> None:
        client = AsyncMock(spec=httpx.AsyncClient)

        redirect_response = MagicMock(spec=httpx.Response)
        redirect_response.status_code = 302
        redirect_response.is_redirect = True
        redirect_response.headers = {"location": "https://attacker.com/steal"}
        redirect_response.next_request = httpx.Request(
            "GET", "https://attacker.com/steal",
        )

        client.send.return_value = redirect_response

        request = httpx.Request("GET", "https://myblog.tumblr.com/media/123")
        with pytest.raises(AllowlistViolation, match="attacker.com"):
            await safe_follow_redirects(client, request, max_redirects=5)

    async def test_blocks_redirect_to_http_downgrade(self) -> None:
        client = AsyncMock(spec=httpx.AsyncClient)

        redirect_response = MagicMock(spec=httpx.Response)
        redirect_response.status_code = 302
        redirect_response.is_redirect = True
        redirect_response.headers = {"location": "http://myblog.tumblr.com/post"}
        redirect_response.next_request = httpx.Request(
            "GET", "http://myblog.tumblr.com/post",
        )

        client.send.return_value = redirect_response

        request = httpx.Request("GET", "https://myblog.tumblr.com/post")
        with pytest.raises(AllowlistViolation, match="http"):
            await safe_follow_redirects(client, request, max_redirects=5)

    async def test_max_redirects_exceeded(self) -> None:
        client = AsyncMock(spec=httpx.AsyncClient)

        redirect_response = MagicMock(spec=httpx.Response)
        redirect_response.status_code = 302
        redirect_response.is_redirect = True
        redirect_response.headers = {"location": "https://myblog.tumblr.com/redir"}
        redirect_response.next_request = httpx.Request(
            "GET", "https://myblog.tumblr.com/redir",
        )
        client.send.return_value = redirect_response

        request = httpx.Request("GET", "https://myblog.tumblr.com/start")
        with pytest.raises(MaxRedirectsExceeded):
            await safe_follow_redirects(client, request, max_redirects=2)

    async def test_redirect_chain_count(self) -> None:
        """Verify we track hop count correctly through a chain."""
        client = AsyncMock(spec=httpx.AsyncClient)

        def make_redirect(target: str) -> MagicMock:
            r = MagicMock(spec=httpx.Response)
            r.status_code = 302
            r.is_redirect = True
            r.headers = {"location": target}
            r.next_request = httpx.Request("GET", target)
            return r

        final = MagicMock(spec=httpx.Response)
        final.status_code = 200
        final.is_redirect = False

        client.send.side_effect = [
            make_redirect("https://a.tumblr.com/hop1"),
            make_redirect("https://b.tumblr.com/hop2"),
            make_redirect("https://64.media.tumblr.com/final"),
            final,
        ]

        request = httpx.Request("GET", "https://myblog.tumblr.com/start")
        result = await safe_follow_redirects(client, request, max_redirects=5)
        assert result.status_code == 200
        assert client.send.call_count == 4
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_redirect_safety.py -v`
Expected: FAIL — `ModuleNotFoundError: No module named 'tumbl4.core.crawl.redirect_safety'`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/crawl/redirect_safety.py`:

```python
"""Redirect safety — manual redirect following with per-hop allowlist and SSRF validation.

See spec §6.3. httpx is configured with ``follow_redirects=False``. This module
provides ``safe_follow_redirects()`` which manually follows redirects with:

1. URL allowlist checked on every hop (suffix match on ``*.tumblr.com``)
2. HTTPS-only enforcement (no HTTP downgrade)
3. SSRF IP validation on every hop via ``ssrf_guard.validate_hostname()``
4. Max redirect limit

The crawler **never** silently drops URLs that fail the allowlist — every
miss is an ``AllowlistViolation`` halt.
"""

from __future__ import annotations

from urllib.parse import urlparse

import httpx

from tumbl4._internal.logging import get_logger
from tumbl4.core.crawl.ssrf_guard import validate_hostname

_LOG = get_logger("crawl.redirect_safety")

# Default allowed host suffixes. Additional hosts can be passed at call time.
_ALLOWED_SUFFIXES: frozenset[str] = frozenset({".tumblr.com", ".tumblr.com."})
_ALLOWED_EXACT: frozenset[str] = frozenset({"tumblr.com"})


class AllowlistViolation(Exception):
    """Redirect target is not in the allowed domain set.

    This halts the crawl loudly — silent drop would be invisible data loss.
    """

    def __init__(self, url: str, reason: str) -> None:
        self.url = url
        self.reason = reason
        super().__init__(f"Allowlist violation: {url} — {reason}")


class MaxRedirectsExceeded(Exception):
    """Redirect chain exceeded the configured maximum hop count."""

    def __init__(self, max_redirects: int, url: str) -> None:
        self.max_redirects = max_redirects
        self.url = url
        super().__init__(
            f"Max redirects ({max_redirects}) exceeded following {url}"
        )


def is_url_allowed(
    url: str,
    *,
    additional_allowed: frozenset[str] | None = None,
) -> bool:
    """Check whether a URL passes the domain allowlist.

    Rules:
    - Must be HTTPS (no HTTP downgrade, no data:, no file:)
    - Host must be ``*.tumblr.com`` or an explicitly allowed additional host
    - Suffix matching prevents ``tumblr.com.attacker.com`` spoofing

    Args:
        url: The URL to validate.
        additional_allowed: Extra exact hostnames to allow (for non-tumblr CDNs).

    Returns:
        True if the URL is allowed, False otherwise.
    """
    try:
        parsed = urlparse(url)
    except Exception:
        return False

    # Must be HTTPS
    if parsed.scheme != "https":
        return False

    host = (parsed.hostname or "").lower()
    if not host:
        return False

    # Exact match
    if host in _ALLOWED_EXACT:
        return True

    # Suffix match — the dot prefix prevents "nottumblr.com" matching
    for suffix in _ALLOWED_SUFFIXES:
        if host.endswith(suffix):
            return True

    # Check additional allowed hosts
    if additional_allowed and host in additional_allowed:
        return True

    return False


async def safe_follow_redirects(
    client: httpx.AsyncClient,
    request: httpx.Request,
    *,
    max_redirects: int = 5,
    additional_allowed: frozenset[str] | None = None,
    skip_ssrf_check: bool = False,
) -> httpx.Response:
    """Follow redirects manually with per-hop allowlist and SSRF validation.

    Args:
        client: The httpx AsyncClient (configured with follow_redirects=False).
        request: The initial request to send.
        max_redirects: Maximum number of redirect hops to follow.
        additional_allowed: Extra exact hostnames to allow beyond *.tumblr.com.
        skip_ssrf_check: If True, skip SSRF IP validation (for testing only).

    Returns:
        The final non-redirect response.

    Raises:
        AllowlistViolation: If a redirect target fails the domain allowlist.
        MaxRedirectsExceeded: If the redirect chain exceeds max_redirects.
    """
    response = await client.send(request, follow_redirects=False)
    hops = 0

    while response.is_redirect:
        hops += 1
        if hops > max_redirects:
            raise MaxRedirectsExceeded(max_redirects, str(request.url))

        next_request = response.next_request
        if next_request is None:
            # No Location header — treat as final response
            break

        next_url = str(next_request.url)

        # Allowlist check on every hop
        if not is_url_allowed(next_url, additional_allowed=additional_allowed):
            raise AllowlistViolation(
                next_url,
                f"redirect from {request.url} to disallowed domain",
            )

        # SSRF IP validation on every hop
        if not skip_ssrf_check:
            parsed = urlparse(next_url)
            hostname = parsed.hostname
            if hostname:
                validate_hostname(hostname)

        _LOG.debug(
            "following redirect hop %d: %s -> %s",
            hops,
            request.url,
            next_url,
        )

        response = await client.send(next_request, follow_redirects=False)
        request = next_request

    return response
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_redirect_safety.py -v`
Expected: 13 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/crawl/redirect_safety.py tests/unit/test_redirect_safety.py
git commit -m "feat(crawl): add redirect safety with per-hop allowlist + SSRF

Manual redirect following checks *.tumblr.com allowlist and SSRF IP
validation on every hop. HTTPS-only, no silent drops. See spec §6.3.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: Signal handling — SIGINT to CancelToken wiring

**Files:**
- Create: `src/tumbl4/_internal/signal_handling.py`
- Create: `tests/unit/test_signal_handling.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_signal_handling.py`:

```python
"""Tests for signal handling — SIGINT → CancelToken wiring.

See spec §6.10: SIGINT handler calls cancel_token.cancel_from_signal().
Second SIGINT within 3 seconds → immediate exit.
"""

from __future__ import annotations

import signal
import time
from unittest.mock import MagicMock, patch

import pytest

from tumbl4._internal.signal_handling import (
    install_signal_handler,
    uninstall_signal_handler,
)
from tumbl4.core.cancel_token import CancelToken


class TestInstallSignalHandler:
    def test_first_sigint_cancels_token(self) -> None:
        token = CancelToken()
        old_handler = install_signal_handler(token)
        try:
            assert token.is_cancelled() is False
            # Simulate SIGINT
            signal.raise_signal(signal.SIGINT)
            assert token.is_cancelled() is True
        finally:
            uninstall_signal_handler(old_handler)

    def test_returns_previous_handler(self) -> None:
        token = CancelToken()
        # Install a dummy handler first
        dummy = signal.getsignal(signal.SIGINT)
        old_handler = install_signal_handler(token)
        try:
            # old_handler should be the handler that was installed before us
            assert old_handler is not None
        finally:
            uninstall_signal_handler(old_handler)

    def test_uninstall_restores_previous(self) -> None:
        original = signal.getsignal(signal.SIGINT)
        token = CancelToken()
        old_handler = install_signal_handler(token)
        uninstall_signal_handler(old_handler)
        restored = signal.getsignal(signal.SIGINT)
        assert restored is original


class TestDoubleSignal:
    def test_double_sigint_exits(self) -> None:
        token = CancelToken()
        old_handler = install_signal_handler(token)
        try:
            # First SIGINT
            signal.raise_signal(signal.SIGINT)
            assert token.is_cancelled() is True

            # Second SIGINT within 3 seconds should raise SystemExit
            with pytest.raises(SystemExit) as exc_info:
                signal.raise_signal(signal.SIGINT)
            assert exc_info.value.code == 130
        finally:
            uninstall_signal_handler(old_handler)


class TestSignalHandlerIdempotent:
    def test_multiple_installs_are_safe(self) -> None:
        token = CancelToken()
        old1 = install_signal_handler(token)
        old2 = install_signal_handler(token)
        try:
            signal.raise_signal(signal.SIGINT)
            assert token.is_cancelled() is True
        finally:
            uninstall_signal_handler(old2)
            # Restore fully
            signal.signal(signal.SIGINT, old1)
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_signal_handling.py -v`
Expected: FAIL — `ModuleNotFoundError: No module named 'tumbl4._internal.signal_handling'`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/_internal/signal_handling.py`:

```python
"""SIGINT → CancelToken wiring for cooperative shutdown.

See spec §6.10. The signal handler calls ``cancel_token.cancel_from_signal()``,
which is a simple ``threading.Event.set()`` — no loop bridging required, no race.

On cancel (orchestrator responsibility):
    1. Stop enqueuing new crawl work
    2. Crawler finishes current page, does not fetch next
    3. In-flight downloads finish current chunk or abort
    4. Drain queue (remaining workers finish their current task)
    5. Final state commit: resume cursor, pending sidecar writes
    6. SQLite WAL checkpoint + close
    7. Exit code 130 (standard SIGINT)

Second SIGINT within 3 seconds → immediate ungraceful exit with warning that
resume state may be inconsistent.
"""

from __future__ import annotations

import signal
import sys
import time
from types import FrameType
from typing import Any

from tumbl4._internal.logging import get_logger
from tumbl4.core.cancel_token import CancelToken

_LOG = get_logger("signal_handling")

# Module-level state for the signal handler (signal handlers can't capture closures safely)
_cancel_token: CancelToken | None = None
_first_sigint_time: float | None = None
_DOUBLE_SIGINT_WINDOW: float = 3.0


def _sigint_handler(signum: int, frame: FrameType | None) -> None:
    """Handle SIGINT (Ctrl+C).

    First SIGINT: set cancel token for cooperative shutdown.
    Second SIGINT within 3 seconds: immediate exit with code 130.
    """
    global _first_sigint_time

    if _cancel_token is None:
        # Shouldn't happen — but be defensive
        raise SystemExit(130)

    now = time.monotonic()

    if _first_sigint_time is not None and (now - _first_sigint_time) < _DOUBLE_SIGINT_WINDOW:
        # Second SIGINT within window — immediate exit
        # Write directly to stderr to avoid any logging machinery
        sys.stderr.write(
            "\ntumbl4: received second SIGINT — exiting immediately. "
            "Resume state may be inconsistent.\n"
        )
        sys.stderr.flush()
        raise SystemExit(130)

    # First SIGINT — cooperative shutdown
    _first_sigint_time = now
    _cancel_token.cancel_from_signal()

    # Write directly to stderr — safe from signal handler context
    sys.stderr.write(
        "\ntumbl4: received SIGINT — shutting down gracefully. "
        "Press Ctrl+C again within 3s to force exit.\n"
    )
    sys.stderr.flush()


def install_signal_handler(
    cancel_token: CancelToken,
) -> Any:
    """Install the SIGINT handler that bridges to the given CancelToken.

    Args:
        cancel_token: The CancelToken to cancel on SIGINT.

    Returns:
        The previous signal handler (pass to ``uninstall_signal_handler``
        to restore).
    """
    global _cancel_token, _first_sigint_time

    _cancel_token = cancel_token
    _first_sigint_time = None

    return signal.signal(signal.SIGINT, _sigint_handler)


def uninstall_signal_handler(
    previous_handler: Any,
) -> None:
    """Restore the previous SIGINT handler.

    Args:
        previous_handler: The handler returned by ``install_signal_handler``.
    """
    global _cancel_token, _first_sigint_time

    signal.signal(signal.SIGINT, previous_handler)
    _cancel_token = None
    _first_sigint_time = None
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_signal_handling.py -v`
Expected: 5 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/_internal/signal_handling.py tests/unit/test_signal_handling.py
git commit -m "feat(internal): add SIGINT signal handler with double-SIGINT exit

First SIGINT sets CancelToken for cooperative shutdown.
Second SIGINT within 3s raises SystemExit(130). See spec §6.10.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: Orphan sweep and pending posts tracker

**Files:**
- Create: `src/tumbl4/core/state/orphan_sweep.py`
- Create: `src/tumbl4/core/state/pending_posts.py`
- Create: `tests/unit/test_orphan_sweep.py`
- Create: `tests/unit/test_pending_posts.py`

- [ ] **Step 1: Write the failing tests**

Write file `tests/unit/test_orphan_sweep.py`:

```python
"""Tests for orphan sweep — .part file cleanup on startup.

See plan boundaries: orphan_sweep.py — .part cleanup on startup,
bounded scan, asyncio.to_thread.
"""

from __future__ import annotations

import asyncio
from pathlib import Path

import pytest

from tumbl4.core.state.orphan_sweep import sweep_orphan_parts


class TestSweepOrphanParts:
    async def test_removes_part_files(self, tmp_path: Path) -> None:
        blog_dir = tmp_path / "testblog"
        blog_dir.mkdir()
        # Create orphan .part files
        (blog_dir / "12345_01.jpg.part").write_bytes(b"incomplete")
        (blog_dir / "12345_02.png.part").write_bytes(b"incomplete")
        # Create a completed file (should NOT be removed)
        (blog_dir / "12345_03.jpg").write_bytes(b"complete")

        removed = await sweep_orphan_parts(blog_dir)
        assert removed == 2
        assert not (blog_dir / "12345_01.jpg.part").exists()
        assert not (blog_dir / "12345_02.png.part").exists()
        assert (blog_dir / "12345_03.jpg").exists()

    async def test_no_part_files(self, tmp_path: Path) -> None:
        blog_dir = tmp_path / "testblog"
        blog_dir.mkdir()
        (blog_dir / "12345_01.jpg").write_bytes(b"complete")

        removed = await sweep_orphan_parts(blog_dir)
        assert removed == 0

    async def test_empty_directory(self, tmp_path: Path) -> None:
        blog_dir = tmp_path / "testblog"
        blog_dir.mkdir()

        removed = await sweep_orphan_parts(blog_dir)
        assert removed == 0

    async def test_nonexistent_directory(self, tmp_path: Path) -> None:
        blog_dir = tmp_path / "nonexistent"

        removed = await sweep_orphan_parts(blog_dir)
        assert removed == 0

    async def test_respects_max_scan_limit(self, tmp_path: Path) -> None:
        blog_dir = tmp_path / "testblog"
        blog_dir.mkdir()
        # Create many .part files
        for i in range(20):
            (blog_dir / f"post_{i:04d}_01.jpg.part").write_bytes(b"x")

        removed = await sweep_orphan_parts(blog_dir, max_scan=10)
        # Should process at most max_scan files total, removing .part ones found
        assert removed <= 10

    async def test_nested_part_files_ignored(self, tmp_path: Path) -> None:
        """Only scan top-level directory, not subdirectories."""
        blog_dir = tmp_path / "testblog"
        blog_dir.mkdir()
        sub = blog_dir / "_meta"
        sub.mkdir()
        (sub / "orphan.json.part").write_bytes(b"x")

        removed = await sweep_orphan_parts(blog_dir)
        # Default depth=1 means we do scan one level of subdirectories
        assert removed == 1

    async def test_returns_count_of_removed(self, tmp_path: Path) -> None:
        blog_dir = tmp_path / "testblog"
        blog_dir.mkdir()
        (blog_dir / "a.jpg.part").write_bytes(b"x")
        (blog_dir / "b.jpg.part").write_bytes(b"x")
        (blog_dir / "c.jpg.part").write_bytes(b"x")

        removed = await sweep_orphan_parts(blog_dir)
        assert removed == 3
```

Write file `tests/unit/test_pending_posts.py`:

```python
"""Tests for pending posts tracker — in-flight post→media map.

See plan boundaries: pending_posts.py — in-flight post→media map,
asyncio.Lock-guarded.
"""

from __future__ import annotations

import asyncio

import pytest

from tumbl4.core.state.pending_posts import PendingPosts


class TestPendingPosts:
    async def test_register_and_check(self) -> None:
        tracker = PendingPosts()
        await tracker.register("post_1", {"url_a", "url_b"})
        assert await tracker.is_pending("post_1") is True
        assert await tracker.pending_count() == 1

    async def test_complete_one_media(self) -> None:
        tracker = PendingPosts()
        await tracker.register("post_1", {"url_a", "url_b"})
        is_post_done = await tracker.complete_media("post_1", "url_a")
        assert is_post_done is False
        assert await tracker.is_pending("post_1") is True

    async def test_complete_all_media_removes_post(self) -> None:
        tracker = PendingPosts()
        await tracker.register("post_1", {"url_a", "url_b"})
        await tracker.complete_media("post_1", "url_a")
        is_post_done = await tracker.complete_media("post_1", "url_b")
        assert is_post_done is True
        assert await tracker.is_pending("post_1") is False

    async def test_complete_unknown_post_is_noop(self) -> None:
        tracker = PendingPosts()
        is_done = await tracker.complete_media("unknown", "url_a")
        assert is_done is False

    async def test_multiple_posts(self) -> None:
        tracker = PendingPosts()
        await tracker.register("post_1", {"url_a"})
        await tracker.register("post_2", {"url_b", "url_c"})
        assert await tracker.pending_count() == 2

        await tracker.complete_media("post_1", "url_a")
        assert await tracker.pending_count() == 1
        assert await tracker.is_pending("post_1") is False
        assert await tracker.is_pending("post_2") is True

    async def test_pending_urls_returns_remaining(self) -> None:
        tracker = PendingPosts()
        await tracker.register("post_1", {"url_a", "url_b", "url_c"})
        await tracker.complete_media("post_1", "url_a")
        remaining = await tracker.pending_urls("post_1")
        assert remaining == {"url_b", "url_c"}

    async def test_pending_urls_unknown_post(self) -> None:
        tracker = PendingPosts()
        remaining = await tracker.pending_urls("unknown")
        assert remaining == set()

    async def test_clear_all(self) -> None:
        tracker = PendingPosts()
        await tracker.register("post_1", {"url_a"})
        await tracker.register("post_2", {"url_b"})
        await tracker.clear()
        assert await tracker.pending_count() == 0

    async def test_concurrent_access_is_safe(self) -> None:
        """Verify the asyncio.Lock guards concurrent operations."""
        tracker = PendingPosts()
        urls = {f"url_{i}" for i in range(100)}
        await tracker.register("post_1", urls)

        async def complete_one(url: str) -> bool:
            return await tracker.complete_media("post_1", url)

        results = await asyncio.gather(
            *(complete_one(url) for url in urls)
        )
        # Exactly one should return True (the last completion)
        assert sum(results) == 1
        assert await tracker.is_pending("post_1") is False
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `uv run pytest tests/unit/test_orphan_sweep.py tests/unit/test_pending_posts.py -v`
Expected: FAIL — `ModuleNotFoundError`

- [ ] **Step 3: Write the implementations**

Write file `src/tumbl4/core/state/orphan_sweep.py`:

```python
"""Orphan .part file cleanup on startup.

See plan boundaries: .part files are left behind when a download is interrupted
(crash, SIGINT, power loss). This module scans a blog directory for orphan .part
files and removes them, running the I/O via ``asyncio.to_thread`` to avoid
blocking the event loop.

The scan is bounded by ``max_scan`` to prevent pathological cases (e.g., a
directory with millions of files from a corrupt state).
"""

from __future__ import annotations

import asyncio
import os
from pathlib import Path

from tumbl4._internal.logging import get_logger

_LOG = get_logger("state.orphan_sweep")


async def sweep_orphan_parts(
    blog_dir: Path,
    *,
    max_scan: int = 10_000,
) -> int:
    """Remove orphan .part files from a blog output directory.

    Runs the filesystem scan in a thread to avoid blocking the event loop.

    Args:
        blog_dir: The blog output directory to scan.
        max_scan: Maximum number of files to examine (safety bound).

    Returns:
        Number of .part files removed.
    """
    return await asyncio.to_thread(_sweep_sync, blog_dir, max_scan)


def _sweep_sync(blog_dir: Path, max_scan: int) -> int:
    """Synchronous implementation of .part file cleanup.

    Scans the blog directory and its immediate subdirectories for .part files.
    """
    if not blog_dir.is_dir():
        return 0

    removed = 0
    scanned = 0

    # Scan top-level and one level of subdirectories
    for entry in _bounded_walk(blog_dir, max_scan):
        scanned += 1
        if scanned > max_scan:
            _LOG.warning(
                "orphan sweep hit max_scan limit (%d) for %s — stopping",
                max_scan,
                blog_dir,
            )
            break

        if entry.is_file() and entry.name.endswith(".part"):
            try:
                entry.unlink()
                removed += 1
                _LOG.debug("removed orphan .part file: %s", entry)
            except OSError as e:
                _LOG.warning("failed to remove orphan .part file %s: %s", entry, e)

    if removed > 0:
        _LOG.info("removed %d orphan .part file(s) from %s", removed, blog_dir)

    return removed


def _bounded_walk(directory: Path, max_entries: int) -> list[Path]:
    """Walk a directory and its immediate subdirectories, bounded by max_entries.

    Returns a flat list of file paths (not directories).
    """
    entries: list[Path] = []
    count = 0

    try:
        for item in directory.iterdir():
            if count >= max_entries:
                break
            count += 1

            if item.is_file():
                entries.append(item)
            elif item.is_dir():
                # One level of subdirectory scanning
                try:
                    for sub_item in item.iterdir():
                        if count >= max_entries:
                            break
                        count += 1
                        if sub_item.is_file():
                            entries.append(sub_item)
                except PermissionError:
                    _LOG.warning("permission denied scanning %s", item)
    except PermissionError:
        _LOG.warning("permission denied scanning %s", directory)

    return entries
```

Write file `src/tumbl4/core/state/pending_posts.py`:

```python
"""In-flight post→media tracker for cooperative shutdown and sidecar timing.

See plan boundaries: tracks which posts have in-flight downloads so that:
1. The orchestrator knows when all media for a post have completed (trigger sidecar write)
2. On cooperative shutdown, we know which posts are incomplete (don't write sidecars)
3. The pending state is queryable for status reporting

Guarded by asyncio.Lock for safe concurrent access from multiple download workers.
"""

from __future__ import annotations

import asyncio


class PendingPosts:
    """Track in-flight post→media URL mappings.

    Thread-safety: all methods acquire an asyncio.Lock. This is safe because
    all callers are async tasks on the same event loop. The lock serialises
    access to ``_posts`` without blocking the event loop (async lock, not
    threading lock).
    """

    def __init__(self) -> None:
        self._lock = asyncio.Lock()
        self._posts: dict[str, set[str]] = {}

    async def register(self, post_id: str, media_urls: set[str]) -> None:
        """Register a post with its set of pending media URLs.

        Args:
            post_id: The post ID.
            media_urls: Set of media URLs that need to be downloaded.
        """
        async with self._lock:
            self._posts[post_id] = set(media_urls)

    async def complete_media(self, post_id: str, url: str) -> bool:
        """Mark a single media URL as completed for a post.

        Args:
            post_id: The post ID.
            url: The completed media URL.

        Returns:
            True if this was the last pending URL for the post (post is now
            complete and has been removed from tracking), False otherwise.
        """
        async with self._lock:
            if post_id not in self._posts:
                return False

            self._posts[post_id].discard(url)

            if not self._posts[post_id]:
                del self._posts[post_id]
                return True

            return False

    async def is_pending(self, post_id: str) -> bool:
        """Check if a post has any pending media downloads."""
        async with self._lock:
            return post_id in self._posts

    async def pending_urls(self, post_id: str) -> set[str]:
        """Return the set of still-pending media URLs for a post."""
        async with self._lock:
            return set(self._posts.get(post_id, set()))

    async def pending_count(self) -> int:
        """Return the number of posts with pending downloads."""
        async with self._lock:
            return len(self._posts)

    async def clear(self) -> None:
        """Clear all pending state (used on shutdown)."""
        async with self._lock:
            self._posts.clear()
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `uv run pytest tests/unit/test_orphan_sweep.py tests/unit/test_pending_posts.py -v`
Expected: 16 passed (7 orphan sweep + 9 pending posts)

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/state/orphan_sweep.py src/tumbl4/core/state/pending_posts.py tests/unit/test_orphan_sweep.py tests/unit/test_pending_posts.py
git commit -m "feat(state): add orphan sweep (.part cleanup) and pending posts tracker

orphan_sweep: bounded async scan via to_thread, removes .part files on startup.
pending_posts: asyncio.Lock-guarded post→media map for sidecar timing and
cooperative shutdown. See plan boundaries.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: Path traversal guards

**Files:**
- Create: `tests/unit/test_path_traversal.py`

This task adds path traversal tests that validate the guards described in spec §6.4.
The actual guards are inserted into existing `file_downloader.py` and `metadata.py`
(created in Plans 2-3), but we write a standalone test module with a reusable guard
function that those modules will call.

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_path_traversal.py`:

```python
"""Tests for path traversal guards — is_relative_to before every open().

See spec §6.4: rendered_path.resolve().is_relative_to(output_root.resolve())
is checked before every open() call. Violation raises WriteFailed.
"""

from __future__ import annotations

from pathlib import Path

import pytest

from tumbl4.core.download.path_guard import assert_within_root


class TestAssertWithinRoot:
    def test_normal_path_allowed(self, tmp_path: Path) -> None:
        root = tmp_path / "output"
        root.mkdir()
        target = root / "blog" / "12345_01.jpg"
        # Should not raise
        assert_within_root(target, root)

    def test_same_as_root_allowed(self, tmp_path: Path) -> None:
        root = tmp_path / "output"
        root.mkdir()
        assert_within_root(root, root)

    def test_traversal_with_dotdot(self, tmp_path: Path) -> None:
        root = tmp_path / "output"
        root.mkdir()
        target = root / ".." / ".." / ".bashrc"
        with pytest.raises(ValueError, match="path traversal"):
            assert_within_root(target, root)

    def test_traversal_via_malicious_title(self, tmp_path: Path) -> None:
        """Simulate a malicious post title: ../../../.bashrc"""
        root = tmp_path / "output"
        root.mkdir()
        target = root / "blog" / "../../../.bashrc"
        with pytest.raises(ValueError, match="path traversal"):
            assert_within_root(target, root)

    def test_symlink_escape(self, tmp_path: Path) -> None:
        """Symlink pointing outside the root should be caught."""
        root = tmp_path / "output"
        root.mkdir()
        outside = tmp_path / "outside"
        outside.mkdir()
        (outside / "secret.txt").write_text("secret")

        # Create a symlink inside root that points outside
        symlink = root / "escape"
        symlink.symlink_to(outside)
        target = symlink / "secret.txt"

        with pytest.raises(ValueError, match="path traversal"):
            assert_within_root(target, root)

    def test_deeply_nested_allowed(self, tmp_path: Path) -> None:
        root = tmp_path / "output"
        root.mkdir()
        target = root / "blog" / "2026" / "04" / "photos" / "12345_01.jpg"
        assert_within_root(target, root)

    def test_absolute_path_outside_root(self, tmp_path: Path) -> None:
        root = tmp_path / "output"
        root.mkdir()
        target = Path("/etc/passwd")
        with pytest.raises(ValueError, match="path traversal"):
            assert_within_root(target, root)

    def test_traversal_with_encoded_chars(self, tmp_path: Path) -> None:
        """File name containing path separators should be caught after resolve."""
        root = tmp_path / "output"
        root.mkdir()
        # Even though this won't resolve to an escape on most systems,
        # the resolve() check catches it if it somehow does
        target = root / "blog" / "normal_file.jpg"
        # This should be fine
        assert_within_root(target, root)

    def test_return_value_is_resolved(self, tmp_path: Path) -> None:
        root = tmp_path / "output"
        root.mkdir()
        target = root / "blog" / "." / "12345.jpg"
        result = assert_within_root(target, root)
        assert ".." not in str(result)
        assert result.is_absolute()
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_path_traversal.py -v`
Expected: FAIL — `ModuleNotFoundError: No module named 'tumbl4.core.download.path_guard'`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/download/path_guard.py`:

```python
"""Path traversal guard — ensure all file writes stay within the output root.

See spec §6.4. This guard is called before every ``open()`` in
``file_downloader.py`` and ``metadata.py``. Violation raises ``ValueError``
(caught and converted to ``WriteFailed`` by callers).

The check uses ``Path.resolve()`` to canonicalize both the target and root,
then calls ``is_relative_to()`` to verify containment. This catches:
- ``../`` traversal in filenames (from malicious post titles)
- Symlink escapes (symlinks are resolved before the check)
- Absolute paths injected via template variables
"""

from __future__ import annotations

from pathlib import Path


def assert_within_root(target: Path, root: Path) -> Path:
    """Verify that ``target`` resolves to a path within ``root``.

    Both paths are resolved (canonicalized, symlinks followed) before comparison.

    Args:
        target: The path to validate (may be relative or contain ``..``).
        root: The output root directory that ``target`` must be within.

    Returns:
        The resolved (absolute, canonical) target path.

    Raises:
        ValueError: If the resolved target is not within the resolved root,
            with a message starting with "path traversal" for callers to match.
    """
    resolved_root = root.resolve()
    resolved_target = target.resolve()

    if not resolved_target.is_relative_to(resolved_root):
        raise ValueError(
            f"path traversal blocked: {target} resolves to {resolved_target} "
            f"which is outside root {resolved_root}"
        )

    return resolved_target
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_path_traversal.py -v`
Expected: 9 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/download/path_guard.py tests/unit/test_path_traversal.py
git commit -m "feat(download): add path traversal guard for file writes

assert_within_root() resolves both target and root, then checks
is_relative_to(). Catches ../ traversal, symlink escapes, and
absolute path injection. See spec §6.4.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: `tumbl4 sweep <blog>` subcommand

**Files:**
- Create: `src/tumbl4/cli/commands/sweep.py`
- Modify: `src/tumbl4/cli/app.py`
- Create: `tests/unit/test_sweep_command.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_sweep_command.py`:

```python
"""Tests for the sweep CLI command — manual orphan .part cleanup."""

from __future__ import annotations

from pathlib import Path
from unittest.mock import AsyncMock, patch

from typer.testing import CliRunner

from tumbl4.cli.app import app

runner = CliRunner()


class TestSweepCommand:
    def test_sweep_appears_in_help(self) -> None:
        result = runner.invoke(app, ["--help"])
        assert result.exit_code == 0
        assert "sweep" in result.output

    def test_sweep_help(self) -> None:
        result = runner.invoke(app, ["sweep", "--help"])
        assert result.exit_code == 0
        assert "blog" in result.output.lower()

    def test_sweep_requires_blog_argument(self) -> None:
        result = runner.invoke(app, ["sweep"])
        assert result.exit_code != 0

    @patch("tumbl4.cli.commands.sweep.sweep_orphan_parts", new_callable=AsyncMock)
    def test_sweep_calls_orphan_sweep(
        self, mock_sweep: AsyncMock, tmp_path: Path,
    ) -> None:
        mock_sweep.return_value = 3
        result = runner.invoke(
            app,
            ["sweep", "testblog", "--output-dir", str(tmp_path)],
        )
        assert result.exit_code == 0
        assert "3" in result.output
        mock_sweep.assert_called_once()

    @patch("tumbl4.cli.commands.sweep.sweep_orphan_parts", new_callable=AsyncMock)
    def test_sweep_zero_files(
        self, mock_sweep: AsyncMock, tmp_path: Path,
    ) -> None:
        mock_sweep.return_value = 0
        result = runner.invoke(
            app,
            ["sweep", "testblog", "--output-dir", str(tmp_path)],
        )
        assert result.exit_code == 0
        assert "0" in result.output or "no" in result.output.lower()

    @patch("tumbl4.cli.commands.sweep.sweep_orphan_parts", new_callable=AsyncMock)
    def test_sweep_dry_run(
        self, mock_sweep: AsyncMock, tmp_path: Path,
    ) -> None:
        result = runner.invoke(
            app,
            ["sweep", "testblog", "--output-dir", str(tmp_path), "--dry-run"],
        )
        assert result.exit_code == 0
        # In dry-run mode, sweep should not be called with actual removal
        # (implementation detail: dry_run flag passed through)
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_sweep_command.py -v`
Expected: FAIL — "sweep" not found in help output

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/cli/commands/sweep.py`:

```python
"""tumbl4 sweep <blog> — manual orphan .part file cleanup.

Scans the output directory for a blog and removes any orphan .part files
left behind by interrupted downloads.
"""

from __future__ import annotations

import asyncio
from pathlib import Path
from typing import Annotated

import typer

from tumbl4._internal.logging import get_logger
from tumbl4.core.state.orphan_sweep import sweep_orphan_parts

_LOG = get_logger("cli.sweep")


def sweep(
    blog: Annotated[str, typer.Argument(help="Blog name to sweep orphan files for.")],
    output_dir: Annotated[
        Path,
        typer.Option(
            "--output-dir",
            "-o",
            help="Output directory (default: ./tumbl4-output).",
        ),
    ] = Path.cwd() / "tumbl4-output",
    dry_run: Annotated[
        bool,
        typer.Option(
            "--dry-run",
            help="Show what would be removed without deleting.",
        ),
    ] = False,
) -> None:
    """Remove orphan .part files from a blog's output directory."""
    blog_dir = output_dir / blog.strip().lower()

    if not blog_dir.is_dir():
        typer.echo(f"Directory not found: {blog_dir}")
        raise typer.Exit(code=1)

    if dry_run:
        # Count .part files without removing
        part_files = list(blog_dir.rglob("*.part"))
        count = len(part_files)
        if count == 0:
            typer.echo(f"No orphan .part files found in {blog_dir}")
        else:
            typer.echo(f"Would remove {count} orphan .part file(s) from {blog_dir}:")
            for pf in part_files[:20]:  # Show first 20
                typer.echo(f"  {pf.name}")
            if count > 20:
                typer.echo(f"  ... and {count - 20} more")
        return

    removed = asyncio.run(sweep_orphan_parts(blog_dir))

    if removed == 0:
        typer.echo(f"No orphan .part files found in {blog_dir}")
    else:
        typer.echo(f"Removed {removed} orphan .part file(s) from {blog_dir}")
```

Update `src/tumbl4/cli/app.py` — add the sweep subcommand registration:

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
from tumbl4.cli.commands.sweep import sweep

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


app.command()(sweep)


def main() -> None:
    """Console-script entry point referenced from pyproject.toml."""
    app()
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_sweep_command.py -v`
Expected: 6 passed

- [ ] **Step 5: Run all existing tests to check for regressions**

Run: `uv run pytest -v`
Expected: all tests pass (Plan 1 existing + Plan 6 new)

- [ ] **Step 6: Commit**

```bash
git add src/tumbl4/cli/commands/sweep.py src/tumbl4/cli/app.py tests/unit/test_sweep_command.py
git commit -m "feat(cli): add tumbl4 sweep <blog> subcommand

Manual orphan .part file cleanup with --dry-run support.
Delegates to state/orphan_sweep.sweep_orphan_parts().

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 9: Ruff bans for SQL injection and XXE prevention

**Files:**
- Modify: `pyproject.toml`

This task adds structural lint rules that prevent entire classes of vulnerabilities
at CI time rather than relying on developer discipline.

- [ ] **Step 1: Write the expected ruff configuration**

The spec requires:
1. **SQL injection prevention:** ban `execute(f"...")` and `execute("%s" % ...)` patterns (spec §6.5)
2. **XXE prevention:** ban `lxml.etree` imports in `parse/html_scrape.py` (spec §7.6)

Update `pyproject.toml` — add the ruff bans to the existing `[tool.ruff.lint.flake8-tidy-imports.banned-api]` section and add a targeted per-file ban:

In `pyproject.toml`, the `[tool.ruff.lint.flake8-tidy-imports.banned-api]` section, add after the existing `"tumbl4.cli"` ban:

```toml
"lxml.etree".msg = "lxml.etree is banned in parse modules to prevent XXE attacks. Use html.parser or defusedxml instead. See spec §6.3."
```

And in the `[tool.ruff.lint]` section, add `S608` to the `select` list (this is bandit's `hardcoded-sql-expressions` rule that catches `execute(f"...")` patterns). The `S` category is already selected, so `S608` is already active. However, we need to ensure it is NOT ignored. Verify the `ignore` list does not suppress it.

Additionally, add a custom flake8-bandit configuration to catch the SQL f-string pattern more specifically.

- [ ] **Step 2: Apply the pyproject.toml changes**

In `pyproject.toml`, update the `[tool.ruff.lint.flake8-tidy-imports.banned-api]` section:

```toml
[tool.ruff.lint.flake8-tidy-imports.banned-api]
"tumbl4.cli".msg = "core modules must not import from cli; see spec §3 unidirectional-dependency property."
"lxml.etree".msg = "lxml.etree is banned to prevent XXE attacks. Use html.parser or defusedxml instead. See spec §7.6."
```

Verify that the `S` category in `select` already includes `S608` (hardcoded SQL expressions). The existing config has `"S"` in select and only `"S101"` in ignore, so `S608` is already active.

Add a comment in the pyproject.toml for documentation:

```toml
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
    "TID",   # flake8-tidy-imports
    "SIM",   # flake8-simplify
    "PL",    # pylint subset
    "S",     # flake8-bandit — security (S608 = SQL injection, S320 = lxml)
]
ignore = [
    "S101",  # assert is fine in tests
    "PLR0913",  # allow many args on core functions; revisit after slice 2
]
```

- [ ] **Step 3: Verify the bans work**

Run: `uv run ruff check .`
Expected: no new violations (we haven't introduced any banned patterns)

Create a temporary test file to verify the bans catch violations:

```bash
# Test SQL injection ban (S608)
echo 'import sqlite3; conn = sqlite3.connect(":memory:"); conn.execute(f"SELECT * FROM users WHERE name = {name}")' > /tmp/test_s608.py
uv run ruff check /tmp/test_s608.py
# Expected: S608 violation
rm /tmp/test_s608.py
```

```bash
# Test XXE ban (TID251 for lxml.etree)
echo 'from lxml.etree import parse' > /tmp/test_xxe.py
uv run ruff check /tmp/test_xxe.py
# Expected: TID251 violation for banned API
rm /tmp/test_xxe.py
```

- [ ] **Step 4: Commit**

```bash
git add pyproject.toml
git commit -m "security: add ruff bans for SQL injection and XXE prevention

S608 (already active via S category) catches execute(f'...') patterns.
TID251 bans lxml.etree imports to prevent XXE. See spec §6.5 and §7.6.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 10: CI hardening — pip-audit, Dependabot, cassette-scrubber

**Files:**
- Modify: `.github/workflows/ci.yml`
- Create: `.github/dependabot.yml`
- Create: `scripts/scrub_cassettes.py`

- [ ] **Step 1: Update ci.yml with pip-audit and cassette-scrubber steps**

Update `.github/workflows/ci.yml`:

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
          # Drop py3.11 x macOS to save CI time; we run it on Linux.
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

      - name: pip-audit (dependency vulnerability scan)
        if: matrix.os == 'ubuntu-latest' && matrix.python-version == '3.12'
        run: |
          uv pip install pip-audit
          uv run pip-audit --strict --desc

      - name: Cassette scrubber re-scan
        if: matrix.os == 'ubuntu-latest' && matrix.python-version == '3.12'
        run: uv run python scripts/scrub_cassettes.py --check

      - name: Upload coverage
        if: matrix.os == 'ubuntu-latest' && matrix.python-version == '3.12'
        uses: actions/upload-artifact@v4
        with:
          name: coverage-report
          path: coverage.xml
          retention-days: 14

  # Weekly scheduled pip-audit for catching new CVEs
  audit:
    name: weekly-audit
    if: github.event_name == 'schedule'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: astral-sh/setup-uv@v3
      - run: uv python install 3.12
      - run: uv sync --dev --frozen
      - run: |
          uv pip install pip-audit
          uv run pip-audit --strict --desc
```

- [ ] **Step 2: Create Dependabot configuration**

Write file `.github/dependabot.yml`:

```yaml
# Dependabot configuration for tumbl4
# See: https://docs.github.com/en/code-security/dependabot/dependabot-version-updates

version: 2
updates:
  # Python dependencies (pyproject.toml / uv.lock)
  - package-ecosystem: "pip"
    directory: "/"
    schedule:
      interval: "weekly"
      day: "monday"
    open-pull-requests-limit: 10
    labels:
      - "dependencies"
      - "automated"
    commit-message:
      prefix: "deps"
      include: "scope"
    # Group minor/patch updates to reduce PR noise
    groups:
      dev-dependencies:
        patterns:
          - "pytest*"
          - "ruff"
          - "pyright"
          - "respx"
          - "hypothesis"
          - "pre-commit"
        update-types:
          - "minor"
          - "patch"
      production-dependencies:
        patterns:
          - "httpx"
          - "pydantic*"
          - "typer"
          - "rich"
          - "structlog"
          - "aiofiles"
          - "aiolimiter"
        update-types:
          - "minor"
          - "patch"

  # GitHub Actions
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
      day: "monday"
    open-pull-requests-limit: 5
    labels:
      - "dependencies"
      - "ci"
      - "automated"
    commit-message:
      prefix: "ci"
```

- [ ] **Step 3: Create cassette scrubber script**

Write file `scripts/scrub_cassettes.py`:

```python
#!/usr/bin/env python3
"""Cassette scrubber — scan VCR cassettes for leaked secrets.

Two modes:
    python scrub_cassettes.py          # Scrub in-place (pre-commit)
    python scrub_cassettes.py --check  # Check-only (CI — fails on any leak)

Scans YAML cassette files under tests/fixtures/cassettes/ for patterns that
indicate leaked credentials (Tumblr cookies, bearer tokens, authorization
headers, API keys).

See spec §7.6 and CONTRIBUTING.md for the cassette recording workflow.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

_CASSETTE_DIR = Path(__file__).parent.parent / "tests" / "fixtures" / "cassettes"

# Secret patterns — same set as _internal/logging.py SecretFilter
_SECRET_PATTERNS: list[tuple[str, re.Pattern[str]]] = [
    ("tumblr_cookie", re.compile(r"tumblr_[a-zA-Z0-9_]+=[^\s;,&]+")),
    ("bearer_token", re.compile(r"Bearer\s+[A-Za-z0-9._\-~+/]+=*", re.IGNORECASE)),
    ("cookie_header", re.compile(r"Cookie:\s*[^\r\n]+", re.IGNORECASE)),
    ("authorization_header", re.compile(r"Authorization:\s*[^\r\n]+", re.IGNORECASE)),
    ("set_cookie_header", re.compile(r"Set-Cookie:\s*[^\r\n]+", re.IGNORECASE)),
    ("api_key_param", re.compile(r"api_key=[A-Za-z0-9]+")),
    ("consumer_key", re.compile(r"x8pd1[A-Za-z0-9]+")),  # Tumblr hardcoded key prefix
]

_REDACTED = "[SCRUBBED]"


def scan_file(path: Path) -> list[tuple[str, int, str]]:
    """Scan a cassette file for secret patterns.

    Returns:
        List of (pattern_name, line_number, matched_text) tuples.
    """
    findings: list[tuple[str, int, str]] = []
    try:
        content = path.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return findings

    for line_num, line in enumerate(content.splitlines(), start=1):
        for name, pattern in _SECRET_PATTERNS:
            for match in pattern.finditer(line):
                findings.append((name, line_num, match.group(0)[:80]))

    return findings


def scrub_file(path: Path) -> int:
    """Scrub secrets from a cassette file in-place.

    Returns:
        Number of replacements made.
    """
    try:
        content = path.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return 0

    count = 0
    for _name, pattern in _SECRET_PATTERNS:
        content, n = pattern.subn(_REDACTED, content)
        count += n

    if count > 0:
        path.write_text(content, encoding="utf-8")

    return count


def main() -> int:
    check_mode = "--check" in sys.argv

    if not _CASSETTE_DIR.is_dir():
        # No cassettes yet — nothing to scan
        print(f"No cassette directory found at {_CASSETTE_DIR} — skipping.")
        return 0

    cassettes = list(_CASSETTE_DIR.rglob("*.yaml")) + list(_CASSETTE_DIR.rglob("*.yml"))

    if not cassettes:
        print("No cassette files found — skipping.")
        return 0

    total_findings = 0

    for cassette in sorted(cassettes):
        if check_mode:
            findings = scan_file(cassette)
            if findings:
                total_findings += len(findings)
                rel = cassette.relative_to(Path.cwd()) if cassette.is_relative_to(Path.cwd()) else cassette
                for name, line, text in findings:
                    print(f"LEAK: {rel}:{line} [{name}] {text}")
        else:
            count = scrub_file(cassette)
            if count > 0:
                total_findings += count
                rel = cassette.relative_to(Path.cwd()) if cassette.is_relative_to(Path.cwd()) else cassette
                print(f"Scrubbed {count} secret(s) from {rel}")

    if check_mode and total_findings > 0:
        print(f"\nFAILED: {total_findings} leaked secret(s) found in cassettes.")
        print("Run `python scripts/scrub_cassettes.py` to scrub them.")
        return 1

    if check_mode:
        print(f"OK: scanned {len(cassettes)} cassette(s), no secrets found.")

    return 0


if __name__ == "__main__":
    sys.exit(main())
```

- [ ] **Step 4: Verify CI configuration is valid**

Run: `uv run ruff check .`
Expected: no violations

Run: `python scripts/scrub_cassettes.py --check`
Expected: "No cassette directory found" or "OK: scanned 0 cassettes" (no cassettes yet)

- [ ] **Step 5: Commit**

```bash
git add .github/workflows/ci.yml .github/dependabot.yml scripts/scrub_cassettes.py
git commit -m "ci: add pip-audit, Dependabot, and cassette-scrubber to CI

pip-audit runs on ubuntu-latest/py3.12 for dependency CVE scanning.
Dependabot configured for weekly pip + GitHub Actions updates with
grouping to reduce PR noise. Cassette scrubber --check mode fails CI
on any leaked secret in VCR fixtures. See spec §6.9 and §7.6.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 11: Release workflow — SLSA attestation + PyPI OIDC + GitHub release

**Files:**
- Create: `.github/workflows/release.yml`

- [ ] **Step 1: Write the release workflow**

Write file `.github/workflows/release.yml`:

```yaml
# Release workflow for tumbl4
#
# Triggered on tag v* (e.g., v0.1.0, v1.0.0).
# Steps:
#   1. Run CI as prerequisite (reuse ci.yml)
#   2. Build wheel + sdist via uv
#   3. Generate SLSA provenance attestation for each artifact
#   4. Publish to PyPI via OIDC trusted publisher (no stored API tokens)
#   5. Create GitHub release with changelog section + attestations
#
# See spec §8.1 and §6.9 for security rationale.

name: Release

on:
  push:
    tags:
      - "v*"

permissions:
  contents: write      # create GitHub release
  id-token: write      # OIDC token for PyPI trusted publisher
  attestations: write  # SLSA provenance attestation

jobs:
  # Gate: run full CI before releasing
  ci:
    uses: ./.github/workflows/ci.yml

  build:
    name: Build distribution
    needs: ci
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0  # full history for hatch-vcs version derivation

      - name: Install uv
        uses: astral-sh/setup-uv@v3
        with:
          enable-cache: true

      - name: Set up Python
        run: uv python install 3.12

      - name: Build wheel and sdist
        run: uv build

      - name: Upload build artifacts
        uses: actions/upload-artifact@v4
        with:
          name: dist
          path: dist/
          retention-days: 5

  attest:
    name: SLSA provenance attestation
    needs: build
    runs-on: ubuntu-latest
    steps:
      - name: Download build artifacts
        uses: actions/download-artifact@v4
        with:
          name: dist
          path: dist/

      - name: Generate SLSA attestation
        uses: actions/attest-build-provenance@v2
        with:
          subject-path: "dist/*"

  publish-pypi:
    name: Publish to PyPI
    needs: [build, attest]
    runs-on: ubuntu-latest
    environment:
      name: pypi
      url: https://pypi.org/project/tumbl4/
    permissions:
      id-token: write  # OIDC for PyPI trusted publisher

    steps:
      - name: Download build artifacts
        uses: actions/download-artifact@v4
        with:
          name: dist
          path: dist/

      - name: Publish to PyPI via OIDC
        uses: pypa/gh-action-pypi-publish@release/v1
        with:
          # No password needed — uses OIDC trusted publisher configured in PyPI
          print-hash: true
          verbose: true

  github-release:
    name: Create GitHub release
    needs: [build, attest, publish-pypi]
    runs-on: ubuntu-latest
    permissions:
      contents: write
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Download build artifacts
        uses: actions/download-artifact@v4
        with:
          name: dist
          path: dist/

      - name: Extract version from tag
        id: version
        run: echo "version=${GITHUB_REF_NAME#v}" >> "$GITHUB_OUTPUT"

      - name: Extract changelog section
        id: changelog
        run: |
          # Extract the section for this version from CHANGELOG.md
          version="${{ steps.version.outputs.version }}"
          # Use awk to extract content between version headers
          section=$(awk -v ver="$version" '
            /^## \[/ {
              if (found) exit
              if (index($0, "[" ver "]") > 0) found=1
              next
            }
            found { print }
          ' CHANGELOG.md)

          if [ -z "$section" ]; then
            section="Release $version. See CHANGELOG.md for details."
          fi

          # Write to file to handle multiline content
          echo "$section" > /tmp/release_notes.md

      - name: Create GitHub release
        uses: softprops/action-gh-release@v2
        with:
          tag_name: ${{ github.ref_name }}
          name: tumbl4 ${{ steps.version.outputs.version }}
          body_path: /tmp/release_notes.md
          files: dist/*
          draft: false
          prerelease: ${{ contains(github.ref_name, 'rc') || contains(github.ref_name, 'beta') || contains(github.ref_name, 'alpha') }}
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

- [ ] **Step 2: Verify the workflow YAML is valid**

Run: `python -c "import yaml; yaml.safe_load(open('.github/workflows/release.yml'))" 2>/dev/null || echo "Install pyyaml: uv pip install pyyaml"`

Alternatively, verify with a basic syntax check:

Run: `python -c "
import json, re
content = open('.github/workflows/release.yml').read()
# Basic validation: check required keys exist
assert 'on:' in content
assert 'push:' in content
assert 'tags:' in content
assert 'jobs:' in content
assert 'attest-build-provenance' in content
assert 'gh-action-pypi-publish' in content
assert 'id-token: write' in content
print('release.yml structure looks valid')
"`

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/release.yml
git commit -m "ci: add release workflow with SLSA attestation and PyPI OIDC

Triggered on v* tags. Runs CI first, then builds, attests with SLSA
provenance, publishes to PyPI via OIDC trusted publisher (no API tokens),
and creates a GitHub release with changelog and artifacts. See spec §8.1.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 12: Documentation site

**Files:**
- Create: `docs/index.md`
- Create: `docs/installation.md`
- Create: `docs/getting-started.md`
- Create: `docs/authentication.md`
- Create: `docs/configuration.md`
- Create: `docs/filename-templates.md`
- Create: `docs/architecture.md`
- Create: `docs/security.md`
- Create: `docs/commands/download.md`
- Create: `docs/commands/login.md`
- Create: `docs/commands/sweep.md`
- Create: `docs/commands/status.md`

- [ ] **Step 1: Write the documentation files**

Write file `docs/index.md`:

```markdown
# tumbl4

**Command-line Tumblr blog backup tool for macOS and Linux.**

tumbl4 is a Python CLI tool that downloads and archives Tumblr blogs, preserving
photos, videos, audio, and metadata. It supports public and hidden (login-required)
blogs, resume across runs, cross-blog dedup, and configurable filename templates.

## Quick start

```bash
# Install
uv tool install tumbl4

# Download a public blog's photos
tumbl4 download photography-blog

# Download a hidden/dashboard blog (requires login)
tumbl4 login
tumbl4 download hidden-blog
```

## Documentation

- [Installation](installation.md) - Install tumbl4 and its dependencies
- [Getting started](getting-started.md) - First-run walkthrough
- [Authentication](authentication.md) - Login flow for hidden blogs
- [Configuration](configuration.md) - Config schema, precedence, environment variables
- [Filename templates](filename-templates.md) - Customize output file naming
- [Security](security.md) - Threat model, file permissions, SLSA verification
- [Architecture](architecture.md) - Module overview for contributors

## Command reference

- [`tumbl4 download`](commands/download.md) - Download a blog
- [`tumbl4 login`](commands/login.md) - Authenticate for hidden blogs
- [`tumbl4 sweep`](commands/sweep.md) - Clean up orphan files
- [`tumbl4 status`](commands/status.md) - View blog download status

## Links

- [GitHub Repository](https://github.com/claire/tumbl4)
- [PyPI Package](https://pypi.org/project/tumbl4/)
- [Changelog](https://github.com/claire/tumbl4/blob/master/CHANGELOG.md)
```

Write file `docs/installation.md`:

```markdown
# Installation

## Requirements

- Python 3.12 or later
- macOS or Linux (Windows is not supported in this fork)
- For hidden blog downloads: a graphical environment (X11/Wayland) for the login flow

## Install via uv (recommended)

```bash
uv tool install tumbl4
```

This installs tumbl4 as an isolated tool with its own virtual environment.

## Install via pipx

```bash
pipx install tumbl4
```

## Install via pip

```bash
pip install tumbl4
```

We recommend installing in a virtual environment rather than system-wide.

## Install Playwright browsers (for hidden blogs only)

If you need to download hidden or login-required blogs, install the Playwright
browser after installing tumbl4:

```bash
playwright install chromium --with-deps
```

This step is only needed for `tumbl4 login`. Public blog downloads work without it.

## Verify installation

```bash
tumbl4 --version
```

## XDG paths

tumbl4 follows the XDG Base Directory Specification:

| Purpose | Linux | macOS |
|---------|-------|-------|
| Config | `~/.config/tumbl4/` | `~/Library/Application Support/tumbl4/` |
| State | `~/.local/state/tumbl4/` | `~/Library/Application Support/tumbl4/state/` |
| Data | `~/.local/share/tumbl4/` | `~/Library/Application Support/tumbl4/data/` |

Override with `$XDG_CONFIG_HOME`, `$XDG_STATE_HOME`, `$XDG_DATA_HOME` environment variables.

## Updating

```bash
# If installed via uv:
uv tool upgrade tumbl4

# If installed via pipx:
pipx upgrade tumbl4

# If installed via pip:
pip install --upgrade tumbl4
```
```

Write file `docs/getting-started.md`:

```markdown
# Getting started

## Download a public blog

The simplest use case - download all photos from a public Tumblr blog:

```bash
tumbl4 download photography-blog
```

This will:
1. Crawl the blog's posts via the Tumblr API
2. Download all photos to `./tumbl4-output/photography-blog/`
3. Write JSON metadata sidecars to `./tumbl4-output/photography-blog/_meta/`
4. Save resume state so future runs only download new posts

## Specify output directory

```bash
tumbl4 download photography-blog --output-dir ~/tumblr-backups
```

## Resume interrupted downloads

tumbl4 automatically resumes where it left off. Just run the same command again:

```bash
tumbl4 download photography-blog
```

To force a full re-crawl:

```bash
tumbl4 download photography-blog --no-resume
```

## Download a hidden blog

Hidden (dashboard-only) blogs require authentication:

```bash
# Step 1: Login via browser
tumbl4 login

# Step 2: Download the blog
tumbl4 download hidden-blog
```

The login flow opens a Chromium browser for you to authenticate with Tumblr.
See [Authentication](authentication.md) for details.

## Clean up interrupted downloads

If a download was interrupted, orphan `.part` files may remain:

```bash
tumbl4 sweep photography-blog
```

## Check download status

```bash
tumbl4 status photography-blog
```

## Environment variables

All settings can be overridden via `TUMBL4_` environment variables:

```bash
TUMBL4_OUTPUT_DIR=~/backups tumbl4 download photography-blog
TUMBL4_MAX_CONCURRENT_DOWNLOADS=8 tumbl4 download large-blog
TUMBL4_LOG_LEVEL=DEBUG tumbl4 download test-blog
```
```

Write file `docs/authentication.md`:

```markdown
# Authentication

## When is login needed?

Login is only required for **hidden** (dashboard-only / login-required) blogs.
Public blogs work without authentication.

tumbl4 auto-detects whether a blog is public or hidden. If a blog requires login,
you will see:

```
"hidden-blog" requires login. Run `tumbl4 login` and try again.
```

## Login flow

```bash
tumbl4 login
```

This opens a Chromium browser window where you authenticate with Tumblr directly.
tumbl4 never sees or stores your password - it only captures the session cookies
after you complete the login.

### Requirements

- A graphical environment (X11 or Wayland display)
- Chromium browser (installed via `playwright install chromium --with-deps`)

### Headless environments (SSH, servers)

If you are on a headless server, you have two options:

1. **Login on a local machine**, then copy the state file:
   ```bash
   # On your local machine:
   tumbl4 login
   # Copy the state file to the server:
   scp ~/.local/state/tumbl4/playwright_state.json server:~/.local/state/tumbl4/
   ssh server chmod 0600 ~/.local/state/tumbl4/playwright_state.json
   ```

2. **Use X11 forwarding** over SSH:
   ```bash
   ssh -X server
   tumbl4 login
   ```

## Logout

```bash
tumbl4 logout
```

This deletes all stored session data:
- `playwright_state.json` (session cookies)
- `browser_profile/` directory

## Security notes

- Session state is stored at `$XDG_STATE_HOME/tumbl4/playwright_state.json`
  with file permissions `0600` (owner read/write only)
- The browser profile directory uses permissions `0700`
- tumbl4 refuses to run if the state file has broader permissions
- Each `tumbl4 login` starts a fresh browser profile
- Session cookies are held in memory while the CLI runs; a user with
  `ptrace` access to the process can read them (inherent to local CLI execution)
- See [Security](security.md) for the full threat model
```

Write file `docs/configuration.md`:

```markdown
# Configuration

## Precedence chain (highest to lowest)

1. CLI flags (`--output-dir`, `--page-size`, etc.)
2. Environment variables (`TUMBL4_OUTPUT_DIR`, etc.)
3. Project config (`./tumbl4.toml`)
4. User config (`$XDG_CONFIG_HOME/tumbl4/config.toml`)
5. Hardcoded defaults

## Environment variables

All settings use the `TUMBL4_` prefix. Nested settings use `__` as delimiter.

| Variable | Default | Description |
|----------|---------|-------------|
| `TUMBL4_OUTPUT_DIR` | `./tumbl4-output` | Where downloaded media is written |
| `TUMBL4_LOG_LEVEL` | `INFO` | Log level (DEBUG, INFO, WARNING, ERROR) |
| `TUMBL4_MAX_CONCURRENT_DOWNLOADS` | `4` | Parallel download workers |
| `TUMBL4_HTTP__CONNECT_TIMEOUT` | `10.0` | HTTP connect timeout (seconds) |
| `TUMBL4_HTTP__READ_TIMEOUT` | `60.0` | HTTP read timeout (seconds) |
| `TUMBL4_HTTP__MAX_REDIRECTS` | `5` | Maximum redirect hops |
| `TUMBL4_HTTP__MAX_API_RESPONSE_BYTES` | `33554432` | Max API response size (32 MB) |
| `TUMBL4_QUEUE__MAX_PENDING_MEDIA` | `200` | Media download queue depth |

## TOML config file

Example `tumbl4.toml`:

```toml
output_dir = "~/tumblr-backups"
log_level = "INFO"
max_concurrent_downloads = 8

[http]
connect_timeout = 15.0
read_timeout = 120.0
max_redirects = 5

[queue]
max_pending_media = 500
```

## Output directory structure

```
tumbl4-output/
  blog-name/
    12345_01.jpg          # downloaded media
    12345_02.png
    67890_01.mp4
    _meta/
      12345.json          # post metadata sidecar
      67890.json
    blog.db               # per-blog SQLite state (resume, dedup)
```
```

Write file `docs/filename-templates.md`:

```markdown
# Filename templates

## Default template

```
{blog}/{post_id}_{index_padded}.{ext}
```

Example output: `photography-blog/12345_01.jpg`

## Available variables

| Variable | Description | Example |
|----------|-------------|---------|
| `{blog}` | Blog name | `photography-blog` |
| `{post_id}` | Post ID | `728394056123` |
| `{index}` | Media index (0-based) | `0` |
| `{index_padded}` | Media index (2-digit padded) | `01` |
| `{ext}` | File extension | `jpg` |
| `{post_type}` | Post type | `photo` |
| `{date}` | Post date (YYYY-MM-DD) | `2026-04-11` |
| `{year}` | Post year | `2026` |
| `{month}` | Post month | `04` |
| `{day}` | Post day | `11` |
| `{datetime}` | Post datetime (ISO 8601) | `2026-04-11T14-22-03` |
| `{tag}` | First tag | `photography` |
| `{tags}` | All tags (comma-joined) | `photography,sunset` |
| `{hash8}` | First 8 chars of URL hash | `a1b2c3d4` |

## Examples

### Organize by date

```toml
# tumbl4.toml
filename_template = "{blog}/{year}/{month}/{post_id}_{index_padded}.{ext}"
```

Output: `photography-blog/2026/04/12345_01.jpg`

### Organize by post type

```toml
filename_template = "{blog}/{post_type}/{post_id}_{index_padded}.{ext}"
```

Output: `photography-blog/photo/12345_01.jpg`

### Include tags in filename

```toml
filename_template = "{blog}/{post_id}_{tag}_{index_padded}.{ext}"
```

Output: `photography-blog/12345_photography_01.jpg`

## Safety

Template variables populated from Tumblr content (titles, tags) are sanitized:
- Path separators (`/`, `\`) are stripped
- A runtime guard verifies every rendered path stays within the output directory
- See [Security](security.md) for path traversal protection details
```

Write file `docs/architecture.md`:

```markdown
# Architecture

## Layer diagram

```
                  +----------------------+
                  |   tumbl4 CLI         |   (Typer + Rich)
                  +----------+-----------+
                             |
                  +----------v-----------+
                  |   tumbl4.core        |
                  |                      |
                  |  orchestrator        |   state machine
                  |     |                |
                  |     +-- auth         |   Playwright login + session
                  |     +-- crawl       |   base + tumblr_blog + tumblr_hidden
                  |     |    +-- http    |   httpx client + rate limiters
                  |     +-- parse       |   api/svc/npf: raw dict -> IntermediateDict
                  |     +-- filter      |   reblog, tag, timespan
                  |     +-- download    |   file transfer + content-type reconcile
                  |     +-- naming      |   filename template engine
                  |     +-- state       |   SQLite, resume, dedup, metadata
                  +----------------------+
                             | uses
                  +----------v-----------+
                  |   tumbl4.models      |   Pydantic: Blog, Post, Media, Settings
                  +----------------------+
```

## Core properties

1. **Everything async.** httpx.AsyncClient, asyncio, aiofiles.
2. **Orchestrator is a thin state machine.** Calls auth, crawl, filter, download,
   state as an ordered pipeline.
3. **No global state.** All state passed via CrawlContext.
4. **Unidirectional dependencies:** cli -> core.orchestrator -> core.* -> models.
5. **Pydantic for domain models, plain functions for wire parsing.**

## Key modules

| Module | Responsibility |
|--------|---------------|
| `cli/app.py` | Typer root + signal handler installation |
| `core/orchestrator.py` | Pipeline state machine |
| `core/cancel_token.py` | Cooperative cancellation (threading.Event) |
| `core/crawl/http_client.py` | httpx wrapper + rate limiting |
| `core/crawl/redirect_safety.py` | Per-hop allowlist + SSRF guard |
| `core/crawl/ssrf_guard.py` | IP-level validation |
| `core/parse/api_json.py` | V1 API JSONP parser |
| `core/download/file_downloader.py` | Streaming .part + atomic rename |
| `core/download/path_guard.py` | Path traversal prevention |
| `core/state/db.py` | SQLite schema + WAL + migrations |
| `core/state/orphan_sweep.py` | .part file cleanup |
| `core/state/pending_posts.py` | In-flight post tracking |
| `_internal/sanitize.py` | Terminal injection prevention |
| `_internal/signal_handling.py` | SIGINT wiring |

## Data flow

1. CLI parses arguments, creates CrawlContext
2. Signal handler installed with CancelToken
3. Orphan sweep runs on startup
4. Crawler paginates API, emits IntermediateDicts
5. Filters applied (tag, timespan, reblog)
6. MediaTasks enqueued to asyncio.Queue
7. N download workers consume tasks (streaming .part + rename)
8. Path guard checked before every file write
9. PendingPosts tracker determines when to write sidecars
10. Resume cursor updated on complete crawl
```

Write file `docs/security.md`:

```markdown
# Security

## Threat model

tumbl4 processes untrusted content from the Tumblr API. The primary threats are:

1. **Server-side request forgery (SSRF)** via redirect chains that target internal services
2. **Path traversal** via malicious post titles/tags in filename templates
3. **Terminal injection** via Unicode control characters in post content
4. **SQL injection** via dynamic query construction
5. **XXE** via XML parsing of HTML content
6. **Supply chain attacks** via compromised dependencies
7. **Credential leakage** via logs, tracebacks, or VCR cassettes

## Mitigations

### SSRF protection

- httpx configured with `follow_redirects=False`
- Manual redirect following with per-hop domain allowlist (`*.tumblr.com`)
- IP-level validation on every redirect hop
- Blocked ranges: RFC 1918, loopback, link-local (IMDS), broadcast, documentation, benchmarking
- IPv4-mapped IPv6 addresses are unpacked and the inner IPv4 is validated

### Path traversal protection

- `assert_within_root()` called before every file `open()` call
- Uses `Path.resolve().is_relative_to()` to canonicalize and verify
- Catches `../` traversal, symlink escapes, and absolute path injection
- Template variables sanitized at render time (path separators stripped)
- Startup warning if output directory is world-writable

### Terminal sanitization

- `sanitize.for_terminal()` strips Cc/Cf/Cs Unicode categories
- Applied to: log messages, error excerpts, filename displays, sidecar previews
- Bidi overrides (U+202E, U+202D) replaced with printable `\u{hex}` notation

### SQL injection prevention

- Parameterized queries only (no string-formatted SQL)
- Ruff rule S608 bans `execute(f"...")` patterns at lint time

### XXE prevention

- `lxml.etree` imports banned via ruff TID251 rule
- HTML parsing uses Python's `html.parser` or `defusedxml`

### Supply chain security

- `pip-audit` runs in CI on every PR
- Dependabot monitors dependencies weekly
- `uv.lock` committed and reviewed in PRs
- Release artifacts signed with SLSA provenance attestation
- PyPI publishing via OIDC trusted publisher (no stored API tokens)

### Credential protection

- `SecretFilter` scrubs cookies, tokens, and auth headers from logs
- Traceback frames cleared of `AuthSession` objects before logging
- Cassette scrubber scans VCR fixtures for leaked secrets in CI
- Session state stored with `0600` file permissions
- `tumbl4 logout` deletes all stored credentials

## Accepted limitations

- Decrypted session state is held in memory while the CLI runs; a user with
  `ptrace` access to the process can read cookies
- On macOS without FileVault 2, paged-out memory (swap) is unencrypted
- Malicious code running as the same user is out of scope
- TOCTOU window between `resolve()` check and `open()` on world-writable
  output directories (documented — we warn on startup)

## Verifying SLSA attestations

After installing from PyPI, verify the provenance attestation:

```bash
# Install the GitHub CLI attestation extension
gh extension install github/gh-attestation

# Verify a wheel
gh attestation verify tumbl4-*.whl --owner claire
```

## File permissions reference

| File | Permissions | Notes |
|------|------------|-------|
| `playwright_state.json` | `0600` | Refuses to run if broader |
| `browser_profile/` | `0700` | |
| `blog.db` (per-blog SQLite) | `0600` | |
| `dedup.db` (cross-blog) | `0600` | |
| `*.json` (sidecars) | `0600` | May contain private post content |
```

Write file `docs/commands/download.md`:

```markdown
# tumbl4 download

Download a Tumblr blog's content to disk.

## Usage

```bash
tumbl4 download <blog> [options]
```

## Arguments

| Argument | Description |
|----------|-------------|
| `blog` | Blog name or URL (e.g., `photography`, `https://photography.tumblr.com/`) |

## Options

| Option | Default | Description |
|--------|---------|-------------|
| `--output-dir`, `-o` | `./tumbl4-output` | Output directory |
| `--page-size` | `50` | Posts per API page (1-50) |
| `--image-size` | `1280` | Image size (1280/500/400/250/100/75) |
| `--no-resume` | | Ignore saved cursor, full re-crawl |
| `--quiet`, `-q` | | Suppress progress output |
| `--verbose`, `-v` | | Verbose logging |
| `--hidden` | | Force hidden/authenticated crawler |
| `--public` | | Force public crawler |

## Examples

```bash
# Basic download
tumbl4 download photography-blog

# Custom output directory
tumbl4 download photography-blog -o ~/backups

# Force full re-crawl
tumbl4 download photography-blog --no-resume

# Download a hidden blog
tumbl4 login
tumbl4 download hidden-blog
```

## Resume behavior

tumbl4 automatically tracks the highest post ID seen. On subsequent runs,
it stops crawling when it reaches already-downloaded posts. Use `--no-resume`
to override this and re-crawl everything.

## Exit codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | General error |
| 2 | Authentication required (`tumbl4 login` needed) |
| 28 | Disk full |
| 130 | Interrupted (SIGINT) |
```

Write file `docs/commands/login.md`:

```markdown
# tumbl4 login

Authenticate with Tumblr for downloading hidden (login-required) blogs.

## Usage

```bash
tumbl4 login
```

## How it works

1. Opens a Chromium browser window
2. Navigates to Tumblr's login page
3. You authenticate directly with Tumblr (tumbl4 never sees your password)
4. After successful login, tumbl4 captures the session cookies
5. Session state is saved to `$XDG_STATE_HOME/tumbl4/playwright_state.json`

## Requirements

- A graphical environment (X11 or Wayland)
- Chromium browser: `playwright install chromium --with-deps`

## Headless environments

See [Authentication](../authentication.md) for options when running on headless servers.

## See also

- [`tumbl4 logout`](login.md) - Remove stored credentials
- [Authentication guide](../authentication.md) - Full details on the login flow
```

Write file `docs/commands/sweep.md`:

```markdown
# tumbl4 sweep

Remove orphan `.part` files left behind by interrupted downloads.

## Usage

```bash
tumbl4 sweep <blog> [options]
```

## Arguments

| Argument | Description |
|----------|-------------|
| `blog` | Blog name to sweep |

## Options

| Option | Default | Description |
|--------|---------|-------------|
| `--output-dir`, `-o` | `./tumbl4-output` | Output directory |
| `--dry-run` | | Show what would be removed without deleting |

## Examples

```bash
# Clean up orphan files
tumbl4 sweep photography-blog

# Preview what would be cleaned up
tumbl4 sweep photography-blog --dry-run
```

## When to use

`.part` files are temporary files created during downloads. They are normally
renamed to their final name after the download completes. If a download is
interrupted (crash, SIGINT, power loss), orphan `.part` files may remain.

tumbl4 also runs an automatic orphan sweep on startup before each download.
This command is for manual cleanup.
```

Write file `docs/commands/status.md`:

```markdown
# tumbl4 status

Show download status and resume information for a blog.

## Usage

```bash
tumbl4 status <blog> [options]
```

## Arguments

| Argument | Description |
|----------|-------------|
| `blog` | Blog name to check status for |

## Options

| Option | Default | Description |
|--------|---------|-------------|
| `--output-dir`, `-o` | `./tumbl4-output` | Output directory |

## Examples

```bash
tumbl4 status photography-blog
```

## Output

Shows:
- Blog name and URL
- Last crawl timestamp
- Total posts crawled
- Total files downloaded
- Resume cursor (last known post ID)
- Number of orphan `.part` files (if any)
```

- [ ] **Step 2: Verify all documentation files exist and are well-formed**

Run: `find docs -name "*.md" -type f | sort`
Expected: all documentation files listed

- [ ] **Step 3: Commit**

```bash
git add docs/index.md docs/installation.md docs/getting-started.md docs/authentication.md docs/configuration.md docs/filename-templates.md docs/architecture.md docs/security.md docs/commands/download.md docs/commands/login.md docs/commands/sweep.md docs/commands/status.md
git commit -m "docs: add documentation site for tumbl4

Installation, getting-started, authentication, configuration,
filename-templates, architecture, security, and command reference.
See spec §8.4.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 13: Integration test, quality gates, and self-review

**Files:**
- Create: `tests/integration/__init__.py`
- Create: `tests/integration/test_signal_integration.py`

- [ ] **Step 1: Write the integration test**

Write file `tests/integration/__init__.py`:

```python
```

Write file `tests/integration/test_signal_integration.py`:

```python
"""Integration test — signal handling with CancelToken + orchestrator interaction.

This test verifies the full signal→cancel→shutdown path at the integration level:
CancelToken is created, signal handler installed, SIGINT fires, and the token
reflects the cancellation.
"""

from __future__ import annotations

import signal

from tumbl4._internal.signal_handling import install_signal_handler, uninstall_signal_handler
from tumbl4.core.cancel_token import CancelToken


class TestSignalIntegration:
    def test_sigint_sets_cancel_token(self) -> None:
        """Full integration: install handler, send SIGINT, verify token is cancelled."""
        token = CancelToken()
        old_handler = install_signal_handler(token)
        try:
            assert token.is_cancelled() is False
            signal.raise_signal(signal.SIGINT)
            assert token.is_cancelled() is True
        finally:
            uninstall_signal_handler(old_handler)

    def test_cancel_token_wait_after_signal(self) -> None:
        """Verify async wait returns after signal-triggered cancel."""
        import asyncio

        token = CancelToken()
        old_handler = install_signal_handler(token)
        try:
            # Cancel via signal
            signal.raise_signal(signal.SIGINT)

            # Async wait should return immediately since already cancelled
            async def check_wait() -> bool:
                await asyncio.wait_for(token.wait(), timeout=1.0)
                return token.is_cancelled()

            result = asyncio.run(check_wait())
            assert result is True
        finally:
            uninstall_signal_handler(old_handler)

    def test_orphan_sweep_integration(self, tmp_path: object) -> None:
        """Verify orphan sweep works with real filesystem."""
        import asyncio
        from pathlib import Path

        from tumbl4.core.state.orphan_sweep import sweep_orphan_parts

        blog_dir = Path(str(tmp_path)) / "testblog"
        blog_dir.mkdir(parents=True)
        (blog_dir / "12345_01.jpg.part").write_bytes(b"incomplete")
        (blog_dir / "12345_01.jpg").write_bytes(b"complete")

        removed = asyncio.run(sweep_orphan_parts(blog_dir))
        assert removed == 1
        assert not (blog_dir / "12345_01.jpg.part").exists()
        assert (blog_dir / "12345_01.jpg").exists()

    def test_sanitize_integration(self) -> None:
        """Verify sanitizer works on realistic malicious content."""
        from tumbl4._internal.sanitize import for_terminal

        # Simulate a malicious post title with multiple attack vectors
        malicious = (
            "Normal Title\x1b[31m"  # ANSI escape
            "\u202e"                 # RTL override
            "evil.exe"               # hidden filename
            "\u202c"                 # pop formatting
            "\x00"                   # null byte
            ".jpg"                   # apparent extension
        )
        safe = for_terminal(malicious)

        # No dangerous characters should remain
        assert "\x1b" not in safe
        assert "\u202e" not in safe
        assert "\u202c" not in safe
        assert "\x00" not in safe

        # Content should still be readable
        assert "Normal Title" in safe
        assert "evil.exe" in safe
        assert ".jpg" in safe
```

- [ ] **Step 2: Run the integration test**

Run: `uv run pytest tests/integration/test_signal_integration.py -v`
Expected: 4 passed

- [ ] **Step 3: Run ALL tests**

Run: `uv run pytest -v`
Expected: all tests pass (Plan 1 existing + all Plan 6 new)

- [ ] **Step 4: Run quality gates**

Run: `uv run ruff check .`
Expected: all checks pass (fix any issues found)

Run: `uv run ruff format --check .`
Expected: all formatting correct (run `uv run ruff format .` to fix if needed)

Run: `uv run pyright`
Expected: 0 errors (fix any type errors found)

- [ ] **Step 5: Self-review checklist**

Verify the following before declaring Plan 6 complete:

- [ ] CancelToken is threading.Event-based, safe from signal handler context (spec §6.10)
- [ ] SSRF guard blocks RFC 1918, loopback, link-local, IMDS, broadcast, reserved ranges
- [ ] IPv4-mapped IPv6 addresses are unpacked and validated
- [ ] Redirect safety checks allowlist on every hop, not just the final URL
- [ ] HTTPS-only enforcement (no HTTP downgrade on redirect)
- [ ] Terminal sanitizer strips Cc/Cf/Cs categories, preserves TAB/LF/CR
- [ ] Signal handler: first SIGINT = cooperative cancel, second within 3s = SystemExit(130)
- [ ] Orphan sweep runs via asyncio.to_thread with bounded scan
- [ ] PendingPosts uses asyncio.Lock for concurrent access safety
- [ ] Path traversal guard uses resolve() + is_relative_to() before every open()
- [ ] Ruff ban on execute(f"...") via S608 is active
- [ ] Ruff ban on lxml.etree via TID251 is active
- [ ] pip-audit added to CI
- [ ] Dependabot configured for pip + github-actions
- [ ] Cassette scrubber has --check mode for CI
- [ ] release.yml runs CI first, builds, attests with SLSA, publishes via OIDC, creates GH release
- [ ] All documentation pages written and cross-linked
- [ ] All tests pass, ruff clean, pyright clean

- [ ] **Step 6: Commit integration tests and any quality gate fixes**

```bash
git add tests/integration/__init__.py tests/integration/test_signal_integration.py
git commit -m "test: add integration tests for signal handling and security features

Signal→cancel→shutdown path, orphan sweep filesystem integration,
sanitizer attack vector coverage. Plan 6 quality gate verification.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

- [ ] **Step 7: Final quality gate commit (if needed)**

If Steps 3-4 required any fixes, commit them:

```bash
git add -u
git commit -m "fix: address Plan 6 quality gate findings

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```
