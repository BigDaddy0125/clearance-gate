[CmdletBinding()]
param(
    [string]$BaseUrl = "http://127.0.0.1:5082",
    [string]$OutputRoot = "",
    [string]$AuditStorePath = "",
    [string]$Profile = "itops_deployment_v1",
    [string]$AuthorizeInputPath = "",
    [string]$AcknowledgeInputPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$adapterRoot = Join-Path $repoRoot "examples\pilot-adapter"
$converterPath = Join-Path $adapterRoot "convert-change-control-example.ps1"

$resolvedAuthorizeInputPath =
    if ([string]::IsNullOrWhiteSpace($AuthorizeInputPath)) {
        Join-Path $adapterRoot "change-control-request.json"
    }
    else {
        $AuthorizeInputPath
    }

$resolvedAcknowledgeInputPath =
    if ([string]::IsNullOrWhiteSpace($AcknowledgeInputPath)) {
        Join-Path $adapterRoot "change-control-ack.json"
    }
    else {
        $AcknowledgeInputPath
    }

$resolvedOutputRoot =
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repoRoot "artifacts\caller-integration-rehearsal"
    }
    else {
        $OutputRoot
    }

$resolvedAuditStorePath =
    if ([string]::IsNullOrWhiteSpace($AuditStorePath)) {
        Join-Path $repoRoot "artifacts\caller-integration-rehearsal\caller-integration.db"
    }
    else {
        $AuditStorePath
    }

