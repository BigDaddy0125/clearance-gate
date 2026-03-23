# Post-Pilot Decision Memo

Use this memo after the first real pilot finishes.

Its purpose is to make the next decision explicit instead of letting scope drift happen through ad hoc requests.

## Snapshot

- Pilot name:
- Pilot window:
- Commit or release bundle:
- Primary profile used:
- Calling system:

## What Happened

Summarize the real usage shape:

- which actions were gated
- how often `PROCEED`, `BLOCK`, `REQUIRE_ACK`, and `DEGRADE` appeared
- whether acknowledgment remained bounded and understandable
- whether audit/export evidence was sufficient for review

## What Worked

Record the parts that supported the pilot without widening scope.

Examples:

- explicit profile use remained workable
- request idempotency behaved as expected
- audit replay by request id was useful
- diagnostics were sufficient for operators

## What Hurt

Record the friction points without immediately treating them as approved features.

Examples:

- deployment handoff friction
- too much manual request mapping
- insufficient log visibility
- profile lifecycle confusion

## Boundary Check

Answer each explicitly:

1. Did ClearanceGate remain an authorization boundary rather than becoming a workflow coordinator?
2. Did any pilot request require hidden heuristics or implicit profile selection?
3. Did any pilot request require new decision outcomes?
4. Did any pilot request push toward UI-first productization instead of API-first boundary use?

## Decision

Choose one:

- continue with the current boundary shape
- continue with tighter operational hardening
- continue with one narrow `v1` expansion
- pause and reduce scope
- redefine the product scope explicitly

## Approved Next Work

List only work that is explicitly approved from the current `v1` backlog.

- approved item 1:
- approved item 2:
- approved item 3:

## Explicitly Rejected Work

List requests that came up but should not move forward under the current product definition.

- rejected item 1:
- rejected item 2:

## Final Note

If the pilot outcome suggests workflow routing, hidden authority substitution, implicit profile selection, or new outcome types, do not treat that as ordinary backlog grooming.

Treat it as a product-scope decision.
