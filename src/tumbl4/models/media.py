"""MediaTask and DownloadResult domain models."""

from __future__ import annotations

import hashlib
from pathlib import Path
from typing import Literal
from urllib.parse import urlparse

from pydantic import BaseModel, computed_field


class MediaTask(BaseModel):
    """Describes a single media file to download."""

    url: str
    post_id: str
    blog_name: str
    index: int
    output_dir: str

    @computed_field  # type: ignore[prop-decorator]
    @property
    def url_hash(self) -> str:
        """SHA-256 hex digest of the URL, used for deduplication."""
        return hashlib.sha256(self.url.encode()).hexdigest()

    @computed_field  # type: ignore[prop-decorator]
    @property
    def filename(self) -> str:
        """``{post_id}_{index:02d}.{ext}`` — ext derived from the URL path."""
        path = urlparse(self.url).path
        # posixpath.splitext analogue — take everything after the last dot
        dot_pos = path.rfind(".")
        if dot_pos != -1 and dot_pos > path.rfind("/"):
            ext = path[dot_pos + 1 :]
            return f"{self.post_id}_{self.index:02d}.{ext}"
        return f"{self.post_id}_{self.index:02d}"

    @computed_field  # type: ignore[prop-decorator]
    @property
    def final_path(self) -> Path:
        """Absolute path where the finished file will be written."""
        return Path(self.output_dir) / self.filename

    @computed_field  # type: ignore[prop-decorator]
    @property
    def part_path(self) -> Path:
        """Temporary ``.part`` path used during download."""
        return Path(self.output_dir) / f"{self.filename}.part"


class DownloadResult(BaseModel):
    """Outcome of a single media download attempt."""

    url: str
    post_id: str
    filename: str | None
    byte_count: int
    status: Literal["success", "failed"]
    error: str | None = None
