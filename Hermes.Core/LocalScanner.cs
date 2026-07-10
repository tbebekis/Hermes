// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Scans the local synchronization folder.
/// </summary>
public class LocalScanner
{
    // ● private

    static string NormalizeRelativePath(string PathText)
    {
        return PathText.Replace(Path.DirectorySeparatorChar, '/');
    }
    static string GetParentRelativePath(string RelativePath)
    {
        string Parent = Path.GetDirectoryName(RelativePath);
        return string.IsNullOrWhiteSpace(Parent) ? null : NormalizeRelativePath(Parent);
    }
    static string ComputeMd5(string FilePath)
    {
        using MD5 Md5 = MD5.Create();
        using FileStream Stream = File.OpenRead(FilePath);
        byte[] Hash = Md5.ComputeHash(Stream);
        return Convert.ToHexString(Hash).ToLowerInvariant();
    }
    static LocalScanItem CreateDirectoryItem(string RootPath, string DirectoryPath)
    {
        DirectoryInfo Info = new(DirectoryPath);
        string RelativePath = NormalizeRelativePath(Path.GetRelativePath(RootPath, DirectoryPath));

        return new LocalScanItem()
        {
            RelativePath = RelativePath,
            Name = Info.Name,
            ParentRelativePath = GetParentRelativePath(RelativePath),
            ItemType = "Folder",
            ModifiedTime = Info.LastWriteTimeUtc,
        };
    }
    static LocalScanItem CreateFileItem(string RootPath, string FilePath)
    {
        FileInfo Info = new(FilePath);
        string RelativePath = NormalizeRelativePath(Path.GetRelativePath(RootPath, FilePath));

        return new LocalScanItem()
        {
            RelativePath = RelativePath,
            Name = Info.Name,
            ParentRelativePath = GetParentRelativePath(RelativePath),
            ItemType = "File",
            Size = Info.Length,
            ModifiedTime = Info.LastWriteTimeUtc,
            ContentHash = ComputeMd5(FilePath),
        };
    }

    // ● public

    /// <summary>
    /// Scans the local folder.
    /// </summary>
    public Task<Result<IReadOnlyList<LocalScanItem>>> ScanAsync(string LocalRootPath, CancellationToken CancellationToken)
    {
        Guard.NotNullOrWhiteSpace(LocalRootPath, nameof(LocalRootPath));

        if (!Directory.Exists(LocalRootPath))
            return Task.FromResult(Result<IReadOnlyList<LocalScanItem>>.Failure($"Local root folder does not exist: {LocalRootPath}"));

        List<LocalScanItem> Items = new();

        foreach (string DirectoryPath in Directory.EnumerateDirectories(LocalRootPath, "*", SearchOption.AllDirectories).OrderBy(Item => Item, StringComparer.Ordinal))
        {
            CancellationToken.ThrowIfCancellationRequested();
            Items.Add(CreateDirectoryItem(LocalRootPath, DirectoryPath));
        }

        foreach (string FilePath in Directory.EnumerateFiles(LocalRootPath, "*", SearchOption.AllDirectories).OrderBy(Item => Item, StringComparer.Ordinal))
        {
            CancellationToken.ThrowIfCancellationRequested();
            Items.Add(CreateFileItem(LocalRootPath, FilePath));
        }

        return Task.FromResult(Result<IReadOnlyList<LocalScanItem>>.Success(Items));
    }
}
