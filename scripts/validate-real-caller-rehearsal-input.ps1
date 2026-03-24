[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$AuthorizeInputPath,

    [Parameter(Mandatory = $true)]
    [string]$AcknowledgeInputPath,

    [string]$Profile = "itops_deployment_v1"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$converter = Join-Path $repoRoot "examples\pilot-adapter\convert-change-control-example.ps1"

if (-not (Test-Path $AuthorizeInputPath)) {
    throw "Authorize input is missing at '$AuthorizeInputPath'."
}

if (-not (Test-Path $AcknowledgeInputPath)) {
    throw "Acknowledge input is missing at '$AcknowledgeInputPath'."
}

$authorizeInput = Get-Content -Raw -Path $AuthorizeInputPath | ConvertFrom-Json
$acknowledgeInput = Get-Content -Raw -Path $AcknowledgeInputPath | ConvertFrom-Json

$authorizeMapped = & $converter `
    -Mode authorize `
    -Profile $Profile `
    -AuthorizeInputPath $AuthorizeInputPath | ConvertFrom-Json

$acknowledgeMapped = & $converter `
    -Mode acknowledge `
    -Profile $Profile `
    -AcknowledgeInputPath $AcknowledgeInputPath | ConvertFrom-Json

if ([string]$authorizeInput.executionId -ne [string]$acknowledgeInput.executionId) {
    throw "Caller authorize and acknowledge payloads must keep the same executionId."
}

if ([string]$authorizeMapped.decisionId -ne [string]$acknowledgeMapped.decisionId) {
    throw "Mapped authorize and acknowledge payloads must keep the same decisionId."
}

if ([string]::IsNullOrWhiteSpace([string]$authorizeMapped.requestId)) {
    throw "Mapped authorize payload must keep an explicit requestId."
}

if ([string]::IsNullOrWhiteSpace([string]$authorizeMapped.profile)) {
    throw "Mapped authorize payload must keep an explicit profile."
}

if ([string]$acknowledgeMapped.acknowledger.role -ne "acknowledging_authority") {
    throw "Mapped acknowledge payload must keep role 'acknowledging_authority'."
}

Write-Host "Real caller rehearsal inputs are valid and map deterministically."
