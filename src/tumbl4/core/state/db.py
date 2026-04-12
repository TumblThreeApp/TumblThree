"""SQLite-backed state database for tumbl4."""

from __future__ import annotations

import sqlite3
from datetime import UTC, datetime

from tumbl4._internal.logging import get_logger

log = get_logger("core.state.db")

_SCHEMA_VERSION = 1

_DDL = """\
CREATE TABLE IF NOT EXISTS crawl_state (
    blog_name TEXT NOT NULL,
    crawler_type TEXT NOT NULL DEFAULT 'public',
    last_id INTEGER NOT NULL DEFAULT 0,
    last_complete_crawl TEXT,
    cursor_version INTEGER NOT NULL DEFAULT 1,
    PRIMARY KEY (blog_name, crawler_type)
);

CREATE TABLE IF NOT EXISTS downloads (
    url_hash TEXT PRIMARY KEY,
    url TEXT NOT NULL,
    blog_name TEXT NOT NULL,
    post_id TEXT NOT NULL,
    filename TEXT,
    bytes INTEGER NOT NULL DEFAULT 0,
    status TEXT NOT NULL DEFAULT 'success',
    error TEXT,
    created_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_downloads_blog ON downloads(blog_name);
CREATE INDEX IF NOT EXISTS idx_downloads_post ON downloads(post_id);

CREATE TABLE IF NOT EXISTS posts (
    post_id TEXT PRIMARY KEY,
    blog_name TEXT NOT NULL,
    sidecar_written INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL
);
"""


class StateDb:
    """Per-blog SQLite database tracking downloads, posts, and crawl cursors."""

    def __init__(self, path: str) -> None:
        log.debug("opening state database", extra={"db_path": path})
        self._conn = sqlite3.connect(path)
        self._conn.execute("PRAGMA journal_mode=WAL")
        self._conn.execute("PRAGMA synchronous=NORMAL")
        self._conn.executescript(_DDL)
        self._conn.execute(f"PRAGMA user_version = {_SCHEMA_VERSION}")
        self._conn.commit()

    def execute(self, sql: str, params: tuple[object, ...] = ()) -> sqlite3.Cursor:
        """Execute a parameterized SQL statement and return the cursor."""
        return self._conn.execute(sql, params)

    def record_download(
        self,
        *,
        url_hash: str,
        url: str,
        blog_name: str,
        post_id: str,
        filename: str | None,
        byte_count: int,
        status: str,
        error: str | None = None,
    ) -> None:
        """Insert or replace a download record."""
        now = datetime.now(UTC).isoformat()
        self._conn.execute(
            """
            INSERT OR REPLACE INTO downloads
                (url_hash, url, blog_name, post_id, filename, bytes, status, error, created_at)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            """,
            (url_hash, url, blog_name, post_id, filename, byte_count, status, error, now),
        )

    def is_downloaded(self, url_hash: str) -> bool:
        """Return True if the given URL hash exists in the downloads table."""
        row = self._conn.execute(
            "SELECT 1 FROM downloads WHERE url_hash = ?", (url_hash,)
        ).fetchone()
        return row is not None

    def mark_post_complete(self, post_id: str, blog_name: str) -> None:
        """Insert or replace a posts record with sidecar_written=1."""
        now = datetime.now(UTC).isoformat()
        self._conn.execute(
            """
            INSERT OR REPLACE INTO posts
                (post_id, blog_name, sidecar_written, created_at)
            VALUES (?, ?, 1, ?)
            """,
            (post_id, blog_name, now),
        )

    def is_post_complete(self, post_id: str) -> bool:
        """Return True if the post has sidecar_written=1."""
        row = self._conn.execute(
            "SELECT sidecar_written FROM posts WHERE post_id = ?", (post_id,)
        ).fetchone()
        return row is not None and bool(row[0])

    def commit(self) -> None:
        """Commit the current transaction."""
        self._conn.commit()

    def close(self) -> None:
        """Commit and close the database connection."""
        self._conn.commit()
        self._conn.close()
        log.debug("state database closed")
