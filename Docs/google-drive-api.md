# Google Drive API Notes

## Purpose

This document records observed Google Drive API behavior while building the Hermes exploration tool.

The notes are based on real `TestApp` runs. They should guide the future sync metadata model and Sync Engine design.

## OAuth

Hermes started with:

- `DriveService.Scope.DriveMetadataReadonly`

This scope was enough for:

- `about.get`
- `files.list`
- `files.get`
- `changes.getStartPageToken`

It was not enough for write operations such as folder creation.

Hermes then changed to:

- `DriveService.Scope.DriveFile`

Hermes then changed to:

- `DriveService.Scope.Drive`

The product goal is a full read/write mirror of a selected Drive tree, so `DriveFile` is not sufficient as the final development scope.

When the scope changes, the saved token has to be invalidated. Hermes checks the saved token scope and deletes the token file when it does not include the required scope.

Observed behavior:

- First authentication after the scope change opened the browser and requested consent.
- Later authentications reused the saved token.
- The token is stored at `{SysConfig.AppFolderPath}/Credentials/google-token.json`.
- Full Drive scope is now the development assumption.
- With `DriveService.Scope.DriveFile`, changes made in the browser were reported only for files and folders visible to the app, such as objects created by Hermes.
- Browser-side changes to unrelated Drive objects were not reported to Hermes under the `drive.file` scope.

Observed behavior after switching to `DriveService.Scope.Drive`:

- The old saved token was rejected after exact scope matching was fixed.
- Authentication opened the browser and requested new consent.
- `files.list` for root returned existing non-Hermes Drive items.
- `changes.list` reported browser-side rename of an existing non-Hermes root file.
- This confirms that broad Drive scope is suitable for full mirror exploration.

Observed non-Hermes browser rename under full Drive scope:

- Start page token used: `37`
- Change count: `1`
- `ItemId`: `1mWLHUMpr7EoMm0o3nDDD_FpYcR95bTrc`
- `Removed`: `False`
- `HasItem`: `True`
- `Name`: `GoogleDriveText00.txt`
- `MimeType`: `text/plain`
- `Size`: `3858`
- `Md5Hash`: `6eb03272d4c7ff48756461a25c05f54e`
- `ModifiedTime`: `2026-07-10 09:03:08 UTC`
- `CreatedTime`: `2026-07-09 21:22:10 UTC`
- `ParentId`: root id
- `Trashed`: `False`
- `Version`: `6`
- `NewStartPageToken`: `38`

## About

`about.get` is used to prove that authentication and `DriveService` creation work.

Requested fields:

- `user(displayName,emailAddress)`
- `storageQuota(limit,usage)`

Observed values:

- app name is Hermes
- user email is available
- storage quota limit is available
- storage quota usage is available

## Start Page Token

`changes.getStartPageToken` returns the initial remote changes cursor.

Observed value in an early run:

- `4`

Hermes does not persist this token yet during exploration.

## Root Folder Listing

Root listing uses `files.list`.

Query:

```text
'root' in parents and trashed = false
```

Requested fields:

```text
files(id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version)
```

Observed behavior:

- Only immediate root children are returned.
- Nested files are not returned.
- Normal listings with `trashed = false` represent the current visible tree.
- Trashed items do not appear in normal folder listings unless explicitly requested.
- The root parent id returned in `Parents` was:

```text
0AGOX4SwqGE2yUk9PVA
```

## Folder Listing

Folder listing uses `files.list` with a specific folder id.

Query shape:

```text
'<folder-id>' in parents and trashed = false
```

Observed behavior:

- Only immediate folder children are returned.
- Listing `Folder00` returned `NestedText00.txt`.
- The nested file had `ParentId` equal to the `Folder00` id.
- Normal folder listings should be treated as the current visible tree.

## Get File

`files.get` is used to retrieve one item by id.

Requested fields:

```text
id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version
```

Observed behavior:

- `files.get` returns the same core metadata shape as `files.list` when the same fields are requested.
- File and folder objects map cleanly to `StorageItem`.

## Create Folder

Folder creation uses `files.create`.

Metadata sent:

- `Name`
- `MimeType = application/vnd.google-apps.folder`
- optional `Parents`

Requested response fields:

```text
id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version
```

Observed behavior when creating `CreatedByHermes00` in root:

- `Id`: `1cZyUmlCUb_JDYTjssYsF6phUdYC7ex35`
- `Name`: `CreatedByHermes00`
- `MimeType`: `application/vnd.google-apps.folder`
- `IsFolder`: `True`
- `Size`: not available
- `Md5Hash`: not available
- `CreatedTime` and `ModifiedTime` were equal
- `ParentId`: root id
- `Trashed`: `False`
- `Version`: `1`

## Rename

Rename uses `files.update`.

Metadata sent:

- `Name`

No other metadata fields are sent.

Requested response fields:

```text
id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version
```

Observed behavior when renaming the app-created folder from `CreatedByHermes00` to `CreatedByHermes01`:

- `Id` stayed the same: `1cZyUmlCUb_JDYTjssYsF6phUdYC7ex35`
- `Name` changed from `CreatedByHermes00` to `CreatedByHermes01`
- `MimeType` stayed the same
- `IsFolder` stayed `True`
- `Size` stayed not available
- `Md5Hash` stayed not available
- `CreatedTime` stayed the same
- `ModifiedTime` changed from `2026-07-09 22:08:41 UTC` to `2026-07-09 22:16:47 UTC`
- `ParentId` stayed the same: root id
- `Trashed` stayed `False`
- `Version` changed from `1` to `2`

