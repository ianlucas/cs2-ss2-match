/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;

namespace Match;

[PluginMetadata(
    Id = "Match",
    Version = "1.0.0",
    Name = "Match",
    Author = "Ian Lucas",
    Description = "A match coordinator plugin."
)]
public partial class Match(ISwiftlyCore core) : BasePlugin(core)
{
    public override void Load(bool hotReload)
    {
        Swiftly.Initialize();
        ConVars.Initialize();
    }

    public override void Unload() { }
}
