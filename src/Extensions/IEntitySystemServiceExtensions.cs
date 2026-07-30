/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.EntitySystem;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Match;

public static class IEntitySystemServiceExtensions
{
    extension(IEntitySystemService self)
    {
        public IEnumerable<CCSPlayerPawn> GetAlivePawnsInTeam(Team team)
        {
            return self.GetAllEntitiesByDesignerName<CCSPlayerPawn>("player")
                .Where(pawn => pawn.LifeState == 0 && pawn.TeamNum == (byte)team);
        }
    }
}