The `files.update` response and the following `files.get` response matched for the observed fields.

Rename is therefore a metadata change that preserves identity and parent, but updates `ModifiedTime` and increments `Version`.

Sync implication:

- A remote rename must be treated as a change to the existing item identified by `Id`.
- It must not be treated as delete plus create.

## Move

Move uses `files.update`.

Metadata sent:

- empty metadata object

Request settings:

- `AddParents = newParentId`
- `RemoveParents = oldParentId`

The old parent id is obtained by calling `files.get` before the update.

Requested response fields:

```text
id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version
```

Observed behavior when moving the app-created folder `CreatedByHermes01` from root into `Folder00`:

- `Id` stayed the same: `1cZyUmlCUb_JDYTjssYsF6phUdYC7ex35`
- `Name` stayed the same: `CreatedByHermes01`
- `MimeType` stayed the same
- `IsFolder` stayed `True`
- `Size` stayed not available
- `Md5Hash` stayed not available
- `CreatedTime` stayed the same
- `ModifiedTime` stayed the same: `2026-07-09 22:16:47 UTC`
- `ParentId` changed from root id to `Folder00` id
- `Trashed` stayed `False`
- `Version` changed from `3` to `4`

The `files.update` response and the following `files.get` response matched for the observed fields.

Observed behavior when moving the same folder back from `Folder00` to root:

- `Id` stayed the same: `1cZyUmlCUb_JDYTjssYsF6phUdYC7ex35`
- `Name` stayed the same: `CreatedByHermes01`
- `MimeType` stayed the same
- `IsFolder` stayed `True`
- `Size` stayed not available
- `Md5Hash` stayed not available
- `CreatedTime` stayed the same
- `ModifiedTime` stayed the same: `2026-07-09 22:16:47 UTC`
- `ParentId` changed from `Folder00` id back to root id
- `Trashed` stayed `False`
- `Version` changed from `4` to `5`

Observation:

- Move preserves item identity.
- Move changes parent hierarchy.
- Move increments `Version`.
- Move did not change `ModifiedTime` in either folder move test.
- Remote change detection cannot rely only on `ModifiedTime`.

Sync implication:

- A remote move must be treated as a hierarchy update to the existing item identified by `Id`.
- It must not be treated as delete plus create.
- `ModifiedTime` alone is not sufficient for detecting remote changes.
- In this test, moving a folder changed `Version` but did not change `ModifiedTime`.

## Trash

Trash uses `files.update`.

Metadata sent:

- `Trashed = true`

This is a soft-delete operation. Permanent delete is not used in this test.

Requested response fields:

```text
id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version
```

Observed behavior when moving the app-created folder `CreatedByHermes01` to trash:

- `Id` stayed the same: `1cZyUmlCUb_JDYTjssYsF6phUdYC7ex35`
- `Name` stayed the same: `CreatedByHermes01`
- `MimeType` stayed the same
- `IsFolder` stayed `True`
- `Size` stayed not available
- `Md5Hash` stayed not available
- `CreatedTime` stayed the same
- `ModifiedTime` stayed the same: `2026-07-09 22:16:47 UTC`
- `ParentId` stayed the same: root id
- `Trashed` changed from `False` to `True`
- `Version` changed from `5` to `6`

The `files.update` response and the following `files.get` response matched for the observed fields.

Observed behavior after trash:

- The item can still be retrieved with `files.get`.
- The item still has its parent id.
- The item does not appear in normal folder listings that use `trashed = false`.
- Trash changed `Version` but did not change `ModifiedTime` in this folder trash test.

Sync implication:

- A remote trash operation must be treated as a soft-delete state change on the existing item identified by `Id`.
- It must not be treated as immediate permanent deletion.
- `ModifiedTime` alone is not sufficient for detecting trash state changes.
- In this test, trashing changed `Version` but did not change `ModifiedTime`.

## Restore

Restore uses `files.update`.

Metadata sent:

- `Trashed = false`

Requested response fields:

```text
id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version
```

Observed behavior when restoring the app-created folder `CreatedByHermes01` from trash:

- `Id` stayed the same: `1cZyUmlCUb_JDYTjssYsF6phUdYC7ex35`
- `Name` stayed the same: `CreatedByHermes01`
- `MimeType` stayed the same
- `IsFolder` stayed `True`
- `Size` stayed not available
- `Md5Hash` stayed not available
- `CreatedTime` stayed the same
- `ModifiedTime` stayed the same: `2026-07-09 22:16:47 UTC`
- `ParentId` stayed the same: root id
- `Trashed` changed from `True` to `False`
- `Version` changed from `6` to `7`

The `files.update` response and the following `files.get` response matched for the observed fields.

Observed behavior after restore:

- The item appeared again in `files.list` for root with `trashed = false`.
- The listed metadata matched the restored item.

Sync implication:

- A remote restore operation must be treated as a soft-delete state change on the existing item identified by `Id`.
- It must not be treated as a new create.
- In this test, restoring changed `Version` but did not change `ModifiedTime`.

## Upload File

Upload uses `files.create` media upload.

Metadata sent:

- `Name`
- optional `Parents`

Content sent:

