// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Initializes the Hermes data library.
/// </summary>
static public class DataLib
{
    // ● private

    const string DefaultDbConnectionsFileName = "DbConnections.json";
    static DbLogListenerHermes fLogListener;
    static string GetOutputDefaultDbConnectionsFilePath()
    {
        return Path.Combine(AppContext.BaseDirectory, DefaultDbConnectionsFileName);
    }
    static string GetApplicationDbConnectionsFilePath()
    {
        return Path.Combine(SysConfig.AppFolderPath, DefaultDbConnectionsFileName);
    }
    static bool ShouldCopyFile(string SourceFilePath, string TargetFilePath)
    {
        return !File.Exists(TargetFilePath)
            || File.GetLastWriteTimeUtc(SourceFilePath) > File.GetLastWriteTimeUtc(TargetFilePath);
    }
    static void WriteEmbeddedDbConnectionsFile(string TargetFilePath)
    {
        string JsonText = ResourceFiles.GetResourceFileText(typeof(DataLib).Assembly, string.Empty, DefaultDbConnectionsFileName);
        if (!string.IsNullOrWhiteSpace(JsonText))
            File.WriteAllText(TargetFilePath, JsonText);
    }

    // ● public

    /// <summary>
    /// Forces this assembly to load so Tripous type discovery can find its types.
    /// </summary>
    static public void Load()
    {
        Settings.Load();
    }
    /// <summary>
    /// Ensures the default database connection settings file exists in the application folder.
    /// </summary>
    static public void EnsureDefaultDbConnectionsFile()
    {
        Directory.CreateDirectory(SysConfig.AppFolderPath);

        string SourceFilePath = GetOutputDefaultDbConnectionsFilePath();
        string TargetFilePath = GetApplicationDbConnectionsFilePath();

        if (File.Exists(SourceFilePath))
        {
            if (ShouldCopyFile(SourceFilePath, TargetFilePath))
                File.Copy(SourceFilePath, TargetFilePath, true);

            return;
        }

        if (!File.Exists(TargetFilePath))
            WriteEmbeddedDbConnectionsFile(TargetFilePath);
    }
    /// <summary>
    /// Initializes data-layer services.
    /// </summary>
    static public void Initialize()
    {
        fLogListener = new DbLogListenerHermes();
    }

    // ● properties

    /// <summary>
    /// Gets the application settings.
    /// </summary>
    static public AppSettings Settings { get; } = new();
}
