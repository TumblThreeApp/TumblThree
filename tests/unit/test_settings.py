"""Tests for the Settings model (Pydantic BaseSettings)."""

from __future__ import annotations

from pathlib import Path

import pytest
from pydantic import ValidationError

from tumbl4.models.settings import Settings


def test_settings_defaults_construct_without_error() -> None:
    s = Settings()
    assert s.output_dir is not None
    assert s.log_level == "INFO"
    assert s.max_concurrent_downloads == 4
    assert s.queue.max_pending_media == 200
    assert s.queue.max_pending_sidecars == 16
    assert s.http.connect_timeout == 10.0
    assert s.http.read_timeout == 60.0
    assert s.http.max_api_response_bytes == 32 * 1024 * 1024


def test_settings_accepts_env_overrides(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("TUMBL4_LOG_LEVEL", "DEBUG")
    monkeypatch.setenv("TUMBL4_MAX_CONCURRENT_DOWNLOADS", "8")
    s = Settings()
    assert s.log_level == "DEBUG"
    assert s.max_concurrent_downloads == 8


def test_settings_output_dir_coerces_to_path(tmp_path: Path) -> None:
    s = Settings(output_dir=str(tmp_path))
    assert s.output_dir == tmp_path
    assert isinstance(s.output_dir, Path)


def test_settings_rejects_invalid_log_level() -> None:
    with pytest.raises(ValidationError):
        Settings(log_level="NONSENSE")
