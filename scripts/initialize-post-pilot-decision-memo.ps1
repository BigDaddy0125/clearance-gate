[CmdletBinding()]
param(
    [string]$ReviewRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent

if ([string]::IsNullOrWhiteSpace($ReviewRoot)) {
    $postPilotReviewRoot = Join-Path $repoRoot "artifacts\post-pilot-review"
    if (-not (Test-Path $postPilotReviewRoot)) {
        throw "Post-pilot review root is missing at '$postPilotReviewRoot'."
    }

    $latestReview = Get-ChildItem -Path $postPilotReviewRoot -Directory |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $latestReview) {
        throw "No post-pilot review directories were found under '$postPilotReviewRoot'."
    }

    $resolvedReviewRoot = $latestReview.FullName
}
else {
    $resolvedReviewRoot = $ReviewRoot
}

$reviewRoot = [System.IO.Path]::GetFullPath($resolvedReviewRoot)
$reviewManifestPath = Join-Path $reviewRoot "review-manifest.json"

if (-not (Test-Path $reviewManifestPath)) {
    throw "Review manifest is missing at '$reviewManifestPath'."
}

$reviewManifest = Get-Content -Raw -Path $reviewManifestPath | ConvertFrom-Json
$decisionMemoDraftPath = Join-Path $reviewRoot "decision-memo-draft.md"

$draftLines = @(
    "# Post-Pilot Decision Memo Draft",
    "",
    'Generated from `review-manifest.json`.',
    "",
    "Use this draft with [post-pilot-decision-memo.md](/C:/work/clearance-gate/docs/post-pilot-decision-memo.md).",
    "",
    "## Snapshot",
    "",
    ("- Pilot name:"),
    ("- Pilot window:"),
    ("- Commit or release bundle: " + [string]$reviewManifest.releaseBundleCommit),
    ("- Primary profile used: " + [string]$reviewManifest.profile),
    ("- Calling system:"),
    "",
    "## Evidence Anchors",
    "",
    ('- Request id: `' + [string]$reviewManifest.requestId + '`'),
    ('- Decision id: `' + [string]$reviewManifest.decisionId + '`'),
    ('- Evidence id: `' + [string]$reviewManifest.evidenceId + '`'),
    ('- Final outcome: `' + [string]$reviewManifest.finalOutcome + '`'),
    ('- Final clearance state: `' + [string]$reviewManifest.finalClearanceState + '`'),
    ('- Review root: `' + $reviewRoot + '`'),
    "",
    "## What Happened",
    "",
    "- which actions were gated:",
    '- how often `PROCEED`, `BLOCK`, `REQUIRE_ACK`, and `DEGRADE` appeared:',
    "- whether acknowledgment remained bounded and understandable:",
    "- whether audit/export evidence was sufficient for review:",
    "",
    "## What Worked",
    "",
    "- explicit profile use remained workable:",
    "- request idempotency behaved as expected:",
    "- audit replay by request id was useful:",
    "- diagnostics were sufficient for operators:",
    "",
    "## What Hurt",
    "",
    "- deployment handoff friction:",
    "- too much manual request mapping:",
    "- insufficient log visibility:",
    "- profile lifecycle confusion:",
    "",
    "## Boundary Check",
    "",
    "1. Did ClearanceGate remain an authorization boundary rather than becoming a workflow coordinator?",
    "2. Did any pilot request require hidden heuristics or implicit profile selection?",
    "3. Did any pilot request require new decision outcomes?",
    "4. Did any pilot request push toward UI-first productization instead of API-first boundary use?",
    "",
    "## Decision",
    "",
    "- continue with the current boundary shape",
    "- continue with tighter operational hardening",
    '- continue with one narrow `v1` expansion',
    "- pause and reduce scope",
    "- redefine the product scope explicitly",
    "",
    "## Approved Next Work",
    "",
    "- approved item 1:",
    "- approved item 2:",
    "- approved item 3:",
    "",
    "## Explicitly Rejected Work",
    "",
    "- rejected item 1:",
    "- rejected item 2:",
    "",
    "## Review Inputs",
    "",
    "- [pilot-acceptance-checklist.md](docs/pilot-acceptance-checklist.md)",
    "- [post-pilot-review-flow.md](docs/post-pilot-review-flow.md)",
    "- [v1-backlog.md](docs/v1-backlog.md)",
    '- `evidence/release-readiness-summary.md`',
    '- `evidence/audit-compact.json`',
    '- `evidence/audit-export.json`'
)

Set-Content -Path $decisionMemoDraftPath -Value $draftLines

Write-Host ("Initialized decision memo draft at " + $decisionMemoDraftPath)
