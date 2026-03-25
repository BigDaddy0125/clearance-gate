[CmdletBinding()]
param(
    [string]$EvidencePackageRoot = "",
    [string]$OutputRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent

if ([string]::IsNullOrWhiteSpace($EvidencePackageRoot)) {
    $pilotEvidenceRoot = Join-Path $repoRoot "artifacts\pilot-evidence"
    if (-not (Test-Path $pilotEvidenceRoot)) {
        throw "Pilot evidence root is missing at '$pilotEvidenceRoot'."
    }

    $latestPackage = Get-ChildItem -Path $pilotEvidenceRoot -Directory |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $latestPackage) {
        throw "No packaged pilot evidence directories were found under '$pilotEvidenceRoot'."
    }

    $resolvedEvidencePackageRoot = $latestPackage.FullName
}
else {
    $resolvedEvidencePackageRoot = $EvidencePackageRoot
}

$resolvedOutputRoot =
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repoRoot "artifacts\post-pilot-review"
    }
    else {
        $OutputRoot
    }

$evidenceRoot = [System.IO.Path]::GetFullPath($resolvedEvidencePackageRoot)
$outputRoot = [System.IO.Path]::GetFullPath($resolvedOutputRoot)

$requiredEvidenceFiles = @(
    "bundle-manifest.json",
    "evidence-manifest.json",
    "release-readiness-summary.md",
    "responses\audit-compact.json",
    "responses\audit-export.json"
)

foreach ($relativePath in $requiredEvidenceFiles) {
    $fullPath = Join-Path $evidenceRoot $relativePath
    if (-not (Test-Path $fullPath)) {
        throw "Required evidence file is missing at '$fullPath'."
    }
}

$evidenceManifest = Get-Content -Raw -Path (Join-Path $evidenceRoot "evidence-manifest.json") | ConvertFrom-Json

$reviewName = "post-pilot-review-" + [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
$reviewRoot = Join-Path $outputRoot $reviewName
$reviewDocsRoot = Join-Path $reviewRoot "docs"
$reviewEvidenceRoot = Join-Path $reviewRoot "evidence"

[System.IO.Directory]::CreateDirectory($reviewDocsRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($reviewEvidenceRoot) | Out-Null

Copy-Item -Path (Join-Path $evidenceRoot "bundle-manifest.json") -Destination $reviewEvidenceRoot
Copy-Item -Path (Join-Path $evidenceRoot "evidence-manifest.json") -Destination $reviewEvidenceRoot
Copy-Item -Path (Join-Path $evidenceRoot "release-readiness-summary.md") -Destination $reviewEvidenceRoot
Copy-Item -Path (Join-Path $evidenceRoot "responses\audit-compact.json") -Destination $reviewEvidenceRoot
Copy-Item -Path (Join-Path $evidenceRoot "responses\audit-export.json") -Destination $reviewEvidenceRoot

$documentsToCopy = @(
    "docs\post-pilot-review-flow.md",
    "docs\pilot-acceptance-checklist.md",
    "docs\post-pilot-decision-memo.md",
    "docs\v1-backlog.md"
)

foreach ($relativePath in $documentsToCopy) {
    Copy-Item -Path (Join-Path $repoRoot $relativePath) -Destination $reviewDocsRoot
}

$reviewManifest = [ordered]@{
    createdUtc = [DateTime]::UtcNow.ToString("o")
    sourceEvidencePackage = $evidenceRoot
    releaseBundleCommit = [string]$evidenceManifest.releaseBundleCommit
    profile = [string]$evidenceManifest.profile
    requestId = [string]$evidenceManifest.requestId
    decisionId = [string]$evidenceManifest.decisionId
    evidenceId = [string]$evidenceManifest.evidenceId
    finalOutcome = [string]$evidenceManifest.finalOutcome
    finalClearanceState = [string]$evidenceManifest.finalClearanceState
    reviewDocs = @(
        "docs/post-pilot-review-flow.md",
        "docs/pilot-acceptance-checklist.md",
        "docs/post-pilot-decision-memo.md",
        "docs/v1-backlog.md"
    )
    evidenceFiles = @(
        "evidence/bundle-manifest.json",
        "evidence/evidence-manifest.json",
        "evidence/release-readiness-summary.md",
        "evidence/audit-compact.json",
        "evidence/audit-export.json"
    )
    decisionMemoDraft = "decision-memo-draft.md"
}

$reviewManifest | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $reviewRoot "review-manifest.json")

& (Join-Path $repoRoot "scripts\initialize-post-pilot-decision-memo.ps1") -ReviewRoot $reviewRoot | Out-Null

Write-Host ("Prepared post-pilot review at " + $reviewRoot)
$reviewRoot
