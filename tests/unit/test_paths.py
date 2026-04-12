"""Tests for tumbl4._internal.paths (XDG base directory resolution)."""

from __future__ import annotations

import sys
from pathlib import Path

import pytest

from tumbl4._internal import paths


def test_config_dir_respects_xdg_config_home(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    monkeypatch.setenv("XDG_CONFIG_HOME", str(tmp_path / "xdg-config"))
    result = paths.config_dir()
    assert result == tmp_path / "xdg-config" / "tumbl4"


def test_state_dir_respects_xdg_state_home(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    monkeypatch.setenv("XDG_STATE_HOME", str(tmp_path / "xdg-state"))
    result = paths.state_dir()
    assert result == tmp_path / "xdg-state" / "tumbl4"


def test_data_dir_respects_xdg_data_home(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    monkeypatch.setenv("XDG_DATA_HOME", str(tmp_path / "xdg-data"))
    result = paths.data_dir()
    assert result == tmp_path / "xdg-data" / "tumbl4"


def test_config_dir_linux_default(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    monkeypatch.delenv("XDG_CONFIG_HOME", raising=False)
    monkeypatch.setenv("HOME", str(tmp_path))
    if sys.platform == "darwin":
        # On macOS the platform-specific fallback is used
        result = paths.config_dir()
        assert "tumbl4" in str(result)
    else:
        result = paths.config_dir()
        assert result == tmp_path / ".config" / "tumbl4"


def test_playwright_state_file_is_under_state_dir(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    monkeypatch.setenv("XDG_STATE_HOME", str(tmp_path / "xdg-state"))
    result = paths.playwright_state_file()
    assert result == tmp_path / "xdg-state" / "tumbl4" / "playwright_state.json"


def test_browser_profile_dir_is_under_state_dir(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    monkeypatch.setenv("XDG_STATE_HOME", str(tmp_path / "xdg-state"))
    result = paths.browser_profile_dir()
    assert result == tmp_path / "xdg-state" / "tumbl4" / "browser_profile"


def test_dedup_db_is_under_data_dir(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    monkeypatch.setenv("XDG_DATA_HOME", str(tmp_path / "xdg-data"))
    result = paths.dedup_db()
    assert result == tmp_path / "xdg-data" / "tumbl4" / "dedup.db"
