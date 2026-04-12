"""Structural test: tumbl4.core must not import from tumbl4.cli.

Spec §3 core property: "Unidirectional dependencies — cli → core.orchestrator
→ core.* → models. Nothing imports upward." This test walks the source tree
and catches any violation at test time. It complements any lint rule that
may be added later.
"""

from __future__ import annotations

import ast
from pathlib import Path


def _repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def _iter_python_files(root: Path) -> list[Path]:
    return sorted(root.rglob("*.py"))


def _module_imports(source: str) -> set[str]:
    """Return every fully-qualified module name imported by this source file."""
    try:
        tree = ast.parse(source)
    except SyntaxError:
        return set()
    names: set[str] = set()
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            for alias in node.names:
                names.add(alias.name)
        elif isinstance(node, ast.ImportFrom) and node.module:
            names.add(node.module)
    return names


def test_core_does_not_import_from_cli() -> None:
    core_dir = _repo_root() / "src" / "tumbl4" / "core"
    if not core_dir.exists():
        # In Plan 1 core/ is just a marker __init__.py. The test still runs
        # and passes trivially. Later plans add modules under core/.
        return
    violations: list[tuple[Path, str]] = []
    for path in _iter_python_files(core_dir):
        source = path.read_text(encoding="utf-8")
        for imported in _module_imports(source):
            if imported.startswith("tumbl4.cli"):
                violations.append((path, imported))
    assert not violations, (
        f"tumbl4.core modules must not import from tumbl4.cli; violations: {violations}"
    )


def test_models_does_not_import_from_cli_or_core() -> None:
    """Models are the shared vocabulary — they import from nothing in tumbl4."""
    models_dir = _repo_root() / "src" / "tumbl4" / "models"
    if not models_dir.exists():
        return
    violations: list[tuple[Path, str]] = []
    for path in _iter_python_files(models_dir):
        source = path.read_text(encoding="utf-8")
        for imported in _module_imports(source):
            if imported.startswith("tumbl4.cli") or imported.startswith("tumbl4.core"):
                violations.append((path, imported))
    assert not violations, (
        f"tumbl4.models must not import from cli or core; violations: {violations}"
    )
