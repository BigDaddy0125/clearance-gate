# clearance-gate

ClearanceGate is a formal authorization system for high-risk execution boundaries.

This repository is for the full product, not the marketing site.

Current repository goals:

- define the product architecture
- define verifiable security and authorization claims
- maintain TLA+ models for core guarantees
- build the executable authorization service

Initial focus:

- authorization kernel first
- evidence and audit second
- one narrow profile and one real pilot path third

Repository layout:

- `docs/` product, architecture, and formal verification strategy
- `docs/operations-runbook.md` startup, schema, and verification runbook
- `docs/observability-contract.md` stable event ids and structured logging contract
- `docs/operator-logging-guide.md` operator-focused guide for triaging current boundary events
- `docs/operator-triage-cheatsheet.md` shortest operator triage reference for startup, caller, profile, and lookup issues
- `docs/profile-lifecycle.md` profile naming, versioning, and fail-closed lifecycle rules
- `docs/release-readiness.md` current release gate checklist
- `docs/v0-scope.md` v0 pilot scope and completion definition
- `docs/api-examples.md` pilot-facing request, response, and diagnostics examples
- `docs/deployment-runbook.md` concrete deployment and smoke-flow steps
- `docs/pilot-acceptance-checklist.md` pilot entry, success, and stop criteria
- `docs/pilot-execution-checklist.md` operator checklist for the actual pilot execution window
- `docs/pilot-dry-run-checklist.md` final rehearsal checklist before the first controlled pilot rollout
- `docs/pilot-rollback-note.md` narrow rollback rule for the controlled pilot
- `docs/pilot-sample-session.md` one complete step-by-step pilot session
- `docs/pilot-incident-response.md` minimum response guide when the pilot boundary appears unhealthy
- `docs/pilot-evidence-package.md` minimum archival package for a pilot session
- `docs/pilot-adapter-example.md` one narrow caller-side mapping example for the current pilot
- `docs/pilot-adapter-checklist.md` checklist for adopting the maintained pilot adapter pattern
- `docs/caller-integration-rehearsal.md` live rehearsal path for the maintained caller adapter against a real ClearanceGate instance
- `docs/caller-onboarding-checklist.md` minimum checklist for systems that call ClearanceGate
- `docs/post-v0-backlog.md` deferred and next-phase work after the current pilot boundary
- `docs/v1-backlog.md` smallest safe next-version candidates after the pilot
- `docs/post-pilot-review-flow.md` minimum review path from pilot evidence to an explicit next-step decision
- `docs/post-pilot-decision-memo.md` template for deciding what happens after the pilot
- embedded profiles currently include `itops_deployment_v1` and `incident_mitigation_v1`
- `examples/deployment/appsettings.Production.example.json` minimal deployment config shape
- `examples/deployment/appsettings.Pilot.example.json` pilot-oriented deployment config shape
- `examples/deployment/appsettings.LocalValidation.example.json` local validation config shape
- `examples/operations/operator-log-sample.jsonl` sample structured log lines for operator triage and review handoff
- `examples/pilot-adapter/` narrow caller-side mapping samples
- `tla/` machine-checkable models and scenario configs
- `src/` executable service and libraries
- `docs/claim-traceability.md` claim-to-formal/runtime/test mapping
- `scripts/check-claim-traceability.ps1` checklist completeness guard
- `scripts/check-delivery-handoff.ps1` repository-backed completeness check for delivery, pilot, and review handoff assets
- `scripts/run-deployment-smoke-check.ps1` bounded deployment smoke flow
- `scripts/prepare-pilot-rollout.ps1` prepares the final controlled-pilot handoff directory from the latest release review
- `scripts/run-controlled-pilot-dry-run.ps1` executes one full local controlled-pilot rehearsal and records the resulting artifact chain
- `scripts/capture-pilot-sample-session.ps1` runs the sample path with unique ids and packages the resulting evidence
- `scripts/package-pilot-evidence.ps1` packages bundle metadata and pilot evidence into a reviewable archive directory
- `scripts/prepare-post-pilot-review.ps1` turns a pilot evidence package into a focused post-pilot review directory
- `scripts/initialize-post-pilot-decision-memo.ps1` generates a decision memo draft from the latest prepared review
- `scripts/prepare-release-review.ps1` turns the current release bundle into a focused pre-pilot review directory
- `scripts/check-controlled-pilot-readiness.ps1` repository-backed completeness check for the final controlled-pilot assets
- `scripts/validate-pilot-adapter-example.ps1` verifies the maintained pilot adapter conversion stays deterministic
- `scripts/prepare-caller-integration-review.ps1` prepares a focused caller-side integration handoff directory
- `scripts/check-caller-integration-handoff.ps1` repository-backed completeness check for the pilot adapter handoff assets
- `scripts/run-caller-integration-rehearsal.ps1` runs the maintained caller adapter against a live local ClearanceGate instance and captures the resulting evidence
- `scripts/publish-release-bundle.ps1` repeatable local publish bundle for pilot delivery
- `scripts/validate-release-bundle.ps1` bundle completeness check for local and CI use

Important constraints:

- ClearanceGate does not execute actions
- ClearanceGate does not recommend actions
- ClearanceGate decides only whether execution is authorized now
- fail-open behavior is prohibited
- non-blocking outcomes require audit evidence

Current environment note:

- `dotnet` SDK and Java are expected locally for runtime claim tests and TLC runs
- use `powershell -ExecutionPolicy Bypass -File .\scripts\run-tlc.ps1 -IncludeRed` to run the formal regression suite on Windows

Operational note:

- startup performs fail-closed validation of embedded profiles and the audit store schema before serving requests
- unsupported audit schema versions or invalid profile catalogs must stop the process rather than degrade open
- read-only profile diagnostics are available without changing the rule that authorization requests must specify an explicit profile id
