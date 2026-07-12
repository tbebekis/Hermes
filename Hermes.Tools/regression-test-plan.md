# Hermes Tools Regression Test Plan

This document lists repeatable synchronization scenarios for Hermes.Tools.

Run these scenarios after changes in synchronization planning, metadata import, mutation execution, Google Drive integration, or service cycle coordination.

## Baseline

- Build `Hermes.Tools`, `Hermes.Service`, and `Hermes.Desktop`.
- Run `Hermes.Tests`.
- Run `git diff --check`.
- Start each live scenario from `reset-test-state`.
- Stop `Hermes.Service` before editing both local and remote sides for conflict tests.
- Capture `local-tree`, `drive-tree`, `db-tree`, and `status` after critical steps.

## Validated Live Scenarios

- MoveTarget regression: local tree move and remote namespace processing must not produce `[db] UNEXPECTED MoveTarget`.
- Empty bootstrap: empty local, empty database, and empty Drive root must produce no work and no conflicts.
- Local tree upload followed by repeated idle cycle: all local items must upload to Drive, database paths and remote ids must match, and the next cycle must be no-op.
- Local file edit: local content hash must upload to Drive and commit to base.
- Remote file edit: remote content hash must download to local and commit to base.
- Local file rename: Drive name must change while preserving remote id.
- Remote file rename: local name must change while preserving remote id.
- Local folder rename with descendants: Drive folder name and descendant paths must update while preserving remote ids.
- Remote folder rename with descendants: local folder name and descendant paths must update while preserving remote ids.
- Remote tree move under a new folder: local tree must move with all descendants and no duplicated paths.
- Local tree move back to root: Drive tree must move with all descendants and a repeated cycle must not delete child files.
- Remote subtree move under a target folder followed by local move-back: descendants and remote ids must be preserved, target folder contents must remain, and similar sibling names such as `TargetExtra` must not be affected.
- Move folder into another folder and back: local, database, and Drive paths must remain stable after the return move.
- Move file between folders repeatedly: repeated moves must preserve the remote file id and must not create duplicates.
- Local file delete: Drive file must be trashed or removed according to the executor path and base must commit only after verification.
- Remote file trash: local file must be removed and database state must match the trashed remote observation.
- Remote file trash followed by restore before service sees it: final active remote state must be a no-op with zero conflicts.
- Remote file trash plus new local file at the same path: the file must remain an open conflict and repeated cycles must not overwrite either side.
- Remote permanent file delete: local and database state must converge without stale conflicts.
- Remote item missing from Drive but still present in database after permanent delete: the local file must be removed, base state must become missing, no conflict must remain, and repeated cycles must be no-op.
- Remote folder delete or trash with descendants: descendant remote deletes must be covered by the ancestor folder operation and must not produce permanent failed child operations.
- Remote folder trash with descendants followed by restore: local and database state must restore with descendants and no stale conflicts.
- Local folder delete with descendants followed by restore: restored local tree must converge without stale remote deletes.
- Empty Google Drive trash after remote deletion followed by cycle: database must not retain stale conflicts or stale remote active items.
- Local delete then recreate same file with same content while remote remains active: final scan must be compatible and no-op.
- Local delete then recreate same file with different content while remote remains active: the change must upload to Drive and the next cycle must be no-op.
- Local and remote edit same file while service is stopped: one open conflict must persist across repeated cycles.
- Local and remote edit same file then manual resolution by matching local content to remote content: the open conflict must close, local/remote/base hashes must match, and the next cycle must be no-op.
- Local and remote edit same file then manual resolution by matching remote content to local content: the open conflict must close, local/remote/base hashes must match, and the next cycle must be no-op.
- Local edit plus remote trash of the same file while service is stopped: one open conflict must persist across repeated cycles.
- Local edit plus remote trash then manual resolution by deleting the local file: the conflict must close, file base must become missing, empty trash must leave no stale file conflict, and the parent folder may remain active unless explicitly removed.
- Local edit plus remote trash then manual resolution by restoring and updating the remote file to match local content: the conflict must close, local/remote/base hashes must match, and the next cycle must be no-op.
- Local delete plus remote edit of the same file while service is stopped: one open conflict must persist across repeated cycles.
- Local delete plus remote edit then manual resolution by restoring local content from remote: the conflict must close, hashes must match, and the next cycle must be no-op.
- Local delete plus remote edit then manual resolution by trashing the remote file: the conflict must close, file base must become missing, empty trash must leave no stale file conflict, and the parent folder may remain active unless explicitly removed.
- Local parent folder delete plus remote child edit while service is stopped: compatible sibling deletes may commit, but the edited child conflict must remain open across repeated cycles and remote content must not be lost.
- Local parent folder delete plus remote child rename while service is stopped: compatible sibling deletes may commit, but the renamed child conflict must remain open across repeated cycles and remote namespace must not be lost.
- Local rename plus remote edit of the same file while service is stopped: the conflict must remain open and repeated cycles must not overwrite remote content.
- Local folder rename plus remote child edit while service is stopped: folder namespace apply must not commit the descendant base snapshot; the child content conflict must stay open across repeated cycles.
- Local folder rename plus local child edit while service is stopped: namespace and content changes must both apply, preserve remote ids, commit matching base snapshots, and the next cycle must be no-op.
- Remote folder rename plus remote child edit while service is stopped: namespace and content changes must both apply locally, preserve remote ids, commit matching base snapshots, and the next cycle must be no-op.
- Remote folder move plus remote child edit while service is stopped: namespace and content changes must both apply locally, preserve remote ids, preserve target folder contents, commit matching base snapshots, and the next cycle must be no-op.
- Local folder move plus local child edit while service is stopped: namespace and content changes must both apply remotely, preserve remote ids, preserve target folder contents, commit matching base snapshots, and the next cycle must be no-op.
- Remote folder move plus local child edit while service is stopped: remote namespace changes and local content changes must both apply, preserve remote ids, preserve target folder contents, commit matching base snapshots, and the next cycle must be no-op.
- Local and remote rename the same folder to different names while service is stopped: the folder conflict must stay open across repeated cycles, descendants must not be overwritten, and resolving to either local or remote name must converge cleanly.
- Remote parent folder rename plus local child edit while service is stopped: compatible namespace and content changes must apply together, preserving child remote id.
- Local move plus remote rename of the same file to incompatible paths: expected conflict or blocked state must persist without overwriting either side.
- Same-name namespace collision from duplicate active Drive names: both colliding items must be blocked, local/base must not be mutated, and conflicts must close after one duplicate is renamed by id.
- Duplicate file name at root and nested folders: collision must be detected, then resolved after renaming one duplicate by id.
- Duplicate folder name with descendants: collision must be detected, descendants must not be lost, and resolution must converge after renaming one duplicate by id.
- Edge names with spaces, Greek characters, mixed case, and deep paths: local upload, remote edit, and remote rename must converge with correct paths, hashes, and preserved remote ids.
- Similar sibling folder names such as `Doc` and `DocExtra`: remote and local renames of one sibling must not affect the other sibling or its descendants.
- Case-only rename where supported by the local filesystem: local-to-remote and remote-to-local casing changes must converge, preserve remote id, and repeated cycles must be no-op.
- Empty folders: local empty folder upload, remote empty folder download, folder-only rename in both directions, local delete, remote trash, repeated cycle, and empty trash must converge with zero conflicts.
- Deep-tree 10-cycle mutation batch: local upload, idle no-op, remote leaf edit, local file edit, local folder rename, remote folder rename, local file move, remote file move, local empty folder delete, remote file trash, and final idle cycle must converge with preserved remote ids, matching base snapshots, no namespace collisions, no open conflicts, and no unexpected sibling loss.
- Conflict-resolution batch: edit/edit, local delete plus remote edit, local edit plus remote trash, and local rename plus remote edit must first remain stable as open conflicts across repeated cycles; manual remote-wins and local-wins resolutions must close conflicts, commit matching base snapshots, and the final idle cycle must be no-op.
- Local rename plus remote edit manual resolution caveat: resolving by delete-and-recreate at the remote path creates a temporary namespace collision with the tracked remote item; removing the unmatched local duplicate clears the collision, and an explicit local-wins or remote-wins action is still required to close the remaining conflict.
- Local folder rename plus remote child edit manual resolution: the initial child conflict must stay open, but after local content is manually matched to remote content at the renamed local path, the conflict must close and the descendant base snapshot must commit the renamed path and resolved content hash.
- Trash-restore-duplicate batch: local baseline upload, idle no-op, remote trash followed by restore before observation, remote permanent delete plus empty trash, duplicate active Drive name detection, duplicate resolution by id rename, remote folder trash with descendants, remote folder restore with descendants, and final idle cycle must converge with no stale conflicts, matching base snapshots, restored descendants, and no namespace collisions after resolution.
- Remote folder rename plus local child move into the renamed target path before local observes the remote folder rename: clearing the unmatched local duplicate folder must not crash the next cycle when duplicate tracked `LocalKey` rows remain; the cycle must return a deterministic blocked namespace collision instead of HTTP 500.
- Post-fix namespace sanity batch: baseline upload, idle no-op, remote folder rename observed locally, local child move after the rename is observed, local folder rename observed remotely, remote child edit, remote folder move plus local sibling edit, and final idle cycle must converge with no duplicate tracked keys, no namespace collisions, preserved remote ids, and matching base snapshots.
- Service restart between changes and cycle execution: persisted metadata must resume without duplicate operations.
- Manual cycle while another cycle is running: service must return `A synchronization cycle is already running` and the active cycle must finish normally.

