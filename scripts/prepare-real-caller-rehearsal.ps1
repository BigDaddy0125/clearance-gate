[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$AuthorizeInputPath,

    [Parameter(Mandatory = $true)]
    [string]$AcknowledgeInputPath,

    [string]$Profile = "itops_deployment_v1",
    [string]$OutputRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$adapterRoot = Join-Path $repoRoot "examples\pilot-adapter"

$resolvedOutputRoot =
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repoRoot "artifacts\real-caller-rehearsal"
    }
    else {
        $OutputRoot
    }

& (Join-Path $repoRoot "scripts\validate-real-caller-rehearsal-input.ps1") `
    -AuthorizeInputPath $AuthorizeInputPath `
    -AcknowledgeInputPath $AcknowledgeInputPath `
    -Profile $Profile | Out-Null

$rehearsalName = "real-caller-rehearsal-" + [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
$rehearsalRoot = Join-Path $resolvedOutputRoot $rehearsalName
$docsRoot = Join-Path $rehearsalRoot "docs"
$inputsRoot = Join-Path $rehearsalRoot "inputs"
$mappedRoot = Join-Path $rehearsalRoot "mapped"

[System.IO.Directory]::CreateDirectory($docsRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($inputsRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($mappedRoot) | Out-Null

$docsToCopy = @(
    "docs\caller-onboarding-checklist.md",
    "docs\pilot-adapter-checklist.md",
    "docs\caller-integration-rehearsal.md",
    "docs\real-caller-rehearsal.md"
)

foreach ($relativePath in $docsToCopy) {
    Copy-Item -Path (Join-Path $repoRoot $relativePath) -Destination $docsRoot
}

Copy-Item -Path $AuthorizeInputPath -Destination (Join-Path $inputsRoot "caller-authorize-request.json")
Copy-Item -Path $AcknowledgeInputPath -Destination (Join-Path $inputsRoot "caller-acknowledge-request.json")
Copy-Item -Path (Join-Path $adapterRoot "convert-change-control-example.ps1") -Destination $rehearsalRoot

$mappedAuthorize = & (Join-Path $adapterRoot "convert-change-control-example.ps1") `
    -Mode authorize `
    -Profile $Profile `
    -AuthorizeInputPath $AuthorizeInputPath | ConvertFrom-Json

$mappedAcknowledge = & (Join-Path $adapterRoot "convert-change-control-example.ps1") `
    -Mode acknowledge `
    -Profile $Profile `
    -AcknowledgeInputPath $AcknowledgeInputPath | ConvertFrom-Json

$mappedAuthorize | ConvertTo-Json -Depth 20 | Set-Content -Path (Join-Path $mappedRoot "mapped-authorize-request.json")
$mappedAcknowledge | ConvertTo-Json -Depth 20 | Set-Content -Path (Join-Path $mappedRoot "mapped-acknowledge-request.json")

$manifest = [ordered]@{
    createdUtc = [DateTime]::UtcNow.ToString("o")
    profile = $Profile
    authorizeInputPath = $AuthorizeInputPath
    acknowledgeInputPath = $AcknowledgeInputPath
    docs = @(
        "docs/caller-onboarding-checklist.md",
        "docs/pilot-adapter-checklist.md",
        "docs/caller-integration-rehearsal.md",
        "docs/real-caller-rehearsal.md"
    )
    inputs = @(
        "inputs/caller-authorize-request.json",
        "inputs/caller-acknowledge-request.json"
    )
    mapped = @(
        "mapped/mapped-authorize-request.json",
        "mapped/mapped-acknowledge-request.json"
    )
}

$manifest | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $rehearsalRoot "review-manifest.json")

Write-Host ("Prepared real caller rehearsal at " + $rehearsalRoot)
$rehearsalRoot
