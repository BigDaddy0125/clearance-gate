------------------------------ MODULE DurableEvidenceGate_BadEarlyEmit ------------------------------
EXTENDS FiniteSets

(******************************************************************************
* Negative model: deliberately incorrect emission of non-blocking outcomes
* before evidence becomes durable.
******************************************************************************)

CONSTANTS
  OutcomeUniverse,
  NonBlockingOutcomes,
  BlockingOutcomes,
  NoOutcome

ASSUME
  /\ OutcomeUniverse /= {}
  /\ NonBlockingOutcomes \subseteq OutcomeUniverse
  /\ BlockingOutcomes \subseteq OutcomeUniverse
  /\ NonBlockingOutcomes \cap BlockingOutcomes = {}
  /\ NonBlockingOutcomes \cup BlockingOutcomes = OutcomeUniverse
  /\ NoOutcome \notin OutcomeUniverse

VARIABLES evidenceDurable, emittedOutcome

Init ==
  /\ evidenceDurable = FALSE
  /\ emittedOutcome = NoOutcome

PersistEvidence ==
  /\ emittedOutcome = NoOutcome
  /\ evidenceDurable = FALSE
  /\ evidenceDurable' = TRUE
  /\ emittedOutcome' = emittedOutcome

EmitBlocking ==
  /\ emittedOutcome = NoOutcome
  /\ \E outcome \in BlockingOutcomes:
      emittedOutcome' = outcome
  /\ evidenceDurable' = evidenceDurable

EmitNonBlockingBad ==
  /\ emittedOutcome = NoOutcome
  /\ \E outcome \in NonBlockingOutcomes:
      emittedOutcome' = outcome
  /\ evidenceDurable' = evidenceDurable

Stutter ==
  /\ evidenceDurable' = evidenceDurable
  /\ emittedOutcome' = emittedOutcome

Next ==
  PersistEvidence
  \/ EmitBlocking
  \/ EmitNonBlockingBad
  \/ Stutter

Spec ==
  Init /\ [][Next]_<<evidenceDurable, emittedOutcome>>

Inv_EmittedOutcomeIsKnown ==
  emittedOutcome = NoOutcome \/ emittedOutcome \in OutcomeUniverse

Inv_NonBlockingOutcomesRequireDurableEvidence ==
  emittedOutcome \in NonBlockingOutcomes => evidenceDurable

=============================================================================
