[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent

$requiredPaths = @(
    "docs/real-caller-intake.md",
    "docs/real-caller-rehearsal.md",
    "scripts/initialize-real-caller-intake.ps1",
    "scripts/validate-real-caller-rehearsal-input.ps1",
    "scripts/prepare-real-caller-rehearsal.ps1",
    "examples/real-caller-intake/caller-authorize.template.json",
    "examples/real-caller-intake/caller-acknowledge.template.json",
    "examples/real-caller-intake/intake-manifest.template.json"
)

foreach ($relativePath in $requiredPaths) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path $fullPath)) {
        throw "Real caller intake asset is missing at '$fullPath'."
    }
}

$readmeText = Get-Content -Raw -Path (Join-Path $repoRoot "README.md")
$requiredReadmeMentions = @(
    "docs/real-caller-intake.md",
    "scripts/initialize-real-caller-intake.ps1",
    "scripts/validate-real-caller-rehearsal-input.ps1",
    "scripts/prepare-real-caller-rehearsal.ps1"
)

foreach ($mention in $requiredReadmeMentions) {
    if (-not $readmeText.Contains($mention)) {
        throw "README is missing real caller intake mention '$mention'."
    }
}

Write-Host "Real caller intake handoff assets are complete and anchored."
