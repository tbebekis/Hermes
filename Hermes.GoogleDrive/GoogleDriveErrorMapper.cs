// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.GoogleDrive;

/// <summary>
/// Maps Google Drive API failures to provider-neutral storage errors.
/// </summary>
static public class GoogleDriveErrorMapper
{
    // ● private

    static string GetProviderErrorCode(GoogleApiException Ex)
    {
        if (Ex.Error == null)
            return string.Empty;

        if (Ex.Error.Errors != null)
        {
            foreach (SingleError Error in Ex.Error.Errors)
            {
                if (!string.IsNullOrWhiteSpace(Error.Reason))
                    return Error.Reason;
            }
        }

        return Ex.Error.Code == 0 ? string.Empty : Ex.Error.Code.ToString();
    }
    static bool HasReason(GoogleApiException Ex, string Reason)
    {
        if (Ex.Error?.Errors == null)
            return false;

        foreach (SingleError Error in Ex.Error.Errors)
        {
            if (string.Equals(Error.Reason ?? string.Empty, Reason, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
    static bool IsInvalidCheckpoint(GoogleApiException Ex, string OperationName, string Checkpoint)
    {
        if (Ex.HttpStatusCode != HttpStatusCode.BadRequest)
            return false;

        if (string.IsNullOrWhiteSpace(Checkpoint))
            return false;

        if (!string.Equals(OperationName ?? string.Empty, "ListChanges", StringComparison.OrdinalIgnoreCase))
            return false;

        return Ex.Message.Contains("Invalid Value", StringComparison.OrdinalIgnoreCase);
    }
    static bool IsRateLimit(GoogleApiException Ex)
    {
        return Ex.HttpStatusCode == (HttpStatusCode)429
            || HasReason(Ex, "rateLimitExceeded")
            || HasReason(Ex, "userRateLimitExceeded");
    }
    static bool IsTemporarilyUnavailable(GoogleApiException Ex)
    {
        return Ex.HttpStatusCode == HttpStatusCode.InternalServerError
            || Ex.HttpStatusCode == HttpStatusCode.BadGateway
            || Ex.HttpStatusCode == HttpStatusCode.ServiceUnavailable
            || Ex.HttpStatusCode == HttpStatusCode.GatewayTimeout;
    }
    static StorageErrorKind GetKind(GoogleApiException Ex, string OperationName, string Checkpoint)
    {
        if (IsInvalidCheckpoint(Ex, OperationName, Checkpoint))
            return StorageErrorKind.CheckpointInvalid;
        if (Ex.HttpStatusCode == HttpStatusCode.NotFound)
            return StorageErrorKind.NotFound;
        if (IsRateLimit(Ex))
            return StorageErrorKind.RateLimited;
        if (IsTemporarilyUnavailable(Ex))
            return StorageErrorKind.TemporarilyUnavailable;
        if (Ex.HttpStatusCode == HttpStatusCode.Unauthorized || Ex.HttpStatusCode == HttpStatusCode.Forbidden)
            return StorageErrorKind.PermissionDenied;
        if (Ex.HttpStatusCode == HttpStatusCode.Conflict || Ex.HttpStatusCode == HttpStatusCode.PreconditionFailed)
            return StorageErrorKind.Conflict;
        if (Ex.HttpStatusCode == HttpStatusCode.BadRequest)
            return StorageErrorKind.InvalidRequest;

        return StorageErrorKind.Unknown;
    }

    // ● public

    /// <summary>
    /// Maps a Google API exception to a provider-neutral storage error.
    /// </summary>
    static public StorageError Map(
        GoogleApiException Ex,
        string OperationName = "",
        string ItemId = "",
        string Checkpoint = "")
    {
        Guard.NotNull(Ex, nameof(Ex));

        StorageErrorKind Kind = GetKind(Ex, OperationName, Checkpoint);
        bool IsRetryable = Kind == StorageErrorKind.RateLimited || Kind == StorageErrorKind.TemporarilyUnavailable;

        return new StorageError(
            Kind,
            Ex.Message,
            IsRetryable,
            false,
            TimeSpan.Zero,
            "Google Drive",
            GetProviderErrorCode(Ex),
            Ex.HttpStatusCode.ToString(),
            OperationName,
            ItemId,
            Checkpoint,
            Ex);
    }
}
