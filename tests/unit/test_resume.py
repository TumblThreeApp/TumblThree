"""Tests for tumbl4.core.state.resume — resume cursor persistence."""

from __future__ import annotations

from tumbl4.core.state.db import StateDb
from tumbl4.core.state.resume import load_cursor, save_cursor

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _make_db() -> StateDb:
    return StateDb(":memory:")


# ---------------------------------------------------------------------------
# load_cursor tests
# ---------------------------------------------------------------------------


def test_load_cursor_returns_0_for_new_blog() -> None:
    db = _make_db()
    try:
        assert load_cursor(db, "newblog") == 0
    finally:
        db.close()


def test_load_cursor_returns_0_for_unknown_crawler_type() -> None:
    db = _make_db()
    try:
        save_cursor(db, "myblog", 42, crawler_type="public")
        assert load_cursor(db, "myblog", crawler_type="private") == 0
    finally:
        db.close()


# ---------------------------------------------------------------------------
# save_cursor / load_cursor round-trip
# ---------------------------------------------------------------------------


def test_save_and_load_cursor() -> None:
    db = _make_db()
    try:
        save_cursor(db, "myblog", 12345)
        assert load_cursor(db, "myblog") == 12345
    finally:
        db.close()


def test_update_existing_cursor() -> None:
    db = _make_db()
    try:
        save_cursor(db, "myblog", 100)
        save_cursor(db, "myblog", 200)
        assert load_cursor(db, "myblog") == 200
    finally:
        db.close()


def test_separate_blogs_have_separate_cursors() -> None:
    db = _make_db()
    try:
        save_cursor(db, "blog-a", 111)
        save_cursor(db, "blog-b", 999)
        assert load_cursor(db, "blog-a") == 111
        assert load_cursor(db, "blog-b") == 999
    finally:
        db.close()


def test_separate_crawler_types_have_separate_cursors() -> None:
    db = _make_db()
    try:
        save_cursor(db, "myblog", 10, crawler_type="public")
        save_cursor(db, "myblog", 20, crawler_type="private")
        assert load_cursor(db, "myblog", crawler_type="public") == 10
        assert load_cursor(db, "myblog", crawler_type="private") == 20
    finally:
        db.close()


# ---------------------------------------------------------------------------
# last_complete_crawl timestamp
# ---------------------------------------------------------------------------


def test_save_cursor_records_last_complete_crawl() -> None:
    db = _make_db()
    try:
        save_cursor(db, "myblog", 42)
        row = db.execute(
            "SELECT last_complete_crawl FROM crawl_state WHERE blog_name = ?", ("myblog",)
        ).fetchone()
        assert row is not None
        assert row[0] is not None
        # Should be a valid ISO format timestamp string
        assert "T" in row[0]
    finally:
        db.close()


def test_update_cursor_updates_last_complete_crawl() -> None:
    db = _make_db()
    try:
        save_cursor(db, "myblog", 1)
        first_row = db.execute(
            "SELECT last_complete_crawl FROM crawl_state WHERE blog_name = ?", ("myblog",)
        ).fetchone()
        assert first_row is not None
        first_ts = first_row[0]

        save_cursor(db, "myblog", 2)
        second_row = db.execute(
            "SELECT last_complete_crawl FROM crawl_state WHERE blog_name = ?", ("myblog",)
        ).fetchone()
        assert second_row is not None
        second_ts = second_row[0]

        # Both timestamps should be valid ISO strings; second >= first
        assert second_ts >= first_ts
    finally:
        db.close()
