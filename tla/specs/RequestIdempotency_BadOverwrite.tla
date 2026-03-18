------------------------------ MODULE RequestIdempotency_BadOverwrite ------------------------------
EXTENDS FiniteSets

(******************************************************************************
* Negative model: replay overwrites the first durable decision.
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

SaveBad(requestId, decisionId, store) ==
  [store EXCEPT ![requestId] = decisionId]

EvidenceFor(requestId, store) ==
  "evidence:" \o store[requestId]

Inv_ReplayKeepsFirstDecision ==
  \A requestId \in RequestIds:
    \A firstDecision \in DecisionIds:
      \A secondDecision \in DecisionIds:
        LET firstStore == SaveBad(requestId, firstDecision, EmptyStore)
            secondStore == SaveBad(requestId, secondDecision, firstStore)
        IN
          /\ secondStore[requestId] = firstDecision
          /\ EvidenceFor(requestId, secondStore) = "evidence:" \o firstDecision

Inv_ReplayDoesNotGrowStore ==
  \A requestId \in RequestIds:
    \A firstDecision \in DecisionIds:
      \A secondDecision \in DecisionIds:
        LET firstStore == SaveBad(requestId, firstDecision, EmptyStore)
            secondStore == SaveBad(requestId, secondDecision, firstStore)
        IN Cardinality({r \in RequestIds : secondStore[r] # NoDecision}) = 1

VARIABLE dummy

Init == dummy = 0
Next == dummy' = dummy
Spec == Init /\ [][Next]_<<dummy>>

=============================================================================
