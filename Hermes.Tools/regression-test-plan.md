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
