------------------------------ MODULE ProfileConformance_BadImplicitAllow ------------------------------
EXTENDS FiniteSets

(******************************************************************************
* Negative model: a profile weakens the kernel by introducing a new outcome,
* allowing a fail-closed state to proceed, and dropping evidence from a
* non-blocking outcome.
******************************************************************************)

CONSTANTS
  KernelOutcomes,
  ProceedOutcome,
  NonBlockingOutcomes,
  RequiresEvidenceOutcomes,
  InfoInsufficientOutcomeBad,
  AwaitingAckOutcomeBad,
  AuthorizedOutcomeBad,
  BlockedOutcomeBad,
  DegradedOutcomeBad

ASSUME
  /\ KernelOutcomes /= {}
  /\ ProceedOutcome \in KernelOutcomes
  /\ NonBlockingOutcomes \subseteq KernelOutcomes
  /\ RequiresEvidenceOutcomes \subseteq KernelOutcomes

Inv_ProfileUsesOnlyKernelOutcomes ==
  /\ InfoInsufficientOutcomeBad \in KernelOutcomes
  /\ AwaitingAckOutcomeBad \in KernelOutcomes
  /\ AuthorizedOutcomeBad \in KernelOutcomes
  /\ BlockedOutcomeBad \in KernelOutcomes
  /\ DegradedOutcomeBad \in KernelOutcomes

Inv_ProfilePreservesFailClosed ==
  /\ InfoInsufficientOutcomeBad # ProceedOutcome
  /\ BlockedOutcomeBad # ProceedOutcome
  /\ DegradedOutcomeBad # ProceedOutcome

Inv_ProfileRequiresEvidenceForNonBlocking ==
  NonBlockingOutcomes \subseteq RequiresEvidenceOutcomes

VARIABLE dummy

Init == dummy = 0
Next == dummy' = dummy
Spec == Init /\ [][Next]_<<dummy>>

=============================================================================
