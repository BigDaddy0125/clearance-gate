[CmdletBinding()]
param(
    [string]$BundleRoot = ""
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

$bundleRoot = [System.IO.Path]::GetFullPath($resolvedBundleRoot)
$manifestPath = Join-Path $bundleRoot "bundle-manifest.json"
$appDirectory = Join-Path $bundleRoot "app"
$docsDirectory = Join-Path $bundleRoot "docs"
$examplesDirectory = Join-Path $bundleRoot "examples\deployment"
$operationsExamplesDirectory = Join-Path $bundleRoot "examples\operations"

if (-not (Test-Path $manifestPath)) {
    throw "Bundle manifest is missing at '$manifestPath'."
}

$manifest = Get-Content -Raw -Path $manifestPath | ConvertFrom-Json
if ($manifest.product -ne "ClearanceGate") {
    throw "Bundle manifest product must be 'ClearanceGate'."
}

if (-not (Test-Path $appDirectory)) {
    throw "Published app directory is missing at '$appDirectory'."
}

$appDll = Join-Path $appDirectory "ClearanceGate.Api.dll"
if (-not (Test-Path $appDll)) {
    throw "Published app dll is missing at '$appDll'."
}

$requiredDocs = @(
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
    "near-real-pilot-dry-run.md",
    "real-caller-substitution.md"
)

foreach ($doc in $requiredDocs) {
    $docPath = Join-Path $docsDirectory $doc
    if (-not (Test-Path $docPath)) {
        throw "Required bundle document is missing at '$docPath'."
    }
}

$requiredExamples = @(
    "appsettings.LocalValidation.example.json",
    "appsettings.Pilot.example.json",
    "appsettings.Production.example.json"
)

foreach ($example in $requiredExamples) {
    $examplePath = Join-Path $examplesDirectory $example
    if (-not (Test-Path $examplePath)) {
        throw "Deployment config example is missing at '$examplePath'."
    }
}

$requiredOperationsExamples = @(
    "operator-log-sample.jsonl"
)

foreach ($example in $requiredOperationsExamples) {
    $examplePath = Join-Path $operationsExamplesDirectory $example
    if (-not (Test-Path $examplePath)) {
        throw "Operations example is missing at '$examplePath'."
    }
}

if ($null -eq $manifest.embeddedProfiles -or $manifest.embeddedProfiles.Count -eq 0) {
    throw "Bundle manifest must declare at least one embedded profile."
}

if ($null -eq $manifest.includedExamples -or $manifest.includedExamples.Count -lt $requiredExamples.Count) {
    throw "Bundle manifest must declare the deployment examples included in the bundle."
}

Write-Host ("Validated release bundle at " + $bundleRoot)
