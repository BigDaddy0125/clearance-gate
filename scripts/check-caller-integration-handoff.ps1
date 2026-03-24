[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent

$requiredPaths = @(
    "docs/caller-onboarding-checklist.md",
    "docs/pilot-adapter-example.md",
    "docs/pilot-adapter-checklist.md",
    "docs/real-caller-rehearsal.md",
    "scripts/validate-pilot-adapter-example.ps1",
    "scripts/validate-real-caller-rehearsal-input.ps1",
    "scripts/prepare-caller-integration-review.ps1",
    "scripts/prepare-real-caller-rehearsal.ps1",
    "scripts/run-caller-integration-rehearsal.ps1",
    "examples/pilot-adapter/change-control-request.json",
    "examples/pilot-adapter/change-control-ack.json",
    "examples/pilot-adapter/mapped-authorize-request.json",
    "examples/pilot-adapter/mapped-acknowledge-request.json",
    "examples/pilot-adapter/convert-change-control-example.ps1"
)

foreach ($relativePath in $requiredPaths) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path $fullPath)) {
        throw "Caller integration handoff asset is missing at '$fullPath'."
    }
}

$readmeText = Get-Content -Raw -Path (Join-Path $repoRoot "README.md")
$requiredReadmeMentions = @(
    "docs/pilot-adapter-checklist.md",
    "scripts/validate-pilot-adapter-example.ps1",
    "scripts/validate-real-caller-rehearsal-input.ps1",
    "scripts/prepare-real-caller-rehearsal.ps1",
    "scripts/prepare-caller-integration-review.ps1",
    "scripts/run-caller-integration-rehearsal.ps1"
)

foreach ($mention in $requiredReadmeMentions) {
    if (-not $readmeText.Contains($mention)) {
        throw "README is missing caller integration handoff mention '$mention'."
    }
}

Write-Host "Caller integration handoff assets are complete and anchored."
