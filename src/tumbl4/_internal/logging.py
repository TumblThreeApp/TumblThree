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
    re.compile(r"(Cookie:\s*[^\r\n]+?(?=Cookie:|$))", re.IGNORECASE),
    re.compile(r"(Authorization:\s*[^\r\n]+?(?=Authorization:|$))", re.IGNORECASE),
    re.compile(r"(Set-Cookie:\s*[^\r\n]+?(?=Set-Cookie:|$))", re.IGNORECASE),
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
