// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Writes Tripous log entries to the Hermes log table.
/// </summary>
internal class DbLogListenerHermes : SyncedLogListener
{
    // ● private

    readonly DataModule fModule;

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DbLogListenerHermes"/> class.
    /// </summary>
    public DbLogListenerHermes()
    {
        ModuleDef ModuleDef = DataRegistry.Modules.Get("Log");
        fModule = ModuleDef.Create();
    }

    // ● public

    /// <inheritdoc/>
    public override void ProcessLogSynced(LogEntry Entry)
    {
        try
        {
            LogRecord LogRecord = new(Entry);
            fModule.Insert();
            DataRow Row = fModule.tblItem.Rows[0];
            LogRecord.AddToRow(Row);
            fModule.Commit();
        }
        catch (Exception Ex)
        {
            Console.WriteLine(Ex);
        }
    }
}
