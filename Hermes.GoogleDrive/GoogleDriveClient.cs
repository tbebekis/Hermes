// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.GoogleDrive;

/// <summary>
/// Wraps Google Drive API calls.
/// </summary>
public class GoogleDriveClient
{
    // ● private

    private readonly GoogleDriveAuthManager fAuthManager;
    private readonly GoogleDriveMapper fMapper;
    private DriveService fDriveService;

    private DriveService RequireDriveService()
    {
        if (fDriveService == null)
            throw new InvalidOperationException("Google Drive client is not authenticated.");

        return fDriveService;
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleDriveClient"/> class.
    /// </summary>
    public GoogleDriveClient(GoogleDriveAuthManager AuthManager, GoogleDriveMapper Mapper)
    {
        fAuthManager = Guard.NotNull(AuthManager, nameof(AuthManager));
        fMapper = Guard.NotNull(Mapper, nameof(Mapper));
    }

    // ● public

    /// <summary>
    /// Authenticates with Google Drive.
    /// </summary>
    public async Task AuthenticateAsync(CancellationToken CancellationToken)
    {
        UserCredential Credential = await fAuthManager.AuthenticateAsync(CancellationToken);
        fDriveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = Credential,
            ApplicationName = CommonConstants.ApplicationName
        });
    }

    /// <summary>
    /// Gets information about the authenticated Drive account.
    /// </summary>
    public async Task<GoogleDriveAbout> GetAboutAsync(CancellationToken CancellationToken)
    {
        DriveService Service = RequireDriveService();
        AboutResource.GetRequest Request = Service.About.Get();
        Request.Fields = "user(displayName,emailAddress),storageQuota(limit,usage)";
        About About = await Request.ExecuteAsync(CancellationToken);

        FilesResource.GetRequest RootRequest = Service.Files.Get("root");
        RootRequest.Fields = "id";
        DriveFile RootFolder = await RootRequest.ExecuteAsync(CancellationToken);

        return fMapper.MapAbout(About, RootFolder.Id ?? string.Empty);
    }

    /// <summary>
    /// Gets a Google Drive start page token.
    /// </summary>
    public async Task<string> GetStartPageTokenAsync(CancellationToken CancellationToken)
    {
        DriveService Service = RequireDriveService();
        ChangesResource.GetStartPageTokenRequest Request = Service.Changes.GetStartPageToken();
        StartPageToken Token = await Request.ExecuteAsync(CancellationToken);
        return Token.StartPageTokenValue ?? string.Empty;
    }

    /// <summary>
    /// Lists files visible to the application.
    /// </summary>
    public async Task<IReadOnlyList<StorageItem>> ListFilesAsync(CancellationToken CancellationToken)
    {
        DriveService Service = RequireDriveService();
        FilesResource.ListRequest Request = Service.Files.List();
        Request.PageSize = 25;
        Request.Fields = "files(id,name,mimeType,parents,size,md5Checksum,trashed)";
        FileList FileList = await Request.ExecuteAsync(CancellationToken);
        List<StorageItem> Result = new();

        if (FileList.Files == null)
            return Result;

        foreach (DriveFile File in FileList.Files)
            Result.Add(fMapper.MapFile(File));

        return Result;
    }

    /// <summary>
    /// Lists changes after a page token.
    /// </summary>
    public async Task<IReadOnlyList<StorageChange>> ListChangesAsync(string PageToken, CancellationToken CancellationToken)
    {
        Guard.NotNullOrWhiteSpace(PageToken, nameof(PageToken));

        DriveService Service = RequireDriveService();
        ChangesResource.ListRequest Request = Service.Changes.List(PageToken);
        Request.PageSize = 25;
        Request.Fields = "changes(fileId,removed,file(id,name,mimeType,parents,size,md5Checksum,trashed)),newStartPageToken,nextPageToken";
        ChangeList ChangeList = await Request.ExecuteAsync(CancellationToken);
        List<StorageChange> Result = new();

        if (ChangeList.Changes == null)
            return Result;

        foreach (Change Change in ChangeList.Changes)
            Result.Add(fMapper.MapChange(Change));

        return Result;
    }

    /// <summary>
    /// Creates a Google Drive folder.
    /// </summary>
    public async Task<StorageFolder> CreateFolderAsync(string Name, string ParentId = null, CancellationToken CancellationToken = default)
    {
        Guard.NotNullOrWhiteSpace(Name, nameof(Name));

        DriveService Service = RequireDriveService();
        DriveFile FileMetadata = new()
        {
            Name = Name,
            MimeType = GoogleDriveConstants.FolderMimeType
        };

        if (!string.IsNullOrWhiteSpace(ParentId))
            FileMetadata.Parents = new List<string> { ParentId };

        FilesResource.CreateRequest Request = Service.Files.Create(FileMetadata);
        Request.Fields = "id,name,mimeType,parents";
        DriveFile Created = await Request.ExecuteAsync(CancellationToken);
        StorageItem Item = fMapper.MapFile(Created);

        if (Item is StorageFolder Folder)
            return Folder;

        throw new HermesException("Google Drive returned a non-folder item after folder creation.");
    }
}
