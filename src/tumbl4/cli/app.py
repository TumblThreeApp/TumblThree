"""Top-level Typer application for tumbl4.

This module exposes `app` (the Typer instance) and `main()` (the function
referenced by the `tumbl4` console script in pyproject.toml).

Later plans add subcommands (download, login, logout, list, config, status,
sweep) under `src/tumbl4/cli/commands/`. In Plan 1 we only wire up --version
and --help so the CLI entry point is installable and testable.
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
    # The callback body is only reached when no --version flag was given.
    # We currently have no global options beyond --version, so this is a no-op.
    return None


from tumbl4.cli.commands.download import download  # noqa: E402

app.command()(download)


def main() -> None:
    """Console-script entry point referenced from pyproject.toml."""
    app()
