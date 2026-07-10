# Google Drive Exploration Plan

## Decision

Before implementing real sync logic, Hermes will pause Sync Engine work and use `TestApp` as a Google Drive exploration tool.

The reason is that synchronization depends first on understanding the Google Drive data model. The algorithm comes later. The immediate goal is to learn how the Google Drive API behaves, especially how the `File` resource changes after each operation.

Observed API behavior is recorded in `Docs/google-drive-api.md`.

## TestApp Role

`TestApp` is now the temporary Google Drive Exploration Tool.

Each new button should explore one Google Drive API operation in isolation. The tool should log:

- request summary
- response summary
- execution time
- important metadata
- errors and exception details
- observations useful for future sync design

No sync logic should be added during this exploration phase.

Google Drive OAuth currently uses `DriveService.Scope.Drive`. Hermes targets a full read/write mirror of a selected Drive tree, so broad Drive access is the development assumption.

Earlier exploration used `DriveService.Scope.DriveFile`. That scope showed that browser-side changes are reported only for app-visible files and folders, which is not enough for the full mirror product goal.

## Exploration Order

Recommended order:

- About
- Get Start Page Token
- List Root Folder
- List Folder
- Get File
- Create Folder
- Rename
- Move
- Delete
- Upload
- Download
- List Changes

`About`, `Get Start Page Token`, `List Root Folder`, `List Folder`, `Get File`, `Create Folder`, `Rename`, `Move`, `Trash`, `Restore`, `Delete Permanently`, `Upload File`, `Download File`, `Update File Content`, and `List Changes` are already implemented. The next likely operation is permanent delete behavior analysis.

## Primary API Object

The most important Google Drive API object for Hermes is `File`.

Before designing sync metadata or algorithms, Hermes must understand which `File` properties matter, when they change, and which values are stable identifiers.

Important properties to study:

- `Id`
- `Name`
- `MimeType`
- `Parents`
- `ModifiedTime`
- `CreatedTime`
- `Md5Checksum`
- `Size`
- `Version`
- `Trashed`
- `Capabilities`
- `Permissions`
- `Owners`

## Experiments

For each experiment, record the before and after `File` metadata.

Required experiments:

- rename a file
- move a file to another folder
- change file content
- upload a new version
- create a folder
- move a folder
- trash a file
- restore a file from trash
- permanently delete a test file

Destructive experiments must use test files and test folders only.

## Design Principle

Hermes should not treat paths as the primary synchronization identity.

Google Drive identity is based on:

```text
File.Id
```

The future sync model should be based on:

```text
Local file identity/state
        <=>
Google File.Id
```

and not on:

```text
Local path
        <=>
Google path
```

Paths are still important metadata, but they are not stable identity.

## Storage Model Boundary

Google Drive native models, such as `Google.Apis.Drive.v3.Data.File`, must remain inside `Hermes.GoogleDrive`.

Provider-facing Hermes code should use the provider-neutral `StorageItem` model from `Hermes.Storage`.

The mapping boundary is:

```text
Google Drive File
        ->
GoogleDriveMapper
        ->
StorageItem
```

`StorageItem` is the minimal provider-independent contract required by Hermes. It should contain sync-relevant metadata only, not the entire Google Drive API surface.

## Sync Engine Guidance

Do not start real Sync Engine implementation until the Google Drive model is better understood.

The current focus is state modeling:

- what is the remote identity
- what metadata is stable
- what metadata changes after each operation
- what must be stored locally
- what can be derived
- how changes are reported by `changes.list`

Only after this exploration should Hermes finalize sync metadata tables and sync algorithms.
