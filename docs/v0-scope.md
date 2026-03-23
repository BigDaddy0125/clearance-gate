# ClearanceGate V0 Scope

This document defines what counts as a `v0` pilot-ready ClearanceGate.

It is intentionally narrow.

`v0` is not "the finished product".

`v0` is the first version that is narrow enough to pilot, while still preserving the core authorization-boundary semantics.

## V0 Product Definition

`v0` means ClearanceGate can act as a fail-closed authorization boundary for one concrete profile family, with durable evidence, bounded acknowledgment, idempotent request handling, and executable claim coverage.

For `v0`, ClearanceGate is:

- an authorization decision service
- an evidence and audit generator
- a profile-driven boundary for one pilot path

For `v0`, ClearanceGate is not:

- a workflow engine
- an action orchestrator
- a recommendation system
- a generalized policy marketplace
- a multi-tenant control plane

## V0 In Scope

The following capabilities are in scope for `v0`:

- explicit authorization requests via `POST /authorize`
- bounded acknowledgment via `POST /acknowledge`
- compact audit replay by decision id or request id
- fuller audit export by decision id or request id
- durable SQLite-backed evidence and audit persistence
- startup fail-closed validation for profiles, config, and audit schema
- one embedded pilot profile: `itops_deployment_v1`
- executable runtime claim coverage
- executable formal green/red baseline for `CG1` through `CG6`
- read-only profile diagnostics for embedded catalog visibility

## V0 Out Of Scope

The following are explicitly out of scope for `v0`:

- automatic profile selection at request time
- caller-transparent "latest profile" substitution
- downstream action execution
- human workflow routing
- generalized approval inboxes
- authentication/authorization productization beyond the current service boundary
- rich UI console
- multi-profile pilot packs
- distributed storage backends beyond the current SQLite path

## V0 Acceptance Criteria

`v0` is complete only if all of the following are true:

- all core outcomes remain constrained to `PROCEED`, `BLOCK`, `REQUIRE_ACK`, `DEGRADE`
- acknowledgment cannot turn blocked or degraded states into `PROCEED`
- non-blocking outcomes remain reconstructable from durable audit state
- request replay remains idempotent
- profile rules cannot weaken kernel guarantees
- startup remains fail-closed for invalid profile/config/schema conditions
- claim traceability remains `COMPLETE` for `CG1` through `CG6`
- runtime and formal verification gates are green

## V0 Pilot Shape

The target pilot shape for `v0` is:

- one profile family
- one narrow request shape
- one audit persistence path
- one formal regression suite
- one operational runbook

The point of `v0` is to prove boundary correctness and pilot operability, not product breadth.

## Current V0 Status

As of the current repository state:

- the core boundary and verification stack are in place
- startup/config/schema/profile fail-closed gates are in place
- audit compact/export and profile diagnostics are in place
- release-readiness checklist exists

The remaining `v0` work should be biased toward final readiness and pilot clarity, not new execution features.

## Next V0-Oriented Work

The next tasks that still fit inside `v0` are:

- deployment/runbook refinement
- final release checklist tightening
- pilot-facing API examples and sample request packs
- small diagnostics/readiness improvements that do not alter decision semantics
