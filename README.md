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
- `tla/` machine-checkable models and scenario configs
- `src/` executable service and libraries

Important constraints:

- ClearanceGate does not execute actions
- ClearanceGate does not recommend actions
- ClearanceGate decides only whether execution is authorized now
- fail-open behavior is prohibited
- non-blocking outcomes require audit evidence

Current environment note:

- `dotnet` SDK and Java are expected locally for runtime claim tests and TLC runs
- use `powershell -ExecutionPolicy Bypass -File .\scripts\run-tlc.ps1 -IncludeRed` to run the formal regression suite on Windows
