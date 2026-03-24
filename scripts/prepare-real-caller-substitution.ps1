[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CallerSystem,

    [Parameter(Mandatory = $true)]
    [string]$ActionDescription,

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

$resolvedOutputRoot =
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repoRoot "artifacts\real-caller-substitution"
    }
    else {
        $OutputRoot
    }

if (-not (Test-Path $AuthorizeInputPath)) {
    throw "Authorize input is missing at '$AuthorizeInputPath'."
}

if (-not (Test-Path $AcknowledgeInputPath)) {
    throw "Acknowledge input is missing at '$AcknowledgeInputPath'."
}

& (Join-Path $repoRoot "scripts\initialize-real-caller-intake.ps1") `
    -CallerSystem $CallerSystem `
    -Profile $Profile `
    -ActionDescription $ActionDescription | Out-Null

$latestIntake = Get-ChildItem -Path (Join-Path $repoRoot "artifacts\real-caller-intake") -Directory |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if ($null -eq $latestIntake) {
    throw "Failed to initialize real caller intake."
}

$intakeRoot = $latestIntake.FullName
$intakeAuthorizePath = Join-Path $intakeRoot "inputs\caller-authorize.json"
$intakeAcknowledgePath = Join-Path $intakeRoot "inputs\caller-acknowledge.json"

Copy-Item -Path $AuthorizeInputPath -Destination $intakeAuthorizePath -Force
Copy-Item -Path $AcknowledgeInputPath -Destination $intakeAcknowledgePath -Force

$manifestPath = Join-Path $intakeRoot "intake-manifest.json"
$manifest = Get-Content -Raw -Path $manifestPath | ConvertFrom-Json
$manifest.status = "READY_FOR_REHEARSAL"
$manifest.notes = @(
    "Prepared from caller-owned substitution payloads.",
    "Run validation before live rehearsal.",
    "Do not replace explicit profile selection with diagnostics lookups."
)
$manifest | ConvertTo-Json -Depth 10 | Set-Content -Path $manifestPath

& (Join-Path $repoRoot "scripts\validate-real-caller-intake-package.ps1") `
    -IntakeRoot $intakeRoot `
    -RequireReadyStatus | Out-Null

& (Join-Path $repoRoot "scripts\promote-real-caller-intake.ps1") `
    -IntakeRoot $intakeRoot | Out-Null

$latestPromotion = Get-ChildItem -Path (Join-Path $repoRoot "artifacts\real-caller-promotion") -Directory |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if ($null -eq $latestPromotion) {
    throw "Failed to create real caller promotion package."
}

$substitutionName = "real-caller-substitution-" + [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
$substitutionRoot = Join-Path $resolvedOutputRoot $substitutionName
[System.IO.Directory]::CreateDirectory($substitutionRoot) | Out-Null

$substitutionManifest = [ordered]@{
    createdUtc = [DateTime]::UtcNow.ToString("o")
    callerSystem = $CallerSystem
    profile = $Profile
    actionDescription = $ActionDescription
    authorizeInputPath = $AuthorizeInputPath
    acknowledgeInputPath = $AcknowledgeInputPath
    intakeRoot = $intakeRoot
    promotionRoot = $latestPromotion.FullName
    nextCommand = "powershell -ExecutionPolicy Bypass -File .\scripts\run-caller-integration-rehearsal.ps1 -AuthorizeInputPath `"$AuthorizeInputPath`" -AcknowledgeInputPath `"$AcknowledgeInputPath`" -Profile $Profile"
}

$substitutionManifest | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $substitutionRoot "substitution-manifest.json")

Write-Host ("Prepared real caller substitution at " + $substitutionRoot)
