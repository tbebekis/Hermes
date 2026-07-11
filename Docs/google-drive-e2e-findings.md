# Google Drive E2E Findings

## Purpose

This document summarizes the Google Drive end-to-end findings validated before starting conflict classification work.

## Validated Areas

- Local rename and move propagation for files.
- Local rename and move propagation for folders.
- Combined local folder rename and move.
- Local file delete propagated to remote trash.
- Local folder subtree delete propagated to remote trash.
- Remote trash propagated to local delete.
- Remote permanent file delete tombstone propagated to local delete.
- Remote permanent folder delete tombstone with descendant state commit.
- Remote file restore downloaded the local file again.
- Remote folder subtree restore downloaded parent folder, child folder, and nested file in order.
- Invalid remote checkpoint recovery.
- Google Drive changes pagination.
- Google Drive folder listing pagination.

## Google Drive Behavior

- Google Drive item identity is the file id.
- Rename preserves identity and parent.
- Move preserves identity and name.
- Trash preserves identity and metadata, but hides the item from normal folder listings.
- Restore preserves identity and makes the item visible again.
- Permanent delete may leave only a change tombstone with the item id.
- Permanent folder delete may return the parent folder tombstone before descendant tombstones.
- Descendant tombstones may arrive in a later changes pass.
- Folder listing requires pagination through `nextPageToken`.
- Changes listing requires pagination through `nextPageToken`.
- `ModifiedTime` is not sufficient for remote change detection.
- `Version` is useful as a provider change signal but is not a content-only version.
- `Md5Hash` is the strongest regular-file content comparison signal when available.

## Checkpoint Recovery

- Google `changes.list` invalid token maps to `StorageErrorKind.CheckpointInvalid`.
- The runner clears `REMOTE_CHECKPOINT.StartPageToken` after invalid checkpoint detection.
- The current pass fails visibly after the invalid checkpoint.
- The next pass bootstraps with a full remote snapshot and writes a fresh token.
- Recovery must not silently advance to a new token without reconciling possible missed changes.

## Sync Design Implications

- Sync planning should compare base state, observed local state, and observed remote state.
- Observations must not mutate base snapshots directly.
- Remote observation updates and checkpoint advancement must be atomic.
- Remote folder delete handling must commit descendant state when the remote folder delete is verified.
- Planner logic must handle ancestor rename, move, trash, restore, and delete effects on descendants.
- Conflict classification should start in memory before adding UI, operation queue, or conflict persistence.

