$ErrorActionPreference = "Stop"

$ManualCopyCommand = "Get-Content .\docs\codex\runs\latest.md -Raw -Encoding UTF8 | Set-Clipboard"

try {
    $ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $RepoRoot = Split-Path -Parent $ScriptDir
    $ReportPath = Join-Path $RepoRoot "docs\codex\runs\latest.md"

    if (-not (Test-Path -LiteralPath $ReportPath -PathType Leaf)) {
        throw "Run Report not found: $ReportPath"
    }

    Get-Content -LiteralPath $ReportPath -Raw -Encoding UTF8 | Set-Clipboard
    Write-Output "Copied latest Run Report to clipboard: $ReportPath"
    exit 0
}
catch {
    Write-Error "Failed to copy latest Run Report: $($_.Exception.Message)"
    Write-Output "Manual copy command:"
    Write-Output $ManualCopyCommand
    exit 1
}
