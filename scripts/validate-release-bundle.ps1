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
$exampleConfigPath = Join-Path $bundleRoot "examples\deployment\appsettings.Production.example.json"

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
    "api-examples.md",
    "pilot-evidence-package.md"
)

foreach ($doc in $requiredDocs) {
    $docPath = Join-Path $docsDirectory $doc
    if (-not (Test-Path $docPath)) {
        throw "Required bundle document is missing at '$docPath'."
    }
}

if (-not (Test-Path $exampleConfigPath)) {
    throw "Deployment config example is missing at '$exampleConfigPath'."
}

if ($null -eq $manifest.embeddedProfiles -or $manifest.embeddedProfiles.Count -eq 0) {
    throw "Bundle manifest must declare at least one embedded profile."
}

Write-Host ("Validated release bundle at " + $bundleRoot)
