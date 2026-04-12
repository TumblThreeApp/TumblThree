"""Tests for tumbl4.core.state.db — SQLite state database."""

from __future__ import annotations

from pathlib import Path

from tumbl4.core.state.db import StateDb

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _make_db() -> StateDb:
    return StateDb(":memory:")


# ---------------------------------------------------------------------------
# Schema / init tests
# ---------------------------------------------------------------------------


def test_tables_created_on_init() -> None:
    db = _make_db()
    try:
        rows = db.execute(
            "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name"
        ).fetchall()
        table_names = {row[0] for row in rows}
        assert "crawl_state" in table_names
        assert "downloads" in table_names
        assert "posts" in table_names
    finally:
        db.close()


def test_wal_mode_enabled_in_memory() -> None:
    db = _make_db()
    try:
        row = db.execute("PRAGMA journal_mode").fetchone()
        # :memory: databases report 'memory', not 'wal', because WAL is a
        # file-level concept. We verify the PRAGMA was accepted without error.
        assert row is not None
    finally:
        db.close()


def test_user_version_set_to_1() -> None:
    db = _make_db()
    try:
        row = db.execute("PRAGMA user_version").fetchone()
        assert row is not None
        assert row[0] == 1
    finally:
        db.close()


# ---------------------------------------------------------------------------
# record_download / is_downloaded
# ---------------------------------------------------------------------------


def test_record_download_and_is_downloaded() -> None:
    db = _make_db()
    try:
        db.record_download(
            url_hash="abc123",
            url="https://example.com/image.jpg",
            blog_name="testblog",
            post_id="12345",
            filename="image.jpg",
            byte_count=1024,
            status="success",
        )
        db.commit()
        assert db.is_downloaded("abc123") is True
        assert db.is_downloaded("nonexistent") is False
    finally:
        db.close()


def test_record_download_with_failed_status() -> None:
    db = _make_db()
    try:
        db.record_download(
            url_hash="deadbeef",
            url="https://example.com/broken.jpg",
            blog_name="testblog",
            post_id="99999",
            filename=None,
            byte_count=0,
            status="failed",
            error="HTTP 404",
        )
        db.commit()
        assert db.is_downloaded("deadbeef") is True
        row = db.execute(
            "SELECT status, error FROM downloads WHERE url_hash = ?", ("deadbeef",)
        ).fetchone()
        assert row is not None
        assert row[0] == "failed"
        assert row[1] == "HTTP 404"
    finally:
        db.close()


def test_record_download_insert_or_replace() -> None:
    db = _make_db()
    try:
        db.record_download(
            url_hash="dup",
            url="https://example.com/a.jpg",
            blog_name="blog",
            post_id="1",
            filename="a.jpg",
            byte_count=100,
            status="success",
        )
        db.record_download(
            url_hash="dup",
            url="https://example.com/a.jpg",
            blog_name="blog",
            post_id="1",
            filename="a.jpg",
            byte_count=200,
            status="success",
        )
        db.commit()
        rows = db.execute("SELECT bytes FROM downloads WHERE url_hash = 'dup'").fetchall()
        assert len(rows) == 1
        assert rows[0][0] == 200
    finally:
        db.close()


# ---------------------------------------------------------------------------
# mark_post_complete / is_post_complete
# ---------------------------------------------------------------------------


def test_mark_post_complete_and_is_post_complete() -> None:
    db = _make_db()
    try:
        assert db.is_post_complete("post-1") is False
        db.mark_post_complete("post-1", "myblog")
        db.commit()
        assert db.is_post_complete("post-1") is True
    finally:
        db.close()


def test_is_post_complete_returns_false_for_missing_post() -> None:
    db = _make_db()
    try:
        assert db.is_post_complete("no-such-post") is False
    finally:
        db.close()


# ---------------------------------------------------------------------------
# On-disk WAL mode test
# ---------------------------------------------------------------------------


def test_on_disk_database_uses_wal_mode(tmp_path: Path) -> None:
    db_path = str(tmp_path / "test.db")
    db = StateDb(db_path)
    try:
        row = db.execute("PRAGMA journal_mode").fetchone()
        assert row is not None
        assert row[0] == "wal"
    finally:
        db.close()
