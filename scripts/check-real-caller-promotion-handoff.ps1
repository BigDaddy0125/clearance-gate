[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent

$requiredPaths = @(
    "docs/real-caller-promotion.md",
    "scripts/create-sample-real-caller-intake.ps1",
    "scripts/validate-real-caller-intake-package.ps1",
    "scripts/promote-real-caller-intake.ps1"
)

foreach ($relativePath in $requiredPaths) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path $fullPath)) {
        throw "Real caller promotion asset is missing at '$fullPath'."
    }
}

$readmeText = Get-Content -Raw -Path (Join-Path $repoRoot "README.md")
$requiredReadmeMentions = @(
    "docs/real-caller-promotion.md",
    "scripts/create-sample-real-caller-intake.ps1",
    "scripts/validate-real-caller-intake-package.ps1",
    "scripts/promote-real-caller-intake.ps1"
)

foreach ($mention in $requiredReadmeMentions) {
    if (-not $readmeText.Contains($mention)) {
        throw "README is missing real caller promotion mention '$mention'."
    }
}

Write-Host "Real caller promotion handoff assets are complete and anchored."