## Core Live Scenarios

- Empty bootstrap: empty local, empty database, empty Drive root, no conflicts.
- Local tree upload: create nested folders and files locally, run a cycle, verify all items exist in local, database, and Drive.
- Repeated idle cycle: run another cycle after no changes, verify nothing is deleted or duplicated.
- Local file edit: edit a local file, run a cycle, verify Drive content/hash changes.
- Remote file edit: edit a Drive file, run a cycle, verify local content/hash changes.
- Remote tree move: move a remote folder tree under a new remote folder, run a cycle, verify local move and all children.
- Local tree move back to root: move the local tree back to root, run a cycle, verify Drive move and all children.
- Repeated cycle after tree move: run another cycle, verify no child files are deleted.
- Remote file rename: rename a Drive file, run a cycle, verify local rename.
- Local file rename: rename a local file, run a cycle, verify Drive rename and remote id preservation.
- Remote folder rename: rename a Drive folder with descendants, run a cycle, verify local rename and children.
- Local folder rename: rename a local folder with descendants, run a cycle, verify Drive rename and remote id preservation.
- Local file delete: delete a local file, run a cycle, verify Drive trash/removal state.
- Remote file trash: trash a Drive file, run a cycle, verify local deletion.
- Local folder delete: delete a local folder with descendants, run a cycle, verify remote folder and descendants are removed.
- Remote folder trash: trash a Drive folder with descendants, run a cycle, verify local folder and descendants are removed.

