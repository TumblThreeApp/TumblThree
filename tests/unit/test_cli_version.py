"""Tests for tumbl4 CLI --version flag."""

from __future__ import annotations

from typer.testing import CliRunner

import tumbl4
from tumbl4.cli.app import app


runner = CliRunner()


def test_version_flag_prints_version_and_exits_zero() -> None:
    result = runner.invoke(app, ["--version"])
    assert result.exit_code == 0
    assert tumbl4.__version__ in result.stdout


def test_version_flag_short_form_prints_version() -> None:
    result = runner.invoke(app, ["-V"])
    assert result.exit_code == 0
    assert tumbl4.__version__ in result.stdout


def test_help_flag_exits_zero() -> None:
    result = runner.invoke(app, ["--help"])
    assert result.exit_code == 0
    assert "Usage:" in result.stdout


def test_no_args_shows_help_without_crashing() -> None:
    # With no subcommands registered yet, running bare `tumbl4` should not crash.
    # Typer's `no_args_is_help=True` historically exits with code 0 on some
    # versions and code 2 on others (Click-ish semantics). Both are acceptable
    # outcomes for Plan 1 — what we care about is that the output contains
    # "tumbl4" and that the process exits cleanly (not a stack trace).
    result = runner.invoke(app, [])
    assert result.exit_code in (0, 2), (
        f"unexpected exit code {result.exit_code}; output: {result.output!r}"
    )
    assert "tumbl4" in result.output or "Usage:" in result.output
