// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests provider-neutral storage errors.
/// </summary>
public class StorageErrorTests
{
    // ● public

    /// <summary>
    /// Verifies that simple storage errors expose kind and message.
    /// </summary>
    [Fact]
    public void StorageErrorStoresKindAndMessage()
    {
        StorageError Error = new(StorageErrorKind.NotFound, "Item was not found.");

        Assert.Equal(StorageErrorKind.NotFound, Error.Kind);
        Assert.Equal("Item was not found.", Error.Message);
        Assert.False(Error.IsRetryable);
        Assert.False(Error.HasRetryAfter);
        Assert.Equal(TimeSpan.Zero, Error.RetryAfter);
        Assert.Equal(string.Empty, Error.ProviderName);
        Assert.Null(Error.InnerException);
    }

    /// <summary>
    /// Verifies that storage errors expose provider diagnostics.
    /// </summary>
    [Fact]
    public void StorageErrorStoresProviderDiagnostics()
    {
        InvalidOperationException Exception = new("Provider failure.");
        StorageError Error = new(
            StorageErrorKind.RateLimited,
            "Rate limited.",
            true,
            true,
            TimeSpan.FromSeconds(30),
            "Google Drive",
            "rateLimitExceeded",
            "429",
            "ListChanges",
            "item-id",
            "page-token",
            Exception);

        Assert.Equal(StorageErrorKind.RateLimited, Error.Kind);
        Assert.Equal("Rate limited.", Error.Message);
        Assert.True(Error.IsRetryable);
        Assert.True(Error.HasRetryAfter);
        Assert.Equal(TimeSpan.FromSeconds(30), Error.RetryAfter);
        Assert.Equal("Google Drive", Error.ProviderName);
        Assert.Equal("rateLimitExceeded", Error.ProviderErrorCode);
        Assert.Equal("429", Error.ProviderStatusCode);
        Assert.Equal("ListChanges", Error.OperationName);
        Assert.Equal("item-id", Error.ItemId);
        Assert.Equal("page-token", Error.Checkpoint);
        Assert.Same(Exception, Error.InnerException);
    }

    /// <summary>
    /// Verifies that storage results expose structured storage errors.
    /// </summary>
    [Fact]
    public void StorageResultStoresStructuredError()
    {
        StorageError Error = new(StorageErrorKind.CheckpointInvalid, "Invalid checkpoint.");

        StorageResult<string> Result = StorageResult<string>.Failure(Error);

        Assert.True(Result.Failed);
        Assert.Equal("Invalid checkpoint.", Result.ErrorText);
        Assert.Same(Error, Result.Error);
    }
    /// <summary>
    /// Verifies successful storage results expose a value and no structured error.
    /// </summary>
    [Fact]
    public void StorageResultStoresSuccessValue()
    {
        StorageResult<string> Result = StorageResult<string>.Success("value");

        Assert.True(Result.Succeeded);
        Assert.Equal("value", Result.Value);
        Assert.Equal(string.Empty, Result.ErrorText);
        Assert.Null(Result.Error);
    }
}
