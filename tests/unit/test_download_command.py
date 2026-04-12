"""Tests for the 'tumbl4 download' CLI subcommand."""

from __future__ import annotations

from typer.testing import CliRunner

from tumbl4.cli.app import app

runner = CliRunner()


def test_download_appears_in_help() -> None:
    result = runner.invoke(app, ["--help"])
    assert result.exit_code == 0
    assert "download" in result.output


def test_download_subcommand_help_shows_blog_argument() -> None:
    result = runner.invoke(app, ["download", "--help"])
    assert result.exit_code == 0
    assert "blog" in result.output.lower()


def test_download_no_args_fails_missing_required() -> None:
    result = runner.invoke(app, ["download"])
    assert result.exit_code != 0


def test_download_with_blog_name_parses_cli() -> None:
    # The command will fail at runtime (no network), but CLI parsing must succeed.
    # We verify "Missing" is NOT in the output — meaning all required args were found.
    result = runner.invoke(app, ["download", "testblog"])
    assert "Missing" not in result.output


def test_download_invalid_page_size_fails() -> None:
    result = runner.invoke(app, ["download", "testblog", "--page-size", "0"])
    assert result.exit_code != 0


def test_download_invalid_image_size_fails() -> None:
    result = runner.invoke(app, ["download", "testblog", "--image-size", "9999"])
    assert result.exit_code != 0
