# Hermes

Hermes is a Linux desktop synchronization project for Google Drive.

The system is split in two parts:

- A background service that performs authentication, synchronization, change tracking, uploads, downloads, conflict handling, and metadata storage.
- An Avalonia desktop application that works as a control panel for the service.

The desktop application does not perform synchronization directly. It communicates with the service through a local API or IPC.

## Architecture

The intended solution layout is:

```text
Hermes.Common
Hermes.Storage
Hermes.Core
Hermes.GoogleDrive
Hermes.Service
Hermes.Desktop
Hermes.Tests
```

`Hermes.Core` contains the sync engine and depends only on storage abstractions. It must not depend on Google Drive directly.

`Hermes.GoogleDrive` implements the Google Drive provider behind those abstractions.

`Hermes.Desktop` is the Avalonia UI for status, activity, settings, folder selection, logs, and conflict handling.

## MVP Direction

The first version targets:

- Google OAuth login.
- Local folder selection.
- Google Drive root folder selection or creation.
- Initial sync.
- Local filesystem watcher plus periodic scan.
- Remote polling through Google Drive `changes.list`.
- Upload, download, delete, and rename handling.
- SQLite metadata.
- Basic conflict handling with keep-both behavior.

Push notifications, cloud relay infrastructure, advanced merge tools, and selective sync are planned as later concerns.
