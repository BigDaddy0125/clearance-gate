[CmdletBinding()]
param(
    [string]$Profile = "itops_deployment_v1",
    [string]$OutputRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent

$resolvedOutputRoot =
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repoRoot "artifacts\real-caller-intake"
    }
    else {
        $OutputRoot
    }

& (Join-Path $repoRoot "scripts\initialize-real-caller-intake.ps1") `
    -CallerSystem "example-caller" `
    -Profile $Profile `
    -ActionDescription "Example gated deployment action" `
    -OutputRoot $resolvedOutputRoot | Out-Null

$latestIntake = Get-ChildItem -Path $resolvedOutputRoot -Directory |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if ($null -eq $latestIntake) {
    throw "Failed to initialize a sample real caller intake package."
}

$authorizeExample = Get-Content -Raw -Path (Join-Path $repoRoot "examples\pilot-adapter\change-control-request.json") | ConvertFrom-Json
$acknowledgeExample = Get-Content -Raw -Path (Join-Path $repoRoot "examples\pilot-adapter\change-control-ack.json") | ConvertFrom-Json

$authorizeExample | ConvertTo-Json -Depth 20 | Set-Content -Path (Join-Path $latestIntake.FullName "inputs\caller-authorize.json")
$acknowledgeExample | ConvertTo-Json -Depth 20 | Set-Content -Path (Join-Path $latestIntake.FullName "inputs\caller-acknowledge.json")

$manifestPath = Join-Path $latestIntake.FullName "intake-manifest.json"
$manifest = Get-Content -Raw -Path $manifestPath | ConvertFrom-Json
$manifest.status = "READY_FOR_REHEARSAL"
$manifest.notes = @(
    "Sample intake created from the maintained change-control example.",
    "This package is safe for CI and local validation only.",
    "Replace with caller-owned payloads before a real rehearsal."
)

$manifest | ConvertTo-Json -Depth 10 | Set-Content -Path $manifestPath

Write-Host ("Created sample real caller intake at " + $latestIntake.FullName)
