# Operator Triage Cheatsheet

This is the shortest operator reference for the current boundary.

Use it during deployment checks, pilot execution, and release review.

## Healthy Startup

Look for this sequence:

- `1000 ValidationStarted`
- `1002 ProfileCatalogLoaded`
- `1003 ValidationCompleted`
- `2002 InitializationCompleted`

If that sequence is present, startup passed the current boundary checks.

## Stop Immediately

Treat these as stop signals:

- `1004 ValidationFailed`
- missing `1003 ValidationCompleted`
- missing `2002 InitializationCompleted`
- any sign that startup validation was bypassed

## Caller Problem

These are usually caller-side problems:

- `3003 CallerRoleRejected`
- `3105 CallerRoleRejected`

Action:

- stop the affected caller flow
- compare the caller against [caller-onboarding-checklist.md](/C:/work/clearance-gate/docs/caller-onboarding-checklist.md)

## Profile Problem

These are usually embedded-profile or configuration problems:

- `3002 ProfileRoleRejected`
- `3104 ProfileRoleRejected`

Action:

- pause rollout
- confirm the release bundle and embedded profile catalog match expectations

## Expected But Not Healthy

These are not process crashes, but still matter:

- `3102 InvalidStateRejected`
- `3201 LookupNotFound`

Interpret them carefully:

- `3102` means acknowledgment was attempted on a non-eligible decision
- `3201` is only an incident when a durable record should already exist

## One-Line Rules

- startup bad: `1004`
- config/schema suspicious: missing `1003` or `2002`
- caller misuse: `3003` or `3105`
- profile mismatch: `3002` or `3104`
- normal replay: `3000`
- healthy durable authorize: `3001`
- healthy durable acknowledgment: `3103`

## Sample Logs

Use [operator-log-sample.jsonl](/C:/work/clearance-gate/examples/operations/operator-log-sample.jsonl) as the current example shape for triage.

## Source Anchors

- [operator-logging-guide.md](/C:/work/clearance-gate/docs/operator-logging-guide.md)
- [observability-contract.md](/C:/work/clearance-gate/docs/observability-contract.md)
