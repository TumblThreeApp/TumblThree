"""Tests for Blog, BlogRef, Post, MediaTask, and DownloadResult models."""

from __future__ import annotations

import hashlib
from pathlib import Path

import pytest
from pydantic import ValidationError

from tumbl4.models.blog import Blog, BlogRef
from tumbl4.models.media import DownloadResult, MediaTask
from tumbl4.models.post import Post

# ---------------------------------------------------------------------------
# BlogRef
# ---------------------------------------------------------------------------


class TestBlogRefFromInput:
    def test_bare_name(self) -> None:
        ref = BlogRef.from_input("photography")
        assert ref.name == "photography"
        assert ref.url == "https://photography.tumblr.com/"

    def test_bare_name_uppercased_is_normalised(self) -> None:
        ref = BlogRef.from_input("Photography")
        assert ref.name == "photography"
        assert ref.url == "https://photography.tumblr.com/"

    def test_subdomain_url_with_trailing_slash(self) -> None:
        ref = BlogRef.from_input("https://photography.tumblr.com/")
        assert ref.name == "photography"
        assert ref.url == "https://photography.tumblr.com/"

    def test_subdomain_url_without_trailing_slash(self) -> None:
        ref = BlogRef.from_input("https://photography.tumblr.com")
        assert ref.name == "photography"
        assert ref.url == "https://photography.tumblr.com/"

    def test_www_url(self) -> None:
        ref = BlogRef.from_input("https://www.tumblr.com/photography")
        assert ref.name == "photography"
        assert ref.url == "https://photography.tumblr.com/"

    def test_www_url_with_trailing_slash(self) -> None:
        ref = BlogRef.from_input("https://www.tumblr.com/photography/")
        assert ref.name == "photography"
        assert ref.url == "https://photography.tumblr.com/"


class TestBlogRefFrozen:
    def test_is_frozen(self) -> None:
        ref = BlogRef.from_input("photography")
        with pytest.raises((AttributeError, TypeError)):
            ref.name = "other"  # type: ignore[misc]

    def test_is_hashable(self) -> None:
        ref = BlogRef.from_input("photography")
        # Frozen dataclasses are hashable
        assert hash(ref) is not None
        s: set[BlogRef] = {ref}
        assert ref in s


# ---------------------------------------------------------------------------
# Blog
# ---------------------------------------------------------------------------


class TestBlogModel:
    def test_minimal_construction(self) -> None:
        blog = Blog(name="photography", url="https://photography.tumblr.com/")
        assert blog.name == "photography"
        assert blog.url == "https://photography.tumblr.com/"
        assert blog.title is None
        assert blog.total_posts == 0

    def test_full_construction(self) -> None:
        blog = Blog(
            name="photography",
            url="https://photography.tumblr.com/",
            title="Beautiful Photos",
            total_posts=1234,
        )
        assert blog.title == "Beautiful Photos"
        assert blog.total_posts == 1234


# ---------------------------------------------------------------------------
# Post
# ---------------------------------------------------------------------------


class TestPostModel:
    def _minimal(self) -> Post:
        return Post(
            post_id="123456",
            blog_name="photography",
            post_url="https://photography.tumblr.com/post/123456",
            post_type="photo",
            timestamp_utc="2024-01-15T12:00:00Z",
        )

    def test_minimal_construction(self) -> None:
        post = self._minimal()
        assert post.post_id == "123456"
        assert post.blog_name == "photography"
        assert post.post_type == "photo"
        assert post.tags == []
        assert post.is_reblog is False
        assert post.reblog_source_blog is None
        assert post.reblog_source_post_id is None
        assert post.title is None
        assert post.body_text is None
        assert post.body_html is None
        assert post.media_count == 0

    def test_with_tags(self) -> None:
        post = Post(
            post_id="123456",
            blog_name="photography",
            post_url="https://photography.tumblr.com/post/123456",
            post_type="photo",
            timestamp_utc="2024-01-15T12:00:00Z",
            tags=["nature", "landscape"],
        )
        assert post.tags == ["nature", "landscape"]

    def test_reblog_fields(self) -> None:
        post = Post(
            post_id="123456",
            blog_name="photography",
            post_url="https://photography.tumblr.com/post/123456",
            post_type="photo",
            timestamp_utc="2024-01-15T12:00:00Z",
            is_reblog=True,
            reblog_source_blog="originalblog",
            reblog_source_post_id="999999",
        )
        assert post.is_reblog is True
        assert post.reblog_source_blog == "originalblog"
        assert post.reblog_source_post_id == "999999"

    def test_invalid_post_type_rejected(self) -> None:
        with pytest.raises(ValidationError):
            Post(
                post_id="123456",
                blog_name="photography",
                post_url="https://photography.tumblr.com/post/123456",
                post_type="invalid_type",  # type: ignore[arg-type]
                timestamp_utc="2024-01-15T12:00:00Z",
            )

    def test_all_valid_post_types(self) -> None:
        valid_types = ["photo", "video", "audio", "text", "quote", "link", "answer"]
        for pt in valid_types:
            post = Post(
                post_id="123",
                blog_name="blog",
                post_url="https://blog.tumblr.com/post/123",
                post_type=pt,  # type: ignore[arg-type]
                timestamp_utc="2024-01-15T12:00:00Z",
            )
            assert post.post_type == pt