- local file stream
- MIME type derived from the local file extension

Requested response fields:

```text
id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version
```

Observed behavior when uploading `/home/teo/gdrive_dam/UploadByHermes00.txt` to root:

- `Id`: `1uf1a3bc-zZOKXqDWmxyEbvNW6YE33Qfp`
- `Name`: `UploadByHermes00.txt`
- `MimeType`: `text/plain`
- `IsFolder`: `False`
- `Size`: `5`
- `Md5Hash`: `d6c43639164bd159609fde47ae1477cc`
- `CreatedTime` and `ModifiedTime` were equal
- `ParentId`: root id
- `Trashed`: `False`
- `Version`: `1`

Initial observation:

- A newly uploaded regular file has `Size`.
- A newly uploaded regular file has `Md5Hash`.
- A newly uploaded regular file starts with `Version = 1`.
- Upload creates a new Drive item with a new `Id`.
- Upload establishes the initial local-to-remote mapping.

Sync implication:

- Uploading a local file creates a new remote Drive object.
- The remote `Id` becomes the stable identity of the uploaded file.
- The future sync model should store the relationship between local path/state and remote `Id`.
- For regular files, `Md5Hash` can be used as a content comparison signal.

Observed behavior in the following root listing:

- The uploaded file appeared in `files.list` for root with `trashed = false`.
- `Id`, `Name`, `MimeType`, `Size`, `Md5Hash`, `CreatedTime`, `ModifiedTime`, `ParentId`, and `Trashed` matched the upload response.
- `Version` appeared as `3` in the listing, while the upload response had returned `Version = 1`.

Open question:

- `Version` may change shortly after upload or may be reported differently between upload response and later list response. This needs more observation before using `Version` as a direct local comparison value.
- `Version` is not a content-only version. The upload response returned `Version = 1`, but later metadata reads showed `Version = 3` without a known user content update.
- `Version` appears to be a general object revision counter or Drive-side state counter. It may increase because of metadata actions, indexing, internal Drive processing, or other provider behavior.
- `Version` should not be used alone as proof that file content changed.

## Download File

Download uses `files.get` media download.

Inputs:

- remote file id
- local destination path

Observed behavior when downloading `UploadByHermes00.txt` to `/home/teo/gdrive_dam/DownloadedByHermes00.txt`:

- Remote `Id`: `1uf1a3bc-zZOKXqDWmxyEbvNW6YE33Qfp`
- Remote `Name`: `UploadByHermes00.txt`
- Remote `MimeType`: `text/plain`
- Remote `Size`: `5`
- Remote `Md5Hash`: `d6c43639164bd159609fde47ae1477cc`
- Remote `CreatedTime` stayed the same
- Remote `ModifiedTime` stayed the same
- Remote `ParentId` stayed root id
- Remote `Trashed` stayed `False`
- Remote `Version` stayed `3`
- Downloaded local file size was `5`

Initial observation:

- Download does not change remote metadata.
- Local output size matched remote `Size`.
- For regular files, the remote `Md5Hash` can be used later to verify content equivalence if the local MD5 is computed.

Sync implication:

- Download materializes remote content locally.
- The remote `Id` remains the identity anchor.
- For regular files, `Md5Hash` is the natural content comparison signal after download.

## Update File Content

Content update uses `files.update` media upload.

Inputs:

- remote file id
- local source file path

Metadata sent:

- empty metadata object

Content sent:

- local file stream
- MIME type derived from the local file extension

Requested response fields:

```text
id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version
```

Questions to answer:

- Does `Id` remain unchanged?
- Does `Name` remain unchanged?
- Does `Size` change?
- Does `Md5Hash` change?
- Does `ModifiedTime` change?
- Does `CreatedTime` remain unchanged?
- Does `Version` increment?
- Does `ParentId` remain unchanged?

No observations have been recorded yet.

Observed behavior when updating `UploadByHermes00.txt` from 5 bytes to 34 bytes:

- `Id` stayed the same: `1uf1a3bc-zZOKXqDWmxyEbvNW6YE33Qfp`
- `Name` stayed the same: `UploadByHermes00.txt`
- `MimeType` stayed the same: `text/plain`
- `Size` changed from `5` to `34`
- `Md5Hash` changed from `d6c43639164bd159609fde47ae1477cc` to `c5e1a7283d80d84387f0fef6f80e3bf0`
- `ModifiedTime` changed from `2026-07-09 22:40:08 UTC` to `2026-07-09 23:07:22 UTC`
- `CreatedTime` stayed the same
- `ParentId` stayed the same: root id
- `Trashed` stayed `False`
- `Version` changed from `3` to `4` in the update response
- The refreshed item immediately after update matched the update response
- The local MD5 matched the remote `Md5Hash`

Observed behavior when downloading the updated file:

- Downloaded local file size was `34`
- Downloaded local MD5 was `c5e1a7283d80d84387f0fef6f80e3bf0`
- Local size matched remote `Size`
- Local MD5 matched remote `Md5Hash`
- The remote metadata before download showed `Version = 6`, while the update response had returned `Version = 4`

Observation:

- Content update preserves remote identity.
- Content update changes content metadata: `Size`, `Md5Hash`, and `ModifiedTime`.
- Content update increments `Version`, but `Version` may advance again after the update without another known user content change.

Sync implication:

