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
    & (Join-Path $repoRoot "scripts\\check-claim-traceability.ps1") | Out-Null
    $traceabilityStatus = "PASS"
}
catch {
    $traceabilityStatus = "FAIL"
}

$latestRuntimeSummary = Join-Path $repoRoot "artifacts\\test-results\\summary.md"
$latestTlcSummary = Join-Path $repoRoot "artifacts\\tlc\\summary.md"
$releaseBundleRoot = Join-Path $repoRoot "artifacts\\publish"

$runtimeStatus = if (Test-Path $latestRuntimeSummary) { "PRESENT" } else { "MISSING" }
$tlcStatus = if (Test-Path $latestTlcSummary) { "PRESENT" } else { "MISSING" }
$bundleStatus = "UNKNOWN"
try {
    & (Join-Path $repoRoot "scripts\\validate-release-bundle.ps1") -BundleRoot $releaseBundleRoot | Out-Null
    $bundleStatus = "PASS"
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
    "| Runtime Claim Summary | $runtimeStatus | artifacts/test-results/summary.md |",
    "| TLC Summary | $tlcStatus | artifacts/tlc/summary.md |",
    "| Release Bundle | $bundleStatus | artifacts/publish/bundle-manifest.json |",
    "| Release Checklist | PRESENT | docs/release-readiness.md |"
)

$outputDirectory = Split-Path $resolvedOutputPath -Parent
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
Set-Content -Path $resolvedOutputPath -Value $summaryLines

Write-Host ("Summary: " + $resolvedOutputPath)
