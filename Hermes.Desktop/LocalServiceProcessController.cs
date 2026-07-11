// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Desktop;

/// <summary>
/// Starts the local Hermes service process in development mode.
/// </summary>
public class LocalServiceProcessController
{
    // ● private

    static string ConfigurationName
    {
        get
        {
#if DEBUG
            return "Debug";
#else
            return "Release";
#endif
        }
    }
    static string ServiceEnvironmentPath => Environment.GetEnvironmentVariable("HERMES_SERVICE_PATH") ?? string.Empty;
    static string DevServiceAssemblyPath
    {
        get
        {
            string BasePath = AppContext.BaseDirectory;
            string ProjectPath = Path.GetFullPath(Path.Combine(BasePath, "..", "..", "..", "..", "Hermes.Service"));
            return Path.Combine(ProjectPath, "bin", ConfigurationName, "net10.0", "Hermes.Service.dll");
        }
    }
    static string ResolveServicePath()
    {
        if (!string.IsNullOrWhiteSpace(ServiceEnvironmentPath) && File.Exists(ServiceEnvironmentPath))
            return ServiceEnvironmentPath;

        string LocalExecutable = Path.Combine(AppContext.BaseDirectory, "Hermes.Service");
        if (File.Exists(LocalExecutable))
            return LocalExecutable;

        string LocalAssembly = Path.Combine(AppContext.BaseDirectory, "Hermes.Service.dll");
        if (File.Exists(LocalAssembly))
            return LocalAssembly;

        if (File.Exists(DevServiceAssemblyPath))
            return DevServiceAssemblyPath;

        return string.Empty;
    }
    static ProcessStartInfo CreateStartInfo(string ServicePath)
    {
        ProcessStartInfo Result;

        if (Path.GetExtension(ServicePath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
        {
            Result = new ProcessStartInfo("dotnet", "\"" + ServicePath + "\"");
        }
        else
        {
            Result = new ProcessStartInfo(ServicePath);
        }

        Result.WorkingDirectory = Path.GetDirectoryName(ServicePath) ?? AppContext.BaseDirectory;
        Result.UseShellExecute = false;
        Result.CreateNoWindow = true;
        return Result;
    }

    // ● public

    /// <summary>
    /// Starts the local Hermes service process.
    /// </summary>
    public LocalServiceControlResult Start()
    {
        string ServicePath = ResolveServicePath();

        if (string.IsNullOrWhiteSpace(ServicePath))
            return LocalServiceControlResult.Failure("Hermes.Service executable was not found.");

        try
        {
            Process Process = Process.Start(CreateStartInfo(ServicePath));

            if (Process == null)
                return LocalServiceControlResult.Failure("Hermes.Service process could not be started.");

            return LocalServiceControlResult.Success("Hermes.Service start requested.");
        }
        catch (Exception Ex)
        {
            return LocalServiceControlResult.Failure(Ex.Message);
        }
    }
}
