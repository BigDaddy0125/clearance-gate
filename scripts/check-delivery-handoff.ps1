[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent

$requiredPaths = @(
    "docs/operator-logging-guide.md",
    "docs/operator-triage-cheatsheet.md",
    "docs/deployment-runbook.md",
    "docs/release-readiness.md",
    "docs/pilot-execution-checklist.md",
    "docs/pilot-incident-response.md",
    "docs/pilot-evidence-package.md",
    "docs/post-pilot-review-flow.md",
    "scripts/run-deployment-smoke-check.ps1",
    "scripts/package-pilot-evidence.ps1",
    "scripts/capture-pilot-sample-session.ps1",
    "scripts/prepare-release-review.ps1",
    "scripts/prepare-post-pilot-review.ps1",
    "scripts/initialize-post-pilot-decision-memo.ps1",
    "examples/deployment/appsettings.LocalValidation.example.json",
    "examples/deployment/appsettings.Pilot.example.json",
    "examples/deployment/appsettings.Production.example.json",
    "examples/operations/operator-log-sample.jsonl"
)

foreach ($relativePath in $requiredPaths) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path $fullPath)) {
        throw "Required delivery handoff asset is missing at '$fullPath'."
    }
}

$readmePath = Join-Path $repoRoot "README.md"
$readmeText = Get-Content -Raw -Path $readmePath
$requiredReadmeMentions = @(
    "docs/operator-triage-cheatsheet.md",
    "scripts/prepare-release-review.ps1",
    "scripts/capture-pilot-sample-session.ps1",
    "scripts/initialize-post-pilot-decision-memo.ps1",
    "examples/operations/operator-log-sample.jsonl"
)

foreach ($mention in $requiredReadmeMentions) {
    if (-not $readmeText.Contains($mention)) {
        throw "README is missing delivery handoff mention '$mention'."
    }
}

Write-Host "Delivery handoff assets are complete and anchored."
