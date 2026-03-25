[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$IntakeRoot,

    [string]$OutputRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$resolvedIntakeRoot = [System.IO.Path]::GetFullPath($IntakeRoot)

$resolvedOutputRoot =
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repoRoot "artifacts\real-caller-promotion"
    }
    else {
        $OutputRoot
    }

& (Join-Path $repoRoot "scripts\validate-real-caller-intake-package.ps1") `
    -IntakeRoot $resolvedIntakeRoot `
    -RequireReadyStatus | Out-Null

$manifestPath = Join-Path $resolvedIntakeRoot "intake-manifest.json"
$manifest = Get-Content -Raw -Path $manifestPath | ConvertFrom-Json

$rehearsalRoot = & (Join-Path $repoRoot "scripts\prepare-real-caller-rehearsal.ps1") `
    -AuthorizeInputPath (Join-Path $resolvedIntakeRoot "inputs\caller-authorize.json") `
    -AcknowledgeInputPath (Join-Path $resolvedIntakeRoot "inputs\caller-acknowledge.json") `
    -Profile ([string]$manifest.profile)

if ([string]::IsNullOrWhiteSpace([string]$rehearsalRoot) -or -not (Test-Path $rehearsalRoot)) {
    throw "Failed to locate the prepared real caller rehearsal directory."
}

$promotionName = "real-caller-promotion-" + [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
$promotionRoot = Join-Path $resolvedOutputRoot $promotionName
[System.IO.Directory]::CreateDirectory($promotionRoot) | Out-Null

Copy-Item -Path $manifestPath -Destination (Join-Path $promotionRoot "intake-manifest.json")

Copy-Item -Path (Join-Path $rehearsalRoot "review-manifest.json") -Destination (Join-Path $promotionRoot "rehearsal-review-manifest.json")

$promotionManifest = [ordered]@{
    createdUtc = [DateTime]::UtcNow.ToString("o")
    intakeRoot = $resolvedIntakeRoot
    callerSystem = [string]$manifest.callerSystem
    profile = [string]$manifest.profile
    actionDescription = [string]$manifest.actionDescription
    rehearsalRoot = $rehearsalRoot
}

$promotionManifest | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $promotionRoot "promotion-manifest.json")

Write-Host ("Promoted real caller intake at " + $promotionRoot)
$promotionRoot
