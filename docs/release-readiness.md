# Release Readiness

This checklist defines the current minimum release gate for ClearanceGate.

It does not certify business readiness.

It certifies that the current authorization boundary, formal suite, and startup safeguards are intact.

## Required Gates

Release is blocked unless all of the following are green:

- startup validation remains fail-closed
- claim traceability resolves
- runtime claim tests pass
- TLC green models pass
- TLC red models fail as expected
- profile diagnostics remain read-only and do not change request semantics

## Startup Gate

Verify:

- embedded profiles load successfully
- profile names use canonical `<family>_v<positive integer>` identity
- audit store configuration is valid
- audit schema is supported or migratable
- unsupported future schema versions stop startup

Primary anchors:

- [StartupValidation.cs](/C:/work/clearance-gate/src/ClearanceGate.Api/StartupValidation.cs)
- [operations-runbook.md](/C:/work/clearance-gate/docs/operations-runbook.md)
- [StartupFailureTests.cs](/C:/work/clearance-gate/tests/ClearanceGate.Api.Tests/StartupFailureTests.cs)
- [AuditStoreSchemaTests.cs](/C:/work/clearance-gate/tests/ClearanceGate.Api.Tests/AuditStoreSchemaTests.cs)

## Claim Gate

Verify:

- [security-claims.md](/C:/work/clearance-gate/docs/security-claims.md) and [claim-traceability.md](/C:/work/clearance-gate/docs/claim-traceability.md) are current
- `scripts/check-claim-traceability.ps1` passes

## Delivery Handoff Gate

Verify:

- `scripts/check-delivery-handoff.ps1` passes
- operator, deployment, pilot, and review handoff assets remain present
- README entry points remain current for those assets

## Controlled Pilot Gate

Verify:

- `scripts/check-controlled-pilot-readiness.ps1` passes
- the dry-run checklist is current
- the rollback note is current
- the pilot rollout preparation script is present and usable
- `scripts/run-controlled-pilot-dry-run.ps1` is present and usable

## Caller Integration Gate

Verify:

- `scripts/check-caller-integration-handoff.ps1` passes
- `scripts/check-real-caller-intake-handoff.ps1` passes
- `scripts/initialize-real-caller-intake.ps1` is present and usable
- `scripts/validate-pilot-adapter-example.ps1` passes
- `scripts/validate-real-caller-rehearsal-input.ps1` is present and usable for caller-owned payloads
- `scripts/prepare-caller-integration-review.ps1` succeeds
- `scripts/prepare-real-caller-rehearsal.ps1` is present and usable
- `scripts/run-caller-integration-rehearsal.ps1` is present and usable

## Runtime Gate

Verify:

- `dotnet test .\tests\ClearanceGate.Api.Tests\ClearanceGate.Api.Tests.csproj --configuration Release` passes
- audit compact/export parity remains stable
- request idempotency remains stable
- profile lifecycle and startup failures remain fail-closed

## Formal Gate

Verify:

- `powershell -ExecutionPolicy Bypass -File .\scripts\run-tlc.ps1 -IncludeRed` passes
- expected-failure models still fail for the intended bad variants

## Diagnostics Gate

Verify:

- `GET /profiles` returns the embedded catalog view
- `GET /profiles/latest/{family}` resolves the latest embedded version for a known family
- authorization requests still require explicit `profile`

Primary anchors:

- [profile-lifecycle.md](/C:/work/clearance-gate/docs/profile-lifecycle.md)
- [ProfileDiagnosticsTests.cs](/C:/work/clearance-gate/tests/ClearanceGate.Api.Tests/ProfileDiagnosticsTests.cs)

## CI Gate

The GitHub Actions workflow should publish:

- traceability pass/fail
- controlled pilot readiness pass/fail
- caller integration handoff pass/fail
- real caller intake handoff pass/fail
- runtime claim summary
- TLC summary
- release bundle status, commit, and embedded profile list
- caller integration review artifacts
- real caller rehearsal artifacts
- pilot-execution support anchors
- uploaded runtime and TLC artifacts

Primary anchor:

- [verification.yml](/C:/work/clearance-gate/.github/workflows/verification.yml)

## Bundle Gate

Verify:

- `powershell -ExecutionPolicy Bypass -File .\scripts\publish-release-bundle.ps1` succeeds
- `powershell -ExecutionPolicy Bypass -File .\scripts\validate-release-bundle.ps1` succeeds
- the release bundle includes `bundle-manifest.json`
- the bundle includes deployment docs and all deployment config examples
- the bundle includes operator triage docs and sample logs
- `powershell -ExecutionPolicy Bypass -File .\scripts\prepare-release-review.ps1` succeeds

## Pilot Execution Gate

Verify:

- [run-deployment-smoke-check.ps1](/C:/work/clearance-gate/scripts/run-deployment-smoke-check.ps1) is present and usable
- [operator-logging-guide.md](/C:/work/clearance-gate/docs/operator-logging-guide.md) is current
- [caller-onboarding-checklist.md](/C:/work/clearance-gate/docs/caller-onboarding-checklist.md) is current
- [pilot-execution-checklist.md](/C:/work/clearance-gate/docs/pilot-execution-checklist.md) is current
- [pilot-incident-response.md](/C:/work/clearance-gate/docs/pilot-incident-response.md) is current

## Pilot Evidence Gate

Verify:

- [pilot-evidence-package.md](/C:/work/clearance-gate/docs/pilot-evidence-package.md) is current
- [package-pilot-evidence.ps1](/C:/work/clearance-gate/scripts/package-pilot-evidence.ps1) is present and usable
- [capture-pilot-sample-session.ps1](/C:/work/clearance-gate/scripts/capture-pilot-sample-session.ps1) is present and usable
- the release bundle includes the pilot evidence guide

## Minimal Approval Rule

A release candidate is acceptable only when:

- no startup gate is bypassed
- no claim is downgraded from `COMPLETE`
- no expected-failure formal model turns green
- no runtime test that covers `CG1` to `CG6` regresses