## Conflict Scenarios

- Local and remote edit same file while service is stopped, then start service and verify one open conflict.
- Local edit plus remote trash of same file while service is stopped, then verify conflict.
- Local delete plus remote edit of same file while service is stopped, then verify conflict.
- Local rename plus remote edit of same file while service is stopped, then verify conflict or intended plan.
- Local move plus remote rename of same file to incompatible paths, then verify conflict.
- Same-name namespace collision: create two different items that project to the same local path, then verify blocked/conflict state.
- Resolve conflict manually by making both sides compatible, run cycles until the open conflict is closed.

## Restore And Trash Scenarios

- Remote trash then restore the same file, verify local restore when appropriate.
- Remote trash then create a new local file with the same path, verify conflict or correct adoption.
- Local delete then recreate same file with same content, verify compatible restore/no conflict.
- Local delete then recreate same file with different content while remote still exists, verify expected plan/conflict.
- Empty Google Drive trash after remote deletions, run cycle, verify no stale conflicts.

## Edge Cases

- Deep nested tree with several levels and multiple files per level.
- Multiple sibling folders with similar names.
- File names with spaces.
- File names with Greek characters.
- File names with mixed case.
- Rename case only, if the local filesystem behavior supports it.
- Empty folders.
- Move folder into another folder and back.
- Move file between folders repeatedly.
- Delete parent folder after child file was edited remotely.
- Remote item missing from Drive but still present in database.
- Service restart between changes and cycle execution.
- Manual cycle while scheduled cycle is running, verify the guard.
- Desktop closed while service is running, if service lifetime changes later.

## Acceptance

- No unexpected local, database, or Drive paths.
- No duplicated folder trees.
- No unexpected remote id changes for rename/move-only operations.
- No child files lost after folder moves.
- No open conflicts after compatible changes.
- Open conflicts appear for incompatible concurrent changes.
- Repeated cycles are stable and do not mutate clean state.
