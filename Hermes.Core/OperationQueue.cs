// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Stores planned synchronization operations.
/// </summary>
public class OperationQueue
{
    // ● private

    private readonly Queue<SyncOperation> fOperations = new();

    // ● public

    /// <summary>
    /// Adds an operation to the queue.
    /// </summary>
    public void Enqueue(SyncOperation Operation)
    {
        fOperations.Enqueue(Guard.NotNull(Operation, nameof(Operation)));
    }

    /// <summary>
    /// Attempts to remove the next operation from the queue.
    /// </summary>
    public bool TryDequeue(out SyncOperation Operation)
    {
        if (fOperations.Count > 0)
        {
            Operation = fOperations.Dequeue();
            return true;
        }

        Operation = default;
        return false;
    }

    // ● properties

    /// <summary>
    /// Gets the current operation count.
    /// </summary>
    public int Count => fOperations.Count;
}
