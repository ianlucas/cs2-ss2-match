/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Players;

namespace Match;

public static class IPlayerManagerServiceExtensions
{
    extension(IPlayerManagerService self)
    {
        public IPlayer? GetPlayerFromSteamID(ulong steamID)
        {
            return self.GetAllPlayers().FirstOrDefault(p => p.SteamID == steamID);
        }

        public IEnumerable<IPlayer> GetPlayersInTeams()
        {
            return self.GetAllPlayers()
                .Where(p => p.Pawn?.TeamNum == (int)Team.CT || p.Pawn?.TeamNum == (int)Team.T);
        }

        public void UpdateScoreboards()
        {
            Swiftly.Core.GameEvent.Fire<EventNextlevelChanged>();
        }
    }
}
