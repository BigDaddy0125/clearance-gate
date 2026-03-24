[CmdletBinding()]
param(
    [string]$BaseUrl = "http://127.0.0.1:5083",
    [string]$OutputRoot = "",
    [string]$AuditStorePath = "",
    [string]$AuthorizeInputPath = "",
    [string]$AcknowledgeInputPath = "",
    [string]$Profile = "itops_deployment_v1"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$nearRealExamplesRoot = Join-Path $repoRoot "examples\real-caller-intake"

$resolvedOutputRoot =
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repoRoot "artifacts\near-real-pilot-dry-run"
    }
    else {
        $OutputRoot
    }

$resolvedAuditStorePath =
    if ([string]::IsNullOrWhiteSpace($AuditStorePath)) {
        Join-Path $repoRoot "artifacts\near-real-pilot-dry-run\near-real-pilot.db"
    }
    else {
        $AuditStorePath
    }

$resolvedAuthorizeInputPath =
    if ([string]::IsNullOrWhiteSpace($AuthorizeInputPath)) {
        Join-Path $nearRealExamplesRoot "near-real-authorize.json"
    }
    else {
        $AuthorizeInputPath
    }

$resolvedAcknowledgeInputPath =
    if ([string]::IsNullOrWhiteSpace($AcknowledgeInputPath)) {
        Join-Path $nearRealExamplesRoot "near-real-acknowledge.json"
    }
    else {
        $AcknowledgeInputPath
    }

& (Join-Path $repoRoot "scripts\validate-release-bundle.ps1") | Out-Null
& (Join-Path $repoRoot "scripts\prepare-pilot-rollout.ps1") | Out-Null

$publishRoot = Join-Path $repoRoot "artifacts\publish\app"
$publishedExe = Join-Path $publishRoot "ClearanceGate.Api.exe"
$publishedDll = Join-Path $publishRoot "ClearanceGate.Api.dll"

if (Test-Path $publishedExe) {
    $hostCommand = $publishedExe
    $hostArguments = @("--urls", $BaseUrl)
}
elseif (Test-Path $publishedDll) {
    $hostCommand = "dotnet"
    $hostArguments = @($publishedDll, "--urls", $BaseUrl)
}
else {
    throw "Published API host is missing from '$publishRoot'."
}

$dryRunName = "near-real-pilot-dry-run-" + [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
$dryRunRoot = Join-Path $resolvedOutputRoot $dryRunName
[System.IO.Directory]::CreateDirectory($dryRunRoot) | Out-Null
[System.IO.Directory]::CreateDirectory((Split-Path $resolvedAuditStorePath -Parent)) | Out-Null

$job = Start-Job -ScriptBlock {
    param(
        [string]$repoRoot,
        [string]$auditStorePath,
        [string]$hostCommand,
        [string[]]$hostArguments
    )

    Set-Location $repoRoot
    $env:ConnectionStrings__AuditStore = "Data Source=$auditStorePath"
    & $hostCommand @hostArguments
} -ArgumentList $repoRoot, $resolvedAuditStorePath, $hostCommand, $hostArguments

try {
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        Start-Sleep -Seconds 1
        try {
            Invoke-RestMethod -Method Get -Uri "$BaseUrl/profiles" | Out-Null
            break
        }
        catch {
            if ($attempt -eq 30) {
                throw "Near-real pilot dry-run host did not become ready at '$BaseUrl'."
            }
        }
    }

    & (Join-Path $repoRoot "scripts\run-caller-integration-rehearsal.ps1") `
        -BaseUrl $BaseUrl `
        -AuditStorePath $resolvedAuditStorePath `
        -AuthorizeInputPath $resolvedAuthorizeInputPath `
        -AcknowledgeInputPath $resolvedAcknowledgeInputPath `
        -Profile $Profile `
        -UseExistingHost | Out-Null

    & (Join-Path $repoRoot "scripts\prepare-post-pilot-review.ps1") | Out-Null
}
finally {
    Stop-Job $job -ErrorAction SilentlyContinue | Out-Null
    Remove-Job $job -Force -ErrorAction SilentlyContinue | Out-Null
}

$latestPilotRollout = Get-ChildItem -Path (Join-Path $repoRoot "artifacts\pilot-rollout") -Directory |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

$latestCallerRehearsal = Get-ChildItem -Path (Join-Path $repoRoot "artifacts\caller-integration-rehearsal") -Directory |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

$latestPilotEvidence = Get-ChildItem -Path (Join-Path $repoRoot "artifacts\pilot-evidence") -Directory |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

$latestPostPilotReview = Get-ChildItem -Path (Join-Path $repoRoot "artifacts\post-pilot-review") -Directory |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

$dryRunManifest = [ordered]@{
    createdUtc = [DateTime]::UtcNow.ToString("o")
    baseUrl = $BaseUrl
    profile = $Profile
    auditStorePath = $resolvedAuditStorePath
    authorizeInputPath = $resolvedAuthorizeInputPath
    acknowledgeInputPath = $resolvedAcknowledgeInputPath
    pilotRolloutRoot = if ($null -eq $latestPilotRollout) { "" } else { $latestPilotRollout.FullName }
    callerIntegrationRehearsalRoot = if ($null -eq $latestCallerRehearsal) { "" } else { $latestCallerRehearsal.FullName }
    pilotEvidenceRoot = if ($null -eq $latestPilotEvidence) { "" } else { $latestPilotEvidence.FullName }
    postPilotReviewRoot = if ($null -eq $latestPostPilotReview) { "" } else { $latestPostPilotReview.FullName }
}

$dryRunManifest | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $dryRunRoot "dry-run-manifest.json")

Write-Host ("Near-real pilot dry-run completed at " + $dryRunRoot)
