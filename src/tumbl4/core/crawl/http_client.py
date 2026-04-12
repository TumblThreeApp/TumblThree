"""HTTP client wrapper for Tumblr API requests.

Wraps :class:`httpx.AsyncClient` with:
- Configurable timeouts and connection limits from :class:`HttpSettings`
- Rate limiting via :class:`aiolimiter.AsyncLimiter`
- Standardised ``User-Agent`` header
- Response size cap
- Error mapping to the tumbl4 exception hierarchy
"""

from __future__ import annotations

import httpx
from aiolimiter import AsyncLimiter

import tumbl4
from tumbl4.core.errors import RateLimited, ResponseTooLarge, ServerError
from tumbl4.models.settings import HttpSettings

__all__ = ["TumblrHttpClient"]

_DEFAULT_RATE_LIMIT_REQUESTS: float = 20.0
_DEFAULT_RATE_LIMIT_PERIOD: float = 10.0


def _check_status(response: httpx.Response) -> None:
    """Map HTTP error responses to tumbl4 exceptions.

    Args:
        response: The :class:`httpx.Response` to inspect.

    Raises:
        RateLimited: On HTTP 429, with ``retry_after`` parsed from the
            ``Retry-After`` header when present.
        ServerError: On any 5xx response.
        httpx.HTTPStatusError: For any other non-2xx response (via
            :meth:`httpx.Response.raise_for_status`).
    """
    if response.status_code == 429:
        retry_after_raw = response.headers.get("Retry-After")
        retry_after: float | None = None
        if retry_after_raw is not None:
            try:
                retry_after = float(retry_after_raw)
            except ValueError:
                retry_after = None
        raise RateLimited(
            f"Rate limited by server (HTTP 429)",
            retry_after=retry_after,
        )
    if response.is_server_error:
        raise ServerError(
            f"Server error (HTTP {response.status_code})",
            status_code=response.status_code,
        )
    response.raise_for_status()


class TumblrHttpClient:
    """Async HTTP client configured for Tumblr API access.

    Args:
        settings: HTTP-layer configuration (timeouts, limits, user-agent suffix).
        rate_limiter: Optional custom rate limiter.  If ``None``, a default
            limiter of 20 requests per 10 seconds is created.
    """

    def __init__(
        self,
        settings: HttpSettings,
        rate_limiter: AsyncLimiter | None = None,
    ) -> None:
        self.user_agent: str = (
            f"tumbl4/{tumbl4.__version__} ({settings.user_agent_suffix})"
        )
        self._rate_limiter: AsyncLimiter = rate_limiter or AsyncLimiter(
            _DEFAULT_RATE_LIMIT_REQUESTS,
            _DEFAULT_RATE_LIMIT_PERIOD,
        )
        self._settings: HttpSettings = settings
        self._client: httpx.AsyncClient = httpx.AsyncClient(
            timeout=httpx.Timeout(
                connect=settings.connect_timeout,
                read=settings.read_timeout,
                write=settings.write_timeout,
                pool=settings.pool_timeout,
            ),
            limits=httpx.Limits(
                max_connections=settings.max_connections,
                max_keepalive_connections=settings.max_keepalive_connections,
            ),
            max_redirects=settings.max_redirects,
            headers={"User-Agent": self.user_agent},
        )

    @property
    def client(self) -> httpx.AsyncClient:
        """The underlying :class:`httpx.AsyncClient` (for streaming downloads)."""
        return self._client

    @property
    def rate_limiter(self) -> AsyncLimiter:
        """The :class:`aiolimiter.AsyncLimiter` controlling request throughput."""
        return self._rate_limiter

    async def get_api(self, url: str) -> str:
        """Perform a rate-limited GET request and return the response body text.

        Args:
            url: The URL to fetch.

        Returns:
            The response body decoded as text.

        Raises:
            RateLimited: On HTTP 429.
            ServerError: On any 5xx response.
            ResponseTooLarge: When the response body exceeds
                :attr:`HttpSettings.max_api_response_bytes`.
            httpx.HTTPStatusError: For other non-2xx responses.
        """
        async with self._rate_limiter:
            response = await self._client.get(url)

        _check_status(response)

        body = response.content
        if len(body) > self._settings.max_api_response_bytes:
            raise ResponseTooLarge(
                f"Response from {url!r} is {len(body)} bytes, "
                f"exceeding the {self._settings.max_api_response_bytes}-byte cap"
            )

        return response.text

    async def aclose(self) -> None:
        """Close the underlying HTTP client and release connections."""
        await self._client.aclose()
