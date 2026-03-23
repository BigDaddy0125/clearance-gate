[CmdletBinding()]
param(
    [switch]$IncludeRed,
    [string]$Java = "java",
    [string]$TlaToolsJar = "",
    [string]$OutputRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$resolvedTlaToolsJarInput =
    if ([string]::IsNullOrWhiteSpace($TlaToolsJar)) {
        Join-Path $repoRoot "tools\\tla2tools.jar"
    }
    else {
        $TlaToolsJar
    }
$resolvedOutputRootInput =
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        Join-Path $repoRoot "artifacts\\tlc"
    }
    else {
        $OutputRoot
    }
$resolvedJar = (Resolve-Path $resolvedTlaToolsJarInput).Path
$resolvedOutputRoot = [System.IO.Path]::GetFullPath($resolvedOutputRootInput)
[System.IO.Directory]::CreateDirectory($resolvedOutputRoot) | Out-Null
$runId = [Guid]::NewGuid().ToString("N")

& (Join-Path $PSScriptRoot "generate-all-profile-tla-configs.ps1")

$models = @(
    @{
        Name = "kernel_ok"
        Spec = ".\\tla\\specs\\ClearanceKernel.tla"
        Config = ".\\tla\\models\\kernel_ok.cfg"
        ExpectSuccess = $true
    },
    @{
        Name = "ack_bounded_ok"
        Spec = ".\\tla\\specs\\AcknowledgmentBounded.tla"
        Config = ".\\tla\\models\\ack_bounded_ok.cfg"
        ExpectSuccess = $true
    },
    @{
        Name = "idempotency_ok"
        Spec = ".\\tla\\specs\\RequestIdempotency.tla"
        Config = ".\\tla\\models\\idempotency_ok.cfg"
        ExpectSuccess = $true
    },
    @{
        Name = "durable_evidence_ok"
        Spec = ".\\tla\\specs\\DurableEvidenceGate.tla"
        Config = ".\\tla\\models\\durable_evidence_ok.cfg"
        ExpectSuccess = $true
    }
)

$generatedProfileConfigs = Get-ChildItem -Path (Join-Path $repoRoot "generated\\tla") -Filter "*_profile_conformance.cfg" -File |
    Sort-Object Name
$generatedRoleConfigs = Get-ChildItem -Path (Join-Path $repoRoot "generated\\tla") -Filter "*_profile_role_conformance.cfg" -File |
    Sort-Object Name

foreach ($config in $generatedProfileConfigs) {
    $models += @{
        Name = [System.IO.Path]::GetFileNameWithoutExtension($config.Name)
        Spec = ".\\tla\\specs\\ProfileConformance.tla"
        Config = (".\\generated\\tla\\" + $config.Name)
        ExpectSuccess = $true
    }
}

foreach ($config in $generatedRoleConfigs) {
    $models += @{
        Name = [System.IO.Path]::GetFileNameWithoutExtension($config.Name)
        Spec = ".\\tla\\specs\\ProfileRoleConformance.tla"
        Config = (".\\generated\\tla\\" + $config.Name)
        ExpectSuccess = $true
    }
}

if ($IncludeRed) {
    $models += @(
        @{
            Name = "kernel_negative_fail_open"
            Spec = ".\\tla\\specs\\ClearanceKernel_BadFailOpen.tla"
            Config = ".\\tla\\models\\kernel_negative_fail_open.cfg"
            ExpectSuccess = $false
            ExpectedViolation = "Inv_AuthorizedOnlyFromAuthorizedState"
        },
        @{
            Name = "ack_bounded_negative_universal_ack"
            Spec = ".\\tla\\specs\\AcknowledgmentBounded_BadUniversalAck.tla"
            Config = ".\\tla\\models\\ack_bounded_negative_universal_ack.cfg"
            ExpectSuccess = $false
            ExpectedViolation = "Inv_AckOnlyAuthorizesAwaitingAck"
        },
        @{
            Name = "idempotency_negative_overwrite"
            Spec = ".\\tla\\specs\\RequestIdempotency_BadOverwrite.tla"
            Config = ".\\tla\\models\\idempotency_negative_overwrite.cfg"
            ExpectSuccess = $false
            ExpectedViolation = "Inv_ReplayKeepsFirstDecision"
        },
        @{
            Name = "durable_evidence_negative_early_emit"
            Spec = ".\\tla\\specs\\DurableEvidenceGate_BadEarlyEmit.tla"
            Config = ".\\tla\\models\\durable_evidence_negative_early_emit.cfg"
            ExpectSuccess = $false
            ExpectedViolation = "Inv_NonBlockingOutcomesRequireDurableEvidence"
        },
        @{
            Name = "profile_conformance_negative_implicit_allow"
            Spec = ".\\tla\\specs\\ProfileConformance_BadImplicitAllow.tla"
            Config = ".\\tla\\models\\profile_conformance_negative_implicit_allow.cfg"
            ExpectSuccess = $false
            ExpectedViolation = "Inv_ProfileUsesOnlyKernelOutcomes"
        },
        @{
            Name = "profile_role_conformance_negative_role_bypass"
            Spec = ".\\tla\\specs\\ProfileRoleConformance_BadRoleBypass.tla"
            Config = ".\\tla\\models\\profile_role_conformance_negative_role_bypass.cfg"
            ExpectSuccess = $false
            ExpectedViolation = "Inv_ProfileDeclaresAcknowledgmentRole"
        }
    )
}

