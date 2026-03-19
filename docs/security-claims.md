# ClearanceGate Security And Authorization Claims

This document is the claim inventory for formal verification.

Each claim should map to:

- one TLA+ spec or harness
- one or more TLC model configs
- at least one negative scenario

## Claims

### CG1: Outcome exclusivity

Statement:

- a decision instance has exactly one active clearance state
- exactly one authorization outcome applies

Spec:

- `tla/specs/ClearanceKernel.tla`

Models:

- `tla/models/kernel_ok.cfg`
- `tla/models/kernel_negative_fail_open.cfg`

### CG2: Fail-closed under uncertainty

Statement:

- no degraded or insufficient state may authorize execution

Spec:

- `tla/specs/ClearanceKernel.tla`
- `tla/specs/ClearanceKernel_BadFailOpen.tla`

Models:

- `tla/models/kernel_ok.cfg`
- `tla/models/kernel_negative_fail_open.cfg`

### CG3: Acknowledgment is bounded

Statement:

- acknowledgment cannot override non-overridable constraints or degraded state

Spec:

- `tla/specs/AcknowledgmentBounded.tla`
- `tla/specs/AcknowledgmentBounded_BadUniversalAck.tla`

Models:

- `tla/models/ack_bounded_ok.cfg`
- `tla/models/ack_bounded_negative_universal_ack.cfg`

Executable coverage:

- `tests/ClearanceGate.Api.Tests/AuthorizationClaimsTests.cs`
- runtime enforcement in `src/ClearanceGate.Audit/SqliteDecisionAuditStore.cs`

### CG4: Non-blocking outcomes require evidence

Statement:

- `PROCEED` and `REQUIRE_ACK` are reachable only if evidence is durable

Spec:

- `tla/specs/DurableEvidenceGate.tla`
- `tla/specs/DurableEvidenceGate_BadEarlyEmit.tla`

Models:

- `tla/models/durable_evidence_ok.cfg`
- `tla/models/durable_evidence_negative_early_emit.cfg`

Executable coverage:

- `tests/ClearanceGate.Api.Tests/AuthorizationClaimsTests.cs`
- durable persistence in `src/ClearanceGate.Audit/SqliteDecisionAuditStore.cs`

### CG5: Request replay is idempotent

Statement:

- the same `request_id` does not create divergent outcomes or duplicate durable evidence

Spec:

- `tla/specs/RequestIdempotency.tla`
- `tla/specs/RequestIdempotency_BadOverwrite.tla`

Models:

- `tla/models/idempotency_ok.cfg`
- `tla/models/idempotency_negative_overwrite.cfg`

Executable coverage:

- `tests/ClearanceGate.Api.Tests/AuthorizationClaimsTests.cs`
- runtime enforcement in `src/ClearanceGate.Audit/SqliteDecisionAuditStore.cs`

### CG6: Profiles cannot weaken kernel invariants

Statement:

- a profile may define schema, constraints, and responsibility roles
- a profile may not add new outcomes or bypass audit and fail-closed rules

Spec:

- `tla/specs/ProfileConformance.tla`
- `tla/specs/ProfileConformance_BadImplicitAllow.tla`

Models:

- generated from `src/ClearanceGate.Profiles/itops_deployment_v1.json` via `scripts/generate-profile-tla-config.ps1`
- `tla/models/profile_conformance_negative_implicit_allow.cfg`

Runtime anchors:

- `src/ClearanceGate.Profiles/itops_deployment_v1.json`
- `src/ClearanceGate.Policy/ItOpsDeploymentPolicyEvaluator.cs`
