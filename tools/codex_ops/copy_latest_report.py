"""Copy the latest Codex Run Report to the clipboard when supported."""

from __future__ import annotations

import argparse
import platform
import subprocess
import sys
from pathlib import Path


MANUAL_COPY_COMMAND = (
    r"Get-Content .\docs\codex\runs\latest.md -Raw -Encoding UTF8 | Set-Clipboard"
)


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def resolve_path(value: str) -> Path:
    path = Path(value)
    if path.is_absolute():
        return path
    return repo_root() / path


def print_manual_copy() -> None:
    print("Manual copy command:")
    print(MANUAL_COPY_COMMAND)


def powershell_single_quote(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def copy_on_windows(report_path: Path) -> tuple[bool, str]:
    powershell = "powershell.exe"
    command = (
        "Get-Content -LiteralPath "
        f"{powershell_single_quote(str(report_path))} "
        "-Raw -Encoding UTF8 | Set-Clipboard"
    )
    try:
        completed = subprocess.run(
            [powershell, "-NoProfile", "-Command", command],
            capture_output=True,
            check=False,
            text=True,
        )
    except OSError as error:
        return False, str(error)

    if completed.returncode != 0:
        detail = completed.stderr.strip() or completed.stdout.strip()
        return False, detail or f"PowerShell exited with {completed.returncode}."
    return True, "Copied latest Run Report to clipboard."


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Copy docs/codex/runs/latest.md.")
    parser.add_argument("--path", default="docs/codex/runs/latest.md")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    report_path = resolve_path(args.path)

    if not report_path.exists():
        print(f"Run Report not found: {report_path}", file=sys.stderr)
        print_manual_copy()
        return 1
    if not report_path.is_file():
        print(f"Run Report path is not a file: {report_path}", file=sys.stderr)
        print_manual_copy()
        return 1

    try:
        text = report_path.read_text(encoding="utf-8")
    except UnicodeDecodeError as error:
        print(f"Run Report is not valid UTF-8: {report_path}: {error}", file=sys.stderr)
        print_manual_copy()
        return 1

    if platform.system() != "Windows":
        print("Clipboard copy is not supported by this script on this platform.")
        print(f"latest.md is readable as UTF-8: {report_path}")
        print_manual_copy()
        return 0

    copied, message = copy_on_windows(report_path)
    if copied:
        print(message)
        return 0

    print(f"Clipboard copy failed: {message}", file=sys.stderr)
    print(f"latest.md is readable as UTF-8: {report_path}")
    print_manual_copy()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
