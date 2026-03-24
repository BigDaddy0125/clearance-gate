[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent

$requiredPaths = @(
    "docs/real-caller-substitution.md",
    "scripts/prepare-real-caller-substitution.ps1"
)

foreach ($relativePath in $requiredPaths) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path $fullPath)) {
        throw "Real caller substitution asset is missing at '$fullPath'."
    }
}

$readmeText = Get-Content -Raw -Path (Join-Path $repoRoot "README.md")
$requiredReadmeMentions = @(
    "docs/real-caller-substitution.md",
    "scripts/prepare-real-caller-substitution.ps1"
)

foreach ($mention in $requiredReadmeMentions) {
    if (-not $readmeText.Contains($mention)) {
        throw "README is missing real caller substitution mention '$mention'."
    }
}

Write-Host "Real caller substitution handoff assets are complete and anchored."
