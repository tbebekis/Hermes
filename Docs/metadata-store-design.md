# Metadata Store Design

## Purpose

This document is a version 0 draft for the Hermes metadata store.

It records the synchronization concepts, invariants, identities, and first schema direction. It is not a final database schema and should stay open while Google Drive exploration continues.

The Metadata Store is the authoritative synchronization memory for Hermes. It is not an authoritative copy of the local filesystem or Google Drive.

## Product Goal

Hermes targets a reliable two-way mirror between a local directory tree and an existing Google Drive tree.

Current product direction:

- Full read/write mirror of a selected Drive tree.
- Broad Google Drive access is the development assumption.
- A restricted `drive.file` mode may be supported later for explicitly selected files or folders.

The sync engine must not depend on OAuth scope details. Core logic should see only provider capabilities and the configured synchronization root.

## Sync Root

A sync root is the boundary of one synchronization relationship.

```text
Local root directory
        <=>
Provider account
        <=>
Remote root item or folder
```

All metadata belongs to a specific sync root.

A sync root allows Hermes to support:

- entire My Drive.
- a selected Drive folder.
- a folder selected through a restricted access flow.
- multiple independent local-to-remote relationships.
- other providers later.

The remote checkpoint is scoped to a provider account and sync root. It is not a global application singleton.

## Core State Model

The Metadata Store records observations from both synchronization endpoints together with the last committed common state.

- Base state.
- Observed local state.
- Observed remote state.

The filesystem and the remote provider are the real data sources. The Metadata Store records the latest observations Hermes made of those sources.

```text
Filesystem
        ->
Observation
        ->
Metadata Store
```

```text
Google Drive
        ->
Observation
        ->
Metadata Store
```

The base state is the last state Hermes confirmed was successfully synchronized on both sides. The observed states are the latest local and remote readings. Observed state is not automatically trusted as a synchronization decision.

The sync planner should compare:

```text
Base State
Observed Local State
Observed Remote State
```

This keeps planning state-based. It avoids trying to replay exact operation history from Google Drive changes, because the Changes API reports changed identities and current state, not every intermediate user action.

## Invariants

The synchronization model depends on these invariants:

- A tracked item belongs to exactly one sync root.
- Remote provider item id is immutable for the lifetime of a remote item.
- Paths are mutable metadata.
- The base snapshot changes only after successful synchronization and verification.
- Observed snapshots never modify the base snapshot directly.
- Metadata changes and checkpoint advancement are atomic.
- The Metadata Store is the authoritative synchronization memory.
- Provider-native models do not cross into Core metadata or planner contracts.

## Identities

Hermes needs its own internal tracked-item identity.

Remote identity:

- Google Drive `File.Id`.
- Stable across rename, move, content update, trash, and restore.
- Missing after permanent delete except as the `FileId` in a tombstone change.
- Independent from name and parent path.
- Multiple remote items may share the same parent id and name.

Local identity:

- Current first version may use normalized relative path plus local file attributes.
- A stronger local identity may be added later if Linux inode/device tracking is useful.

Path:

- Important metadata.
- Not the primary identity.
- Can change through rename or move.
- Not guaranteed to be unique on the remote side.
- Should be derivable from hierarchy when possible.

Hierarchy:

- Should be represented by parent identity.
- Ancestor rename can change derived local paths for descendants without producing remote changes for the descendant items themselves.
- Ancestor move can change derived local paths for descendants without producing remote changes for the descendant items themselves.
- Ancestor trash can propagate provider trashed state to descendants and may produce descendant changes.
- Ancestor restore can propagate provider trashed state to descendants and may produce descendant changes.

## Tracked Item

A tracked item represents one logical identity known by Hermes.

Minimum concepts:

- Hermes internal id.
- Sync root id.
- Remote provider item id.
- Local identity or local key.

Tracked item should stay small. Names, paths, sizes, hashes, timestamps, existence state, and provider metadata belong in snapshots, not in the tracked item itself.

## Base Snapshot

The base snapshot is the last confirmed common state.

The base snapshot is immutable until a synchronization plan has completed successfully and its result has been verified. Starting an upload, download, move, rename, or delete operation does not change the base snapshot.

The base snapshot changes only when Hermes has confirmed that both endpoints now share the same intended state. This rule is required for safe recovery after operation failure or process crash.

It should contain sync-relevant metadata, for example:

