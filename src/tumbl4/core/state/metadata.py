"""JSON metadata sidecar writer for tumbl4."""

from __future__ import annotations

import json
import os
from pathlib import Path

from tumbl4._internal.logging import get_logger

log = get_logger("core.state.metadata")


def write_sidecar(
    *,
    output_dir: Path,
    post_id: str,
    blog_name: str,
    post_url: str,
    post_type: str,
    timestamp_utc: str,
    tags: list[str],
    is_reblog: bool,
    media_results: list[dict[str, object]],
    reblog_source: dict[str, str] | None = None,
    title: str | None = None,
    body_text: str | None = None,
    body_html: str | None = None,
) -> Path:
    """Write a JSON metadata sidecar for a post.

    Writes to ``{output_dir}/_meta/{post_id}.json`` using an atomic
    ``.part`` + ``os.rename`` pattern with 0o600 file permissions.

    Returns the final sidecar path.
    """
    meta_dir = output_dir / "_meta"
    meta_dir.mkdir(parents=True, exist_ok=True)

    final_path = meta_dir / f"{post_id}.json"
    part_path = meta_dir / f"{post_id}.json.part"

    payload: dict[str, object] = {
        "$schema_version": 1,
        "blog": blog_name,
        "post_id": post_id,
        "post_url": post_url,
        "type": post_type,
        "timestamp_utc": timestamp_utc,
        "tags": tags,
        "is_reblog": is_reblog,
        "reblog_source": reblog_source,
        "title": title,
        "body_text": body_text,
        "body_html": body_html,
        "media": media_results,
    }

    content = json.dumps(payload, ensure_ascii=False, indent=2)
    encoded = content.encode()

    # Write to .part file with 0o600 permissions using os.open
    fd = os.open(str(part_path), os.O_WRONLY | os.O_CREAT | os.O_TRUNC, 0o600)
    try:
        os.write(fd, encoded)
    finally:
        os.close(fd)

    os.rename(str(part_path), str(final_path))

    log.debug(
        "wrote metadata sidecar",
        extra={"post_id": post_id, "blog_name": blog_name, "path": str(final_path)},
    )

    return final_path
