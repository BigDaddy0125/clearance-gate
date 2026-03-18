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

- planned

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
