/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.SchemaDefinitions;

namespace Match;

public static class CCSGameRulesExtensions
{
    extension(CCSGameRules self)
    {
        public bool AreTeamsPlayingSwitchedSides()
        {
            return Natives.CCSGameRules_AreTeamsPlayingSwitchedSides.Call(self.Address);
        }
    }
}