- Item type.
- Parent identity.
- Name.
- Relative path.
- Size.
- MD5 hash or provider checksum.
- Created time.
- Modified time.
- Provider version.
- Trashed flag.
- Existence flag.

For a file, content comparison should prefer strong content metadata such as MD5 when available. Google Drive `Version` is useful as a change signal, but it is not a content-only version.

For folders, `Version` can change when hierarchy changes while `ModifiedTime` remains unchanged. `ModifiedTime` alone is not enough to detect remote hierarchy changes.

## State Transitions

The basic synchronization flow is:

```text
Observed Local
Observed Remote
        ->
Three-way Diff
        ->
Sync Plan
        ->
Execute
        ->
Verify
        ->
Commit Base Snapshot
```

The base snapshot is committed only at the final step.

## Observed Local Snapshot

The observed local snapshot is produced by the latest local scan.

Likely fields:

- Relative path.
- Item type.
- Size.
- Modified time.
- Local content hash when calculated.
- Existence flag.

Observed missing is only an observation. If the local scanner does not find an item, Hermes records `Exists = false` for that observation. The planner later decides whether that means delete propagation, conflict, recovery, or another action.

Open question:

- Whether Hermes should calculate hashes during every scan or only when size/time indicate possible content change.

## Observed Remote Snapshot

The observed remote snapshot is produced by a full remote listing or by applying Changes API results.

Likely fields:

- Remote item id.
- Parent remote id.
- Name.
- Item type.
- MIME type.
- Size.
- MD5 hash.
- Created time.
- Modified time.
- Provider version.
- Trashed flag.
- Last provider change time when available.
- Existence flag.

Changes API tombstones may have only:

- Remote item id.
- Removed flag.
- Change time.

The model must not synthesize a fake item when item metadata is unavailable.

Observed remote missing is only an observation. A Google Drive tombstone or a failed fetch does not by itself decide the synchronization operation.

## Tombstones

A tombstone preserves the identity history of an item that disappeared from one side.

Tombstones are needed to distinguish:

- delete propagation.
- delete vs modify conflict.
- permanent remote delete where metadata can no longer be fetched.
- local delete before the corresponding remote delete is confirmed.

A tombstone does not preserve the file itself. It preserves the knowledge that an identity existed, when removal was observed, what the last known common state was, and whether deletion has been reconciled on both sides.

Provider trash is not the same as a permanent-delete tombstone. Trash is a soft-delete state while the provider item identity and metadata may still be fetchable.

A remote permanent delete should preserve at least:

- Sync root id.
- Remote item id.
- Previous tracked item id when known.
- Change time when available.
- Previous base snapshot reference or copied summary metadata.
- Reconciliation state.

## Remote Checkpoint

The remote checkpoint stores the provider cursor for incremental remote sync.

Minimum identity:

- Provider.
- Account or connection id.
- Sync root id.
- Remote root item id.
- Start page token.

Checkpoint update rule:

- Process all returned change pages successfully.
- Persist observed remote state and tombstones atomically with the new start page token.
- Never commit the new token before the corresponding changes are stored.

Invalid checkpoint rule:

- A provider may reject an old, malformed, or expired checkpoint.
- This must become a provider-neutral checkpoint-invalid condition.
- Recovery should be explicit, probably full remote rescan plus a fresh start page token.
- Hermes must not silently skip from an invalid checkpoint to a new checkpoint without reconciling possible missed changes.

## Consistency Model

The first consistency rule is:

```text
metadata
+
checkpoint
```

are committed together, or neither is committed.

This applies to remote change processing. A crash must not leave Hermes with a new checkpoint that skips changes whose metadata was not stored.

The same principle should later apply to operation execution records and base snapshot commits. Hermes should be able to restart and decide whether an operation was not started, is pending verification, or was completed and committed.

## Pending Operations

Pending operations are not part of the first schema implementation, but the model should leave room for them.

Examples:

- Create remote folder.
- Upload file.
- Download file.
- Update content.
- Rename.
- Move.
- Trash or delete.
- Restore when supported.

Pending operation persistence will matter for crash recovery and retry behavior.

## Conflicts

Conflicts should be represented explicitly, but the first metadata store increment does not need full conflict tables.

Expected conflict classes:

- local modify vs remote modify.
- delete vs modify.
- rename collision.
- duplicate name collision under the same parent.
- local move vs remote move.
- ancestor rename affecting local descendant paths.

