[CmdletBinding()]
param(
    [string]$TrxPath = "",
    [string]$OutputPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$resolvedTrxPath =
    if ([string]::IsNullOrWhiteSpace($TrxPath)) {
        Join-Path $repoRoot "artifacts\\test-results\\runtime-claims.trx"
    }
    else {
        $TrxPath
    }
$resolvedOutputPath =
    if ([string]::IsNullOrWhiteSpace($OutputPath)) {
        Join-Path $repoRoot "artifacts\\test-results\\summary.md"
    }
    else {
        $OutputPath
    }

if (-not (Test-Path $resolvedTrxPath)) {
    $candidate = Get-ChildItem (Join-Path $repoRoot "artifacts\\test-results") -Recurse -Filter *.trx |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -ne $candidate) {
        $resolvedTrxPath = $candidate.FullName
    }
}

[xml]$trx = Get-Content $resolvedTrxPath
$namespaceManager = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
$namespaceManager.AddNamespace("trx", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")

$results = @(
    $trx.SelectNodes("//trx:UnitTestResult", $namespaceManager) |
    ForEach-Object {
        [pscustomobject]@{
            TestName = $_.testName
            Outcome = $_.outcome
            Duration = $_.duration
        }
    }
)

$summaryLines = @(
    "# Runtime Claim Test Results",
    "",
    "| Test | Outcome | Duration |",
    "| --- | --- | --- |"
)
$summaryLines += $results | ForEach-Object {
    "| $($_.TestName) | $($_.Outcome) | $($_.Duration) |"
}

$outputDirectory = Split-Path $resolvedOutputPath -Parent
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
Set-Content -Path $resolvedOutputPath -Value $summaryLines

Write-Host ("Summary: " + $resolvedOutputPath)
