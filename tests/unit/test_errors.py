"""Tests for tumbl4 exception taxonomy."""

from __future__ import annotations

import pytest

from tumbl4.core.errors import (
    AllowlistViolation,
    BlogNotFound,
    BlogRequiresLogin,
    ConfigError,
    CrawlError,
    DiskFull,
    DownloadError,
    HashMismatch,
    ParseError,
    RateLimited,
    ResponseTooLarge,
    ServerError,
    StateError,
    Tumbl4Error,
    WriteFailed,
)

# ---------------------------------------------------------------------------
# Base class
# ---------------------------------------------------------------------------


def test_tumbl4error_is_exception() -> None:
    assert issubclass(Tumbl4Error, Exception)


def test_tumbl4error_can_be_raised() -> None:
    with pytest.raises(Tumbl4Error):
        raise Tumbl4Error("base error")


# ---------------------------------------------------------------------------
# First-level branches all inherit from Tumbl4Error
# ---------------------------------------------------------------------------


@pytest.mark.parametrize(
    "exc_cls",
    [ConfigError, CrawlError, DownloadError, StateError],
)
def test_first_level_branches_inherit_tumbl4error(exc_cls: type[Tumbl4Error]) -> None:
    assert issubclass(exc_cls, Tumbl4Error)


# ---------------------------------------------------------------------------
# CrawlError subtree
# ---------------------------------------------------------------------------


@pytest.mark.parametrize(
    "exc_cls",
    [RateLimited, ServerError, BlogNotFound, BlogRequiresLogin, ResponseTooLarge, ParseError],
)
def test_crawl_subclasses_inherit_crawlerror(exc_cls: type[CrawlError]) -> None:
    assert issubclass(exc_cls, CrawlError)


@pytest.mark.parametrize(
    "exc_cls",
    [RateLimited, ServerError, BlogNotFound, BlogRequiresLogin, ResponseTooLarge, ParseError],
)
def test_crawl_subclasses_inherit_tumbl4error(exc_cls: type[CrawlError]) -> None:
    assert issubclass(exc_cls, Tumbl4Error)


# ---------------------------------------------------------------------------
# DownloadError subtree
# ---------------------------------------------------------------------------


@pytest.mark.parametrize(
    "exc_cls",
    [DiskFull, WriteFailed, HashMismatch, AllowlistViolation],
)
def test_download_subclasses_inherit_downloaderror(exc_cls: type[DownloadError]) -> None:
    assert issubclass(exc_cls, DownloadError)


@pytest.mark.parametrize(
    "exc_cls",
    [DiskFull, WriteFailed, HashMismatch, AllowlistViolation],
)
def test_download_subclasses_inherit_tumbl4error(exc_cls: type[DownloadError]) -> None:
    assert issubclass(exc_cls, Tumbl4Error)


# ---------------------------------------------------------------------------
# RateLimited stores retry_after
# ---------------------------------------------------------------------------


def test_rate_limited_stores_retry_after_float() -> None:
    exc = RateLimited("too many requests", retry_after=30.0)
    assert exc.retry_after == 30.0


def test_rate_limited_stores_retry_after_none() -> None:
    exc = RateLimited("too many requests", retry_after=None)
    assert exc.retry_after is None


def test_rate_limited_retry_after_defaults_to_none() -> None:
    exc = RateLimited("too many requests")
    assert exc.retry_after is None


def test_rate_limited_can_be_raised_and_caught() -> None:
    with pytest.raises(RateLimited) as exc_info:
        raise RateLimited("slow down", retry_after=60.5)
    assert exc_info.value.retry_after == 60.5


# ---------------------------------------------------------------------------
# ServerError stores status_code
# ---------------------------------------------------------------------------


def test_server_error_stores_status_code() -> None:
    exc = ServerError("internal server error", status_code=500)
    assert exc.status_code == 500


def test_server_error_can_be_raised_and_caught() -> None:
    with pytest.raises(ServerError) as exc_info:
        raise ServerError("bad gateway", status_code=502)
    assert exc_info.value.status_code == 502


# ---------------------------------------------------------------------------
# ParseError stores excerpt
# ---------------------------------------------------------------------------


def test_parse_error_stores_excerpt() -> None:
    exc = ParseError("failed to parse", excerpt="<html>bad content</html>")
    assert exc.excerpt == "<html>bad content</html>"


def test_parse_error_can_be_raised_and_caught() -> None:
    with pytest.raises(ParseError) as exc_info:
        raise ParseError("unexpected token", excerpt="{'key': }")
    assert exc_info.value.excerpt == "{'key': }"


# ---------------------------------------------------------------------------
# Catch-by-base semantics (isinstance / except hierarchy)
# ---------------------------------------------------------------------------


def test_rate_limited_caught_as_crawlerror() -> None:
    with pytest.raises(CrawlError):
        raise RateLimited("rate limited")


def test_server_error_caught_as_tumbl4error() -> None:
    with pytest.raises(Tumbl4Error):
        raise ServerError("server error", status_code=503)


def test_disk_full_caught_as_downloaderror() -> None:
    with pytest.raises(DownloadError):
        raise DiskFull("disk full")


def test_hash_mismatch_caught_as_tumbl4error() -> None:
    with pytest.raises(Tumbl4Error):
        raise HashMismatch("hash mismatch")


def test_config_error_caught_as_tumbl4error() -> None:
    with pytest.raises(Tumbl4Error):
        raise ConfigError("bad config")


def test_state_error_caught_as_tumbl4error() -> None:
    with pytest.raises(Tumbl4Error):
        raise StateError("bad state")
