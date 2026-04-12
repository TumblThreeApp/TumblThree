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
