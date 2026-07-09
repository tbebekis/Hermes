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
        foreach (Control Control in ButtonPanel.Children)
        {
            if (Control is Button Button)
                Button.IsEnabled = Enabled;
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
        StorageItem Item = await fDriveClient.GetFileAsync(FileId, CancellationToken.None);
        LogStorageItem("file item", Item);
        AppendLog("Get File: succeeded");
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
    Task ListChangesAsync()
    {
        AppendLog("List Changes placeholder async method called.");
        return Task.CompletedTask;
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
    async void CreateFolderClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("Create Folder", CreateFolderAsync);
    }
    async void ListChangesClick(object Sender, RoutedEventArgs Args)
    {
        await RunButtonAsync("List Changes", ListChangesAsync);
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        fDriveClient = new GoogleDriveClient(fAuthManager, new GoogleDriveMapper());
        InitializeComponent();
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
