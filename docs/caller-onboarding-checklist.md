# Caller Onboarding Checklist

This checklist is for the external system or team that wants to call ClearanceGate.

Its purpose is to confirm that the caller integrates with ClearanceGate as an authorization boundary, not as a workflow or recommendation system.

## Integration Goal

Before onboarding, the caller should be able to answer:

- which execution action is being gated
- which system will call `POST /authorize`
- which human authority can issue bounded acknowledgment when required
- how the caller will stop execution on `BLOCK` or `DEGRADE`

If those answers are unclear, onboarding is incomplete.

## Caller Preconditions

Confirm all of the following:

- the caller can make HTTP requests to the ClearanceGate API
- the caller can provide an explicit `profile` value
- the caller can generate a stable `requestId`
- the caller can preserve the returned `decisionId` and `evidenceId`
- the caller can retrieve audit by `decisionId` or `requestId`

## Request Contract Checklist

The caller must supply:

- `requestId`
  - stable for retries and replay of the same logical request
- `decisionId`
  - unique for the attempted authorization decision
- `profile`
  - explicit, never inferred by ClearanceGate
- `action`
  - enough description for audit reconstruction
- `context`
  - only fields supported by the active profile and caller mapping
- `responsibility`
  - explicit owner and role
- `metadata`
  - source system and timestamp

The caller must not:

- omit `profile`
- rely on "latest profile" lookup as an execution shortcut
- generate a new `requestId` for every retry of the same request
- execute the action before authorization is resolved

## Outcome Handling Checklist

The caller must implement these rules exactly:

- `PROCEED`
  - execution may continue
- `BLOCK`
  - execution must not continue
- `REQUIRE_ACK`
  - execution must pause until bounded acknowledgment succeeds
- `DEGRADE`
  - execution must not continue

The caller must not:

- treat `BLOCK` as retriable approval
- treat `DEGRADE` as soft success
- auto-acknowledge on behalf of a human authority

## Acknowledgment Checklist

If the caller supports acknowledgment:

- it must preserve the original `decisionId`
- it must send an explicit acknowledger id
- it must send the role `acknowledging_authority`
- it must record who actually performed the acknowledgment

If the caller cannot meet those conditions, it should not implement acknowledgment flow yet.

## Audit And Reconciliation Checklist

The caller should be ready to:

- record `decisionId`
- record `requestId`
- record `evidenceId`
- retrieve compact audit for quick review
- retrieve export audit for fuller reconstruction

Recommended reconciliation path:

- use `requestId` for idempotent caller-side correlation
- use `decisionId` when following one specific authorization record

## Pilot Adapter Checklist

If the caller uses an adapter layer:

- mapping must be deterministic
- profile selection must remain explicit
- authority mapping must remain explicit
- adapter output should match the current request contract exactly

Reference:

- [pilot-adapter-example.md](/C:/work/clearance-gate/docs/pilot-adapter-example.md)

## Operational Checklist

Before first real use, the caller team should verify:

- the deployed ClearanceGate build maps to a verified commit or bundle
- startup validation passed
- `/profiles` is reachable
- `/profiles/latest/{family}` is used only for diagnostics, not hidden execution-time substitution
- one smoke flow has been run successfully

## Stop Conditions

Do not onboard the caller yet if any of the following are true:

- the caller wants ClearanceGate to pick the profile automatically
- the caller wants ClearanceGate to execute downstream actions
- the caller wants implicit approval routing
- the caller cannot stop execution on `BLOCK` or `DEGRADE`
- the caller cannot preserve stable request correlation

Those are not onboarding gaps.

They are product-scope conflicts.
