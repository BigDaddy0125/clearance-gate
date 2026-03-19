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

function Resolve-TraceabilityFileToken {
    param(
        [string]$RepoRoot,
        [string]$Token
    )

    $normalizedToken = $Token -replace '/', '\'
    $candidates = New-Object System.Collections.Generic.List[string]

    if ($normalizedToken -match '\\') {
        $candidates.Add((Join-Path $RepoRoot $normalizedToken)) | Out-Null
    }
    else {
        switch -Regex ($normalizedToken) {
            '\.tla$' { $candidates.Add((Join-Path $RepoRoot ("tla\\specs\\" + $normalizedToken))) | Out-Null; break }
            '\.cfg$' {
                $candidates.Add((Join-Path $RepoRoot ("tla\\models\\" + $normalizedToken))) | Out-Null
                $candidates.Add((Join-Path $RepoRoot ("generated\\tla\\" + $normalizedToken))) | Out-Null
                break
            }
            '\.cs$' {
                $candidates.Add((Join-Path $RepoRoot ("src\\" + $normalizedToken))) | Out-Null
                $candidates.Add((Join-Path $RepoRoot ("tests\\ClearanceGate.Api.Tests\\" + $normalizedToken))) | Out-Null
                break
            }
            '\.ps1$' { $candidates.Add((Join-Path $RepoRoot ("scripts\\" + $normalizedToken))) | Out-Null; break }
            '\.yml$' { $candidates.Add((Join-Path $RepoRoot (".github\\workflows\\" + $normalizedToken))) | Out-Null; break }
            default { $candidates.Add((Join-Path $RepoRoot $normalizedToken)) | Out-Null; break }
        }
    }

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    $searchResult = Get-ChildItem $RepoRoot -Recurse -File -Filter $normalizedToken |
        Select-Object -ExpandProperty FullName

    if (@($searchResult).Count -eq 1) {
        return $searchResult
    }

    return $null
}

$content = Get-Content $resolvedTraceabilityPath -Raw
$requiredClaims = "CG1", "CG2", "CG3", "CG4", "CG5", "CG6"

foreach ($claim in $requiredClaims) {
    if ($content -notmatch [regex]::Escape("| $claim | COMPLETE |")) {
        throw "Claim '$claim' is not marked COMPLETE in the traceability checklist."
    }
}

$tableLines = Get-Content $resolvedTraceabilityPath |
    Where-Object { $_ -match '^\| CG[0-9]+ \|' }

foreach ($line in $tableLines) {
    $columns = $line.Trim('|').Split('|') | ForEach-Object { $_.Trim() }
    $claim = $columns[0]
    $formalColumn = $columns[2]
    $runtimeColumn = $columns[3]
    $testsColumn = $columns[4]
    $ciColumn = $columns[5]

    $fileTokens = [regex]::Matches(
        ($formalColumn + ", " + $runtimeColumn + ", " + $ciColumn),
        '[A-Za-z0-9_\./-]+\.(?:tla|cfg|cs|ps1|yml)'
    ) | ForEach-Object { $_.Value }

    foreach ($token in $fileTokens | Sort-Object -Unique) {
        $resolvedPath = Resolve-TraceabilityFileToken -RepoRoot $repoRoot -Token $token
        if ($null -eq $resolvedPath) {
            throw "Claim '$claim' references missing file '$token' in the traceability checklist."
        }
    }

    $testTokens = [regex]::Matches($testsColumn, '([A-Za-z0-9_]+)\.([A-Za-z0-9_]+)') |
        ForEach-Object {
            [pscustomobject]@{
                ClassName = $_.Groups[1].Value
                MethodName = $_.Groups[2].Value
            }
        }

    foreach ($testToken in $testTokens) {
        $testFilePath = Join-Path $repoRoot ("tests\\ClearanceGate.Api.Tests\\" + $testToken.ClassName + ".cs")
        if (-not (Test-Path $testFilePath)) {
            throw "Claim '$claim' references missing test file '$($testToken.ClassName).cs'."
        }

        $testContent = Get-Content $testFilePath -Raw
        if ($testContent -notmatch ('\b' + [regex]::Escape($testToken.MethodName) + '\b')) {
            throw "Claim '$claim' references missing test method '$($testToken.MethodName)' in '$($testToken.ClassName).cs'."
        }
    }
}

Write-Host "Traceability checklist is complete for CG1-CG6 and repository anchors resolve."