& (Join-Path $repoRoot "scripts\validate-release-bundle.ps1") | Out-Null
& (Join-Path $repoRoot "scripts\validate-pilot-adapter-example.ps1") -Profile $Profile | Out-Null
& (Join-Path $repoRoot "scripts\validate-real-caller-rehearsal-input.ps1") `
    -AuthorizeInputPath $resolvedAuthorizeInputPath `
    -AcknowledgeInputPath $resolvedAcknowledgeInputPath `
    -Profile $Profile | Out-Null
& (Join-Path $repoRoot "scripts\prepare-caller-integration-review.ps1") | Out-Null

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

$rehearsalName = "caller-integration-rehearsal-" + [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
$rehearsalRoot = Join-Path $resolvedOutputRoot $rehearsalName
$requestsRoot = Join-Path $rehearsalRoot "requests"
$responsesRoot = Join-Path $rehearsalRoot "responses"

[System.IO.Directory]::CreateDirectory($rehearsalRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($requestsRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($responsesRoot) | Out-Null
[System.IO.Directory]::CreateDirectory((Split-Path $resolvedAuditStorePath -Parent)) | Out-Null

$runSuffix = [Guid]::NewGuid().ToString("N").Substring(0, 12)
$requestId = "req-caller-rehearsal-$runSuffix"
$decisionId = "dec-caller-rehearsal-$runSuffix"
$authorizeTimestamp = [DateTime]::UtcNow.ToString("o")
$ackTimestamp = [DateTime]::UtcNow.AddMinutes(1).ToString("o")

function Write-JsonFile {
    param(
        [string]$Path,
        $Value
    )

    $Value | ConvertTo-Json -Depth 20 | Set-Content -Path $Path
}

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
                throw "Caller integration rehearsal host did not become ready at '$BaseUrl'."
            }
        }
    }

    $callerAuthorize = Get-Content -Raw -Path $resolvedAuthorizeInputPath | ConvertFrom-Json
    $callerAuthorize.ticketId = $requestId
    $callerAuthorize.executionId = $decisionId
    $callerAuthorize.source.recordedAt = $authorizeTimestamp
    Write-JsonFile -Path (Join-Path $requestsRoot "caller-authorize-request.json") -Value $callerAuthorize

    $callerAcknowledge = Get-Content -Raw -Path $resolvedAcknowledgeInputPath | ConvertFrom-Json
    $callerAcknowledge.executionId = $decisionId
    $callerAcknowledge.authority.recordedAt = $ackTimestamp
    Write-JsonFile -Path (Join-Path $requestsRoot "caller-acknowledge-request.json") -Value $callerAcknowledge

    $authorizeMapped = & $converterPath `
        -Mode authorize `
        -Profile $Profile `
        -AuthorizeInputPath (Join-Path $requestsRoot "caller-authorize-request.json") | ConvertFrom-Json
    $authorizeMapped.requestId = $requestId
    $authorizeMapped.decisionId = $decisionId
    $authorizeMapped.profile = $Profile
    $authorizeMapped.metadata.timestamp = $authorizeTimestamp
    Write-JsonFile -Path (Join-Path $requestsRoot "mapped-authorize-request.json") -Value $authorizeMapped

    $ackMapped = & $converterPath `
        -Mode acknowledge `
        -Profile $Profile `
        -AcknowledgeInputPath (Join-Path $requestsRoot "caller-acknowledge-request.json") | ConvertFrom-Json
    $ackMapped.decisionId = $decisionId
    $ackMapped.acknowledgment.timestamp = $ackTimestamp
    Write-JsonFile -Path (Join-Path $requestsRoot "mapped-acknowledge-request.json") -Value $ackMapped

    $profilesResponse = Invoke-RestMethod -Method Get -Uri "$BaseUrl/profiles"
    Write-JsonFile -Path (Join-Path $responsesRoot "profiles-response.json") -Value $profilesResponse

    $authorizeResponse = Invoke-RestMethod `
        -Method Post `
        -Uri "$BaseUrl/authorize" `
        -ContentType "application/json" `
        -Body ($authorizeMapped | ConvertTo-Json -Depth 20)

    if ([string]$authorizeResponse.outcome -ne "REQUIRE_ACK") {
        throw "Authorize outcome mismatch. Expected 'REQUIRE_ACK' but got '$($authorizeResponse.outcome)'."
    }

    Write-JsonFile -Path (Join-Path $responsesRoot "authorize-response.json") -Value $authorizeResponse

    $ackResponse = Invoke-RestMethod `
        -Method Post `
        -Uri "$BaseUrl/acknowledge" `
        -ContentType "application/json" `
        -Body ($ackMapped | ConvertTo-Json -Depth 20)

    if ([string]$ackResponse.outcome -ne "PROCEED") {
        throw "Acknowledge outcome mismatch. Expected 'PROCEED' but got '$($ackResponse.outcome)'."
    }

    Write-JsonFile -Path (Join-Path $responsesRoot "acknowledge-response.json") -Value $ackResponse

    $compactAudit = Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/$decisionId"
    $exportAudit = Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/$decisionId/export"
    $requestCompact = Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/request/$requestId"
    $requestExport = Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/request/$requestId/export"

    Write-JsonFile -Path (Join-Path $responsesRoot "audit-compact.json") -Value $compactAudit
    Write-JsonFile -Path (Join-Path $responsesRoot "audit-export.json") -Value $exportAudit
    Write-JsonFile -Path (Join-Path $responsesRoot "audit-request-compact.json") -Value $requestCompact
    Write-JsonFile -Path (Join-Path $responsesRoot "audit-request-export.json") -Value $requestExport

    & (Join-Path $repoRoot "scripts\package-pilot-evidence.ps1") `
        -AuthorizeResponsePath (Join-Path $responsesRoot "authorize-response.json") `
        -AcknowledgeResponsePath (Join-Path $responsesRoot "acknowledge-response.json") `
        -CompactAuditPath (Join-Path $responsesRoot "audit-compact.json") `
        -ExportAuditPath (Join-Path $responsesRoot "audit-export.json") `
        -ProfilesResponsePath (Join-Path $responsesRoot "profiles-response.json") | Out-Null
}
finally {
    Stop-Job $job -ErrorAction SilentlyContinue | Out-Null
    Remove-Job $job -Force -ErrorAction SilentlyContinue | Out-Null
}

$latestEvidence = Get-ChildItem -Path (Join-Path $repoRoot "artifacts\pilot-evidence") -Directory |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

$latestCallerReview = Get-ChildItem -Path (Join-Path $repoRoot "artifacts\caller-integration-review") -Directory |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

$rehearsalManifest = [ordered]@{
    createdUtc = [DateTime]::UtcNow.ToString("o")
    baseUrl = $BaseUrl
    profile = $Profile
    requestId = $requestId
    decisionId = $decisionId
    auditStorePath = $resolvedAuditStorePath
    authorizeInputPath = $resolvedAuthorizeInputPath
    acknowledgeInputPath = $resolvedAcknowledgeInputPath
    pilotEvidenceRoot = if ($null -eq $latestEvidence) { "" } else { $latestEvidence.FullName }
    callerIntegrationReviewRoot = if ($null -eq $latestCallerReview) { "" } else { $latestCallerReview.FullName }
}

$rehearsalManifest | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $rehearsalRoot "rehearsal-manifest.json")

Write-Host ("Caller integration rehearsal completed at " + $rehearsalRoot)
