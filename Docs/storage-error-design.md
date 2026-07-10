# Storage Error Design

## Purpose

This document is a draft for provider-neutral storage error handling in Hermes.

Core logic must not depend on provider-native exceptions such as `GoogleApiException`. Providers should translate native failures into a small set of provider-neutral error categories and diagnostics.

## Goals

- Let Core make policy decisions without Google-specific exception handling.
- Preserve provider diagnostics for logs and troubleshooting.
- Distinguish retryable failures from permanent failures.
- Represent checkpoint invalidation explicitly.
- Avoid hiding synchronization consistency risks behind generic failures.

## Non-Goals

- This document does not define the final exception hierarchy.
- This document does not define UI text.
- This document does not implement retry policy.
- This document does not decide full checkpoint recovery behavior.

## Error Kind

Draft enum shape:

```csharp
public enum StorageErrorKind
{
    Unknown,
    NotFound,
    CheckpointInvalid,
    PermissionDenied,
    RateLimited,
    TemporarilyUnavailable,
    Conflict,
    InvalidRequest
}
```

## Error Data

Provider-neutral error data should include:

- `Kind`
- `Message`
- `IsRetryable`
- `RetryAfter`
- `ProviderName`
- `ProviderErrorCode`
- `ProviderStatusCode`
- `OperationName`
- `ItemId`
- `Checkpoint`
- `InnerException`

Not every field is meaningful for every error.

Nullable or empty field meanings:

- `RetryAfter` empty means the provider did not supply retry timing.
- `ProviderErrorCode` empty means no structured provider code was available.
- `ProviderStatusCode` empty means the error did not come from an HTTP-style provider response.
- `ItemId` empty means the error was not item-specific.
- `Checkpoint` empty means the error was not checkpoint-specific.

## Retry Semantics

`IsRetryable` is a provider recommendation, not an automatic command.

Core may still decide not to retry when:

- the sync root is disabled.
- the operation is no longer relevant.
- retry budget is exhausted.
- retry would risk duplicate side effects.

Retryable kinds:

- `RateLimited`
- `TemporarilyUnavailable`
- some `Unknown` provider failures when classified as transient.

Normally non-retryable kinds:

- `NotFound`
- `CheckpointInvalid`
- `PermissionDenied`
- `Conflict`
- `InvalidRequest`

## Checkpoint Invalid

Checkpoint invalidation is a first-class error.

Meaning:

- The provider rejected a saved incremental sync cursor.
- Hermes cannot safely continue incremental processing from that cursor.

Observed Google Drive behavior:

- `changes.list("invalid-token")`
- exception: `Google.GoogleApiException`
- HTTP status: `BadRequest`
- message: `Invalid Value`

Expected provider-neutral classification:

- `Kind`: `CheckpointInvalid`
- `IsRetryable`: `False`
- `Checkpoint`: rejected page token.
- `ProviderStatusCode`: `BadRequest`
- `ProviderErrorCode`: Google error code when available.

Core policy:

- Stop incremental remote processing for the sync root.
- Mark the sync root as requiring remote rescan.
- Do not silently request a new start page token and continue.
- Recovery must reconcile possible missed changes.

## Not Found

Meaning:

- A provider item id could not be fetched.
- For Google Drive, this can mean permanent delete or inaccessible item.

Observed Google Drive behavior:

- `files.get` after permanent delete returns HTTP `NotFound`.

Expected provider-neutral classification:

- `Kind`: `NotFound`
- `IsRetryable`: `False`
- `ItemId`: requested item id.

Core policy:

- Do not assume delete by itself.
- If the Changes API returned `Removed = true`, treat it as remote permanent-delete observation.
- If a direct fetch fails unexpectedly, compare with base and current observations before deciding recovery.

## Permission Denied

Meaning:

- Provider rejected the operation because Hermes lacks access.
- This may happen after account changes, scope changes, revoked consent, or remote sharing changes.

