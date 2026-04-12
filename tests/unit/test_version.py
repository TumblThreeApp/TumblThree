"""Smoke test that the package exposes a version string."""

from __future__ import annotations

import re
import tomllib
from pathlib import Path

import tumbl4


def test_version_is_semver_string() -> None:
    assert hasattr(tumbl4, "__version__")
    assert isinstance(tumbl4.__version__, str)
    # SemVer 2.0 regex (without build metadata) — matches MAJOR.MINOR.PATCH[-PRERELEASE]
    pattern = r"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$"
    assert re.match(pattern, tumbl4.__version__), (
        f"__version__ {tumbl4.__version__!r} is not valid SemVer"
    )


def test_version_matches_pyproject() -> None:
    # Ensures the __init__.py version and the pyproject.toml version do not drift.
    # We intentionally parse pyproject.toml directly rather than importing
    # build-time metadata, so this test catches the case where someone bumps
    # one but forgets the other.
    pyproject = Path(__file__).resolve().parents[2] / "pyproject.toml"
    data = tomllib.loads(pyproject.read_text(encoding="utf-8"))
    assert data["project"]["version"] == tumbl4.__version__
