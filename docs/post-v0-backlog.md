# Post-V0 Backlog

This document separates work that is likely valuable after the current `v0` pilot from work that should remain out of scope unless the product definition changes.

Its purpose is simple:

- protect the current authorization-boundary scope
- make future expansion explicit instead of accidental

## How To Read This Backlog

Items here are not automatically approved.

They are only candidates for work after the `v0` pilot proves useful.

The order reflects scope safety, not business priority alone.

## Candidate V1 Work

### 1. Deployment Hardening

Safe next-step work:

- package the API host for more repeatable environment deployment
- add environment-specific configuration examples
- refine database file placement and backup guidance
- add more explicit operational logging guidance

Why it fits:

- improves operability without changing decision semantics

### 2. Pilot Adapters

Safe next-step work:

- add one narrow adapter or integration path for the pilot domain
- provide sample caller integration patterns
- add request mapping examples from external systems into the current API

Why it fits:

- improves adoption while preserving the boundary role of the service

### 3. Profile Expansion

Safe next-step work:

- add one additional embedded profile family
- refine profile metadata and compatibility documentation
- add more explicit lifecycle checks for multiple versions within a family

Why it fits:

- extends the current profile-driven model without changing the kernel role

### 4. Observability Hardening

Safe next-step work:

- structured logs for startup, authorization, and acknowledgment paths
- more explicit diagnostics for audit store state
- release and pilot summaries that are easier to inspect over time

Why it fits:

- improves operability and review without weakening fail-closed behavior

### 5. Test And Formal Depth

Safe next-step work:

- expand store-level and contract-level tests
- add more generated constants to formal configs
- tighten claim dashboards and CI summaries

Why it fits:

- increases assurance without widening product scope

## Explicitly Deferred Work

The following are not part of the current post-`v0` default path and should require a deliberate scope decision:

- workflow routing
- generalized approval inboxes
- execution orchestration
- recommendations or decision suggestions
- automatic caller-side profile substitution
- broad UI console or portal-first productization
- multi-tenant control plane design
- generalized marketplace for policies or profiles

## Escalation Rule

If future work would:

- hide the explicit `profile` requirement
- dilute fail-closed startup behavior
- add new decision outcomes
- turn the service into a workflow coordinator
- move authority from explicit inputs into hidden heuristics

then that work is not a normal backlog item.

It is a product-scope change and must be treated that way.

## Recommended Sequence After V0

The safest sequence after the pilot is:

1. deployment hardening
2. one narrow pilot adapter
3. one additional profile only if the first pilot justifies it
4. observability and reporting refinements
5. broader scope decisions only after boundary value is proven
