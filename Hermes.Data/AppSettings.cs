// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Application settings stored in the Hermes application folder.
/// </summary>
public class AppSettings : SettingsBase
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="AppSettings"/> class.
    /// </summary>
    public AppSettings()
    {
    }

    // ● properties

    /// <summary>
    /// Gets or sets Google-related settings.
    /// </summary>
    public GoogleSettings Google { get; set; } = new();
}
