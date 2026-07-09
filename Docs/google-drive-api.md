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

When the scope changed, the saved token had to be invalidated. Hermes now checks the saved token scope and deletes the token file when it does not include the required scope.

Observed behavior:

- First authentication after the scope change opened the browser and requested consent.
- Later authentications reused the saved token.
- The token is stored at `{SysConfig.AppFolderPath}/Credentials/google-token.json`.
- Full Drive scope is not used.

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

## Sync-Relevant Conclusions

Google Drive item identity is `File.Id`.

Hermes should not use path as primary identity.

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
