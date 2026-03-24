# Operator Logging Guide

This guide explains how an operator should read ClearanceGate's current structured logs.

It is not a general observability handbook.

It is the smallest guide needed to decide whether a startup, deployment, or pilot run is healthy.

## First Principle

Read ClearanceGate logs as boundary events, not as generic application chatter.

The important question is:

- did the authorization boundary start correctly and preserve its fail-closed guarantees?

## Priority Order

When reviewing logs, use this order:

1. startup validation events
2. audit store initialization events
3. authorization and acknowledgment decision events
4. audit lookup warnings

## Startup Events

The most important startup events are:

- `1000 ValidationStarted`
- `1001 AuditStoreOptionsResolved`
- `1002 ProfileCatalogLoaded`
- `1003 ValidationCompleted`
- `1004 ValidationFailed`

Operator rule:

- if `1004 ValidationFailed` appears, the service should not be treated as healthy
- if `1000` appears without `1003`, startup should be treated as incomplete

## Audit Store Events

The current audit store events are:

- `2000 InitializationStarted`
- `2001 DirectoryReady`
- `2002 InitializationCompleted`

Operator rule:

- `2002` should appear during healthy startup
- if startup reaches `1003` but audit store events are absent, treat that as suspicious and verify configuration directly

## Authorization Events

The key authorization events are:

- `3000 ReplayReturned`
- `3001 DecisionRecorded`
- `3002 ProfileRoleRejected`
- `3003 CallerRoleRejected`

Interpretation:

- `3001` means a decision reached durable audit storage
- `3000` means request idempotency returned the canonical earlier record
- `3002` means the active profile configuration itself is incompatible with the requested boundary role
- `3003` means the caller sent the wrong role

Operator rule:

- repeated `3002` is a configuration problem
- repeated `3003` is a caller integration problem

## Acknowledgment Events

The key acknowledgment events are:

- `3100 DecisionNotFound`
- `3101 DecisionDisappeared`
- `3102 InvalidStateRejected`
- `3103 AcknowledgmentRecorded`
- `3104 ProfileRoleRejected`
- `3105 CallerRoleRejected`

Interpretation:

- `3103` is the only healthy bounded acknowledgment success signal
- `3102` means the decision was not in an acknowledgment-eligible state
- `3104` and `3105` should be treated the same way as the authorization-side role failures

Operator rule:

- do not interpret `3102` as generic failure of the service
- do interpret repeated `3104` as a profile configuration issue

## Audit Query Events

The current audit query events are:

- `3200 LookupReturned`
- `3201 LookupNotFound`

Operator rule:

- `3201` is not automatically an incident
- `3201` is normal for unknown request ids or decision ids
- treat it as a problem only when callers expect a durable record that should already exist

## Useful Fields

Preserve these fields in log pipelines when present:

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
- `DataSource`
- `SchemaVersion`
- `CurrentVersion`

## Simple Triage Rules

Use this compact decision rule:

- startup issue:
  look for `1004`
- deployment/config issue:
  look for missing `1003` or missing `2002`
- caller integration issue:
  look for `3003` or `3105`
- profile configuration issue:
  look for `3002` or `3104`
- expected not-found lookup:
  `3201` without any missing-decision claim from the caller

## Source Anchors

For the exact event ids and field contract, see:

- [observability-contract.md](/C:/work/clearance-gate/docs/observability-contract.md)
- [operations-runbook.md](/C:/work/clearance-gate/docs/operations-runbook.md)
