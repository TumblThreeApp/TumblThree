"""Tests for TumblrHttpClient."""

from __future__ import annotations

import httpx
import pytest
import respx
from aiolimiter import AsyncLimiter

import tumbl4
from tumbl4.core.crawl.http_client import TumblrHttpClient
from tumbl4.core.errors import RateLimited, ResponseTooLarge, ServerError
from tumbl4.models.settings import HttpSettings

_TEST_URL = "https://api.tumblr.com/v2/blog/test/posts"


@respx.mock
async def test_get_api_returns_body_text() -> None:
    """get_api returns the response body as text on a 200 response."""
    respx.get(_TEST_URL).mock(return_value=httpx.Response(200, text="hello tumblr"))
    client = TumblrHttpClient(HttpSettings())
    try:
        result = await client.get_api(_TEST_URL)
        assert result == "hello tumblr"
    finally:
        await client.aclose()


@respx.mock
async def test_get_api_raises_rate_limited_on_429() -> None:
    """get_api raises RateLimited with retry_after when the server returns 429."""
    respx.get(_TEST_URL).mock(
        return_value=httpx.Response(429, headers={"Retry-After": "42"}, text="slow down")
    )
    client = TumblrHttpClient(HttpSettings())
    try:
        with pytest.raises(RateLimited) as exc_info:
            await client.get_api(_TEST_URL)
        assert exc_info.value.retry_after == 42.0
    finally:
        await client.aclose()


@respx.mock
async def test_get_api_raises_rate_limited_without_retry_after_header() -> None:
    """get_api raises RateLimited with retry_after=None when Retry-After is absent."""
    respx.get(_TEST_URL).mock(return_value=httpx.Response(429, text="slow down"))
    client = TumblrHttpClient(HttpSettings())
    try:
        with pytest.raises(RateLimited) as exc_info:
            await client.get_api(_TEST_URL)
        assert exc_info.value.retry_after is None
    finally:
        await client.aclose()


@respx.mock
async def test_get_api_raises_server_error_on_500() -> None:
    """get_api raises ServerError with the correct status_code on a 500 response."""
    respx.get(_TEST_URL).mock(return_value=httpx.Response(500, text="oops"))
    client = TumblrHttpClient(HttpSettings())
    try:
        with pytest.raises(ServerError) as exc_info:
            await client.get_api(_TEST_URL)
        assert exc_info.value.status_code == 500
    finally:
        await client.aclose()


@respx.mock
async def test_get_api_raises_server_error_on_503() -> None:
    """get_api raises ServerError with the correct status_code on a 503 response."""
    respx.get(_TEST_URL).mock(return_value=httpx.Response(503, text="unavailable"))
    client = TumblrHttpClient(HttpSettings())
    try:
        with pytest.raises(ServerError) as exc_info:
            await client.get_api(_TEST_URL)
        assert exc_info.value.status_code == 503
    finally:
        await client.aclose()


@respx.mock
async def test_get_api_raises_response_too_large() -> None:
    """get_api raises ResponseTooLarge when body exceeds max_api_response_bytes."""
    # Build a body that is just over the cap
    cap = 1024
    big_body = "x" * (cap + 1)
    respx.get(_TEST_URL).mock(return_value=httpx.Response(200, text=big_body))
    settings = HttpSettings(max_api_response_bytes=cap)
    client = TumblrHttpClient(settings)
    try:
        with pytest.raises(ResponseTooLarge):
            await client.get_api(_TEST_URL)
    finally:
        await client.aclose()


async def test_user_agent_contains_tumbl4_prefix() -> None:
    """user_agent attribute starts with 'tumbl4/'."""
    client = TumblrHttpClient(HttpSettings())
    try:
        assert client.user_agent.startswith("tumbl4/")
    finally:
        await client.aclose()


async def test_user_agent_includes_version_and_suffix() -> None:
    """user_agent includes the version string and the configured suffix."""
    settings = HttpSettings(user_agent_suffix="test-suffix")
    client = TumblrHttpClient(settings)
    try:
        assert tumbl4.__version__ in client.user_agent
        assert "test-suffix" in client.user_agent
    finally:
        await client.aclose()


@respx.mock
async def test_rate_limiter_is_acquired_before_request() -> None:
    """Rate limiter acquire() is called before every get_api request."""
    respx.get(_TEST_URL).mock(return_value=httpx.Response(200, text="ok"))

    acquired: list[bool] = []

    class SpyLimiter(AsyncLimiter):
        async def __aenter__(self) -> None:
            acquired.append(True)
            await super().__aenter__()

    spy_limiter = SpyLimiter(100, 1)
    client = TumblrHttpClient(HttpSettings(), rate_limiter=spy_limiter)
    try:
        await client.get_api(_TEST_URL)
        assert acquired, "rate limiter was never acquired"
    finally:
        await client.aclose()


async def test_client_property_returns_httpx_async_client() -> None:
    """The .client property returns the underlying httpx.AsyncClient."""
    client = TumblrHttpClient(HttpSettings())
    try:
        assert isinstance(client.client, httpx.AsyncClient)
    finally:
        await client.aclose()


async def test_rate_limiter_property_returns_async_limiter() -> None:
    """The .rate_limiter property returns the AsyncLimiter instance."""
    client = TumblrHttpClient(HttpSettings())
    try:
        assert isinstance(client.rate_limiter, AsyncLimiter)
    finally:
        await client.aclose()
