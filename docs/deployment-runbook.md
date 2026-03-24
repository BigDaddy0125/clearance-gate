# Deployment Runbook

This runbook turns the current `v0` boundary into concrete deployment steps.

It is intentionally narrow and aligned to the current primary pilot profile shape.

## Deployment Goal

Deploy one ClearanceGate instance that:

- starts fail-closed
- uses a configured SQLite audit store
- exposes the current API surface
- passes the existing runtime and formal gates before release

## Preconditions

Before deployment, confirm:

- the target artifact is built from a commit that passed the current verification workflow
- the embedded profile catalog is unchanged or intentionally updated
- the audit store path is known
- the host has a writable directory for the SQLite database files

## Required Inputs

At minimum, set:

- application working directory
- `ConnectionStrings__AuditStore`

This setting is required. Startup should fail if it is missing or blank.

Reference example:

- [appsettings.Production.example.json](/C:/work/clearance-gate/examples/deployment/appsettings.Production.example.json)
- [appsettings.Pilot.example.json](/C:/work/clearance-gate/examples/deployment/appsettings.Pilot.example.json)
- [appsettings.LocalValidation.example.json](/C:/work/clearance-gate/examples/deployment/appsettings.LocalValidation.example.json)

Example value:

```text
ConnectionStrings__AuditStore=Data Source=C:\clearancegate-data\clearancegate.db
```

## Local Deployment Steps

1. Restore and build:

```powershell
dotnet restore .\ClearanceGate.slnx
dotnet build .\ClearanceGate.slnx --configuration Release
```

2. Run verification gates:

```powershell
dotnet test .\tests\ClearanceGate.Api.Tests\ClearanceGate.Api.Tests.csproj --configuration Release
powershell -ExecutionPolicy Bypass -File .\scripts\check-claim-traceability.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\run-tlc.ps1 -IncludeRed
```

3. Publish a repeatable bundle:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-release-bundle.ps1
```

This produces:

- `artifacts/publish/app`
- `artifacts/publish/bundle-manifest.json`
- copied deployment/runbook docs
- deployment config examples

4. Start the API from the bundle:

```powershell
$env:ConnectionStrings__AuditStore = "Data Source=C:\clearancegate-data\clearancegate.db"
.\artifacts\publish\app\ClearanceGate.Api.exe
```

If you publish for a non-Windows portable target, run the generated dll with `dotnet`.

## First-Start Checks

On first start, confirm:

- startup does not fail
- the database file is created at the configured path
- `/profiles` returns the embedded catalog
- `/profiles/latest/itops_deployment` returns `itops_deployment_v1`

Useful checks:

```powershell
Invoke-RestMethod -Method Get -Uri http://localhost:5000/profiles
Invoke-RestMethod -Method Get -Uri http://localhost:5000/profiles/latest/itops_deployment
```

Recommended operational note:

- place the SQLite file under a dedicated writable directory such as `C:\clearancegate-data`
- keep that directory outside the repo working tree
- treat the database file as pilot evidence and back it up before any upgrade or reset

## Pilot Smoke Flow

Run one bounded smoke sequence:

1. authorize the risk example from [authorize-risk.json](/C:/work/clearance-gate/examples/v0/authorize-risk.json)
2. confirm `REQUIRE_ACK`
3. acknowledge via [acknowledge-risk.json](/C:/work/clearance-gate/examples/v0/acknowledge-risk.json)
4. confirm compact audit and export views

Reference:

- [api-examples.md](/C:/work/clearance-gate/docs/api-examples.md)
- [run-deployment-smoke-check.ps1](/C:/work/clearance-gate/scripts/run-deployment-smoke-check.ps1)
- [package-pilot-evidence.ps1](/C:/work/clearance-gate/scripts/package-pilot-evidence.ps1)
- [publish-release-bundle.ps1](/C:/work/clearance-gate/scripts/publish-release-bundle.ps1)
- [pilot-execution-checklist.md](/C:/work/clearance-gate/docs/pilot-execution-checklist.md)

Example:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-deployment-smoke-check.ps1
```

## Smoke-Check to Evidence Rule

Do not treat the smoke-check as complete just because the script returns success.

Also preserve:

- the returned `requestId`
- the returned `decisionId`
- the returned `evidenceId`
- the compact audit view
- the export audit view

Use the smoke-check as a bridge from deployment verification to durable evidence verification.

## Pilot Evidence Packaging

After a successful smoke-check or pilot session, package the resulting evidence for review:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-pilot-evidence.ps1
```

This produces a timestamped directory under `artifacts/pilot-evidence` containing:

- release bundle manifest
- release-readiness summary
- compact audit response
- export audit response
- authorize and acknowledge responses
- profile diagnostics response
- operator and pilot execution guides

Reference:

- [pilot-evidence-package.md](/C:/work/clearance-gate/docs/pilot-evidence-package.md)

## Release Review Preparation

To prepare a focused release review directory from the current bundle:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\prepare-release-review.ps1
```

This produces a timestamped directory under `artifacts/release-review` containing:

- the bundle manifest
- the release-readiness summary
- deployment and operator docs
- all deployment config examples

Use this as the pre-pilot review handoff directory.

## Failure Handling

If startup fails:

- do not bypass the failure
- inspect whether the failure is profile-related, config-related, or schema-related
- correct the boundary condition and restart

If runtime smoke flow fails:

- stop the pilot rollout
- preserve the database file and logs
- preserve the smoke-check request and resulting identifiers
- confirm claim traceability and runtime tests locally before retrying

## Upgrade Rule

For `v0`, upgrades must remain conservative:

- do not silently change the audit store path
- back up the SQLite audit file before replacing binaries or changing configuration
- do not auto-select newer profile versions for callers
- do not skip startup validation

## Exit Condition

Deployment is acceptable only if:

- startup succeeds
- diagnostics succeed
- smoke flow succeeds
- release-readiness checklist remains green
