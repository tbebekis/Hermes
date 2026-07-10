// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.GoogleDrive;

/// <summary>
/// Wraps Google Drive API calls.
/// </summary>
public class GoogleDriveClient
{
    // ● private

    readonly GoogleDriveAuthManager fAuthManager;
    readonly GoogleDriveMapper fMapper;
    DriveService fDriveService;

    DriveService RequireDriveService()
    {
        if (fDriveService == null)
            throw new InvalidOperationException("Google Drive client is not authenticated.");

        return fDriveService;
    }

    static string EscapeDriveQueryValue(string Value)
    {
        return Value.Replace("'", "\\'");
    }
    static string GetMimeType(string FilePath)
    {
        string Extension = Path.GetExtension(FilePath).ToLowerInvariant();
        return Extension switch
        {
            ".csv" => "text/csv",
            ".html" => "text/html",
            ".json" => "application/json",
            ".md" => "text/markdown",
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".txt" => "text/plain",
            ".xml" => "application/xml",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
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
        return fMapper.MapAbout(About, string.Empty);
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
    /// Lists the immediate children of the Google Drive root folder.
    /// </summary>
    public async Task<IReadOnlyList<StorageItem>> ListRootFolderAsync(CancellationToken CancellationToken)
    {
        return await ListFolderAsync("root", CancellationToken);
    }

    /// <summary>
    /// Lists the immediate children of the specified Google Drive folder.
    /// </summary>
    public async Task<IReadOnlyList<StorageItem>> ListFolderAsync(string FolderId, CancellationToken CancellationToken)
    {
        Guard.NotNullOrWhiteSpace(FolderId, nameof(FolderId));

        DriveService Service = RequireDriveService();
        List<StorageItem> Result = new();
        string PageToken = string.Empty;

        do
        {
            FilesResource.ListRequest Request = Service.Files.List();
            Request.Q = $"'{EscapeDriveQueryValue(FolderId)}' in parents and trashed = false";
            Request.Fields = "nextPageToken,files(id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version)";
            Request.PageToken = PageToken;
            FileList FileList = await Request.ExecuteAsync(CancellationToken);

            if (FileList.Files != null)
            {
                foreach (DriveFile File in FileList.Files)
                    Result.Add(fMapper.MapFile(File));
            }

            PageToken = FileList.NextPageToken ?? string.Empty;
        }
        while (!string.IsNullOrWhiteSpace(PageToken));

        return Result;
    }
    /// <summary>
    /// Gets a single Google Drive item.
    /// </summary>
    public async Task<StorageItem> GetFileAsync(string FileId, CancellationToken CancellationToken)
    {
        Guard.NotNullOrWhiteSpace(FileId, nameof(FileId));

        DriveService Service = RequireDriveService();
        FilesResource.GetRequest Request = Service.Files.Get(FileId);
        Request.Fields = "id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version";
        try
        {
            DriveFile File = await Request.ExecuteAsync(CancellationToken);
            return fMapper.MapFile(File);
        }
        catch (GoogleApiException Ex) when (Ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new GoogleDriveNotFoundException(FileId, Ex);
        }
    }
    /// <summary>
    /// Renames a Google Drive item.
    /// </summary>
    public async Task<StorageItem> RenameFileAsync(string FileId, string Name, CancellationToken CancellationToken)
    {
        Guard.NotNullOrWhiteSpace(FileId, nameof(FileId));
        Guard.NotNullOrWhiteSpace(Name, nameof(Name));

        DriveService Service = RequireDriveService();
        DriveFile FileMetadata = new()
        {
            Name = Name
        };
        FilesResource.UpdateRequest Request = Service.Files.Update(FileMetadata, FileId);
        Request.Fields = "id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version";
        DriveFile Updated = await Request.ExecuteAsync(CancellationToken);
        return fMapper.MapFile(Updated);
    }
    /// <summary>
    /// Moves a Google Drive item to a new parent folder.
    /// </summary>
    public async Task<StorageItem> MoveFileAsync(string FileId, string OldParentId, string NewParentId, CancellationToken CancellationToken)
    {
        Guard.NotNullOrWhiteSpace(FileId, nameof(FileId));
        Guard.NotNullOrWhiteSpace(OldParentId, nameof(OldParentId));
        Guard.NotNullOrWhiteSpace(NewParentId, nameof(NewParentId));

        DriveService Service = RequireDriveService();
        DriveFile FileMetadata = new();
        FilesResource.UpdateRequest Request = Service.Files.Update(FileMetadata, FileId);
        Request.AddParents = NewParentId;
        Request.RemoveParents = OldParentId;
        Request.Fields = "id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version";
        DriveFile Updated = await Request.ExecuteAsync(CancellationToken);
        return fMapper.MapFile(Updated);
    }
    /// <summary>
    /// Moves a Google Drive item to trash.
    /// </summary>
    public async Task<StorageItem> TrashFileAsync(string FileId, CancellationToken CancellationToken)
    {
        Guard.NotNullOrWhiteSpace(FileId, nameof(FileId));

        DriveService Service = RequireDriveService();
        DriveFile FileMetadata = new()
        {
            Trashed = true
        };
        FilesResource.UpdateRequest Request = Service.Files.Update(FileMetadata, FileId);
        Request.Fields = "id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version";
        DriveFile Updated = await Request.ExecuteAsync(CancellationToken);
        return fMapper.MapFile(Updated);
    }
    /// <summary>
    /// Restores a Google Drive item from trash.
    /// </summary>
    public async Task<StorageItem> RestoreFileAsync(string FileId, CancellationToken CancellationToken)
    {
        Guard.NotNullOrWhiteSpace(FileId, nameof(FileId));

        DriveService Service = RequireDriveService();
        DriveFile FileMetadata = new()
        {
            Trashed = false
        };
        FilesResource.UpdateRequest Request = Service.Files.Update(FileMetadata, FileId);
        Request.Fields = "id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version";
        DriveFile Updated = await Request.ExecuteAsync(CancellationToken);
        return fMapper.MapFile(Updated);
    }
    /// <summary>
    /// Permanently deletes a Google Drive item.
    /// </summary>
    public async Task DeleteFileAsync(string FileId, CancellationToken CancellationToken)
    {
        Guard.NotNullOrWhiteSpace(FileId, nameof(FileId));

        DriveService Service = RequireDriveService();
        FilesResource.DeleteRequest Request = Service.Files.Delete(FileId);
        await Request.ExecuteAsync(CancellationToken);
    }
    /// <summary>
    /// Lists changes after a page token.
    /// </summary>
    public async Task<StorageChangeListResult> ListChangesAsync(string PageToken, CancellationToken CancellationToken)
    {
        Guard.NotNullOrWhiteSpace(PageToken, nameof(PageToken));

        DriveService Service = RequireDriveService();
        List<StorageChange> Result = new();
        string CurrentToken = PageToken;
        string NewStartPageToken = string.Empty;

        while (!string.IsNullOrWhiteSpace(CurrentToken))
        {
            ChangesResource.ListRequest Request = Service.Changes.List(CurrentToken);
            Request.PageSize = 100;
            Request.IncludeRemoved = true;
            Request.Fields = "changes(fileId,removed,time,file(id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version)),newStartPageToken,nextPageToken";
            ChangeList ChangeList = await Request.ExecuteAsync(CancellationToken);

            if (ChangeList.Changes != null)
            {
                foreach (Change Change in ChangeList.Changes)
                    Result.Add(fMapper.MapChange(Change));
            }

            if (!string.IsNullOrWhiteSpace(ChangeList.NewStartPageToken))
                NewStartPageToken = ChangeList.NewStartPageToken;

            CurrentToken = ChangeList.NextPageToken ?? string.Empty;
        }

        return new StorageChangeListResult(PageToken, NewStartPageToken, Result);
    }

    /// <summary>
    /// Creates a Google Drive folder.
    /// </summary>
    public async Task<StorageItem> CreateFolderAsync(string Name, string ParentId = null, CancellationToken CancellationToken = default)
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
        Request.Fields = "id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version";
        DriveFile Created = await Request.ExecuteAsync(CancellationToken);
        return fMapper.MapFile(Created);
    }
    /// <summary>
    /// Uploads a local file to Google Drive.
    /// </summary>
    public async Task<StorageItem> UploadFileAsync(string LocalFilePath, string ParentId = null, CancellationToken CancellationToken = default)
    {
        Guard.NotNullOrWhiteSpace(LocalFilePath, nameof(LocalFilePath));

        if (!System.IO.File.Exists(LocalFilePath))
            throw new FileNotFoundException("Upload source file was not found.", LocalFilePath);

        DriveService Service = RequireDriveService();
        string FileName = Path.GetFileName(LocalFilePath);
        string MimeType = GetMimeType(LocalFilePath);
        DriveFile FileMetadata = new()
        {
            Name = FileName
        };

        if (!string.IsNullOrWhiteSpace(ParentId))
            FileMetadata.Parents = new List<string> { ParentId };

        await using FileStream Stream = System.IO.File.OpenRead(LocalFilePath);
        FilesResource.CreateMediaUpload Request = Service.Files.Create(FileMetadata, Stream, MimeType);
        Request.Fields = "id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version";
        IUploadProgress Progress = await Request.UploadAsync(CancellationToken);

        if (Progress.Status != UploadStatus.Completed)
            throw new HermesException(Progress.Exception == null ? "Google Drive upload failed." : Progress.Exception.Message);

        return fMapper.MapFile(Request.ResponseBody);
    }
    /// <summary>
    /// Updates the content of an existing Google Drive file.
    /// </summary>
    public async Task<StorageItem> UpdateFileContentAsync(string FileId, string LocalFilePath, CancellationToken CancellationToken)
    {
        Guard.NotNullOrWhiteSpace(FileId, nameof(FileId));
        Guard.NotNullOrWhiteSpace(LocalFilePath, nameof(LocalFilePath));

        if (!System.IO.File.Exists(LocalFilePath))
            throw new FileNotFoundException("Update source file was not found.", LocalFilePath);

        DriveService Service = RequireDriveService();
        string MimeType = GetMimeType(LocalFilePath);
        DriveFile FileMetadata = new();

        await using FileStream Stream = System.IO.File.OpenRead(LocalFilePath);
        FilesResource.UpdateMediaUpload Request = Service.Files.Update(FileMetadata, FileId, Stream, MimeType);
        Request.Fields = "id,name,mimeType,size,md5Checksum,modifiedTime,createdTime,parents,trashed,version";
        IUploadProgress Progress = await Request.UploadAsync(CancellationToken);

        if (Progress.Status != UploadStatus.Completed)
            throw new HermesException(Progress.Exception == null ? "Google Drive content update failed." : Progress.Exception.Message);

        return fMapper.MapFile(Request.ResponseBody);
    }
    /// <summary>
    /// Downloads a Google Drive file to a local file.
    /// </summary>
    public async Task<StorageItem> DownloadFileAsync(string FileId, string LocalFilePath, CancellationToken CancellationToken)
    {
        Guard.NotNullOrWhiteSpace(FileId, nameof(FileId));
        Guard.NotNullOrWhiteSpace(LocalFilePath, nameof(LocalFilePath));

        DriveService Service = RequireDriveService();
        StorageItem Item = await GetFileAsync(FileId, CancellationToken);

        if (Item.IsFolder)
            throw new HermesException("Cannot download a Google Drive folder as a regular file.");
        if (Item.MimeType.StartsWith("application/vnd.google-apps.", StringComparison.Ordinal))
            throw new HermesException("Google Drive Docs Editors files cannot be downloaded as binary content. Export is not supported yet.");

        string FolderPath = Path.GetDirectoryName(LocalFilePath);
        if (!string.IsNullOrWhiteSpace(FolderPath))
            Directory.CreateDirectory(FolderPath);

        await using FileStream Stream = System.IO.File.Create(LocalFilePath);
        FilesResource.GetRequest Request = Service.Files.Get(FileId);
        IDownloadProgress Progress = await Request.DownloadAsync(Stream, CancellationToken);

        if (Progress.Status != DownloadStatus.Completed)
            throw new HermesException(Progress.Exception == null ? "Google Drive download failed." : Progress.Exception.Message);

        return Item;
    }
}