Duplicate remote names are a special mirror problem. Google Drive may allow two remote siblings with the same name, while the local filesystem cannot represent both with the same relative path. The planner needs a collision policy before it can materialize those items locally.

Google Drive also allows renaming an existing item to the same name as siblings under the same parent. This is reported as a normal update for the same remote item id, not as a provider error.

## Namespace Mapping

Remote namespace and local namespace are not identical.

```text
Remote Name / Parent
        ->
Namespace Mapping Policy
        ->
Resolved Local Relative Path
```

Google Drive allows multiple siblings with the same parent id and name. A local filesystem directory cannot represent those items with the exact same path.

Initial v1 collision policy:

- Detect namespace collision.
- Do not overwrite local files.
- Do not silently rename remote items.
- Do not automatically invent disambiguated local names as the default behavior.
- Create an explicit conflict.
- Pause operations affecting the colliding namespace.
- Require user resolution.

Automatic deterministic local disambiguation may be added later as a configurable policy, but it adds round-trip complexity because the disambiguated local name is not the remote name.

## Schema Proposal V0

This is a draft, not a final database schema. It describes the first model shape that must be able to represent the exploration findings.

### `SYNC_ROOT`

Purpose:

- Represents one independent synchronization relationship.
- Scopes local root, provider connection, remote root, and checkpoint.

Identity:

- Primary key: `Id`.

Minimum fields:

- `Id`
- `ProviderName`
- `ConnectionId`
- `LocalRootPath`
- `RemoteRootItemId`
- `IsEnabled`
- `CreatedTime`

Invariants:

- One sync root maps one local root to one provider account and remote root.
- Checkpoints belong to a sync root.
- Tracked items belong to exactly one sync root.

Nullable fields:

- `RemoteRootItemId` may be empty only before remote root selection is completed.
- `ConnectionId` may be empty only before account identification is implemented.

Lifecycle:

- Created when the user configures a synchronization root.
- Updated when root settings change.
- Disabled rather than deleted when the user pauses synchronization.

Updated by:

- Application settings or setup workflow.

Allowed changes:

- `IsEnabled`.
- root configuration while no sync is running.

### `TRACKED_ITEM`

Purpose:

- Represents the identity of one logical item tracked by Hermes.
- Stays small and identity-oriented.

Identity:

- Primary key: `Id`.
- Foreign key: `SyncRootId` -> `SYNC_ROOT.Id`.

Minimum fields:

- `Id`
- `SyncRootId`
- `RemoteItemId`
- `LocalKey`
- `ItemType`

Invariants:

- Belongs to exactly one sync root.
- `RemoteItemId` is unique only within the relevant provider account and sync root context.
- Names, paths, sizes, hashes, timestamps, trashed state, and existence state are stored in snapshots.
- There is no unique constraint on remote `(ParentId, Name)`.

Nullable fields:

- `RemoteItemId` may be empty for local-only items not uploaded yet.
- `LocalKey` may be empty for remote-only items not materialized locally yet.

Lifecycle:

- Created when Hermes first correlates or discovers a logical item.
- Retained while base or tombstone state is needed.
- Not deleted immediately when an endpoint removes the item.

Updated by:

- Correlation logic.
- Metadata store commit workflow.

Allowed changes:

- `RemoteItemId` when a local-only item is first uploaded.
- `LocalKey` when a remote-only item is first materialized locally.
- `ItemType` should not change after creation.

### `BASE_SNAPSHOT`

Purpose:

- Stores the last verified common state for a tracked item.
- Represents what Hermes knows both endpoints agreed on after successful synchronization.

Identity:

- Primary key: `TrackedItemId`.
- Foreign key: `TrackedItemId` -> `TRACKED_ITEM.Id`.

Minimum fields:

- `TrackedItemId`
- `Exists`
- `ItemType`
- `Name`
- `LocalRelativePath`
- `RemoteParentId`
- `Size`
- `ContentHash`
- `CreatedTime`
- `ModifiedTime`
- `ProviderVersion`
- `Trashed`
- `CommittedTime`

Invariants:

- Updated only after plan execution, verification, and commit.
- Never updated directly by local scanner.
- Never updated directly by remote listing or Changes API.
- `LocalRelativePath` is the committed namespace mapping result, not identity.

Nullable fields:

- `Name` may be empty when `Exists = false`.
- `LocalRelativePath` may be empty when no local materialization exists.
- `RemoteParentId` may be empty for remote root or local-only state before upload.
- `Size` may be empty for folders or provider-native documents.
- `ContentHash` may be empty for folders, Google Docs files, or un-hashed local files.
- `CreatedTime`, `ModifiedTime`, and `ProviderVersion` may be empty when the provider does not supply them.

Lifecycle:

- Created after first successful synchronization or baseline adoption.
- Replaced after successful verified sync operations.
- Preserved for conflict detection and tombstone reconciliation.

Updated by:

- Commit step after verification.

Allowed changes:

- Only as part of a base commit transaction.

### `LOCAL_OBSERVED_SNAPSHOT`

Purpose:

- Stores the latest local filesystem observation for a tracked item.
- Records what Hermes saw, not what Hermes decided.

Identity:

- Primary key: `TrackedItemId`.
- Foreign key: `TrackedItemId` -> `TRACKED_ITEM.Id`.

Minimum fields:

- `TrackedItemId`
- `Exists`
- `RelativePath`
- `Name`
- `ParentRelativePath`
- `ItemType`
- `Size`
- `ModifiedTime`
- `ContentHash`
- `ObservedTime`
- `ScanId`

Invariants:

- `Exists = false` means the scanner did not observe the item locally.
- Missing locally is not automatically a delete operation.
- Observed snapshot never mutates base snapshot directly.

Nullable fields:

- `RelativePath`, `Name`, and `ParentRelativePath` may be empty when `Exists = false`.
- `Size` is empty for folders or missing items.
- `ContentHash` may be empty when not calculated.
- `ModifiedTime` may be empty when the item is missing.

Lifecycle:

- Upserted during local scans.
- Replaced by newer observations.

Updated by:

- Local scanner.

Allowed changes:

- Any field may change during a new local observation.

### `REMOTE_OBSERVED_SNAPSHOT`

Purpose:

- Stores the latest remote provider observation for a tracked item.
- Represents full listing, Changes API item state, trash state, or permanent-delete tombstone state.

Identity:

- Primary key: `TrackedItemId`.
- Foreign key: `TrackedItemId` -> `TRACKED_ITEM.Id`.

Minimum fields:

- `TrackedItemId`
- `RemoteItemId`
- `Exists`
- `Removed`
- `Name`
- `RemoteParentId`
- `ItemType`
- `MimeType`
- `Size`
- `ContentHash`
- `CreatedTime`
- `ModifiedTime`
- `ProviderVersion`
- `Trashed`
- `ProviderChangeTime`
- `ObservedTime`

Invariants:

- `RemoteItemId` is always present when known from the provider.
- `Exists = true`, `Trashed = true`, and `Removed = false` represents provider trash.
- `Exists = false`, `Removed = true`, and item metadata empty represents permanent delete tombstone observation.
- No synthetic metadata is created for removed changes.
- There is no unique constraint on `(RemoteParentId, Name)`.

Nullable fields:

- `Name`, `RemoteParentId`, `ItemType`, `MimeType`, `Size`, `ContentHash`, `CreatedTime`, `ModifiedTime`, `ProviderVersion`, and `Trashed` may be empty for permanent-delete tombstones.
- `ContentHash` may be empty for folders and provider-native documents.
- `ProviderChangeTime` may be empty for full listings.

Lifecycle:

- Upserted during remote full listing or Changes API processing.
- Tombstone observation replaces item metadata only when the provider no longer returns the item.

Updated by:

- Remote scanner.
- Changes API processor.

Allowed changes:

- Any field may change during a new remote observation.
- Remote observation and checkpoint advancement must be atomic for Changes API processing.

### `REMOTE_CHECKPOINT`

Purpose:

- Stores the incremental remote synchronization cursor for a sync root.

Identity:

- Primary key: `SyncRootId`.
- Foreign key: `SyncRootId` -> `SYNC_ROOT.Id`.

Minimum fields:

- `SyncRootId`
- `ProviderName`
- `ConnectionId`
- `StartPageToken`
- `UpdatedTime`

Invariants:

- Unique per sync root.
- Token belongs to the provider account and remote root context.
- Advanced only after the corresponding remote observations are persisted.

Nullable fields:

- `StartPageToken` may be empty before the first remote checkpoint is acquired.

Lifecycle:

- Created when remote incremental sync is initialized.
- Updated after successful change page processing.
- Marked invalid or replaced only through explicit recovery.

Updated by:

- Remote change processor.
- Checkpoint recovery workflow.

