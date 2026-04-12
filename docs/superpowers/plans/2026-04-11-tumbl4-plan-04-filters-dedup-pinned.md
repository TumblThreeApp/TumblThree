# tumbl4 Plan 4: Filters + Cross-Blog Dedup + Pinned Posts

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Filter posts by reblog status, tags, and timespan. Deduplicate media across multiple blogs via a shared SQLite index. Correctly handle pinned posts so the resume cursor is never poisoned by a stale pinned post ID.

**Architecture:** Three pure-function filter modules sit between the crawler output and the media-queue enqueue step. Each filter is `(IntermediateDict, config) -> bool` -- keep or drop. The cross-blog dedup layer is a shared SQLite database (`$XDG_DATA_HOME/tumbl4/dedup.db`) consulted after per-blog dedup; it is URL-keyed (SHA-256) and tracks which blog first downloaded each media file. The pinned post resolver does an upfront HTML fetch of the blog's front page, extracts `___INITIAL_STATE___` to identify the pinned post ID, and excludes it from `highest_post_id` computation. Two new subcommands (`tumbl4 list` and `tumbl4 status <blog>`) surface state to the user.

**Tech Stack:** Python 3.12+, httpx (async HTTP), SQLite (state), Rich (output), Typer (CLI). Filters use no external dependencies -- they are pure functions over data already in memory.

**Builds on Plans 1-3:** `IntermediateDict` and `MediaEntry` in `core/parse/intermediate.py`, `StateDb` in `core/state/db.py`, `TumblrHttpClient` in `core/crawl/http_client.py`, `run_crawl` in `core/orchestrator.py`, `download` command in `cli/commands/download.py`, `html_scrape.py` in `core/parse/html_scrape.py` (Plan 3), `data_dir()`/`dedup_db()` in `_internal/paths.py`.

**Plans in this series:**

| # | Plan | Deliverable |
|---|---|---|
| 1 | Foundation (shipped) | `tumbl4 --version`; tooling + CI green |
| 2 | MVP public blog photo crawl (shipped) | `tumbl4 download <blog>` downloads photos, resumable |
| 3 | All post types + sidecars + templates (shipped) | Every post type; configurable filename templates |
| **4** | **Filters + dedup + pinned posts (this plan)** | **Tag/timespan filters; cross-blog dedup; pinned-post fix** |
| 5 | Auth + hidden blog crawler | `tumbl4 login` + hidden/dashboard blog downloads |
| 6 | Security hardening + release | Redirect safety, SSRF guards, signal handling, SLSA release |

**Spec references:**
- Design spec: `docs/superpowers/specs/2026-04-11-tumbl4-macos-cli-port-design.md` (sections 5.8, 5.10, 5.16, 7.2)
- Plan boundaries: `docs/superpowers/specs/2026-04-11-tumbl4-plan-boundaries.md`

---

## File Structure (Plan 4 additions)

New files are marked with `+`. Modified files are marked with `~`.

```
src/tumbl4/
├── __init__.py                              # (unchanged)
├── cli/
│   ├── app.py                            ~  # register list + status subcommands
│   ├── commands/
│   │   ├── __init__.py                   ~  # (unchanged)
│   │   ├── download.py                   ~  # wire filter + dedup flags
│   │   ├── list_blogs.py                 +  # tumbl4 list
│   │   └── status.py                     +  # tumbl4 status <blog>
│   └── output/
│       └── tables.py                     +  # Rich table formatters for list/status
├── core/
│   ├── __init__.py                       ~  # re-export filter_post
│   ├── orchestrator.py                   ~  # integrate filters + cross-blog dedup
│   ├── crawl/
│   │   ├── pinned_resolver.py            +  # HTML fetch + ___INITIAL_STATE___ pinned ID
│   │   └── tumblr_blog.py               ~  # skip pinned post in highest_id calc
│   ├── filter/
│   │   ├── __init__.py                   +  # FilterConfig + apply_filters()
│   │   ├── reblog.py                     +  # originals-only / reblogs-only
│   │   ├── tag.py                        +  # --tag / --exclude-tag
│   │   └── timespan.py                   +  # --since / --until
│   └── state/
│       └── dedup.py                      +  # cross-blog dedup SQLite layer
├── models/
│   └── settings.py                       ~  # add FilterSettings nested model
tests/
├── conftest.py                           ~  # add filter + dedup fixtures
├── unit/
│   ├── test_filter_reblog.py             +
│   ├── test_filter_tag.py                +
│   ├── test_filter_timespan.py           +
│   ├── test_filter_integration.py        +
│   ├── test_dedup.py                     +
│   ├── test_pinned_resolver.py           +
│   ├── test_list_command.py              +
│   └── test_status_command.py            +
└── component/
    └── test_filter_pipeline.py           +  # filters + dedup + orchestrator end-to-end
```

---

## Task 1: Reblog filter — pure function with table-driven tests

**Files:**
- Create: `src/tumbl4/core/filter/__init__.py`
- Create: `src/tumbl4/core/filter/reblog.py`
- Create: `tests/unit/test_filter_reblog.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_filter_reblog.py`:

```python
"""Table-driven tests for the reblog filter.

Each row in the test matrix is (is_reblog, include_originals, include_reblogs) -> expected.
See spec section 5.16.
"""

import pytest

from tumbl4.core.filter.reblog import filter_reblog


class TestFilterReblog:
    """Table-driven: every combination of is_reblog x config."""

    @pytest.mark.parametrize(
        "is_reblog, include_originals, include_reblogs, expected",
        [
            # Both True -> keep everything (no-op)
            (False, True, True, True),
            (True, True, True, True),
            # Originals only
            (False, True, False, True),
            (True, True, False, False),
            # Reblogs only
            (False, False, True, False),
            (True, False, True, True),
            # Both False is a ConfigError and handled at config load time,
            # but the filter itself should return False for safety.
            (False, False, False, False),
            (True, False, False, False),
        ],
        ids=[
            "original-both-true",
            "reblog-both-true",
            "original-originals-only",
            "reblog-originals-only",
            "original-reblogs-only",
            "reblog-reblogs-only",
            "original-both-false",
            "reblog-both-false",
        ],
    )
    def test_filter_matrix(
        self,
        is_reblog: bool,
        include_originals: bool,
        include_reblogs: bool,
        expected: bool,
    ) -> None:
        result = filter_reblog(
            is_reblog=is_reblog,
            include_originals=include_originals,
            include_reblogs=include_reblogs,
        )
        assert result is expected

    def test_default_keeps_everything(self) -> None:
        """Default config (both True) keeps originals and reblogs."""
        assert filter_reblog(is_reblog=False) is True
        assert filter_reblog(is_reblog=True) is True
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_filter_reblog.py -v`
Expected: FAIL -- `ModuleNotFoundError: No module named 'tumbl4.core.filter'`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/filter/__init__.py`:

```python
"""Post filter pipeline — pure functions applied between crawl and download.

Each filter is a predicate: True to keep the post, False to drop it.
Filters are composed via apply_filters() which short-circuits on first drop.

See spec section 5.16.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime

from tumbl4.core.filter.reblog import filter_reblog
from tumbl4.core.filter.tag import filter_tag
from tumbl4.core.filter.timespan import filter_timespan
from tumbl4.core.parse.intermediate import IntermediateDict


@dataclass(frozen=True)
class FilterConfig:
    """Immutable configuration for the post filter pipeline."""

    include_originals: bool = True
    include_reblogs: bool = True
    include_tags: list[str] = field(default_factory=list)
    exclude_tags: list[str] = field(default_factory=list)
    since: datetime | None = None
    until: datetime | None = None


def apply_filters(post: IntermediateDict, config: FilterConfig) -> bool:
    """Apply all configured filters to a post. Returns True to keep, False to drop.

    Short-circuits on the first filter that returns False.
    """
    if not filter_reblog(
        is_reblog=post["is_reblog"],
        include_originals=config.include_originals,
        include_reblogs=config.include_reblogs,
    ):
        return False

    if not filter_tag(
        post_tags=post["tags"],
        include_tags=config.include_tags,
        exclude_tags=config.exclude_tags,
    ):
        return False

    if not filter_timespan(
        timestamp_utc=post["timestamp_utc"],
        since=config.since,
        until=config.until,
    ):
        return False

    return True
```

Write file `src/tumbl4/core/filter/reblog.py`:

```python
"""Reblog filter — keep or drop posts based on original vs. reblog status.

Pure function. No side effects, no I/O.

See spec section 5.16:
    - include_reblogs=True, include_originals=True -> keep all (no-op)
    - include_reblogs=False -> drop reblogs (originals only)
    - include_originals=False -> drop originals (reblogs only)
    - Both False is a ConfigError at load time; filter returns False as safety net
"""

from __future__ import annotations


def filter_reblog(
    *,
    is_reblog: bool,
    include_originals: bool = True,
    include_reblogs: bool = True,
) -> bool:
    """Return True to keep the post, False to drop it."""
    if is_reblog:
        return include_reblogs
    return include_originals
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_filter_reblog.py -v`
Expected: 10 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/filter/__init__.py src/tumbl4/core/filter/reblog.py tests/unit/test_filter_reblog.py
git commit -m "feat(filter): add reblog filter with table-driven tests

Pure function: keep/drop based on is_reblog x include_originals x
include_reblogs. FilterConfig and apply_filters() pipeline in
filter/__init__.py. See spec section 5.16.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Tag filter — case-insensitive, NFC-normalised, OR semantics

**Files:**
- Create: `src/tumbl4/core/filter/tag.py`
- Create: `tests/unit/test_filter_tag.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_filter_tag.py`:

```python
"""Table-driven tests for the tag filter.

Spec section 5.16:
    - include_tags: post kept if ANY tag matches (OR); empty = no include filter
    - exclude_tags: post dropped if ANY tag matches (OR); empty = no exclude filter
    - Include and exclude applied independently; post must pass BOTH
    - Matching is case-insensitive and NFC-normalized
"""

import pytest

from tumbl4.core.filter.tag import filter_tag


class TestFilterTag:
    """Table-driven: every meaningful combination of post tags x config."""

    @pytest.mark.parametrize(
        "post_tags, include_tags, exclude_tags, expected",
        [
            # No filters at all -> keep
            (["art", "wip"], [], [], True),
            ([], [], [], True),
            # Include filter only
            (["art", "wip"], ["art"], [], True),
            (["art", "wip"], ["photography"], [], False),
            (["art", "wip"], ["art", "photography"], [], True),  # OR semantics
            ([], ["art"], [], False),  # no tags, can't match include
            # Exclude filter only
            (["art", "wip"], [], ["nsfw"], True),
            (["art", "nsfw"], [], ["nsfw"], False),
            (["art", "nsfw", "wip"], [], ["nsfw", "gore"], False),  # OR semantics
            ([], [], ["nsfw"], True),  # no tags, nothing to exclude
            # Both include and exclude
            (["art", "wip"], ["art"], ["nsfw"], True),  # passes both
            (["art", "nsfw"], ["art"], ["nsfw"], False),  # passes include, fails exclude
            (["photography"], ["art"], ["nsfw"], False),  # fails include
            (["art", "wip"], ["art"], ["wip"], False),  # passes include, fails exclude
        ],
        ids=[
            "no-filters-with-tags",
            "no-filters-no-tags",
            "include-match",
            "include-no-match",
            "include-or-semantics",
            "include-no-tags",
            "exclude-no-match",
            "exclude-match",
            "exclude-or-semantics",
            "exclude-no-tags",
            "both-pass",
            "include-pass-exclude-fail",
            "include-fail",
            "include-pass-exclude-fail-overlap",
        ],
    )
    def test_filter_matrix(
        self,
        post_tags: list[str],
        include_tags: list[str],
        exclude_tags: list[str],
        expected: bool,
    ) -> None:
        result = filter_tag(
            post_tags=post_tags,
            include_tags=include_tags,
            exclude_tags=exclude_tags,
        )
        assert result is expected

    def test_case_insensitive_include(self) -> None:
        assert filter_tag(post_tags=["Art"], include_tags=["art"]) is True
        assert filter_tag(post_tags=["art"], include_tags=["ART"]) is True

    def test_case_insensitive_exclude(self) -> None:
        assert filter_tag(post_tags=["NSFW"], exclude_tags=["nsfw"]) is False
        assert filter_tag(post_tags=["nsfw"], exclude_tags=["NSFW"]) is False

    def test_nfc_normalization(self) -> None:
        """Combining accent (NFD) should match precomposed (NFC)."""
        # e + combining acute = NFC e-acute
        nfd_tag = "caf\u0065\u0301"  # NFD: e + combining acute accent
        nfc_tag = "caf\u00e9"  # NFC: precomposed e-acute
        assert filter_tag(post_tags=[nfd_tag], include_tags=[nfc_tag]) is True
        assert filter_tag(post_tags=[nfc_tag], include_tags=[nfd_tag]) is True

    def test_whitespace_in_tags(self) -> None:
        """Tags with leading/trailing whitespace should still match after strip."""
        assert filter_tag(post_tags=["  art  "], include_tags=["art"]) is True
        assert filter_tag(post_tags=["art"], include_tags=["  art  "]) is True
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_filter_tag.py -v`
Expected: FAIL -- `ModuleNotFoundError: No module named 'tumbl4.core.filter.tag'`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/filter/tag.py`:

```python
"""Tag filter — include/exclude posts based on their tag list.

