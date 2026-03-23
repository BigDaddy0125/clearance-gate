# Profile Lifecycle

This document defines the current lifecycle rules for embedded ClearanceGate profiles.

The goal is not to build a general workflow catalog.

The goal is to keep profile identity, compatibility, and startup behavior fail-closed.

## Scope

A profile in ClearanceGate currently defines:

- a canonical profile identifier
- a human-readable description
- responsibility roles
- supported kernel-projected constraints

Profiles do not define new authorization outcomes.

Profiles do not weaken kernel fail-closed behavior.

## Identity Rule

Every profile name must use canonical versioned identity:

- `<family>_v<positive integer>`

Examples:

- `itops_deployment_v1`
- `kernel_boundary_v12`

Non-examples:

- `itops_deployment`
- `itops_deployment_v0`
- `ItOps_Deployment_v1`
- `itops-deployment-v1`

The parser and validator live in:

- [ProfileVersionIdentity.cs](/C:/work/clearance-gate/src/ClearanceGate.Profiles/ProfileVersionIdentity.cs)
- [ProfileCatalogValidator.cs](/C:/work/clearance-gate/src/ClearanceGate.Profiles/ProfileCatalogValidator.cs)

## Catalog Rules

At startup, the embedded profile catalog must reject:

- missing profile identifiers
- non-canonical profile names
- duplicate `family/version` pairs
- empty descriptions
- missing required kernel roles
- unsupported constraint kinds
- unsupported required fields
- `ack_required` constraints without `whenRiskFlagPresent`
- duplicate constraint ids

These failures are startup failures, not runtime warnings.

## Current Compatibility Rule

Current request handling requires an explicit profile identifier in the request.

ClearanceGate does not yet auto-resolve "latest profile version" for callers.

This is intentional:

- callers must declare the exact profile version they depend on
- startup must fail if the embedded catalog is malformed
- unknown profile names remain request-time fail-closed

## Runtime Enforcement Link

Profile lifecycle rules currently connect to runtime in these places:

- startup validation in [StartupValidation.cs](/C:/work/clearance-gate/src/ClearanceGate.Api/StartupValidation.cs)
- embedded catalog loading in [EmbeddedProfileCatalog.cs](/C:/work/clearance-gate/src/ClearanceGate.Profiles/EmbeddedProfileCatalog.cs)
- profile projection in [ProfilePolicyProjector.cs](/C:/work/clearance-gate/src/ClearanceGate.Policy/ProfilePolicyProjector.cs)
- authorize/acknowledge role enforcement in [AuthorizationService.cs](/C:/work/clearance-gate/src/ClearanceGate.Application/Services/AuthorizationService.cs) and [AcknowledgmentService.cs](/C:/work/clearance-gate/src/ClearanceGate.Application/Services/AcknowledgmentService.cs)

## Test Anchors

Current lifecycle tests live in:

- [ProfileLifecycleTests.cs](/C:/work/clearance-gate/tests/ClearanceGate.Api.Tests/ProfileLifecycleTests.cs)
- [StartupFailureTests.cs](/C:/work/clearance-gate/tests/ClearanceGate.Api.Tests/StartupFailureTests.cs)

These tests cover both pure validator behavior and startup fail-closed behavior.

## Next Step

The next lifecycle step should be small and explicit:

- add a read-only latest-version resolver for operations and diagnostics

Current status:

- read-only latest-version diagnostics now exist in the embedded catalog and API
- callers still must send an explicit profile id on authorization requests
- diagnostics do not change runtime selection semantics

That resolver should not change the current request rule that callers send an explicit profile id.
