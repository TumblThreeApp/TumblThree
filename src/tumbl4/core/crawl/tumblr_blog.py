"""Public blog crawler using the Tumblr V1 API (``/api/read/json``).

The V1 API is unauthenticated and returns JSONP.  This module provides
:class:`TumblrBlogCrawler`, an async generator that pages through a blog's
post history in offset-based batches and yields one
:class:`~tumbl4.core.parse.intermediate.IntermediateDict` per photo post.

Features:
- Offset-based pagination with configurable page size.
- ``last_id`` stop-fence for incremental / resume crawls.
- ``highest_post_id`` tracking across the full crawl.
- Photo-only filtering (Plan 2).
- Graceful :exc:`~tumbl4.core.errors.RateLimited` handling — sets
  :attr:`TumblrBlogCrawler.rate_limited` and stops rather than propagating.
"""

from __future__ import annotations

from collections.abc import AsyncIterator

from tumbl4._internal.logging import get_logger
from tumbl4.core.crawl.http_client import TumblrHttpClient
from tumbl4.core.errors import RateLimited
from tumbl4.core.parse.api_json import normalize_photo_post, parse_v1_response, strip_jsonp
from tumbl4.core.parse.intermediate import IntermediateDict
from tumbl4.models.blog import BlogRef

__all__ = ["TumblrBlogCrawler"]

_log = get_logger("crawl.tumblr_blog")


class TumblrBlogCrawler:
    """Async generator crawler for a public Tumblr blog via the V1 API.

    Args:
        http: HTTP client used for all API requests.
        blog: The blog to crawl.
        page_size: Number of posts to request per API call (default 50).
        last_id: Resume fence — posts with ``id <= last_id`` are skipped and
            iteration stops once all posts on a page are below this fence.
        image_size: Preferred image size suffix for photo URLs (default "1280").

    Attributes:
        highest_post_id: The largest post id seen so far (updated as posts
            are processed, regardless of whether they are yielded).
        total_posts: Total post count as reported by the API on the first page.
            Updated after the first page is fetched; zero before that.
        rate_limited: Set to ``True`` when a
            :exc:`~tumbl4.core.errors.RateLimited` exception is caught.
    """

    def __init__(
        self,
        http: TumblrHttpClient,
        blog: BlogRef,
        *,
        page_size: int = 50,
        last_id: int = 0,
        image_size: str = "1280",
    ) -> None:
        self._http = http
        self._blog = blog
        self._page_size = page_size
        self._last_id = last_id
        self._image_size = image_size

        self.highest_post_id: int = 0
        self.total_posts: int = 0
        self.rate_limited: bool = False

    async def crawl(self) -> AsyncIterator[IntermediateDict]:
        """Async generator yielding one :class:`IntermediateDict` per photo post.

        Paginates from offset 0 in increments of *page_size* until:
        - The API returns an empty posts list, or
        - Every post on a page has ``id <= last_id`` (resume fence reached), or
        - The current offset meets or exceeds ``total_posts``, or
        - A :exc:`~tumbl4.core.errors.RateLimited` exception is raised
          (``self.rate_limited`` is set to ``True`` and the generator returns).

        Yields:
            :class:`~tumbl4.core.parse.intermediate.IntermediateDict` for each
            photo post whose id is above the ``last_id`` fence.
        """
        offset = 0

        while True:
            url = f"{self._blog.url}api/read/json?debug=1&num={self._page_size}&start={offset}"
            _log.debug(
                "Fetching V1 API page",
                extra={"blog": self._blog.name, "offset": offset, "url": url},
            )

            try:
                raw = await self._http.get_api(url)
            except RateLimited:
                _log.warning(
                    "Rate limited while crawling blog",
                    extra={"blog": self._blog.name, "offset": offset},
                )
                self.rate_limited = True
                return

            data = strip_jsonp(raw)
            blog_name, total_posts, posts = parse_v1_response(data)

            # Update total_posts from every page; the first page is canonical.
            self.total_posts = total_posts

            if not posts:
                _log.debug(
                    "Empty posts list — stopping",
                    extra={"blog": self._blog.name, "offset": offset},
                )
                return

            all_below_fence = True
            for post in posts:
                raw_id = post.get("id", 0)
                try:
                    post_id = int(raw_id)
                except (ValueError, TypeError):
                    post_id = 0

                # Track the highest post id seen.
                self.highest_post_id = max(self.highest_post_id, post_id)

                # Resume fence: skip posts at or below last_id.
                if post_id <= self._last_id:
                    continue

                all_below_fence = False

                # Plan 2: photo posts only.
                post_type = post.get("type", "")
                if post_type != "photo":
                    continue

                yield normalize_photo_post(post, blog_name, self._image_size)

            if all_below_fence:
                _log.debug(
                    "All posts on page are at or below last_id fence — stopping",
                    extra={
                        "blog": self._blog.name,
                        "offset": offset,
                        "last_id": self._last_id,
                    },
                )
                return

            offset += self._page_size
            if offset >= total_posts:
                _log.debug(
                    "Offset reached total_posts — stopping",
                    extra={
                        "blog": self._blog.name,
                        "offset": offset,
                        "total_posts": total_posts,
                    },
                )
                return
