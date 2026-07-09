// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Initializes the Hermes data library.
/// </summary>
static public class DataLib
{
    // ● private

    static private DbLogListenerHermes fLogListener;

    // ● public

    /// <summary>
    /// Forces this assembly to load so Tripous type discovery can find its types.
    /// </summary>
    static public void Load()
    {
    }

    /// <summary>
    /// Initializes data-layer services.
    /// </summary>
    static public void Initialize()
    {
        fLogListener = new DbLogListenerHermes();
    }
}
