"""Exception taxonomy for tumbl4.

All project-specific exceptions derive from :class:`Tumbl4Error`, which itself
subclasses :class:`Exception`.  The hierarchy is:

.. code-block:: text

    Tumbl4Error (base)
    ├── ConfigError
    ├── CrawlError
    │   ├── RateLimited      - retry_after: float | None
    │   ├── ServerError      - status_code: int
    │   ├── BlogNotFound
    │   ├── BlogRequiresLogin
    │   ├── ResponseTooLarge
    │   └── ParseError       - excerpt: str
    ├── DownloadError
    │   ├── DiskFull
    │   ├── WriteFailed
    │   ├── HashMismatch
    │   └── AllowlistViolation
    └── StateError

See design spec §6.1.
"""

from __future__ import annotations

__all__ = [
    "AllowlistViolation",
    "BlogNotFound",
    "BlogRequiresLogin",
    "ConfigError",
    "CrawlError",
    "DiskFull",
    "DownloadError",
    "HashMismatch",
    "ParseError",
    "RateLimited",
    "ResponseTooLarge",
    "ServerError",
    "StateError",
    "Tumbl4Error",
    "WriteFailed",
]


# ---------------------------------------------------------------------------
# Root
# ---------------------------------------------------------------------------


class Tumbl4Error(Exception):
    """Root exception for all tumbl4-specific errors."""


# ---------------------------------------------------------------------------
# First-level branches
# ---------------------------------------------------------------------------


class ConfigError(Tumbl4Error):
    """Raised when configuration is invalid or cannot be loaded."""


class CrawlError(Tumbl4Error):
    """Raised when a network crawl or API request cannot be completed."""


class DownloadError(Tumbl4Error):
    """Raised when a file download or local write operation fails."""


class StateError(Tumbl4Error):
    """Raised when the application state database is inconsistent or unusable."""


# ---------------------------------------------------------------------------
# CrawlError subtree
# ---------------------------------------------------------------------------


class RateLimited(CrawlError):
    """The remote host returned a rate-limit response (HTTP 429 or equivalent).

    Args:
        message: Human-readable description.
        retry_after: Seconds to wait before retrying, or ``None`` if the server
            did not supply a ``Retry-After`` header.
    """

    def __init__(self, message: str, *, retry_after: float | None = None) -> None:
        super().__init__(message)
        self.retry_after: float | None = retry_after


class ServerError(CrawlError):
    """The remote host returned a 5xx error response.

    Args:
        message: Human-readable description.
        status_code: The HTTP status code returned by the server.
    """

    def __init__(self, message: str, *, status_code: int) -> None:
        super().__init__(message)
        self.status_code: int = status_code


class BlogNotFound(CrawlError):
    """The requested Tumblr blog does not exist (HTTP 404)."""


class BlogRequiresLogin(CrawlError):
    """The requested Tumblr blog is only visible to logged-in users."""


class ResponseTooLarge(CrawlError):
    """The server response exceeded the configured size limit."""


class ParseError(CrawlError):
    """A crawled resource could not be parsed into the expected data model.

    Args:
        message: Human-readable description.
        excerpt: A short snippet of the unparseable content for diagnostics.
    """

    def __init__(self, message: str, *, excerpt: str) -> None:
        super().__init__(message)
        self.excerpt: str = excerpt


# ---------------------------------------------------------------------------
# DownloadError subtree
# ---------------------------------------------------------------------------


class DiskFull(DownloadError):
    """The destination filesystem has no space remaining."""


class WriteFailed(DownloadError):
    """Writing a file to disk failed for a reason other than disk-full."""


class HashMismatch(DownloadError):
    """The downloaded file's checksum does not match the expected value."""


class AllowlistViolation(DownloadError):
    """The media URL or file type is not permitted by the configured allowlist."""
