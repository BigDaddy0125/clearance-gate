[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent

$requiredPaths = @(
    "docs/pilot-dry-run-checklist.md",
    "docs/pilot-rollback-note.md",
    "scripts/prepare-pilot-rollout.ps1",
    "scripts/run-deployment-smoke-check.ps1",
    "scripts/capture-pilot-sample-session.ps1",
    "scripts/prepare-post-pilot-review.ps1",
    "scripts/initialize-post-pilot-decision-memo.ps1",
    "examples/deployment/appsettings.Pilot.example.json",
    "examples/operations/operator-log-sample.jsonl"
)

foreach ($relativePath in $requiredPaths) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path $fullPath)) {
        throw "Controlled pilot readiness asset is missing at '$fullPath'."
    }
}

$readmePath = Join-Path $repoRoot "README.md"
$readmeText = Get-Content -Raw -Path $readmePath
$requiredReadmeMentions = @(
    "docs/pilot-dry-run-checklist.md",
    "docs/pilot-rollback-note.md",
    "scripts/prepare-pilot-rollout.ps1"
)

foreach ($mention in $requiredReadmeMentions) {
    if (-not $readmeText.Contains($mention)) {
        throw "README is missing controlled pilot readiness mention '$mention'."
    }
}

Write-Host "Controlled pilot readiness assets are complete and anchored."
