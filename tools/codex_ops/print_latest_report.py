"""Print the latest Codex Run Report as UTF-8 text."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def resolve_path(value: str) -> Path:
    path = Path(value)
    if path.is_absolute():
        return path
    return repo_root() / path


def configure_output() -> None:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    if hasattr(sys.stderr, "reconfigure"):
        sys.stderr.reconfigure(encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Print docs/codex/runs/latest.md.")
    parser.add_argument("--path", default="docs/codex/runs/latest.md")
    parser.add_argument("--max-chars", type=int, default=None)
    return parser.parse_args()


def main() -> int:
    configure_output()
    args = parse_args()
    report_path = resolve_path(args.path)

    if not report_path.exists():
        print(f"Run Report not found: {report_path}", file=sys.stderr)
        return 1
    if not report_path.is_file():
        print(f"Run Report path is not a file: {report_path}", file=sys.stderr)
        return 1

    try:
        text = report_path.read_text(encoding="utf-8")
    except UnicodeDecodeError as error:
        print(f"Run Report is not valid UTF-8: {report_path}: {error}", file=sys.stderr)
        return 1

    if args.max_chars is not None and args.max_chars < 0:
        print("--max-chars must be zero or greater.", file=sys.stderr)
        return 1

    if args.max_chars is not None and len(text) > args.max_chars:
        print(text[: args.max_chars], end="")
        print(f"\n\n[truncated: shown {args.max_chars} of {len(text)} chars]")
    else:
        print(text, end="")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
