"""Tests for tumbl4._internal.tasks.spawn — supervised asyncio task creation."""

from __future__ import annotations

import asyncio
import logging

import pytest

from tumbl4._internal import tasks


async def _ok_coro() -> str:
    await asyncio.sleep(0)
    return "hello"


async def _raising_coro() -> None:
    await asyncio.sleep(0)
    raise RuntimeError("synthetic failure")


# No @pytest.mark.asyncio needed — pyproject.toml sets asyncio_mode = "auto",
# which auto-decorates every `async def test_*` in the suite.


async def test_spawn_returns_task_and_tracks_it() -> None:
    task = tasks.spawn(_ok_coro())
    assert isinstance(task, asyncio.Task)
    # The task should be in the tracked set while still running.
    assert task in tasks._live_tasks()
    result = await task
    assert result == "hello"
    # After completion, supervision removes it from the tracked set.
    await asyncio.sleep(0)  # let done-callback run
    assert task not in tasks._live_tasks()


async def test_spawn_logs_exception_from_failing_task(
    caplog: pytest.LogCaptureFixture,
) -> None:
    caplog.set_level(logging.ERROR, logger="tumbl4._internal.tasks")
    task = tasks.spawn(_raising_coro())
    with pytest.raises(RuntimeError, match="synthetic failure"):
        await task
    await asyncio.sleep(0)  # let done-callback run
    # Supervision should have logged the exception via the done-callback.
    assert any("synthetic failure" in record.getMessage() for record in caplog.records), (
        f"expected error log containing 'synthetic failure', "
        f"got {[r.getMessage() for r in caplog.records]}"
    )


async def test_spawn_handles_cancellation_without_logging_as_error(
    caplog: pytest.LogCaptureFixture,
) -> None:
    caplog.set_level(logging.ERROR, logger="tumbl4._internal.tasks")

    async def long_coro() -> None:
        await asyncio.sleep(10)

    task = tasks.spawn(long_coro())
    task.cancel()
    with pytest.raises(asyncio.CancelledError):
        await task
    await asyncio.sleep(0)
    # Cancellation is expected, not a surprise; do not log as error.
    assert not any(record.levelno == logging.ERROR for record in caplog.records), (
        "cancellation should not produce an error log"
    )
