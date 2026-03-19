[CmdletBinding()]
param(
    [string]$ProfilePath = "",
    [string]$OutputPath = "",
    [string]$RoleOutputPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$resolvedProfilePath =
    if ([string]::IsNullOrWhiteSpace($ProfilePath)) {
        Join-Path $repoRoot "src\\ClearanceGate.Profiles\\itops_deployment_v1.json"
    }
    else {
        $ProfilePath
    }
$resolvedOutputPath =
    if ([string]::IsNullOrWhiteSpace($OutputPath)) {
        Join-Path $repoRoot "generated\\tla\\itops_deployment_v1_profile_conformance.cfg"
    }
    else {
        $OutputPath
    }
$resolvedRoleOutputPath =
    if ([string]::IsNullOrWhiteSpace($RoleOutputPath)) {
        Join-Path $repoRoot "generated\\tla\\itops_deployment_v1_profile_role_conformance.cfg"
    }
    else {
        $RoleOutputPath
    }

function ConvertTo-TlaSetLiteral {
    param([object[]]$Values)

    if ($null -eq $Values -or $Values.Count -eq 0) {
        return "{}"
    }

    $quoted = $Values |
        Sort-Object -Unique |
        ForEach-Object { '"' + $_ + '"' }

    return "{ " + ($quoted -join ", ") + " }"
}

function Get-StringConstantsFromSourceFile {
    param(
        [string]$Path,
        [string]$Pattern
    )

    $matches = Select-String -Path $Path -Pattern $Pattern -AllMatches
    return @(
        $matches.Matches |
        ForEach-Object { $_.Groups[1].Value } |
        Sort-Object -Unique
    )
}

function Get-NamedStringConstantFromSourceFile {
    param(
        [string]$Path,
        [string]$ConstantName
    )

    $match = Select-String -Path $Path -Pattern ('const string ' + [regex]::Escape($ConstantName) + ' = "([^"]+)"') |
        Select-Object -First 1

    if ($null -eq $match) {
        throw "Constant '$ConstantName' was not found in '$Path'."
    }

    return $match.Matches[0].Groups[1].Value
}

$profile = Get-Content $resolvedProfilePath | ConvertFrom-Json
$kernelOutcomeNamesPath = Join-Path $repoRoot "src\\ClearanceGate.Kernel\\KernelOutcomeNames.cs"
$rolesSourcePath = Join-Path $repoRoot "src\\ClearanceGate.Profiles\\KernelResponsibilityRoles.cs"
$constraintKindsSourcePath = Join-Path $repoRoot "src\\ClearanceGate.Profiles\\ProfileConstraintKinds.cs"

$profileRoles = @($profile.responsibilityRoles)
$constraintKinds = @($profile.constraints | ForEach-Object { $_.kind })
$requiredRoles = Get-StringConstantsFromSourceFile -Path $rolesSourcePath -Pattern 'const string [A-Za-z]+ = "([^"]+)"'
$kernelOutcomes = Get-StringConstantsFromSourceFile -Path $kernelOutcomeNamesPath -Pattern 'const string [A-Za-z]+ = "([^"]+)"'
$allowedConstraintKinds = Get-StringConstantsFromSourceFile -Path $constraintKindsSourcePath -Pattern 'const string [A-Za-z]+ = "([^"]+)"'

$requiredAuthorizationRole = Get-NamedStringConstantFromSourceFile -Path $rolesSourcePath -ConstantName "DecisionOwner"
$requiredAcknowledgmentRole = Get-NamedStringConstantFromSourceFile -Path $rolesSourcePath -ConstantName "AcknowledgingAuthority"

$outputDirectory = Split-Path $resolvedOutputPath -Parent
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$roleOutputDirectory = Split-Path $resolvedRoleOutputPath -Parent
[System.IO.Directory]::CreateDirectory($roleOutputDirectory) | Out-Null

$cfg = @"
SPECIFICATION Spec
INVARIANT Inv_ProfileUsesOnlyKernelOutcomes
INVARIANT Inv_ProfilePreservesFailClosed
INVARIANT Inv_ProfileRequiresEvidenceForNonBlocking
INVARIANT Inv_ProfileRolesCoverKernelResponsibilities
INVARIANT Inv_ProfileConstraintKindsAllowed
INVARIANT Inv_ProfileDeclaresOnlyKernelOutcomes

CONSTANTS
  KernelOutcomes = $(ConvertTo-TlaSetLiteral $kernelOutcomes)
  ProceedOutcome = "PROCEED"
  NonBlockingOutcomes = {"PROCEED", "REQUIRE_ACK"}
  RequiresEvidenceOutcomes = {"PROCEED", "REQUIRE_ACK"}
  RequiredRoles = $(ConvertTo-TlaSetLiteral $requiredRoles)
  ProfileRoles = $(ConvertTo-TlaSetLiteral $profileRoles)
  AllowedConstraintKinds = $(ConvertTo-TlaSetLiteral $allowedConstraintKinds)
  ProfileConstraintKinds = $(ConvertTo-TlaSetLiteral $constraintKinds)
  DeclaredOutcomes = {}
  InfoInsufficientOutcome = "BLOCK"
  AwaitingAckOutcome = "REQUIRE_ACK"
  AuthorizedOutcome = "PROCEED"
  BlockedOutcome = "BLOCK"
  DegradedOutcome = "DEGRADE"
"@

$roleCfg = @"
SPECIFICATION Spec
INVARIANT Inv_ProfileDeclaresAuthorizationRole
INVARIANT Inv_ProfileDeclaresAcknowledgmentRole
INVARIANT Inv_AuthorizationRoleMatchesBoundary
INVARIANT Inv_AcknowledgmentRoleMatchesBoundary

CONSTANTS
  RequiredAuthorizationRole = "$requiredAuthorizationRole"
  RequiredAcknowledgmentRole = "$requiredAcknowledgmentRole"
  ProfileRoles = $(ConvertTo-TlaSetLiteral $profileRoles)
  AuthorizationRequestRole = "$requiredAuthorizationRole"
  AcknowledgmentRequestRole = "$requiredAcknowledgmentRole"
"@

Set-Content -Path $resolvedOutputPath -Value $cfg
Set-Content -Path $resolvedRoleOutputPath -Value $roleCfg
Write-Host ("Generated " + $resolvedOutputPath)
Write-Host ("Generated " + $resolvedRoleOutputPath)
