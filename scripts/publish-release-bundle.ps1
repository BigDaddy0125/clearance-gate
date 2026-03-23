[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$OutputRoot = "",
    [string]$Runtime = "",
    [switch]$SelfContained
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$resolvedOutputRoot =
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repoRoot "artifacts\publish"
    }
    else {
        $OutputRoot
    }

$publishRoot = [System.IO.Path]::GetFullPath($resolvedOutputRoot)
$appOutput = Join-Path $publishRoot "app"
$bundleDocs = Join-Path $publishRoot "docs"
$bundleExamples = Join-Path $publishRoot "examples\deployment"

if (Test-Path $publishRoot) {
    Remove-Item -Recurse -Force $publishRoot
}

[System.IO.Directory]::CreateDirectory($appOutput) | Out-Null
[System.IO.Directory]::CreateDirectory($bundleDocs) | Out-Null
[System.IO.Directory]::CreateDirectory($bundleExamples) | Out-Null

$apiProject = Join-Path $repoRoot "src\ClearanceGate.Api\ClearanceGate.Api.csproj"
$publishArguments = @(
    "publish",
    $apiProject,
    "--configuration", $Configuration,
    "--output", $appOutput
)

if (-not [string]::IsNullOrWhiteSpace($Runtime)) {
    $publishArguments += @("--runtime", $Runtime)
}

if ($SelfContained) {
    $publishArguments += @("--self-contained", "true")
}
else {
    $publishArguments += @("--self-contained", "false")
}

& dotnet @publishArguments
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$documentsToCopy = @(
    "docs\deployment-runbook.md",
    "docs\release-readiness.md",
    "docs\operations-runbook.md",
    "docs\observability-contract.md",
    "docs\api-examples.md"
)

foreach ($relativePath in $documentsToCopy) {
    Copy-Item -Path (Join-Path $repoRoot $relativePath) -Destination $bundleDocs
}

Copy-Item `
    -Path (Join-Path $repoRoot "examples\deployment\appsettings.Production.example.json") `
    -Destination $bundleExamples

$profileNames = Get-ChildItem -Path (Join-Path $repoRoot "src\ClearanceGate.Profiles") -Filter *.json -File |
    Sort-Object Name |
    ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_.Name) }

$commit = ""
try {
    $commit = (git -C $repoRoot rev-parse HEAD).Trim()
}
catch {
    $commit = "unknown"
}

$manifest = [ordered]@{
    product = "ClearanceGate"
    bundleCreatedUtc = [DateTime]::UtcNow.ToString("o")
    configuration = $Configuration
    runtime = if ([string]::IsNullOrWhiteSpace($Runtime)) { "portable" } else { $Runtime }
    selfContained = [bool]$SelfContained
    commit = $commit
    embeddedProfiles = @($profileNames)
    publishOutput = "app"
    includedDocs = @(
        "docs/deployment-runbook.md",
        "docs/release-readiness.md",
        "docs/operations-runbook.md",
        "docs/observability-contract.md",
        "docs/api-examples.md"
    )
    includedExamples = @(
        "examples/deployment/appsettings.Production.example.json"
    )
}

$manifest | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $publishRoot "bundle-manifest.json")

Write-Host ("Published release bundle to " + $publishRoot)
