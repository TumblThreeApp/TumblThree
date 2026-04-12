"""Supervised asyncio task spawning for tumbl4.

Spec §6.11 requires that every `asyncio.create_task()` call in tumbl4 goes
through this helper. The helper:

  1. Creates the task via `asyncio.create_task()`.
  2. Adds the task to a module-level tracked set so it is not garbage-collected
     mid-run (a classic asyncio footgun).
  3. Attaches a done-callback that removes the task from the set and logs any
     exception — except `CancelledError`, which is expected on cooperative
     cancel and must not be logged as an error.

Contributors: use `spawn()` instead of raw `asyncio.create_task()`. The ruff
rule `RUF006` enforces this at lint time.
"""

from __future__ import annotations

import asyncio
import logging
from collections.abc import Coroutine
from typing import Any

_LOG = logging.getLogger(__name__)

_tracked: set[asyncio.Task[Any]] = set()


def spawn[T](coro: Coroutine[Any, Any, T], *, name: str | None = None) -> asyncio.Task[T]:
    """Create and supervise an asyncio task.

    Args:
        coro: The coroutine to run as a task.
        name: Optional task name (propagated to asyncio.Task for debug output).

    Returns:
        The created task. The task is tracked internally until it finishes;
        callers may still `await` it directly if they care about its result.
    """
    task: asyncio.Task[T] = asyncio.create_task(coro, name=name)
    _tracked.add(task)
    task.add_done_callback(_on_task_done)
    return task


def _on_task_done(task: asyncio.Task[Any]) -> None:
    _tracked.discard(task)
    if task.cancelled():
        return
    exc = task.exception()
    if exc is None:
        return
    _LOG.error(
        "supervised task %r failed with unhandled exception: %s",
        task.get_name(),
        exc,
        exc_info=exc,
    )


def _live_tasks() -> frozenset[asyncio.Task[Any]]:
    """Test-only view of currently tracked tasks.

    Not part of the public API. Used by tests/unit/test_tasks.py to assert
    tracking behavior.
    """
    return frozenset(_tracked)
