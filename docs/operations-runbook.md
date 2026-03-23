# Operations Runbook

This runbook covers the current operational boundary for ClearanceGate's minimal executable service.

## Startup Preconditions

- Embedded profiles must load and validate at process startup.
- Embedded profile names must use canonical versioned identity: `<family>_v<positive integer>`.
- The audit store connection string must be a valid SQLite connection string with a non-empty `Data Source`.
- The audit database schema must be at or below the current supported schema version.
- Unknown future audit schema versions must stop startup rather than degrade open.

## Startup Failure Modes

- Invalid profile catalog:
  The process fails during startup validation and does not serve requests.
  This includes malformed profile identities or duplicate family/version pairs.
- Invalid audit store configuration:
  The process fails during startup validation and does not serve requests.
- Unsupported audit schema version:
  The process fails during startup validation and does not serve requests.
- Missing or legacy audit schema:
  Startup applies the supported migration chain and stamps the current schema version.

## Local Verification

Run the runtime suite:

```powershell
dotnet test .\tests\ClearanceGate.Api.Tests\ClearanceGate.Api.Tests.csproj --configuration Release
```

Run the formal suite:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-tlc.ps1 -IncludeRed
```

Run the traceability gate:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-claim-traceability.ps1
```

## Audit Store Notes

- The current schema version is defined in [AuditStoreSchema.cs](/C:/work/clearance-gate/src/ClearanceGate.Audit/AuditStoreSchema.cs).
- The migration pipeline is defined in [SqliteAuditStoreInitializer.cs](/C:/work/clearance-gate/src/ClearanceGate.Audit/SqliteAuditStoreInitializer.cs).
- Fresh and legacy databases are covered by [AuditStoreSchemaTests.cs](/C:/work/clearance-gate/tests/ClearanceGate.Api.Tests/AuditStoreSchemaTests.cs).
- `GET /audit/{decisionId}` returns the compact replay view used by existing claim tests.
- `GET /audit/request/{requestId}` returns the compact replay view keyed by the idempotent request identifier.
- `GET /audit/{decisionId}/export` returns a fuller reconstructable envelope for external review or archival export.
- `GET /audit/request/{requestId}/export` returns the fuller reconstructable envelope keyed by request id for idempotent external reconciliation.

## Profile Diagnostics Notes

- `GET /profiles` returns the embedded profile catalog with family/version metadata.
- `GET /profiles/latest/{family}` returns the latest embedded version for a known profile family.
- These diagnostics are read-only; authorization requests still must declare an explicit profile id.

## Deployment Notes

- Set `ConnectionStrings__AuditStore` explicitly in non-local environments.
- Use [appsettings.Production.example.json](/C:/work/clearance-gate/examples/deployment/appsettings.Production.example.json) only as a shape reference; keep the real database path environment-specific.
- Keep the SQLite audit file in a dedicated writable directory outside the repository checkout.
- Back up the audit database file before upgrades or pilot resets.
- Treat startup validation failure as a release-blocking condition.
- Do not bypass startup validation to recover from schema or profile errors; fix the boundary condition and restart.
- Use [run-deployment-smoke-check.ps1](/C:/work/clearance-gate/scripts/run-deployment-smoke-check.ps1) after startup to confirm the bounded authorize/acknowledge/audit flow still holds.

## Structured Logging Notes

The current boundary now emits stable structured logs for:

- startup validation start and completion
- audit store initialization and schema readiness
- authorization replay and authorization decision persistence
- acknowledgment rejection and acknowledgment persistence
- compact and export audit lookups, including not-found cases

Useful fields to preserve in operators' log pipelines:

- `RequestId`
- `DecisionId`
- `Profile`
- `Outcome`
- `ClearanceState`
- `EvidenceId`
- `LookupKind`
- `ViewKind`
