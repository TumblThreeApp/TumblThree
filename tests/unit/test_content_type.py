"""Tests for tumbl4.core.download.content_type.reconcile_extension."""

from __future__ import annotations

import pytest

from tumbl4.core.download.content_type import reconcile_extension


def test_matching_type_returns_original() -> None:
    """When Content-Type matches the URL extension, original filename is returned."""
    result = reconcile_extension("photo.jpg", "image/jpeg")
    assert result == "photo.jpg"


def test_png_url_with_jpeg_content_type_returns_jpg() -> None:
    """A .png URL served with image/jpeg Content-Type should produce .jpg."""
    result = reconcile_extension("photo.png", "image/jpeg")
    assert result == "photo.jpg"


def test_unknown_content_type_keeps_original() -> None:
    """An unrecognised MIME type falls back to the URL extension."""
    result = reconcile_extension("photo.jpg", "application/octet-stream")
    assert result == "photo.jpg"


def test_no_extension_uses_content_type() -> None:
    """A filename with no extension gets an extension from Content-Type."""
    result = reconcile_extension("photo", "image/png")
    assert result == "photo.png"


def test_gif_preserved_when_content_type_matches() -> None:
    """GIF files served with image/gif Content-Type keep the .gif extension."""
    result = reconcile_extension("anim.gif", "image/gif")
    assert result == "anim.gif"


def test_webp_content_type() -> None:
    """image/webp Content-Type produces a .webp extension."""
    result = reconcile_extension("image.jpg", "image/webp")
    assert result == "image.webp"


def test_content_type_with_charset_parameter() -> None:
    """Content-Type with '; charset=utf-8' is handled correctly."""
    result = reconcile_extension("photo.png", "image/jpeg; charset=utf-8")
    assert result == "photo.jpg"


def test_none_content_type_keeps_original() -> None:
    """When Content-Type is None, the original filename is returned unchanged."""
    result = reconcile_extension("photo.jpg", None)
    assert result == "photo.jpg"
