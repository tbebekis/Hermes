// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Service;

/// <summary>
/// Response returned by the local service status endpoint.
/// </summary>
public class ServiceStatusResponse
{
    // ● fields

    static readonly DateTime fStartedUtc = DateTime.UtcNow;

    // ● public

    /// <summary>
    /// Creates the current service status response.
    /// </summary>
    static public ServiceStatusResponse Create(SyncRootRecord SyncRoot, SyncSettings Settings, int OpenConflictCount)
    {
        Guard.NotNull(SyncRoot, nameof(SyncRoot));
        Guard.NotNull(Settings, nameof(Settings));

        return new ServiceStatusResponse()
        {
            ServiceStatus = "Running",
            SynchronizationStatus = "Unknown",
            IpcStatus = "Connected",
            ProcessId = Environment.ProcessId,
            Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? string.Empty,
            StartedUtc = fStartedUtc,
            UptimeSeconds = (int)(DateTime.UtcNow - fStartedUtc).TotalSeconds,
            SyncRootId = SyncRoot.Id,
            ProviderName = SyncRoot.ProviderName,
            LocalRootPath = SyncRoot.LocalRootPath,
            RemoteRootItemId = SyncRoot.RemoteRootItemId ?? string.Empty,
            SyncRootEnabled = SyncRoot.IsEnabled,
            MutationsEnabled = Settings.EnableMutations,
            PollingIntervalSeconds = Settings.PollingIntervalSeconds,
            OpenConflictCount = OpenConflictCount,
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
    /// Gets or sets the service start time in UTC.
    /// </summary>
    public DateTime StartedUtc { get; set; }
    /// <summary>
    /// Gets or sets the service uptime in seconds.
    /// </summary>
    public int UptimeSeconds { get; set; }
    /// <summary>
    /// Gets or sets the configured sync root id.
    /// </summary>
    public string SyncRootId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the local root path.
    /// </summary>
    public string LocalRootPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the remote root item id.
    /// </summary>
    public string RemoteRootItemId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether the sync root is enabled.
    /// </summary>
    public bool SyncRootEnabled { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether endpoint mutations are enabled.
    /// </summary>
    public bool MutationsEnabled { get; set; }
    /// <summary>
    /// Gets or sets the polling interval in seconds.
    /// </summary>
    public int PollingIntervalSeconds { get; set; }
    /// <summary>
    /// Gets or sets the current durable open conflict count.
    /// </summary>
    public int OpenConflictCount { get; set; }
    /// <summary>
    /// Gets or sets the response timestamp in UTC.
    /// </summary>
    public DateTime TimestampUtc { get; set; }
}
