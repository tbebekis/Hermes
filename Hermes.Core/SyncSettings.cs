// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Contains synchronization settings.
/// </summary>
public class SyncSettings
{
    // ● properties

    /// <summary>
    /// Gets or sets the local root folder.
    /// </summary>
    public string LocalRootPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the remote root folder id.
    /// </summary>
    public string RemoteRootFolderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the polling interval in seconds.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 60;
}
