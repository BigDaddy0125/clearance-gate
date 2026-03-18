------------------------------ MODULE AcknowledgmentBounded ------------------------------
EXTENDS FiniteSets

(******************************************************************************
* Claim:
* - acknowledgment is bounded
* - only an awaiting-ack decision with an explicit ack-required constraint
*   may transition to AUTHORIZED
******************************************************************************)

CONSTANTS
  DecisionStates,
  AwaitingAckState,
  AuthorizedState,
  ForbiddenAckStates,
  ConstraintUniverse,
  AckRequiredConstraint

ASSUME
  /\ DecisionStates /= {}
  /\ AwaitingAckState \in DecisionStates
  /\ AuthorizedState \in DecisionStates
  /\ ForbiddenAckStates \subseteq DecisionStates
  /\ AwaitingAckState \notin ForbiddenAckStates
  /\ AuthorizedState \notin ForbiddenAckStates
  /\ AckRequiredConstraint \in ConstraintUniverse

CanAcknowledge(s, constraints) ==
  /\ s = AwaitingAckState
  /\ AckRequiredConstraint \in constraints

AckResult(s, constraints) ==
  IF CanAcknowledge(s, constraints)
  THEN AuthorizedState
  ELSE s

Inv_AckOnlyAuthorizesAwaitingAck ==
  \A s \in DecisionStates:
    \A constraints \in SUBSET ConstraintUniverse:
      AckResult(s, constraints) = AuthorizedState =>
        /\ s = AwaitingAckState
        /\ AckRequiredConstraint \in constraints

Inv_ForbiddenStatesRemainBlocked ==
  \A s \in ForbiddenAckStates:
    \A constraints \in SUBSET ConstraintUniverse:
      AckResult(s, constraints) = s

VARIABLE dummy

Init == dummy = 0
Next == dummy' = dummy
Spec == Init /\ [][Next]_<<dummy>>

=============================================================================
