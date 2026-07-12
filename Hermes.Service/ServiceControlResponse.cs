// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Service;

/// <summary>
/// Response returned by service control commands.
/// </summary>
public class ServiceControlResponse
{
    // ● public

    /// <summary>
    /// Creates a successful service control response.
    /// </summary>
    static public ServiceControlResponse Success(string Message)
    {
        return new ServiceControlResponse()
        {
            Succeeded = true,
            Message = Message ?? string.Empty,
            TimestampUtc = DateTime.UtcNow,
        };
    }
    /// <summary>
    /// Creates a failed service control response.
    /// </summary>
    static public ServiceControlResponse Failure(string Message)
    {
        return new ServiceControlResponse()
        {
            Succeeded = false,
            Message = Message ?? string.Empty,
            TimestampUtc = DateTime.UtcNow,
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
    /// <summary>
    /// Gets or sets the response timestamp in UTC.
    /// </summary>
    public DateTime TimestampUtc { get; set; }
}
