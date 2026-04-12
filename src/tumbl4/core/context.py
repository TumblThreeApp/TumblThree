"""CrawlContext — frozen configuration snapshot for a single blog crawl.

Bundles the immutable settings a crawl pipeline needs so that individual
components (download workers, sidecar writer, resume cursor logic) do not
each need to accept a half-dozen loose parameters.
"""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

from tumbl4.models.blog import BlogRef
from tumbl4.models.settings import Settings


@dataclass(frozen=True)
class CrawlContext:
    """Immutable snapshot of per-blog crawl configuration.

    Attributes:
        blog: The blog being crawled.
        settings: Application-wide settings.
        blog_output_dir: Resolved output directory for this blog's media.
        image_size: Tumblr image size suffix (default ``"1280"``).
    """

    blog: BlogRef
    settings: Settings
    blog_output_dir: Path
    image_size: str = "1280"
