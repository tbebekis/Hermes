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
    private const string CredentialsFolderName = "Credentials";
    private const string ClientSecretFileName = "client_secret.json";
    private const string TokenFileName = "google-token.json";
    private readonly string[] fScopes = [DriveService.Scope.DriveMetadataReadonly];

    // ● private

    /// <summary>
    /// Stores the Google OAuth token in the configured single token file.
    /// </summary>
    private sealed class GoogleDriveTokenDataStore : IDataStore
    {
        // ● private

        private readonly string fTokenFilePath;

        // ● constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleDriveTokenDataStore"/> class.
        /// </summary>
        public GoogleDriveTokenDataStore(string TokenFilePath)
        {
            fTokenFilePath = Guard.NotNullOrWhiteSpace(TokenFilePath, nameof(TokenFilePath));
        }

        // ● public

        /// <inheritdoc/>
        public Task ClearAsync()
        {
            if (System.IO.File.Exists(fTokenFilePath))
                System.IO.File.Delete(fTokenFilePath);

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteAsync<T>(string Key)
        {
            if (System.IO.File.Exists(fTokenFilePath))
                System.IO.File.Delete(fTokenFilePath);

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<T> GetAsync<T>(string Key)
        {
            if (!System.IO.File.Exists(fTokenFilePath))
                return Task.FromResult(default(T));

            string JsonText = System.IO.File.ReadAllText(fTokenFilePath);
            T Result = NewtonsoftJsonSerializer.Instance.Deserialize<T>(JsonText);
            return Task.FromResult(Result);
        }

        /// <inheritdoc/>
        public Task StoreAsync<T>(string Key, T Value)
        {
            string FolderPath = Path.GetDirectoryName(fTokenFilePath);
            if (!string.IsNullOrWhiteSpace(FolderPath))
                Directory.CreateDirectory(FolderPath);

            string JsonText = NewtonsoftJsonSerializer.Instance.Serialize(Value);
            System.IO.File.WriteAllText(fTokenFilePath, JsonText);
            return Task.CompletedTask;
        }
    }

    private async Task<UserCredential> RefreshIfNeededAsync(UserCredential Credential, CancellationToken CancellationToken)
    {
        if (Credential.Token != null && Credential.Token.IsStale)
            await Credential.RefreshTokenAsync(CancellationToken);

        return Credential;
    }

    // ● public

    /// <summary>
    /// Authenticates the user.
    /// </summary>
    public async Task<UserCredential> AuthenticateAsync(CancellationToken CancellationToken)
    {
        string CredentialsFolder = GetHermesCredentialsFolder();
        string ClientSecretPath = GetClientSecretFilePath();
        string TokenFilePath = GetTokenFilePath();

        if (!System.IO.File.Exists(ClientSecretPath))
            throw new FileNotFoundException("Google OAuth client secrets file was not found.", ClientSecretPath);

        Directory.CreateDirectory(CredentialsFolder);

        await using FileStream Stream = new(ClientSecretPath, FileMode.Open, FileAccess.Read);
        ClientSecrets Secrets = GoogleClientSecrets.FromStream(Stream).Secrets;
        UserCredential Credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            Secrets,
            fScopes,
            UserName,
            CancellationToken,
            new GoogleDriveTokenDataStore(TokenFilePath));

        return await RefreshIfNeededAsync(Credential, CancellationToken);
    }

    /// <summary>
    /// Gets the Hermes configuration folder.
    /// </summary>
    static public string GetHermesConfigFolder()
    {
        if (string.IsNullOrWhiteSpace(SysConfig.AppName))
            SysConfig.AppName = CommonConstants.ApplicationName;

        return SysConfig.AppFolderPath;
    }

    /// <summary>
    /// Gets the Hermes Google credentials folder.
    /// </summary>
    static public string GetHermesCredentialsFolder()
    {
        return Path.Combine(GetHermesConfigFolder(), CredentialsFolderName);
    }

    /// <summary>
    /// Gets the OAuth client secret file path.
    /// </summary>
    static public string GetClientSecretFilePath()
    {
        return Path.Combine(GetHermesCredentialsFolder(), ClientSecretFileName);
    }

    /// <summary>
    /// Gets the OAuth token file path.
    /// </summary>
    static public string GetTokenFilePath()
    {
        return Path.Combine(GetHermesCredentialsFolder(), TokenFileName);
    }
}
