// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests Hermes data registry descriptors.
/// </summary>
public class RegistryTests
{
    // ● public

    /// <summary>
    /// Verifies synchronization conflict module registration.
    /// </summary>
    [Fact]
    public void RegisterDescriptorsRegistersSyncConflictModule()
    {
        Registry.RegisterDescriptors();

        ModuleDef Module = DataRegistry.Modules.Get("SyncConflict");

        Assert.NotNull(Module);
        Assert.Equal("SyncConflictDataModule", Module.ClassName);
        Assert.Equal("SYNC_CONFLICT", Module.Table.Name);
        Assert.Equal("Id", Module.Table.KeyField);
        Assert.Contains("State", Module.Table.Fields.Select(Item => Item.Name));
    }
}
