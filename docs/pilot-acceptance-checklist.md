# Pilot Acceptance Checklist

This checklist is for the first real `v0` pilot, not for internal development only.

Its purpose is to answer one question:

- is the current ClearanceGate build acceptable for a narrow pilot boundary?

## Before Pilot Start

Confirm all of the following:

- the deployed build maps to a verified commit
- the release-readiness checklist is green
- the embedded profile catalog contains the intended pilot profile
- the audit store path is configured and writable
- the pilot operator has the example requests from [api-examples.md](/C:/work/clearance-gate/docs/api-examples.md)

## Pilot Entry Criteria

The pilot may start only if:

- `/profiles` responds successfully
- `/profiles/latest/itops_deployment` responds successfully
- a risk example returns `REQUIRE_ACK`
- acknowledgment resolves only the allowed risk path
- a blocked example remains blocked after acknowledgment attempt
- compact audit and export views can both be retrieved

## During Pilot

For each pilot session, confirm:

- the caller supplies an explicit `profile`
- the request uses a stable `requestId`
- the resulting `decisionId` and `evidenceId` are recorded
- audit can be retrieved by both decision id and request id
- any acknowledgment event has an accountable `acknowledging_authority`

## Pilot Success Criteria

The pilot is considered successful only if:

- the boundary stays fail-closed
- no blocked or degraded request is released incorrectly
- non-blocking outcomes remain reconstructable from stored audit state
- replaying the same `requestId` does not create divergent decisions
- profile diagnostics remain informational only

## Pilot Failure Criteria

Stop the pilot if any of the following happen:

- startup validation must be bypassed to keep the service running
- a blocked request becomes `PROCEED`
- audit cannot be reconstructed for a non-blocking decision
- repeated `requestId` values create divergent decision histories
- the active pilot needs behavior outside the current `v0` scope

## Evidence To Preserve

If the pilot is paused or fails, preserve:

- the configured SQLite database files
- the exact example or request payload used
- the `decisionId`
- the `requestId`
- the compact audit view
- the export audit view

## Pilot Review Questions

At the end of the pilot, answer:

1. Did the service remain a narrow authorization boundary rather than becoming a workflow tool?
2. Did operators understand the meaning of `PROCEED`, `BLOCK`, `REQUIRE_ACK`, and `DEGRADE`?
3. Were audit and export views sufficient for reconstruction and review?
4. Did any pilot requirement exceed the current `v0` scope?

## Decision Rule

After the pilot, the recommended outcomes are:

- continue with the current boundary shape
- tighten one or more existing gates
- pause and reduce scope

The recommended outcome is not:

- add broad new orchestration features without redefining scope first
