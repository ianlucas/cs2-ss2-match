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

        public IEnumerable<IPlayer> GetAliveInTeam(Team team)
        {
            return self.GetAlive().Where(p => p.Controller.Team == team);
        }

        public IEnumerable<IPlayer> GetPlayersInTeams()
        {
            return self.GetAllPlayers()
                .Where(p => p.Pawn?.TeamNum == (int)Team.CT || p.Pawn?.TeamNum == (int)Team.T);
        }

        public IEnumerable<IPlayer> GetActualPlayers()
        {
            return self.GetAllPlayers().Where(p => !p.IsFakeClient);
        }

        public void UpdateScoreboards()
        {
            Swiftly.Core.GameEvent.Fire<EventNextlevelChanged>();
        }

        public void RemovePlayerClans()
        {
            bool didUpdatePlayers = false;
            foreach (var player in self.GetAllPlayers())
                if (player.SetPlayerClan(""))
                    didUpdatePlayers = true;
            if (didUpdatePlayers)
                self.UpdateScoreboards();
        }

        public void SendChatRepeat(string message, int amount = 3)
        {
            for (var n = 0; n < amount; n++)
                self.SendChat(message);
        }
    }
}
