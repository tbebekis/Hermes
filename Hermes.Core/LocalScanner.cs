// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Scans the local synchronization folder.
/// </summary>
public class LocalScanner
{
    // ● public

    /// <summary>
    /// Scans the local folder.
    /// </summary>
    public Task<Result<IReadOnlyList<string>>> ScanAsync(string LocalRootPath, CancellationToken CancellationToken)
    {
        Guard.NotNullOrWhiteSpace(LocalRootPath, nameof(LocalRootPath));
        IReadOnlyList<string> Items = Array.Empty<string>();
        return Task.FromResult(Result<IReadOnlyList<string>>.Success(Items));
    }
}
