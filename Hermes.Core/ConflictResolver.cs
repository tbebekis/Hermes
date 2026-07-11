// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Detects synchronization conflicts.
/// </summary>
public class ConflictResolver
{
    // ● public

    /// <summary>
    /// Determines whether a conflict exists for legacy boolean change flags.
    /// </summary>
    public bool HasConflict(bool LocalChanged, bool RemoteChanged)
    {
        return LocalChanged && RemoteChanged;
    }
    /// <summary>
    /// Determines whether the specified diff input classifies as a conflict.
    /// </summary>
    public bool HasConflict(SyncDiffInput Input)
    {
        Guard.NotNull(Input, nameof(Input));

        SyncDiffClassifier Classifier = new();
        return Classifier.Classify(Input) == SyncDiffKind.Conflict;
    }
}
