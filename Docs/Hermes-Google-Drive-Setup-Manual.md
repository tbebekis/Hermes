# Hermes Developer Manual

## Google Drive API Setup

This document describes how a developer sets up Google Cloud and Google Drive API access for the Hermes project.

Hermes uses Google OAuth for desktop applications. The application never asks for the user's Google password. Authentication is performed through the browser and Google returns OAuth tokens to the application.

---

## 1. Create a Google Cloud Project

Open Google Cloud Console:

<https://console.cloud.google.com/>

Sign in with the Google account you want to use for Hermes development.

For development it is recommended to use a separate Google account, not your primary personal account.

In the top project selector, choose:

```text
Select a project
```

Then click:

```text
New project
```

Use:

```text
Project name: Hermes
Project ID: hermes-sync-engine
Location: No organization
```

The Project ID must be globally unique. If `hermes-sync-engine` is not available, choose another stable ID.

Click:

```text
Create
```

After the project is created, select it from the project selector.

---

## 2. Enable Google Drive API

In the left menu go to:

```text
APIs & Services
```

Then open:

```text
Enabled APIs & services
```

Click:

```text
+ Enable APIs and services
```

Search for:

```text
Google Drive API
```

Open the **Google Drive API** result and click:

```text
Enable
```

Hermes needs this API for file metadata, changes, folders, uploads, downloads and later synchronization.

---

## 3. Configure Google Auth Platform

After enabling the Drive API, go to:

```text
Google Auth Platform
```

or from the left menu:

```text
APIs & Services
OAuth consent screen
```

If this is the first time configuring OAuth for the project, click:

```text
Get started
```

### App information

Use:

```text
App name: Hermes
User support email: <your development Google account email>
```

Click:

```text
Next
```

### Audience

Choose:

```text
External
```

For a personal Google account, `Internal` is not available. `External` is the correct choice for development and testing.

Click:

```text
Next
```

### Contact information

Use the same development Google account email.

Click:

```text
Next
```

### Finish

Check:

```text
I agree to the Google API Services: User Data Policy
```

Click:

```text
Create
```

---

## 4. Add Test User

While the app is in Testing mode, only explicitly added test users can authenticate.

Go to:

```text
Google Auth Platform
Audience
```

Find the section:

```text
Test users
```

Add the Google account that will be used for testing Hermes.

Example:

```text
damocles.syracusian@gmail.com
```

Save the changes.

If this step is skipped, Google will reject OAuth login with a message saying the app is in testing and the user has not been granted access.

---

## 5. Create OAuth Desktop Client

Go to:

```text
Google Auth Platform
Clients
```

Click:

```text
Create OAuth client
```

Use:

```text
Application type: Desktop app
Name: Hermes Desktop
```

Click:

```text
Create
```

Google will create an OAuth Client ID and Client Secret.

Click the download icon to download the OAuth JSON file.

The file name will look similar to:

```text
client_secret_1234567890-xxxxxxxxxxxxxxxx.apps.googleusercontent.com.json
```

Do not commit this file to GitHub.

---

## 6. Install OAuth JSON for Hermes

Hermes expects the OAuth configuration file at:

```text
/home/teo/.config/Hermes/Credentials/client_secret.json
```

Create the folder and copy the downloaded file:

```bash
mkdir -p ~/.config/Hermes/Credentials
cp ~/Downloads/client_secret_*.json ~/.config/Hermes/Credentials/client_secret.json
chmod 600 ~/.config/Hermes/Credentials/client_secret.json
```

Verify:

```bash
ls -lah ~/.config/Hermes/Credentials
```

Expected result:

```text
client_secret.json
```

The file should be readable only by the current user.

---

## 7. OAuth Token Storage

After the first successful authentication, Hermes stores the OAuth token here:

```text
/home/teo/.config/Hermes/Credentials/google-token.json
```

The first `Connect` operation opens the browser and asks the user to approve access.

The second `Connect` operation should reuse the saved token and should not open the browser again.

If the access token is stale, the Google client library refreshes it automatically using the refresh token.

---

## 8. Current Hermes OAuth Scope

For the first development phase Hermes uses only metadata read access:

```csharp
DriveService.Scope.DriveMetadataReadonly
```

This is enough for:

- authentication
- About API
- listing metadata
- getting start page token
- listing changes

It is not enough for upload, download, delete or full synchronization.

Broader scopes will be added later only when needed.

---

## 9. TestApp Validation

Run `TestApp`.

Use the `Connect` button.

Expected log for first successful authentication:

```text
authentication: started
starting authentication
client secret: /home/teo/.config/Hermes/Credentials/client_secret.json
token file: /home/teo/.config/Hermes/Credentials/google-token.json
authentication succeeded
authentication: completed
```

Then use the `About` button.

Expected result:

- DriveService is created
- Drive About API succeeds
- log shows app name, user email and quota information if available

---

## 10. Important Notes

Hermes must never ask the user for a Google password.

The correct login flow is:

```text
Hermes
  -> opens browser
  -> Google login / consent
  -> OAuth token returned
  -> Hermes stores token locally
```

For development builds each developer may create their own Google Cloud project and OAuth desktop client.

For a future public release, Hermes may need an official verified OAuth client so that end users do not have to create their own Google Cloud project.

---

## 11. Troubleshooting

### Error: app is in testing and user has no access

Add the Google account under:

```text
Google Auth Platform
Audience
Test users
```

### Browser opens every time

Check that the token file exists:

```text
~/.config/Hermes/Credentials/google-token.json
```

### Client secret not found

Check that the file exists:

```text
~/.config/Hermes/Credentials/client_secret.json
```

### Wrong Google account used

Delete or move the token file and authenticate again:

```bash
rm ~/.config/Hermes/Credentials/google-token.json
```

Then press `Connect` again in TestApp.

---

## 12. Current Status

At the time this document was written, Hermes has verified:

- Google Cloud project creation
- Google Drive API enabled
- OAuth consent / Google Auth Platform configured
- OAuth Desktop Client created
- `client_secret.json` installed locally
- OAuth authentication succeeds
- token reuse works
- Drive About API succeeds

The next exploration steps are:

- Get Start Page Token
- List Root Folder
- Get File metadata
- List Changes
- Study the Google Drive `File` resource in detail before writing sync logic