Expected provider-neutral classification:

- `Kind`: `PermissionDenied`
- `IsRetryable`: `False` unless re-authentication is available and explicitly requested.

Core policy:

- Pause affected sync root or operation.
- Require user action or re-authentication.

## Rate Limited

Meaning:

- Provider temporarily throttled Hermes.

Expected provider-neutral classification:

- `Kind`: `RateLimited`
- `IsRetryable`: `True`
- `RetryAfter`: provider-supplied value when available.

Core policy:

- Retry with backoff.
- Avoid marking items as failed permanently.

## Temporarily Unavailable

Meaning:

- Provider or network failure appears temporary.

Examples:

- service unavailable.
- gateway timeout.
- connection timeout.
- DNS or network outage.

Expected provider-neutral classification:

- `Kind`: `TemporarilyUnavailable`
- `IsRetryable`: `True`

Core policy:

- Retry with backoff.
- Keep checkpoint unchanged unless the failed operation already committed observations atomically.

## Conflict

Meaning:

- Provider rejected an operation because of a state conflict.

Examples:

- precondition failure.
- operation conflicts with current remote state.
- provider-side name or hierarchy rule violation.

Google Drive duplicate names are not provider conflicts because Drive allows them.

Expected provider-neutral classification:

- `Kind`: `Conflict`
- `IsRetryable`: `False` without refreshed observations.

Core policy:

- Refresh relevant state.
- Re-plan or surface conflict.

## Invalid Request

Meaning:

- Hermes sent an invalid request.
- This usually indicates a bug, stale assumptions, or invalid user-provided data.

Expected provider-neutral classification:

- `Kind`: `InvalidRequest`
- `IsRetryable`: `False`

Core policy:

- Log diagnostics.
- Do not retry blindly.

## Unknown

Meaning:

- Provider failure could not be classified.

Expected provider-neutral classification:

- `Kind`: `Unknown`
- `IsRetryable`: conservative provider decision.

Core policy:

- Log full diagnostics.
- Avoid committing checkpoints or base snapshots after unknown failure.

## Google Drive Mapping Draft

Initial mapping rules:

- HTTP `404 NotFound` from `files.get` -> `NotFound`.
- HTTP `400 BadRequest` with `Invalid Value` from `changes.list` -> `CheckpointInvalid`.
- HTTP `401 Unauthorized` -> `PermissionDenied` or re-authentication required.
- HTTP `403 Forbidden` -> `PermissionDenied`, `RateLimited`, or `InvalidRequest` depending on Google error reason.
- HTTP `429 TooManyRequests` -> `RateLimited`.
- HTTP `500`, `502`, `503`, `504` -> `TemporarilyUnavailable`.

Open question:

- Which structured Google error fields are reliably available from `GoogleApiException` in the .NET client?

## Result Integration

Hermes already has `Result` and `Result<T>` primitives.

Current implementation:

- `Hermes.Storage` adds `StorageResult<T>`.
- `StorageResult<T>` derives from `Result<T>`.
- Successful results behave like normal `Result<T>` values.
- Failed storage results carry a structured `StorageError`.
- `IStorageProvider` methods return `StorageResult<T>`.

Provider behavior:

- Providers should catch native provider exceptions.
- Providers should map native exceptions to `StorageError`.
- Native provider exceptions may remain attached as diagnostics in `StorageError.InnerException`.
- Native provider exceptions should not leak into Core policy decisions.

## Open Questions

- Should `StorageError` be a class, record, or exception?
- Should retry policy live in Core, Service, or provider layer?
- How should re-authentication be represented: `PermissionDenied`, a separate `AuthenticationRequired`, or both?
- Should `CheckpointInvalid` be a storage error kind or a sync-root state transition?
- Should provider mapping preserve raw HTTP response content for diagnostics?
- How should partial batch failures be represented when some observations were persisted and others failed?
