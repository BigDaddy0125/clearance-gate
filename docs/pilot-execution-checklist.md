# Pilot Execution Checklist

This checklist is for the actual pilot execution window.

It is narrower than the release checklist and more concrete than the acceptance checklist.

Use it when an operator is about to run a real pilot session.

## Before Starting

Confirm all of the following:

- the deployed build maps to the intended release bundle commit
- `ConnectionStrings__AuditStore` is set explicitly
- the SQLite audit directory exists and is writable
- [run-deployment-smoke-check.ps1](/C:/work/clearance-gate/scripts/run-deployment-smoke-check.ps1) is available on the host
- [operator-logging-guide.md](/C:/work/clearance-gate/docs/operator-logging-guide.md) is available to the operator
- [caller-onboarding-checklist.md](/C:/work/clearance-gate/docs/caller-onboarding-checklist.md) has been reviewed with the pilot caller

## Startup Checks

Confirm all of the following:

- the service starts without bypassing validation
- startup logs show `1000 ValidationStarted`
- startup logs show `1003 ValidationCompleted`
- audit initialization logs show `2002 InitializationCompleted`
- `GET /profiles` succeeds
- `GET /profiles/latest/itops_deployment` succeeds

## Smoke-Check to Evidence Chain

Run one bounded smoke-check and preserve its resulting identifiers.

Required sequence:

1. run [run-deployment-smoke-check.ps1](/C:/work/clearance-gate/scripts/run-deployment-smoke-check.ps1)
2. confirm authorization returns `REQUIRE_ACK`
3. confirm acknowledgment returns `PROCEED`
4. record the returned `requestId`
5. record the returned `decisionId`
6. record the returned `evidenceId`
7. confirm compact audit is retrievable
8. confirm export audit is retrievable

Operator rule:

- do not treat the smoke-check as complete until the identifiers and both audit views have been preserved

## Session Checks

For each pilot request, confirm:

- the caller sends an explicit `profile`
- the caller sends a stable `requestId`
- the result is handled exactly as returned
- any acknowledgment uses the bounded path only
- audit is retrievable by both `decisionId` and `requestId`

## Stop Conditions

Stop the pilot immediately if any of the following happen:

- startup validation is bypassed
- the smoke-check does not produce retrievable audit evidence
- a caller attempts to continue after `BLOCK` or `DEGRADE`
- repeated role rejection events indicate caller misuse is unresolved
- compact and export audit views diverge for the same decision

## End-of-Session Capture

At the end of the session, preserve:

- the release bundle commit
- the active profile id
- the smoke-check `requestId`
- the smoke-check `decisionId`
- the smoke-check `evidenceId`
- the compact audit export
- the full export audit envelope
- any relevant log excerpts containing boundary event ids
