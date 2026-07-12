// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tools;

/// <summary>
/// Console diagnostics and test harness for Hermes synchronization scenarios.
/// </summary>
static public class Program
{
    // ● private

    const string ServiceBaseUrl = "http://127.0.0.1:8765";
    static readonly JsonSerializerOptions fJsonOptions = new() { PropertyNameCaseInsensitive = true };

    static string ConfigFolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CommonConstants.ApplicationName);
    static string DatabasePath => Path.Combine(ConfigFolderPath, "Data", "Hermes.db3");
    static string ServiceSettingsPath => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Hermes.Service", "appsettings.Development.json"));
    static void ConfigureSystem()
    {
        SysConfig.ApplicationMode = ApplicationMode.Service;
        SysConfig.MainAssembly = typeof(Program).Assembly;
        SysConfig.AppName = CommonConstants.ApplicationName;
        SysConfig.AppFolderPath = ConfigFolderPath;
        SysConfig.AppDataFolderPath = ConfigFolderPath;
        SysConfig.AppTempFolderPath = Path.Combine(Path.GetTempPath(), CommonConstants.ApplicationName);
    }
    static SyncSettings LoadSettings()
    {
        if (!File.Exists(ServiceSettingsPath))
            throw new FileNotFoundException("Service development settings file was not found.", ServiceSettingsPath);

        using FileStream Stream = File.OpenRead(ServiceSettingsPath);
        using JsonDocument Document = JsonDocument.Parse(Stream);
        JsonElement Sync = Document.RootElement.GetProperty("Sync");

        return new SyncSettings()
        {
            SyncRootId = Sync.GetProperty("SyncRootId").GetString() ?? "default",
            LocalRootPath = Sync.GetProperty("LocalRootPath").GetString() ?? string.Empty,
            RemoteRootFolderId = Sync.GetProperty("RemoteRootFolderId").GetString() ?? "root",
            PollingIntervalSeconds = Sync.GetProperty("PollingIntervalSeconds").GetInt32(),
            EnableMutations = Sync.GetProperty("EnableMutations").GetBoolean(),
        };
    }
    static async Task<GoogleDriveClient> CreateDriveClientAsync(CancellationToken CancellationToken)
    {
        ConfigureSystem();
        GoogleDriveClient Client = new(new GoogleDriveAuthManager(), new GoogleDriveMapper());
        await Client.AuthenticateAsync(CancellationToken);
        return Client;
    }
    static string ItemKindText(StorageItem Item) => Item.IsFolder ? "Folder" : "File";
    static string RelativePath(string ParentPath, StorageItem Item)
    {
        if (string.IsNullOrWhiteSpace(ParentPath))
            return Item.Name;

        return ParentPath + "/" + Item.Name;
    }
    static async Task PrintDriveTreeAsync(GoogleDriveClient Client, string FolderId, string FolderPath, CancellationToken CancellationToken)
    {
        IReadOnlyList<StorageItem> Items = await Client.ListFolderAsync(FolderId, CancellationToken);

        foreach (StorageItem Item in Items.OrderBy(Item => Item.Name, StringComparer.Ordinal))
        {
            string PathText = RelativePath(FolderPath, Item);
            Console.WriteLine($"{PathText}\t{ItemKindText(Item)}\tid={Item.Id}\tparent={Item.ParentId}\ttrashed={Item.Trashed}\tsize={Item.Size}\thash={Item.Md5Hash}");

            if (Item.IsFolder)
                await PrintDriveTreeAsync(Client, Item.Id, PathText, CancellationToken);
        }
    }
    static async Task<StorageItem> FindDriveItemByPathAsync(GoogleDriveClient Client, string RootFolderId, string PathText, CancellationToken CancellationToken)
    {
        string[] Parts = PathText.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string ParentId = RootFolderId;
        StorageItem Current = null;

        foreach (string Part in Parts)
        {
            IReadOnlyList<StorageItem> Children = await Client.ListFolderAsync(ParentId, CancellationToken);
            Current = Children.SingleOrDefault(Item => string.Equals(Item.Name, Part, StringComparison.Ordinal));

            if (Current == null)
                return null;

            ParentId = Current.Id;
        }

        return Current;
    }
    static async Task<StorageItem> EnsureDriveFolderAsync(GoogleDriveClient Client, string RootFolderId, string PathText, CancellationToken CancellationToken)
    {
        string[] Parts = PathText.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string ParentId = RootFolderId;
        StorageItem Current = null;

        foreach (string Part in Parts)
        {
            IReadOnlyList<StorageItem> Children = await Client.ListFolderAsync(ParentId, CancellationToken);
            Current = Children.SingleOrDefault(Item => string.Equals(Item.Name, Part, StringComparison.Ordinal) && Item.IsFolder);

            if (Current == null)
                Current = await Client.CreateFolderAsync(Part, ParentId, CancellationToken);

            ParentId = Current.Id;
        }

        return Current;
    }
    static async Task<string> GetDriveFolderIdAsync(GoogleDriveClient Client, string RootFolderId, string PathText, CancellationToken CancellationToken)
    {
        if (string.IsNullOrWhiteSpace(PathText) || string.Equals(PathText, ".", StringComparison.Ordinal))
            return RootFolderId;

        StorageItem Folder = await EnsureDriveFolderAsync(Client, RootFolderId, PathText, CancellationToken);
        return Folder.Id;
    }
    static string ParentPath(string PathText)
    {
        int Index = PathText.LastIndexOf('/');
        return Index < 0 ? string.Empty : PathText[..Index];
    }
    static string FileName(string PathText)
    {
        int Index = PathText.LastIndexOf('/');
        return Index < 0 ? PathText : PathText[(Index + 1)..];
    }
    static async Task MoveDriveItemAsync(SyncSettings Settings, string SourcePath, string TargetFolderPath, CancellationToken CancellationToken)
    {
        GoogleDriveClient Client = await CreateDriveClientAsync(CancellationToken);
        string RootFolderId = ResolveRemoteRootItemId(Settings);
        StorageItem Source = await FindDriveItemByPathAsync(Client, RootFolderId, SourcePath, CancellationToken);

        if (Source == null)
            throw new InvalidOperationException("Drive source path was not found: " + SourcePath);

        string TargetFolderId = await GetDriveFolderIdAsync(Client, RootFolderId, TargetFolderPath, CancellationToken);
        StorageItem Moved = await Client.MoveFileAsync(Source.Id, Source.ParentId, TargetFolderId, CancellationToken);
        Console.WriteLine($"{SourcePath} -> {TargetFolderPath}/{Source.Name}\tid={Moved.Id}\tparent={Moved.ParentId}");
    }
    static async Task RenameDriveItemAsync(SyncSettings Settings, string PathText, string NewName, CancellationToken CancellationToken)
    {
        GoogleDriveClient Client = await CreateDriveClientAsync(CancellationToken);
        StorageItem Item = await FindDriveItemByPathAsync(Client, ResolveRemoteRootItemId(Settings), PathText, CancellationToken);

        if (Item == null)
            throw new InvalidOperationException("Drive path was not found: " + PathText);

        StorageItem Renamed = await Client.RenameFileAsync(Item.Id, NewName, CancellationToken);
        Console.WriteLine($"{PathText} -> {NewName}\tid={Renamed.Id}");
    }
    static async Task RenameDriveItemByIdAsync(string RemoteItemId, string NewName, CancellationToken CancellationToken)
    {
        GoogleDriveClient Client = await CreateDriveClientAsync(CancellationToken);
        StorageItem Renamed = await Client.RenameFileAsync(RemoteItemId, NewName, CancellationToken);
        Console.WriteLine($"{RemoteItemId} -> {NewName}\tid={Renamed.Id}");
    }
    static async Task CreateDriveFolderAsync(SyncSettings Settings, string PathText, CancellationToken CancellationToken)
    {
        GoogleDriveClient Client = await CreateDriveClientAsync(CancellationToken);
        StorageItem Folder = await EnsureDriveFolderAsync(Client, ResolveRemoteRootItemId(Settings), PathText, CancellationToken);
        Console.WriteLine($"{PathText}\tid={Folder.Id}");
    }
    static async Task CreateDuplicateDriveFolderAsync(SyncSettings Settings, string PathText, CancellationToken CancellationToken)
    {
        GoogleDriveClient Client = await CreateDriveClientAsync(CancellationToken);
        string RootFolderId = ResolveRemoteRootItemId(Settings);
        string FolderId = await GetDriveFolderIdAsync(Client, RootFolderId, ParentPath(PathText), CancellationToken);
        StorageItem Folder = await Client.CreateFolderAsync(FileName(PathText), FolderId, CancellationToken);
        Console.WriteLine($"{PathText}\tid={Folder.Id}");
    }
    static async Task RestoreDriveItemAsync(string RemoteItemId, CancellationToken CancellationToken)
    {
        GoogleDriveClient Client = await CreateDriveClientAsync(CancellationToken);
        StorageItem Restored = await Client.RestoreFileAsync(RemoteItemId, CancellationToken);
        Console.WriteLine($"{RemoteItemId}\trestored={(!Restored.Trashed).ToString()}\tname={Restored.Name}");
    }
    static async Task EmptyDriveTrashAsync(CancellationToken CancellationToken)
    {
        GoogleDriveClient Client = await CreateDriveClientAsync(CancellationToken);
        await Client.EmptyTrashAsync(CancellationToken);
        Console.WriteLine("DRIVE_EMPTY_TRASH\tOK");
    }
    static async Task TrashDriveItemAsync(SyncSettings Settings, string PathText, CancellationToken CancellationToken)
    {
        GoogleDriveClient Client = await CreateDriveClientAsync(CancellationToken);
        StorageItem Item = await FindDriveItemByPathAsync(Client, ResolveRemoteRootItemId(Settings), PathText, CancellationToken);

        if (Item == null)
            throw new InvalidOperationException("Drive path was not found: " + PathText);

        StorageItem Trashed = await Client.TrashFileAsync(Item.Id, CancellationToken);
        Console.WriteLine($"{PathText}\ttrashed={Trashed.Trashed}\tid={Trashed.Id}");
    }
    static async Task DeleteDriveItemAsync(SyncSettings Settings, string PathText, CancellationToken CancellationToken)
    {
        GoogleDriveClient Client = await CreateDriveClientAsync(CancellationToken);
        StorageItem Item = await FindDriveItemByPathAsync(Client, ResolveRemoteRootItemId(Settings), PathText, CancellationToken);

        if (Item == null)
            throw new InvalidOperationException("Drive path was not found: " + PathText);

        await Client.DeleteFileAsync(Item.Id, CancellationToken);
        Console.WriteLine($"{PathText}\tdeleted=True\tid={Item.Id}");
    }
    static async Task WriteDriveFileAsync(SyncSettings Settings, string PathText, string Text, CancellationToken CancellationToken)
    {
        GoogleDriveClient Client = await CreateDriveClientAsync(CancellationToken);
        string RootFolderId = ResolveRemoteRootItemId(Settings);
        string FolderId = await GetDriveFolderIdAsync(Client, RootFolderId, ParentPath(PathText), CancellationToken);
        StorageItem Existing = await FindDriveItemByPathAsync(Client, RootFolderId, PathText, CancellationToken);
        string TempFilePath = Path.Combine(Path.GetTempPath(), CommonConstants.ApplicationName, Guid.NewGuid().ToString("N") + ".txt");

        WriteTextFile(TempFilePath, Text);

        try
        {
            StorageItem Item = Existing == null
                ? await Client.UploadFileAsync(TempFilePath, FolderId, CancellationToken)
                : await Client.UpdateFileContentAsync(Existing.Id, TempFilePath, CancellationToken);

            if (!string.Equals(Item.Name, FileName(PathText), StringComparison.Ordinal))
                Item = await Client.RenameFileAsync(Item.Id, FileName(PathText), CancellationToken);

            Console.WriteLine($"{PathText}\tid={Item.Id}\thash={Item.Md5Hash}");
        }
        finally
        {
            if (File.Exists(TempFilePath))
                File.Delete(TempFilePath);
        }
    }
    static async Task WriteDuplicateDriveFileAsync(SyncSettings Settings, string PathText, string Text, CancellationToken CancellationToken)
    {
        GoogleDriveClient Client = await CreateDriveClientAsync(CancellationToken);
        string RootFolderId = ResolveRemoteRootItemId(Settings);
        string FolderId = await GetDriveFolderIdAsync(Client, RootFolderId, ParentPath(PathText), CancellationToken);
        string TempFolderPath = Path.Combine(Path.GetTempPath(), CommonConstants.ApplicationName, Guid.NewGuid().ToString("N"));
        string TempFilePath = Path.Combine(TempFolderPath, FileName(PathText));

        WriteTextFile(TempFilePath, Text);

        try
        {
            StorageItem Item = await Client.UploadFileAsync(TempFilePath, FolderId, CancellationToken);
            Console.WriteLine($"{PathText}\tid={Item.Id}\thash={Item.Md5Hash}");
        }
        finally
        {
            if (Directory.Exists(TempFolderPath))
                Directory.Delete(TempFolderPath, true);
        }
    }
    static async Task WriteDriveFileInFolderIdAsync(string ParentFolderId, string Name, string Text, CancellationToken CancellationToken)
    {
        GoogleDriveClient Client = await CreateDriveClientAsync(CancellationToken);
        string TempFolderPath = Path.Combine(Path.GetTempPath(), CommonConstants.ApplicationName, Guid.NewGuid().ToString("N"));
        string TempFilePath = Path.Combine(TempFolderPath, Name);

        WriteTextFile(TempFilePath, Text);

        try
        {
            StorageItem Item = await Client.UploadFileAsync(TempFilePath, ParentFolderId, CancellationToken);
            Console.WriteLine($"{Name}\tid={Item.Id}\tparent={Item.ParentId}\thash={Item.Md5Hash}");
        }
        finally
        {
            if (Directory.Exists(TempFolderPath))
                Directory.Delete(TempFolderPath, true);
        }
    }
    static async Task CleanDriveFolderAsync(GoogleDriveClient Client, string FolderId, CancellationToken CancellationToken)
    {
        IReadOnlyList<StorageItem> Items = await Client.ListFolderAsync(FolderId, CancellationToken);

        foreach (StorageItem Item in Items)
        {
            if (Item.IsFolder)
                await CleanDriveFolderAsync(Client, Item.Id, CancellationToken);

            await Client.TrashFileAsync(Item.Id, CancellationToken);
            Console.WriteLine("DRIVE_TRASH\t" + Item.Name + "\tid=" + Item.Id);
        }
    }
    static async Task ResetTestStateAsync(SyncSettings Settings, CancellationToken CancellationToken)
    {
        GoogleDriveClient Client = await CreateDriveClientAsync(CancellationToken);
        string RootFolderId = ResolveRemoteRootItemId(Settings);

        await CleanDriveFolderAsync(Client, RootFolderId, CancellationToken);
        await Client.EmptyTrashAsync(CancellationToken);
        Console.WriteLine("DRIVE_EMPTY_TRASH\tOK");

        EnsureCleanDirectory(Settings.LocalRootPath);
        Console.WriteLine("LOCAL_CLEAN\t" + Settings.LocalRootPath);

        if (File.Exists(DatabasePath))
        {
            File.Delete(DatabasePath);
            Console.WriteLine("DB_DELETE\t" + DatabasePath);
        }

        await VerifyEmptyAsync(Settings, CancellationToken);
    }
    static async Task<List<string>> GetDrivePathsAsync(GoogleDriveClient Client, string FolderId, string FolderPath, CancellationToken CancellationToken)
    {
        List<string> Result = new();
        IReadOnlyList<StorageItem> Items = await Client.ListFolderAsync(FolderId, CancellationToken);

        foreach (StorageItem Item in Items.OrderBy(Item => Item.Name, StringComparer.Ordinal))
        {
            string PathText = RelativePath(FolderPath, Item);
            Result.Add(PathText);

            if (Item.IsFolder)
                Result.AddRange(await GetDrivePathsAsync(Client, Item.Id, PathText, CancellationToken));
        }

        return Result;
    }
    static IReadOnlyList<string> GetLocalPaths(string LocalRootPath)
    {
        if (!Directory.Exists(LocalRootPath))
            return [];

        LocalScanner Scanner = new();
        Result<IReadOnlyList<LocalScanItem>> Result = Scanner.ScanAsync(LocalRootPath, CancellationToken.None).GetAwaiter().GetResult();
        return Result.Failed
            ? []
            : Result.Value.Select(Item => Item.RelativePath).OrderBy(Item => Item, StringComparer.Ordinal).ToList();
    }
    static void PrintLocalTree(string LocalRootPath)
    {
        if (!Directory.Exists(LocalRootPath))
        {
            Console.WriteLine("Local root folder does not exist: " + LocalRootPath);
            return;
        }

        LocalScanner Scanner = new();
        Result<IReadOnlyList<LocalScanItem>> Result = Scanner.ScanAsync(LocalRootPath, CancellationToken.None).GetAwaiter().GetResult();

        if (Result.Failed)
        {
            Console.WriteLine(Result.ErrorText);
            return;
        }

        foreach (LocalScanItem Item in Result.Value.OrderBy(Item => Item.RelativePath, StringComparer.Ordinal))
            Console.WriteLine($"{Item.RelativePath}\t{Item.ItemType}\tsize={Item.Size?.ToString() ?? string.Empty}\thash={Item.ContentHash ?? string.Empty}");
    }
    static void EnsureCleanDirectory(string FolderPath)
    {
        if (Directory.Exists(FolderPath))
            Directory.Delete(FolderPath, true);

        Directory.CreateDirectory(FolderPath);
    }
    static void WriteTextFile(string FilePath, string Text)
    {
        string FolderPath = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(FolderPath))
            Directory.CreateDirectory(FolderPath);

        File.WriteAllText(FilePath, Text);
    }
    static void SeedLocalTree(string LocalRootPath)
    {
        EnsureCleanDirectory(LocalRootPath);
        Directory.CreateDirectory(Path.Combine(LocalRootPath, "local-tree", "docs", "deep"));
        WriteTextFile(Path.Combine(LocalRootPath, "local-tree", "a.txt"), "a");
        WriteTextFile(Path.Combine(LocalRootPath, "local-tree", "docs", "b.txt"), "b");
        WriteTextFile(Path.Combine(LocalRootPath, "local-tree", "docs", "deep", "c.txt"), "c");
    }
    static void CreateLocalFolder(SyncSettings Settings, string PathText)
    {
        string FolderPath = Path.Combine(Settings.LocalRootPath, PathText.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(FolderPath);
        Console.WriteLine("LOCAL_MKDIR\t" + PathText);
    }
    static void WriteLocalFile(SyncSettings Settings, string PathText, string Text)
    {
        string FilePath = Path.Combine(Settings.LocalRootPath, PathText.Replace('/', Path.DirectorySeparatorChar));
        WriteTextFile(FilePath, Text);
        Console.WriteLine("LOCAL_WRITE\t" + PathText);
    }
    static void MoveLocalItem(SyncSettings Settings, string SourcePath, string TargetPath)
    {
        string Source = Path.Combine(Settings.LocalRootPath, SourcePath.Replace('/', Path.DirectorySeparatorChar));
        string Target = Path.Combine(Settings.LocalRootPath, TargetPath.Replace('/', Path.DirectorySeparatorChar));
        string FolderPath = Path.GetDirectoryName(Target);

        if (!string.IsNullOrWhiteSpace(FolderPath))
            Directory.CreateDirectory(FolderPath);

        if (File.Exists(Source))
            File.Move(Source, Target);
        else if (Directory.Exists(Source))
            Directory.Move(Source, Target);
        else
            throw new FileNotFoundException("Local source path was not found.", Source);

        Console.WriteLine("LOCAL_MOVE\t" + SourcePath + "\t" + TargetPath);
    }
    static void DeleteLocalItem(SyncSettings Settings, string PathText)
    {
        string LocalPath = Path.Combine(Settings.LocalRootPath, PathText.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(LocalPath))
            File.Delete(LocalPath);
        else if (Directory.Exists(LocalPath))
            Directory.Delete(LocalPath, true);
        else
            throw new FileNotFoundException("Local path was not found.", LocalPath);

        Console.WriteLine("LOCAL_DELETE\t" + PathText);
    }
    static SQLiteConnection OpenDatabase()
    {
        if (!File.Exists(DatabasePath))
            throw new FileNotFoundException("Hermes database was not found.", DatabasePath);

        SQLiteConnection Connection = new("Data Source=\"" + DatabasePath + "\"");
        Connection.Open();
        return Connection;
    }
    static string LoadDatabaseRemoteRootItemId(string SyncRootId)
    {
        if (!File.Exists(DatabasePath))
            return string.Empty;

        using SQLiteConnection Connection = OpenDatabase();
        using SQLiteCommand Command = Connection.CreateCommand();
        Command.CommandText = "select RemoteRootItemId from SYNC_ROOT where Id = @Id";
        Command.Parameters.AddWithValue("@Id", SyncRootId);
        object Value = Command.ExecuteScalar();
        return Value == null || Value == DBNull.Value ? string.Empty : Convert.ToString(Value) ?? string.Empty;
    }
    static string ResolveRemoteRootItemId(SyncSettings Settings)
    {
        string DatabaseRootItemId = LoadDatabaseRemoteRootItemId(Settings.SyncRootId);
        return string.IsNullOrWhiteSpace(DatabaseRootItemId) ? Settings.RemoteRootFolderId : DatabaseRootItemId;
    }
    static string TextValue(IDataRecord Record, string Name)
    {
        int Index = Record.GetOrdinal(Name);
        return Record.IsDBNull(Index) ? string.Empty : Convert.ToString(Record.GetValue(Index)) ?? string.Empty;
    }
    static void PrintDbTree()
    {
        using SQLiteConnection Connection = OpenDatabase();
        using SQLiteCommand Command = Connection.CreateCommand();
        Command.CommandText = @"
select
    i.Id,
    i.LocalKey,
    i.ItemType,
    i.RemoteItemId,
    l.ExistsFlag as LocalExists,
    l.RelativePath as LocalPath,
    l.ContentHash as LocalHash,
    r.ExistsFlag as RemoteExists,
    r.Name as RemoteName,
    r.RemoteParentId as RemoteParentId,
    r.Trashed as RemoteTrashed,
    r.ContentHash as RemoteHash,
    b.ExistsFlag as BaseExists,
    b.LocalRelativePath as BasePath,
    b.RemoteParentId as BaseRemoteParentId,
    b.ContentHash as BaseHash
from TRACKED_ITEM i
left join LOCAL_OBSERVED_SNAPSHOT l on l.TrackedItemId = i.Id
left join REMOTE_OBSERVED_SNAPSHOT r on r.TrackedItemId = i.Id
left join BASE_SNAPSHOT b on b.TrackedItemId = i.Id
order by coalesce(i.LocalKey, l.RelativePath, b.LocalRelativePath, r.Name)";

        using SQLiteDataReader Reader = Command.ExecuteReader();
        while (Reader.Read())
        {
            Console.WriteLine(
                $"{TextValue(Reader, "LocalKey")}\t{TextValue(Reader, "ItemType")}\tremoteId={TextValue(Reader, "RemoteItemId")}"
                + $"\tlocalExists={TextValue(Reader, "LocalExists")}\tlocal={TextValue(Reader, "LocalPath")}\tlocalHash={TextValue(Reader, "LocalHash")}"
                + $"\tremoteExists={TextValue(Reader, "RemoteExists")}\tremote={TextValue(Reader, "RemoteName")}\tremoteParent={TextValue(Reader, "RemoteParentId")}\tremoteTrashed={TextValue(Reader, "RemoteTrashed")}\tremoteHash={TextValue(Reader, "RemoteHash")}"
                + $"\tbaseExists={TextValue(Reader, "BaseExists")}\tbase={TextValue(Reader, "BasePath")}\tbaseParent={TextValue(Reader, "BaseRemoteParentId")}\tbaseHash={TextValue(Reader, "BaseHash")}");
        }
    }
    static IReadOnlyList<string> GetDatabaseLocalKeys()
    {
        if (!File.Exists(DatabasePath))
            return [];

        List<string> Result = new();
        using SQLiteConnection Connection = OpenDatabase();
        using SQLiteCommand Command = Connection.CreateCommand();
        Command.CommandText = @"
select i.LocalKey
from TRACKED_ITEM i
left join LOCAL_OBSERVED_SNAPSHOT l on l.TrackedItemId = i.Id
left join REMOTE_OBSERVED_SNAPSHOT r on r.TrackedItemId = i.Id
left join BASE_SNAPSHOT b on b.TrackedItemId = i.Id
where i.LocalKey is not null
  and i.LocalKey <> ''
  and (
      l.ExistsFlag = 1
      or b.ExistsFlag = 1
      or (r.ExistsFlag = 1 and coalesce(r.Trashed, 0) = 0 and coalesce(r.Removed, 0) = 0)
  )
order by i.LocalKey";
        using SQLiteDataReader Reader = Command.ExecuteReader();

        while (Reader.Read())
            Result.Add(Reader.GetString(0));

        return Result;
    }
    static void PrintPathSetReport(string Title, IReadOnlyList<string> Paths)
    {
        string[] Expected =
        [
            "local-tree",
            "local-tree/a.txt",
            "local-tree/docs",
            "local-tree/docs/b.txt",
            "local-tree/docs/deep",
            "local-tree/docs/deep/c.txt",
        ];
        List<string> Missing = Expected.Where(Item => !Paths.Contains(Item, StringComparer.Ordinal)).ToList();
        List<string> Unexpected = Paths
            .Where(Item => !Expected.Contains(Item, StringComparer.Ordinal))
            .Where(Item => !string.IsNullOrWhiteSpace(Item))
            .ToList();
        List<string> NestedLocalTree = Paths
            .Where(Item => Item.Contains("/local-tree/", StringComparison.Ordinal) || Item.EndsWith("/local-tree", StringComparison.Ordinal))
            .ToList();
        List<string> MoveTarget = Paths
            .Where(Item => string.Equals(Item, "MoveTarget", StringComparison.Ordinal) || Item.StartsWith("MoveTarget/", StringComparison.Ordinal))
            .ToList();

        Console.WriteLine("[" + Title + "]");
        Console.WriteLine("items=" + Paths.Count.ToString());

        foreach (string Item in Missing)
            Console.WriteLine("MISSING\t" + Item);

        foreach (string Item in Unexpected)
            Console.WriteLine("UNEXPECTED\t" + Item);

        foreach (string Item in NestedLocalTree)
            Console.WriteLine("NESTED_LOCAL_TREE\t" + Item);

        foreach (string Item in MoveTarget)
            Console.WriteLine("MOVE_TARGET\t" + Item);

        if (Missing.Count == 0 && Unexpected.Count == 0 && NestedLocalTree.Count == 0 && MoveTarget.Count == 0)
            Console.WriteLine("OK");
    }
    static void PrintEmptyReport(string Title, IReadOnlyList<string> Paths)
    {
        Console.WriteLine("[" + Title + "]");
        Console.WriteLine("items=" + Paths.Count.ToString());

        foreach (string Item in Paths)
            Console.WriteLine("UNEXPECTED\t" + Item);

        if (Paths.Count == 0)
            Console.WriteLine("OK");
    }
    static async Task VerifyTreeAsync(SyncSettings Settings, CancellationToken CancellationToken)
    {
        PrintPathSetReport("local", GetLocalPaths(Settings.LocalRootPath));
        PrintPathSetReport("db", GetDatabaseLocalKeys());

        GoogleDriveClient Client = await CreateDriveClientAsync(CancellationToken);
        PrintPathSetReport("drive", await GetDrivePathsAsync(Client, ResolveRemoteRootItemId(Settings), string.Empty, CancellationToken));
    }
    static async Task VerifyEmptyAsync(SyncSettings Settings, CancellationToken CancellationToken)
    {
        PrintEmptyReport("local", GetLocalPaths(Settings.LocalRootPath));
        PrintEmptyReport("db", GetDatabaseLocalKeys());

        GoogleDriveClient Client = await CreateDriveClientAsync(CancellationToken);
        PrintEmptyReport("drive", await GetDrivePathsAsync(Client, ResolveRemoteRootItemId(Settings), string.Empty, CancellationToken));
    }
    static async Task RunCycleAsync(CancellationToken CancellationToken)
    {
        using HttpClient Client = new() { BaseAddress = new Uri(ServiceBaseUrl), Timeout = TimeSpan.FromMinutes(5) };
        using HttpResponseMessage Response = await Client.PostAsync("/sync/run-once", new StringContent(string.Empty), CancellationToken);
        string Text = await Response.Content.ReadAsStringAsync(CancellationToken);
        Console.WriteLine(((int)Response.StatusCode).ToString() + " " + Response.ReasonPhrase);
        Console.WriteLine(Text);
    }
    static async Task PrintStatusAsync(CancellationToken CancellationToken)
    {
        using HttpClient Client = new() { BaseAddress = new Uri(ServiceBaseUrl), Timeout = TimeSpan.FromSeconds(5) };
        string Text = await Client.GetStringAsync("/status", CancellationToken);
        Console.WriteLine(Text);
    }
    static void PrintUsage()
    {
        Console.WriteLine("Hermes.Tools commands:");
        Console.WriteLine("  reset-test-state  Deletes DB, local root contents, Drive root contents, and empties trash.");
        Console.WriteLine("  drive-tree        Lists Google Drive tree under configured remote root.");
        Console.WriteLine("  db-tree           Lists tracked local/remote/base metadata.");
        Console.WriteLine("  local-tree        Lists configured local sync folder.");
        Console.WriteLine("  local-seed        Recreates local test tree under configured local sync folder.");
        Console.WriteLine("  local-mkdir PATH  Creates a local folder under the configured local sync folder.");
        Console.WriteLine("  local-write PATH TEXT");
        Console.WriteLine("                    Writes a local text file under the configured local sync folder.");
        Console.WriteLine("  local-move SOURCE TARGET");
        Console.WriteLine("                    Moves a local file or folder under the configured local sync folder.");
        Console.WriteLine("  local-delete PATH");
        Console.WriteLine("                    Deletes a local file or folder under the configured local sync folder.");
        Console.WriteLine("  drive-mkdir PATH  Creates a Drive folder under configured remote root.");
        Console.WriteLine("  drive-mkdir-duplicate PATH");
        Console.WriteLine("                    Creates a new Drive folder even when the same path already exists.");
        Console.WriteLine("  drive-write PATH TEXT");
        Console.WriteLine("                    Creates or updates a Drive text file under configured remote root.");
        Console.WriteLine("  drive-write-duplicate PATH TEXT");
        Console.WriteLine("                    Creates a new Drive text file even when the same path already exists.");
        Console.WriteLine("  drive-write-parent-id PARENT_ID NAME TEXT");
        Console.WriteLine("                    Creates a new Drive text file under a parent folder id.");
        Console.WriteLine("  drive-rename PATH NEW_NAME");
        Console.WriteLine("                    Renames a Drive item under configured remote root.");
        Console.WriteLine("  drive-rename-id ID NEW_NAME");
        Console.WriteLine("                    Renames a Drive item by remote id.");
        Console.WriteLine("  drive-move SOURCE TARGET_FOLDER");
        Console.WriteLine("                    Moves a Drive item under a Drive folder path, creating target folders.");
        Console.WriteLine("  drive-trash PATH  Moves a Drive item to trash.");
        Console.WriteLine("  drive-delete PATH");
        Console.WriteLine("                    Permanently deletes a Drive item under configured remote root.");
        Console.WriteLine("  drive-restore-id ID");
        Console.WriteLine("                    Restores a Drive item from trash by remote id.");
        Console.WriteLine("  drive-empty-trash");
        Console.WriteLine("                    Empties Google Drive trash without touching local files or DB.");
        Console.WriteLine("  run-cycle         Calls Hermes.Service /sync/run-once.");
        Console.WriteLine("  status            Calls Hermes.Service /status.");
        Console.WriteLine("  verify-empty      Verifies local, db, and Drive are empty.");
        Console.WriteLine("  verify-tree       Verifies expected local-tree shape in local, db, and Drive.");
    }

    // ● public

    /// <summary>
    /// Runs the diagnostics command-line tool.
    /// </summary>
    static public async Task<int> Main(string[] Args)
    {
        try
        {
            SyncSettings Settings = LoadSettings();
            string Command = Args.Length == 0 ? "help" : Args[0].Trim().ToLowerInvariant();

            switch (Command)
            {
                case "reset-test-state":
                    await ResetTestStateAsync(Settings, CancellationToken.None);
                    return 0;
                case "drive-tree":
                    GoogleDriveClient Client = await CreateDriveClientAsync(CancellationToken.None);
                    await PrintDriveTreeAsync(Client, ResolveRemoteRootItemId(Settings), string.Empty, CancellationToken.None);
                    return 0;
                case "db-tree":
                    PrintDbTree();
                    return 0;
                case "local-tree":
                    PrintLocalTree(Settings.LocalRootPath);
                    return 0;
                case "local-seed":
                    SeedLocalTree(Settings.LocalRootPath);
                    PrintLocalTree(Settings.LocalRootPath);
                    return 0;
                case "local-mkdir":
                    if (Args.Length < 2)
                        throw new ArgumentException("local-mkdir requires PATH.");

                    CreateLocalFolder(Settings, Args[1]);
                    return 0;
                case "local-write":
                    if (Args.Length < 3)
                        throw new ArgumentException("local-write requires PATH and TEXT.");

                    WriteLocalFile(Settings, Args[1], Args[2]);
                    return 0;
                case "local-move":
                    if (Args.Length < 3)
                        throw new ArgumentException("local-move requires SOURCE and TARGET.");

                    MoveLocalItem(Settings, Args[1], Args[2]);
                    return 0;
                case "local-delete":
                    if (Args.Length < 2)
                        throw new ArgumentException("local-delete requires PATH.");

                    DeleteLocalItem(Settings, Args[1]);
                    return 0;
                case "drive-mkdir":
                    if (Args.Length < 2)
                        throw new ArgumentException("drive-mkdir requires PATH.");

                    await CreateDriveFolderAsync(Settings, Args[1], CancellationToken.None);
                    return 0;
                case "drive-mkdir-duplicate":
                    if (Args.Length < 2)
                        throw new ArgumentException("drive-mkdir-duplicate requires PATH.");

                    await CreateDuplicateDriveFolderAsync(Settings, Args[1], CancellationToken.None);
                    return 0;
                case "drive-write":
                    if (Args.Length < 3)
                        throw new ArgumentException("drive-write requires PATH and TEXT.");

                    await WriteDriveFileAsync(Settings, Args[1], Args[2], CancellationToken.None);
                    return 0;
                case "drive-write-duplicate":
                    if (Args.Length < 3)
                        throw new ArgumentException("drive-write-duplicate requires PATH and TEXT.");

                    await WriteDuplicateDriveFileAsync(Settings, Args[1], Args[2], CancellationToken.None);
                    return 0;
                case "drive-write-parent-id":
                    if (Args.Length < 4)
                        throw new ArgumentException("drive-write-parent-id requires PARENT_ID, NAME, and TEXT.");

                    await WriteDriveFileInFolderIdAsync(Args[1], Args[2], Args[3], CancellationToken.None);
                    return 0;
                case "drive-rename":
                    if (Args.Length < 3)
                        throw new ArgumentException("drive-rename requires PATH and NEW_NAME.");

                    await RenameDriveItemAsync(Settings, Args[1], Args[2], CancellationToken.None);
                    return 0;
                case "drive-rename-id":
                    if (Args.Length < 3)
                        throw new ArgumentException("drive-rename-id requires ID and NEW_NAME.");

                    await RenameDriveItemByIdAsync(Args[1], Args[2], CancellationToken.None);
                    return 0;
                case "drive-move":
                    if (Args.Length < 3)
                        throw new ArgumentException("drive-move requires SOURCE and TARGET_FOLDER.");

                    await MoveDriveItemAsync(Settings, Args[1], Args[2], CancellationToken.None);
                    return 0;
                case "drive-trash":
                    if (Args.Length < 2)
                        throw new ArgumentException("drive-trash requires PATH.");

                    await TrashDriveItemAsync(Settings, Args[1], CancellationToken.None);
                    return 0;
                case "drive-delete":
                    if (Args.Length < 2)
                        throw new ArgumentException("drive-delete requires PATH.");

                    await DeleteDriveItemAsync(Settings, Args[1], CancellationToken.None);
                    return 0;
                case "drive-restore-id":
                    if (Args.Length < 2)
                        throw new ArgumentException("drive-restore-id requires ID.");

                    await RestoreDriveItemAsync(Args[1], CancellationToken.None);
                    return 0;
                case "drive-empty-trash":
                    await EmptyDriveTrashAsync(CancellationToken.None);
                    return 0;
                case "run-cycle":
                    await RunCycleAsync(CancellationToken.None);
                    return 0;
                case "status":
                    await PrintStatusAsync(CancellationToken.None);
                    return 0;
                case "verify-tree":
                    await VerifyTreeAsync(Settings, CancellationToken.None);
                    return 0;
                case "verify-empty":
                    await VerifyEmptyAsync(Settings, CancellationToken.None);
                    return 0;
                default:
                    PrintUsage();
                    return Command == "help" ? 0 : 1;
            }
        }
        catch (Exception Ex)
        {
            Console.Error.WriteLine(Ex.GetType().Name + ": " + Ex.Message);
            return 2;
        }
    }
}
