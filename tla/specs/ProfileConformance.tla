------------------------------ MODULE ProfileConformance ------------------------------
EXTENDS FiniteSets

(******************************************************************************
* Claim:
* - profiles may define roles, constraints, and metadata
* - profiles may not weaken kernel outcomes, fail-closed semantics,
*   or evidence requirements for non-blocking outcomes
******************************************************************************)

CONSTANTS
  KernelOutcomes,
  ProceedOutcome,
  NonBlockingOutcomes,
  RequiresEvidenceOutcomes,
  RequiredRoles,
  ProfileRoles,
  AllowedConstraintKinds,
  ProfileConstraintKinds,
  DeclaredOutcomes,
  InfoInsufficientOutcome,
  AwaitingAckOutcome,
  AuthorizedOutcome,
  BlockedOutcome,
  DegradedOutcome

ASSUME
  /\ KernelOutcomes /= {}
  /\ ProceedOutcome \in KernelOutcomes
  /\ NonBlockingOutcomes \subseteq KernelOutcomes
  /\ RequiresEvidenceOutcomes \subseteq KernelOutcomes
  /\ RequiredRoles /= {}
  /\ AllowedConstraintKinds /= {}

Inv_ProfileUsesOnlyKernelOutcomes ==
  /\ InfoInsufficientOutcome \in KernelOutcomes
  /\ AwaitingAckOutcome \in KernelOutcomes
  /\ AuthorizedOutcome \in KernelOutcomes
  /\ BlockedOutcome \in KernelOutcomes
  /\ DegradedOutcome \in KernelOutcomes

Inv_ProfilePreservesFailClosed ==
  /\ InfoInsufficientOutcome # ProceedOutcome
  /\ BlockedOutcome # ProceedOutcome
  /\ DegradedOutcome # ProceedOutcome

Inv_ProfileRequiresEvidenceForNonBlocking ==
  NonBlockingOutcomes \subseteq RequiresEvidenceOutcomes

Inv_ProfileRolesCoverKernelResponsibilities ==
  RequiredRoles \subseteq ProfileRoles

Inv_ProfileConstraintKindsAllowed ==
  ProfileConstraintKinds \subseteq AllowedConstraintKinds

Inv_ProfileDeclaresOnlyKernelOutcomes ==
  DeclaredOutcomes \subseteq KernelOutcomes

VARIABLE dummy

Init == dummy = 0
Next == dummy' = dummy
Spec == Init /\ [][Next]_<<dummy>>

=============================================================================
