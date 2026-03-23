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
- `docs/profile-lifecycle.md` profile naming, versioning, and fail-closed lifecycle rules
- `tla/` machine-checkable models and scenario configs
- `src/` executable service and libraries
- `docs/claim-traceability.md` claim-to-formal/runtime/test mapping
- `scripts/check-claim-traceability.ps1` checklist completeness guard

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
