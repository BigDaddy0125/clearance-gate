[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent

$requiredPaths = @(
    "docs/near-real-pilot-dry-run.md",
    "scripts/run-near-real-pilot-dry-run.ps1",
    "examples/real-caller-intake/near-real-authorize.json",
    "examples/real-caller-intake/near-real-acknowledge.json"
)

foreach ($relativePath in $requiredPaths) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path $fullPath)) {
        throw "Near-real pilot handoff asset is missing at '$fullPath'."
    }
}

$readmeText = Get-Content -Raw -Path (Join-Path $repoRoot "README.md")
$requiredReadmeMentions = @(
    "docs/near-real-pilot-dry-run.md",
    "scripts/run-near-real-pilot-dry-run.ps1",
    "examples/real-caller-intake/near-real-authorize.json"
)

foreach ($mention in $requiredReadmeMentions) {
    if (-not $readmeText.Contains($mention)) {
        throw "README is missing near-real pilot handoff mention '$mention'."
    }
}

Write-Host "Near-real pilot handoff assets are complete and anchored."
