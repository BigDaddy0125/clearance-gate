# Near-Real Pilot Dry-Run

Use this path when there is still no real caller in production, but the team wants one full dry-run using a caller payload that is closer to a real deployment or change-control request than the minimal examples.

## Goal

Run one complete local dry-run that starts from a near-real caller payload and ends with:

- a caller integration rehearsal
- a pilot evidence package
- a post-pilot review directory
- one dry-run manifest linking the full chain

## Default Inputs

The current maintained near-real inputs are:

- [near-real-authorize.json](/C:/work/clearance-gate/examples/real-caller-intake/near-real-authorize.json)
- [near-real-acknowledge.json](/C:/work/clearance-gate/examples/real-caller-intake/near-real-acknowledge.json)

## Runnable Helper

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-near-real-pilot-dry-run.ps1
```

This will:

- validate the current release bundle
- prepare pilot rollout material
- start a local published ClearanceGate instance
- run the caller integration rehearsal with the near-real inputs
- package pilot evidence
- prepare post-pilot review
- write a near-real dry-run manifest

## Success Rule

The near-real dry-run is acceptable only if:

- the caller rehearsal succeeds
- the evidence package is produced
- the post-pilot review directory is produced
- the resulting dry-run manifest links the exact evidence and review roots used
