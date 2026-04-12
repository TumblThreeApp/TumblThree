"""XDG Base Directory Specification-compliant paths for tumbl4.

On Linux we follow XDG strictly. On macOS we also honor XDG env vars when set
(so a user can opt in to Linux-style locations), but the defaults fall back to
macOS-native `~/Library/...` conventions when no XDG env vars are present.

All functions return `Path` objects. Callers are responsible for creating
directories on first use — these functions only compute paths, they never
touch the filesystem.

See spec §5.1 (config precedence) and §6.6 (credential storage locations).
"""

from __future__ import annotations

import os
import sys
from pathlib import Path

_APP_NAME = "tumbl4"


def _home() -> Path:
    return Path(os.environ.get("HOME", "~")).expanduser()


def config_dir() -> Path:
    """Return the directory where tumbl4 looks for its user config file.

    Resolution order:
        1. $XDG_CONFIG_HOME/tumbl4
        2. (macOS only, if XDG unset) ~/Library/Application Support/tumbl4
        3. ~/.config/tumbl4
    """
    xdg = os.environ.get("XDG_CONFIG_HOME")
    if xdg:
        return Path(xdg) / _APP_NAME
    if sys.platform == "darwin":
        return _home() / "Library" / "Application Support" / _APP_NAME
    return _home() / ".config" / _APP_NAME


def state_dir() -> Path:
    """Return the directory where tumbl4 stores runtime state (sessions, cursors).

    Resolution order:
        1. $XDG_STATE_HOME/tumbl4
        2. (macOS only, if XDG unset) ~/Library/Application Support/tumbl4/state
        3. ~/.local/state/tumbl4
    """
    xdg = os.environ.get("XDG_STATE_HOME")
    if xdg:
        return Path(xdg) / _APP_NAME
    if sys.platform == "darwin":
        return _home() / "Library" / "Application Support" / _APP_NAME / "state"
    return _home() / ".local" / "state" / _APP_NAME


def data_dir() -> Path:
    """Return the directory where tumbl4 stores long-lived data (dedup DB).

    Resolution order:
        1. $XDG_DATA_HOME/tumbl4
        2. (macOS only, if XDG unset) ~/Library/Application Support/tumbl4/data
        3. ~/.local/share/tumbl4
    """
    xdg = os.environ.get("XDG_DATA_HOME")
    if xdg:
        return Path(xdg) / _APP_NAME
    if sys.platform == "darwin":
        return _home() / "Library" / "Application Support" / _APP_NAME / "data"
    return _home() / ".local" / "share" / _APP_NAME


def playwright_state_file() -> Path:
    """Return the path to the Playwright storage_state JSON (chmod 0600 on write)."""
    return state_dir() / "playwright_state.json"


def browser_profile_dir() -> Path:
    """Return the path to the Playwright browser profile directory (chmod 0700)."""
    return state_dir() / "browser_profile"


def dedup_db() -> Path:
    """Return the path to the shared cross-blog dedup SQLite database."""
    return data_dir() / "dedup.db"
