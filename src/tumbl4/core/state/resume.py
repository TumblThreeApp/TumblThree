"""Resume cursor persistence for tumbl4 crawlers."""

from __future__ import annotations

from datetime import UTC, datetime

from tumbl4._internal.logging import get_logger
from tumbl4.core.state.db import StateDb

log = get_logger("core.state.resume")


def load_cursor(db: StateDb, blog_name: str, crawler_type: str = "public") -> int:
    """Load last_id from crawl_state. Returns 0 if no cursor exists."""
    row = db.execute(
        "SELECT last_id FROM crawl_state WHERE blog_name = ? AND crawler_type = ?",
        (blog_name, crawler_type),
    ).fetchone()
    if row is None:
        log.debug(
            "no resume cursor found, starting from 0",
            extra={"blog_name": blog_name, "crawler_type": crawler_type},
        )
        return 0
    last_id: int = row[0]
    log.debug(
        "loaded resume cursor",
        extra={"blog_name": blog_name, "crawler_type": crawler_type, "last_id": last_id},
    )
    return last_id


def save_cursor(db: StateDb, blog_name: str, last_id: int, crawler_type: str = "public") -> None:
    """Save resume cursor. Uses INSERT ... ON CONFLICT ... DO UPDATE."""
    now = datetime.now(UTC).isoformat()
    db.execute(
        """
        INSERT INTO crawl_state (blog_name, crawler_type, last_id, last_complete_crawl)
        VALUES (?, ?, ?, ?)
        ON CONFLICT (blog_name, crawler_type)
        DO UPDATE SET last_id = excluded.last_id,
                      last_complete_crawl = excluded.last_complete_crawl
        """,
        (blog_name, crawler_type, last_id, now),
    )
    db.commit()
    log.debug(
        "saved resume cursor",
        extra={"blog_name": blog_name, "crawler_type": crawler_type, "last_id": last_id},
    )
