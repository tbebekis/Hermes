// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Storage;

/// <summary>
/// Represents the provider-neutral outcome of a storage operation.
/// </summary>
public class StorageResult<T> : Result<T>
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageResult{T}"/> class.
    /// </summary>
    protected StorageResult(bool Succeeded, T Value, string ErrorText, StorageError Error)
        : base(Succeeded, Value, ErrorText)
    {
        this.Error = Error;
    }

    // ● public

    /// <summary>
    /// Creates a successful storage result.
    /// </summary>
    static public new StorageResult<T> Success(T Value)
    {
        return new StorageResult<T>(true, Value, string.Empty, null);
    }
    /// <summary>
    /// Creates a failed storage result.
    /// </summary>
    static public StorageResult<T> Failure(StorageError Error)
    {
        Guard.NotNull(Error, nameof(Error));
        return new StorageResult<T>(false, default, Error.Message, Error);
    }

    // ● properties

    /// <summary>
    /// Gets the structured storage error when the operation failed.
    /// </summary>
    public StorageError Error { get; }
}
