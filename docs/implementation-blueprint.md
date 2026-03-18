# ClearanceGate Implementation Blueprint

## Product Scope

This repository is for the executable product.

The product is:

- an authorization boundary
- an evidence generator
- a profile-driven control plane

The product is not:

- a workflow engine
- a recommendation engine
- a risk scoring platform
- an execution orchestrator

## First Version Architecture

The first version should contain these modules.

### 1. API host

Responsibilities:

- expose `POST /authorize`
- expose `POST /acknowledge`
- expose `GET /audit/{decisionId}`
- authenticate callers
- translate HTTP requests into application commands

### 2. Application layer

Responsibilities:

- authorize requests
- process acknowledgments
- load profiles and rule sets
- coordinate audit persistence

### 3. Kernel

Responsibilities:

- state machine
- outcome mapping
- invariant-safe transitions

This must remain small and stable.

### 4. Policy engine

Responsibilities:

- evaluate declared constraints
- identify non-overridable conditions
- identify acknowledgment requirements

### 5. Audit and evidence layer

Responsibilities:

- persist durable evidence
- reconstruct decision timelines
- enforce "no non-blocking outcome without evidence"

### 6. Profile layer

Responsibilities:

- versioned profile definitions
- input schema metadata
- responsibility role metadata

## First Implementation Slice

The first slice should target one profile:

- `itops_deployment_v1`

Why this profile first:

- clear execution boundary
- easier pilot path than finance or robotics
- good fit for shadow mode and blocking mode

## Repository Layout

- `src/ClearanceGate.Api/`
- `src/ClearanceGate.Application/`
- `src/ClearanceGate.Kernel/`
- `src/ClearanceGate.Policy/`
- `src/ClearanceGate.Audit/`
- `src/ClearanceGate.Contracts/`
- `src/ClearanceGate.Profiles/`
- `docs/`
- `tla/`

## Delivery Phases

### Phase 0

- kernel model
- claim inventory
- repository skeleton

### Phase 1

- request and response contracts
- minimal API host
- in-memory profile and audit implementations

### Phase 2

- durable Postgres-backed audit store
- idempotent request handling
- acknowledgment flow

### Phase 3

- one real pilot adapter
- shadow mode
- audit replay pack
