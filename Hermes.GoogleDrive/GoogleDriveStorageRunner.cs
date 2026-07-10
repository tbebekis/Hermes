// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.GoogleDrive;

/// <summary>
/// Runs Google Drive storage operations and maps provider exceptions to storage results.
/// </summary>
static class GoogleDriveStorageRunner
{
    // ● private

    static StorageError MapNotFound(GoogleDriveNotFoundException Ex, string OperationName)
    {
        return new StorageError(
            StorageErrorKind.NotFound,
            Ex.Message,
            false,
            false,
            TimeSpan.Zero,
            "Google Drive",
            string.Empty,
            HttpStatusCode.NotFound.ToString(),
            OperationName,
            Ex.FileId,
            string.Empty,
            Ex);
    }

    // ● public

    /// <summary>
    /// Runs a Google Drive operation.
    /// </summary>
    static public async Task<StorageResult<T>> RunAsync<T>(string OperationName, Func<Task<T>> Operation)
    {
        try
        {
            T Value = await Operation();
            return StorageResult<T>.Success(Value);
        }
        catch (GoogleDriveNotFoundException Ex)
        {
            return StorageResult<T>.Failure(MapNotFound(Ex, OperationName));
        }
        catch (GoogleApiException Ex)
        {
            return StorageResult<T>.Failure(GoogleDriveErrorMapper.Map(Ex, OperationName));
        }
    }
    /// <summary>
    /// Runs a Google Drive operation for a specific item id.
    /// </summary>
    static public async Task<StorageResult<T>> RunAsync<T>(string OperationName, string ItemId, Func<Task<T>> Operation)
    {
        try
        {
            T Value = await Operation();
            return StorageResult<T>.Success(Value);
        }
        catch (GoogleDriveNotFoundException Ex)
        {
            return StorageResult<T>.Failure(MapNotFound(Ex, OperationName));
        }
        catch (GoogleApiException Ex)
        {
            return StorageResult<T>.Failure(GoogleDriveErrorMapper.Map(Ex, OperationName, ItemId));
        }
    }
    /// <summary>
    /// Runs a Google Drive checkpoint operation.
    /// </summary>
    static public async Task<StorageResult<T>> RunCheckpointAsync<T>(string OperationName, string Checkpoint, Func<Task<T>> Operation)
    {
        try
        {
            T Value = await Operation();
            return StorageResult<T>.Success(Value);
        }
        catch (GoogleApiException Ex)
        {
            return StorageResult<T>.Failure(GoogleDriveErrorMapper.Map(Ex, OperationName, string.Empty, Checkpoint));
        }
    }
}
