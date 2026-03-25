[CmdletBinding()]
param(
    [string]$OutputRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent

$resolvedOutputRoot =
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repoRoot "artifacts\caller-integration-review"
    }
    else {
        $OutputRoot
    }

& (Join-Path $repoRoot "scripts\validate-pilot-adapter-example.ps1") | Out-Null

$reviewName = "caller-integration-review-" + [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
$reviewRoot = Join-Path $resolvedOutputRoot $reviewName
$docsRoot = Join-Path $reviewRoot "docs"
$examplesRoot = Join-Path $reviewRoot "examples\pilot-adapter"

[System.IO.Directory]::CreateDirectory($docsRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($examplesRoot) | Out-Null

$docsToCopy = @(
    "docs\caller-onboarding-checklist.md",
    "docs\pilot-adapter-example.md",
    "docs\pilot-adapter-checklist.md",
    "docs\real-caller-rehearsal.md"
)

foreach ($relativePath in $docsToCopy) {
    Copy-Item -Path (Join-Path $repoRoot $relativePath) -Destination $docsRoot
}

Get-ChildItem -Path (Join-Path $repoRoot "examples\pilot-adapter") -File |
    ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $examplesRoot
    }

$reviewManifest = [ordered]@{
    createdUtc = [DateTime]::UtcNow.ToString("o")
    profile = "itops_deployment_v1"
    docs = @(
        "docs/caller-onboarding-checklist.md",
        "docs/pilot-adapter-example.md",
        "docs/pilot-adapter-checklist.md",
        "docs/real-caller-rehearsal.md"
    )
    examples = @(
        Get-ChildItem -Path $examplesRoot -File |
            Sort-Object Name |
            ForEach-Object { "examples/pilot-adapter/" + $_.Name }
    )
}

$reviewManifest | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $reviewRoot "review-manifest.json")

Write-Host ("Prepared caller integration review at " + $reviewRoot)
$reviewRoot
