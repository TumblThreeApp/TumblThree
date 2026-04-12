"""Shared pytest fixtures for tumbl4 tests."""

from __future__ import annotations

from collections.abc import Iterator
from pathlib import Path

import pytest


@pytest.fixture
def tmp_output_dir(tmp_path: Path) -> Iterator[Path]:
    """Return a temporary output directory for tests that write files."""
    out = tmp_path / "output"
    out.mkdir()
    yield out
