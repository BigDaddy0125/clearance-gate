# Post-Pilot Review Flow

This flow starts after a pilot evidence package has been captured.

It is the minimum review path from pilot evidence to an explicit next-step decision.

## Goal

Use one evidence package to answer:

1. did the pilot stay inside the current authorization-boundary definition
2. did the pilot satisfy the acceptance criteria
3. what work, if any, is explicitly approved next

## Inputs

The review starts from one packaged pilot evidence directory produced by:

- [package-pilot-evidence.ps1](/C:/work/clearance-gate/scripts/package-pilot-evidence.ps1)

The package should already contain:

- release bundle manifest
- release-readiness summary
- compact audit response
- export audit response
- operator and pilot execution guides

## Review Steps

1. Prepare a review directory with [prepare-post-pilot-review.ps1](/C:/work/clearance-gate/scripts/prepare-post-pilot-review.ps1).
2. Read the generated `decision-memo-draft.md` and confirm the `requestId`, `decisionId`, `evidenceId`, final `outcome`, and final `clearanceState`.
3. Review the pilot against [pilot-acceptance-checklist.md](/C:/work/clearance-gate/docs/pilot-acceptance-checklist.md).
4. Finish the generated memo draft using [post-pilot-decision-memo.md](/C:/work/clearance-gate/docs/post-pilot-decision-memo.md) as the source template.
5. Compare any approved next work against [v1-backlog.md](/C:/work/clearance-gate/docs/v1-backlog.md).

## Decision Rule

The review should end with one explicit outcome:

- continue with the current boundary shape
- continue with tighter operational hardening
- continue with one narrow `v1` expansion
- pause and reduce scope
- redefine the product scope explicitly

If the evidence points toward workflow routing, hidden profile selection, hidden authority substitution, or new outcome types, treat that as scope redefinition.

## Operator Rule

Do not skip from evidence package to backlog discussion.

The acceptance checklist and the post-pilot memo are the required bridge.