- A remote content update should be treated as content state change on the existing item identified by `Id`.
- For regular files, `Md5Hash` is a stronger content comparison signal than `Version`.
- `Version` remains useful as a change signal, but not as a direct content-version value.

## Incremental Changes After Content Update

Use the saved Changes API page token from before the content update.

For the current test, that token is:

```text
26
```

Expected flow:

```text
Saved token 26
        ->
Update existing file content
        ->
Changes.List(pageToken: 26)
        ->
Process all returned pages
        ->
Save NewStartPageToken only after successful processing
```

Questions to answer:

- Is exactly one changed `FileId` returned?
- Is it the existing file id?
- Does the change contain the updated `Size` and `Md5Hash`?
- Does the change contain the new `ModifiedTime`?
- Is `Removed` false?
- Is `HasFile` true?
- Does the new token advance beyond `26`?
- Does the content download itself create another change?
- Does a second `Changes.List` call using the new token return zero changes?

No observations have been recorded yet.

Observed behavior after content update using saved page token `26`:

- Change count: `1`
- Changed `FileId`: `1uf1a3bc-zZOKXqDWmxyEbvNW6YE33Qfp`
- `Removed`: `False`
- `HasFile`: `True`
- `Time`: `2026-07-09 23:07:26 UTC`
- `Size`: `34`
- `Md5Hash`: `c5e1a7283d80d84387f0fef6f80e3bf0`
- `ModifiedTime`: `2026-07-09 23:07:22 UTC`
- `CreatedTime`: unchanged
- `ParentId`: root id
- `Trashed`: `False`
- `Version`: `6`
- `NewStartPageToken`: `29`

Initial observation:

- The content update appeared as exactly one changed object from token `26`.
- The change contained the updated `Size`, `Md5Hash`, and `ModifiedTime`.
- Downloading the file after the content update did not create a separate change in this result.
- The returned `Change.Time` was later than `File.ModifiedTime`.
- The change stream returned the current file state, including `Version = 6`.

Pending check:

- Run `Changes.List` again with token `29`; expected result is zero changes.

Checkpoint verification:

- Running `Changes.List` again with token `29` returned `change count = 0`.
- `NewStartPageToken` remained `29`.
- Already processed changes were not returned again.

Sync implication:

- The changes checkpoint behavior is suitable for incremental sync.
- Persisting `NewStartPageToken` after successful processing prevents reprocessing the same changes.

## Permanent Delete

Permanent delete uses `files.delete`.

This is different from trashing an item.

Use only a safe app-created test object.

Inputs:

- remote file id

Expected test sequence:

- upload a fresh temporary file, such as `DeleteByHermes00.txt`
- consume its creation change and save a clean checkpoint
- get the file and log its current state
- permanently delete it with `files.delete`
- call `files.get` for the same id and observe whether it returns 404
- list the former parent folder and verify whether the item disappears
- call `changes.list` from the clean checkpoint
- call `changes.list` again from the returned checkpoint

Observed setup:

- Uploaded temporary file: `DeleteByHermes00.txt`
- File id: `1KzI8p4Vqha8VbqJl57yv_dkDhVG55Bwa`
- Size: `17`
- Md5Hash: `f56eaed39b7e7488960e00eac623900e`
- ParentId: root id
- Upload response version: `1`
- Creation change version: `3`

Clean checkpoint before permanent delete:

```text
32
```

Observed behavior:

- `files.get` before deletion returned the file normally.
- `files.delete` succeeded.
- `files.get` after permanent deletion returned not found.
- TestApp observed and logged the 404 through `GoogleDriveNotFoundException`.
- The deleted item disappeared from normal root listing.

After permanent deletion:

- The deleted `FileId` was no longer fetchable through `files.get`.
- No metadata was available through `files.get`.
- The normal visible tree did not include the item.

## Changes API After Permanent Delete

Questions to answer:

- Is `Removed` true?
- Is `HasFile` false?
- If `File` is present, which metadata fields remain available?
- Does the change preserve the deleted `FileId`?
- Does the deletion produce exactly one change?
- Does the next checkpoint return zero changes?

Storage model implication:

- A permanently removed change may not contain a complete `StorageItem`.
- The provider-neutral change model should allow item metadata to be absent.
- The deleted `FileId` must be matched against the Hermes metadata store so the Sync Planner can locate the corresponding local item.

Observed behavior when listing changes from checkpoint `32` after permanent deletion:

- Change count: `1`
- `FileId`: `1KzI8p4Vqha8VbqJl57yv_dkDhVG55Bwa`
- `Removed`: `True`
- `Time`: `2026-07-09 23:27:02 UTC`
- `HasFile`: `False`
- `NewStartPageToken`: `34`

Observed conclusion:

- Permanent delete produced exactly one change.
- The change preserved the deleted `FileId`.
- `Removed` was `True`.
- `HasFile` was `False`.
- No `StorageItem` metadata was available in the change.

Sync implication:

- Permanent delete must be handled through the Changes API.
- The deleted `FileId` must be matched against the Hermes metadata store.
- The metadata store is required because the remote object can no longer be fetched.
- The provider-neutral change model must allow missing `StorageItem` metadata for removed changes.
- A removed change means the remote identity no longer exists.
- A permanent deletion is represented by a tombstone-like change.
- A provider-neutral `StorageChange` cannot require `StorageItem` to be present.

Checkpoint verification:

