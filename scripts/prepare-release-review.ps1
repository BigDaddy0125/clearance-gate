[CmdletBinding()]
param(
    [string]$BundleRoot = "",
    [string]$OutputRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent

$resolvedBundleRoot =
    if ([string]::IsNullOrWhiteSpace($BundleRoot)) {
        Join-Path $repoRoot "artifacts\publish"
    }
    else {
        $BundleRoot
    }

$resolvedOutputRoot =
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repoRoot "artifacts\release-review"
    }
    else {
        $OutputRoot
    }

& (Join-Path $repoRoot "scripts\validate-release-bundle.ps1") -BundleRoot $resolvedBundleRoot | Out-Null
& (Join-Path $repoRoot "scripts\write-release-readiness-summary.ps1") | Out-Null

$bundleRoot = [System.IO.Path]::GetFullPath($resolvedBundleRoot)
$outputRoot = [System.IO.Path]::GetFullPath($resolvedOutputRoot)
$bundleManifestPath = Join-Path $bundleRoot "bundle-manifest.json"
$summaryPath = Join-Path $repoRoot "artifacts\release-readiness\summary.md"
$reviewName = "release-review-" + [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
$reviewRoot = Join-Path $outputRoot $reviewName
$reviewDocsRoot = Join-Path $reviewRoot "docs"
$reviewExamplesRoot = Join-Path $reviewRoot "examples\deployment"
$reviewOperationsExamplesRoot = Join-Path $reviewRoot "examples\operations"

[System.IO.Directory]::CreateDirectory($reviewDocsRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($reviewExamplesRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($reviewOperationsExamplesRoot) | Out-Null

Copy-Item -Path $bundleManifestPath -Destination $reviewRoot
Copy-Item -Path $summaryPath -Destination (Join-Path $reviewRoot "release-readiness-summary.md")

$docsToCopy = @(
    "deployment-runbook.md",
    "release-readiness.md",
    "operations-runbook.md",
    "observability-contract.md",
    "operator-logging-guide.md",
    "operator-triage-cheatsheet.md",
    "api-examples.md",
    "pilot-evidence-package.md",
    "real-caller-intake.md",
    "real-caller-promotion.md",
    "real-caller-rehearsal.md",
    "near-real-pilot-dry-run.md"
)

foreach ($doc in $docsToCopy) {
    Copy-Item -Path (Join-Path $bundleRoot "docs\$doc") -Destination $reviewDocsRoot
}

Get-ChildItem -Path (Join-Path $bundleRoot "examples\deployment") -Filter *.json -File |
    ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $reviewExamplesRoot
    }

Get-ChildItem -Path (Join-Path $bundleRoot "examples\operations") -File |
    ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $reviewOperationsExamplesRoot
    }

$bundleManifest = Get-Content -Raw -Path $bundleManifestPath | ConvertFrom-Json
$reviewManifest = [ordered]@{
    createdUtc = [DateTime]::UtcNow.ToString("o")
    sourceBundleRoot = $bundleRoot
    releaseBundleCommit = [string]$bundleManifest.commit
    embeddedProfiles = @($bundleManifest.embeddedProfiles)
    includedDocs = @($bundleManifest.includedDocs)
    includedExamples = @(
        Get-ChildItem -Path $reviewExamplesRoot -Filter *.json -File |
            Sort-Object Name |
            ForEach-Object { "examples/deployment/" + $_.Name }
        Get-ChildItem -Path $reviewOperationsExamplesRoot -File |
            Sort-Object Name |
            ForEach-Object { "examples/operations/" + $_.Name }
    )
}

$reviewManifest | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $reviewRoot "review-manifest.json")

Write-Host ("Prepared release review at " + $reviewRoot)
