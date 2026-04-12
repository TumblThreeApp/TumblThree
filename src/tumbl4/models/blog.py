"""Blog and BlogRef domain models."""

from __future__ import annotations

from dataclasses import dataclass

from pydantic import BaseModel


@dataclass(frozen=True)
class BlogRef:
    """A lightweight, immutable reference to a Tumblr blog.

    Used as a key / parameter wherever a blog needs to be identified before
    the full Blog metadata has been fetched.
    """

    name: str  # subdomain, e.g. "photography"
    url: str  # full URL with trailing slash, e.g. "https://photography.tumblr.com/"

    @classmethod
    def from_input(cls, raw: str) -> BlogRef:
        """Normalise a user-supplied blog identifier into a BlogRef.

        Accepted forms:
        - Bare name: ``photography``
        - Subdomain URL: ``https://photography.tumblr.com`` (trailing slash optional)
        - www URL: ``https://www.tumblr.com/photography`` (trailing slash optional)
        """
        stripped = raw.strip().rstrip("/")

        if stripped.startswith("https://www.tumblr.com/"):
            # https://www.tumblr.com/photography[/...]
            path_part = stripped[len("https://www.tumblr.com/"):]
            name = path_part.split("/")[0].lower()
        elif stripped.startswith("https://") and ".tumblr.com" in stripped:
            # https://photography.tumblr.com
            host = stripped[len("https://"):].split("/")[0]
            name = host.replace(".tumblr.com", "").lower()
        else:
            # Bare name
            name = stripped.lower()

        url = f"https://{name}.tumblr.com/"
        return cls(name=name, url=url)


class Blog(BaseModel):
    """Full metadata for a Tumblr blog as returned by the API."""

    name: str
    url: str
    title: str | None = None
    total_posts: int = 0
