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
$bundleOperationsExamples = Join-Path $publishRoot "examples\operations"
$deploymentExamplesRoot = Join-Path $repoRoot "examples\deployment"
$operationsExamplesRoot = Join-Path $repoRoot "examples\operations"

if (Test-Path $publishRoot) {
    for ($attempt = 1; $attempt -le 5; $attempt++) {
        try {
            Remove-Item -Recurse -Force $publishRoot
            break
        }
        catch {
            if ($attempt -eq 5) {
                throw
            }

            Start-Sleep -Milliseconds (500 * $attempt)
        }
    }
}

[System.IO.Directory]::CreateDirectory($appOutput) | Out-Null
[System.IO.Directory]::CreateDirectory($bundleDocs) | Out-Null
[System.IO.Directory]::CreateDirectory($bundleExamples) | Out-Null
[System.IO.Directory]::CreateDirectory($bundleOperationsExamples) | Out-Null

$apiProject = Join-Path $repoRoot "src\ClearanceGate.Api\ClearanceGate.Api.csproj"
$publishArguments = @(
    "publish",
    $apiProject,
    "--configuration", $Configuration,
    "--output", $appOutput,
    "-p:UseSharedCompilation=false"
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

for ($attempt = 1; $attempt -le 5; $attempt++) {
    & dotnet @publishArguments
    if ($LASTEXITCODE -eq 0) {
        break
    }

    if ($attempt -eq 5) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    Start-Sleep -Milliseconds (500 * $attempt)
}

$documentsToCopy = @(
    "docs\deployment-runbook.md",
    "docs\release-readiness.md",
    "docs\operations-runbook.md",
    "docs\observability-contract.md",
    "docs\operator-logging-guide.md",
    "docs\operator-triage-cheatsheet.md",
    "docs\api-examples.md",
    "docs\pilot-evidence-package.md",
    "docs\real-caller-intake.md",
    "docs\real-caller-promotion.md",
    "docs\real-caller-rehearsal.md",
    "docs\near-real-pilot-dry-run.md",
    "docs\real-caller-substitution.md"
)

foreach ($relativePath in $documentsToCopy) {
    Copy-Item -Path (Join-Path $repoRoot $relativePath) -Destination $bundleDocs
}

Get-ChildItem -Path $deploymentExamplesRoot -Filter *.json -File |
    ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $bundleExamples
    }

Get-ChildItem -Path $operationsExamplesRoot -File |
    ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $bundleOperationsExamples
    }

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
        "docs/operator-logging-guide.md",
        "docs/operator-triage-cheatsheet.md",
        "docs/api-examples.md",
        "docs/pilot-evidence-package.md",
        "docs/real-caller-intake.md",
        "docs/real-caller-promotion.md",
        "docs/real-caller-rehearsal.md",
        "docs/near-real-pilot-dry-run.md",
        "docs/real-caller-substitution.md"
    )
    includedExamples = @(
        Get-ChildItem -Path $deploymentExamplesRoot -Filter *.json -File |
            Sort-Object Name |
            ForEach-Object { "examples/deployment/" + $_.Name }
        Get-ChildItem -Path $operationsExamplesRoot -File |
            Sort-Object Name |
            ForEach-Object { "examples/operations/" + $_.Name }
    )
}

$manifest | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $publishRoot "bundle-manifest.json")

Write-Host ("Published release bundle to " + $publishRoot)
