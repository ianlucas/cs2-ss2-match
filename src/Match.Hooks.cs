/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Match;

public partial class Match
{
    public Natives.CCSPlayerController_ChangeTeamDelegate OnChangeTeam(
        Func<Natives.CCSPlayerController_ChangeTeamDelegate> next
    )
    {
        return (thisPtr, team) =>
        {
            var controller = Core.Memory.ToSchemaClass<CCSPlayerController>(thisPtr);
            var player = controller.ToPlayer()?.GetState();
            if (Game.AreTeamsLocked())
                if (player != null)
                {
                    var currentTeam = (int)player.Team.CurrentTeam;
                    if (team != currentTeam)
                        team = currentTeam;
                }
                else
                    team = (int)Team.Spectator;
            next()(thisPtr, team);
        };
    }

    public Natives.CCSBotManager_MaintainBotQuotaDelegate OnMaintainBotQuota(
        Func<Natives.CCSBotManager_MaintainBotQuotaDelegate> next
    )
    {
        return (thisPtr) =>
        {
            if (!ConVars.IsBots.Value)
            {
                if (DidKickBots)
                    return 0;
                foreach (var player in Core.PlayerManager.GetAllPlayers())
                    if (player.IsFakeClient)
                        player.Kick(
                            "Kicked by match_bots' ConVar.",
                            ENetworkDisconnectionReason.NETWORK_DISCONNECT_KICKED
                        );
                DidKickBots = true;
                return 0;
            }
            var neededPerTeam = ConVars.PlayersNeededPerTeam.Value;
            var teams = new List<(IEnumerable<IPlayer>, string)>()
            {
                (Core.PlayerManager.GetCT(), "ct"),
                (Core.PlayerManager.GetT(), "t"),
            };
            foreach (var (players, team) in teams)
            {
                int bots = 0;
                int humans = 0;
                IPlayer? botToKick = null;
                foreach (var player in players)
                    if (player.IsFakeClient)
                    {
                        bots++;
                        botToKick ??= player;
                    }
                    else
                        humans++;
                if (bots + humans > neededPerTeam && botToKick != null)
                    botToKick.Kick(
                        "Kicked by match_bots' ConVar.",
                        ENetworkDisconnectionReason.NETWORK_DISCONNECT_KICKED
                    );
                if (bots + humans < neededPerTeam)
                    Core.Engine.ExecuteCommand($"bot_add_{team}");
            }
            return next()(thisPtr);
        };
    }
}
