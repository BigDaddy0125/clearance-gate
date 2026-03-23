# V1 Backlog

This document turns the post-`v0` backlog into a narrower `v1` candidate list.

It does not mean all items are approved.

It means these are the items that can be discussed without redefining ClearanceGate into a different product.

## Purpose

`v1` should answer one question:

- what is the smallest next version that increases pilot usefulness without weakening the authorization-boundary model?

## V1 Entry Rule

No `v1` planning should start unless:

- the `v0` pilot is complete
- the pilot acceptance checklist has been reviewed
- the pilot outcome has been written down in a post-pilot decision memo

## Candidate V1 Themes

### 1. Deployment Packaging

Candidate work:

- refine the release bundle for more repeatable operator handoff
- add environment-specific publish examples
- make bundle validation part of standard release review

Why this belongs in `v1`:

- it improves delivery without changing decision semantics

### 2. Narrow Domain Adapter

Candidate work:

- turn one pilot adapter example into a maintained adapter pack
- add caller-specific request mapping guidance for one real integration path
- add adapter smoke examples that preserve explicit profile usage

Why this belongs in `v1`:

- it improves adoption without making ClearanceGate an orchestration hub

### 3. Additional Profile Family Maturity

Candidate work:

- deepen lifecycle docs for multi-family embedded profiles
- add one more version within an existing family only if compatibility is explicit
- keep all profile additions inside current supported constraint kinds

Why this belongs in `v1`:

- it expands useful coverage while preserving kernel invariants

### 4. Operational Observability

Candidate work:

- preserve the current structured log contract across releases
- add a small operator-focused logging guide
- tighten release summaries so pilot review is faster

Why this belongs in `v1`:

- it improves operability without widening product scope

### 5. Formal And Runtime Depth

Candidate work:

- extend generated formal constants for embedded profiles
- add more contract-level runtime tests where drift risk remains
- tighten release and traceability reporting

Why this belongs in `v1`:

- it increases assurance without changing product shape

## V1 Non-Goals

The following are not normal `v1` items:

- workflow routing
- generic approval inboxes
- hidden profile selection
- automatic decision suggestions
- broad UI-first productization
- multi-tenant control plane design
- generalized plugin ecosystem

If any of these become necessary, that is not simple backlog growth.

It is a product-definition change.

## Prioritization Rule

When choosing between candidate `v1` items, prefer work that:

1. preserves explicit `profile` selection
2. preserves fail-closed behavior
3. preserves durable evidence reconstruction
4. improves operability or adoption without adding new decision outcomes

## Exit Condition

`v1` planning is acceptable only when the final list still describes:

- an authorization boundary service
- not a workflow engine
- not a recommendation system
- not an execution orchestrator
