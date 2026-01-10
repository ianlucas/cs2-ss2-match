/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.SchemaDefinitions;

namespace Match;

public static class CCSPlayerControllerExtensions
{
    extension(CCSPlayerController self)
    {
        public int GetHealth()
        {
            return Math.Max(
                (self.SteamID == 0 ? self.Pawn.Value : self.PlayerPawn.Value)?.Health ?? 0,
                0
            );
        }

        public PlayerState? GetState()
        {
            return Game
                .Teams.SelectMany(t => t.Players)
                .FirstOrDefault(p => p.SteamID == self.SteamID);
        }
    }
}
