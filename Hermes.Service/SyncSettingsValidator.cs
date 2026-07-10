// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Service;

/// <summary>
/// Validates synchronization settings used by the background service.
/// </summary>
public class SyncSettingsValidator : IValidateOptions<SyncSettings>
{
    // ● private

    static void RequireText(List<string> Failures, string Value, string Name)
    {
        if (string.IsNullOrWhiteSpace(Value))
            Failures.Add($"{Name} is required.");
    }

    // ● public

    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string Name, SyncSettings Options)
    {
        List<string> Failures = new();

        if (Options == null)
            return ValidateOptionsResult.Fail("Sync settings are required.");

        RequireText(Failures, Options.SyncRootId, nameof(Options.SyncRootId));
        RequireText(Failures, Options.LocalRootPath, nameof(Options.LocalRootPath));
        RequireText(Failures, Options.RemoteRootFolderId, nameof(Options.RemoteRootFolderId));

        if (Options.PollingIntervalSeconds <= 0)
            Failures.Add($"{nameof(Options.PollingIntervalSeconds)} must be greater than zero.");

        return Failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(Failures);
    }
}
