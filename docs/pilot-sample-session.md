# Pilot Sample Session

This document shows one complete `v0` pilot session from start to finish.

It is not a UI walkthrough.

It is an operator transcript for the current API boundary.

## Goal

Demonstrate one narrow pilot path where:

- a risk-flagged request becomes `REQUIRE_ACK`
- bounded acknowledgment turns it into `PROCEED`
- audit and export remain reconstructable by both `decisionId` and `requestId`
- profile diagnostics remain read-only

## Session Assets

Request files:

- [authorize-risk.json](/C:/work/clearance-gate/examples/v0/authorize-risk.json)
- [acknowledge-risk.json](/C:/work/clearance-gate/examples/v0/acknowledge-risk.json)

Expected response examples:

- [authorize-risk-response.json](/C:/work/clearance-gate/examples/v0/responses/authorize-risk-response.json)
- [acknowledge-risk-response.json](/C:/work/clearance-gate/examples/v0/responses/acknowledge-risk-response.json)
- [audit-risk-compact-response.json](/C:/work/clearance-gate/examples/v0/responses/audit-risk-compact-response.json)
- [audit-risk-export-response.json](/C:/work/clearance-gate/examples/v0/responses/audit-risk-export-response.json)
- [profiles-response.json](/C:/work/clearance-gate/examples/v0/responses/profiles-response.json)

Runnable helper:

- [run-sample-session.ps1](/C:/work/clearance-gate/examples/v0/run-sample-session.ps1)

## Session Sequence

### 1. Start the Service

In one PowerShell window:

```powershell
cd C:\work\clearance-gate
$env:ConnectionStrings__AuditStore = "Data Source=C:\clearancegate-data\clearancegate.db"
dotnet run --project .\src\ClearanceGate.Api\ClearanceGate.Api.csproj --configuration Release
```

### 2. Check Profile Diagnostics

In a second PowerShell window:

```powershell
cd C:\work\clearance-gate
Invoke-RestMethod -Method Get -Uri http://localhost:5000/profiles | ConvertTo-Json -Depth 10
Invoke-RestMethod -Method Get -Uri http://localhost:5000/profiles/latest/itops_deployment | ConvertTo-Json -Depth 10
```

The shape should match [profiles-response.json](/C:/work/clearance-gate/examples/v0/responses/profiles-response.json).

### 3. Authorize a Risk-Flagged Request

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:5000/authorize `
  -ContentType "application/json" `
  -InFile .\examples\v0\authorize-risk.json | ConvertTo-Json -Depth 10
```

Expected result:

- `outcome = REQUIRE_ACK`
- `clearanceState = AWAITING_ACK`
- `evidenceId` is present

Reference:

- [authorize-risk-response.json](/C:/work/clearance-gate/examples/v0/responses/authorize-risk-response.json)

### 4. Submit Bounded Acknowledgment

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:5000/acknowledge `
  -ContentType "application/json" `
  -InFile .\examples\v0\acknowledge-risk.json | ConvertTo-Json -Depth 10
```

Expected result:

- `outcome = PROCEED`
- `clearanceState = AUTHORIZED`

Reference:

- [acknowledge-risk-response.json](/C:/work/clearance-gate/examples/v0/responses/acknowledge-risk-response.json)

### 5. Retrieve Compact Audit

```powershell
Invoke-RestMethod -Method Get -Uri http://localhost:5000/audit/dec-example-risk-1 | ConvertTo-Json -Depth 10
Invoke-RestMethod -Method Get -Uri http://localhost:5000/audit/request/req-example-risk-1 | ConvertTo-Json -Depth 10
```

Expected result:

- two timeline entries: `AWAITING_ACK`, `AUTHORIZED`
- `outcome = PROCEED`
- `responsibility.acknowledger = alice`

Reference:

- [audit-risk-compact-response.json](/C:/work/clearance-gate/examples/v0/responses/audit-risk-compact-response.json)

### 6. Retrieve Export Audit

```powershell
Invoke-RestMethod -Method Get -Uri http://localhost:5000/audit/dec-example-risk-1/export | ConvertTo-Json -Depth 10
Invoke-RestMethod -Method Get -Uri http://localhost:5000/audit/request/req-example-risk-1/export | ConvertTo-Json -Depth 10
```

Expected result:

- compact/export core fields align
- export also shows `requestId`, `profile`, `summary`, and `clearanceState`

Reference:

- [audit-risk-export-response.json](/C:/work/clearance-gate/examples/v0/responses/audit-risk-export-response.json)

## Completion Signal

The sample session is successful only if all of the following hold:

- diagnostics succeed
- authorization returns `REQUIRE_ACK`
- acknowledgment returns `PROCEED`
- audit is readable by both `decisionId` and `requestId`
- compact and export views remain consistent

## Related Docs

- [api-examples.md](/C:/work/clearance-gate/docs/api-examples.md)
- [deployment-runbook.md](/C:/work/clearance-gate/docs/deployment-runbook.md)
- [pilot-acceptance-checklist.md](/C:/work/clearance-gate/docs/pilot-acceptance-checklist.md)