Pure function. No side effects, no I/O.

See spec section 5.16:
    - include_tags: post kept if ANY post tag matches ANY include tag (OR)
    - exclude_tags: post dropped if ANY post tag matches ANY exclude tag (OR)
    - Both applied independently; post must satisfy BOTH
    - Matching is case-insensitive and NFC-normalized on both sides
    - Empty list = no filter for that direction
"""

from __future__ import annotations

import unicodedata


def _normalize(tag: str) -> str:
    """NFC-normalize, lowercase, and strip a tag for comparison."""
    return unicodedata.normalize("NFC", tag.strip().lower())


def filter_tag(
    *,
    post_tags: list[str],
    include_tags: list[str] | None = None,
    exclude_tags: list[str] | None = None,
) -> bool:
    """Return True to keep the post, False to drop it.

    Args:
        post_tags: Tags on the post (from IntermediateDict).
        include_tags: If non-empty, post must have at least one matching tag.
        exclude_tags: If non-empty, post must NOT have any matching tag.
    """
    if include_tags is None:
        include_tags = []
    if exclude_tags is None:
        exclude_tags = []

    normalized_post = {_normalize(t) for t in post_tags}

    # Include check: if include_tags is non-empty, at least one must match
    if include_tags:
        normalized_include = {_normalize(t) for t in include_tags}
        if not normalized_post & normalized_include:
            return False

    # Exclude check: if any post tag matches an exclude tag, drop the post
    if exclude_tags:
        normalized_exclude = {_normalize(t) for t in exclude_tags}
        if normalized_post & normalized_exclude:
            return False

    return True
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_filter_tag.py -v`
Expected: 18 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/filter/tag.py tests/unit/test_filter_tag.py
git commit -m "feat(filter): add tag filter with case-insensitive NFC matching

OR semantics for both include and exclude. Post must satisfy both
directions independently. Table-driven tests cover all edge cases
including NFD/NFC normalization. See spec section 5.16.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Timespan filter — since/until on post creation date

**Files:**
- Create: `src/tumbl4/core/filter/timespan.py`
- Create: `tests/unit/test_filter_timespan.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_filter_timespan.py`:

```python
"""Table-driven tests for the timespan filter.

Spec section 5.16:
    - since: post kept if timestamp_utc >= since
    - until: post kept if timestamp_utc <= until
    - Filters on post creation date, not reblog date
    - Both None = no filter; both set = inclusive-inclusive range
"""

from datetime import UTC, datetime

import pytest

from tumbl4.core.filter.timespan import filter_timespan

# Test reference points
_2025_JAN = datetime(2025, 1, 15, tzinfo=UTC)
_2025_JUN = datetime(2025, 6, 15, tzinfo=UTC)
_2025_DEC = datetime(2025, 12, 15, tzinfo=UTC)
_2026_JAN = datetime(2026, 1, 15, tzinfo=UTC)
_2026_APR = datetime(2026, 4, 11, tzinfo=UTC)


class TestFilterTimespan:
    """Table-driven: every meaningful combination of timestamp x config."""

    @pytest.mark.parametrize(
        "timestamp_utc, since, until, expected",
        [
            # No filters -> keep everything
            ("2025-06-15T00:00:00+00:00", None, None, True),
            # Since only
            ("2026-01-15T00:00:00+00:00", _2025_JUN, None, True),
            ("2025-06-15T00:00:00+00:00", _2025_JUN, None, True),  # exact match = keep
            ("2025-01-15T00:00:00+00:00", _2025_JUN, None, False),  # before since
            # Until only
            ("2025-01-15T00:00:00+00:00", None, _2025_JUN, True),
            ("2025-06-15T00:00:00+00:00", None, _2025_JUN, True),  # exact match = keep
            ("2025-12-15T00:00:00+00:00", None, _2025_JUN, False),  # after until
            # Both since and until (inclusive range)
            ("2025-06-15T00:00:00+00:00", _2025_JAN, _2025_DEC, True),  # inside range
            ("2025-01-15T00:00:00+00:00", _2025_JAN, _2025_DEC, True),  # at since boundary
            ("2025-12-15T00:00:00+00:00", _2025_JAN, _2025_DEC, True),  # at until boundary
            ("2024-12-31T00:00:00+00:00", _2025_JAN, _2025_DEC, False),  # before range
            ("2026-01-15T00:00:00+00:00", _2025_JAN, _2025_DEC, False),  # after range
        ],
        ids=[
            "no-filter",
            "since-after",
            "since-exact",
            "since-before",
            "until-before",
            "until-exact",
            "until-after",
            "range-inside",
            "range-at-since",
            "range-at-until",
            "range-before",
            "range-after",
        ],
    )
    def test_filter_matrix(
        self,
        timestamp_utc: str,
        since: datetime | None,
        until: datetime | None,
        expected: bool,
    ) -> None:
        result = filter_timespan(
            timestamp_utc=timestamp_utc,
            since=since,
            until=until,
        )
        assert result is expected

    def test_handles_naive_iso_timestamp(self) -> None:
        """Timestamps without timezone info should be treated as UTC."""
        result = filter_timespan(
            timestamp_utc="2025-06-15T00:00:00",
            since=_2025_JAN,
            until=_2025_DEC,
        )
        assert result is True

    def test_handles_unix_epoch_format(self) -> None:
        """The epoch timestamp from our parser (1970-01-01) should be filtered correctly."""
        result = filter_timespan(
            timestamp_utc="1970-01-01T00:00:00+00:00",
            since=_2025_JAN,
        )
        assert result is False

    def test_handles_malformed_timestamp(self) -> None:
        """Malformed timestamps should not crash -- drop the post."""
        result = filter_timespan(
            timestamp_utc="not-a-date",
            since=_2025_JAN,
        )
        assert result is False

    def test_malformed_timestamp_no_filter(self) -> None:
        """Malformed timestamp with no filters should still keep the post."""
        result = filter_timespan(
            timestamp_utc="not-a-date",
            since=None,
            until=None,
        )
        assert result is True
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_filter_timespan.py -v`
Expected: FAIL -- `ModuleNotFoundError: No module named 'tumbl4.core.filter.timespan'`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/filter/timespan.py`:

```python
"""Timespan filter — keep posts within a date range.

Pure function. No side effects, no I/O.

See spec section 5.16:
    - since: post kept if timestamp_utc >= since
    - until: post kept if timestamp_utc <= until
    - Filters on post creation date (timestamp_utc), not reblog date
    - Both None = no filter (keep all)
    - Both set = inclusive-inclusive range
"""

from __future__ import annotations

from datetime import UTC, datetime

from tumbl4._internal.logging import get_logger

logger = get_logger(__name__)


def _parse_timestamp(raw: str) -> datetime | None:
    """Parse an ISO8601 timestamp string to a timezone-aware datetime.

    Returns None if the timestamp cannot be parsed.
    """
    try:
        dt = datetime.fromisoformat(raw)
        # If the timestamp is naive (no timezone info), treat as UTC
        if dt.tzinfo is None:
            dt = dt.replace(tzinfo=UTC)
        return dt
    except (ValueError, TypeError):
        return None


def filter_timespan(
    *,
    timestamp_utc: str,
    since: datetime | None = None,
    until: datetime | None = None,
) -> bool:
    """Return True to keep the post, False to drop it.

    Args:
        timestamp_utc: ISO8601 timestamp string from IntermediateDict.
        since: If set, drop posts before this datetime (inclusive boundary).
        until: If set, drop posts after this datetime (inclusive boundary).
    """
    # No filters active -> keep everything
    if since is None and until is None:
        return True

    post_dt = _parse_timestamp(timestamp_utc)
    if post_dt is None:
        logger.warning("unparseable timestamp, dropping post", extra={"raw": timestamp_utc})
        return False

    if since is not None and post_dt < since:
        return False

    if until is not None and post_dt > until:
        return False

    return True
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_filter_timespan.py -v`
Expected: 16 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/filter/timespan.py tests/unit/test_filter_timespan.py
git commit -m "feat(filter): add timespan filter with since/until range

Inclusive-inclusive datetime range on post creation date. Naive
timestamps treated as UTC. Malformed timestamps dropped when filters
are active, kept when no filters configured. See spec section 5.16.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: Filter integration — apply_filters and FilterConfig validation

**Files:**
- Modify: `src/tumbl4/core/filter/__init__.py` (already created in Task 1)
- Modify: `src/tumbl4/models/settings.py`
- Create: `tests/unit/test_filter_integration.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_filter_integration.py`:

```python
"""Integration tests for the filter pipeline — apply_filters and FilterConfig.

