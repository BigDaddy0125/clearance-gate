# ClearanceGate Formal Verification Strategy

## Purpose

This document adapts the strongest parts of the `clawdbot-formal-models` method to ClearanceGate.

The key idea is not "one giant formal model for the whole product".

The key idea is:

- define concrete claims
- model each claim with the smallest useful abstraction
- keep a green model that should pass
- keep a red model that should fail with a counterexample
- run both in CI as a regression suite

This strategy is designed to support auditable assurance, not vague marketing claims.

Executable claim tests are allowed as a companion layer, but they do not replace formal models.

- integration tests prove the implementation currently respects the claim
- TLA+ models prove the claim is structurally meaningful under abstraction
- both should point to the same claim identifier

## What We Are Verifying

ClearanceGate should formally verify structural authorization correctness, not domain correctness.

In scope:

- clearance state exclusivity
- authorization outcome totality
- fail-closed behavior under uncertainty
- acknowledgment boundary correctness
- auditability reachability for non-blocking outcomes
- replay and idempotency properties on request processing
- profile conformance to kernel constraints

Out of scope:

- business correctness
- risk scoring quality
- prediction quality
- human judgment correctness
- downstream execution correctness
- physical-world safety

## Method

The repository should treat formal verification as a regression suite.

For each important claim:

1. Write the claim in plain language.
2. Map it to one invariant or temporal property.
3. Build the smallest TLA+ model that captures that property.
4. Add a green scenario that should pass.
5. Add a red scenario or bad variant that should fail.
6. Keep both runnable in CI.

This is the main lesson from `clawdbot-formal-models`: negative models are not optional. They prove the check is meaningful.

For ClearanceGate, each mature claim should ideally have:

- one claim entry in `docs/security-claims.md`
- one runtime test or harness in `tests/`
- one green TLA+ model
- one red TLA+ model

## Claim Inventory

The first verification wave for ClearanceGate should focus on these claims.

### CG1: Exactly one authorization outcome exists for every valid decision instance

Statement:

- a valid decision instance cannot simultaneously produce multiple outcomes
- a valid decision instance cannot end in an undefined authorization result

Targets:

- mutual exclusivity
- outcome totality

Green model:

- legal state mapping only

Red model:

- a buggy mapping that lets one state imply multiple outcomes

### CG2: Authorization never succeeds under degraded or insufficient conditions

Statement:

- `INFO_INSUFFICIENT` cannot lead to `PROCEED`
- `DEGRADED` cannot lead to `PROCEED`

Green model:

- fail-closed transition rules

Red model:

- a bad transition or outcome map that allows fail-open behavior

### CG3: Acknowledgment cannot override non-overridable constraints

Statement:

- `REQUIRE_ACK` is bounded responsibility acceptance, not arbitrary override power
- acknowledgment cannot authorize execution from degraded or structurally blocked states

Green model:

- acknowledgment only resolves allowed risk-flagged paths

Red model:

- bad acknowledgment semantics that turns any flagged state into `AUTHORIZED`

### CG4: Any non-blocking outcome is auditable

Statement:

- `PROCEED` and `REQUIRE_ACK` require reconstructable evidence
- no path exists from evaluation to non-blocking outcome without evidence persistence

Green model:

- outcome emission gated by evidence creation

Red model:

- bad model that emits `PROCEED` before evidence is durable

### CG5: Request processing is idempotent

Statement:

- replaying the same `request_id` returns the same outcome and same evidence reference
- duplicate processing does not create divergent authorization history

Green model:

- stable dedupe semantics

Red model:

- bad implementation that creates multiple evidence records or conflicting outcomes

### CG6: Profiles cannot weaken kernel invariants

Statement:

- a profile may define schema, constraints, and responsibility roles
- a profile may not add new outcomes or bypass audit and fail-closed rules

Green model:

- profile constrained by kernel

Red model:

- bad profile that re-enables forbidden path or introduces implicit allow

## Modeling Style

The modeling style should stay small and composable.

- kernel invariants get one spec each when practical
- scenario configs sit in `tla/models`
- bad variants live next to the green spec when the bug is semantic
- constants capture finite domains and profile-specific scenarios

Avoid a giant early model that tries to encode every product concern at once.

## Repository Pattern

Recommended structure:

- `docs/formal-verification-strategy.md`
- `docs/security-claims.md`
- `tla/specs/`
- `tla/models/`
- `tools/tla/`
- `scripts/`
- `generated/`

## CI Strategy

CI should run:

- a publishable green set
- a smaller expected-failure red set

Expected-failure runs must be checked carefully:

- failure must happen
- failure must be due to the intended invariant violation

This prevents vacuous checks and accidental CI greenwashing.

## Conformance Strategy

As the implementation matures, some formal constants should be generated from real code or real profile definitions.

Examples:

- authorization outcomes
- profile rule metadata
- non-overridable constraint definitions
- request schema constants

The formal models still remain abstractions, but extracted constants reduce drift.

## Rollout Plan

### Phase A: Kernel proofs

- state exclusivity
- totality
- fail-closed

### Phase B: Evidence and acknowledgment

- auditability reachability
- acknowledgment boundedness

### Phase C: Request semantics

- idempotency
- replay resistance
- durable evidence before allow

Current implementation status:

- CG1 and CG2 have green/red TLA coverage
- CG3 and CG5 have green/red TLA coverage plus runtime claim tests
- CG4 has green/red TLA coverage plus runtime durable-store coverage
- CG6 has green/red TLA coverage for profile conformance against kernel outcomes and evidence rules

### Phase D: Profile conformance

- profile cannot weaken kernel guarantees

## Claim Discipline

ClearanceGate should only make claims that have:

- a written statement
- a linked spec
- a runnable model
- a reproducible green run
- a reproducible red run

That discipline is the most important pattern to borrow from the Clawdbot repo.
