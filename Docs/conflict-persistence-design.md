# Conflict Persistence Design

## Purpose

This document records the intended first durable conflict model after in-memory conflict classification.

The goal is to persist unresolved conflict and namespace collision state without executing endpoint mutations.

## Scope

The first persistence increment should store enough information to resume after restart and show blocked work later in the UI.

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

Draft table name:

- `SYNC_CONFLICT`

Draft fields:

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

The first implementation can key active conflicts by `TrackedItemId`. Namespace collision groups may create one row per affected tracked item, using the same bounded message shape exposed by run summaries.

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
- eventually provide conflict details to desktop UI.

## Transaction Boundary

Conflict upserts should happen in the same transaction as the observations and planning result that produced them.

Conflict resolution caused by a later clean plan should happen in the same transaction as that later classification pass.
