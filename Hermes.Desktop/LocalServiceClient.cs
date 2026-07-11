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
            using Stream Stream = await fClient.GetStreamAsync("/status");
            return await JsonSerializer.DeserializeAsync<LocalServiceStatus>(Stream, fJsonOptions);
        }
        catch
        {
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
            using Stream Stream = await fClient.GetStreamAsync("/conflicts/open");
            List<LocalOpenConflict> Result = await JsonSerializer.DeserializeAsync<List<LocalOpenConflict>>(Stream, fJsonOptions);
            return Result ?? [];
        }
        catch
        {
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
            using Stream Stream = await fClient.GetStreamAsync("/logs/recent");
            List<LocalRecentLog> Result = await JsonSerializer.DeserializeAsync<List<LocalRecentLog>>(Stream, fJsonOptions);
            return Result ?? [];
        }
        catch
        {
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
            using HttpResponseMessage Response = await fClient.PostAsync("/control/stop", new StringContent(string.Empty));
            using Stream Stream = await Response.Content.ReadAsStreamAsync();
            LocalServiceControlResult Result = await JsonSerializer.DeserializeAsync<LocalServiceControlResult>(Stream, fJsonOptions);

            if (Result == null)
                return LocalServiceControlResult.Failure("The local service returned an empty response.");

            return Result;
        }
        catch (Exception Ex)
        {
            return LocalServiceControlResult.Failure(Ex.Message);
        }
    }
}
