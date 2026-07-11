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
}
