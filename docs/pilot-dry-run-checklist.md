# Pilot Dry-Run Checklist

Use this checklist before the first controlled pilot rollout.

It is for the final rehearsal, not for ordinary local development.

## Inputs

Confirm all of the following are ready:

- the release bundle
- the release review directory
- the deployment config selected for the target environment
- the operator logging guide and triage cheatsheet
- the caller onboarding checklist

## Dry-Run Sequence

Perform the following in order:

1. start the service with the intended pilot configuration
2. confirm startup validation completes without bypass
3. confirm audit store initialization completes
4. run [run-deployment-smoke-check.ps1](/C:/work/clearance-gate/scripts/run-deployment-smoke-check.ps1)
5. run [capture-pilot-sample-session.ps1](/C:/work/clearance-gate/scripts/capture-pilot-sample-session.ps1)
6. prepare a post-pilot review directory from the captured evidence

## Required Signals

The dry run is acceptable only if:

- startup shows `1003 ValidationCompleted`
- audit store shows `2002 InitializationCompleted`
- the smoke-check returns the bounded `REQUIRE_ACK -> PROCEED` path
- the captured session produces a pilot evidence package
- the post-pilot review directory includes `decision-memo-draft.md`

## Stop Signals

Stop the dry run if any of the following happen:

- startup validation fails
- smoke-check does not preserve evidence
- compact and export audit diverge
- caller role or profile role mismatches appear unexpectedly
- rollback instructions cannot be followed cleanly

## Exit Rule

The pilot may proceed only if this dry run completes without boundary exceptions and without bypassing the current guards.
