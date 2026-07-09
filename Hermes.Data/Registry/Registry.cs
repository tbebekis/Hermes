// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Registers Hermes data schema and descriptors.
/// </summary>
static public class Registry
{
    // ● private

    static private readonly List<SchemaVersionDef> SchemaVersionList = new()
    {
        new SchemaVersion1()
    };

    static private readonly List<RegistryVersion> RegistryVersionList = new()
    {
        new RegistryVersion1()
    };

    // ● public

    /// <summary>
    /// Registers database schema versions.
    /// </summary>
    static public void RegisterSchemas()
    {
        foreach (SchemaVersionDef Version in SchemaVersionList)
            Version.Register();
    }

    /// <summary>
    /// Registers data descriptors.
    /// </summary>
    static public void RegisterDescriptors()
    {
        foreach (RegistryVersion Version in RegistryVersionList)
        {
            Version.RegisterLookups();
            Version.RegisterLookupSources();
            Version.RegisterLocators();
            Version.RegisterCodeProviders();
            Version.RegisterModules();
            Version.RegisterFactBoxes();
        }

        DataRegistry.UpdateLocatorReferences();
    }
}
