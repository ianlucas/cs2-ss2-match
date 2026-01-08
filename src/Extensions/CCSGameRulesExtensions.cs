/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Players;
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

        public void HandleSwapTeams()
        {
            Natives.CCSGameRules_HandleSwapTeams.Call(self.Address);
        }

        public Team DetermineWinnerBySurvival()
        {
            var tPlayers = Swiftly.Core.PlayerManager.GetT();
            var ctPlayers = Swiftly.Core.PlayerManager.GetCT();
            int tAlive = tPlayers.Count(CountAlive);
            int tHealth = tPlayers.Sum(SumHealth);
            int ctAlive = ctPlayers.Count(CountAlive);
            int ctHealth = ctPlayers.Sum(SumHealth);
            if (ctAlive != tAlive)
            {
                var winner = ctAlive > tAlive ? Team.CT : Team.T;
                Game.Log($"(Alive ct={ctAlive} t={tAlive}) winner={winner}");
                return winner;
            }
            if (ctHealth != tHealth)
            {
                var winner = ctHealth > tHealth ? Team.CT : Team.T;
                Game.Log($"(Health ct={ctHealth} t={tHealth}) winner={winner}");
                return winner;
            }
            var randomWinner = (Team)new Random().Next(2, 4);
            Game.Log($"(Random) randomWinner={randomWinner}");
            return randomWinner;
            static bool CountAlive(IPlayer player) => player.GetHealth() > 0;
            static int SumHealth(IPlayer player) => player.GetHealth();
        }
    }
}
