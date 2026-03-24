[CmdletBinding()]
param(
    [string]$BaseUrl = "http://127.0.0.1:5081",
    [string]$OutputRoot = "",
    [string]$AuditStorePath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent

$resolvedOutputRoot =
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repoRoot "artifacts\controlled-pilot-dry-run"
    }
    else {
        $OutputRoot
    }

$resolvedAuditStorePath =
    if ([string]::IsNullOrWhiteSpace($AuditStorePath)) {
        Join-Path $repoRoot "artifacts\controlled-pilot-dry-run\dry-run.db"
    }
    else {
        $AuditStorePath
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

$dryRunName = "controlled-pilot-dry-run-" + [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
$dryRunRoot = Join-Path $resolvedOutputRoot $dryRunName
$dbDirectory = Split-Path $resolvedAuditStorePath -Parent
[System.IO.Directory]::CreateDirectory($dryRunRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($dbDirectory) | Out-Null

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
                throw "Controlled pilot dry-run host did not become ready at '$BaseUrl'."
            }
        }
    }

    & (Join-Path $repoRoot "scripts\run-deployment-smoke-check.ps1") -BaseUrl $BaseUrl | Out-Null
    & (Join-Path $repoRoot "scripts\capture-pilot-sample-session.ps1") -BaseUrl $BaseUrl | Out-Null
    & (Join-Path $repoRoot "scripts\prepare-post-pilot-review.ps1") | Out-Null
}
finally {
    Stop-Job $job -ErrorAction SilentlyContinue | Out-Null
    Remove-Job $job -Force -ErrorAction SilentlyContinue | Out-Null
}

$latestPilotRollout = Get-ChildItem -Path (Join-Path $repoRoot "artifacts\pilot-rollout") -Directory |
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
    auditStorePath = $resolvedAuditStorePath
    pilotRolloutRoot = if ($null -eq $latestPilotRollout) { "" } else { $latestPilotRollout.FullName }
    pilotEvidenceRoot = if ($null -eq $latestPilotEvidence) { "" } else { $latestPilotEvidence.FullName }
    postPilotReviewRoot = if ($null -eq $latestPostPilotReview) { "" } else { $latestPostPilotReview.FullName }
}

$dryRunManifest | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $dryRunRoot "dry-run-manifest.json")

Write-Host ("Controlled pilot dry-run completed at " + $dryRunRoot)