Verifies that FilterConfig wiring, validation, and the composed pipeline
work end-to-end on IntermediateDict inputs.
"""

from datetime import UTC, datetime

import pytest

from tumbl4.core.errors import ConfigError
from tumbl4.core.filter import FilterConfig, apply_filters, validate_filter_config
from tumbl4.core.parse.intermediate import IntermediateDict, MediaEntry


def _make_post(**overrides: object) -> IntermediateDict:
    """Build a minimal IntermediateDict with sensible defaults."""
    defaults: IntermediateDict = {
        "schema_version": 1,
        "source_format": "api",
        "post_id": "12345",
        "blog_name": "testblog",
        "post_url": "https://testblog.tumblr.com/post/12345",
        "post_type": "photo",
        "timestamp_utc": "2025-06-15T12:00:00+00:00",
        "tags": ["art", "wip"],
        "is_reblog": False,
        "reblog_source": None,
        "title": None,
        "body_text": "A test photo",
        "body_html": "<p>A test photo</p>",
        "media": [
            MediaEntry(
                kind="photo",
                url="https://64.media.tumblr.com/abc123/photo.jpg",
                width=1280,
                height=960,
                mime_type=None,
                alt_text=None,
                duration_ms=None,
            )
        ],
        "raw_content_blocks": None,
    }
    return {**defaults, **overrides}  # type: ignore[return-value]


class TestApplyFilters:
    def test_default_config_keeps_all(self) -> None:
        config = FilterConfig()
        assert apply_filters(_make_post(), config) is True
        assert apply_filters(_make_post(is_reblog=True), config) is True

    def test_originals_only(self) -> None:
        config = FilterConfig(include_reblogs=False)
        assert apply_filters(_make_post(is_reblog=False), config) is True
        assert apply_filters(_make_post(is_reblog=True), config) is False

    def test_reblogs_only(self) -> None:
        config = FilterConfig(include_originals=False)
        assert apply_filters(_make_post(is_reblog=True), config) is True
        assert apply_filters(_make_post(is_reblog=False), config) is False

    def test_tag_filter_applied(self) -> None:
        config = FilterConfig(include_tags=["photography"])
        assert apply_filters(_make_post(tags=["photography", "sunset"]), config) is True
        assert apply_filters(_make_post(tags=["art", "wip"]), config) is False

    def test_exclude_tag_filter_applied(self) -> None:
        config = FilterConfig(exclude_tags=["nsfw"])
        assert apply_filters(_make_post(tags=["art", "nsfw"]), config) is False
        assert apply_filters(_make_post(tags=["art", "wip"]), config) is True

    def test_timespan_filter_applied(self) -> None:
        since = datetime(2025, 6, 1, tzinfo=UTC)
        until = datetime(2025, 6, 30, tzinfo=UTC)
        config = FilterConfig(since=since, until=until)
        assert apply_filters(_make_post(timestamp_utc="2025-06-15T12:00:00+00:00"), config) is True
        assert apply_filters(_make_post(timestamp_utc="2025-05-01T12:00:00+00:00"), config) is False
        assert apply_filters(_make_post(timestamp_utc="2025-07-15T12:00:00+00:00"), config) is False

    def test_all_filters_combined(self) -> None:
        """Post must pass ALL filters."""
        config = FilterConfig(
            include_reblogs=False,
            include_tags=["art"],
            since=datetime(2025, 1, 1, tzinfo=UTC),
        )
        # Original, has art tag, after since -> keep
        assert apply_filters(
            _make_post(is_reblog=False, tags=["art"], timestamp_utc="2025-06-15T00:00:00+00:00"),
            config,
        ) is True
        # Reblog -> drop (reblog filter)
        assert apply_filters(
            _make_post(is_reblog=True, tags=["art"], timestamp_utc="2025-06-15T00:00:00+00:00"),
            config,
        ) is False
        # No art tag -> drop (tag filter)
        assert apply_filters(
            _make_post(is_reblog=False, tags=["photography"], timestamp_utc="2025-06-15T00:00:00+00:00"),
            config,
        ) is False
        # Before since -> drop (timespan filter)
        assert apply_filters(
            _make_post(is_reblog=False, tags=["art"], timestamp_utc="2024-06-15T00:00:00+00:00"),
            config,
        ) is False

    def test_short_circuit_on_reblog(self) -> None:
        """Reblog filter runs first -- tag/timespan never evaluated."""
        config = FilterConfig(
            include_reblogs=False,
            include_tags=["nonexistent"],
            since=datetime(2099, 1, 1, tzinfo=UTC),
        )
        # Reblog should be dropped by reblog filter, not tag or timespan
        assert apply_filters(_make_post(is_reblog=True), config) is False


class TestValidateFilterConfig:
    def test_valid_config_passes(self) -> None:
        validate_filter_config(FilterConfig())
        validate_filter_config(FilterConfig(include_reblogs=False))
        validate_filter_config(FilterConfig(include_originals=False))

    def test_both_false_raises_config_error(self) -> None:
        with pytest.raises(ConfigError, match="both originals and reblogs"):
            validate_filter_config(FilterConfig(include_originals=False, include_reblogs=False))

    def test_since_after_until_raises_config_error(self) -> None:
        with pytest.raises(ConfigError, match="--since.*after.*--until"):
            validate_filter_config(FilterConfig(
                since=datetime(2026, 1, 1, tzinfo=UTC),
                until=datetime(2025, 1, 1, tzinfo=UTC),
            ))
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_filter_integration.py -v`
Expected: FAIL -- `ImportError: cannot import name 'validate_filter_config'`

- [ ] **Step 3: Write the implementation**

Update `src/tumbl4/core/filter/__init__.py` -- add the `validate_filter_config` function after the existing `apply_filters`:

```python
"""Post filter pipeline — pure functions applied between crawl and download.

Each filter is a predicate: True to keep the post, False to drop it.
Filters are composed via apply_filters() which short-circuits on first drop.

See spec section 5.16.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime

from tumbl4.core.errors import ConfigError
from tumbl4.core.filter.reblog import filter_reblog
from tumbl4.core.filter.tag import filter_tag
from tumbl4.core.filter.timespan import filter_timespan
from tumbl4.core.parse.intermediate import IntermediateDict


@dataclass(frozen=True)
class FilterConfig:
    """Immutable configuration for the post filter pipeline."""

    include_originals: bool = True
    include_reblogs: bool = True
    include_tags: list[str] = field(default_factory=list)
    exclude_tags: list[str] = field(default_factory=list)
    since: datetime | None = None
    until: datetime | None = None


def apply_filters(post: IntermediateDict, config: FilterConfig) -> bool:
    """Apply all configured filters to a post. Returns True to keep, False to drop.

    Short-circuits on the first filter that returns False.
    """
    if not filter_reblog(
        is_reblog=post["is_reblog"],
        include_originals=config.include_originals,
        include_reblogs=config.include_reblogs,
    ):
        return False

    if not filter_tag(
        post_tags=post["tags"],
        include_tags=config.include_tags,
        exclude_tags=config.exclude_tags,
    ):
        return False

    if not filter_timespan(
        timestamp_utc=post["timestamp_utc"],
        since=config.since,
        until=config.until,
    ):
        return False

    return True


def validate_filter_config(config: FilterConfig) -> None:
    """Validate filter configuration at load time. Raises ConfigError on invalid combos.

    Checked at CLI parse time, before any crawl begins.
    """
    if not config.include_originals and not config.include_reblogs:
        raise ConfigError(
            "Cannot exclude both originals and reblogs — "
            "this would drop every post"
        )

    if config.since is not None and config.until is not None:
        if config.since > config.until:
            raise ConfigError(
                f"--since ({config.since.isoformat()}) is after "
                f"--until ({config.until.isoformat()}) — empty date range"
            )
```

Add `FilterSettings` to `src/tumbl4/models/settings.py`. After the existing `HttpSettings` class, add:

```python
class FilterSettings(BaseModel):
    """Filter configuration, populated from CLI flags."""

    include_originals: bool = True
    include_reblogs: bool = True
    include_tags: list[str] = Field(default_factory=list)
    exclude_tags: list[str] = Field(default_factory=list)
    since: str | None = None
    until: str | None = None
```

And add to the `Settings` class, after the `http` field:

```python
    filter: FilterSettings = Field(default_factory=FilterSettings)
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_filter_integration.py -v`
Expected: 12 passed

- [ ] **Step 5: Run all existing tests to check for regressions**

Run: `uv run pytest -v`
Expected: all tests pass

- [ ] **Step 6: Commit**

```bash
git add src/tumbl4/core/filter/__init__.py src/tumbl4/models/settings.py tests/unit/test_filter_integration.py
git commit -m "feat(filter): add validate_filter_config and FilterSettings

Config validation at load time: both-false reblog check, since>until
range check. FilterSettings in models/settings.py for CLI wiring.
Integration tests verify composed pipeline on IntermediateDict.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: Cross-blog dedup SQLite layer

**Files:**
- Create: `src/tumbl4/core/state/dedup.py`
- Create: `tests/unit/test_dedup.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_dedup.py`:

```python
"""Tests for the cross-blog dedup SQLite layer.

Spec section 5.10:
    - Shared SQLite at $XDG_DATA_HOME/tumbl4/dedup.db
    - URL-keyed (SHA-256 of initial-request URL)
    - Tracks first_seen_blog for provenance
    - --no-dedup bypasses cross-blog only; per-blog always active
    - Lookup is O(log n) via indexed column, not in-memory set
"""

import hashlib
from pathlib import Path

import pytest

from tumbl4.core.state.dedup import DedupDb


def _url_hash(url: str) -> str:
    return hashlib.sha256(url.encode()).hexdigest()


class TestDedupDb:
    def test_creates_tables_on_init(self, tmp_path: Path) -> None:
        db = DedupDb(str(tmp_path / "dedup.db"))
        tables = db.execute(
            "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name"
        ).fetchall()
        table_names = [row[0] for row in tables]
        assert "cross_blog_urls" in table_names
        db.close()

    def test_wal_mode_enabled(self, tmp_path: Path) -> None:
        db = DedupDb(str(tmp_path / "dedup.db"))
        mode = db.execute("PRAGMA journal_mode").fetchone()[0]
        assert mode == "wal"
        db.close()

    def test_is_known_returns_false_for_new_url(self, tmp_path: Path) -> None:
        db = DedupDb(str(tmp_path / "dedup.db"))
        assert db.is_known(_url_hash("https://example.com/photo1.jpg")) is False
        db.close()

    def test_record_and_lookup(self, tmp_path: Path) -> None:
        db = DedupDb(str(tmp_path / "dedup.db"))
        url = "https://64.media.tumblr.com/abc/photo.jpg"
        url_hash = _url_hash(url)
        db.record(url_hash=url_hash, url=url, blog_name="blog_a")
        assert db.is_known(url_hash) is True
        db.close()

    def test_first_seen_blog_recorded(self, tmp_path: Path) -> None:
        db = DedupDb(str(tmp_path / "dedup.db"))
        url = "https://64.media.tumblr.com/abc/photo.jpg"
        url_hash = _url_hash(url)
        db.record(url_hash=url_hash, url=url, blog_name="blog_a")
        info = db.get_info(url_hash)
        assert info is not None
        assert info["first_seen_blog"] == "blog_a"
        db.close()

    def test_second_blog_does_not_overwrite_first_seen(self, tmp_path: Path) -> None:
        db = DedupDb(str(tmp_path / "dedup.db"))
        url = "https://64.media.tumblr.com/abc/photo.jpg"
        url_hash = _url_hash(url)
        db.record(url_hash=url_hash, url=url, blog_name="blog_a")
        db.record(url_hash=url_hash, url=url, blog_name="blog_b")
        info = db.get_info(url_hash)
        assert info is not None
        assert info["first_seen_blog"] == "blog_a"
        db.close()

    def test_separate_urls_tracked_independently(self, tmp_path: Path) -> None:
        db = DedupDb(str(tmp_path / "dedup.db"))
        url1 = "https://64.media.tumblr.com/abc/photo1.jpg"
        url2 = "https://64.media.tumblr.com/def/photo2.jpg"
        db.record(url_hash=_url_hash(url1), url=url1, blog_name="blog_a")
        assert db.is_known(_url_hash(url1)) is True
        assert db.is_known(_url_hash(url2)) is False
        db.close()

    def test_in_memory_mode(self) -> None:
        """DedupDb should work with :memory: for testing."""
        db = DedupDb(":memory:")
        url = "https://example.com/test.jpg"
        url_hash = _url_hash(url)
        assert db.is_known(url_hash) is False
        db.record(url_hash=url_hash, url=url, blog_name="test")
        assert db.is_known(url_hash) is True
        db.close()

    def test_count(self, tmp_path: Path) -> None:
        db = DedupDb(str(tmp_path / "dedup.db"))
        assert db.count() == 0
        db.record(url_hash=_url_hash("url1"), url="url1", blog_name="a")
        db.record(url_hash=_url_hash("url2"), url="url2", blog_name="b")
        assert db.count() == 2
        db.close()

    def test_persistence_across_reopen(self, tmp_path: Path) -> None:
        db_path = str(tmp_path / "dedup.db")
        url = "https://example.com/persistent.jpg"
        url_hash = _url_hash(url)

        db = DedupDb(db_path)
        db.record(url_hash=url_hash, url=url, blog_name="blog_a")
        db.close()

        db2 = DedupDb(db_path)
        assert db2.is_known(url_hash) is True
        db2.close()

    def test_user_version_set(self, tmp_path: Path) -> None:
        db = DedupDb(str(tmp_path / "dedup.db"))
        version = db.execute("PRAGMA user_version").fetchone()[0]
        assert version == 1
        db.close()
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_dedup.py -v`
Expected: FAIL -- `ModuleNotFoundError: No module named 'tumbl4.core.state.dedup'`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/state/dedup.py`:

```python
"""Cross-blog dedup — shared SQLite database tracking URLs seen across all blogs.

Stored at $XDG_DATA_HOME/tumbl4/dedup.db (see _internal/paths.py:dedup_db()).
Key = SHA-256 of initial-request URL (same as per-blog dedup).
Tracks first_seen_blog for provenance/debugging.

Spec section 5.10:
    - Not loaded into a Python set at startup — queries go to SQLite directly
    - URL column is indexed for O(log n) lookup
    - --no-dedup bypasses cross-blog dedup only; per-blog dedup always active
    - Consulted between filter and media-queue enqueue
"""

