// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.GoogleDrive;

/// <summary>
/// Handles Google Drive installed application authentication.
/// </summary>
public class GoogleDriveAuthManager
{
    // ● private

    private const string UserName = "user";
    private readonly string[] fScopes =
    [
        DriveService.Scope.DriveMetadataReadonly,
        DriveService.Scope.DriveFile
    ];

    // ● public

    /// <summary>
    /// Authenticates the user and creates a Drive service.
    /// </summary>
    public async Task<DriveService> AuthenticateAsync(CancellationToken CancellationToken)
    {
        string ConfigFolder = GetHermesConfigFolder();
        string ClientSecretPath = Path.Combine(ConfigFolder, "client_secret.json");
        string TokenFolder = Path.Combine(ConfigFolder, "tokens");

        if (!System.IO.File.Exists(ClientSecretPath))
            throw new FileNotFoundException("Google OAuth client secrets file was not found.", ClientSecretPath);

        Directory.CreateDirectory(TokenFolder);

        await using FileStream Stream = new(ClientSecretPath, FileMode.Open, FileAccess.Read);
        ClientSecrets Secrets = GoogleClientSecrets.FromStream(Stream).Secrets;
        UserCredential Credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            Secrets,
            fScopes,
            UserName,
            CancellationToken,
            new FileDataStore(TokenFolder, true));

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = Credential,
            ApplicationName = CommonConstants.ApplicationName
        });
    }

    /// <summary>
    /// Gets the Hermes configuration folder.
    /// </summary>
    static public string GetHermesConfigFolder()
    {
        string HomeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(HomeFolder, ".config", "hermes");
    }
}