$failures = New-Object System.Collections.Generic.List[string]
$results = New-Object System.Collections.Generic.List[object]

foreach ($model in $models) {
    $specPath = $model.Spec
    $configPath = $model.Config
    $metaDir = Join-Path $resolvedOutputRoot ("meta-" + $runId + "-" + $model.Name)
    $logPath = Join-Path $resolvedOutputRoot ($model.Name + ".log")
    $stdoutPath = Join-Path $resolvedOutputRoot ($model.Name + "." + $runId + ".stdout.tmp")
    $stderrPath = Join-Path $resolvedOutputRoot ($model.Name + "." + $runId + ".stderr.tmp")

    if (Test-Path $metaDir) {
        Remove-Item -Recurse -Force $metaDir -ErrorAction SilentlyContinue
    }
    if (Test-Path $stdoutPath) {
        Remove-Item -Force $stdoutPath
    }
    if (Test-Path $stderrPath) {
        Remove-Item -Force $stderrPath
    }

    $arguments = @(
        "-cp", $resolvedJar,
        "tlc2.TLC",
        "-cleanup",
        "-deadlock",
        "-metadir", $metaDir,
        "-config", $configPath,
        $specPath
    )

    Push-Location $repoRoot
    try {
        $process = Start-Process -FilePath $Java `
            -ArgumentList $arguments `
            -WorkingDirectory $repoRoot `
            -NoNewWindow `
            -Wait `
            -PassThru `
            -RedirectStandardOutput $stdoutPath `
            -RedirectStandardError $stderrPath
    }
    finally {
        Pop-Location
    }

    $stdout = if (Test-Path $stdoutPath) { Get-Content $stdoutPath } else { @() }
    $stderr = if (Test-Path $stderrPath) { Get-Content $stderrPath } else { @() }
    $output = @($stdout) + @($stderr)
    $exitCode = $process.ExitCode
    $output | Set-Content -Path $logPath

    Remove-Item -Force $stdoutPath, $stderrPath -ErrorAction SilentlyContinue

    if ($model.ExpectSuccess) {
        if ($exitCode -eq 0) {
            Write-Host ("PASS " + $model.Name)
            $results.Add([pscustomobject]@{ Name = $model.Name; Status = "PASS"; Log = $logPath }) | Out-Null
        }
        else {
            $failures.Add("Green model '$($model.Name)' failed unexpectedly. See $logPath")
            Write-Host ("FAIL " + $model.Name)
            $results.Add([pscustomobject]@{ Name = $model.Name; Status = "FAIL"; Log = $logPath }) | Out-Null
        }

        continue
    }

    $expectedViolation = $model.ExpectedViolation
    $sawInvariantViolation =
        ($exitCode -ne 0) -and
        (($output -join [Environment]::NewLine) -match [regex]::Escape($expectedViolation))

    if ($sawInvariantViolation) {
        Write-Host ("EXPECTED-FAIL " + $model.Name)
        $results.Add([pscustomobject]@{ Name = $model.Name; Status = "EXPECTED-FAIL"; Log = $logPath }) | Out-Null
    }
    else {
        $failures.Add("Red model '$($model.Name)' did not fail with expected invariant '$expectedViolation'. See $logPath")
        Write-Host ("FAIL " + $model.Name)
        $results.Add([pscustomobject]@{ Name = $model.Name; Status = "FAIL"; Log = $logPath }) | Out-Null
    }
}

Write-Host ""
Write-Host ("Logs: " + $resolvedOutputRoot)

$summaryPath = Join-Path $resolvedOutputRoot "summary.md"
$summaryLines = @(
    "# TLC Results",
    "",
    "| Model | Status | Log |",
    "| --- | --- | --- |"
)
$summaryLines += $results | ForEach-Object {
    "| $($_.Name) | $($_.Status) | $($_.Log) |"
}
Set-Content -Path $summaryPath -Value $summaryLines
Write-Host ("Summary: " + $summaryPath)

if ($failures.Count -gt 0) {
    Write-Host ""
    foreach ($failure in $failures) {
        Write-Host $failure
    }

    exit 1
}
