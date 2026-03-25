[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$OutputRoot = "",
    [string]$ProfileFamily = "itops_deployment",
    [string]$ExpectedProfileId = "itops_deployment_v1",
    [string]$ApiKey = "clearancegate-local-dev-key"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent

$resolvedOutputRoot =
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repoRoot "artifacts\pilot-session-capture"
    }
    else {
        $OutputRoot
    }

$authorizeTemplatePath = Join-Path $repoRoot "examples\v0\authorize-risk.json"
$acknowledgeTemplatePath = Join-Path $repoRoot "examples\v0\acknowledge-risk.json"
$sessionName = "pilot-session-" + [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
$sessionRoot = Join-Path $resolvedOutputRoot $sessionName
$requestsRoot = Join-Path $sessionRoot "requests"
$responsesRoot = Join-Path $sessionRoot "responses"

[System.IO.Directory]::CreateDirectory($requestsRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($responsesRoot) | Out-Null

$runSuffix = [Guid]::NewGuid().ToString("N").Substring(0, 12)
$requestId = "req-pilot-session-$runSuffix"
$decisionId = "dec-pilot-session-$runSuffix"
$evidenceId = "evidence:$decisionId"
$authorizeTimestamp = [DateTime]::UtcNow.ToString("o")
$acknowledgeTimestamp = [DateTime]::UtcNow.AddMinutes(1).ToString("o")
$authHeaders = @{ Authorization = "Bearer $ApiKey" }

function Write-JsonFile {
    param(
        [string]$Path,
        $Value
    )

    $Value | ConvertTo-Json -Depth 20 | Set-Content -Path $Path
}

Write-Host "Checking profile diagnostics..."
$profilesResponse = Invoke-RestMethod -Method Get -Uri "$BaseUrl/profiles" -Headers $authHeaders
$latestProfileResponse = Invoke-RestMethod -Method Get -Uri "$BaseUrl/profiles/latest/$ProfileFamily" -Headers $authHeaders

if ([string]$latestProfileResponse.profile.profile -ne $ExpectedProfileId) {
    throw "Latest profile mismatch. Expected '$ExpectedProfileId' but got '$($latestProfileResponse.profile.profile)'."
}

Write-JsonFile -Path (Join-Path $responsesRoot "profiles-response.json") -Value $profilesResponse
Write-JsonFile -Path (Join-Path $responsesRoot "latest-profile-response.json") -Value $latestProfileResponse

Write-Host "Preparing bounded authorize request..."
$authorizePayload = Get-Content -Raw -Path $authorizeTemplatePath | ConvertFrom-Json
$authorizePayload.requestId = $requestId
$authorizePayload.decisionId = $decisionId
$authorizePayload.profile = $ExpectedProfileId
$authorizePayload.metadata.timestamp = $authorizeTimestamp
Write-JsonFile -Path (Join-Path $requestsRoot "authorize-request.json") -Value $authorizePayload

Write-Host "Submitting authorize request..."
$authorizeResponse = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/authorize" `
    -Headers $authHeaders `
    -ContentType "application/json" `
    -Body ($authorizePayload | ConvertTo-Json -Depth 20)

if ([string]$authorizeResponse.outcome -ne "REQUIRE_ACK") {
    throw "Authorize outcome mismatch. Expected 'REQUIRE_ACK' but got '$($authorizeResponse.outcome)'."
}

if ([string]$authorizeResponse.clearanceState -ne "AWAITING_ACK") {
    throw "Authorize clearanceState mismatch. Expected 'AWAITING_ACK' but got '$($authorizeResponse.clearanceState)'."
}

Write-JsonFile -Path (Join-Path $responsesRoot "authorize-response.json") -Value $authorizeResponse

Write-Host "Preparing bounded acknowledgment..."
$acknowledgePayload = Get-Content -Raw -Path $acknowledgeTemplatePath | ConvertFrom-Json
$acknowledgePayload.decisionId = $decisionId
$acknowledgePayload.acknowledgment.timestamp = $acknowledgeTimestamp
Write-JsonFile -Path (Join-Path $requestsRoot "acknowledge-request.json") -Value $acknowledgePayload

Write-Host "Submitting acknowledgment..."
$acknowledgeResponse = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/acknowledge" `
    -Headers $authHeaders `
    -ContentType "application/json" `
    -Body ($acknowledgePayload | ConvertTo-Json -Depth 20)

if ([string]$acknowledgeResponse.outcome -ne "PROCEED") {
    throw "Acknowledge outcome mismatch. Expected 'PROCEED' but got '$($acknowledgeResponse.outcome)'."
}

if ([string]$acknowledgeResponse.clearanceState -ne "AUTHORIZED") {
    throw "Acknowledge clearanceState mismatch. Expected 'AUTHORIZED' but got '$($acknowledgeResponse.clearanceState)'."
}

Write-JsonFile -Path (Join-Path $responsesRoot "acknowledge-response.json") -Value $acknowledgeResponse

Write-Host "Reading audit views..."
$compactByDecision = Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/$decisionId" -Headers $authHeaders
$exportByDecision = Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/$decisionId/export" -Headers $authHeaders
$compactByRequest = Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/request/$requestId" -Headers $authHeaders
$exportByRequest = Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/request/$requestId/export" -Headers $authHeaders

if ([string]$compactByDecision.evidenceId -ne $evidenceId) {
    throw "Compact audit evidenceId mismatch. Expected '$evidenceId' but got '$($compactByDecision.evidenceId)'."
}

if ([string]$exportByDecision.decisionId -ne $decisionId) {
    throw "Export audit decisionId mismatch. Expected '$decisionId' but got '$($exportByDecision.decisionId)'."
}

Write-JsonFile -Path (Join-Path $responsesRoot "audit-compact.json") -Value $compactByDecision
Write-JsonFile -Path (Join-Path $responsesRoot "audit-export.json") -Value $exportByDecision
Write-JsonFile -Path (Join-Path $responsesRoot "audit-request-compact.json") -Value $compactByRequest
Write-JsonFile -Path (Join-Path $responsesRoot "audit-request-export.json") -Value $exportByRequest

Write-Host "Packaging pilot evidence..."
$pilotEvidenceRoot = Join-Path $repoRoot "artifacts\pilot-evidence"
& (Join-Path $repoRoot "scripts\package-pilot-evidence.ps1") `
    -AuthorizeResponsePath (Join-Path $responsesRoot "authorize-response.json") `
    -AcknowledgeResponsePath (Join-Path $responsesRoot "acknowledge-response.json") `
    -CompactAuditPath (Join-Path $responsesRoot "audit-compact.json") `
    -ExportAuditPath (Join-Path $responsesRoot "audit-export.json") `
    -ProfilesResponsePath (Join-Path $responsesRoot "profiles-response.json")

$latestPackage = Get-ChildItem -Path $pilotEvidenceRoot -Directory |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if ($null -eq $latestPackage) {
    throw "Pilot evidence package was not created under '$pilotEvidenceRoot'."
}

$packagePath = $latestPackage.FullName

$captureManifest = [ordered]@{
    createdUtc = [DateTime]::UtcNow.ToString("o")
    baseUrl = $BaseUrl
    profileFamily = $ProfileFamily
    profile = $ExpectedProfileId
    requestId = $requestId
    decisionId = $decisionId
    evidenceId = $evidenceId
    authorizeRequestPath = "requests/authorize-request.json"
    acknowledgeRequestPath = "requests/acknowledge-request.json"
    authorizeResponsePath = "responses/authorize-response.json"
    acknowledgeResponsePath = "responses/acknowledge-response.json"
    compactAuditPath = "responses/audit-compact.json"
    exportAuditPath = "responses/audit-export.json"
    packagedEvidenceRoot = $packagePath
}

$captureManifest | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $sessionRoot "capture-manifest.json")

Write-Host ("Captured pilot session to " + $sessionRoot)
