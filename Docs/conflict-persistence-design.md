# Conflict Persistence Design

## Purpose

This document records the first durable conflict model after in-memory conflict classification.

The goal is to persist unresolved conflict and namespace collision state without executing endpoint mutations.

## Scope

The first persistence increment stores enough information to resume after restart and show blocked work later in the UI.

Included:

- sync root id.
- tracked item id.
- diff kind.
- decision kind.
- conflict state.
- concise message.
- first observed time.
- last observed time.
- resolved time.

Not included:

- user resolution workflow.
- automatic conflict resolution policy.
- operation queue persistence.
- endpoint mutation retry records.
- historical conflict audit trail.

## Conflict States

Initial states:

- `Open`
- `Resolved`

Open conflicts are active blockers. Resolved conflicts are retained only long enough for later cleanup policy.

## Table Shape

Table name:

- `SYNC_CONFLICT`

Fields:

- `Id`
- `SyncRootId`
- `TrackedItemId`
- `DiffKind`
- `DecisionKind`
- `State`
- `Message`
- `FirstObservedTime`
- `LastObservedTime`
- `ResolvedTime`

The first implementation keys active conflicts by `TrackedItemId`. Namespace collision groups create one row per affected tracked item, using the same bounded message shape exposed by run summaries.

`SYNC_CONFLICT` is also registered as a Tripous metadata module named `SyncConflict`.

## Write Rules

When planning produces `SyncPlanDecisionKind.Conflict` or `SyncPlanDecisionKind.Blocked`:

- upsert an open conflict row.
- update `LastObservedTime`.
- do not create a normal execution intent.

When the same tracked item later plans as a non-conflict, non-blocked decision:

- mark the open conflict row as resolved.
- allow normal planning to continue.

## Read Rules

Run summaries should continue to be computed from the current run result.

Durable reads should be separate:

- list open conflicts for a sync root.
- count open conflicts for service status.
- list open conflict details with tracked item, base snapshot, local observation, and remote observation context.
- eventually provide conflict details to desktop UI.

`MetadataSyncRunResult.OpenConflictCount` exposes the durable open conflict count after a completed run.

## Transaction Boundary

Conflict upserts happen in the same metadata planning side-effect transaction as metadata-only base snapshot commits.

Conflict resolution caused by a later clean plan happens in the same metadata planning side-effect transaction.
