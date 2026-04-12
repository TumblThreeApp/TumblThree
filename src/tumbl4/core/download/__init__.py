"""Streaming media downloader with Content-Type extension reconciliation.

Public surface:
- ``download_media`` — download a single :class:`~tumbl4.models.media.MediaTask`
  to disk using atomic ``.part`` + rename semantics.
- ``reconcile_extension`` — map a (filename, content-type) pair to the
  correct file extension, preferring the server-reported MIME type.
"""
