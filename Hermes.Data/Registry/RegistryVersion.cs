// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Base class for Hermes registry versions.
/// </summary>
public class RegistryVersion
{
    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistryVersion"/> class.
    /// </summary>
    public RegistryVersion()
    {
    }

    // ● public

    /// <summary>
    /// Registers data modules.
    /// </summary>
    public virtual void RegisterModules()
    {
    }

    /// <summary>
    /// Registers module fact boxes.
    /// </summary>
    public virtual void RegisterFactBoxes()
    {
    }

    /// <summary>
    /// Registers lookups.
    /// </summary>
    public virtual void RegisterLookups()
    {
    }

    /// <summary>
    /// Registers lookup sources.
    /// </summary>
    public virtual void RegisterLookupSources()
    {
    }

    /// <summary>
    /// Registers locators.
    /// </summary>
    public virtual void RegisterLocators()
    {
    }

    /// <summary>
    /// Registers code providers.
    /// </summary>
    public virtual void RegisterCodeProviders()
    {
    }

    // ● properties

    /// <summary>
    /// Gets the registry version number.
    /// </summary>
    public virtual int VersionNumber { get; } = -1;
}
