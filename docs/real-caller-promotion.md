# Real Caller Promotion

Use this path after a real caller intake package has been filled in and marked ready.

Its purpose is to move one completed intake package into a rehearsal-ready package without rewriting the handoff logic each time.

## Goal

Promote one intake package from:

- collected caller-owned payload drafts

to:

- validated caller-owned payloads
- prepared real-caller rehearsal review material
- one promotion manifest that links the intake and rehearsal roots

## Ready Rule

An intake package may be promoted only when:

- `intake-manifest.json` exists
- `status` is `READY_FOR_REHEARSAL`
- placeholder values have been replaced
- caller-owned authorize and acknowledge payloads are present
- the payloads pass [validate-real-caller-intake-package.ps1](/C:/work/clearance-gate/scripts/validate-real-caller-intake-package.ps1)

## Validation

To validate one intake package:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-real-caller-intake-package.ps1 `
  -IntakeRoot .\artifacts\real-caller-intake\real-caller-intake-YYYYMMDD-HHMMSS `
  -RequireReadyStatus
```

## Promotion

To promote one intake package into a rehearsal-ready review package:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\promote-real-caller-intake.ps1 `
  -IntakeRoot .\artifacts\real-caller-intake\real-caller-intake-YYYYMMDD-HHMMSS
```

This writes a directory under `artifacts/real-caller-promotion/` containing:

- the intake manifest
- the rehearsal review manifest
- a promotion manifest linking both roots

## Success Rule

Promotion is acceptable only if:

- intake validation stays green
- the active profile remains explicit
- the resulting rehearsal package is prepared successfully
- the promotion manifest points to the exact intake and rehearsal roots used