from __future__ import annotations

import sqlite3
from datetime import UTC, datetime

from tumbl4._internal.logging import get_logger

logger = get_logger(__name__)

_SCHEMA_VERSION = 1

_SCHEMA_SQL = """\
CREATE TABLE IF NOT EXISTS cross_blog_urls (
    url_hash        TEXT PRIMARY KEY,
    url             TEXT NOT NULL,
    first_seen_blog TEXT NOT NULL,
    created_at      TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_cross_blog_urls_hash
    ON cross_blog_urls(url_hash);
"""


class DedupDb:
    """Shared cross-blog dedup database.

    Separate from the per-blog StateDb. Opened once per tumbl4 session,
    shared across all blogs being crawled in that session.
    """

    def __init__(self, path: str) -> None:
        self._conn = sqlite3.connect(path)
        self._conn.row_factory = sqlite3.Row
        self._setup()

    def _setup(self) -> None:
        try:
            self._conn.execute("PRAGMA journal_mode=WAL")
        except sqlite3.OperationalError:
            pass  # :memory: databases don't support WAL
        self._conn.execute("PRAGMA synchronous=NORMAL")
        self._conn.executescript(_SCHEMA_SQL)
        current_version = self._conn.execute("PRAGMA user_version").fetchone()[0]
        if current_version < _SCHEMA_VERSION:
            self._conn.execute(f"PRAGMA user_version = {_SCHEMA_VERSION}")
        self._conn.commit()

    def execute(
        self, sql: str, params: tuple[object, ...] = ()
    ) -> sqlite3.Cursor:
        """Execute a parameterised SQL query."""
        return self._conn.execute(sql, params)

    def is_known(self, url_hash: str) -> bool:
        """Check if a URL hash has been seen in any blog."""
        row = self._conn.execute(
            "SELECT 1 FROM cross_blog_urls WHERE url_hash = ?", (url_hash,)
        ).fetchone()
        return row is not None

    def get_info(self, url_hash: str) -> dict[str, str] | None:
        """Get provenance info for a URL hash. Returns None if not found."""
        row = self._conn.execute(
            "SELECT url, first_seen_blog, created_at FROM cross_blog_urls WHERE url_hash = ?",
            (url_hash,),
        ).fetchone()
        if row is None:
            return None
        return {
            "url": row[0],
            "first_seen_blog": row[1],
            "created_at": row[2],
        }

    def record(self, *, url_hash: str, url: str, blog_name: str) -> None:
        """Record a URL as seen. First-seen blog is preserved on conflict (INSERT OR IGNORE)."""
        now = datetime.now(UTC).isoformat()
        self._conn.execute(
            "INSERT OR IGNORE INTO cross_blog_urls "
            "(url_hash, url, first_seen_blog, created_at) "
            "VALUES (?, ?, ?, ?)",
            (url_hash, url, blog_name, now),
        )
        self._conn.commit()

    def count(self) -> int:
        """Return the total number of URLs tracked across all blogs."""
        row = self._conn.execute("SELECT COUNT(*) FROM cross_blog_urls").fetchone()
        return int(row[0])

    def close(self) -> None:
        self._conn.close()
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_dedup.py -v`
Expected: 11 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/state/dedup.py tests/unit/test_dedup.py
git commit -m "feat(state): add cross-blog dedup SQLite layer

Shared database at dedup_db() path. URL-keyed (SHA-256), tracks
first_seen_blog provenance. INSERT OR IGNORE preserves original
discoverer. WAL mode, indexed lookups. See spec section 5.10.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: Pinned post resolver

**Files:**
- Create: `src/tumbl4/core/crawl/pinned_resolver.py`
- Create: `tests/unit/test_pinned_resolver.py`

- [ ] **Step 1: Write the failing test**

Write file `tests/unit/test_pinned_resolver.py`:

```python
"""Tests for the pinned post resolver.

Spec section 5.8:
    - Fetch blog HTML front page
    - Extract ___INITIAL_STATE___ JSON
    - Identify pinned post ID (if any)
    - Pinned post excluded from highest_post_id computation but still downloaded
"""

import json

import httpx
import pytest
import respx

from tumbl4.core.crawl.pinned_resolver import (
    extract_initial_state,
    find_pinned_post_id,
    resolve_pinned_post,
)


# Minimal ___INITIAL_STATE___ JSON with a pinned post
_INITIAL_STATE_WITH_PIN = {
    "PeeprRoute": {
        "blogData": {
            "testblog": {
                "pinnedPostId": "728394056000",
            }
        }
    }
}

# ___INITIAL_STATE___ JSON with no pinned post
_INITIAL_STATE_NO_PIN = {
    "PeeprRoute": {
        "blogData": {
            "testblog": {}
        }
    }
}

# Alternative shape: response-wrapped (no PeeprRoute)
_INITIAL_STATE_RESPONSE_SHAPE = {
    "response": {
        "blogData": {
            "testblog": {
                "pinnedPostId": "728394056000",
            }
        }
    }
}


def _wrap_html(state_json: dict) -> str:
    """Build a minimal HTML page with ___INITIAL_STATE___ embedded."""
    return (
        '<html><body><script>window["___INITIAL_STATE___"] = '
        + json.dumps(state_json)
        + ";</script></body></html>"
    )


class TestExtractInitialState:
    def test_extracts_from_html(self) -> None:
        html = _wrap_html(_INITIAL_STATE_WITH_PIN)
        result = extract_initial_state(html)
        assert result is not None
        assert "PeeprRoute" in result

    def test_returns_none_for_missing_state(self) -> None:
        html = "<html><body>No state here</body></html>"
        result = extract_initial_state(html)
        assert result is None

    def test_handles_single_line_format(self) -> None:
        html = 'window["___INITIAL_STATE___"] = {"key": "value"};'
        result = extract_initial_state(html)
        assert result is not None
        assert result["key"] == "value"

    def test_handles_multiline_format(self) -> None:
        html = (
            'window["___INITIAL_STATE___"] = {\n'
            '  "key": "value"\n'
            "};"
        )
        result = extract_initial_state(html)
        assert result is not None
        assert result["key"] == "value"

    def test_handles_assignment_without_window(self) -> None:
        """Some pages use ___INITIAL_STATE___ = ... instead of window[...]."""
        html = '___INITIAL_STATE___ = {"key": "value"};'
        result = extract_initial_state(html)
        assert result is not None
        assert result["key"] == "value"


class TestFindPinnedPostId:
    def test_peepr_route_shape(self) -> None:
        result = find_pinned_post_id(_INITIAL_STATE_WITH_PIN, "testblog")
        assert result == "728394056000"

    def test_response_shape(self) -> None:
        result = find_pinned_post_id(_INITIAL_STATE_RESPONSE_SHAPE, "testblog")
        assert result == "728394056000"

    def test_no_pinned_post(self) -> None:
        result = find_pinned_post_id(_INITIAL_STATE_NO_PIN, "testblog")
        assert result is None

    def test_missing_blog_data(self) -> None:
        result = find_pinned_post_id({"PeeprRoute": {}}, "testblog")
        assert result is None

    def test_wrong_blog_name(self) -> None:
        result = find_pinned_post_id(_INITIAL_STATE_WITH_PIN, "otherblog")
        assert result is None

    def test_empty_pinned_id(self) -> None:
        """pinnedPostId present but empty string -> None."""
        state = {"PeeprRoute": {"blogData": {"testblog": {"pinnedPostId": ""}}}}
        result = find_pinned_post_id(state, "testblog")
        assert result is None

    def test_pinned_id_zero(self) -> None:
        """pinnedPostId = "0" -> None (not a valid post ID)."""
        state = {"PeeprRoute": {"blogData": {"testblog": {"pinnedPostId": "0"}}}}
        result = find_pinned_post_id(state, "testblog")
        assert result is None

    def test_numeric_pinned_id_coerced_to_string(self) -> None:
        """Some responses have pinnedPostId as int, not string."""
        state = {"PeeprRoute": {"blogData": {"testblog": {"pinnedPostId": 728394056000}}}}
        result = find_pinned_post_id(state, "testblog")
        assert result == "728394056000"


class TestResolvePinnedPost:
    @respx.mock
    async def test_resolves_pinned_post_id(self) -> None:
        html = _wrap_html(_INITIAL_STATE_WITH_PIN)
        respx.get("https://testblog.tumblr.com/").respond(200, text=html)

        async with httpx.AsyncClient() as client:
            result = await resolve_pinned_post(client, "testblog")

        assert result == "728394056000"

    @respx.mock
    async def test_returns_none_when_no_pinned(self) -> None:
        html = _wrap_html(_INITIAL_STATE_NO_PIN)
        respx.get("https://testblog.tumblr.com/").respond(200, text=html)

        async with httpx.AsyncClient() as client:
            result = await resolve_pinned_post(client, "testblog")

        assert result is None

    @respx.mock
    async def test_returns_none_on_http_error(self) -> None:
        respx.get("https://testblog.tumblr.com/").respond(404)

        async with httpx.AsyncClient() as client:
            result = await resolve_pinned_post(client, "testblog")

        assert result is None

    @respx.mock
    async def test_returns_none_on_no_initial_state(self) -> None:
        respx.get("https://testblog.tumblr.com/").respond(
            200, text="<html><body>No state</body></html>"
        )

        async with httpx.AsyncClient() as client:
            result = await resolve_pinned_post(client, "testblog")

        assert result is None

    @respx.mock
    async def test_returns_none_on_connection_error(self) -> None:
        respx.get("https://testblog.tumblr.com/").mock(
            side_effect=httpx.ConnectError("connection refused")
        )

        async with httpx.AsyncClient() as client:
            result = await resolve_pinned_post(client, "testblog")

        assert result is None
```

- [ ] **Step 2: Run test to verify it fails**

Run: `uv run pytest tests/unit/test_pinned_resolver.py -v`
Expected: FAIL -- `ModuleNotFoundError: No module named 'tumbl4.core.crawl.pinned_resolver'`

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/core/crawl/pinned_resolver.py`:

