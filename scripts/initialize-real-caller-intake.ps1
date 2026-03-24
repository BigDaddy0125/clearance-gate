[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CallerSystem,

    [string]$Profile = "itops_deployment_v1",

    [string]$ActionDescription = "",

    [string]$OutputRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$templatesRoot = Join-Path $repoRoot "examples\real-caller-intake"

$resolvedOutputRoot =
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repoRoot "artifacts\real-caller-intake"
    }
    else {
        $OutputRoot
    }

$intakeName = "real-caller-intake-" + [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
$intakeRoot = Join-Path $resolvedOutputRoot $intakeName
$docsRoot = Join-Path $intakeRoot "docs"
$inputsRoot = Join-Path $intakeRoot "inputs"

[System.IO.Directory]::CreateDirectory($docsRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($inputsRoot) | Out-Null

$docsToCopy = @(
    "docs\caller-onboarding-checklist.md",
    "docs\pilot-adapter-checklist.md",
    "docs\real-caller-intake.md",
    "docs\real-caller-rehearsal.md"
)

foreach ($relativePath in $docsToCopy) {
    Copy-Item -Path (Join-Path $repoRoot $relativePath) -Destination $docsRoot
}

Copy-Item -Path (Join-Path $templatesRoot "caller-authorize.template.json") -Destination (Join-Path $inputsRoot "caller-authorize.json")
Copy-Item -Path (Join-Path $templatesRoot "caller-acknowledge.template.json") -Destination (Join-Path $inputsRoot "caller-acknowledge.json")

$manifestTemplate = Get-Content -Raw -Path (Join-Path $templatesRoot "intake-manifest.template.json") | ConvertFrom-Json
$manifestTemplate.callerSystem = $CallerSystem
$manifestTemplate.profile = $Profile
$manifestTemplate.actionDescription =
    if ([string]::IsNullOrWhiteSpace($ActionDescription)) {
        "Replace with the real gated execution action."
    }
    else {
        $ActionDescription
    }

$manifest = [ordered]@{
    createdUtc = [DateTime]::UtcNow.ToString("o")
    callerSystem = [string]$manifestTemplate.callerSystem
    profile = [string]$manifestTemplate.profile
    actionDescription = [string]$manifestTemplate.actionDescription
    status = "DRAFT"
    inputs = @(
        "inputs/caller-authorize.json",
        "inputs/caller-acknowledge.json"
    )
    docs = @(
        "docs/caller-onboarding-checklist.md",
        "docs/pilot-adapter-checklist.md",
        "docs/real-caller-intake.md",
        "docs/real-caller-rehearsal.md"
    )
    notes = @(
        "Fill in the caller-owned payload drafts before rehearsal.",
        "Keep profile explicit and deterministic.",
        "Do not widen the adapter shape without an explicit product decision."
    )
}

$manifest | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $intakeRoot "intake-manifest.json")

Write-Host ("Initialized real caller intake at " + $intakeRoot)
