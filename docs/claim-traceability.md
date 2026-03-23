# ClearanceGate Claim Traceability Checklist

This document is the review checklist for claim completeness.

A claim is considered phase-complete only when it has:

- a written statement in `docs/security-claims.md`
- a formal green and red path
- a runtime enforcement anchor
- an executable test anchor
- a CI execution path

Status values:

- `COMPLETE`: all expected anchors exist and are wired
- `PARTIAL`: claim exists, but one or more anchors are missing

## Checklist

| Claim | Status | Formal | Runtime | Tests | CI |
| --- | --- | --- | --- | --- | --- |
| CG1 | COMPLETE | `ClearanceKernel.tla`, `kernel_ok.cfg`, `kernel_negative_fail_open.cfg` | `src/ClearanceGate.Kernel/ClearanceKernel.cs` | `KernelClaimsTests.EveryKnownState_MapsToExactlyOneDefinedOutcome` | `scripts/run-tlc.ps1`, `.github/workflows/verification.yml` |
| CG2 | COMPLETE | `ClearanceKernel.tla`, `ClearanceKernel_BadFailOpen.tla`, `kernel_ok.cfg`, `kernel_negative_fail_open.cfg` | `src/ClearanceGate.Kernel/ClearanceKernel.cs` | `KernelClaimsTests.DegradedAndInsufficientStates_NeverProceed`, `AuthorizationClaimsTests.BlockedDecision_CannotBeReleasedByAcknowledgment` | `scripts/run-tlc.ps1`, `.github/workflows/verification.yml` |
| CG3 | COMPLETE | `AcknowledgmentBounded.tla`, `AcknowledgmentBounded_BadUniversalAck.tla`, `ack_bounded_ok.cfg`, `ack_bounded_negative_universal_ack.cfg` | `src/ClearanceGate.Audit/SqliteDecisionAuditStore.cs`, `src/ClearanceGate.Application/Services/AcknowledgmentService.cs` | `AuthorizationClaimsTests.RiskFlaggedDecision_RequiresAck_ThenAuditShowsAuthorizedAfterAck`, `AuthorizationClaimsTests.BlockedDecision_CannotBeReleasedByAcknowledgment`, `SqliteDecisionAuditStoreTests.SaveAcknowledgment_ReplayDoesNotAppendDuplicateTimeline`, `SqliteDecisionAuditStoreTests.SaveAcknowledgment_InvalidStateDoesNotMutateStoredRecord` | `scripts/run-tlc.ps1`, `.github/workflows/verification.yml` |
| CG4 | COMPLETE | `DurableEvidenceGate.tla`, `DurableEvidenceGate_BadEarlyEmit.tla`, `durable_evidence_ok.cfg`, `durable_evidence_negative_early_emit.cfg` | `src/ClearanceGate.Application/Services/AuthorizationService.cs`, `src/ClearanceGate.Audit/SqliteDecisionAuditStore.cs`, `src/ClearanceGate.Application/Services/AuditQueryService.cs` | `AuthorizationClaimsTests.NonBlockingOutcome_ImmediatelyExposesDurableEvidenceInAudit`, `AuthorizationClaimsTests.AuditEvidence_RemainsReadableAfterApplicationRestart`, `AuthorizationClaimsTests.AuditExport_ReturnsReconstructableDecisionEnvelope`, `AuthorizationClaimsTests.AuditExport_AfterAcknowledgmentReflectsAuthorizedState`, `AuthorizationClaimsTests.AuditRecordAndExport_AgreeForSameDecision`, `AuditResponseConsistencyTests.CompactAndExportViews_AgreeAcrossDecisionAndRequestEndpoints_BeforeAcknowledgment`, `AuditResponseConsistencyTests.CompactAndExportViews_AgreeAcrossDecisionAndRequestEndpoints_AfterAcknowledgment`, `AuditResponseConsistencyTests.AuditViews_ReturnStableOrderingAcrossRepeatedReads` | `scripts/run-tlc.ps1`, `.github/workflows/verification.yml` |
| CG5 | COMPLETE | `RequestIdempotency.tla`, `RequestIdempotency_BadOverwrite.tla`, `idempotency_ok.cfg`, `idempotency_negative_overwrite.cfg` | `src/ClearanceGate.Application/Services/AuthorizationService.cs`, `src/ClearanceGate.Audit/SqliteDecisionAuditStore.cs`, `src/ClearanceGate.Application/Services/AuditQueryService.cs` | `AuthorizationClaimsTests.SameRequestId_RemainsIdempotentAcrossConcurrentRequests`, `AuthorizationClaimsTests.AuditByRequestId_ResolvesCanonicalDecisionAndExport`, `SqliteDecisionAuditStoreTests.SaveAuthorization_RequestConflictReturnsCanonicalRecordWithoutDuplicateChildRows`, `AuditResponseConsistencyTests.CompactAndExportViews_AgreeAcrossDecisionAndRequestEndpoints_BeforeAcknowledgment`, `AuditResponseConsistencyTests.CompactAndExportViews_AgreeAcrossDecisionAndRequestEndpoints_AfterAcknowledgment` | `scripts/run-tlc.ps1`, `.github/workflows/verification.yml` |
| CG6 | COMPLETE | `ProfileConformance.tla`, `ProfileConformance_BadImplicitAllow.tla`, `ProfileRoleConformance.tla`, `ProfileRoleConformance_BadRoleBypass.tla`, generated configs, negative cfg models | `src/ClearanceGate.Profiles/EmbeddedProfileCatalog.cs`, `src/ClearanceGate.Profiles/ProfileCatalogValidator.cs`, `src/ClearanceGate.Profiles/ProfileVersionIdentity.cs`, `src/ClearanceGate.Policy/ProfilePolicyProjector.cs`, `src/ClearanceGate.Policy/ItOpsDeploymentPolicyEvaluator.cs`, `src/ClearanceGate.Application/Services/AuthorizationService.cs`, `src/ClearanceGate.Application/Services/AcknowledgmentService.cs` | `AuthorizationClaimsTests.UnknownProfile_IsRejectedFailClosed`, `AuthorizationClaimsTests.AuthorizationRole_MustMatchProfileRequirement`, `AuthorizationClaimsTests.AcknowledgmentRole_MustMatchProfileRequirement`, `AuthorizationClaimsTests.MissingSourceSystem_MapsToProfileRequiredFieldConstraint`, `AuthorizationClaimsTests.SecondEmbeddedProfile_ProjectsItsOwnRiskConstraint`, `ProfileLifecycleTests.*`, `ProfileDiagnosticsTests.*`, `StartupFailureTests.ApplicationStartup_RejectsInvalidProfileIdentity`, `StartupFailureTests.ApplicationStartup_RejectsDuplicateProfileFamilyVersion` | `scripts/run-tlc.ps1`, `.github/workflows/verification.yml` |

## Review Notes

- `CG1` and `CG2` now have direct kernel runtime tests, not just formal coverage.
- `CG4` now has an immediate audit reconstruction test, not just restart-based durability coverage.
- `CG5` now includes both API-level idempotency tests and store-level duplicate-write suppression tests.
- `CG6` includes both profile outcome/evidence conformance and role boundary conformance, plus profile lifecycle validator and startup-failure anchors.
- audit read models now also have dedicated consistency tests across compact/export and decision/request lookup paths.

## Maintenance Rule

When a new claim is added or an existing claim is changed:

1. Update `docs/security-claims.md`.
2. Update this checklist.
3. Add or update formal green/red coverage.
4. Add or update runtime claim tests.
5. Ensure CI still executes the relevant formal and runtime paths.