```python
"""Pinned post resolver — identify and exclude pinned posts from cursor computation.

Spec section 5.8:
    Pinned posts can be months or years older than the blog's newest post.
    Without this step, the resume cursor would be set to the pinned post's
    ancient ID, causing a full re-crawl on every run.

    The resolver fetches the blog's HTML front page, extracts
    ___INITIAL_STATE___ JSON, and identifies the pinned post ID. The pinned
    post is still downloaded — it's only excluded from highest_post_id.

    Two possible ___INITIAL_STATE___ shapes:
    1. PeeprRoute shape: state["PeeprRoute"]["blogData"][blog_name]["pinnedPostId"]
    2. Response shape: state["response"]["blogData"][blog_name]["pinnedPostId"]
"""

from __future__ import annotations

import json
import re

import httpx

from tumbl4._internal.logging import get_logger

logger = get_logger(__name__)

# Two regex patterns matching TumblThree's extraction approach.
# Single-line: window["___INITIAL_STATE___"] = {...};
_SINGLE_LINE_RE = re.compile(
    r'(?:window\[")___INITIAL_STATE___(?:"\])\s*=\s*(\{.+?\})\s*;',
    re.DOTALL,
)
# Fallback: bare ___INITIAL_STATE___ = {...};
_BARE_RE = re.compile(
    r'___INITIAL_STATE___\s*=\s*(\{.+?\})\s*;',
    re.DOTALL,
)


def extract_initial_state(html: str) -> dict | None:
    """Extract the ___INITIAL_STATE___ JSON from an HTML page.

    Tries the window["___INITIAL_STATE___"] pattern first, then falls
    back to bare ___INITIAL_STATE___. Returns None if neither matches
    or if the JSON is malformed.
    """
    for pattern in (_SINGLE_LINE_RE, _BARE_RE):
        match = pattern.search(html)
        if match:
            try:
                return json.loads(match.group(1))  # type: ignore[no-any-return]
            except json.JSONDecodeError:
                logger.debug("matched ___INITIAL_STATE___ regex but JSON parse failed")
                continue
    return None


def find_pinned_post_id(state: dict, blog_name: str) -> str | None:
    """Find the pinned post ID from an ___INITIAL_STATE___ dict.

    Checks PeeprRoute shape first, then response-wrapped shape.
    Returns None if no pinned post is found.
    """
    # Try PeeprRoute shape
    pinned = _extract_pinned_from_blog_data(
        state.get("PeeprRoute", {}), blog_name
    )
    if pinned is not None:
        return pinned

    # Try response-wrapped shape
    pinned = _extract_pinned_from_blog_data(
        state.get("response", {}), blog_name
    )
    if pinned is not None:
        return pinned

    return None


def _extract_pinned_from_blog_data(container: object, blog_name: str) -> str | None:
    """Extract pinnedPostId from a blogData container."""
    if not isinstance(container, dict):
        return None

    blog_data = container.get("blogData")
    if not isinstance(blog_data, dict):
        return None

    blog = blog_data.get(blog_name)
    if not isinstance(blog, dict):
        return None

    pinned_raw = blog.get("pinnedPostId")
    if pinned_raw is None:
        return None

    pinned_str = str(pinned_raw).strip()
    if not pinned_str or pinned_str == "0":
        return None

    return pinned_str


async def resolve_pinned_post(
    client: httpx.AsyncClient,
    blog_name: str,
) -> str | None:
    """Fetch a blog's HTML front page and extract its pinned post ID.

    Returns the pinned post ID as a string, or None if:
    - The blog has no pinned post
    - The HTML fetch fails
    - ___INITIAL_STATE___ cannot be extracted
    - The pinned post ID cannot be found in the state

    This function never raises — errors are logged and None is returned.
    """
    url = f"https://{blog_name}.tumblr.com/"
    try:
        response = await client.get(url)
        if response.status_code >= 400:
            logger.debug(
                "pinned resolver: HTTP %d for %s",
                response.status_code,
                blog_name,
            )
            return None

        state = extract_initial_state(response.text)
        if state is None:
            logger.debug("pinned resolver: no ___INITIAL_STATE___ for %s", blog_name)
            return None

        pinned = find_pinned_post_id(state, blog_name)
        if pinned:
            logger.info(
                "pinned resolver: blog %s has pinned post %s", blog_name, pinned
            )
        return pinned

    except (httpx.HTTPError, Exception) as exc:
        logger.debug(
            "pinned resolver: error fetching %s: %s", blog_name, exc
        )
        return None
```

- [ ] **Step 4: Run test to verify it passes**

Run: `uv run pytest tests/unit/test_pinned_resolver.py -v`
Expected: 18 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/core/crawl/pinned_resolver.py tests/unit/test_pinned_resolver.py
git commit -m "feat(crawl): add pinned post resolver

Fetches blog HTML, extracts ___INITIAL_STATE___ with two regex
patterns, identifies pinnedPostId from PeeprRoute or response
shape. Never raises — logs and returns None on any error.
See spec section 5.8.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: Wire filters + dedup + pinned into orchestrator and download command

**Files:**
- Modify: `src/tumbl4/core/orchestrator.py`
- Modify: `src/tumbl4/core/crawl/tumblr_blog.py`
- Modify: `src/tumbl4/cli/commands/download.py`
- Modify: `src/tumbl4/core/__init__.py`

- [ ] **Step 1: Update the orchestrator to integrate filters and cross-blog dedup**

Modify `src/tumbl4/core/orchestrator.py`. The key changes:

1. Accept `FilterConfig` and `DedupDb | None` parameters in `run_crawl()`
2. Apply `apply_filters()` on each `IntermediateDict` before enqueuing media
3. Check cross-blog dedup on each media URL before enqueuing
4. Record to cross-blog dedup after successful download
5. Track `posts_filtered` count in `CrawlResult`

Replace the entire file:

```python
"""Crawl orchestrator — state machine driving the crawl-download pipeline.

Producer-consumer: the crawler yields IntermediateDicts, the orchestrator
applies filters, checks dedup, enqueues MediaTasks onto an asyncio.Queue,
and N download workers consume them. Sidecars are written after all media
for a post resolve.
"""

from __future__ import annotations

import asyncio
from collections import defaultdict
from collections.abc import Awaitable, Callable
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Protocol

import httpx

from tumbl4._internal.logging import get_logger
from tumbl4._internal.tasks import spawn
from tumbl4.core.download.file_downloader import download_media
from tumbl4.core.filter import FilterConfig, apply_filters
from tumbl4.core.parse.intermediate import IntermediateDict
from tumbl4.core.state.db import StateDb
from tumbl4.core.state.dedup import DedupDb
from tumbl4.core.state.metadata import write_sidecar
from tumbl4.core.state.resume import load_cursor, save_cursor
from tumbl4.models.blog import BlogRef
from tumbl4.models.media import DownloadResult, MediaTask
from tumbl4.models.settings import Settings

logger = get_logger(__name__)


class CrawlerProtocol(Protocol):
    """Protocol for blog crawlers."""

    highest_post_id: int
    total_posts: int
    rate_limited: bool

    def crawl(self) -> Any: ...  # AsyncIterator[IntermediateDict]


@dataclass
class CrawlResult:
    """Summary of a crawl run."""

    blog_name: str
    posts_crawled: int = 0
    posts_filtered: int = 0
    downloads_success: int = 0
    downloads_failed: int = 0
    downloads_skipped: int = 0
    downloads_deduped_cross_blog: int = 0
    complete: bool = False


DownloadFn = Callable[[MediaTask, httpx.AsyncClient], Awaitable[DownloadResult]]


async def run_crawl(
    *,
    settings: Settings,
    blog: BlogRef,
    crawler: CrawlerProtocol,
    download_fn: DownloadFn = download_media,
    no_resume: bool = False,
    filter_config: FilterConfig | None = None,
    dedup_db: DedupDb | None = None,
) -> CrawlResult:
    """Run the full crawl pipeline for a single blog.

    1. Open per-blog state database
    2. Crawl pages, apply filters, check dedup, enqueue MediaTasks
    3. Download workers consume tasks
    4. Write sidecars on post completion
    5. Update resume cursor on complete crawl
    """
    if filter_config is None:
        filter_config = FilterConfig()

    blog_dir = settings.output_dir / blog.name
    blog_dir.mkdir(parents=True, exist_ok=True)

    # State database lives alongside downloads
    from tumbl4._internal.paths import data_dir

    db_dir = data_dir()
    db_dir.mkdir(parents=True, exist_ok=True)
    db_path = db_dir / f"{blog.name}.db"
    db = StateDb(str(db_path))

    result = CrawlResult(blog_name=blog.name)
    queue: asyncio.Queue[MediaTask | None] = asyncio.Queue(
        maxsize=settings.queue.max_pending_media
    )

    # Track media per post for sidecar writing
    post_media: dict[str, list[DownloadResult]] = defaultdict(list)
    post_data: dict[str, IntermediateDict] = {}
    post_expected_count: dict[str, int] = {}

    async def download_worker(client: httpx.AsyncClient) -> None:
        while True:
            task = await queue.get()
            if task is None:
                queue.task_done()
                break

            # Per-blog dedup check (always active)
            if db.is_downloaded(task.url_hash):
                result.downloads_skipped += 1
                post_media[task.post_id].append(DownloadResult(
                    url=task.url, post_id=task.post_id,
                    filename=None, byte_count=0, status="skipped",
                ))
                _maybe_write_sidecar(db, settings.output_dir, task.post_id, post_data, post_media, post_expected_count)
                queue.task_done()
                continue

            dl_result = await download_fn(task, client)

            # Record in per-blog state database
            db.record_download(
                url_hash=task.url_hash,
                url=task.url,
                blog_name=task.blog_name,
                post_id=task.post_id,
                filename=dl_result.filename,
                byte_count=dl_result.byte_count,
                status=dl_result.status,
                error=dl_result.error,
            )

            # Record in cross-blog dedup if download succeeded
            if dl_result.status == "success" and dedup_db is not None:
                dedup_db.record(
                    url_hash=task.url_hash,
                    url=task.url,
                    blog_name=task.blog_name,
                )

            if dl_result.status == "success":
                result.downloads_success += 1
            else:
                result.downloads_failed += 1

            post_media[task.post_id].append(dl_result)
            _maybe_write_sidecar(db, settings.output_dir, task.post_id, post_data, post_media, post_expected_count)

            queue.task_done()

    # Start download workers
    async with httpx.AsyncClient(
        follow_redirects=True,
        headers={"User-Agent": f"tumbl4/{__import__('tumbl4').__version__}"},
    ) as client:
        workers = [spawn(download_worker(client), name=f"dl-worker-{i}") for i in range(settings.max_concurrent_downloads)]

        # Crawl and enqueue
        async for intermediate in crawler.crawl():
            result.posts_crawled += 1

            # Apply filters
            if not apply_filters(intermediate, filter_config):
                result.posts_filtered += 1
                continue

            post_data[intermediate["post_id"]] = intermediate

            if not intermediate["media"]:
                # Post with no media -- write sidecar immediately
                post_expected_count[intermediate["post_id"]] = 0
                _write_post_sidecar(db, settings.output_dir, intermediate, [])
                continue

            # Check cross-blog dedup and enqueue media
            media_to_enqueue: list[MediaTask] = []
            for idx, media in enumerate(intermediate["media"]):
                task = MediaTask(
                    url=media["url"],
                    post_id=intermediate["post_id"],
                    blog_name=intermediate["blog_name"],
                    index=idx,
                    output_dir=str(blog_dir),
                )

                # Cross-blog dedup check (skippable with --no-dedup)
                if dedup_db is not None and dedup_db.is_known(task.url_hash):
                    result.downloads_deduped_cross_blog += 1
                    post_media[intermediate["post_id"]].append(DownloadResult(
                        url=task.url, post_id=task.post_id,
                        filename=None, byte_count=0, status="deduped",
                    ))
                    continue

                media_to_enqueue.append(task)

            post_expected_count[intermediate["post_id"]] = len(media_to_enqueue) + len(post_media.get(intermediate["post_id"], []))

            if not media_to_enqueue:
                # All media deduped -- write sidecar with dedup results
                _write_post_sidecar(db, settings.output_dir, intermediate, post_media.get(intermediate["post_id"], []))
                continue

            for task in media_to_enqueue:
                await queue.put(task)

        # Signal workers to stop
        for _ in workers:
            await queue.put(None)
        await asyncio.gather(*workers)

    # Update resume cursor only if crawl was complete (no rate limit)
    if not crawler.rate_limited:
        if crawler.highest_post_id > 0 and not no_resume:
            save_cursor(db, blog.name, crawler.highest_post_id)
        result.complete = True
    else:
        result.complete = False

    db.close()
    return result


def _maybe_write_sidecar(
    db: StateDb,
    output_dir: Path,
    post_id: str,
    post_data: dict[str, IntermediateDict],
    post_media: dict[str, list[DownloadResult]],
    post_expected_count: dict[str, int],
) -> None:
    """Write sidecar if all media for a post have been processed."""
    intermediate = post_data.get(post_id)
    expected = post_expected_count.get(post_id)
    if intermediate is None or expected is None:
        return
    if len(post_media.get(post_id, [])) >= expected:
        _write_post_sidecar(db, output_dir, intermediate, post_media.get(post_id, []))


def _write_post_sidecar(
    db: StateDb,
    output_dir: Path,
    intermediate: IntermediateDict,
    media_results: list[DownloadResult],
) -> None:
    """Write the JSON sidecar and mark the post complete in state."""
    media_dicts = [
        {
            "filename": r.filename,
            "url": r.url,
            "bytes": r.byte_count,
            "status": r.status,
            **({"error": r.error} if r.error else {}),
        }
        for r in media_results
    ]

    write_sidecar(
        output_dir=output_dir / intermediate["blog_name"],
        post_id=intermediate["post_id"],
        blog_name=intermediate["blog_name"],
        post_url=intermediate["post_url"],
        post_type=intermediate["post_type"],
        timestamp_utc=intermediate["timestamp_utc"],
        tags=intermediate["tags"],
        is_reblog=intermediate["is_reblog"],
        media_results=media_dicts,
        reblog_source=intermediate["reblog_source"],  # type: ignore[arg-type]
        body_text=intermediate["body_text"],
        body_html=intermediate["body_html"],
    )

    db.mark_post_complete(intermediate["post_id"], intermediate["blog_name"])
```

