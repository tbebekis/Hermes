// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests Google Drive storage error mapping.
/// </summary>
public class GoogleDriveErrorMapperTests
{
    // ● private

    static GoogleApiException CreateException(HttpStatusCode StatusCode, string Message)
    {
        return new GoogleApiException("drive", Message)
        {
            HttpStatusCode = StatusCode
        };
    }
    static GoogleApiException CreateException(HttpStatusCode StatusCode, string Message, string Reason)
    {
        GoogleApiException Ex = CreateException(StatusCode, Message);
        Ex.Error = new RequestError
        {
            Code = (int)StatusCode,
            Message = Message,
            Errors = new List<SingleError>
            {
                new()
                {
                    Reason = Reason,
                    Message = Message
                }
            }
        };
        return Ex;
    }

    // ● public

    /// <summary>
    /// Verifies that invalid changes tokens map to checkpoint invalid.
    /// </summary>
    [Fact]
    public void MapClassifiesInvalidChangesToken()
    {
        GoogleApiException Ex = CreateException(HttpStatusCode.BadRequest, "Invalid Value");

        StorageError Error = GoogleDriveErrorMapper.Map(Ex, "ListChanges", string.Empty, "invalid-token");

        Assert.Equal(StorageErrorKind.CheckpointInvalid, Error.Kind);
        Assert.False(Error.IsRetryable);
        Assert.Equal("Google Drive", Error.ProviderName);
        Assert.Equal("BadRequest", Error.ProviderStatusCode);
        Assert.Equal("ListChanges", Error.OperationName);
        Assert.Equal("invalid-token", Error.Checkpoint);
        Assert.Same(Ex, Error.InnerException);
    }

    /// <summary>
    /// Verifies that not found maps to not found.
    /// </summary>
    [Fact]
    public void MapClassifiesNotFound()
    {
        GoogleApiException Ex = CreateException(HttpStatusCode.NotFound, "File not found.");

        StorageError Error = GoogleDriveErrorMapper.Map(Ex, "GetItem", "item-id");

        Assert.Equal(StorageErrorKind.NotFound, Error.Kind);
        Assert.False(Error.IsRetryable);
        Assert.Equal("item-id", Error.ItemId);
        Assert.Equal("NotFound", Error.ProviderStatusCode);
    }

    /// <summary>
    /// Verifies that Google rate limit reasons map to rate limited.
    /// </summary>
    [Fact]
    public void MapClassifiesRateLimitReason()
    {
        GoogleApiException Ex = CreateException(HttpStatusCode.Forbidden, "Rate limit exceeded.", "rateLimitExceeded");

        StorageError Error = GoogleDriveErrorMapper.Map(Ex, "ListChanges");

        Assert.Equal(StorageErrorKind.RateLimited, Error.Kind);
        Assert.True(Error.IsRetryable);
        Assert.Equal("rateLimitExceeded", Error.ProviderErrorCode);
        Assert.Equal("Forbidden", Error.ProviderStatusCode);
    }
}
