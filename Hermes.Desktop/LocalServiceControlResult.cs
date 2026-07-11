// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Local service control command result.
/// </summary>
public class LocalServiceControlResult
{
    // ● public

    /// <summary>
    /// Creates a successful command result.
    /// </summary>
    static public LocalServiceControlResult Success(string Message)
    {
        return new LocalServiceControlResult()
        {
            Succeeded = true,
            Message = Message ?? string.Empty,
        };
    }
    /// <summary>
    /// Creates a failed command result.
    /// </summary>
    static public LocalServiceControlResult Failure(string Message)
    {
        return new LocalServiceControlResult()
        {
            Succeeded = false,
            Message = Message ?? string.Empty,
        };
    }

    // ● properties

    /// <summary>
    /// Gets or sets a value indicating whether the command succeeded.
    /// </summary>
    public bool Succeeded { get; set; }
    /// <summary>
    /// Gets or sets the command result message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
