// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Common;

/// <summary>
/// Represents the outcome of an operation.
/// </summary>
public class Result
{
    // ● private

    private readonly string fErrorText;

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    protected Result(bool Succeeded, string ErrorText)
    {
        this.Succeeded = Succeeded;
        fErrorText = ErrorText ?? string.Empty;
    }

    // ● public

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    static public Result Success()
    {
        return new Result(true, string.Empty);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    static public Result Failure(string ErrorText)
    {
        Guard.NotNullOrWhiteSpace(ErrorText, nameof(ErrorText));
        return new Result(false, ErrorText);
    }

    // ● properties

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool Failed => !Succeeded;

    /// <summary>
    /// Gets the failure text, or an empty string when the operation succeeded.
    /// </summary>
    public string ErrorText => fErrorText;
}