Allowed changes:

- `StartPageToken` and `UpdatedTime` after successful atomic remote observation commit.

## Provider Error Classification

Core should not depend on provider-native exceptions such as `GoogleApiException`.

Detailed provider-neutral storage error design is tracked in `Docs/storage-error-design.md`.

Draft error kinds:

- `Unknown`
- `NotFound`
- `CheckpointInvalid`
- `PermissionDenied`
- `RateLimited`
- `TemporarilyUnavailable`
- `Conflict`
- `InvalidRequest`

Provider-neutral error information:

- kind.
- message.
- retryable flag.
- retry-after time when available.
- provider error code.
- inner exception for diagnostics.

Checkpoint invalid policy:

- Stop incremental processing.
- Mark the sync root as requiring remote rescan.
- Do not silently request a fresh token and continue.
- Decide recovery through explicit rescan/reconciliation workflow.

## Scenario Walkthroughs

The schema proposal must be validated against these scenarios before SQLite implementation.

For each scenario, record expected rows for:

- `SYNC_ROOT`
- `TRACKED_ITEM`
- `BASE_SNAPSHOT`
- `LOCAL_OBSERVED_SNAPSHOT`
- `REMOTE_OBSERVED_SNAPSHOT`
- `REMOTE_CHECKPOINT`

Scenarios:

- normal active file.
- local-only new item.
- remote-only new item.
- remote rename.
- remote folder move.
- remote folder rename with unchanged descendants.
- trashed remote item.
- restored remote item.
- permanently removed remote item.
- duplicate remote siblings.
- observed local missing.
- committed base state.

### Normal Active File

Remote file:

- `RemoteItemId`: `1mWLHUMpr7EoMm0o3nDDD_FpYcR95bTrc`
- `Name`: `DuplicateName.txt`
- `RemoteParentId`: root id
- `Size`: `3858`
- `ContentHash`: `6eb03272d4c7ff48756461a25c05f54e`
- `Trashed`: `False`

Expected rows:

- `TRACKED_ITEM`
  - `Id`: internal id.
  - `SyncRootId`: configured sync root.
  - `RemoteItemId`: `1mWLHUMpr7EoMm0o3nDDD_FpYcR95bTrc`.
  - `LocalKey`: local identity if materialized.
  - `ItemType`: file.
- `BASE_SNAPSHOT`
  - `Exists`: `True`.
  - `Name`: `DuplicateName.txt`.
  - `LocalRelativePath`: namespace mapping result.
  - `RemoteParentId`: root id.
  - `Size`: `3858`.
  - `ContentHash`: `6eb03272d4c7ff48756461a25c05f54e`.
  - `Trashed`: `False`.
- `LOCAL_OBSERVED_SNAPSHOT`
  - `Exists`: `True` when the local file is present.
  - `RelativePath`: observed local path.
  - `Size` and `ContentHash`: observed local content state.
- `REMOTE_OBSERVED_SNAPSHOT`
  - `Exists`: `True`.
  - `Removed`: `False`.
  - `RemoteItemId`: `1mWLHUMpr7EoMm0o3nDDD_FpYcR95bTrc`.
  - item metadata populated.

Schema check:

- Remote identity is independent from local path.
- Base snapshot can match both local and remote observations.

### Local-Only New Item

A local-only new item exists in the local filesystem and has no remote provider id yet.

Expected rows:

- `TRACKED_ITEM`
  - `Id`: internal id.
  - `SyncRootId`: configured sync root.
  - `RemoteItemId`: empty.
  - `LocalKey`: local identity or local path key.
  - `ItemType`: file or folder.
- `BASE_SNAPSHOT`
  - absent until the item is uploaded and verified, or present with `Exists = False` only if the item was previously tracked and removed remotely.
- `LOCAL_OBSERVED_SNAPSHOT`
  - `Exists`: `True`.
  - `RelativePath`: observed local path.
  - local metadata populated.
- `REMOTE_OBSERVED_SNAPSHOT`
  - absent, or `Exists = False` if this item is correlated with a previous remote identity that is now missing.

Schema check:

- `TRACKED_ITEM.RemoteItemId` must be nullable or empty before upload.
- The planner can classify local-only state without inventing a remote item.
- Base snapshot is not created merely because a local item was observed.

### Remote-Only New Item

A remote-only new item exists in the provider and has not been materialized locally yet.

Expected rows:

- `TRACKED_ITEM`
  - `Id`: internal id.
  - `SyncRootId`: configured sync root.
  - `RemoteItemId`: provider item id.
  - `LocalKey`: empty until local materialization.
  - `ItemType`: file or folder.
- `BASE_SNAPSHOT`
  - absent until the item is downloaded/materialized and verified, unless adopting an existing remote tree as baseline.
- `LOCAL_OBSERVED_SNAPSHOT`
  - absent, or `Exists = False` if a local observation was attempted and no mapped local path exists.
- `REMOTE_OBSERVED_SNAPSHOT`
  - `Exists`: `True`.
  - `Removed`: `False`.
  - remote metadata populated.

Schema check:

- `TRACKED_ITEM.LocalKey` must be nullable or empty before local materialization.
- The metadata store can represent provider discovery before local files exist.
- Baseline adoption must be an explicit commit decision, not a side effect of remote observation.

### Remote Rename

Remote rename changes item name while preserving remote identity.

Expected rows:

- `TRACKED_ITEM`
  - unchanged.
- `BASE_SNAPSHOT`
  - unchanged until synchronization plan is executed, verified, and committed.
- `REMOTE_OBSERVED_SNAPSHOT`
  - same `RemoteItemId`.
  - new `Name`.
  - same `RemoteParentId`.
  - updated `ModifiedTime` and `ProviderVersion` when supplied.
- `LOCAL_OBSERVED_SNAPSHOT`
  - still reflects the current local filesystem state.

Schema check:

- Planner detects remote namespace change by comparing base and remote observed snapshot.
- Base is not modified by the Changes API observation.

### Remote Folder Move

Remote folder move changes the moved folder parent id. Descendant remote identities do not change and descendants may not appear in `changes.list`.

Expected rows for moved folder:

- `TRACKED_ITEM`
  - unchanged.
- `BASE_SNAPSHOT`
  - old `RemoteParentId` until commit.
- `REMOTE_OBSERVED_SNAPSHOT`
  - same `RemoteItemId`.
  - new `RemoteParentId`.
  - `ProviderVersion` changed.
  - `ModifiedTime` may remain unchanged.

Expected rows for descendant:

- `TRACKED_ITEM`
  - unchanged.
- `BASE_SNAPSHOT`
  - unchanged.
- `REMOTE_OBSERVED_SNAPSHOT`
  - may be unchanged if no descendant change was returned.

Schema check:

- Derived local paths must be invalidated through hierarchy, not through descendant remote changes.
- Planner needs hierarchy traversal by parent identity.

### Remote Folder Rename With Unchanged Descendants

Remote folder rename changes the folder name. Descendants may not appear in `changes.list`.

Expected rows for renamed folder:

- `TRACKED_ITEM`
  - unchanged.
- `BASE_SNAPSHOT`
  - old name until commit.
- `REMOTE_OBSERVED_SNAPSHOT`
  - same `RemoteItemId`.
  - new `Name`.
  - same `RemoteParentId`.

Expected rows for descendant:

- remote item snapshot can remain unchanged.
- derived local path may still need to change because ancestor path changed.

Schema check:

- Local path must be a namespace projection, not the identity.
- Descendant path changes can be derived without descendant snapshot changes.

### Trashed Remote Item

Provider trash is soft-delete state, not permanent removal.

Expected rows:

- `TRACKED_ITEM`
  - unchanged.
- `BASE_SNAPSHOT`
  - unchanged until trash propagation is executed, verified, and committed.
- `REMOTE_OBSERVED_SNAPSHOT`
  - `Exists`: `True`.
  - `Removed`: `False`.
  - `Trashed`: `True`.
  - item metadata still populated.

Schema check:

- `Trashed` must be separate from `Removed`.
- A trashed item may still be fetchable by remote id.

### Restored Remote Item

Provider restore is a soft-delete state change back to visible state.

Expected rows:

- `TRACKED_ITEM`
  - unchanged.
- `REMOTE_OBSERVED_SNAPSHOT`
  - `Exists`: `True`.
  - `Removed`: `False`.
  - `Trashed`: `False`.
  - item metadata populated.
- `BASE_SNAPSHOT`
  - unchanged until restore is reconciled and committed.

Schema check:

- Restore is represented as state observation, not item recreation.

### Permanently Removed Remote Item

Permanent delete may return only a provider tombstone with item id.

Expected rows:

