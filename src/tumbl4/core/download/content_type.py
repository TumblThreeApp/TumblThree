"""Content-Type to file-extension reconciliation.

When a server returns a Content-Type that disagrees with the URL's own
extension, we trust the server.  Unknown or absent Content-Types fall back
to whatever extension the URL already carries.
"""

from __future__ import annotations

from typing import Final

_MIME_TO_EXT: Final[dict[str, str]] = {
    "image/jpeg": "jpg",
    "image/png": "png",
    "image/gif": "gif",
    "image/webp": "webp",
    "image/tiff": "tiff",
    "image/heic": "heic",
    "image/avif": "avif",
    "video/mp4": "mp4",
    "video/webm": "webm",
    "audio/mpeg": "mp3",
    "audio/mp4": "m4a",
    "audio/ogg": "ogg",
}


def reconcile_extension(filename: str, content_type: str | None) -> str:
    """Return the correct file extension for *filename* given *content_type*.

    Rules (in priority order):
    1. If *content_type* is ``None`` or not in the known MIME map, keep the
       extension already present in *filename* (or the bare stem if there is
       none).
    2. Otherwise use the extension that corresponds to the MIME type, even if
       it differs from the URL's own extension.

    Parameters
    ----------
    filename:
        The basename as derived from the URL (e.g. ``"photo.jpg"``).
    content_type:
        The raw ``Content-Type`` header value, which may include parameters
        such as ``; charset=utf-8``.  Pass ``None`` when the header is absent.

    Returns
    -------
    str
        Full filename with the resolved extension (e.g. ``"photo.jpg"`` or
        ``"photo.png"``).
    """
    # Strip parameters like "; charset=utf-8"
    mime = content_type.split(";")[0].strip().lower() if content_type else None

    # Determine the extension the URL suggests (may be empty string).
    dot_pos = filename.rfind(".")
    url_ext = filename[dot_pos + 1 :] if dot_pos != -1 else ""
    stem = filename[: dot_pos] if dot_pos != -1 else filename

    if mime and mime in _MIME_TO_EXT:
        resolved_ext = _MIME_TO_EXT[mime]
        return f"{stem}.{resolved_ext}"

    # Unknown or absent content-type — keep whatever the URL had.
    if url_ext:
        return filename
    return stem
