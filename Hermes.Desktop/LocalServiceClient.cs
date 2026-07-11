// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Calls the local Hermes service HTTP API.
/// </summary>
public class LocalServiceClient
{
    // ● fields

    readonly HttpClient fClient;
    readonly JsonSerializerOptions fJsonOptions;

    // ● private

    void ClearError()
    {
        LastErrorMessage = string.Empty;
    }
    void SetError(Exception Ex)
    {
        LastErrorMessage = Ex.GetType().Name + ": " + Ex.Message;
    }
    async Task<T> GetJsonAsync<T>(string Path)
    {
        ClearError();
        using HttpResponseMessage Response = await fClient.GetAsync(Path);

        if (!Response.IsSuccessStatusCode)
        {
            LastErrorMessage = ((int)Response.StatusCode).ToString() + " " + Response.ReasonPhrase + ": " + await Response.Content.ReadAsStringAsync();
            return default;
        }

        using Stream Stream = await Response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<T>(Stream, fJsonOptions);
    }

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalServiceClient"/> class.
    /// </summary>
    public LocalServiceClient()
    {
        fClient = new HttpClient()
        {
            BaseAddress = new Uri("http://127.0.0.1:8765"),
            Timeout = TimeSpan.FromSeconds(2),
        };
        fJsonOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
        };
    }

    // ● public

    /// <summary>
    /// Gets the local service status, or returns null when the service cannot be reached.
    /// </summary>
    public async Task<LocalServiceStatus> GetStatusAsync()
    {
        try
        {
            return await GetJsonAsync<LocalServiceStatus>("/status");
        }
        catch (Exception Ex)
        {
            SetError(Ex);
            return null;
        }
    }
    /// <summary>
    /// Gets open conflicts from the local service, or returns null when the service cannot be reached.
    /// </summary>
    public async Task<IReadOnlyList<LocalOpenConflict>> GetOpenConflictsAsync()
    {
        try
        {
            List<LocalOpenConflict> Result = await GetJsonAsync<List<LocalOpenConflict>>("/conflicts/open");
            if (Result == null && !string.IsNullOrWhiteSpace(LastErrorMessage))
                return null;

            return Result ?? [];
        }
        catch (Exception Ex)
        {
            SetError(Ex);
            return null;
        }
    }
    /// <summary>
    /// Gets recent logs from the local service, or returns null when the service cannot be reached.
    /// </summary>
    public async Task<IReadOnlyList<LocalRecentLog>> GetRecentLogsAsync()
    {
        try
        {
            List<LocalRecentLog> Result = await GetJsonAsync<List<LocalRecentLog>>("/logs/recent");
            if (Result == null && !string.IsNullOrWhiteSpace(LastErrorMessage))
                return null;

            return Result ?? [];
        }
        catch (Exception Ex)
        {
            SetError(Ex);
            return null;
        }
    }
    /// <summary>
    /// Requests the local service to stop.
    /// </summary>
    public async Task<LocalServiceControlResult> StopAsync()
    {
        try
        {
            ClearError();
            using HttpResponseMessage Response = await fClient.PostAsync("/control/stop", new StringContent(string.Empty));
            using Stream Stream = await Response.Content.ReadAsStreamAsync();
            LocalServiceControlResult Result = await JsonSerializer.DeserializeAsync<LocalServiceControlResult>(Stream, fJsonOptions);

            if (Result == null)
                return LocalServiceControlResult.Failure("The local service returned an empty response.");

            return Result;
        }
        catch (Exception Ex)
        {
            SetError(Ex);
            return LocalServiceControlResult.Failure(Ex.Message);
        }
    }

    // ● properties

    /// <summary>
    /// Gets the latest HTTP API error message.
    /// </summary>
    public string LastErrorMessage { get; private set; } = string.Empty;
}