- [ ] **Step 2: Update the crawler to skip pinned posts in highest_id computation**

Modify `src/tumbl4/core/crawl/tumblr_blog.py`. Add a `pinned_post_id` parameter to `TumblrBlogCrawler.__init__()` and skip the pinned post when computing `highest_post_id`:

In the `__init__` method, add after `self._image_size = image_size`:
```python
        self._pinned_post_id = pinned_post_id
```

Add the parameter to the signature:
```python
    def __init__(
        self,
        http: TumblrHttpClient,
        blog: BlogRef,
        *,
        page_size: int = 50,
        last_id: int = 0,
        image_size: str = "1280",
        pinned_post_id: str | None = None,
    ) -> None:
```

In the `crawl()` method, change the highest_post_id tracking block. Replace:
```python
                # Track highest post ID
                if post_id_int > self.highest_post_id:
                    self.highest_post_id = post_id_int
```

With:
```python
                # Track highest post ID (skip pinned post per spec section 5.8)
                if post_id_int > self.highest_post_id:
                    if self._pinned_post_id is None or post_id_str != self._pinned_post_id:
                        self.highest_post_id = post_id_int
```

- [ ] **Step 3: Update the download command to accept filter and dedup flags**

Modify `src/tumbl4/cli/commands/download.py`. Add the new CLI flags to the `download()` function signature and wire them through to the orchestrator. The updated function signatures should be:

```python
def download(
    blog: Annotated[str, typer.Argument(help="Blog name or URL (e.g., 'photography' or 'https://photography.tumblr.com')")],
    output_dir: Annotated[Path | None, typer.Option("--output-dir", "-o", help="Output directory")] = None,
    page_size: Annotated[int, typer.Option("--page-size", help="Posts per API page (1-50)", callback=_validate_page_size)] = 50,
    image_size: Annotated[str, typer.Option("--image-size", help="Image size: 1280, 500, 400, 250, 100, 75", callback=_validate_image_size)] = "1280",
    no_resume: Annotated[bool, typer.Option("--no-resume", help="Ignore saved cursor, full re-crawl")] = False,
    quiet: Annotated[bool, typer.Option("--quiet", "-q", help="Suppress progress output")] = False,
    verbose: Annotated[bool, typer.Option("--verbose", "-v", help="Enable debug logging")] = False,
    # Plan 4 filter flags
    tag: Annotated[list[str] | None, typer.Option("--tag", help="Include only posts with this tag (repeatable, OR)")] = None,
    exclude_tag: Annotated[list[str] | None, typer.Option("--exclude-tag", help="Exclude posts with this tag (repeatable, OR)")] = None,
    since: Annotated[str | None, typer.Option("--since", help="Include only posts on or after this date (YYYY-MM-DD)")] = None,
    until: Annotated[str | None, typer.Option("--until", help="Include only posts on or before this date (YYYY-MM-DD)")] = None,
    originals_only: Annotated[bool, typer.Option("--originals-only", help="Skip reblogs, download originals only")] = False,
    reblogs_only: Annotated[bool, typer.Option("--reblogs-only", help="Skip originals, download reblogs only")] = False,
    no_dedup: Annotated[bool, typer.Option("--no-dedup", help="Disable cross-blog dedup (per-blog dedup always active)")] = False,
) -> None:
    """Download photos from a public Tumblr blog."""
    asyncio.run(_download_async(
        blog=blog,
        output_dir=output_dir,
        page_size=page_size,
        image_size=image_size,
        no_resume=no_resume,
        quiet=quiet,
        verbose=verbose,
        tag=tag or [],
        exclude_tag=exclude_tag or [],
        since=since,
        until=until,
        originals_only=originals_only,
        reblogs_only=reblogs_only,
        no_dedup=no_dedup,
    ))
```

In `_download_async`, build FilterConfig and DedupDb before calling `run_crawl()`:

```python
async def _download_async(
    *,
    blog: str,
    output_dir: Path | None,
    page_size: int,
    image_size: str,
    no_resume: bool,
    quiet: bool,
    verbose: bool,
    tag: list[str],
    exclude_tag: list[str],
    since: str | None,
    until: str | None,
    originals_only: bool,
    reblogs_only: bool,
    no_dedup: bool,
) -> None:
    """Async implementation of the download command."""
    import logging
    from datetime import UTC, datetime

    from tumbl4._internal.paths import data_dir, dedup_db as dedup_db_path
    from tumbl4.core.crawl.http_client import TumblrHttpClient
    from tumbl4.core.crawl.pinned_resolver import resolve_pinned_post
    from tumbl4.core.crawl.tumblr_blog import TumblrBlogCrawler
    from tumbl4.core.errors import ConfigError
    from tumbl4.core.filter import FilterConfig, validate_filter_config
    from tumbl4.core.orchestrator import run_crawl
    from tumbl4.core.state.db import StateDb
    from tumbl4.core.state.dedup import DedupDb
    from tumbl4.core.state.resume import load_cursor
    from tumbl4.models.blog import BlogRef
    from tumbl4.models.settings import Settings

    if verbose:
        logging.basicConfig(level=logging.DEBUG)
    elif quiet:
        logging.basicConfig(level=logging.WARNING)
    else:
        logging.basicConfig(level=logging.INFO)

    blog_ref = BlogRef.from_input(blog)
    settings = Settings()
    if output_dir:
        settings = Settings(output_dir=output_dir)

    # Build filter config
    since_dt = None
    until_dt = None
    if since:
        try:
            since_dt = datetime.strptime(since, "%Y-%m-%d").replace(tzinfo=UTC)
        except ValueError:
            console.print(f"[red]Invalid --since date: {since}. Use YYYY-MM-DD format.[/red]")
            raise typer.Exit(code=1)
    if until:
        try:
            until_dt = datetime.strptime(until, "%Y-%m-%d").replace(
                hour=23, minute=59, second=59, tzinfo=UTC
            )
        except ValueError:
            console.print(f"[red]Invalid --until date: {until}. Use YYYY-MM-DD format.[/red]")
            raise typer.Exit(code=1)

    filter_config = FilterConfig(
        include_originals=not reblogs_only,
        include_reblogs=not originals_only,
        include_tags=tag,
        exclude_tags=exclude_tag,
        since=since_dt,
        until=until_dt,
    )
    try:
        validate_filter_config(filter_config)
    except ConfigError as e:
        console.print(f"[red]Filter config error: {e}[/red]")
        raise typer.Exit(code=1)

    if not quiet:
        console.print(f"[bold]tumbl4[/bold] downloading [cyan]{blog_ref.name}[/cyan]")

    # Load resume cursor
    db_dir = data_dir()
    db_dir.mkdir(parents=True, exist_ok=True)
    db_path = db_dir / f"{blog_ref.name}.db"

    last_id = 0
    if not no_resume and db_path.exists():
        db = StateDb(str(db_path))
        last_id = load_cursor(db, blog_ref.name)
        db.close()
        if last_id > 0 and not quiet:
            console.print(f"  Resuming from post ID {last_id}")

    # Open cross-blog dedup (unless --no-dedup)
    dedup: DedupDb | None = None
    if not no_dedup:
        dedup_path = dedup_db_path()
        dedup_path.parent.mkdir(parents=True, exist_ok=True)
        dedup = DedupDb(str(dedup_path))

    http = TumblrHttpClient(settings.http)

    try:
        # Resolve pinned post
        pinned_post_id = await resolve_pinned_post(http.client, blog_ref.name)
        if pinned_post_id and not quiet:
            console.print(f"  Pinned post detected: {pinned_post_id} (excluded from cursor)")

        crawler = TumblrBlogCrawler(
            http, blog_ref,
            page_size=page_size,
            last_id=last_id,
            image_size=image_size,
            pinned_post_id=pinned_post_id,
        )

        result = await run_crawl(
            settings=settings,
            blog=blog_ref,
            crawler=crawler,
            no_resume=no_resume,
            filter_config=filter_config,
            dedup_db=dedup,
        )

        if not quiet:
            console.print(f"\n[bold green]Done![/bold green]")
            console.print(f"  Posts crawled: {result.posts_crawled}")
            if result.posts_filtered > 0:
                console.print(f"  Posts filtered out: {result.posts_filtered}")
            console.print(f"  Downloads: {result.downloads_success} success, "
                          f"{result.downloads_failed} failed, "
                          f"{result.downloads_skipped} skipped (per-blog dedup)")
            if result.downloads_deduped_cross_blog > 0:
                console.print(f"  Cross-blog dedup: {result.downloads_deduped_cross_blog} skipped")
            if not result.complete:
                console.print("[yellow]  Crawl incomplete (rate limited). Run again to continue.[/yellow]")
    finally:
        await http.aclose()
        if dedup is not None:
            dedup.close()
```

- [ ] **Step 4: Update core __init__.py**

Update `src/tumbl4/core/__init__.py`:

```python
"""Core modules — orchestrator, crawlers, parsers, downloaders, state, filters.

Unstable public API — may change between minor versions until v1.0.0.
"""

from tumbl4.core.filter import FilterConfig, apply_filters
from tumbl4.core.orchestrator import CrawlResult, run_crawl

__all__ = ["CrawlResult", "FilterConfig", "apply_filters", "run_crawl"]
```

- [ ] **Step 5: Run all tests to verify nothing is broken**

Run: `uv run pytest -v`
Expected: all tests pass

- [ ] **Step 6: Commit**