- Running `Changes.List` again with token `34` returned `change count = 0`.
- `NewStartPageToken` remained `34`.
- The permanent delete change was not returned again.

## Changes API Under Drive File Scope

Earlier exploration used:

```text
DriveService.Scope.DriveFile
```

Observed behavior:

- Renaming unrelated Drive files in the browser did not appear in `changes.list`.
- Listing changes from token `34` returned `change count = 0` and advanced the checkpoint to `35`.
- Listing changes again from token `35` returned `change count = 0` and advanced the checkpoint to `36`.
- Renaming an app-created file in the browser did appear in `changes.list`.

Observed app-created file rename:

- Start page token used: `36`
- Change count: `1`
- `ItemId`: `1uf1a3bc-zZOKXqDWmxyEbvNW6YE33Qfp`
- `Removed`: `False`
- `HasItem`: `True`
- `Name`: `UploadByHermes001.txt`
- `MimeType`: `text/plain`
- `Size`: `34`
- `Md5Hash`: `c5e1a7283d80d84387f0fef6f80e3bf0`
- `ModifiedTime`: `2026-07-10 08:15:11 UTC`
- `CreatedTime`: `2026-07-09 22:40:08 UTC`
- `ParentId`: root id
- `Trashed`: `False`
- `Version`: `9`
- `NewStartPageToken`: `37`

Sync implication:

- With `DriveService.Scope.DriveFile`, Hermes can only sync the app-visible Drive subset.
- A full user Drive mirror would require broader Drive access or an explicit user-driven file/folder access model.
- The sync engine must treat provider access scope as part of the provider capability and configuration model.

## Changes API

Changes listing uses `changes.list`.

Requested fields:

```text
nextPageToken
newStartPageToken
changes(fileId,removed,time,file(id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version))
```

Request settings:

- `PageSize = 100`
- `IncludeRemoved = true`

The exploration command supports:

- saved start page token from `AppSettings.Google.StartPageToken`
- optional manually entered page token
- automatic pagination through `NextPageToken`
- capture and temporary persistence of `NewStartPageToken`
- mapping `Change.File` to `StorageItem` when present

Questions to answer:

- Do rename, move, trash, restore, and upload operations appear in `changes.list`?
- Does download appear as a remote change?
- For trash, does the change contain `File` with `Trashed = true`, or `Removed = true`?
- For permanent delete later, does `Removed` become `true`?
- Do folder move operations appear even though `ModifiedTime` did not change?
- Is `Version` useful inside the change stream?
- Does `changes.list` return duplicate changes for the same `FileId`?
- Does the final state of the file appear, or every intermediate operation?

No observations have been recorded yet. Run `List Changes` from `TestApp` and add the results here.

Observed behavior when listing changes from page token `4` after the first exploration cycle:

- Change count: `2`
- `NewStartPageToken`: `26`
- Change for folder `1cZyUmlCUb_JDYTjssYsF6phUdYC7ex35`
- Change for uploaded file `1uf1a3bc-zZOKXqDWmxyEbvNW6YE33Qfp`

Folder change:

- `FileId`: `1cZyUmlCUb_JDYTjssYsF6phUdYC7ex35`
- `Removed`: `False`
- `HasFile`: `True`
- `Time`: `2026-07-09 22:33:23 UTC`
- `Name`: `CreatedByHermes01`
- `ParentId`: root id
- `Trashed`: `False`
- `Version`: `7`
- `ModifiedTime`: `2026-07-09 22:16:47 UTC`

Uploaded file change:

- `FileId`: `1uf1a3bc-zZOKXqDWmxyEbvNW6YE33Qfp`
- `Removed`: `False`
- `HasFile`: `True`
- `Time`: `2026-07-09 22:40:12 UTC`
- `Name`: `UploadByHermes00.txt`
- `ParentId`: root id
- `Trashed`: `False`
- `Size`: `5`
- `Md5Hash`: `d6c43639164bd159609fde47ae1477cc`
- `Version`: `3`
- `ModifiedTime`: `2026-07-09 22:40:08 UTC`

Initial observations:

- `changes.list` did not return every intermediate operation separately from this token.
- The folder had gone through create, rename, move, move back, trash, and restore, but the change stream returned one change for the folder with its final visible state.
- The uploaded file had upload and download activity, but the change stream returned one change for the uploaded file.
- Download did not appear as a separate remote change.
- Folder move/trash/restore changes are represented through the final file state when listing from the older token.
- `Change.Time` can be later than `File.ModifiedTime`.
- `NewStartPageToken` must be persisted after processing changes.

Sync implication:

- The Changes API appears suitable for incremental remote synchronization, but it may coalesce multiple operations on the same file id into the latest state visible from the requested token.
- The sync engine must process changes as state reconciliation by `FileId`, not as an exact replay of every user operation.
- A local metadata store is required to compare previous known state with the changed `StorageItem` state.
- Hermes should treat the Changes API as an incremental changed-object feed.
- For each returned `FileId`, the sync engine should compare the returned current state against the locally persisted remote metadata snapshot and derive the required operation.
- The page token is a synchronization checkpoint.
- The new page token must only be committed after the complete page sequence has been processed successfully.

## File Object Observations

Regular files:

- have `Size`
- have `Md5Hash`
- have `MimeType`, such as `text/plain`
- have `ParentId`
- have `CreatedTime`
- have `ModifiedTime`
- have `Version`

