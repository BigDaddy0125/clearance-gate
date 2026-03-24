# Real Caller Substitution

Use this path when a real or near-real caller payload is ready to replace the maintained near-real examples.

Its purpose is to prepare one substitution package that moves directly from caller-owned payload files into:

- an intake package
- a validated promotion package
- one ready-to-run rehearsal command

## Goal

Reduce the handoff work required when the first real caller payload arrives.

The substitution path should answer:

- which caller files are being used
- which explicit profile they target
- which intake package was created
- which promotion package was created
- which rehearsal command should run next

## Runnable Helper

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\prepare-real-caller-substitution.ps1 `
  -CallerSystem real-caller `
  -ActionDescription "Describe the gated execution action" `
  -AuthorizeInputPath .\path\to\caller-authorize.json `
  -AcknowledgeInputPath .\path\to\caller-acknowledge.json `
  -Profile itops_deployment_v1
```

## Success Rule

The substitution package is acceptable only if:

- the intake package is created
- the intake package validates with `READY_FOR_REHEARSAL`
- promotion succeeds
- the substitution manifest points to the exact intake and promotion roots used
- the package includes the exact rehearsal command to run next
