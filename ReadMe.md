# Hermes

Hermes is a Linux desktop synchronization project for Google Drive.

The system is split in two parts:

- A background service that performs authentication, synchronization, change tracking, uploads, downloads, conflict handling, and metadata storage.
- An Avalonia desktop application that works as a control panel for the service.

The desktop application does not perform synchronization directly. It communicates with the service through a local API or IPC.

## Architecture

The solution is split into several projects with narrow responsibilities.

### Hermes.Common

`Hermes.Common` contains small shared primitives used by the rest of the solution, such as result types, guard helpers, constants, and base exception types. It is intentionally low-level and must not depend on storage, sync, Google Drive, Avalonia, service hosting, or Tripous UI infrastructure.

### Hermes.Storage

`Hermes.Storage` defines the storage-provider contract used by the sync engine. It contains abstractions and models such as storage items, files, folders, changes, provider capabilities, and storage-provider interfaces. It must remain independent of Google Drive, Avalonia, service code, and UI concerns.

### Hermes.Core

`Hermes.Core` contains provider-neutral synchronization concepts such as sync planning, diff classification, conflict classification, local scanning, and sync settings. It may depend on `Hermes.Common` and `Hermes.Storage`, but it must not reference `Hermes.GoogleDrive` or contain Google-specific code.

### Hermes.GoogleDrive

`Hermes.GoogleDrive` contains the Google Drive provider implementation and all Google-specific code. It owns OAuth authentication, Drive API client wiring, Google-to-Hermes mapping, and the eventual Google Drive implementation of the storage provider. Public Hermes contracts should not expose Google API or Tripous types.

### Hermes.Data

`Hermes.Data` is the Tripous-based data and infrastructure layer for Hermes. It contains database schema registrations, registry module registrations, data modules, default database connection configuration, metadata storage, metadata synchronization sessions, and execution infrastructure.

### Hermes.Service

`Hermes.Service` is the Linux background worker service. Its role is to host the metadata synchronization loop, wire dependencies, run synchronization work, and integrate with systemd. The service should remain thin and delegate synchronization behavior to `Hermes.Data` and provider-neutral models.

### Hermes.Desktop

`Hermes.Desktop` is the final Avalonia desktop UI for end users. It is intended to show service status, activity, settings, folder selection, logs, and conflict-handling views. It should not contain sync logic directly; it should eventually communicate with the service or local API/IPC.

### TestApp

`TestApp` is the developer playground Avalonia application. It is used for manual testing of OAuth, Google Drive API contact, local folder selection, and other development experiments before those flows are moved into the final desktop UI or service. It may reference development-only infrastructure and Google Drive code more directly than production-facing projects.

### Hermes.Tests

`Hermes.Tests` is the automated unit test project. It contains xUnit tests for shared primitives, storage models, metadata storage, planning, execution, provider mapping, and service composition. It is separate from `TestApp`, which is for interactive/manual testing.

### Docs

`Docs` is a solution folder for design notes and architecture documents. It is not a compiled project, but it records decisions and research that guide the implementation.

The main dependency direction should remain:

```text
Hermes.Service -> Hermes.Data -> Hermes.Core -> Hermes.Storage -> Hermes.Common
Hermes.GoogleDrive -> Hermes.Data and Hermes.Storage
Hermes.Data -> Tripous infrastructure
Hermes.Desktop -> UI and app-host infrastructure
TestApp -> development/testing surface
```

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

## Silk Icons

The Hermes user interface includes icons from the Silk icon set 1.3.

Author: Mark James

Project: https://github.com/legacy-icons/famfamfam-silk

License: Creative Commons Attribution 2.5

License URL: http://creativecommons.org/licenses/by/2.5/
