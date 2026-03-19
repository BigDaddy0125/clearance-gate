------------------------------ MODULE RequestIdempotency ------------------------------
EXTENDS FiniteSets, Sequences

(******************************************************************************
* Claim:
* - replaying the same request_id is idempotent
* - the first durable decision/evidence pair wins
******************************************************************************)

CONSTANTS
  RequestIds,
  DecisionIds,
  NoDecision

ASSUME
  /\ RequestIds /= {}
  /\ DecisionIds /= {}
  /\ NoDecision \notin DecisionIds

EmptyStore == [r \in RequestIds |-> NoDecision]

Save(requestId, decisionId, store) ==
  IF store[requestId] # NoDecision
  THEN store
  ELSE [store EXCEPT ![requestId] = decisionId]

EvidenceFor(requestId, store) ==
  "evidence:" \o store[requestId]

Inv_ReplayKeepsFirstDecision ==
  \A requestId \in RequestIds:
    \A firstDecision \in DecisionIds:
      \A secondDecision \in DecisionIds:
        LET firstStore == Save(requestId, firstDecision, EmptyStore)
            secondStore == Save(requestId, secondDecision, firstStore)
        IN
          /\ secondStore[requestId] = firstDecision
          /\ EvidenceFor(requestId, secondStore) = "evidence:" \o firstDecision

Inv_ReplayDoesNotGrowStore ==
  \A requestId \in RequestIds:
    \A firstDecision \in DecisionIds:
      \A secondDecision \in DecisionIds:
        LET firstStore == Save(requestId, firstDecision, EmptyStore)
            secondStore == Save(requestId, secondDecision, firstStore)
        IN Cardinality({r \in RequestIds : secondStore[r] # NoDecision}) = 1

VARIABLE dummy

Init == dummy = 0
Next == dummy' = dummy
Spec == Init /\ [][Next]_<<dummy>>

=============================================================================
