// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Common;

/// <summary>
/// Provides argument validation helpers.
/// </summary>
static public class Guard
{
    // ● public

    /// <summary>
    /// Ensures that a string value is not null, empty, or whitespace.
    /// </summary>
    static public string NotNullOrWhiteSpace(string Value, string ParameterName)
    {
        if (string.IsNullOrWhiteSpace(Value))
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", ParameterName);

        return Value;
    }

    /// <summary>
    /// Ensures that an object value is not null.
    /// </summary>
    static public T NotNull<T>(T Value, string ParameterName)
    {
        if (Value == null)
            throw new ArgumentNullException(ParameterName);

        return Value;
    }
}
