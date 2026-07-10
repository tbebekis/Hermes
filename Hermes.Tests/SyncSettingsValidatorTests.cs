// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests service synchronization settings validation.
/// </summary>
public class SyncSettingsValidatorTests
{
    // ● private

    static SyncSettings ValidSettings() => new()
    {
        SyncRootId = "default",
        LocalRootPath = "/tmp/hermes",
        RemoteRootFolderId = "root",
        PollingIntervalSeconds = 60,
    };

    // ● public

    /// <summary>
    /// Verifies valid settings pass validation.
    /// </summary>
    [Fact]
    public void ValidateAcceptsValidSettings()
    {
        SyncSettingsValidator Validator = new();

        ValidateOptionsResult Result = Validator.Validate(string.Empty, ValidSettings());

        Assert.False(Result.Failed);
    }

    /// <summary>
    /// Verifies missing required values fail validation.
    /// </summary>
    [Fact]
    public void ValidateRejectsMissingRequiredValues()
    {
        SyncSettingsValidator Validator = new();
        SyncSettings Settings = ValidSettings();
        Settings.SyncRootId = string.Empty;
        Settings.LocalRootPath = string.Empty;
        Settings.RemoteRootFolderId = string.Empty;

        ValidateOptionsResult Result = Validator.Validate(string.Empty, Settings);

        Assert.True(Result.Failed);
        Assert.Contains("SyncRootId is required.", Result.Failures);
        Assert.Contains("LocalRootPath is required.", Result.Failures);
        Assert.Contains("RemoteRootFolderId is required.", Result.Failures);
    }

    /// <summary>
    /// Verifies invalid polling interval fails validation.
    /// </summary>
    [Fact]
    public void ValidateRejectsInvalidPollingInterval()
    {
        SyncSettingsValidator Validator = new();
        SyncSettings Settings = ValidSettings();
        Settings.PollingIntervalSeconds = 0;

        ValidateOptionsResult Result = Validator.Validate(string.Empty, Settings);

        Assert.True(Result.Failed);
        Assert.Contains("PollingIntervalSeconds must be greater than zero.", Result.Failures);
    }
}
