# Pilot Adapter Example

This document shows one narrow caller-side adapter pattern for the current `v0` pilot.

Its goal is not to create an integration framework.

Its goal is to show how an external caller can map its own payload shape into the current ClearanceGate boundary without weakening the boundary.

## Scope

This example assumes:

- one caller domain
- one explicit profile
- one explicit request mapping
- no hidden profile selection
- no hidden acknowledgment authority selection

The adapter remains outside ClearanceGate itself.

## Boundary Rules

The adapter may:

- rename fields
- reshape caller metadata into the current request contract
- map caller risk indicators into explicit `riskFlags`
- compose deterministic ids from caller identifiers

The adapter must not:

- omit `profile`
- pick a profile implicitly from heuristics
- suppress required fields that the profile expects
- convert a blocked condition into an acknowledgment-eligible one
- auto-acknowledge on behalf of a human authority

## Example Caller Shape

The example input files are:

- [change-control-request.json](/C:/work/clearance-gate/examples/pilot-adapter/change-control-request.json)
- [change-control-ack.json](/C:/work/clearance-gate/examples/pilot-adapter/change-control-ack.json)

These represent one hypothetical change-control caller that does not use ClearanceGate's native wire format directly.

## Mapping Rules

Authorization mapping:

- `ticketId` -> `requestId`
- `executionId` -> `decisionId`
- explicit adapter argument `profile` -> `profile`
- `operation.kind` -> `action.type`
- `operation.summary` -> `action.description`
- `changeWindow` -> `context.attributes.changeWindow`
- `riskIndicators` -> `riskFlags`
- `requester.id` -> `responsibility.owner`
- fixed role `decision_owner` -> `responsibility.role`
- `source.system` -> `metadata.sourceSystem`
- `source.recordedAt` -> `metadata.timestamp`

Acknowledgment mapping:

- `executionId` -> `decisionId`
- `authority.id` -> `acknowledger.id`
- fixed role `acknowledging_authority` -> `acknowledger.role`
- fixed type `risk_acceptance` -> `acknowledgment.type`
- `authority.recordedAt` -> `acknowledgment.timestamp`

## Example Output

The mapped ClearanceGate request bodies are:

- [mapped-authorize-request.json](/C:/work/clearance-gate/examples/pilot-adapter/mapped-authorize-request.json)
- [mapped-acknowledge-request.json](/C:/work/clearance-gate/examples/pilot-adapter/mapped-acknowledge-request.json)

## Conversion Helper

Use [convert-change-control-example.ps1](/C:/work/clearance-gate/examples/pilot-adapter/convert-change-control-example.ps1) to convert the example caller payloads into the current ClearanceGate request bodies.

Examples:

```powershell
powershell -ExecutionPolicy Bypass -File .\examples\pilot-adapter\convert-change-control-example.ps1 -Mode authorize
powershell -ExecutionPolicy Bypass -File .\examples\pilot-adapter\convert-change-control-example.ps1 -Mode acknowledge
```

## Why This Is Safe

This adapter pattern stays consistent with ClearanceGate's current role:

- the caller still sends an explicit profile
- the adapter only performs deterministic translation
- all boundary enforcement still happens inside ClearanceGate
- acknowledgment remains bounded and explicit

If a future adapter would need hidden profile selection, implicit authority substitution, or workflow routing, that is no longer a narrow pilot adapter. It is a product-scope change.
