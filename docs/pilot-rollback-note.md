# Pilot Rollback Note

This note defines the narrow rollback rule for the current controlled pilot.

It is not a generic disaster recovery document.

## Rollback Trigger

Rollback is required when:

- startup validation cannot pass cleanly
- the smoke-check fails
- audit evidence cannot be reconstructed
- a caller path attempts to continue after `BLOCK` or `DEGRADE`
- profile or role mismatches indicate the deployed pilot is not the intended one

## Rollback Actions

Perform these steps in order:

1. stop accepting new pilot traffic
2. preserve the SQLite audit files
3. preserve the current logs and current release bundle commit
4. preserve the latest pilot evidence package and post-pilot review directory if present
5. revert to the last known-good bundle and configuration, or stop the pilot entirely

## Do Not

Do not:

- disable startup validation
- change profile semantics in place
- reinterpret `BLOCK` or `DEGRADE` as proceedable
- discard the current audit database before preserving it

## Minimum Rollback Evidence

Keep all of the following:

- release bundle commit
- active profile id
- current deployment configuration reference
- SQLite audit database files
- latest release review directory
- latest pilot evidence package
- latest post-pilot review directory

## Restart Rule

Only resume the pilot after:

- the rollback cause is understood
- the replacement bundle passes the current release readiness gates
- the dry-run checklist completes successfully again