Folders:

- have `MimeType = application/vnd.google-apps.folder`
- have no `Size`
- have no `Md5Hash`
- have `ParentId`
- have `CreatedTime`
- have `ModifiedTime`
- have `Version`

Observed sample root file:

- `Name`: `GoogleDriveText00.txt`
- `MimeType`: `text/plain`
- `Size`: `3858`
- `Md5Hash`: `6eb03272d4c7ff48756461a25c05f54e`
- `Version`: changed from `2` to `3` during exploration

Observed sample nested file:

- `Name`: `NestedText00.txt`
- `MimeType`: `text/plain`
- `Size`: `2845`
- `Md5Hash`: `19a879d59fb58d573ae20606f1b48352`
- `ParentId`: `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
- `Version`: `3`

Observed sample folder:

- `Name`: `Folder00`
- `MimeType`: `application/vnd.google-apps.folder`
- `Size`: not available
- `Md5Hash`: not available
- `Version`: `2`

## Duplicate Names

Google Drive allows multiple items with the same name under the same parent.

Observed root listing after creating duplicate names:

- Parent id: `0AGOX4SwqGE2yUk9PVA`
- Root item count: `7`
- Two regular files had the same name: `DuplicateName.txt`
- Both regular files had the same parent id.
- Both regular files had the same MIME type, size, and MD5 hash.
- The two regular files had different Google Drive ids.
- A Google Docs item named `DuplicateName` also existed under the same parent.

Observed duplicate regular file 1:

- `Id`: `13yhgbrh0x73FxRKHE-dp2s58lMN2h8Dm`
- `Name`: `DuplicateName.txt`
- `MimeType`: `text/plain`
- `Size`: `3858`
- `Md5Hash`: `6eb03272d4c7ff48756461a25c05f54e`
- `CreatedTime`: `2026-07-10 09:09:19 UTC`
- `ModifiedTime`: `2026-07-10 09:09:25 UTC`
- `ParentId`: root id
- `Version`: `4`

Observed duplicate regular file 2:

- `Id`: `1H26EQ5Bdm8us8rMsvlFTRLCAul7aVT2w`
- `Name`: `DuplicateName.txt`
- `MimeType`: `text/plain`
- `Size`: `3858`
- `Md5Hash`: `6eb03272d4c7ff48756461a25c05f54e`
- `CreatedTime`: `2026-07-10 09:08:49 UTC`
- `ModifiedTime`: `2026-07-10 09:09:11 UTC`
- `ParentId`: root id
- `Version`: `4`

Observed Google Docs item:

- `Id`: `1srnEU8lfMr4nyvJyK0g6ykm2wYEaBm1pyvvyqGkzJ8c`
- `Name`: `DuplicateName`
- `MimeType`: `application/vnd.google-apps.document`
- `Size`: `1024`
- `Md5Hash`: not available
- `CreatedTime`: `2026-07-10 09:07:39 UTC`
- `ModifiedTime`: `2026-07-10 09:08:23 UTC`
- `ParentId`: root id
- `Version`: `7` in listing

Changes API from token `38` returned three changes:

- `1H26EQ5Bdm8us8rMsvlFTRLCAul7aVT2w`, `DuplicateName.txt`
- `13yhgbrh0x73FxRKHE-dp2s58lMN2h8Dm`, `DuplicateName.txt`
- `1srnEU8lfMr4nyvJyK0g6ykm2wYEaBm1pyvvyqGkzJ8c`, `DuplicateName`

The new start page token was `57`.

Sync implication:

- `(ParentId, Name)` is not unique in Google Drive.
- Remote identity must be based on `File.Id`.
- A local filesystem mirror needs a collision policy for remote siblings with the same name, because a single local folder cannot contain two entries with the exact same filename.
- The metadata store must allow multiple tracked remote items with the same parent id and name.

## Rename Collision

Google Drive allows renaming an item to the same name as existing siblings under the same parent.

Observed rename collision setup:

- Start page token used before rename: `58`
- Existing duplicate name under root: `DuplicateName.txt`
- Renamed item id: `1mWLHUMpr7EoMm0o3nDDD_FpYcR95bTrc`
- Old name: `GoogleDriveText00.txt`
- New name: `DuplicateName.txt`
- Parent id: `0AGOX4SwqGE2yUk9PVA`

Observed rename response:

- `Id` stayed the same: `1mWLHUMpr7EoMm0o3nDDD_FpYcR95bTrc`
- `Name` changed to `DuplicateName.txt`
- `MimeType` stayed `text/plain`
- `Size` stayed `3858`
- `Md5Hash` stayed `6eb03272d4c7ff48756461a25c05f54e`
- `CreatedTime` stayed `2026-07-09 21:22:10 UTC`
- `ModifiedTime` changed to `2026-07-10 09:18:36 UTC`
- `ParentId` stayed root id
- `Trashed` stayed `False`
- `Version` changed from `7` to `8`

Observed root listing after rename:

- Root contained three regular files named `DuplicateName.txt`.
- All three had the same parent id.
- All three had different Google Drive ids.

Observed Changes API from token `58`:

- Change count: `1`
- `ItemId`: `1mWLHUMpr7EoMm0o3nDDD_FpYcR95bTrc`
- `Removed`: `False`
- `HasItem`: `True`
- `Name`: `DuplicateName.txt`
- `ModifiedTime`: `2026-07-10 09:18:36 UTC`
- `Version`: `8`
- `NewStartPageToken`: `59`

Sync implication:

- Rename collision is not rejected by Google Drive.
- It is reported as a normal changed-object update for the existing `File.Id`.
- The sync planner must detect local materialization collisions separately.
- Remote duplicate-name conflicts are mirror conflicts, not provider operation errors.

## Folder Rename

Folder rename updates the folder item itself. It does not produce changes for immediate children in the observed test.

Observed setup:

- Start page token before rename: `59`
- Folder id: `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
- Old folder name: `Folder00`
- New folder name: `Folder00Renamed`
- Folder parent id: root id

