// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace TestApp;

/// <summary>
/// Main playground window.
/// </summary>
public partial class MainWindow : Window
{
    // ● private

    readonly GoogleDriveAuthManager fAuthManager = new();
    readonly GoogleDriveClient fDriveClient;
    string fLocalMirrorFolderPath = string.Empty;
    void AppendLog(string Text)
    {
        if (!string.IsNullOrWhiteSpace(edtLog.Text))
            edtLog.Text += Environment.NewLine;

        edtLog.Text += $"[{DateTimeOffset.Now:HH:mm:ss}] {Text}";
        edtLog.CaretIndex = edtLog.Text.Length;
    }
    void SetButtonsEnabled(bool Enabled)
    {
        SetButtonsEnabled(ButtonPanel, Enabled);
    }
    void SetButtonsEnabled(Control Root, bool Enabled)
    {
        if (Root is Button Button)
            Button.IsEnabled = Enabled;

        if (Root is Panel Panel)
        {
            foreach (Control Control in Panel.Children)
                SetButtonsEnabled(Control, Enabled);
        }
        else if (Root is ContentControl ContentControl && ContentControl.Content is Control Content)
        {
            SetButtonsEnabled(Content, Enabled);
        }
    }
    async Task RunButtonAsync(string OperationName, Func<Task> Operation)
    {
        DateTimeOffset StartedAt = DateTimeOffset.Now;

        SetButtonsEnabled(false);
        AppendLog($"{OperationName}: started at {StartedAt:yyyy-MM-dd HH:mm:ss}.");

        try
        {
            await Operation();
            AppendLog($"{OperationName}: completed.");
        }
        catch (Exception Ex)
        {
            AppendLog($"{OperationName}: failed.");
            AppendLog($"{Ex.GetType().FullName}: {Ex.Message}");
            AppendLog(Ex.StackTrace ?? "No stack trace.");
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }
    async Task ConnectAsync()
    {
        AppendLog("starting authentication");
        AppendLog($"client secret: {GoogleDriveAuthManager.GetClientSecretFilePath()}");
        AppendLog($"token file: {GoogleDriveAuthManager.GetTokenFilePath()}");
        await fAuthManager.AuthenticateAsync(CancellationToken.None);
        AppendLog("authentication succeeded");

        fLocalMirrorFolderPath = await GetLocalMirrorFolderPathAsync();
        AppendLog($"local mirror folder: {fLocalMirrorFolderPath}");
    }
    async Task AboutAsync()
    {
        AppendLog("About: authenticating");
        await fDriveClient.AuthenticateAsync(CancellationToken.None);

        AppendLog("About: calling Drive About API");
        GoogleDriveAbout About = await fDriveClient.GetAboutAsync(CancellationToken.None);

        AppendLog($"app name: {CommonConstants.ApplicationName}");
        AppendLog($"user email: {FormatTextValue(About.EmailAddress)}");
        AppendLog($"storage quota limit: {FormatByteValue(About.StorageLimit)}");
        AppendLog($"storage quota usage: {FormatByteValue(About.StorageUsage)}");
        AppendLog("About: succeeded");
    }
    async Task GetStartPageTokenAsync()
    {
        AppendLog("Get Start Page Token: authenticating");
        await fDriveClient.AuthenticateAsync(CancellationToken.None);

        AppendLog("Get Start Page Token: calling Changes.GetStartPageToken");
        string Token = await fDriveClient.GetStartPageTokenAsync(CancellationToken.None);

        AppendLog($"start page token: {FormatTextValue(Token)}");
        SaveStartPageToken(Token);
        AppendLog("Get Start Page Token: succeeded");
    }
    async Task ListRootFolderAsync()
    {
        AppendLog("List Root Folder: authenticating");
        await fDriveClient.AuthenticateAsync(CancellationToken.None);

        AppendLog("List Root Folder: calling Files.List");
        IReadOnlyList<StorageItem> Files = await fDriveClient.ListRootFolderAsync(CancellationToken.None);

        AppendLog($"root item count: {Files.Count}");

        foreach (StorageItem Item in Files)
            LogStorageItem("root item", Item);

        AppendLog("List Root Folder: succeeded");
    }
    async Task ListFolderAsync()
    {
        string FolderId = edtFolderId.Text == null ? string.Empty : edtFolderId.Text.Trim();
        if (string.IsNullOrWhiteSpace(FolderId))
            throw new InvalidOperationException("Folder id is required.");

        AppendLog("List Folder: authenticating");
        await fDriveClient.AuthenticateAsync(CancellationToken.None);

        AppendLog($"List Folder: folder id: {FolderId}");
        AppendLog("List Folder: calling Files.List");
        IReadOnlyList<StorageItem> Files = await fDriveClient.ListFolderAsync(FolderId, CancellationToken.None);

        AppendLog($"folder item count: {Files.Count}");

        foreach (StorageItem Item in Files)
            LogStorageItem("folder item", Item);

        AppendLog("List Folder: succeeded");
    }
    async Task GetFileAsync()
    {
        string FileId = edtFileId.Text == null ? string.Empty : edtFileId.Text.Trim();
        if (string.IsNullOrWhiteSpace(FileId))
            throw new InvalidOperationException("File id is required.");

        AppendLog("Get File: authenticating");
        await fDriveClient.AuthenticateAsync(CancellationToken.None);

        AppendLog($"Get File: file id: {FileId}");
        AppendLog("Get File: calling Files.Get");
        try
        {
            StorageItem Item = await fDriveClient.GetFileAsync(FileId, CancellationToken.None);
            LogStorageItem("file item", Item);
        }
        catch (GoogleDriveNotFoundException Ex)
        {
            AppendLog("Get File: not found.");
            AppendLog("Get File: observed 404 for this FileId.");
            AppendLog($"{Ex.GetType().FullName}: {Ex.Message}");
        }

        AppendLog("Get File: succeeded");
    }
    async Task RenameAsync()
    {
        string FileId = edtFileId.Text == null ? string.Empty : edtFileId.Text.Trim();
        string NewName = edtNewName.Text == null ? string.Empty : edtNewName.Text.Trim();
        if (string.IsNullOrWhiteSpace(FileId))
            throw new InvalidOperationException("File id is required.");
        if (string.IsNullOrWhiteSpace(NewName))
            throw new InvalidOperationException("New name is required.");

        AppendLog("Rename: authenticating");
        await fDriveClient.AuthenticateAsync(CancellationToken.None);

        AppendLog($"Rename: file id: {FileId}");
        AppendLog($"Rename: new name: {NewName}");
        AppendLog("Rename: current item before update");
        StorageItem Before = await fDriveClient.GetFileAsync(FileId, CancellationToken.None);
        LogStorageItem("before rename item", Before);
        AppendLog("Rename: calling Files.Update");
        StorageItem Updated = await fDriveClient.RenameFileAsync(FileId, NewName, CancellationToken.None);
        LogStorageItem("updated item", Updated);
        AppendLog("Rename: refreshing item after update");
        StorageItem Refreshed = await fDriveClient.GetFileAsync(FileId, CancellationToken.None);
        LogStorageItem("after rename item", Refreshed);
        AppendLog("Rename: succeeded");
    }
    async Task MoveAsync()
    {
        string FileId = edtFileId.Text == null ? string.Empty : edtFileId.Text.Trim();
        string NewParentId = edtMoveParentId.Text == null ? string.Empty : edtMoveParentId.Text.Trim();
        if (string.IsNullOrWhiteSpace(FileId))
            throw new InvalidOperationException("File id is required.");
        if (string.IsNullOrWhiteSpace(NewParentId))
            throw new InvalidOperationException("New parent id is required.");

        AppendLog("Move: authenticating");
        await fDriveClient.AuthenticateAsync(CancellationToken.None);

        AppendLog($"Move: file id: {FileId}");
        AppendLog($"Move: new parent id: {NewParentId}");
        AppendLog("Move: current item before update");
        StorageItem Before = await fDriveClient.GetFileAsync(FileId, CancellationToken.None);
        LogStorageItem("before move item", Before);
        AppendLog($"Move: old parent id: {FormatTextValue(Before.ParentId)}");
        AppendLog("Move: calling Files.Update");
        StorageItem Updated = await fDriveClient.MoveFileAsync(FileId, Before.ParentId, NewParentId, CancellationToken.None);
        LogStorageItem("updated item", Updated);
        AppendLog("Move: refreshing item after update");
        StorageItem Refreshed = await fDriveClient.GetFileAsync(FileId, CancellationToken.None);
        LogStorageItem("after move item", Refreshed);
        AppendLog("Move: succeeded");
    }
    async Task TrashAsync()
    {
        string FileId = edtFileId.Text == null ? string.Empty : edtFileId.Text.Trim();
        if (string.IsNullOrWhiteSpace(FileId))
            throw new InvalidOperationException("File id is required.");

        AppendLog("Trash: authenticating");
        await fDriveClient.AuthenticateAsync(CancellationToken.None);

        AppendLog($"Trash: file id: {FileId}");
        AppendLog("Trash: current item before update");
        StorageItem Before = await fDriveClient.GetFileAsync(FileId, CancellationToken.None);
        LogStorageItem("before trash item", Before);
        AppendLog("Trash: calling Files.Update");
        StorageItem Updated = await fDriveClient.TrashFileAsync(FileId, CancellationToken.None);
        LogStorageItem("updated item", Updated);
        AppendLog("Trash: refreshing item after update");
        StorageItem Refreshed = await fDriveClient.GetFileAsync(FileId, CancellationToken.None);
        LogStorageItem("after trash item", Refreshed);
        AppendLog("Trash: succeeded");
    }
    async Task RestoreAsync()
    {
        string FileId = edtFileId.Text == null ? string.Empty : edtFileId.Text.Trim();
        if (string.IsNullOrWhiteSpace(FileId))
            throw new InvalidOperationException("File id is required.");

        AppendLog("Restore: authenticating");
        await fDriveClient.AuthenticateAsync(CancellationToken.None);

        AppendLog($"Restore: file id: {FileId}");
        AppendLog("Restore: current item before update");
        StorageItem Before = await fDriveClient.GetFileAsync(FileId, CancellationToken.None);
        LogStorageItem("before restore item", Before);
        AppendLog("Restore: calling Files.Update");
        StorageItem Updated = await fDriveClient.RestoreFileAsync(FileId, CancellationToken.None);
        LogStorageItem("updated item", Updated);
        AppendLog("Restore: refreshing item after update");
        StorageItem Refreshed = await fDriveClient.GetFileAsync(FileId, CancellationToken.None);
        LogStorageItem("after restore item", Refreshed);
        AppendLog("Restore: succeeded");
    }
    async Task DeletePermanentlyAsync()
    {
        string FileId = edtFileId.Text == null ? string.Empty : edtFileId.Text.Trim();
        if (string.IsNullOrWhiteSpace(FileId))
            throw new InvalidOperationException("File id is required.");

        AppendLog("Delete Permanently: authenticating");
        await fDriveClient.AuthenticateAsync(CancellationToken.None);

        AppendLog($"Delete Permanently: file id: {FileId}");
        AppendLog("Delete Permanently: calling Files.Delete");
        await fDriveClient.DeleteFileAsync(FileId, CancellationToken.None);
        AppendLog("Delete Permanently: succeeded");
    }
    async Task CreateFolderAsync()
    {
        string FolderName = edtNewFolderName.Text == null ? string.Empty : edtNewFolderName.Text.Trim();
        string ParentId = edtNewFolderParentId.Text == null ? string.Empty : edtNewFolderParentId.Text.Trim();
        if (string.IsNullOrWhiteSpace(FolderName))
            throw new InvalidOperationException("New folder name is required.");

        AppendLog("Create Folder: authenticating");
        await fDriveClient.AuthenticateAsync(CancellationToken.None);

        AppendLog($"Create Folder: folder name: {FolderName}");
        AppendLog($"Create Folder: parent id: {FormatTextValue(ParentId)}");
        AppendLog("Create Folder: calling Files.Create");
        StorageItem Item = await fDriveClient.CreateFolderAsync(FolderName, ParentId, CancellationToken.None);
        LogStorageItem("created folder item", Item);
        AppendLog("Create Folder: succeeded");
    }
    async Task UploadFileAsync()
    {
        string LocalFilePath = edtUploadFilePath.Text == null ? string.Empty : edtUploadFilePath.Text.Trim();
        string ParentId = edtUploadParentId.Text == null ? string.Empty : edtUploadParentId.Text.Trim();
        if (string.IsNullOrWhiteSpace(LocalFilePath))
            throw new InvalidOperationException("Local file path is required.");
        if (!System.IO.File.Exists(LocalFilePath))
            throw new FileNotFoundException("Upload source file was not found.", LocalFilePath);

        FileInfo Info = new(LocalFilePath);
        AppendLog("Upload File: authenticating");
        await fDriveClient.AuthenticateAsync(CancellationToken.None);

        AppendLog($"Upload File: local file path: {LocalFilePath}");
        AppendLog($"Upload File: local file size: {Info.Length}");
        AppendLog($"Upload File: parent id: {FormatTextValue(ParentId)}");
        AppendLog("Upload File: calling Files.Create media upload");
        StorageItem Item = await fDriveClient.UploadFileAsync(LocalFilePath, ParentId, CancellationToken.None);
        LogStorageItem("uploaded file item", Item);
        AppendLog("Upload File: succeeded");
    }
    async Task UpdateFileContentAsync()
    {
        string FileId = edtFileId.Text == null ? string.Empty : edtFileId.Text.Trim();
        string LocalFilePath = edtUploadFilePath.Text == null ? string.Empty : edtUploadFilePath.Text.Trim();
        if (string.IsNullOrWhiteSpace(FileId))
            throw new InvalidOperationException("File id is required.");
        if (string.IsNullOrWhiteSpace(LocalFilePath))
            throw new InvalidOperationException("Local source file path is required.");
        if (!System.IO.File.Exists(LocalFilePath))
            throw new FileNotFoundException("Update source file was not found.", LocalFilePath);

        FileInfo Info = new(LocalFilePath);
        string LocalMd5Hash = ComputeFileMd5(LocalFilePath);
        AppendLog("Update File Content: authenticating");
        await fDriveClient.AuthenticateAsync(CancellationToken.None);

        AppendLog($"Update File Content: file id: {FileId}");
        AppendLog($"Update File Content: local file path: {LocalFilePath}");
        AppendLog($"Update File Content: local file size: {Info.Length}");
        AppendLog($"Update File Content: local md5: {LocalMd5Hash}");
        AppendLog("Update File Content: current item before update");
        StorageItem Before = await fDriveClient.GetFileAsync(FileId, CancellationToken.None);
        LogStorageItem("before content update item", Before);
        AppendLog("Update File Content: calling Files.Update media upload");
        StorageItem Updated = await fDriveClient.UpdateFileContentAsync(FileId, LocalFilePath, CancellationToken.None);
        LogStorageItem("updated item", Updated);
        AppendLog($"Update File Content: remote md5 equals local md5: {TextEquals(Updated.Md5Hash, LocalMd5Hash)}");
        AppendLog("Update File Content: refreshing item after update");
        StorageItem Refreshed = await fDriveClient.GetFileAsync(FileId, CancellationToken.None);
        LogStorageItem("after content update item", Refreshed);
        AppendLog($"Update File Content: refreshed md5 equals local md5: {TextEquals(Refreshed.Md5Hash, LocalMd5Hash)}");
        AppendLog("Update File Content: succeeded");
    }
    async Task DownloadFileAsync()
    {
        string FileId = edtFileId.Text == null ? string.Empty : edtFileId.Text.Trim();
        string LocalFilePath = edtDownloadFilePath.Text == null ? string.Empty : edtDownloadFilePath.Text.Trim();
        if (string.IsNullOrWhiteSpace(FileId))
            throw new InvalidOperationException("File id is required.");
        if (string.IsNullOrWhiteSpace(LocalFilePath))
            throw new InvalidOperationException("Download path is required.");

        AppendLog("Download File: authenticating");
        await fDriveClient.AuthenticateAsync(CancellationToken.None);

        AppendLog($"Download File: file id: {FileId}");
        AppendLog($"Download File: local file path: {LocalFilePath}");
        AppendLog("Download File: current item before download");
        StorageItem Item = await fDriveClient.GetFileAsync(FileId, CancellationToken.None);
        LogStorageItem("download source item", Item);
        AppendLog("Download File: calling Files.Get media download");
        StorageItem DownloadedItem = await fDriveClient.DownloadFileAsync(FileId, LocalFilePath, CancellationToken.None);
        FileInfo Info = new(LocalFilePath);
        string LocalMd5Hash = ComputeFileMd5(LocalFilePath);
        AppendLog($"Download File: local file size: {Info.Length}");
        AppendLog($"Download File: local md5: {LocalMd5Hash}");
        AppendLog($"Download File: local size equals remote size: {Info.Length == DownloadedItem.Size}");
        AppendLog($"Download File: local md5 equals remote md5: {TextEquals(LocalMd5Hash, DownloadedItem.Md5Hash)}");
        LogStorageItem("downloaded file item", DownloadedItem);
        AppendLog("Download File: succeeded");
    }
    async Task ListChangesAsync()
    {
        string PageToken = GetChangesPageToken();

        AppendLog("List Changes: authenticating");
        await fDriveClient.AuthenticateAsync(CancellationToken.None);

        if (string.IsNullOrWhiteSpace(PageToken))
        {
            AppendLog("List Changes: no saved token, requesting current start page token");
            PageToken = await fDriveClient.GetStartPageTokenAsync(CancellationToken.None);
            SaveStartPageToken(PageToken);
            AppendLog($"List Changes: saved start page token: {FormatTextValue(PageToken)}");
            AppendLog("List Changes: no changes are expected until more Drive operations happen after this token.");
            return;
        }

        AppendLog($"List Changes: using page token: {PageToken}");
        StorageChangeListResult Result = await fDriveClient.ListChangesAsync(PageToken, CancellationToken.None);
        AppendLog($"List Changes: change count: {Result.Changes.Count}");

        foreach (StorageChange Change in Result.Changes)
            LogDriveChange(Change);

        AppendLog($"List Changes: new start page token: {FormatTextValue(Result.NewStartPageToken)}");

        if (!string.IsNullOrWhiteSpace(Result.NewStartPageToken))
            SaveStartPageToken(Result.NewStartPageToken);

        AppendLog("List Changes: succeeded");
    }
    async void ConnectClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("authentication", ConnectAsync);
    }
    async void AboutClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("About", AboutAsync);
    }
    async void GetStartPageTokenClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("Get Start Page Token", GetStartPageTokenAsync);
    }
    async void ListRootFolderClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("List Root Folder", ListRootFolderAsync);
    }
    async void ListFolderClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("List Folder", ListFolderAsync);
    }
    async void GetFileClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("Get File", GetFileAsync);
    }
    async void RenameClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("Rename", RenameAsync);
    }
    async void MoveClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("Move", MoveAsync);
    }
    async void TrashClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("Trash", TrashAsync);
    }
    async void RestoreClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("Restore", RestoreAsync);
    }
    async void DeletePermanentlyClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("Delete Permanently", DeletePermanentlyAsync);
    }
    async void CreateFolderClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("Create Folder", CreateFolderAsync);
    }
    async void UploadFileClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("Upload File", UploadFileAsync);
    }
    async void UpdateFileContentClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("Update File Content", UpdateFileContentAsync);
    }
    async void DownloadFileClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("Download File", DownloadFileAsync);
    }
    async void ListChangesClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("List Changes", ListChangesAsync);
    }
    void ClearLogClick(object Sender, RoutedEventArgs Args)
    {
        edtLog.Text = "Ready.";
        edtLog.CaretIndex = edtLog.Text.Length;
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        fDriveClient = new GoogleDriveClient(fAuthManager, new GoogleDriveMapper());
        InitializeComponent();
        edtChangesPageToken.Text = DataLib.Settings.Google.StartPageToken;
    }

    // ● private

    void LogStorageItem(string Header, StorageItem Item)
    {
        AppendLog(Header);
        AppendLog($"  Id: {FormatTextValue(Item.Id)}");
        AppendLog($"  Name: {FormatTextValue(Item.Name)}");
        AppendLog($"  MimeType: {FormatTextValue(Item.MimeType)}");
        AppendLog($"  IsFolder: {Item.IsFolder}");
        AppendLog($"  Size: {FormatSizeValue(Item.Size)}");
        AppendLog($"  Md5Hash: {FormatTextValue(Item.Md5Hash)}");
        AppendLog($"  ModifiedTime: {FormatDateValue(Item.ModifiedTime)}");
        AppendLog($"  CreatedTime: {FormatDateValue(Item.CreatedTime)}");
        AppendLog($"  ParentId: {FormatTextValue(Item.ParentId)}");
        AppendLog($"  Trashed: {Item.Trashed}");
        AppendLog($"  Version: {FormatVersionValue(Item.Version)}");
    }
    void LogDriveChange(StorageChange Change)
    {
        AppendLog("change item");
        AppendLog($"  ItemId: {FormatTextValue(Change.ItemId)}");
        AppendLog($"  Removed: {Change.Removed}");
        AppendLog($"  Time: {FormatDateValue(Change.Time)}");
        AppendLog($"  HasItem: {Change.Item != null}");

        if (Change.Item != null)
            LogStorageItem("  changed storage item", Change.Item);
    }
    string GetChangesPageToken()
    {
        string PageToken = edtChangesPageToken.Text == null ? string.Empty : edtChangesPageToken.Text.Trim();
        if (!string.IsNullOrWhiteSpace(PageToken))
            return PageToken;

        return DataLib.Settings.Google.StartPageToken;
    }
    void SaveStartPageToken(string Token)
    {
        if (string.IsNullOrWhiteSpace(Token))
            return;

        DataLib.Settings.Google.StartPageToken = Token;
        DataLib.Settings.Save();
        edtChangesPageToken.Text = Token;
        AppendLog("start page token saved");
    }
    static string ComputeFileMd5(string FilePath)
    {
        using MD5 Md5 = MD5.Create();
        using FileStream Stream = System.IO.File.OpenRead(FilePath);
        byte[] Hash = Md5.ComputeHash(Stream);
        return Convert.ToHexString(Hash).ToLowerInvariant();
    }
    static bool TextEquals(string A, string B)
    {
        return string.Equals(A ?? string.Empty, B ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
    string FormatTextValue(string Value)
    {
        return string.IsNullOrWhiteSpace(Value) ? "not available" : Value;
    }
    string FormatByteValue(long Value)
    {
        return Value > 0 ? Value.ToString() : "not available";
    }
    string FormatSizeValue(long Value)
    {
        return Value > 0 ? Value.ToString() : "not available";
    }
    string FormatVersionValue(long Value)
    {
        return Value > 0 ? Value.ToString() : "not available";
    }
    string FormatDateValue(DateTimeOffset Value)
    {
        return Value == default ? "not available" : Value.ToString();
    }
    string FormatDateValue(DateTimeOffset? Value)
    {
        return Value.HasValue ? FormatDateValue(Value.Value) : "not available";
    }
    async Task<IStorageFolder> GetSuggestedMirrorFolderAsync(IStorageProvider StorageProvider)
    {
        string HomeFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string MirrorFolderPath = Path.Combine(HomeFolderPath, "gdrive");

        if (!Directory.Exists(MirrorFolderPath))
            return null;

        return await StorageProvider.TryGetFolderFromPathAsync(MirrorFolderPath);
    }
    async Task<string> SelectLocalMirrorFolderAsync()
    {
        TopLevel TopLevel = TopLevel.GetTopLevel(this);
        if (TopLevel == null)
            throw new InvalidOperationException("Cannot get Avalonia top level window.");

        IStorageProvider StorageProvider = TopLevel.StorageProvider;
        if (!StorageProvider.CanPickFolder)
            throw new NotSupportedException("Folder selection is not supported on this platform.");

        IStorageFolder SuggestedFolder = await GetSuggestedMirrorFolderAsync(StorageProvider);
        FolderPickerOpenOptions Options = new()
        {
            Title = "Select Google Drive mirror folder",
            AllowMultiple = false,
            SuggestedStartLocation = SuggestedFolder
        };

        IReadOnlyList<IStorageFolder> Folders = await StorageProvider.OpenFolderPickerAsync(Options);
        if (Folders.Count == 0)
            throw new OperationCanceledException("Local mirror folder selection was canceled.");

        using IStorageFolder Folder = Folders[0];
        string FolderPath = Folder.TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(FolderPath))
            throw new InvalidOperationException("The selected folder does not expose a local file system path.");

        return FolderPath;
    }
    async Task<string> GetLocalMirrorFolderPathAsync()
    {
        string FolderPath = DataLib.Settings.Google.DriveFolderPath;

        if (!string.IsNullOrWhiteSpace(FolderPath) && Directory.Exists(FolderPath))
        {
            AppendLog("using saved local mirror folder");
            return FolderPath;
        }

        AppendLog("select local mirror folder");
        FolderPath = await SelectLocalMirrorFolderAsync();

        DataLib.Settings.Google.DriveFolderPath = FolderPath;
        DataLib.Settings.Save();
        AppendLog("local mirror folder saved");

        return FolderPath;
    }
}
