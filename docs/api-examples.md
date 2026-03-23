# API Examples

This document is the pilot-facing example pack for ClearanceGate `v0`.

It shows the current API surface as it exists now.

These examples are intentionally narrow and map to the single embedded pilot profile:

- `itops_deployment_v1`

## Preconditions

- the service is running locally
- the startup gate has passed
- the embedded profile catalog contains `itops_deployment_v1`

Default local base URL:

```text
http://localhost:5000
```

## Files

Reusable request bodies are provided in:

- [authorize-risk.json](/C:/work/clearance-gate/examples/v0/authorize-risk.json)
- [authorize-blocked.json](/C:/work/clearance-gate/examples/v0/authorize-blocked.json)
- [acknowledge-risk.json](/C:/work/clearance-gate/examples/v0/acknowledge-risk.json)

## Flow 1: Risk-Flagged Request Requiring Acknowledgment

Authorize:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:5000/authorize `
  -ContentType "application/json" `
  -InFile .\examples\v0\authorize-risk.json
```

Expected response shape:

```json
{
  "decisionId": "dec-example-risk-1",
  "outcome": "REQUIRE_ACK",
  "clearanceState": "AWAITING_ACK",
  "evidenceId": "evidence:dec-example-risk-1",
  "reason": {
    "summary": "Authorization requires explicit acknowledgment.",
    "constraintsTriggered": [
      "RISK_ACK_REQUIRED"
    ]
  },
  "version": {
    "kernel": "0.1.0",
    "policy": "itops_deployment_v1"
  }
}
```

Acknowledge:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:5000/acknowledge `
  -ContentType "application/json" `
  -InFile .\examples\v0\acknowledge-risk.json
```

Expected response shape:

```json
{
  "decisionId": "dec-example-risk-1",
  "outcome": "PROCEED",
  "clearanceState": "AUTHORIZED",
  "evidenceId": "evidence:dec-example-risk-1"
}
```

Read compact audit:

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri http://localhost:5000/audit/dec-example-risk-1
```

Expected compact audit shape:

```json
{
  "decisionId": "dec-example-risk-1",
  "evidenceId": "evidence:dec-example-risk-1",
  "authorizationTimeline": [
    { "state": "AWAITING_ACK", "timestamp": "2026-03-18T10:00:00Z" },
    { "state": "AUTHORIZED", "timestamp": "2026-03-18T10:05:00Z" }
  ],
  "outcome": "PROCEED",
  "responsibility": {
    "owner": "alice",
    "acknowledger": "alice"
  },
  "constraintsApplied": [
    "RISK_ACK_REQUIRED"
  ],
  "version": {
    "kernel": "0.1.0",
    "policy": "itops_deployment_v1"
  }
}
```

Read fuller export:

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri http://localhost:5000/audit/dec-example-risk-1/export
```

The export view includes:

- `requestId`
- `profile`
- `summary`
- `clearanceState`

## Flow 2: Structurally Blocked Request

Authorize:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:5000/authorize `
  -ContentType "application/json" `
  -InFile .\examples\v0\authorize-blocked.json
```

Expected response shape:

```json
{
  "decisionId": "dec-example-block-1",
  "outcome": "BLOCK",
  "clearanceState": "INFO_INSUFFICIENT",
  "evidenceId": "evidence:dec-example-block-1",
  "reason": {
    "summary": "Authorization blocked because required context is missing.",
    "constraintsTriggered": [
      "OWNER_REQUIRED",
      "SOURCE_REQUIRED"
    ]
  },
  "version": {
    "kernel": "0.1.0",
    "policy": "itops_deployment_v1"
  }
}
```

Acknowledgment must not release this decision:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:5000/acknowledge `
  -ContentType "application/json" `
  -InFile .\examples\v0\acknowledge-risk.json
```

Expected result:

- HTTP `409`
- title `Acknowledgment rejected`

## Flow 3: Request-Id Based Audit Lookup

Compact view by request id:

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri http://localhost:5000/audit/request/req-example-risk-1
```

Export view by request id:

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri http://localhost:5000/audit/request/req-example-risk-1/export
```

These endpoints exist so external systems can reconcile using the idempotent request identifier rather than a decision id discovered later.

## Flow 4: Profile Diagnostics

List embedded profiles:

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri http://localhost:5000/profiles
```

Expected response shape:

```json
{
  "profiles": [
    {
      "profile": "itops_deployment_v1",
      "family": "itops_deployment",
      "version": 1,
      "description": "Initial profile for deployment and change authorization.",
      "isLatest": true
    }
  ]
}
```

Resolve latest version for a family:

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri http://localhost:5000/profiles/latest/itops_deployment
```

Important:

- this is read-only diagnostics
- callers still must send an explicit `profile` on `/authorize`

## Notes

- ClearanceGate does not execute the action described in the request.
- ClearanceGate does not choose a profile automatically.
- `v0` remains intentionally narrow: one pilot profile, one explicit request shape, one durable audit path.