```bash
git add src/tumbl4/core/orchestrator.py src/tumbl4/core/crawl/tumblr_blog.py src/tumbl4/cli/commands/download.py src/tumbl4/core/__init__.py
git commit -m "feat(core): wire filters, cross-blog dedup, and pinned posts into pipeline

Orchestrator now accepts FilterConfig and DedupDb. Filters applied
between crawl and media enqueue. Cross-blog dedup checked per-media
URL. Pinned post excluded from highest_post_id in crawler. Download
command gains --tag, --exclude-tag, --since, --until, --originals-only,
--reblogs-only, --no-dedup flags.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: `tumbl4 list` and `tumbl4 status <blog>` subcommands

**Files:**
- Create: `src/tumbl4/cli/output/tables.py`
- Create: `src/tumbl4/cli/commands/list_blogs.py`
- Create: `src/tumbl4/cli/commands/status.py`
- Modify: `src/tumbl4/cli/app.py`
- Create: `tests/unit/test_list_command.py`
- Create: `tests/unit/test_status_command.py`

- [ ] **Step 1: Write the failing tests**

Write file `tests/unit/test_list_command.py`:

```python
"""Tests for the tumbl4 list subcommand."""

from typer.testing import CliRunner

from tumbl4.cli.app import app

runner = CliRunner()


class TestListCommand:
    def test_list_appears_in_help(self) -> None:
        result = runner.invoke(app, ["--help"])
        assert result.exit_code == 0
        assert "list" in result.output

    def test_list_help(self) -> None:
        result = runner.invoke(app, ["list", "--help"])
        assert result.exit_code == 0
        assert "managed" in result.output.lower() or "blog" in result.output.lower()

    def test_list_no_blogs_message(self) -> None:
        """When no blogs have been crawled, show a friendly message."""
        result = runner.invoke(app, ["list"])
        # Should succeed even with no data
        assert result.exit_code == 0
```

Write file `tests/unit/test_status_command.py`:

```python
"""Tests for the tumbl4 status subcommand."""

from typer.testing import CliRunner

from tumbl4.cli.app import app

runner = CliRunner()


class TestStatusCommand:
    def test_status_appears_in_help(self) -> None:
        result = runner.invoke(app, ["--help"])
        assert result.exit_code == 0
        assert "status" in result.output

    def test_status_help(self) -> None:
        result = runner.invoke(app, ["status", "--help"])
        assert result.exit_code == 0
        assert "blog" in result.output.lower()

    def test_status_requires_blog_argument(self) -> None:
        result = runner.invoke(app, ["status"])
        assert result.exit_code != 0

    def test_status_unknown_blog(self) -> None:
        """Status for an unknown blog should show a friendly message."""
        result = runner.invoke(app, ["status", "nonexistent-blog-99999"])
        assert result.exit_code == 0
        assert "no data" in result.output.lower() or "not found" in result.output.lower() or "never" in result.output.lower()
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `uv run pytest tests/unit/test_list_command.py tests/unit/test_status_command.py -v`
Expected: FAIL -- "list" and "status" not found in help output

- [ ] **Step 3: Write the implementation**

Write file `src/tumbl4/cli/output/tables.py`:

```python
"""Rich table formatters for the list and status subcommands."""

from __future__ import annotations

from rich.console import Console
from rich.table import Table

console = Console()


def render_blog_list(blogs: list[dict[str, object]]) -> None:
    """Render a list of managed blogs as a Rich table."""
    if not blogs:
        console.print("[dim]No blogs found. Run [bold]tumbl4 download <blog>[/bold] to start.[/dim]")
        return

    table = Table(title="Managed Blogs")
    table.add_column("Blog", style="cyan")
    table.add_column("Last Crawl", style="green")
    table.add_column("Downloads", justify="right")
    table.add_column("Resume ID", justify="right", style="dim")

    for blog in blogs:
        table.add_row(
            str(blog.get("name", "")),
            str(blog.get("last_crawl", "never")),
            str(blog.get("download_count", 0)),
            str(blog.get("last_id", 0)),
        )

    console.print(table)


def render_blog_status(status: dict[str, object]) -> None:
    """Render detailed status for a single blog as a Rich table."""
    table = Table(title=f"Status: {status.get('name', 'unknown')}")
    table.add_column("Property", style="bold")
    table.add_column("Value")

    table.add_row("Blog name", str(status.get("name", "")))
    table.add_row("Last crawl", str(status.get("last_crawl", "never")))
    table.add_row("Resume post ID", str(status.get("last_id", 0)))
    table.add_row("Total downloads", str(status.get("download_count", 0)))
    table.add_row("Successful", str(status.get("success_count", 0)))
    table.add_row("Failed", str(status.get("failed_count", 0)))
    table.add_row("Posts with sidecars", str(status.get("post_count", 0)))
    table.add_row("Database path", str(status.get("db_path", "")))

    console.print(table)
```

Write file `src/tumbl4/cli/commands/list_blogs.py`:

```python
"""tumbl4 list — show all managed blogs from state."""

from __future__ import annotations

import sqlite3
from pathlib import Path

from tumbl4._internal.logging import get_logger
from tumbl4._internal.paths import data_dir
from tumbl4.cli.output.tables import render_blog_list

logger = get_logger(__name__)


def list_blogs() -> None:
    """Show all blogs that have been crawled by tumbl4."""
    db_dir = data_dir()

    if not db_dir.exists():
        render_blog_list([])
        return

    blogs: list[dict[str, object]] = []
    for db_file in sorted(db_dir.glob("*.db")):
        # Skip the cross-blog dedup database
        if db_file.name == "dedup.db":
            continue

        blog_name = db_file.stem
        try:
            conn = sqlite3.connect(str(db_file))
            conn.row_factory = sqlite3.Row

            # Get crawl state
            row = conn.execute(
                "SELECT last_id, last_complete_crawl FROM crawl_state "
                "WHERE blog_name = ? ORDER BY last_complete_crawl DESC LIMIT 1",
                (blog_name,),
            ).fetchone()

            last_id = row["last_id"] if row else 0
            last_crawl = row["last_complete_crawl"] if row else "never"

            # Get download count
            count_row = conn.execute(
                "SELECT COUNT(*) as cnt FROM downloads WHERE blog_name = ?",
                (blog_name,),
            ).fetchone()
            download_count = count_row["cnt"] if count_row else 0

            conn.close()

            blogs.append({
                "name": blog_name,
                "last_crawl": last_crawl or "never",
                "download_count": download_count,
                "last_id": last_id,
            })
        except sqlite3.Error as e:
            logger.warning("failed to read state for %s: %s", blog_name, e)
            blogs.append({
                "name": blog_name,
                "last_crawl": "error",
                "download_count": 0,
                "last_id": 0,
            })

    render_blog_list(blogs)
```

Write file `src/tumbl4/cli/commands/status.py`:

```python
"""tumbl4 status <blog> — show detailed status for a single blog."""

from __future__ import annotations

import sqlite3
from typing import Annotated

import typer

from tumbl4._internal.logging import get_logger
from tumbl4._internal.paths import data_dir
from tumbl4.cli.output.tables import console, render_blog_status
from tumbl4.models.blog import BlogRef

logger = get_logger(__name__)


def status(
    blog: Annotated[str, typer.Argument(help="Blog name or URL")],
) -> None:
    """Show detailed crawl status for a blog."""
    blog_ref = BlogRef.from_input(blog)
    db_dir = data_dir()
    db_path = db_dir / f"{blog_ref.name}.db"

    if not db_path.exists():
        console.print(f"[dim]No data found for [cyan]{blog_ref.name}[/cyan]. "
                       f"Blog has never been crawled.[/dim]")
        return

    try:
        conn = sqlite3.connect(str(db_path))
        conn.row_factory = sqlite3.Row

        # Crawl state
        crawl_row = conn.execute(
            "SELECT last_id, last_complete_crawl FROM crawl_state "
            "WHERE blog_name = ? ORDER BY last_complete_crawl DESC LIMIT 1",
            (blog_ref.name,),
        ).fetchone()

        last_id = crawl_row["last_id"] if crawl_row else 0
        last_crawl = crawl_row["last_complete_crawl"] if crawl_row else "never"

        # Download counts
        total_row = conn.execute(
            "SELECT COUNT(*) as cnt FROM downloads WHERE blog_name = ?",
            (blog_ref.name,),
        ).fetchone()
        download_count = total_row["cnt"] if total_row else 0

        success_row = conn.execute(
            "SELECT COUNT(*) as cnt FROM downloads WHERE blog_name = ? AND status = 'success'",
            (blog_ref.name,),
        ).fetchone()
        success_count = success_row["cnt"] if success_row else 0

        failed_row = conn.execute(
            "SELECT COUNT(*) as cnt FROM downloads WHERE blog_name = ? AND status = 'failed'",
            (blog_ref.name,),
        ).fetchone()
        failed_count = failed_row["cnt"] if failed_row else 0

        # Post count
        post_row = conn.execute(
            "SELECT COUNT(*) as cnt FROM posts WHERE blog_name = ? AND sidecar_written = 1",
            (blog_ref.name,),
        ).fetchone()
        post_count = post_row["cnt"] if post_row else 0

        conn.close()

        render_blog_status({
            "name": blog_ref.name,
            "last_crawl": last_crawl or "never",
            "last_id": last_id,
            "download_count": download_count,
            "success_count": success_count,
            "failed_count": failed_count,
            "post_count": post_count,
            "db_path": str(db_path),
        })

    except sqlite3.Error as e:
        console.print(f"[red]Error reading state for {blog_ref.name}: {e}[/red]")
        raise typer.Exit(code=1)
```

Modify `src/tumbl4/cli/app.py` — add the list and status command registrations. The full updated file:

```python
"""Top-level Typer application for tumbl4.

This module exposes `app` (the Typer instance) and `main()` (the function
referenced by the `tumbl4` console script in pyproject.toml).
"""

from __future__ import annotations

from typing import Annotated

import typer

import tumbl4

app = typer.Typer(
    name="tumbl4",
    help="Command-line Tumblr blog backup tool for macOS and Linux.",
    no_args_is_help=True,
    add_completion=False,
)


def _version_callback(value: bool) -> None:
    if value:
        typer.echo(f"tumbl4 {tumbl4.__version__}")
        raise typer.Exit(code=0)


@app.callback()
def root(
    version: Annotated[
        bool,
        typer.Option(
            "--version",
            "-V",
            help="Show version and exit.",
            callback=_version_callback,
            is_eager=True,
        ),
    ] = False,
) -> None:
    """tumbl4 — command-line Tumblr blog backup tool for macOS and Linux."""
    return None


# Register subcommands
from tumbl4.cli.commands.download import download  # noqa: E402
from tumbl4.cli.commands.list_blogs import list_blogs  # noqa: E402
from tumbl4.cli.commands.status import status  # noqa: E402

app.command()(download)
app.command(name="list")(list_blogs)
app.command()(status)


def main() -> None:
    """Console-script entry point referenced from pyproject.toml."""
    app()
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `uv run pytest tests/unit/test_list_command.py tests/unit/test_status_command.py -v`
Expected: 7 passed

- [ ] **Step 5: Commit**

```bash
git add src/tumbl4/cli/output/tables.py src/tumbl4/cli/commands/list_blogs.py src/tumbl4/cli/commands/status.py src/tumbl4/cli/app.py tests/unit/test_list_command.py tests/unit/test_status_command.py
git commit -m "feat(cli): add 'tumbl4 list' and 'tumbl4 status <blog>' subcommands

list: shows all managed blogs with last crawl time, download count,
resume ID. status: shows detailed per-blog state from SQLite.
Both use Rich tables for output.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 9: Component test — filters + dedup + pinned in the pipeline

