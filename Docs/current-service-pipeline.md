# Current Service Pipeline

## Purpose

This document records the service pipeline that is currently active in Hermes and separates it from older Core synchronization scaffolding.

It is intended as a temporary cleanup aid. If the legacy path is removed safely, this document can be removed too.

## Active Service Path

The current background service path is:

```text
Program
    -> Worker
    -> MetadataSyncLoop
    -> IMetadataSyncRunner
    -> MetadataSyncRunner
    -> MetadataSyncSession
    -> SqlMetadataStore
    -> SyncPlanner
    -> ISyncExecutor
```

The active service composition is registered through:

```text
ServiceRegistrations.AddHermesServiceServices(...)
```

The active host construction entry point is:

```text
Program.CreateHostBuilder(...)
```

## Active Responsibilities

- `Program` creates and runs the service host.
- `Worker` adapts the hosted-service lifecycle to the metadata sync loop.
- `MetadataSyncLoop` handles polling, pass execution, logging, and graceful shutdown.
- `MetadataSyncRunner` decides whether to bootstrap from a full remote snapshot or continue from remote changes.
- `MetadataSyncSession` imports observations, creates planner decisions, executes pending requests, and commits verified results.
- `SqlMetadataStore` is the current synchronization memory.
- `SyncMutationExecutorBase` is the current mutation executor registered by the service.

## Legacy Core Path

The following Core classes are not part of the current service runtime path:

- `SyncService`
- `SyncEngine`
- `MetadataStore`
- `OperationQueue`
- `SyncOperation`
- `SyncOperationType`

They appear to belong to an earlier synchronization engine shape based on an operation queue and a simple drive start page token store.

## Cleanup Decision

Before deleting the legacy path, verify:

- No service registration resolves these types.
- No production code constructs these types.
- Existing tests that cover only these legacy types are either removed with them or replaced by tests for the active metadata pipeline.
- Public API removal is acceptable for the current project phase.

If those checks pass, the legacy Core path can be removed and this document can be deleted.
