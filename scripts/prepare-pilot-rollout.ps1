[CmdletBinding()]
param(
    [string]$OutputRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent

$resolvedOutputRoot =
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repoRoot "artifacts\pilot-rollout"
    }
    else {
        $OutputRoot
    }

$releaseReviewRoot = & (Join-Path $repoRoot "scripts\prepare-release-review.ps1")
& (Join-Path $repoRoot "scripts\write-release-readiness-summary.ps1") | Out-Null

if ([string]::IsNullOrWhiteSpace([string]$releaseReviewRoot) -or -not (Test-Path $releaseReviewRoot)) {
    throw "Release review directory could not be prepared."
}

$rolloutName = "pilot-rollout-" + [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
$rolloutRoot = Join-Path $resolvedOutputRoot $rolloutName
$docsRoot = Join-Path $rolloutRoot "docs"
$examplesRoot = Join-Path $rolloutRoot "examples"
$reviewRoot = Join-Path $rolloutRoot "release-review"

[System.IO.Directory]::CreateDirectory($docsRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($examplesRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($reviewRoot) | Out-Null

$docsToCopy = @(
    "docs\pilot-dry-run-checklist.md",
    "docs\pilot-rollback-note.md",
    "docs\pilot-execution-checklist.md",
    "docs\pilot-incident-response.md",
    "docs\caller-onboarding-checklist.md",
    "docs\operator-logging-guide.md",
    "docs\operator-triage-cheatsheet.md"
)

foreach ($relativePath in $docsToCopy) {
    Copy-Item -Path (Join-Path $repoRoot $relativePath) -Destination $docsRoot
}

Copy-Item -Path (Join-Path $repoRoot "examples\operations\operator-log-sample.jsonl") -Destination $examplesRoot
Copy-Item -Path (Join-Path $repoRoot "examples\deployment\appsettings.Pilot.example.json") -Destination $examplesRoot
Copy-Item -Path (Join-Path $repoRoot "examples\deployment\appsettings.Production.example.json") -Destination $examplesRoot

Copy-Item -Path (Join-Path $releaseReviewRoot "*") -Destination $reviewRoot -Recurse

$rolloutManifest = [ordered]@{
    createdUtc = [DateTime]::UtcNow.ToString("o")
    sourceReleaseReview = $releaseReviewRoot
    releaseReadinessSummary = "release-review/release-readiness-summary.md"
    requiredDocs = @(
        "docs/pilot-dry-run-checklist.md",
        "docs/pilot-rollback-note.md",
        "docs/pilot-execution-checklist.md",
        "docs/pilot-incident-response.md",
        "docs/caller-onboarding-checklist.md",
        "docs/operator-logging-guide.md",
        "docs/operator-triage-cheatsheet.md"
    )
    requiredExamples = @(
        "examples/appsettings.Pilot.example.json",
        "examples/appsettings.Production.example.json",
        "examples/operator-log-sample.jsonl"
    )
}

$rolloutManifest | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $rolloutRoot "rollout-manifest.json")

Write-Host ("Prepared pilot rollout at " + $rolloutRoot)
$rolloutRoot
