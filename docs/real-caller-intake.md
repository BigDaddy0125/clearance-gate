# Real Caller Intake

Use this path before the first real caller rehearsal.

Its purpose is to collect caller-owned payloads and integration metadata in a consistent shape before any live rehearsal starts.

This is not a new runtime feature.

It is a handoff discipline for the existing authorization boundary.

## Goal

Collect the minimum caller-owned inputs needed to move from onboarding into a real rehearsal:

- one candidate authorize payload
- one candidate acknowledge payload
- one explicit profile
- one caller system name
- one short description of the gated execution action

## Required Intake Outputs

An acceptable intake package must contain:

- a caller authorize payload draft
- a caller acknowledge payload draft
- an intake manifest
- the current caller onboarding and adapter checklists
- the current real-caller rehearsal guide

## Intake Rules

The intake package must keep these rules explicit:

- `profile` selection remains explicit
- the caller owns its request payloads
- ClearanceGate does not infer authority
- ClearanceGate does not pick profiles from diagnostics
- caller retries must preserve one logical `requestId`

## Initialization

To prepare a starter intake package:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\initialize-real-caller-intake.ps1 `
  -CallerSystem example-caller `
  -Profile itops_deployment_v1 `
  -ActionDescription "Describe the gated execution action"
```

This creates a directory under `artifacts/real-caller-intake/` containing:

- caller payload templates
- an intake manifest template
- the relevant onboarding and rehearsal docs

## Exit Condition

The intake package is ready to move forward only when:

- the caller payload drafts are filled in
- the caller system and action are identified
- the active profile is explicit
- the caller agrees to deterministic mapping only
- the package can move directly into [real-caller-rehearsal.md](/C:/work/clearance-gate/docs/real-caller-rehearsal.md)

Once the package is filled in, validate and promote it through:

- [validate-real-caller-intake-package.ps1](/C:/work/clearance-gate/scripts/validate-real-caller-intake-package.ps1)
- [real-caller-promotion.md](/C:/work/clearance-gate/docs/real-caller-promotion.md)
