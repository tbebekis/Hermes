// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Google-related application settings.
/// </summary>
public class GoogleSettings
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleSettings"/> class.
    /// </summary>
    public GoogleSettings()
    {
    }

    // ● properties

    /// <summary>
    /// Gets or sets the full path of the local Google Drive mirror folder.
    /// </summary>
    public string DriveFolderPath { get; set; } = string.Empty;
}
