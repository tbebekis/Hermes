// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Common;

/// <summary>
/// Base exception type for Hermes application errors.
/// </summary>
public class HermesException : Exception
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="HermesException"/> class.
    /// </summary>
    public HermesException(string Message)
        : base(Message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HermesException"/> class.
    /// </summary>
    public HermesException(string Message, Exception InnerException)
        : base(Message, InnerException)
    {
    }
}
