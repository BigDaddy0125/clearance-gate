------------------------------ MODULE AcknowledgmentBounded_BadUniversalAck ------------------------------
EXTENDS FiniteSets

(******************************************************************************
* Negative model: deliberately incorrect universal-override acknowledgment.
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

AckResultBad(s, constraints) ==
  IF s # AuthorizedState
  THEN AuthorizedState
  ELSE s

Inv_AckOnlyAuthorizesAwaitingAck ==
  \A s \in DecisionStates:
    \A constraints \in SUBSET ConstraintUniverse:
      ((s # AuthorizedState)
        /\ AckResultBad(s, constraints) = AuthorizedState) =>
        /\ s = AwaitingAckState
        /\ AckRequiredConstraint \in constraints

Inv_ForbiddenStatesRemainBlocked ==
  \A s \in ForbiddenAckStates:
    \A constraints \in SUBSET ConstraintUniverse:
      AckResultBad(s, constraints) = s

VARIABLE dummy

Init == dummy = 0
Next == dummy' = dummy
Spec == Init /\ [][Next]_<<dummy>>

=============================================================================
