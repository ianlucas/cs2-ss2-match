/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Players;

namespace Match;

public static class IPlayerExtensions
{
    extension(IPlayer self)
    {
        public bool SetPlayerClan(string clan)
        {
            if (self.Controller.Clan != clan)
            {
                self.Controller.Clan = clan;
                self.Controller.ClanUpdated();
                return true;
            }
            return false;
        }

        public int GetHealth()
        {
            return self.Controller.GetHealth();
        }

        public PlayerState? GetState()
        {
            return Game
                .Teams.SelectMany(t => t.Players)
                .FirstOrDefault(p => p.SteamID == self.SteamID);
        }
    }
}
