[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$ProfileFamily = "itops_deployment",
    [string]$ExpectedProfileId = "itops_deployment_v1"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$authorizeRisk = Join-Path $repoRoot "examples\v0\authorize-risk.json"
$ackRisk = Join-Path $repoRoot "examples\v0\acknowledge-risk.json"
$runSuffix = [Guid]::NewGuid().ToString("N").Substring(0, 12)
$expectedDecisionId = "dec-deployment-smoke-$runSuffix"
$expectedRequestId = "req-deployment-smoke-$runSuffix"
$expectedEvidenceId = "evidence:$expectedDecisionId"
$authorizeTimestamp = [DateTime]::UtcNow.ToString("o")
$ackTimestamp = [DateTime]::UtcNow.AddMinutes(1).ToString("o")

function Assert-Equal {
    param(
        [string]$Name,
        $Actual,
        $Expected
    )

    if ($Actual -cne $Expected) {
        throw "$Name mismatch. Expected '$Expected' but got '$Actual'."
    }
}

function Assert-True {
    param(
        [string]$Name,
        [bool]$Condition
    )

    if (-not $Condition) {
        throw "$Name failed."
    }
}

Write-Host "Checking profile catalog..."
$profiles = Invoke-RestMethod -Method Get -Uri "$BaseUrl/profiles"
$latestProfile = Invoke-RestMethod -Method Get -Uri "$BaseUrl/profiles/latest/$ProfileFamily"

Assert-Equal -Name "Latest profile id" -Actual $latestProfile.profile.profile -Expected $ExpectedProfileId
Assert-True -Name "Catalog contains expected profile" -Condition ($profiles.profiles.profile -contains $ExpectedProfileId)

Write-Host "Authorizing bounded-risk request..."
$authorizePayload = Get-Content -Raw -Path $authorizeRisk | ConvertFrom-Json
$authorizePayload.requestId = $expectedRequestId
$authorizePayload.decisionId = $expectedDecisionId
$authorizePayload.profile = $ExpectedProfileId
$authorizePayload.metadata.timestamp = $authorizeTimestamp
$authorizeBody = $authorizePayload | ConvertTo-Json -Depth 10

$authorizeResponse = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/authorize" `
    -ContentType "application/json" `
    -Body $authorizeBody

Assert-Equal -Name "Authorize decisionId" -Actual $authorizeResponse.decisionId -Expected $expectedDecisionId
Assert-Equal -Name "Authorize outcome" -Actual $authorizeResponse.outcome -Expected "REQUIRE_ACK"
Assert-Equal -Name "Authorize clearanceState" -Actual $authorizeResponse.clearanceState -Expected "AWAITING_ACK"
Assert-Equal -Name "Authorize evidenceId" -Actual $authorizeResponse.evidenceId -Expected $expectedEvidenceId

Write-Host "Submitting bounded acknowledgment..."
$ackPayload = Get-Content -Raw -Path $ackRisk | ConvertFrom-Json
$ackPayload.decisionId = $expectedDecisionId
$ackPayload.acknowledgment.timestamp = $ackTimestamp
$ackBody = $ackPayload | ConvertTo-Json -Depth 10

$ackResponse = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/acknowledge" `
    -ContentType "application/json" `
    -Body $ackBody

Assert-Equal -Name "Ack decisionId" -Actual $ackResponse.decisionId -Expected $expectedDecisionId
Assert-Equal -Name "Ack outcome" -Actual $ackResponse.outcome -Expected "PROCEED"
Assert-Equal -Name "Ack clearanceState" -Actual $ackResponse.clearanceState -Expected "AUTHORIZED"
Assert-Equal -Name "Ack evidenceId" -Actual $ackResponse.evidenceId -Expected $expectedEvidenceId

Write-Host "Checking compact and export audit views..."
$compactByDecision = Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/$expectedDecisionId"
$exportByDecision = Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/$expectedDecisionId/export"
$compactByRequest = Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/request/$expectedRequestId"
$exportByRequest = Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/request/$expectedRequestId/export"

Assert-Equal -Name "Compact decision outcome" -Actual $compactByDecision.outcome -Expected "PROCEED"
Assert-Equal -Name "Compact decision evidenceId" -Actual $compactByDecision.evidenceId -Expected $expectedEvidenceId
Assert-Equal -Name "Compact decision timeline count" -Actual $compactByDecision.authorizationTimeline.Count -Expected 2
Assert-Equal -Name "Export decision profile" -Actual $exportByDecision.profile -Expected $ExpectedProfileId
Assert-Equal -Name "Export decision requestId" -Actual $exportByDecision.requestId -Expected $expectedRequestId
Assert-Equal -Name "Export decision clearanceState" -Actual $exportByDecision.clearanceState -Expected "AUTHORIZED"
Assert-Equal -Name "Compact request decisionId" -Actual $compactByRequest.decisionId -Expected $expectedDecisionId
Assert-Equal -Name "Export request decisionId" -Actual $exportByRequest.decisionId -Expected $expectedDecisionId
Assert-Equal -Name "Export request requestId" -Actual $exportByRequest.requestId -Expected $expectedRequestId
Assert-Equal -Name "Export request evidenceId" -Actual $exportByRequest.evidenceId -Expected $expectedEvidenceId

Write-Host "Smoke check passed."
