[CmdletBinding()]
param(
    [string]$OutputPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$resolvedOutputPath =
    if ([string]::IsNullOrWhiteSpace($OutputPath)) {
        Join-Path $repoRoot "artifacts\\release-readiness\\summary.md"
    }
    else {
        $OutputPath
    }

$traceabilityStatus = "UNKNOWN"
try {
    & (Join-Path $repoRoot "scripts\\check-delivery-handoff.ps1") | Out-Null
    $deliveryHandoffStatus = "PASS"
}
catch {
    $deliveryHandoffStatus = "FAIL"
}

try {
    & (Join-Path $repoRoot "scripts\\check-controlled-pilot-readiness.ps1") | Out-Null
    $controlledPilotStatus = "PASS"
}
catch {
    $controlledPilotStatus = "FAIL"
}

try {
    & (Join-Path $repoRoot "scripts\\check-claim-traceability.ps1") | Out-Null
    $traceabilityStatus = "PASS"
}
catch {
    $traceabilityStatus = "FAIL"
}

$latestRuntimeSummary = Join-Path $repoRoot "artifacts\\test-results\\summary.md"
$latestTlcSummary = Join-Path $repoRoot "artifacts\\tlc\\summary.md"
$releaseBundleRoot = Join-Path $repoRoot "artifacts\\publish"
$pilotSmokeCheckPath = Join-Path $repoRoot "scripts\\run-deployment-smoke-check.ps1"
$operatorLoggingGuidePath = Join-Path $repoRoot "docs\\operator-logging-guide.md"
$callerOnboardingGuidePath = Join-Path $repoRoot "docs\\caller-onboarding-checklist.md"
$pilotExecutionChecklistPath = Join-Path $repoRoot "docs\\pilot-execution-checklist.md"
$pilotIncidentResponsePath = Join-Path $repoRoot "docs\\pilot-incident-response.md"
$pilotEvidencePackageGuidePath = Join-Path $repoRoot "docs\\pilot-evidence-package.md"
$pilotEvidencePackagingScriptPath = Join-Path $repoRoot "scripts\\package-pilot-evidence.ps1"
$pilotSessionCaptureScriptPath = Join-Path $repoRoot "scripts\\capture-pilot-sample-session.ps1"

$runtimeStatus = if (Test-Path $latestRuntimeSummary) { "PRESENT" } else { "MISSING" }
$tlcStatus = if (Test-Path $latestTlcSummary) { "PRESENT" } else { "MISSING" }
$pilotSmokeCheckStatus = if (Test-Path $pilotSmokeCheckPath) { "PRESENT" } else { "MISSING" }
$operatorLoggingGuideStatus = if (Test-Path $operatorLoggingGuidePath) { "PRESENT" } else { "MISSING" }
$callerOnboardingGuideStatus = if (Test-Path $callerOnboardingGuidePath) { "PRESENT" } else { "MISSING" }
$pilotExecutionChecklistStatus = if (Test-Path $pilotExecutionChecklistPath) { "PRESENT" } else { "MISSING" }
$pilotIncidentResponseStatus = if (Test-Path $pilotIncidentResponsePath) { "PRESENT" } else { "MISSING" }
$pilotEvidencePackageGuideStatus = if (Test-Path $pilotEvidencePackageGuidePath) { "PRESENT" } else { "MISSING" }
$pilotEvidencePackagingScriptStatus = if (Test-Path $pilotEvidencePackagingScriptPath) { "PRESENT" } else { "MISSING" }
$pilotSessionCaptureScriptStatus = if (Test-Path $pilotSessionCaptureScriptPath) { "PRESENT" } else { "MISSING" }
$bundleStatus = "UNKNOWN"
$bundleCommit = "N/A"
$bundleProfiles = "N/A"
try {
    & (Join-Path $repoRoot "scripts\\validate-release-bundle.ps1") -BundleRoot $releaseBundleRoot | Out-Null
    $bundleStatus = "PASS"
    $bundleManifestPath = Join-Path $releaseBundleRoot "bundle-manifest.json"
    $bundleManifest = Get-Content -Raw -Path $bundleManifestPath | ConvertFrom-Json
    $bundleCommit = [string]$bundleManifest.commit
    $bundleProfiles = ((@($bundleManifest.embeddedProfiles) | Sort-Object) -join ", ")
}
catch {
    $bundleStatus = "FAIL"
}

$summaryLines = @(
    "# Release Readiness Summary",
    "",
    "| Gate | Status | Anchor |",
    "| --- | --- | --- |",
    "| Traceability | $traceabilityStatus | docs/claim-traceability.md |",
    "| Delivery Handoff | $deliveryHandoffStatus | scripts/check-delivery-handoff.ps1 |",
    "| Controlled Pilot | $controlledPilotStatus | scripts/check-controlled-pilot-readiness.ps1 |",
    "| Runtime Claim Summary | $runtimeStatus | artifacts/test-results/summary.md |",
    "| TLC Summary | $tlcStatus | artifacts/tlc/summary.md |",
    "| Release Bundle | $bundleStatus | artifacts/publish/bundle-manifest.json |",
    "| Bundle Commit | $bundleCommit | artifacts/publish/bundle-manifest.json |",
    "| Bundle Profiles | $bundleProfiles | artifacts/publish/bundle-manifest.json |",
    "| Pilot Smoke Check | $pilotSmokeCheckStatus | scripts/run-deployment-smoke-check.ps1 |",
    "| Operator Logging Guide | $operatorLoggingGuideStatus | docs/operator-logging-guide.md |",
    "| Caller Onboarding Guide | $callerOnboardingGuideStatus | docs/caller-onboarding-checklist.md |",
    "| Pilot Execution Checklist | $pilotExecutionChecklistStatus | docs/pilot-execution-checklist.md |",
    "| Pilot Incident Response | $pilotIncidentResponseStatus | docs/pilot-incident-response.md |",
    "| Pilot Evidence Guide | $pilotEvidencePackageGuideStatus | docs/pilot-evidence-package.md |",
    "| Pilot Evidence Script | $pilotEvidencePackagingScriptStatus | scripts/package-pilot-evidence.ps1 |",
    "| Pilot Session Capture Script | $pilotSessionCaptureScriptStatus | scripts/capture-pilot-sample-session.ps1 |",
    "| Release Checklist | PRESENT | docs/release-readiness.md |"
)

$outputDirectory = Split-Path $resolvedOutputPath -Parent
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
Set-Content -Path $resolvedOutputPath -Value $summaryLines

Write-Host ("Summary: " + $resolvedOutputPath)