**Files:**
- Create: `tests/component/test_filter_pipeline.py`

- [ ] **Step 1: Write the component test**

Write file `tests/component/test_filter_pipeline.py`:

```python
"""Component test — filters, cross-blog dedup, and pinned posts end-to-end.

Verifies the full pipeline with mocked HTTP: crawl posts, apply filters,
check dedup, download, and verify sidecars.
"""

import json
from collections.abc import AsyncIterator
from pathlib import Path
from unittest.mock import MagicMock

import httpx
import pytest
import respx

from tumbl4.core.filter import FilterConfig
from tumbl4.core.orchestrator import run_crawl
from tumbl4.core.parse.intermediate import IntermediateDict, MediaEntry
from tumbl4.core.state.dedup import DedupDb
from tumbl4.models.blog import BlogRef
from tumbl4.models.media import DownloadResult, MediaTask
from tumbl4.models.settings import Settings


def _make_post(
    post_id: str,
    media_url: str,
    is_reblog: bool = False,
    tags: list[str] | None = None,
    timestamp: str = "2025-06-15T12:00:00+00:00",
) -> IntermediateDict:
    return IntermediateDict(
        schema_version=1,
        source_format="api",
        post_id=post_id,
        blog_name="testblog",
        post_url=f"https://testblog.tumblr.com/post/{post_id}",
        post_type="photo",
        timestamp_utc=timestamp,
        tags=tags or [],
        is_reblog=is_reblog,
        reblog_source=None,
        title=None,
        body_text=None,
        body_html=None,
        media=[
            MediaEntry(
                kind="photo", url=media_url,
                width=1280, height=960,
                mime_type=None, alt_text=None, duration_ms=None,
            )
        ],
        raw_content_blocks=None,
    )


def _make_fake_crawler(posts: list[IntermediateDict], pinned_id: str | None = None) -> MagicMock:
    """Build a mock crawler that yields the given posts."""
    async def fake_crawl() -> AsyncIterator[IntermediateDict]:
        for p in posts:
            yield p

    mock = MagicMock()
    mock.crawl = fake_crawl
    highest = max((int(p["post_id"]) for p in posts), default=0)
    mock.highest_post_id = highest
    mock.total_posts = len(posts)
    mock.rate_limited = False
    return mock


async def _fake_download(task: MediaTask, client: httpx.AsyncClient) -> DownloadResult:
    """Simulate a successful download without network."""
    return DownloadResult(
        url=task.url,
        post_id=task.post_id,
        filename=f"{task.post_id}_{task.index:02d}.jpg",
        byte_count=1024,
        status="success",
    )


class TestFilterPipeline:
    async def test_originals_only_filters_reblogs(self, tmp_path: Path) -> None:
        posts = [
            _make_post("300", "https://example.com/a.jpg", is_reblog=False, tags=["art"]),
            _make_post("200", "https://example.com/b.jpg", is_reblog=True, tags=["art"]),
            _make_post("100", "https://example.com/c.jpg", is_reblog=False, tags=["art"]),
        ]
        settings = Settings(output_dir=tmp_path / "output", max_concurrent_downloads=1)
        crawler = _make_fake_crawler(posts)
        filter_config = FilterConfig(include_reblogs=False)

        result = await run_crawl(
            settings=settings,
            blog=BlogRef.from_input("testblog"),
            crawler=crawler,
            download_fn=_fake_download,
            no_resume=True,
            filter_config=filter_config,
        )

        assert result.posts_crawled == 3
        assert result.posts_filtered == 1
        assert result.downloads_success == 2

    async def test_tag_filter_keeps_matching_only(self, tmp_path: Path) -> None:
        posts = [
            _make_post("300", "https://example.com/a.jpg", tags=["photography", "sunset"]),
            _make_post("200", "https://example.com/b.jpg", tags=["art", "wip"]),
            _make_post("100", "https://example.com/c.jpg", tags=["photography"]),
        ]
        settings = Settings(output_dir=tmp_path / "output", max_concurrent_downloads=1)
        crawler = _make_fake_crawler(posts)
        filter_config = FilterConfig(include_tags=["photography"])

        result = await run_crawl(
            settings=settings,
            blog=BlogRef.from_input("testblog"),
            crawler=crawler,
            download_fn=_fake_download,
            no_resume=True,
            filter_config=filter_config,
        )

        assert result.posts_crawled == 3
        assert result.posts_filtered == 1
        assert result.downloads_success == 2

    async def test_timespan_filter(self, tmp_path: Path) -> None:
        from datetime import UTC, datetime

        posts = [
            _make_post("300", "https://example.com/a.jpg", timestamp="2025-06-15T00:00:00+00:00"),
            _make_post("200", "https://example.com/b.jpg", timestamp="2025-03-01T00:00:00+00:00"),
            _make_post("100", "https://example.com/c.jpg", timestamp="2025-09-01T00:00:00+00:00"),
        ]
        settings = Settings(output_dir=tmp_path / "output", max_concurrent_downloads=1)
        crawler = _make_fake_crawler(posts)
        filter_config = FilterConfig(
            since=datetime(2025, 5, 1, tzinfo=UTC),
            until=datetime(2025, 7, 31, tzinfo=UTC),
        )

        result = await run_crawl(
            settings=settings,
            blog=BlogRef.from_input("testblog"),
            crawler=crawler,
            download_fn=_fake_download,
            no_resume=True,
            filter_config=filter_config,
        )

        assert result.posts_crawled == 3
        assert result.posts_filtered == 2
        assert result.downloads_success == 1

    async def test_cross_blog_dedup_skips_known_urls(self, tmp_path: Path) -> None:
        """URLs already in the cross-blog dedup DB should be skipped."""
        import hashlib

        posts = [
            _make_post("300", "https://example.com/shared.jpg"),
            _make_post("200", "https://example.com/unique.jpg"),
        ]
        settings = Settings(output_dir=tmp_path / "output", max_concurrent_downloads=1)
        crawler = _make_fake_crawler(posts)

        # Pre-populate cross-blog dedup with one URL
        dedup = DedupDb(":memory:")
        shared_hash = hashlib.sha256(b"https://example.com/shared.jpg").hexdigest()
        dedup.record(url_hash=shared_hash, url="https://example.com/shared.jpg", blog_name="other_blog")

        result = await run_crawl(
            settings=settings,
            blog=BlogRef.from_input("testblog"),
            crawler=crawler,
            download_fn=_fake_download,
            no_resume=True,
            dedup_db=dedup,
        )

        assert result.posts_crawled == 2
        assert result.downloads_deduped_cross_blog == 1
        assert result.downloads_success == 1
        dedup.close()

    async def test_no_dedup_flag_bypasses_cross_blog(self, tmp_path: Path) -> None:
        """With --no-dedup (dedup_db=None), cross-blog dedup is skipped."""
        posts = [
            _make_post("300", "https://example.com/shared.jpg"),
        ]
        settings = Settings(output_dir=tmp_path / "output", max_concurrent_downloads=1)
        crawler = _make_fake_crawler(posts)

        result = await run_crawl(
            settings=settings,
            blog=BlogRef.from_input("testblog"),
            crawler=crawler,
            download_fn=_fake_download,
            no_resume=True,
            dedup_db=None,  # --no-dedup
        )

        assert result.downloads_success == 1
        assert result.downloads_deduped_cross_blog == 0

    async def test_combined_filters_and_dedup(self, tmp_path: Path) -> None:
        """All filters + dedup applied together in the right order."""
        import hashlib
        from datetime import UTC, datetime

        posts = [
            _make_post("400", "https://example.com/a.jpg", is_reblog=False, tags=["art"], timestamp="2025-06-15T00:00:00+00:00"),
            _make_post("300", "https://example.com/b.jpg", is_reblog=True, tags=["art"], timestamp="2025-06-15T00:00:00+00:00"),   # filtered: reblog
            _make_post("200", "https://example.com/c.jpg", is_reblog=False, tags=["nsfw"], timestamp="2025-06-15T00:00:00+00:00"),  # filtered: wrong tag
            _make_post("100", "https://example.com/d.jpg", is_reblog=False, tags=["art"], timestamp="2024-01-01T00:00:00+00:00"),   # filtered: before since
        ]
        settings = Settings(output_dir=tmp_path / "output", max_concurrent_downloads=1)
        crawler = _make_fake_crawler(posts)

        dedup = DedupDb(":memory:")
        filter_config = FilterConfig(
            include_reblogs=False,
            include_tags=["art"],
            since=datetime(2025, 1, 1, tzinfo=UTC),
        )

        result = await run_crawl(
            settings=settings,
            blog=BlogRef.from_input("testblog"),
            crawler=crawler,
            download_fn=_fake_download,
            no_resume=True,
            filter_config=filter_config,
            dedup_db=dedup,
        )

        assert result.posts_crawled == 4
        assert result.posts_filtered == 3
        assert result.downloads_success == 1
        dedup.close()
```

- [ ] **Step 2: Run the component test**

Run: `uv run pytest tests/component/test_filter_pipeline.py -v`
Expected: 6 passed

- [ ] **Step 3: Run ALL tests**

Run: `uv run pytest -v`
Expected: all tests pass

- [ ] **Step 4: Run quality gates**

Run: `uv run ruff check .`
Expected: all checks pass (fix any issues found)

Run: `uv run ruff format --check .`
Expected: all formatting correct (run `uv run ruff format .` to fix if needed)

Run: `uv run pyright`
Expected: 0 errors (fix any type errors found)

- [ ] **Step 5: Commit**

```bash
git add tests/component/test_filter_pipeline.py
git commit -m "test: add component tests for filter + dedup pipeline

End-to-end pipeline with fake crawler/downloader: originals-only,
tag filter, timespan, cross-blog dedup, --no-dedup bypass, and
combined filters+dedup. Verifies correct post_filtered and
downloads_deduped_cross_blog counts.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

- [ ] **Step 6: Final quality gate commit (if needed)**

If Steps 3-4 required any fixes, commit them:

```bash
git add -u
git commit -m "fix: address Plan 4 quality gate findings

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Self-Review Checklist

Before calling Plan 4 complete, verify:

- [ ] **All tests pass:** `uv run pytest -v` — 0 failures
- [ ] **Ruff clean:** `uv run ruff check .` — 0 violations
- [ ] **Ruff format:** `uv run ruff format --check .` — all files formatted
- [ ] **Pyright clean:** `uv run pyright` — 0 errors
- [ ] **Filters are pure functions:** `reblog.py`, `tag.py`, `timespan.py` have no side effects, no I/O, no global state
- [ ] **Table-driven tests:** Every filter has a parametrized test matrix covering all meaningful input combinations
- [ ] **Cross-blog dedup uses INSERT OR IGNORE:** First-seen blog is never overwritten
- [ ] **Per-blog dedup always active:** `--no-dedup` only bypasses cross-blog dedup, per-blog is unconditional
- [ ] **Pinned post excluded from cursor:** `TumblrBlogCrawler` skips the pinned post ID when computing `highest_post_id`, but still yields the post for download
- [ ] **FilterConfig validated at load time:** `validate_filter_config()` rejects both-false reblog config and since-after-until
- [ ] **No new dependencies:** All filters use stdlib only; dedup uses sqlite3 (stdlib); pinned resolver reuses httpx already in deps
- [ ] **CLI flags work:** `--tag`, `--exclude-tag`, `--since`, `--until`, `--originals-only`, `--reblogs-only`, `--no-dedup` all wired and validated
- [ ] **list/status subcommands work:** `tumbl4 list` shows managed blogs, `tumbl4 status <blog>` shows per-blog detail
- [ ] **Commit messages follow conventions:** `feat(scope)`, `test:`, `fix:` prefixes with `Co-Authored-By` trailer