# ---------------------------------------------------------------------------
# MediaTask
# ---------------------------------------------------------------------------


class TestMediaTask:
    def _make_task(self, url: str = "https://example.com/media/photo.jpg") -> MediaTask:
        return MediaTask(
            url=url,
            post_id="123456",
            blog_name="photography",
            index=0,
            output_dir="/tmp/tumbl4-output/photography",
        )

    def test_url_hash_is_sha256(self) -> None:
        url = "https://example.com/media/photo.jpg"
        task = self._make_task(url)
        expected = hashlib.sha256(url.encode()).hexdigest()
        assert task.url_hash == expected

    def test_url_hash_changes_with_url(self) -> None:
        task_a = self._make_task("https://example.com/a.jpg")
        task_b = self._make_task("https://example.com/b.jpg")
        assert task_a.url_hash != task_b.url_hash

    def test_filename_format(self) -> None:
        task = self._make_task("https://example.com/media/photo.jpg")
        assert task.filename == "123456_00.jpg"

    def test_filename_index_padding(self) -> None:
        task = MediaTask(
            url="https://example.com/media/photo.png",
            post_id="789",
            blog_name="blog",
            index=5,
            output_dir="/tmp/out",
        )
        assert task.filename == "789_05.png"

    def test_filename_index_two_digits(self) -> None:
        task = MediaTask(
            url="https://example.com/media/video.mp4",
            post_id="789",
            blog_name="blog",
            index=12,
            output_dir="/tmp/out",
        )
        assert task.filename == "789_12.mp4"

    def test_final_path(self) -> None:
        task = self._make_task("https://example.com/media/photo.jpg")
        assert task.final_path == Path("/tmp/tumbl4-output/photography/123456_00.jpg")

    def test_part_path(self) -> None:
        task = self._make_task("https://example.com/media/photo.jpg")
        assert task.part_path == Path("/tmp/tumbl4-output/photography/123456_00.jpg.part")

    def test_url_with_no_extension(self) -> None:
        task = self._make_task("https://example.com/media/photo")
        # ext should be empty string, filename ends with just the post_id_index
        assert task.filename == "123456_00"

    def test_url_with_query_string(self) -> None:
        # Extension should come from the path component only, not query params
        task = self._make_task("https://example.com/media/photo.jpg?w=500&h=300")
        assert task.filename == "123456_00.jpg"


# ---------------------------------------------------------------------------
# DownloadResult
# ---------------------------------------------------------------------------


class TestDownloadResult:
    def test_success_result(self) -> None:
        result = DownloadResult(
            url="https://example.com/photo.jpg",
            post_id="123456",
            filename="123456_00.jpg",
            byte_count=204800,
            status="success",
        )
        assert result.status == "success"
        assert result.byte_count == 204800
        assert result.filename == "123456_00.jpg"
        assert result.error is None

    def test_failed_result(self) -> None:
        result = DownloadResult(
            url="https://example.com/photo.jpg",
            post_id="123456",
            filename=None,
            byte_count=0,
            status="failed",
            error="HTTP 404 Not Found",
        )
        assert result.status == "failed"
        assert result.filename is None
        assert result.byte_count == 0
        assert result.error == "HTTP 404 Not Found"

    def test_invalid_status_rejected(self) -> None:
        with pytest.raises(ValidationError):
            DownloadResult(
                url="https://example.com/photo.jpg",
                post_id="123456",
                filename=None,
                byte_count=0,
                status="pending",  # type: ignore[arg-type]
            )
