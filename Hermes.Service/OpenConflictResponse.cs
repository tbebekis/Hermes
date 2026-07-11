// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Service;

/// <summary>
/// Response item returned for an open synchronization conflict.
/// </summary>
public class OpenConflictResponse
{
    // ● private

    static string ResolveLocalPath(SyncConflictDetailRecord Detail)
    {
        if (!string.IsNullOrWhiteSpace(Detail.LocalObservation?.RelativePath))
            return Detail.LocalObservation.RelativePath;

        if (!string.IsNullOrWhiteSpace(Detail.BaseSnapshot?.LocalRelativePath))
            return Detail.BaseSnapshot.LocalRelativePath;

        return string.Empty;
    }
    static string ResolveRemoteName(SyncConflictDetailRecord Detail)
    {
        if (!string.IsNullOrWhiteSpace(Detail.RemoteObservation?.Name))
            return Detail.RemoteObservation.Name;

        if (!string.IsNullOrWhiteSpace(Detail.BaseSnapshot?.Name))
            return Detail.BaseSnapshot.Name;

        return string.Empty;
    }

    // ● public

    /// <summary>
    /// Creates a response item from a metadata conflict detail record.
    /// </summary>
    static public OpenConflictResponse FromDetail(SyncConflictDetailRecord Detail)
    {
        Guard.NotNull(Detail, nameof(Detail));
        Guard.NotNull(Detail.Conflict, nameof(Detail.Conflict));

        return new OpenConflictResponse()
        {
            Id = Detail.Conflict.Id,
            TrackedItemId = Detail.Conflict.TrackedItemId,
            DiffKind = Detail.Conflict.DiffKind.ToString(),
            DecisionKind = Detail.Conflict.DecisionKind.ToString(),
            Message = Detail.Conflict.Message,
            LocalPath = ResolveLocalPath(Detail),
            RemoteName = ResolveRemoteName(Detail),
            LastObservedTime = Detail.Conflict.LastObservedTime,
        };
    }

    // ● properties

    /// <summary>
    /// Gets or sets the conflict id.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the tracked item id.
    /// </summary>
    public string TrackedItemId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the diff kind.
    /// </summary>
    public string DiffKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the decision kind.
    /// </summary>
    public string DecisionKind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the conflict message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the local relative path.
    /// </summary>
    public string LocalPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the remote item name.
    /// </summary>
    public string RemoteName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the last observed time.
    /// </summary>
    public DateTime LastObservedTime { get; set; }
}
