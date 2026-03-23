[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:5000"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$authorizeRisk = Join-Path $repoRoot "examples\\v0\\authorize-risk.json"
$ackRisk = Join-Path $repoRoot "examples\\v0\\acknowledge-risk.json"

Write-Host "Checking profile diagnostics..."
Invoke-RestMethod -Method Get -Uri "$BaseUrl/profiles" | ConvertTo-Json -Depth 10
Invoke-RestMethod -Method Get -Uri "$BaseUrl/profiles/latest/itops_deployment" | ConvertTo-Json -Depth 10

Write-Host "Authorizing risk example..."
Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/authorize" `
    -ContentType "application/json" `
    -InFile $authorizeRisk | ConvertTo-Json -Depth 10

Write-Host "Submitting bounded acknowledgment..."
Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/acknowledge" `
    -ContentType "application/json" `
    -InFile $ackRisk | ConvertTo-Json -Depth 10

Write-Host "Reading compact and export audit views..."
Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/dec-example-risk-1" | ConvertTo-Json -Depth 10
Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/dec-example-risk-1/export" | ConvertTo-Json -Depth 10
Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/request/req-example-risk-1" | ConvertTo-Json -Depth 10
Invoke-RestMethod -Method Get -Uri "$BaseUrl/audit/request/req-example-risk-1/export" | ConvertTo-Json -Depth 10
