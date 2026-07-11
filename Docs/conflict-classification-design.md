# Conflict Classification Design

## Purpose

This document records the current Hermes conflict classification rules before UI conflict resolution or operation queues are added.

The classifier describes state. It does not execute synchronization operations.

## Inputs

The classifier compares:

- base state.
- latest local observation.
- latest remote observation.

Remote namespace compatibility may also use a provider-neutral projected local path:

- `ProjectedLocalRelativePath`

This value is computed before classification by projecting the remote parent identity into the local namespace.

## Compatible Changes

The following states are compatible:

- both sides changed content to the same state.
- both sides renamed to the same name in the same parent.
- both sides moved to the same projected local path.
- both sides renamed and moved to the same projected local path.
- local missing plus explicit remote tombstone.
- local missing plus remote trash.

Compatible changes plan as:

- `SyncDiffKind.BothChangedCompatible`
- `SyncPlanDecisionKind.CommitBase`

They can advance the base snapshot without endpoint mutation.

## Conflicts

The following states are conflicts:

- local content changed and remote content changed differently.
- local rename and remote rename differ.
- local move and remote move differ.
- local rename plus move and remote rename-only to the same name.
- local delete versus remote content change.
- local delete versus remote rename.
- local delete versus remote move.
- remote missing versus local content change.
- remote missing versus local rename.
- remote tombstone versus local content change.
- remote trash versus local content change.
- folder delete tombstone versus modified local descendant.
- matching projected namespace but differing content.

Conflicts plan as:

- `SyncDiffKind.Conflict`
- `SyncPlanDecisionKind.Conflict`

Conflict execution requests validate as conflict results and are not passed to normal mutation execution.

Conflict decisions are also persisted as open durable conflicts by the metadata planning side-effect step.

## Namespace Collisions

Google Drive allows duplicate sibling names. A local filesystem mirror cannot materialize two active siblings at the same local path.

Duplicate remote siblings are classified as:

- `SyncDiffKind.NamespaceCollision`
- `SyncPlanDecisionKind.Blocked`

Namespace collisions are detected in:

- full remote snapshot planning.
- incremental remote changes planning.

Blocked namespace collision requests validate as blocked results and are not passed to normal mutation execution.

Blocked namespace collision decisions are also persisted as open durable conflicts by the metadata planning side-effect step.

## Conservative Missing State

Local missing plus remote missing without an explicit provider removal signal is not treated as compatible deletion.

The classifier keeps this as:

- `SyncDiffKind.LocalMissing`

This avoids committing a missing base snapshot without a clear remote tombstone or trash observation.

## Run Summaries

Run summaries expose:

- pending execution decision counts.
- pending diff counts.
- namespace collision groups.
- blocked item samples.
- uncommitted conflict result counts.
- bounded conflict messages.
