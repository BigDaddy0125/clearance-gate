# Pilot Adapter Checklist

Use this checklist when a real caller wants to adopt the current adapter pattern.

It is narrower than general caller onboarding.

It assumes the caller already accepts ClearanceGate as an authorization boundary.

## Preconditions

Confirm all of the following:

- the caller has completed [caller-onboarding-checklist.md](/C:/work/clearance-gate/docs/caller-onboarding-checklist.md)
- the caller uses one explicit profile
- the caller accepts deterministic request mapping only
- the caller does not require implicit authority or workflow routing

## Authorization Mapping Checks

Confirm the adapter:

- maps a caller request to one explicit `requestId`
- maps a caller execution identifier to one explicit `decisionId`
- passes an explicit `profile`
- preserves `action`, `responsibility`, and `metadata`
- maps caller risk indicators to explicit `riskFlags`

## Acknowledgment Mapping Checks

Confirm the adapter:

- preserves the original `decisionId`
- sends one explicit acknowledger id
- sends role `acknowledging_authority`
- records one explicit acknowledgment timestamp

## Forbidden Adapter Behavior

The adapter must not:

- choose a profile implicitly
- convert blocked conditions into acknowledgment-eligible ones
- suppress required fields the active profile expects
- auto-acknowledge on behalf of a human authority
- widen outcomes beyond `PROCEED`, `BLOCK`, `REQUIRE_ACK`, `DEGRADE`

## Verification Steps

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-pilot-adapter-example.ps1
```

And confirm:

- the authorize mapping matches the maintained example output
- the acknowledge mapping matches the maintained example output
- the caller integration review directory can be prepared

## Handoff Rule

The adapter is acceptable for pilot use only if:

- mapping remains deterministic
- profile remains explicit
- the validation script stays green
- the caller integration review directory is complete