Observed child before folder rename:

- `Id`: `1QMpZhOz_O3ys3ezInhETbQha3trjJhtC`
- `Name`: `NestedText001.txt`
- `ParentId`: `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
- `ModifiedTime`: `2026-07-10 08:12:22 UTC`
- `Version`: `5`

Observed folder rename response:

- `Id` stayed the same: `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
- `Name` changed to `Folder00Renamed`
- `MimeType` stayed `application/vnd.google-apps.folder`
- `CreatedTime` stayed `2026-07-09 21:24:13 UTC`
- `ModifiedTime` changed from `2026-07-09 21:24:13 UTC` to `2026-07-10 09:23:46 UTC`
- `ParentId` stayed root id
- `Trashed` stayed `False`
- `Version` changed from `4` to `5`

Observed child after folder rename:

- `Id` stayed the same: `1QMpZhOz_O3ys3ezInhETbQha3trjJhtC`
- `Name` stayed `NestedText001.txt`
- `ParentId` stayed `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
- `ModifiedTime` stayed `2026-07-10 08:12:22 UTC`
- `Version` stayed `5`

Observed Changes API from token `59`:

- Change count: `1`
- Changed item id: `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
- `Removed`: `False`
- `HasItem`: `True`
- `Name`: `Folder00Renamed`
- `ModifiedTime`: `2026-07-10 09:23:46 UTC`
- `Version`: `5`
- `NewStartPageToken`: `60`
- The child item was not returned as a separate change.

Sync implication:

- Folder rename is a change to the folder identity only.
- Descendant remote `ParentId` values do not change.
- Descendant metadata does not change simply because an ancestor folder was renamed.
- Local path reconstruction must account for ancestor path changes even when descendants are not returned by the Changes API.
- The metadata store should represent hierarchy by parent identity and derive paths when needed.

## Folder Move

Folder move updates the folder parent identity. It does not produce changes for immediate children in the observed test.

Observed setup:

- Start page token before move: `60`
- Moved folder id: `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
- Folder name: `Folder00Renamed`
- Old parent id: root id
- New parent id: `1cZyUmlCUb_JDYTjssYsF6phUdYC7ex35`
- Child id: `1QMpZhOz_O3ys3ezInhETbQha3trjJhtC`

Observed folder before move:

- `ParentId`: root id
- `ModifiedTime`: `2026-07-10 09:23:46 UTC`
- `Version`: `5`

Observed folder after move:

- `Id` stayed the same: `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
- `Name` stayed `Folder00Renamed`
- `ParentId` changed to `1cZyUmlCUb_JDYTjssYsF6phUdYC7ex35`
- `ModifiedTime` stayed `2026-07-10 09:23:46 UTC`
- `CreatedTime` stayed `2026-07-09 21:24:13 UTC`
- `Trashed` stayed `False`
- `Version` changed from `5` to `6`

Observed child after folder move:

- `Id` stayed the same: `1QMpZhOz_O3ys3ezInhETbQha3trjJhtC`
- `Name` stayed `NestedText001.txt`
- `ParentId` stayed `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
- `ModifiedTime` stayed `2026-07-10 08:12:22 UTC`
- `Version` appeared as `6`

Observed Changes API from token `60`:

- Change count: `1`
- Changed item id: `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
- `Removed`: `False`
- `HasItem`: `True`
- `Name`: `Folder00Renamed`
- `ParentId`: `1cZyUmlCUb_JDYTjssYsF6phUdYC7ex35`
- `ModifiedTime`: `2026-07-10 09:23:46 UTC`
- `Version`: `6`
- `NewStartPageToken`: `61`
- The child item was not returned as a separate change.

Sync implication:

- Folder move is a change to the folder identity only.
- Descendant remote `ParentId` values do not change.
- Descendant local paths change because an ancestor moved, even when descendants are not returned by the Changes API.
- Folder move changed `Version` but did not change `ModifiedTime`.
- `ModifiedTime` alone is not sufficient to detect hierarchy changes.
- Descendant `Version` should not be treated as enough evidence of a direct child change unless the child is returned by listing or changes with other supporting metadata.

## Folder Trash

Folder trash propagates the trashed state to descendants in the observed test.

Observed setup:

- Start page token before trash: `61`
- Trashed folder id: `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
- Folder name: `Folder00Renamed`
- Folder parent id: `1cZyUmlCUb_JDYTjssYsF6phUdYC7ex35`
- Child id: `1QMpZhOz_O3ys3ezInhETbQha3trjJhtC`

Observed folder before trash:

- `Trashed`: `False`
- `ModifiedTime`: `2026-07-10 09:23:46 UTC`
- `Version`: `6`

Observed folder after trash:

- `Id` stayed the same: `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
- `Name` stayed `Folder00Renamed`
- `ParentId` stayed `1cZyUmlCUb_JDYTjssYsF6phUdYC7ex35`
- `Trashed` changed to `True`
- `ModifiedTime` stayed `2026-07-10 09:23:46 UTC`
- `Version` changed from `6` to `7`

