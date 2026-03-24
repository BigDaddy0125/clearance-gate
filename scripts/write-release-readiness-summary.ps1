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
    & (Join-Path $repoRoot "scripts\\check-caller-integration-handoff.ps1") | Out-Null
    $callerIntegrationStatus = "PASS"
}
catch {
    $callerIntegrationStatus = "FAIL"
}

try {
    & (Join-Path $repoRoot "scripts\\check-real-caller-intake-handoff.ps1") | Out-Null
    $realCallerIntakeStatus = "PASS"
}
catch {
    $realCallerIntakeStatus = "FAIL"
}

try {
    & (Join-Path $repoRoot "scripts\\check-real-caller-promotion-handoff.ps1") | Out-Null
    $realCallerPromotionStatus = "PASS"
}
catch {
    $realCallerPromotionStatus = "FAIL"
}

try {
    & (Join-Path $repoRoot "scripts\\check-near-real-pilot-handoff.ps1") | Out-Null
    $nearRealPilotStatus = "PASS"
}
catch {
    $nearRealPilotStatus = "FAIL"
}

try {
    & (Join-Path $repoRoot "scripts\\check-real-caller-substitution-handoff.ps1") | Out-Null
    $realCallerSubstitutionStatus = "PASS"
}
catch {
    $realCallerSubstitutionStatus = "FAIL"
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
$controlledPilotDryRunScriptPath = Join-Path $repoRoot "scripts\\run-controlled-pilot-dry-run.ps1"
$callerIntegrationRehearsalScriptPath = Join-Path $repoRoot "scripts\\run-caller-integration-rehearsal.ps1"
$realCallerIntakeGuidePath = Join-Path $repoRoot "docs\\real-caller-intake.md"
$realCallerIntakeScriptPath = Join-Path $repoRoot "scripts\\initialize-real-caller-intake.ps1"
$realCallerPromotionGuidePath = Join-Path $repoRoot "docs\\real-caller-promotion.md"
$realCallerSampleIntakeScriptPath = Join-Path $repoRoot "scripts\\create-sample-real-caller-intake.ps1"
$realCallerIntakeValidationScriptPath = Join-Path $repoRoot "scripts\\validate-real-caller-intake-package.ps1"
$realCallerPromotionScriptPath = Join-Path $repoRoot "scripts\\promote-real-caller-intake.ps1"
$realCallerRehearsalGuidePath = Join-Path $repoRoot "docs\\real-caller-rehearsal.md"
$realCallerValidationScriptPath = Join-Path $repoRoot "scripts\\validate-real-caller-rehearsal-input.ps1"
$realCallerPreparationScriptPath = Join-Path $repoRoot "scripts\\prepare-real-caller-rehearsal.ps1"
$nearRealPilotGuidePath = Join-Path $repoRoot "docs\\near-real-pilot-dry-run.md"
$nearRealPilotScriptPath = Join-Path $repoRoot "scripts\\run-near-real-pilot-dry-run.ps1"
$realCallerSubstitutionGuidePath = Join-Path $repoRoot "docs\\real-caller-substitution.md"
$realCallerSubstitutionScriptPath = Join-Path $repoRoot "scripts\\prepare-real-caller-substitution.ps1"

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
$controlledPilotDryRunScriptStatus = if (Test-Path $controlledPilotDryRunScriptPath) { "PRESENT" } else { "MISSING" }
$callerIntegrationRehearsalScriptStatus = if (Test-Path $callerIntegrationRehearsalScriptPath) { "PRESENT" } else { "MISSING" }
$realCallerIntakeGuideStatus = if (Test-Path $realCallerIntakeGuidePath) { "PRESENT" } else { "MISSING" }
$realCallerIntakeScriptStatus = if (Test-Path $realCallerIntakeScriptPath) { "PRESENT" } else { "MISSING" }
$realCallerPromotionGuideStatus = if (Test-Path $realCallerPromotionGuidePath) { "PRESENT" } else { "MISSING" }
$realCallerSampleIntakeScriptStatus = if (Test-Path $realCallerSampleIntakeScriptPath) { "PRESENT" } else { "MISSING" }
$realCallerIntakeValidationScriptStatus = if (Test-Path $realCallerIntakeValidationScriptPath) { "PRESENT" } else { "MISSING" }
$realCallerPromotionScriptStatus = if (Test-Path $realCallerPromotionScriptPath) { "PRESENT" } else { "MISSING" }
$realCallerRehearsalGuideStatus = if (Test-Path $realCallerRehearsalGuidePath) { "PRESENT" } else { "MISSING" }
$realCallerValidationScriptStatus = if (Test-Path $realCallerValidationScriptPath) { "PRESENT" } else { "MISSING" }
$realCallerPreparationScriptStatus = if (Test-Path $realCallerPreparationScriptPath) { "PRESENT" } else { "MISSING" }
$nearRealPilotGuideStatus = if (Test-Path $nearRealPilotGuidePath) { "PRESENT" } else { "MISSING" }
$nearRealPilotScriptStatus = if (Test-Path $nearRealPilotScriptPath) { "PRESENT" } else { "MISSING" }
$realCallerSubstitutionGuideStatus = if (Test-Path $realCallerSubstitutionGuidePath) { "PRESENT" } else { "MISSING" }
$realCallerSubstitutionScriptStatus = if (Test-Path $realCallerSubstitutionScriptPath) { "PRESENT" } else { "MISSING" }
$bundleStatus = "UNKNOWN"
$bundleCommit = "N/A"
$bundleProfiles = "N/A"
$bundleManifestPath = Join-Path $releaseBundleRoot "bundle-manifest.json"
try {
    & (Join-Path $repoRoot "scripts\\validate-release-bundle.ps1") -BundleRoot $releaseBundleRoot | Out-Null

    if (-not (Test-Path $bundleManifestPath)) {
        throw "Bundle manifest is missing at '$bundleManifestPath'."
    }

    $bundleManifest = Get-Content -Raw -Path $bundleManifestPath -ErrorAction Stop | ConvertFrom-Json
    $bundleCommit = [string]$bundleManifest.commit
    $bundleProfiles = ((@($bundleManifest.embeddedProfiles) | Sort-Object) -join ", ")
    $bundleStatus = "PASS"
}
catch {
    $bundleStatus = "FAIL"
    $bundleCommit = "N/A"
    $bundleProfiles = "N/A"
}

$summaryLines = @(
    "# Release Readiness Summary",
    "",
    "| Gate | Status | Anchor |",
    "| --- | --- | --- |",
    "| Traceability | $traceabilityStatus | docs/claim-traceability.md |",
    "| Delivery Handoff | $deliveryHandoffStatus | scripts/check-delivery-handoff.ps1 |",
    "| Controlled Pilot | $controlledPilotStatus | scripts/check-controlled-pilot-readiness.ps1 |",
    "| Caller Integration | $callerIntegrationStatus | scripts/check-caller-integration-handoff.ps1 |",
    "| Real Caller Intake | $realCallerIntakeStatus | scripts/check-real-caller-intake-handoff.ps1 |",
    "| Real Caller Promotion | $realCallerPromotionStatus | scripts/check-real-caller-promotion-handoff.ps1 |",
    "| Near-Real Pilot | $nearRealPilotStatus | scripts/check-near-real-pilot-handoff.ps1 |",
    "| Real Caller Substitution | $realCallerSubstitutionStatus | scripts/check-real-caller-substitution-handoff.ps1 |",
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
    "| Controlled Pilot Dry-Run Script | $controlledPilotDryRunScriptStatus | scripts/run-controlled-pilot-dry-run.ps1 |",
    "| Caller Integration Rehearsal Script | $callerIntegrationRehearsalScriptStatus | scripts/run-caller-integration-rehearsal.ps1 |",
    "| Real Caller Intake Guide | $realCallerIntakeGuideStatus | docs/real-caller-intake.md |",
    "| Real Caller Intake Script | $realCallerIntakeScriptStatus | scripts/initialize-real-caller-intake.ps1 |",
    "| Real Caller Promotion Guide | $realCallerPromotionGuideStatus | docs/real-caller-promotion.md |",
    "| Real Caller Sample Intake Script | $realCallerSampleIntakeScriptStatus | scripts/create-sample-real-caller-intake.ps1 |",
    "| Real Caller Intake Validation | $realCallerIntakeValidationScriptStatus | scripts/validate-real-caller-intake-package.ps1 |",
    "| Real Caller Promotion Script | $realCallerPromotionScriptStatus | scripts/promote-real-caller-intake.ps1 |",
    "| Real Caller Rehearsal Guide | $realCallerRehearsalGuideStatus | docs/real-caller-rehearsal.md |",
    "| Real Caller Input Validation | $realCallerValidationScriptStatus | scripts/validate-real-caller-rehearsal-input.ps1 |",
    "| Real Caller Rehearsal Prep | $realCallerPreparationScriptStatus | scripts/prepare-real-caller-rehearsal.ps1 |",
    "| Near-Real Pilot Guide | $nearRealPilotGuideStatus | docs/near-real-pilot-dry-run.md |",
    "| Near-Real Pilot Script | $nearRealPilotScriptStatus | scripts/run-near-real-pilot-dry-run.ps1 |",
    "| Real Caller Substitution Guide | $realCallerSubstitutionGuideStatus | docs/real-caller-substitution.md |",
    "| Real Caller Substitution Script | $realCallerSubstitutionScriptStatus | scripts/prepare-real-caller-substitution.ps1 |",
    "| Release Checklist | PRESENT | docs/release-readiness.md |"
)

$outputDirectory = Split-Path $resolvedOutputPath -Parent
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
Set-Content -Path $resolvedOutputPath -Value $summaryLines

Write-Host ("Summary: " + $resolvedOutputPath)
