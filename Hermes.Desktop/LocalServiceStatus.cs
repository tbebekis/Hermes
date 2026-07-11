// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Local service status returned by the Hermes service HTTP API.
/// </summary>
public class LocalServiceStatus
{
    // ● properties

    /// <summary>
    /// Gets or sets the service status.
    /// </summary>
    public string ServiceStatus { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the synchronization status.
    /// </summary>
    public string SynchronizationStatus { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the IPC status.
    /// </summary>
    public string IpcStatus { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the service process id.
    /// </summary>
    public int ProcessId { get; set; }
    /// <summary>
    /// Gets or sets the service version.
    /// </summary>
    public string Version { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the response timestamp in UTC.
    /// </summary>
    public DateTime TimestampUtc { get; set; }
}
