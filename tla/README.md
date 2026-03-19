# ClearanceGate TLA+

This directory contains machine-checkable models for ClearanceGate.

Modeling rules:

- keep each spec narrow
- define one clear claim per harness when possible
- maintain a green model and a red model
- treat this as a regression suite

Initial model set:

- kernel outcome exclusivity
- fail-closed behavior
- acknowledgment boundedness
- durable evidence before non-blocking outcome
- request idempotency
- profile conformance to kernel invariants
