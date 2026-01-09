/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace Match;

public partial class LiveState
{
    private PlayerTeam? _surrendingTeam;
    private bool _canSurrender = false;

    public void OnSurrenderCommand(ICommandContext context)
    {
        var player = context.Sender;
        var playerState = player?.GetState();
        if (
            playerState != null
            && _canSurrender
            && (_surrendingTeam == null || _surrendingTeam == playerState.Team)
            && !playerState.Team.SurrenderVotes.Contains(playerState.SteamID)
        )
        {
            _surrendingTeam = playerState.Team;
            playerState.Team.SurrenderVotes.Add(playerState.SteamID);
            var neededVotes = playerState.Team.Players.Count(p => p.Handle != null);
            var timerName = $"surrender{playerState.Team.Index}";
            if (playerState.Team.SurrenderVotes.Count >= neededVotes)
            {
                if (!_canSurrender)
                    return;
                Timers.Clear(timerName);
                Swiftly.Core.PlayerManager.SendChat(
                    Swiftly.Core.Localizer[
                        "match.surrender_success",
                        Game.GetChatPrefix(),
                        playerState.Team.FormattedName
                    ]
                );
                playerState.Team.IsSurrended = true;
                playerState.Team.Score = 0;
                playerState.Team.Opposition.Score = 1;
                Game.Log("Terminating by Surrender");
                Swiftly
                    .Core.EntitySystem.GetGameRules()
                    ?.TerminateRound(
                        playerState.Team.CurrentTeam == Team.T
                            ? RoundEndReason.TerroristsSurrender
                            : RoundEndReason.CTsSurrender,
                        0
                    );
            }
            else if (playerState.Team.SurrenderVotes.Count == 1)
            {
                playerState.Team.SendChat(
                    Swiftly.Core.Localizer[
                        "match.surrender_start",
                        Game.GetChatPrefix(),
                        playerState.Name,
                        neededVotes,
                        ConVars.SurrenderTimeout.Value
                    ]
                );
                Timers.Set(
                    timerName,
                    ConVars.SurrenderTimeout.Value,
                    () =>
                    {
                        _surrendingTeam = null;
                        var hadAllSurrenderVotes =
                            playerState.Team.SurrenderVotes.Count == playerState.Team.Players.Count;
                        playerState.Team.SurrenderVotes.Clear();
                        playerState.Team.SendChat(
                            Swiftly.Core.Localizer[
                                hadAllSurrenderVotes
                                    ? "match.surrender_fail1"
                                    : "match.surrender_fail2",
                                Game.GetChatPrefix()
                            ]
                        );
                    }
                );
            }
        }
    }
}
