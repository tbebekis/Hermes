// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Service;

/// <summary>
/// Response returned by the local service status endpoint.
/// </summary>
public class ServiceStatusResponse
{
    // ● public

    /// <summary>
    /// Creates the current service status response.
    /// </summary>
    static public ServiceStatusResponse Create()
    {
        return new ServiceStatusResponse()
        {
            ServiceStatus = "Running",
            SynchronizationStatus = "Unknown",
            IpcStatus = "Connected",
            ProcessId = Environment.ProcessId,
            Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? string.Empty,
            TimestampUtc = DateTime.UtcNow,
        };
    }

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
    /// Gets or sets the local IPC status.
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