- `TRACKED_ITEM`
  - retained so the remote item id can be correlated with previous base state.
- `BASE_SNAPSHOT`
  - retained for delete-vs-modify conflict detection and reconciliation.
- `REMOTE_OBSERVED_SNAPSHOT`
  - `RemoteItemId`: deleted id.
  - `Exists`: `False`.
  - `Removed`: `True`.
  - item metadata fields empty.
  - `ProviderChangeTime`: change time when available.
- `LOCAL_OBSERVED_SNAPSHOT`
  - current local observation decides whether this is delete propagation or conflict.

Schema check:

- No synthetic item metadata is needed.
- Tombstone state can be represented before a dedicated `TOMBSTONE` table exists.

### Duplicate Remote Siblings

Google Drive can contain multiple siblings with the same parent id and name.

Expected rows:

- one `TRACKED_ITEM` per remote `File.Id`.
- one `REMOTE_OBSERVED_SNAPSHOT` per tracked item.
- same `RemoteParentId` and `Name` may appear in multiple rows.
- no uniqueness constraint on remote `(RemoteParentId, Name)`.

Schema check:

- Namespace collision is detected by projecting remote observations to local paths.
- Collision creates conflict state; it does not merge tracked items.

### Observed Local Missing

Local missing is an observation, not a planner decision.

Expected rows:

- `TRACKED_ITEM`
  - retained.
- `BASE_SNAPSHOT`
  - retained.
- `LOCAL_OBSERVED_SNAPSHOT`
  - `Exists`: `False`.
  - path and file metadata fields empty.
- `REMOTE_OBSERVED_SNAPSHOT`
  - current remote state.

Schema check:

- Planner decides whether local missing means local delete, conflict, or recovery action.

### Committed Base State

After successful execution and verification, base snapshot is updated.

Expected commit behavior:

- local and remote observations describe the verified target state.
- `BASE_SNAPSHOT` is replaced with that committed state.
- `CommittedTime` is updated.
- observations may remain as last seen state.
- checkpoint updates are committed atomically with remote observations, not with speculative operations.

Schema check:

- Base update is explicit and separate from observation writes.

## First SQLite Increment

The first implementation should wait until the schema proposal survives the scenario walkthroughs.

Initial persistence operations:

- create, read, and update sync root.
- create and read tracked item.
- upsert local observation.
- upsert remote observation.
- store permanent-delete tombstone state.
- commit base snapshot.
- store and read checkpoint.
- atomic remote observation batch plus checkpoint update.

Not included yet:

- operation queue.
- retries.
- conflict resolution UI.
- planner.
- executor.
- automatic namespace disambiguation.

## Three-Way Diff Classifier

After the first metadata persistence increment, add an in-memory classifier that compares:

```text
Base Snapshot
Local Observation
Remote Observation
```

Draft classifications:

- `NoChange`
- `LocalChanged`
- `RemoteChanged`
- `BothChangedCompatible`
- `Conflict`
- `LocalMissing`
- `RemoteMissing`
- `RemoteTrashed`
- `RemoteRemoved`
- `NamespaceCollision`

This classifier should describe state before Hermes decides which operations to execute.

## Exploration Cases Before Final Schema

Observed cases already reflected in this design:

- duplicate names under the same parent.
- rename collision.
- folder rename and effect on descendants.
- folder move and effect on descendants.
- folder trash and effect on descendants.

Remaining active exploration cases:

- `Files.List` pagination.
- `Changes.List` pagination.
- invalid or expired page token.

These cases can affect uniqueness assumptions, path reconstruction, subtree invalidation, checkpoint recovery, and rescan strategy.

## Open Questions

- Is the final Google scope full `Drive` or a narrower broad-enough scope for selected Drive trees?
- How should Hermes identify a Google account or connection in metadata?
- Should the first local identity be path-only, or should inode/device data be stored from the start?
- When should local file hashes be calculated?
- How long should tombstones be retained?
- Should tombstones be a separate table immediately, or represented first as snapshot rows with `Exists = false`?
- Should trashed remote items be treated as soft-deleted or as a separate state visible to conflict resolution?
- How should invalid page tokens recover: full rescan, checkpoint reset, or user intervention?
- How should duplicate names under the same Drive parent be represented locally?
- Should provider capabilities include access scope mode and sync-root visibility guarantees?
- What exact transaction boundaries are required for local scan observations, remote change observations, operation execution records, and base snapshot commits?
