------------------------------ MODULE ClearanceKernel_BadFailOpen ------------------------------
EXTENDS Naturals, FiniteSets

(******************************************************************************
* Negative model: deliberately incorrect fail-open behavior.
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

OutcomeForBad(s) ==
  IF s = AuthorizedState THEN "PROCEED"
  ELSE IF s = AwaitingAckState THEN "REQUIRE_ACK"
  ELSE IF s = DegradedState THEN "PROCEED"
  ELSE "BLOCK"

Inv_AuthorizedOnlyFromAuthorizedState ==
  \A s \in DecisionStates:
    OutcomeForBad(s) = "PROCEED" => s = AuthorizedState

Inv_DegradedNeverProceeds ==
  OutcomeForBad(DegradedState) # "PROCEED"

Inv_InfoInsufficientNeverProceeds ==
  OutcomeForBad(InfoInsufficientState) # "PROCEED"

VARIABLE dummy

Init == dummy = 0
Next == dummy' = dummy
Spec == Init /\ [][Next]_<<dummy>>

=============================================================================
