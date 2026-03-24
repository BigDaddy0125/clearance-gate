[CmdletBinding()]
param(
    [string]$Profile = "itops_deployment_v1"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$adapterRoot = Join-Path $repoRoot "examples\pilot-adapter"
$converter = Join-Path $adapterRoot "convert-change-control-example.ps1"
$expectedAuthorizePath = Join-Path $adapterRoot "mapped-authorize-request.json"
$expectedAcknowledgePath = Join-Path $adapterRoot "mapped-acknowledge-request.json"

$actualAuthorize = & $converter -Mode authorize -Profile $Profile | ConvertFrom-Json
$actualAcknowledge = & $converter -Mode acknowledge -Profile $Profile | ConvertFrom-Json
$expectedAuthorize = Get-Content -Raw -Path $expectedAuthorizePath | ConvertFrom-Json
$expectedAcknowledge = Get-Content -Raw -Path $expectedAcknowledgePath | ConvertFrom-Json

$actualAuthorizeJson = $actualAuthorize | ConvertTo-Json -Depth 20
$actualAcknowledgeJson = $actualAcknowledge | ConvertTo-Json -Depth 20
$expectedAuthorizeJson = $expectedAuthorize | ConvertTo-Json -Depth 20
$expectedAcknowledgeJson = $expectedAcknowledge | ConvertTo-Json -Depth 20

if ($actualAuthorizeJson -ne $expectedAuthorizeJson) {
    throw "Authorize adapter output does not match the maintained example."
}

if ($actualAcknowledgeJson -ne $expectedAcknowledgeJson) {
    throw "Acknowledge adapter output does not match the maintained example."
}

if ([string]::IsNullOrWhiteSpace([string]$actualAuthorize.profile)) {
    throw "Authorize adapter output must keep an explicit profile."
}

if ([string]$actualAcknowledge.acknowledger.role -ne "acknowledging_authority") {
    throw "Acknowledge adapter output must keep role 'acknowledging_authority'."
}

Write-Host "Pilot adapter example is valid and deterministic."
