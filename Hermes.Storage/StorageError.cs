// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Storage;

/// <summary>
/// Represents provider-neutral storage error information.
/// </summary>
public class StorageError
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageError"/> class.
    /// </summary>
    public StorageError(StorageErrorKind Kind, string Message)
        : this(Kind, Message, false, false, TimeSpan.Zero, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, null)
    {
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="StorageError"/> class.
    /// </summary>
    public StorageError(
        StorageErrorKind Kind,
        string Message,
        bool IsRetryable,
        bool HasRetryAfter,
        TimeSpan RetryAfter,
        string ProviderName,
        string ProviderErrorCode,
        string ProviderStatusCode,
        string OperationName,
        string ItemId,
        string Checkpoint,
        Exception InnerException)
    {
        this.Kind = Kind;
        this.Message = Message ?? string.Empty;
        this.IsRetryable = IsRetryable;
        this.HasRetryAfter = HasRetryAfter;
        this.RetryAfter = RetryAfter;
        this.ProviderName = ProviderName ?? string.Empty;
        this.ProviderErrorCode = ProviderErrorCode ?? string.Empty;
        this.ProviderStatusCode = ProviderStatusCode ?? string.Empty;
        this.OperationName = OperationName ?? string.Empty;
        this.ItemId = ItemId ?? string.Empty;
        this.Checkpoint = Checkpoint ?? string.Empty;
        this.InnerException = InnerException;
    }

    // ● properties

    /// <summary>
    /// Gets the provider-neutral error kind.
    /// </summary>
    public StorageErrorKind Kind { get; }
    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; }
    /// <summary>
    /// Gets a value indicating whether the operation can be retried.
    /// </summary>
    public bool IsRetryable { get; }
    /// <summary>
    /// Gets a value indicating whether retry timing was supplied.
    /// </summary>
    public bool HasRetryAfter { get; }
    /// <summary>
    /// Gets the retry delay when supplied by the provider.
    /// </summary>
    public TimeSpan RetryAfter { get; }
    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public string ProviderName { get; }
    /// <summary>
    /// Gets the provider-specific error code.
    /// </summary>
    public string ProviderErrorCode { get; }
    /// <summary>
    /// Gets the provider-specific status code.
    /// </summary>
    public string ProviderStatusCode { get; }
    /// <summary>
    /// Gets the operation name.
    /// </summary>
    public string OperationName { get; }
    /// <summary>
    /// Gets the provider item id related to the error.
    /// </summary>
    public string ItemId { get; }
    /// <summary>
    /// Gets the checkpoint related to the error.
    /// </summary>
    public string Checkpoint { get; }
    /// <summary>
    /// Gets the provider-native exception for diagnostics.
    /// </summary>
    public Exception InnerException { get; }
}
