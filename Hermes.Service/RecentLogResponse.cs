// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Service;

/// <summary>
/// Response item returned for a recent log row.
/// </summary>
public class RecentLogResponse
{
    // ● public

    /// <summary>
    /// Creates a response item from a recent log record.
    /// </summary>
    static public RecentLogResponse FromRecord(RecentLogRecord Record)
    {
        Guard.NotNull(Record, nameof(Record));

        return new RecentLogResponse()
        {
            Id = Record.Id,
            LogTime = Record.LogTime,
            Level = Record.Level,
            Source = Record.Source,
            EventId = Record.EventId,
            Message = Record.Message,
        };
    }

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