Observed child after folder trash:

- `Id` stayed the same: `1QMpZhOz_O3ys3ezInhETbQha3trjJhtC`
- `Name` stayed `NestedText001.txt`
- `ParentId` stayed `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
- `Trashed` changed to `True`
- `ModifiedTime` stayed `2026-07-10 08:12:22 UTC`
- `Version` changed to `7`

Observed listing trashed folder:

- `ListFolderAsync` uses `trashed = false`.
- Listing the trashed folder returned `folder item count = 0`.
- The child was still fetchable by `files.get`, but it was now `Trashed = True`.

Observed Changes API from token `61`:

- Change count: `2`
- Folder change:
  - `ItemId`: `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
  - `Removed`: `False`
  - `HasItem`: `True`
  - `Trashed`: `True`
  - `Version`: `7`
- Child change:
  - `ItemId`: `1QMpZhOz_O3ys3ezInhETbQha3trjJhtC`
  - `Removed`: `False`
  - `HasItem`: `True`
  - `Trashed`: `True`
  - `Version`: `7`
- `NewStartPageToken`: `63`

Sync implication:

- Folder trash is a soft-delete state change.
- In this test, trash propagated to descendants and descendants appeared as separate changes.
- Trashed descendants may still be fetchable by id.
- Normal folder listing with `trashed = false` hides trashed descendants.
- The planner should treat folder trash as a subtree state change, but still process explicit descendant changes when the provider returns them.

## Folder Restore

Folder restore propagates the restored trashed state to descendants in the observed test.

Observed setup:

- Start page token before restore: `63`
- Restored folder id: `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
- Folder name: `Folder00Renamed`
- Folder parent id: `1cZyUmlCUb_JDYTjssYsF6phUdYC7ex35`
- Child id: `1QMpZhOz_O3ys3ezInhETbQha3trjJhtC`

Observed folder before restore:

- `Trashed`: `True`
- `ModifiedTime`: `2026-07-10 09:23:46 UTC`
- `Version`: `7`

Observed folder after restore:

- `Id` stayed the same: `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
- `Name` stayed `Folder00Renamed`
- `ParentId` stayed `1cZyUmlCUb_JDYTjssYsF6phUdYC7ex35`
- `Trashed` changed to `False`
- `ModifiedTime` stayed `2026-07-10 09:23:46 UTC`
- `Version` changed from `7` to `8`

Observed child after folder restore:

- `Id` stayed the same: `1QMpZhOz_O3ys3ezInhETbQha3trjJhtC`
- `Name` stayed `NestedText001.txt`
- `ParentId` stayed `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
- `Trashed` changed to `False`
- `ModifiedTime` stayed `2026-07-10 08:12:22 UTC`
- `Version` changed from `7` to `8`

Observed listing restored folder:

- `ListFolderAsync` returned the child again.
- Child `Trashed` was `False`.

Observed Changes API from token `63`:

- Change count: `2`
- Folder change:
  - `ItemId`: `1E-6TsbK_f0k81raLcJLXxHgNCzuJJFpS`
  - `Removed`: `False`
  - `HasItem`: `True`
  - `Trashed`: `False`
  - `Version`: `8`
- Child change:
  - `ItemId`: `1QMpZhOz_O3ys3ezInhETbQha3trjJhtC`
  - `Removed`: `False`
  - `HasItem`: `True`
  - `Trashed`: `False`
  - `Version`: `8`
- `NewStartPageToken`: `67`

Sync implication:

- Folder restore is a soft-delete state change.
- In this test, restore propagated to descendants and descendants appeared as separate changes.
- Restored descendants became visible again in normal folder listing with `trashed = false`.
- The planner should treat folder restore as a subtree state change, but still process explicit descendant changes when the provider returns them.

## Invalid Changes Token

Calling `changes.list` with an invalid page token fails with a Google API error.

Observed input:

```text
invalid-token
```

Observed result:

- Exception type: `Google.GoogleApiException`
- HTTP status code: `BadRequest`
- Message: `Invalid Value`
- Failure location: `GoogleDriveClient.ListChangesAsync`

Sync implication:

- Invalid or expired page tokens use provider-neutral handling.
- The Google provider maps this case to `StorageErrorKind.CheckpointInvalid`.
- Recovery clears the stored token, fails the current pass visibly, then bootstraps with a full remote snapshot and a fresh start page token on the next pass.
- Hermes must not silently replace a bad checkpoint without deciding how to reconcile missed changes.

## Sync-Relevant Conclusions

Google Drive item identity is `File.Id`.

Hermes should not use path as primary identity.

Hermes should also not assume that remote `(ParentId, Name)` is unique.

The likely sync identity relationship is:

```text
Local item state
        <=>
Google File.Id
```

Paths and names are metadata that can change.

The provider boundary is:

```text
Google.Apis.Drive.v3.Data.File
        ->
GoogleDriveMapper
        ->
StorageItem
```

Google-specific types must remain inside `Hermes.GoogleDrive`.

`Hermes.Core`, future metadata storage, and normal UI code should use `StorageItem`.
