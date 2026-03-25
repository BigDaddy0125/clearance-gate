[CmdletBinding()]
param(
    [string]$OutputRoot = "",
    [string]$BundleRoot = "",
    [string]$ReleaseSummaryPath = "",
    [string]$AuthorizeResponsePath = "",
    [string]$AcknowledgeResponsePath = "",
    [string]$CompactAuditPath = "",
    [string]$ExportAuditPath = "",
    [string]$ProfilesResponsePath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent

$resolvedOutputRoot =
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repoRoot "artifacts\pilot-evidence"
    }
    else {
        $OutputRoot
    }

$resolvedBundleRoot =
    if ([string]::IsNullOrWhiteSpace($BundleRoot)) {
        Join-Path $repoRoot "artifacts\publish"
    }
    else {
        $BundleRoot
    }

$resolvedReleaseSummaryPath =
    if ([string]::IsNullOrWhiteSpace($ReleaseSummaryPath)) {
        Join-Path $repoRoot "artifacts\release-readiness\summary.md"
    }
    else {
        $ReleaseSummaryPath
    }

$resolvedAuthorizeResponsePath =
    if ([string]::IsNullOrWhiteSpace($AuthorizeResponsePath)) {
        Join-Path $repoRoot "examples\v0\responses\authorize-risk-response.json"
    }
    else {
        $AuthorizeResponsePath
    }

$resolvedAcknowledgeResponsePath =
    if ([string]::IsNullOrWhiteSpace($AcknowledgeResponsePath)) {
        Join-Path $repoRoot "examples\v0\responses\acknowledge-risk-response.json"
    }
    else {
        $AcknowledgeResponsePath
    }

$resolvedCompactAuditPath =
    if ([string]::IsNullOrWhiteSpace($CompactAuditPath)) {
        Join-Path $repoRoot "examples\v0\responses\audit-risk-compact-response.json"
    }
    else {
        $CompactAuditPath
    }

$resolvedExportAuditPath =
    if ([string]::IsNullOrWhiteSpace($ExportAuditPath)) {
        Join-Path $repoRoot "examples\v0\responses\audit-risk-export-response.json"
    }
    else {
        $ExportAuditPath
    }

$resolvedProfilesResponsePath =
    if ([string]::IsNullOrWhiteSpace($ProfilesResponsePath)) {
        Join-Path $repoRoot "examples\v0\responses\profiles-response.json"
    }
    else {
        $ProfilesResponsePath
    }

& (Join-Path $repoRoot "scripts\validate-release-bundle.ps1") -BundleRoot $resolvedBundleRoot | Out-Null

if (-not (Test-Path $resolvedReleaseSummaryPath)) {
    throw "Release summary is missing at '$resolvedReleaseSummaryPath'."
}

$requiredJsonPaths = @(
    $resolvedAuthorizeResponsePath,
    $resolvedCompactAuditPath,
    $resolvedExportAuditPath,
    $resolvedProfilesResponsePath
)

foreach ($path in $requiredJsonPaths) {
    if (-not (Test-Path $path)) {
        throw "Required evidence input is missing at '$path'."
    }
}

$authorizeResponse = Get-Content -Raw -Path $resolvedAuthorizeResponsePath | ConvertFrom-Json
$compactAudit = Get-Content -Raw -Path $resolvedCompactAuditPath | ConvertFrom-Json
$exportAudit = Get-Content -Raw -Path $resolvedExportAuditPath | ConvertFrom-Json
$profilesResponse = Get-Content -Raw -Path $resolvedProfilesResponsePath | ConvertFrom-Json

if ([string]$compactAudit.decisionId -ne [string]$exportAudit.decisionId) {
    throw "Compact and export audit responses disagree on decisionId."
}

if ([string]$compactAudit.evidenceId -ne [string]$exportAudit.evidenceId) {
    throw "Compact and export audit responses disagree on evidenceId."
}

$bundleManifestPath = Join-Path $resolvedBundleRoot "bundle-manifest.json"
$bundleManifest = Get-Content -Raw -Path $bundleManifestPath | ConvertFrom-Json

$packageName = "pilot-evidence-" + [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
$packageRoot = Join-Path $resolvedOutputRoot $packageName
$responsesRoot = Join-Path $packageRoot "responses"
$docsRoot = Join-Path $packageRoot "docs"

[System.IO.Directory]::CreateDirectory($responsesRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($docsRoot) | Out-Null

Copy-Item -Path $bundleManifestPath -Destination $packageRoot
Copy-Item -Path $resolvedReleaseSummaryPath -Destination (Join-Path $packageRoot "release-readiness-summary.md")
Copy-Item -Path $resolvedAuthorizeResponsePath -Destination (Join-Path $responsesRoot "authorize-response.json")
Copy-Item -Path $resolvedCompactAuditPath -Destination (Join-Path $responsesRoot "audit-compact.json")
Copy-Item -Path $resolvedExportAuditPath -Destination (Join-Path $responsesRoot "audit-export.json")
Copy-Item -Path $resolvedProfilesResponsePath -Destination (Join-Path $responsesRoot "profiles-response.json")

if (Test-Path $resolvedAcknowledgeResponsePath) {
    Copy-Item -Path $resolvedAcknowledgeResponsePath -Destination (Join-Path $responsesRoot "acknowledge-response.json")
}

$documentsToCopy = @(
    "docs\pilot-evidence-package.md",
    "docs\pilot-execution-checklist.md",
    "docs\pilot-incident-response.md",
    "docs\operator-logging-guide.md",
    "docs\caller-onboarding-checklist.md"
)

foreach ($relativePath in $documentsToCopy) {
    Copy-Item -Path (Join-Path $repoRoot $relativePath) -Destination $docsRoot
}

$evidenceManifest = [ordered]@{
    createdUtc = [DateTime]::UtcNow.ToString("o")
    releaseBundleCommit = [string]$bundleManifest.commit
    embeddedProfiles = @($bundleManifest.embeddedProfiles)
    profile = [string]$exportAudit.profile
    requestId = [string]$exportAudit.requestId
    decisionId = [string]$exportAudit.decisionId
    evidenceId = [string]$exportAudit.evidenceId
    finalOutcome = [string]$exportAudit.outcome
    finalClearanceState = [string]$exportAudit.clearanceState
    authorizeOutcome = [string]$authorizeResponse.outcome
    authorizeClearanceState = [string]$authorizeResponse.clearanceState
    profilesCatalogCount = @($profilesResponse.profiles).Count
    packageContents = @(
        "bundle-manifest.json",
        "release-readiness-summary.md",
        "responses/authorize-response.json",
        "responses/audit-compact.json",
        "responses/audit-export.json",
        "responses/profiles-response.json",
        "docs/pilot-evidence-package.md",
        "docs/pilot-execution-checklist.md",
        "docs/pilot-incident-response.md",
        "docs/operator-logging-guide.md",
        "docs/caller-onboarding-checklist.md"
    )
}

if (Test-Path (Join-Path $responsesRoot "acknowledge-response.json")) {
    $evidenceManifest.packageContents += "responses/acknowledge-response.json"
}

$evidenceManifest | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $packageRoot "evidence-manifest.json")

Write-Host ("Packaged pilot evidence to " + $packageRoot)
$packageRoot
