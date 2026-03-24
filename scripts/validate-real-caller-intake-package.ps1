[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$IntakeRoot,

    [switch]$RequireReadyStatus
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$resolvedIntakeRoot = [System.IO.Path]::GetFullPath($IntakeRoot)

if (-not (Test-Path $resolvedIntakeRoot)) {
    throw "Intake root is missing at '$resolvedIntakeRoot'."
}

$manifestPath = Join-Path $resolvedIntakeRoot "intake-manifest.json"
if (-not (Test-Path $manifestPath)) {
    throw "Intake manifest is missing at '$manifestPath'."
}

$manifest = Get-Content -Raw -Path $manifestPath | ConvertFrom-Json

if ([string]::IsNullOrWhiteSpace([string]$manifest.callerSystem)) {
    throw "Intake manifest must declare callerSystem."
}

if ([string]::IsNullOrWhiteSpace([string]$manifest.profile)) {
    throw "Intake manifest must declare an explicit profile."
}

if ([string]::IsNullOrWhiteSpace([string]$manifest.actionDescription)) {
    throw "Intake manifest must declare actionDescription."
}

if ($RequireReadyStatus -and [string]$manifest.status -ne "READY_FOR_REHEARSAL") {
    throw "Intake manifest must be READY_FOR_REHEARSAL before promotion."
}

$requiredDocs = @(
    "docs\caller-onboarding-checklist.md",
    "docs\pilot-adapter-checklist.md",
    "docs\real-caller-intake.md",
    "docs\real-caller-rehearsal.md"
)

foreach ($relativePath in $requiredDocs) {
    $fullPath = Join-Path $resolvedIntakeRoot $relativePath
    if (-not (Test-Path $fullPath)) {
        throw "Intake package is missing required document '$relativePath'."
    }
}

$authorizePath = Join-Path $resolvedIntakeRoot "inputs\caller-authorize.json"
$acknowledgePath = Join-Path $resolvedIntakeRoot "inputs\caller-acknowledge.json"

if (-not (Test-Path $authorizePath)) {
    throw "Intake package is missing caller authorize payload."
}

if (-not (Test-Path $acknowledgePath)) {
    throw "Intake package is missing caller acknowledge payload."
}

$placeholderPattern = 'replace-with'
$inputTexts = @(
    (Get-Content -Raw -Path $authorizePath),
    (Get-Content -Raw -Path $acknowledgePath),
    (Get-Content -Raw -Path $manifestPath)
)

foreach ($inputText in $inputTexts) {
    if ($inputText -match $placeholderPattern) {
        throw "Intake package still contains placeholder values."
    }
}

& (Join-Path $repoRoot "scripts\validate-real-caller-rehearsal-input.ps1") `
    -AuthorizeInputPath $authorizePath `
    -AcknowledgeInputPath $acknowledgePath `
    -Profile ([string]$manifest.profile) | Out-Null

Write-Host ("Validated real caller intake package at " + $resolvedIntakeRoot)
