"""Generate Codex Run Reports for the OmokaneEngine repository.

The report headings are intentionally fixed to ASCII English so the file stays
easy to share across tools that may not agree on Japanese heading handling.
"""

from __future__ import annotations

import argparse
import re
from datetime import datetime
from pathlib import Path
from typing import Iterable


DEFAULT_TITLE = "思兼神エンジン用 Run Report + Clipboard共有機構の追加"
DEFAULT_SLUG = "run-report-clipboard"


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def clean_slug(value: str) -> str:
    lowered = value.strip().lower()
    slug = re.sub(r"[^a-z0-9_-]+", "-", lowered).strip("-_")
    return slug or DEFAULT_SLUG


def clean_timestamp(value: str | None, generated_at_dt: datetime) -> str:
    if value is None:
        return generated_at_dt.strftime("%Y-%m-%d_%H%M")
    timestamp = value.strip()
    if not re.fullmatch(r"\d{4}-\d{2}-\d{2}_\d{4}", timestamp):
        raise ValueError("--timestamp must use YYYY-MM-DD_HHMM format.")
    return timestamp


def lines_or_default(lines: Iterable[str], default: str) -> list[str]:
    filtered = [line for line in lines if line.strip()]
    return filtered if filtered else [default]


def bullet_lines(lines: Iterable[str], default: str) -> str:
    return "\n".join(f"- {line}" for line in lines_or_default(lines, default))


def plain_lines(lines: Iterable[str], default: str) -> str:
    return "\n".join(lines_or_default(lines, default))


def build_report(args: argparse.Namespace, generated_at: str) -> str:
    summary = args.summary or [
        "思兼神エンジンのCodex運用補助機構として、Run Report + Clipboard共有機構を追加するサンプルレポートです。",
        "今回の補助機構はゲームエンジン本体ではなく、Codex作業結果をMarkdownで共有するためのものです。",
        "思兼神Core、天照Renderer、建御雷Battle、猿田彦Input、天鳥船Motion、言霊Event、禊Testなどの本体サブシステムは変更していません。",
    ]

    changed_files = args.changed_file or [
        "docs/codex/runs/latest.md",
        "tools/codex_ops/write_run_report.py",
        "tools/codex_ops/print_latest_report.py",
        "tools/codex_ops/copy_latest_report.py",
        "scripts/copy_latest_run_report.ps1",
        "docs/codex/workflow_rules.md",
        "AGENTS.md",
    ]

    warnings = args.warning or [
        "このRun Reportはサンプル生成またはCodex作業報告用です。",
        "ゲーム本体、F#ソース、固定tick更新、描画、戦闘、入力、既存テストの仕様変更は行っていません。",
    ]

    sections = [
        "# Codex Run Report",
        "",
        f"title: {args.title}",
        "",
        f"generated_at: {generated_at}",
        "",
        "## Summary",
        "",
        bullet_lines(summary, "No summary."),
        "",
        "## Changed Files",
        "",
        bullet_lines(changed_files, "No changed files."),
        "",
        "## Verification Commands",
        "",
        plain_lines(args.verification_command or [], "Not run yet."),
        "",
        "## Verification Result",
        "",
        args.verification_result or "Not run yet.",
        "",
        "## Errors",
        "",
        bullet_lines(args.error or [], "None."),
        "",
        "## Warnings",
        "",
        bullet_lines(warnings, "None."),
        "",
        "## Next Candidates",
        "",
        bullet_lines(args.next_candidate or [], "None."),
        "",
        "## Human Confirmation",
        "",
        args.human_confirmation or "Codex作業結果を人間が確認してください。",
        "",
    ]
    return "\n".join(sections)


def write_report(args: argparse.Namespace) -> tuple[Path, Path]:
    root = repo_root()
    runs_dir = root / "docs" / "codex" / "runs"
    runs_dir.mkdir(parents=True, exist_ok=True)

    generated_at_dt = datetime.now().astimezone().replace(microsecond=0)
    generated_at = generated_at_dt.isoformat()
    timestamp = clean_timestamp(args.timestamp, generated_at_dt)
    slug = clean_slug(args.slug)

    content = build_report(args, generated_at)
    report_path = runs_dir / f"{timestamp}_{slug}.md"
    latest_path = runs_dir / "latest.md"

    report_path.write_text(content, encoding="utf-8")
    latest_path.write_text(content, encoding="utf-8")
    return report_path, latest_path


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate a Codex Run Report and update docs/codex/runs/latest.md."
    )
    parser.add_argument("--title", default=DEFAULT_TITLE)
    parser.add_argument("--slug", default=DEFAULT_SLUG)
    parser.add_argument("--timestamp", default=None)
    parser.add_argument("--summary", action="append")
    parser.add_argument("--changed-file", action="append", default=[])
    parser.add_argument("--verification-command", action="append", default=[])
    parser.add_argument("--verification-result", default="")
    parser.add_argument("--error", action="append", default=[])
    parser.add_argument("--warning", action="append", default=[])
    parser.add_argument("--next-candidate", action="append", default=[])
    parser.add_argument("--human-confirmation", default="")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    report_path, latest_path = write_report(args)
    root = repo_root()
    print(f"Wrote Run Report: {report_path.relative_to(root)}")
    print(f"Updated latest Run Report: {latest_path.relative_to(root)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
