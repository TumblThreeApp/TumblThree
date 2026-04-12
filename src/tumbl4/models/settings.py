"""tumbl4 Settings model — Pydantic BaseSettings with env-var and config-file support.

Config precedence (highest → lowest) per spec §5.1:
    1. CLI flags
    2. Environment variables (TUMBL4_*)
    3. Project config (./tumbl4.toml) — wired in a later plan
    4. User config ($XDG_CONFIG_HOME/tumbl4/config.toml) — wired in a later plan
    5. Hardcoded defaults (this file)

In Plan 1 we only implement defaults + env-var overrides. TOML layers are
added in Plan 2 once we have a real config surface to validate.
"""

from __future__ import annotations

from pathlib import Path
from typing import Literal

from pydantic import BaseModel, Field
from pydantic_settings import BaseSettings, SettingsConfigDict

LogLevel = Literal["DEBUG", "INFO", "WARNING", "ERROR", "CRITICAL"]


class QueueSettings(BaseModel):
    max_pending_media: int = Field(default=200, ge=1, le=10_000)
    max_pending_sidecars: int = Field(default=16, ge=1, le=1_000)


class HttpSettings(BaseModel):
    connect_timeout: float = Field(default=10.0, gt=0.0)
    read_timeout: float = Field(default=60.0, gt=0.0)
    write_timeout: float = Field(default=30.0, gt=0.0)
    pool_timeout: float = Field(default=5.0, gt=0.0)
    max_connections: int = Field(default=32, ge=1)
    max_keepalive_connections: int = Field(default=16, ge=1)
    max_redirects: int = Field(default=5, ge=0, le=20)
    max_api_response_bytes: int = Field(default=32 * 1024 * 1024, ge=1024)
    user_agent_suffix: str = Field(
        default="+https://github.com/claire/tumbl4",
        description="Appended to User-Agent header after `tumbl4/{version}`.",
    )


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_prefix="TUMBL4_",
        env_nested_delimiter="__",
        env_file=None,
        extra="forbid",
    )

    output_dir: Path = Field(
        default_factory=lambda: Path.cwd() / "tumbl4-output",
        description="Where downloaded media and sidecars are written.",
    )
    log_level: LogLevel = "INFO"
    max_concurrent_downloads: int = Field(default=4, ge=1, le=32)
    queue: QueueSettings = Field(default_factory=QueueSettings)
    http: HttpSettings = Field(default_factory=HttpSettings)
