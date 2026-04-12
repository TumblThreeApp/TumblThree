"""Tests for tumbl4.core.state.metadata — JSON metadata sidecar writer."""

from __future__ import annotations

import json
import os
import stat
from pathlib import Path

from tumbl4.core.state.metadata import write_sidecar

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _write(output_dir: Path, **kwargs: object) -> Path:
    """Call write_sidecar with sensible defaults, allowing overrides."""
    defaults: dict[str, object] = {
        "output_dir": output_dir,
        "post_id": "123456789",
        "blog_name": "testblog",
        "post_url": "https://testblog.tumblr.com/post/123456789",
        "post_type": "photo",
        "timestamp_utc": "2024-01-15T12:00:00+00:00",
        "tags": ["cat", "cute"],
        "is_reblog": False,
        "media_results": [],
    }
    defaults.update(kwargs)
    return write_sidecar(**defaults)  # type: ignore[arg-type]


# ---------------------------------------------------------------------------
# Basic write tests
# ---------------------------------------------------------------------------


def test_writes_valid_json(tmp_path: Path) -> None:
    path = _write(tmp_path)
    with open(path) as f:
        data = json.load(f)
    assert data["$schema_version"] == 1
    assert data["blog"] == "testblog"
    assert data["post_id"] == "123456789"
    assert data["post_url"] == "https://testblog.tumblr.com/post/123456789"
    assert data["type"] == "photo"
    assert data["timestamp_utc"] == "2024-01-15T12:00:00+00:00"
    assert data["tags"] == ["cat", "cute"]
    assert data["is_reblog"] is False
    assert data["media"] == []


def test_no_part_file_remains(tmp_path: Path) -> None:
    _write(tmp_path)
    part_files = list(tmp_path.glob("**/*.part"))
    assert part_files == [], f"Unexpected .part files: {part_files}"


def test_creates_meta_dir(tmp_path: Path) -> None:
    path = _write(tmp_path)
    assert path.parent == tmp_path / "_meta"
    assert path.parent.is_dir()


def test_sidecar_path_is_post_id_json(tmp_path: Path) -> None:
    path = _write(tmp_path, post_id="987654321")
    assert path.name == "987654321.json"
    assert path == tmp_path / "_meta" / "987654321.json"


# ---------------------------------------------------------------------------
# File permissions
# ---------------------------------------------------------------------------


def test_file_permissions_are_0600(tmp_path: Path) -> None:
    path = _write(tmp_path)
    mode = stat.S_IMODE(os.stat(path).st_mode)
    assert mode == 0o600, f"Expected 0o600, got {oct(mode)}"


# ---------------------------------------------------------------------------
# Optional fields
# ---------------------------------------------------------------------------


def test_optional_fields_default_to_none(tmp_path: Path) -> None:
    path = _write(tmp_path)
    with open(path) as f:
        data = json.load(f)
    assert data["reblog_source"] is None
    assert data["title"] is None
    assert data["body_text"] is None
    assert data["body_html"] is None


def test_reblog_source_written_when_provided(tmp_path: Path) -> None:
    reblog = {"blog_name": "originalblog", "post_url": "https://originalblog.tumblr.com/post/1"}
    path = _write(tmp_path, is_reblog=True, reblog_source=reblog)
    with open(path) as f:
        data = json.load(f)
    assert data["is_reblog"] is True
    assert data["reblog_source"] == reblog


def test_title_and_body_written_when_provided(tmp_path: Path) -> None:
    path = _write(
        tmp_path,
        post_type="text",
        title="Hello World",
        body_text="Some plain text",
        body_html="<p>Some plain text</p>",
    )
    with open(path) as f:
        data = json.load(f)
    assert data["title"] == "Hello World"
    assert data["body_text"] == "Some plain text"
    assert data["body_html"] == "<p>Some plain text</p>"


# ---------------------------------------------------------------------------
# media_results in sidecar (including failed media)
# ---------------------------------------------------------------------------


def test_media_results_written_to_sidecar(tmp_path: Path) -> None:
    media = [
        {"url": "https://example.com/img1.jpg", "filename": "img1.jpg", "status": "success"},
        {"url": "https://example.com/img2.jpg", "filename": None, "status": "failed"},
    ]
    path = _write(tmp_path, media_results=media)
    with open(path) as f:
        data = json.load(f)
    assert len(data["media"]) == 2
    assert data["media"][0]["status"] == "success"
    assert data["media"][1]["status"] == "failed"


def test_failed_media_included_in_sidecar(tmp_path: Path) -> None:
    """Failed downloads must appear in the sidecar for later retry/audit."""
    media = [
        {
            "url": "https://example.com/broken.jpg",
            "filename": None,
            "status": "failed",
            "error": "HTTP 404",
        },
    ]
    path = _write(tmp_path, media_results=media)
    with open(path) as f:
        data = json.load(f)
    assert data["media"][0]["status"] == "failed"
    assert data["media"][0]["error"] == "HTTP 404"


# ---------------------------------------------------------------------------
# Return value
# ---------------------------------------------------------------------------


def test_returns_final_path(tmp_path: Path) -> None:
    path = _write(tmp_path, post_id="42")
    assert isinstance(path, Path)
    assert path.exists()
    assert path == tmp_path / "_meta" / "42.json"
