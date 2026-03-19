[CmdletBinding()]
param(
    [string]$TraceabilityPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$resolvedTraceabilityPath =
    if ([string]::IsNullOrWhiteSpace($TraceabilityPath)) {
        Join-Path $repoRoot "docs\\claim-traceability.md"
    }
    else {
        $TraceabilityPath
    }

$content = Get-Content $resolvedTraceabilityPath -Raw
$requiredClaims = "CG1", "CG2", "CG3", "CG4", "CG5", "CG6"

foreach ($claim in $requiredClaims) {
    if ($content -notmatch [regex]::Escape("| $claim | COMPLETE |")) {
        throw "Claim '$claim' is not marked COMPLETE in the traceability checklist."
    }
}

Write-Host "Traceability checklist is complete for CG1-CG6."
