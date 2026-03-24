# Real Caller Rehearsal

Use this path when the maintained change-control adapter shape is close enough to a real caller that the team wants to rehearse with caller-owned payloads.

This is still a narrow pilot path.

It does not permit hidden profile selection, implicit authority substitution, or workflow behavior.

## Goal

Prove that a caller can supply its own request and acknowledgment payloads, map them deterministically through the maintained adapter shape, and still preserve:

- explicit `profile`
- stable `requestId`
- stable `decisionId`
- bounded acknowledgment
- reconstructable audit evidence

## Required Caller Shape

The current rehearsal still expects the same caller field shape as the maintained example:

- authorize payload
  - `ticketId`
  - `executionId`
  - `operation.kind`
  - `operation.summary`
  - `changeWindow`
  - `riskIndicators`
  - `requester.id`
  - `source.system`
  - `source.recordedAt`
- acknowledge payload
  - `executionId`
  - `authority.id`
  - `authority.recordedAt`

If the real caller cannot produce that shape, treat that as adapter work, not as a reason to widen ClearanceGate.

## Validation

Before rehearsal, validate the caller payloads:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-real-caller-rehearsal-input.ps1 `
  -AuthorizeInputPath .\path\to\caller-authorize.json `
  -AcknowledgeInputPath .\path\to\caller-acknowledge.json `
  -Profile itops_deployment_v1
```

This confirms:

- both files exist
- required caller fields are present
- authorize and acknowledge preserve the same execution id
- mapped output keeps explicit profile and bounded acknowledgment role

## Review Package

To prepare a focused review directory before rehearsal:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\prepare-real-caller-rehearsal.ps1 `
  -AuthorizeInputPath .\path\to\caller-authorize.json `
  -AcknowledgeInputPath .\path\to\caller-acknowledge.json `
  -Profile itops_deployment_v1
```

This produces a directory under `artifacts/real-caller-rehearsal/` containing:

- caller-owned input payloads
- mapped ClearanceGate requests
- the maintained converter
- onboarding and rehearsal docs

## Live Rehearsal

To run the live rehearsal against a local published ClearanceGate instance:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-caller-integration-rehearsal.ps1 `
  -AuthorizeInputPath .\path\to\caller-authorize.json `
  -AcknowledgeInputPath .\path\to\caller-acknowledge.json `
  -Profile itops_deployment_v1
```

## Success Rule

The real caller rehearsal is acceptable only if:

- input validation stays green
- the mapped authorize request returns `REQUIRE_ACK`
- the mapped acknowledgment returns `PROCEED`
- audit is readable by both `decisionId` and `requestId`
- a pilot evidence package is produced
- the caller review directory remains complete
