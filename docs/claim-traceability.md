# ClearanceGate Claim Traceability

This document maps each claim to:

- the formal specification and TLC models
- the runtime enforcement anchor
- the executable integration test anchor
- the CI execution path

The goal is to keep claims auditable and reduce drift between docs, models, and implementation.

## CG1: Outcome exclusivity

Formal:

- `tla/specs/ClearanceKernel.tla`
- `tla/models/kernel_ok.cfg`
- `tla/models/kernel_negative_fail_open.cfg`

Runtime anchors:

- `src/ClearanceGate.Kernel/ClearanceKernel.cs`

Executable anchors:

- `tests/ClearanceGate.Api.Tests/KernelClaimsTests.cs`
  `EveryKnownState_MapsToExactlyOneDefinedOutcome`

CI path:

- `scripts/run-tlc.ps1`
- `.github/workflows/verification.yml`

## CG2: Fail-closed under uncertainty

Formal:

- `tla/specs/ClearanceKernel.tla`
- `tla/specs/ClearanceKernel_BadFailOpen.tla`
- `tla/models/kernel_ok.cfg`
- `tla/models/kernel_negative_fail_open.cfg`

Runtime anchors:

- `src/ClearanceGate.Kernel/ClearanceKernel.cs`

Executable anchors:

- `tests/ClearanceGate.Api.Tests/KernelClaimsTests.cs`
  `DegradedAndInsufficientStates_NeverProceed`
- `tests/ClearanceGate.Api.Tests/AuthorizationClaimsTests.cs`
  `BlockedDecision_CannotBeReleasedByAcknowledgment`

CI path:

- `scripts/run-tlc.ps1`
- `.github/workflows/verification.yml`

## CG3: Acknowledgment is bounded

Formal:

- `tla/specs/AcknowledgmentBounded.tla`
- `tla/specs/AcknowledgmentBounded_BadUniversalAck.tla`
- `tla/models/ack_bounded_ok.cfg`
- `tla/models/ack_bounded_negative_universal_ack.cfg`

Runtime anchors:

- `src/ClearanceGate.Audit/SqliteDecisionAuditStore.cs`
- `src/ClearanceGate.Application/Services/AcknowledgmentService.cs`

Executable anchors:

- `tests/ClearanceGate.Api.Tests/AuthorizationClaimsTests.cs`
  `RiskFlaggedDecision_RequiresAck_ThenAuditShowsAuthorizedAfterAck`
- `tests/ClearanceGate.Api.Tests/AuthorizationClaimsTests.cs`
  `BlockedDecision_CannotBeReleasedByAcknowledgment`

CI path:

- `scripts/run-tlc.ps1`
- `.github/workflows/verification.yml`

## CG4: Non-blocking outcomes require evidence

Formal:

- `tla/specs/DurableEvidenceGate.tla`
- `tla/specs/DurableEvidenceGate_BadEarlyEmit.tla`
- `tla/models/durable_evidence_ok.cfg`
- `tla/models/durable_evidence_negative_early_emit.cfg`

Runtime anchors:

- `src/ClearanceGate.Application/Services/AuthorizationService.cs`
- `src/ClearanceGate.Audit/SqliteDecisionAuditStore.cs`

Executable anchors:

- `tests/ClearanceGate.Api.Tests/AuthorizationClaimsTests.cs`
  `AuditEvidence_RemainsReadableAfterApplicationRestart`

CI path:

- `scripts/run-tlc.ps1`
- `.github/workflows/verification.yml`

## CG5: Request replay is idempotent

Formal:

- `tla/specs/RequestIdempotency.tla`
- `tla/specs/RequestIdempotency_BadOverwrite.tla`
- `tla/models/idempotency_ok.cfg`
- `tla/models/idempotency_negative_overwrite.cfg`

Runtime anchors:

- `src/ClearanceGate.Application/Services/AuthorizationService.cs`
- `src/ClearanceGate.Audit/SqliteDecisionAuditStore.cs`

Executable anchors:

- `tests/ClearanceGate.Api.Tests/AuthorizationClaimsTests.cs`
  `SameRequestId_RemainsIdempotentAcrossConcurrentRequests`

CI path:

- `scripts/run-tlc.ps1`
- `.github/workflows/verification.yml`

## CG6: Profiles cannot weaken kernel invariants

Formal:

- `tla/specs/ProfileConformance.tla`
- `tla/specs/ProfileConformance_BadImplicitAllow.tla`
- `tla/specs/ProfileRoleConformance.tla`
- `tla/specs/ProfileRoleConformance_BadRoleBypass.tla`
- generated configs via `scripts/generate-profile-tla-config.ps1`
- `tla/models/profile_conformance_negative_implicit_allow.cfg`
- `tla/models/profile_role_conformance_negative_role_bypass.cfg`

Runtime anchors:

- `src/ClearanceGate.Profiles/EmbeddedProfileCatalog.cs`
- `src/ClearanceGate.Policy/ProfilePolicyProjector.cs`
- `src/ClearanceGate.Policy/ItOpsDeploymentPolicyEvaluator.cs`
- `src/ClearanceGate.Application/Services/AuthorizationService.cs`
- `src/ClearanceGate.Application/Services/AcknowledgmentService.cs`

Executable anchors:

- `tests/ClearanceGate.Api.Tests/AuthorizationClaimsTests.cs`
  `UnknownProfile_IsRejectedFailClosed`
- `tests/ClearanceGate.Api.Tests/AuthorizationClaimsTests.cs`
  `AuthorizationRole_MustMatchProfileRequirement`
- `tests/ClearanceGate.Api.Tests/AuthorizationClaimsTests.cs`
  `AcknowledgmentRole_MustMatchProfileRequirement`
- `tests/ClearanceGate.Api.Tests/AuthorizationClaimsTests.cs`
  `MissingSourceSystem_MapsToProfileRequiredFieldConstraint`

CI path:

- `scripts/run-tlc.ps1`
- `.github/workflows/verification.yml`
