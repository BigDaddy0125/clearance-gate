# Pilot Evidence Package

This document defines the smallest acceptable evidence package for a pilot session.

It is not a generic export format.

It is the current archival package for the `v0` pilot boundary.

## Purpose

The package should let a reviewer answer three questions:

1. which build was running
2. which boundary decision was exercised
3. whether the resulting audit evidence is reconstructable

## Required Contents

Each pilot evidence package should contain:

- the release bundle manifest
- the release-readiness summary
- the compact audit response
- the export audit response
- the authorize response
- the acknowledge response when acknowledgment occurred
- the profile diagnostics response used for the session
- the pilot execution checklist
- the pilot incident response guide
- the operator logging guide

## Minimum Identifiers

The evidence manifest should include:

- release bundle commit
- active profile id
- `requestId`
- `decisionId`
- `evidenceId`
- final `outcome`
- final `clearanceState`

## Packaging Rule

Use [package-pilot-evidence.ps1](/C:/work/clearance-gate/scripts/package-pilot-evidence.ps1) to build the package directory.

The package is acceptable only if:

- the release bundle validates
- the release-readiness summary exists
- compact and export audit responses are both present
- the compact and export responses agree on `decisionId` and `evidenceId`

## Operator Rule

Do not discard raw session responses once the package is built.

The package is the review bundle.

The raw files remain the operator backup source.
