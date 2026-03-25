# Real Caller Execution Checklist

Use this checklist when a caller team has provided real payload files and the team wants to run the current ClearanceGate path end to end:

1. intake
2. rehearsal
3. controlled pilot execution

This is the shortest path that turns caller-owned files into:

- a validated substitution package
- a live caller rehearsal
- a full controlled-pilot dry-run with evidence and post-pilot review

## File Placement

Put the caller-owned files in the repo before running anything.

Recommended local paths:

- `C:\work\clearance-gate\temp\real-authorize.json`
- `C:\work\clearance-gate\temp\real-acknowledge.json`

If the files live elsewhere, replace the paths in the commands below.

## Required Inputs

You need:

- one authorize payload file
- one acknowledge payload file
- one explicit profile id
- one caller system label
- one short action description

The current default profile for this path is `itops_deployment_v1`.

If the target host does not use the maintained local default key, also pass:

- `-ApiKey "<real-api-key>"`

## Step 1: Intake And Promotion

Run:

```powershell
cd C:\work\clearance-gate

powershell -ExecutionPolicy Bypass -File .\scripts\prepare-real-caller-substitution.ps1 `
  -CallerSystem "real-caller-system" `
  -ActionDescription "Production deployment execution gate" `
  -AuthorizeInputPath .\temp\real-authorize.json `
  -AcknowledgeInputPath .\temp\real-acknowledge.json `
  -Profile itops_deployment_v1
```

This step will:

- initialize a real-caller intake package
- copy the caller-owned payload files into that intake package
- validate the intake package
- promote the intake package into rehearsal-ready material
- create a substitution package with the exact next rehearsal command

Expected output roots:

- `artifacts/real-caller-intake/...`
- `artifacts/real-caller-promotion/...`
- `artifacts/real-caller-substitution/...`

## Step 2: Live Rehearsal

Run:

```powershell
cd C:\work\clearance-gate

powershell -ExecutionPolicy Bypass -File .\scripts\run-caller-integration-rehearsal.ps1 `
  -AuthorizeInputPath .\temp\real-authorize.json `
  -AcknowledgeInputPath .\temp\real-acknowledge.json `
  -Profile itops_deployment_v1 `
  -ApiKey "replace-with-real-api-key"
```

This step will:

- start a local published ClearanceGate instance
- convert the caller payloads through the maintained adapter path
- call `/authorize`
- call `/acknowledge`
- retrieve compact and export audit views
- package pilot evidence

Expected output roots:

- `artifacts/caller-integration-rehearsal/...`
- `artifacts/pilot-evidence/...`

## Step 3: Controlled Pilot Execution

Run:

```powershell
cd C:\work\clearance-gate

powershell -ExecutionPolicy Bypass -File .\scripts\run-near-real-pilot-dry-run.ps1 `
  -AuthorizeInputPath .\temp\real-authorize.json `
  -AcknowledgeInputPath .\temp\real-acknowledge.json `
  -Profile itops_deployment_v1 `
  -ApiKey "replace-with-real-api-key"
```

This step will:

- prepare pilot rollout material
- start a local published ClearanceGate instance
- run the same caller payloads through a full dry-run chain
- generate pilot evidence
- prepare post-pilot review
- generate a decision memo draft

Expected output roots:

- `artifacts/near-real-pilot-dry-run/...`
- `artifacts/pilot-evidence/...`
- `artifacts/post-pilot-review/...`

## Pass Conditions

The path is healthy only if:

- substitution succeeds without changing the explicit profile
- rehearsal returns `REQUIRE_ACK` from `/authorize`
- rehearsal returns `PROCEED` from `/acknowledge`
- compact and export audit are both readable
- the controlled-pilot dry-run produces evidence and post-pilot review artifacts

## Related Docs

- [real-caller-substitution.md](/C:/work/clearance-gate/docs/real-caller-substitution.md)
- [caller-integration-rehearsal.md](/C:/work/clearance-gate/docs/caller-integration-rehearsal.md)
- [near-real-pilot-dry-run.md](/C:/work/clearance-gate/docs/near-real-pilot-dry-run.md)
- [real-caller-intake.md](/C:/work/clearance-gate/docs/real-caller-intake.md)
