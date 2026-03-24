# Pilot Incident Response

This is a minimal response guide for the current pilot boundary.

It is not a general incident management process.

It only explains how to react when the pilot boundary appears unhealthy.

## First Rule

Do not widen the boundary to keep the pilot running.

If the service can only continue by bypassing validation or ignoring outcomes, stop the pilot.

## Startup Failure

Typical signals:

- `1004 ValidationFailed`
- missing `1003 ValidationCompleted`
- missing `2002 InitializationCompleted`

Immediate action:

1. stop rollout
2. preserve the current configuration and recent logs
3. verify `ConnectionStrings__AuditStore`
4. verify the embedded profile catalog has not been corrupted
5. restart only after the boundary condition is corrected

## Caller Integration Failure

Typical signals:

- repeated `3003 CallerRoleRejected`
- repeated `3105 CallerRoleRejected`
- caller attempts to continue after `BLOCK` or `DEGRADE`

Immediate action:

1. stop that caller flow
2. preserve the offending request payload
3. verify the caller against [caller-onboarding-checklist.md](/C:/work/clearance-gate/docs/caller-onboarding-checklist.md)
4. resume only after the caller is sending the intended role and handling outcomes correctly

## Profile Configuration Failure

Typical signals:

- repeated `3002 ProfileRoleRejected`
- repeated `3104 ProfileRoleRejected`

Immediate action:

1. pause the pilot
2. preserve the active profile id and related requests
3. confirm the embedded profile catalog matches the intended release bundle
4. do not patch the live boundary ad hoc

## Evidence Failure

Typical signals:

- smoke-check completes but audit cannot be retrieved
- compact and export views disagree
- expected durable audit state is missing for a non-blocking outcome

Immediate action:

1. stop the pilot
2. preserve the SQLite audit files
3. preserve the `requestId`, `decisionId`, and `evidenceId`
4. export both compact and full audit views if still available
5. treat this as a release-blocking issue until reconstruction is explained

## Not-Found Queries

Typical signal:

- `3201 LookupNotFound`

Interpretation:

- not all `3201` events are incidents
- they are only incidents when a durable decision should already exist

Immediate action:

1. confirm whether the caller expected an existing decision
2. if yes, preserve the identifiers and logs
3. if no, treat it as normal operator noise

## Minimum Evidence Set

For any pilot incident, preserve:

- release bundle commit
- active profile id
- `requestId`
- `decisionId`
- `evidenceId` when present
- compact audit response
- export audit response
- relevant structured log lines
- current SQLite database files
