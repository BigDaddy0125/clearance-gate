[CmdletBinding()]
param(
    [string]$ProfilePath = "",
    [string]$OutputPath = ""
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

$profile = Get-Content $resolvedProfilePath | ConvertFrom-Json

$profileRoles = @($profile.responsibilityRoles)
$constraintKinds = @($profile.constraints | ForEach-Object { $_.kind })

$requiredRoles = @(
    "decision_owner",
    "acknowledging_authority",
    "audit_reviewer"
)
$allowedConstraintKinds = @(
    "ack_required",
    "required_field"
)

$outputDirectory = Split-Path $resolvedOutputPath -Parent
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null

$cfg = @"
SPECIFICATION Spec
INVARIANT Inv_ProfileUsesOnlyKernelOutcomes
INVARIANT Inv_ProfilePreservesFailClosed
INVARIANT Inv_ProfileRequiresEvidenceForNonBlocking
INVARIANT Inv_ProfileRolesCoverKernelResponsibilities
INVARIANT Inv_ProfileConstraintKindsAllowed
INVARIANT Inv_ProfileDeclaresOnlyKernelOutcomes

CONSTANTS
  KernelOutcomes = {"PROCEED", "BLOCK", "REQUIRE_ACK", "DEGRADE"}
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

Set-Content -Path $resolvedOutputPath -Value $cfg
Write-Host ("Generated " + $resolvedOutputPath)
