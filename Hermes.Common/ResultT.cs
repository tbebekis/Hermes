// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Common;

/// <summary>
/// Represents the outcome of an operation that returns a value.
/// </summary>
public class Result<T> : Result
{
    // ● private

    private readonly T fValue;

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    protected Result(bool Succeeded, T Value, string ErrorText)
        : base(Succeeded, ErrorText)
    {
        fValue = Value;
    }

    // ● public

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    static public Result<T> Success(T Value)
    {
        return new Result<T>(true, Value, string.Empty);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    static public new Result<T> Failure(string ErrorText)
    {
        Guard.NotNullOrWhiteSpace(ErrorText, nameof(ErrorText));
        return new Result<T>(false, default, ErrorText);
    }

    // ● properties

    /// <summary>
    /// Gets the operation value.
    /// </summary>
    public T Value => fValue;
}
