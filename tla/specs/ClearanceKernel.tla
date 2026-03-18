------------------------------ MODULE ClearanceKernel ------------------------------
EXTENDS Naturals, FiniteSets

(******************************************************************************
* ClearanceGate authorization kernel model.
*
* Purpose:
* - model the core state space
* - verify outcome exclusivity
* - verify fail-closed behavior
******************************************************************************)

CONSTANTS
  DecisionStates,
  AuthorizedState,
  BlockedStates,
  AwaitingAckState,
  DegradedState,
  InfoInsufficientState

ASSUME
  /\ DecisionStates /= {}
  /\ AuthorizedState \in DecisionStates
  /\ AwaitingAckState \in DecisionStates
  /\ DegradedState \in DecisionStates
  /\ InfoInsufficientState \in DecisionStates
  /\ BlockedStates \subseteq DecisionStates

OutcomeFor(s) ==
  IF s = AuthorizedState THEN "PROCEED"
  ELSE IF s = AwaitingAckState THEN "REQUIRE_ACK"
  ELSE IF s = DegradedState THEN "DEGRADE"
  ELSE "BLOCK"

Inv_OutcomeTotality ==
  \A s \in DecisionStates:
    OutcomeFor(s) \in {"PROCEED", "BLOCK", "REQUIRE_ACK", "DEGRADE"}

Inv_AuthorizedOnlyFromAuthorizedState ==
  \A s \in DecisionStates:
    OutcomeFor(s) = "PROCEED" => s = AuthorizedState

Inv_DegradedNeverProceeds ==
  OutcomeFor(DegradedState) # "PROCEED"

Inv_InfoInsufficientNeverProceeds ==
  OutcomeFor(InfoInsufficientState) # "PROCEED"

VARIABLE dummy

Init == dummy = 0
Next == dummy' = dummy
Spec == Init /\ [][Next]_<<dummy>>

=============================================================================
