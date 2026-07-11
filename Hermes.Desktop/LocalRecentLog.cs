// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Recent log item returned by the local service HTTP API.
/// </summary>
public class LocalRecentLog
{
    // ● properties

    /// <summary>
    /// Gets or sets the log row id.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the log time text.
    /// </summary>
    public string LogTime { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    public string Level { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the log source.
    /// </summary>
    public string Source { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the event id.
    /// </summary>
    public string EventId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the log message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
