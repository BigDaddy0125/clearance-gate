# Caller Integration Rehearsal

This rehearsal uses the maintained change-control adapter pattern against a live ClearanceGate instance.

It is the narrowest possible rehearsal before a real caller pilot.

## Goal

Demonstrate that one external caller shape can:

- map deterministically into the current boundary
- receive `REQUIRE_ACK`
- resolve through bounded acknowledgment to `PROCEED`
- preserve reconstructable audit evidence

## Inputs

The rehearsal uses:

- [change-control-request.json](/C:/work/clearance-gate/examples/pilot-adapter/change-control-request.json)
- [change-control-ack.json](/C:/work/clearance-gate/examples/pilot-adapter/change-control-ack.json)
- [convert-change-control-example.ps1](/C:/work/clearance-gate/examples/pilot-adapter/convert-change-control-example.ps1)

## Runnable Helper

Use:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-caller-integration-rehearsal.ps1
```

This will:

- start a local published ClearanceGate instance
- generate unique caller-side ids
- convert caller payloads through the maintained adapter
- call `/authorize` and `/acknowledge`
- retrieve compact and export audit views
- package pilot evidence
- prepare caller integration review material

## Success Rule

The rehearsal is acceptable only if:

- the adapter validation stays green
- authorize returns `REQUIRE_ACK`
- acknowledgment returns `PROCEED`
- audit is readable by both `decisionId` and `requestId`
- the evidence package and caller integration review are both produced
