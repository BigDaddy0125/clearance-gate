[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$ApiKey = "clearancegate-local-dev-key"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$authorizeRisk = Join-Path $repoRoot "examples\\v0\\authorize-risk.json"
$ackRisk = Join-Path $repoRoot "examples\\v0\\acknowledge-risk.json"
$authHeaders = @{ Authorization = "Bearer $ApiKey" }

Write-Host "Checking profile diagnostics..."
Invoke-RestMethod -Method Get -Uri "$BaseUrl/profiles" -Headers $authHeaders | ConvertTo-Json -Depth 10
Invoke-RestMethod -Method Get -Uri "$BaseUrl/profiles/latest/itops_deployment" -Headers $authHeaders | ConvertTo-Json -Depth 10

Write-Host "Authorizing risk example..."
Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/authorize" `
    -Headers $authHeaders `
    -ContentType "application/json" `
    -InFile $authorizeRisk | ConvertTo-Json -Depth 10

Write-Host "Submitting bounded acknowledgment..."
Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/acknowledge" `
    -Headers $authHeaders `
    -ContentType "application/json" `
    -InFile $ackRisk | ConvertTo-Json -Depth 10

Write-Host "Reading compact and export audit views..."
Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/dec-example-risk-1" -Headers $authHeaders | ConvertTo-Json -Depth 10
Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/dec-example-risk-1/export" -Headers $authHeaders | ConvertTo-Json -Depth 10
Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/request/req-example-risk-1" -Headers $authHeaders | ConvertTo-Json -Depth 10
Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/request/req-example-risk-1/export" -Headers $authHeaders | ConvertTo-Json -Depth 10
