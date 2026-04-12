"""tumbl4 'download' subcommand — crawl and download a Tumblr blog."""

from __future__ import annotations

import asyncio
import logging
from pathlib import Path
from typing import Annotated

import typer

from tumbl4._internal.logging import SecretFilter, get_logger
from tumbl4._internal.paths import data_dir
from tumbl4.cli.output.progress import console
from tumbl4.core.crawl.http_client import TumblrHttpClient
from tumbl4.core.crawl.tumblr_blog import TumblrBlogCrawler
from tumbl4.core.orchestrator import run_crawl
from tumbl4.core.state.db import StateDb
from tumbl4.core.state.resume import load_cursor
from tumbl4.models.blog import BlogRef
from tumbl4.models.settings import Settings

__all__ = ["download"]

_VALID_IMAGE_SIZES = {"1280", "500", "400", "250", "100", "75"}
_PAGE_SIZE_MIN = 1
_PAGE_SIZE_MAX = 50

_log = get_logger("cli.download")


def _validate_page_size(value: int) -> int:
    if value < _PAGE_SIZE_MIN or value > _PAGE_SIZE_MAX:
        raise typer.BadParameter("page-size must be between 1 and 50")
    return value


def _validate_image_size(value: str) -> str:
    if value not in _VALID_IMAGE_SIZES:
        valid = ", ".join(sorted(_VALID_IMAGE_SIZES, key=int))
        raise typer.BadParameter(f"image-size must be one of: {valid}")
    return value


def _setup_logging(*, quiet: bool, verbose: bool) -> None:
    """Configure root tumbl4 logger level based on quiet/verbose flags."""
    if quiet:
        level = logging.ERROR
    elif verbose:
        level = logging.DEBUG
    else:
        level = logging.INFO

    root_logger = logging.getLogger("tumbl4")
    if not root_logger.handlers:
        handler = logging.StreamHandler()
        handler.addFilter(SecretFilter())
        formatter = logging.Formatter("%(levelname)s %(name)s: %(message)s")
        handler.setFormatter(formatter)
        root_logger.addHandler(handler)
    root_logger.setLevel(level)


async def _download_async(
    *,
    blog_input: str,
    output_dir: Path | None,
    page_size: int,
    image_size: str,
    no_resume: bool,
    quiet: bool,
    verbose: bool,
) -> None:
    """Async implementation of the download command."""
    _setup_logging(quiet=quiet, verbose=verbose)

    blog = BlogRef.from_input(blog_input)

    settings_kwargs: dict[str, object] = {}
    if output_dir is not None:
        settings_kwargs["output_dir"] = output_dir
    settings = Settings(**settings_kwargs)  # type: ignore[arg-type]

    # Load resume cursor unless --no-resume was passed.
    last_id = 0
    if not no_resume:
        db_dir = data_dir()
        db_dir.mkdir(parents=True, exist_ok=True)
        db_path = db_dir / f"{blog.name}.db"
        temp_db = StateDb(str(db_path))
        try:
            last_id = load_cursor(temp_db, blog.name)
        finally:
            temp_db.close()

    http_client = TumblrHttpClient(settings.http)
    try:
        crawler = TumblrBlogCrawler(
            http_client,
            blog,
            page_size=page_size,
            last_id=last_id,
            image_size=image_size,
        )

        if not quiet:
            console.print(f"[bold]Downloading[/bold] {blog.url}")

        result = await run_crawl(
            settings=settings,
            blog=blog,
            crawler=crawler,
            no_resume=no_resume,
        )
    finally:
        await http_client.aclose()

    if not quiet:
        if result.complete:
            status = "[green]complete[/green]"
        else:
            status = "[yellow]partial (rate limited)[/yellow]"
        console.print(
            f"\n[bold]{blog.name}[/bold] — {status}\n"
            f"  posts crawled:      {result.posts_crawled}\n"
            f"  downloads success:  {result.downloads_success}\n"
            f"  downloads failed:   {result.downloads_failed}\n"
            f"  downloads skipped:  {result.downloads_skipped}"
        )


def download(
    blog: Annotated[str, typer.Argument(help="Blog name or URL")],
    output_dir: Annotated[
        Path | None,
        typer.Option("--output-dir", "-o", help="Output directory for downloaded files"),
    ] = None,
    page_size: Annotated[
        int,
        typer.Option(
            "--page-size", help="Posts per API request (1-50)", callback=_validate_page_size
        ),
    ] = 50,
    image_size: Annotated[
        str,
        typer.Option(
            "--image-size",
            help="Preferred image size (1280, 500, 400, 250, 100, 75)",
            callback=_validate_image_size,
        ),
    ] = "1280",
    no_resume: Annotated[
        bool,
        typer.Option("--no-resume", help="Ignore saved resume cursor and start from the beginning"),
    ] = False,
    quiet: Annotated[
        bool, typer.Option("--quiet", "-q", help="Suppress all output except errors")
    ] = False,
    verbose: Annotated[bool, typer.Option("--verbose", "-v", help="Enable debug logging")] = False,
) -> None:
    """Download all photos from a Tumblr blog."""
    asyncio.run(
        _download_async(
            blog_input=blog,
            output_dir=output_dir,
            page_size=page_size,
            image_size=image_size,
            no_resume=no_resume,
            quiet=quiet,
            verbose=verbose,
        )
    )
