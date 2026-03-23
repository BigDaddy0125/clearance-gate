# Observability Contract

This document defines the current log contract for ClearanceGate's boundary events.

It does not introduce a logging product surface.

It defines which events operators should expect and which fields should be preserved if logs are shipped elsewhere.

## Scope

The current contract covers:

- startup validation
- audit store initialization
- authorization decisions
- acknowledgment decisions
- audit lookup paths

It does not define metrics, traces, or long-term storage requirements.

## Event Id Ranges

| Range | Area |
| --- | --- |
| `1000-1099` | startup validation |
| `2000-2099` | audit store initialization |
| `3000-3099` | authorization |
| `3100-3199` | acknowledgment |
| `3200-3299` | audit query |

## Current Event Ids

| Event Id | Name | Meaning |
| --- | --- | --- |
| `1000` | `ValidationStarted` | fail-closed startup validation began |
| `1001` | `AuditStoreOptionsResolved` | audit store config was resolved for startup validation |
| `1002` | `ProfileCatalogLoaded` | embedded profile catalog loaded successfully at startup |
| `1003` | `ValidationCompleted` | startup validation completed successfully |
| `1004` | `ValidationFailed` | startup validation failed and startup should stop |
| `2000` | `InitializationStarted` | audit store initialization began |
| `2001` | `DirectoryReady` | audit store directory was ensured |
| `2002` | `InitializationCompleted` | audit store schema is ready for use |
| `3000` | `ReplayReturned` | request id replay returned an existing durable record |
| `3001` | `DecisionRecorded` | authorization decision was durably recorded |
| `3002` | `ProfileRoleRejected` | profile configuration could not satisfy authorization role requirement |
| `3003` | `CallerRoleRejected` | caller supplied wrong authorization role |
| `3100` | `DecisionNotFound` | acknowledgment target decision was not found |
| `3101` | `DecisionDisappeared` | decision disappeared before acknowledgment write completed |
| `3102` | `InvalidStateRejected` | acknowledgment was rejected because state was not eligible |
| `3103` | `AcknowledgmentRecorded` | bounded acknowledgment was durably recorded |
| `3104` | `ProfileRoleRejected` | profile configuration could not satisfy acknowledgment role requirement |
| `3105` | `CallerRoleRejected` | caller supplied wrong acknowledgment role |
| `3200` | `LookupReturned` | audit lookup returned a record |
| `3201` | `LookupNotFound` | audit lookup returned no record |

## Stable Structured Fields

Operators should preserve these fields when present:

- `RequestId`
- `DecisionId`
- `Profile`
- `Outcome`
- `ClearanceState`
- `EvidenceId`
- `ConstraintCount`
- `AcknowledgerId`
- `LookupKind`
- `LookupValue`
- `ViewKind`
- `ConnectionStringPresent`
- `DataSource`
- `SchemaVersion`
- `CurrentVersion`

## Interpretation Rules

- `ValidationFailed` is release-blocking and startup should stop.
- `DecisionRecorded` means the decision reached durable audit storage.
- `ReplayReturned` means idempotency returned the canonical earlier record.
- `AcknowledgmentRecorded` does not mean generic override; it means a bounded `AWAITING_ACK` path resolved successfully.
- `LookupNotFound` is expected for unknown ids and should not be treated as process failure by itself.

## Source Anchors

The current event definitions live in:

- [StartupValidation.cs](/C:/work/clearance-gate/src/ClearanceGate.Api/StartupValidation.cs)
- [SqliteAuditStoreInitializer.cs](/C:/work/clearance-gate/src/ClearanceGate.Audit/SqliteAuditStoreInitializer.cs)
- [AuthorizationService.cs](/C:/work/clearance-gate/src/ClearanceGate.Application/Services/AuthorizationService.cs)
- [AcknowledgmentService.cs](/C:/work/clearance-gate/src/ClearanceGate.Application/Services/AcknowledgmentService.cs)
- [AuditQueryService.cs](/C:/work/clearance-gate/src/ClearanceGate.Application/Services/AuditQueryService.cs)

## Current Limits

This contract is intentionally minimal.

Current limitations:

- no dedicated metrics surface
- no distributed tracing contract
- no separate audit-log sink
- no guarantee yet about external log serialization format

The stability promise is at the event-id and structured-field level, not the exact rendered console line format.
