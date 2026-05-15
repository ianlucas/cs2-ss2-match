/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Match.Get5.Events;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;

namespace Match;

public partial class LiveState
{
    private bool _wasPaused = false;
    private string _wasPausedType = "";
    private PlayerTeam? _teamWhichPaused = null;

    public void CheckPauseEvents()
    {
        var gameRules = Runtime.Core.EntitySystem.GetGameRules();
        if (gameRules == null || !gameRules.FreezePeriod)
            return;
        var isTeamPaused = gameRules.TerroristTimeOutActive || gameRules.CTTimeOutActive;
        var isTechnicalPaused = gameRules.TechnicalTimeOut;
        var isMatchPaused = gameRules.MatchWaitingForResume;
        var isPaused = isTeamPaused || isTechnicalPaused || isMatchPaused;
        var didPauseStateChange = _wasPaused != isPaused;
        if (didPauseStateChange)
        {
            if (isPaused)
            {
                Team? sideWhichPaused =
                    gameRules.TerroristTimeOutActive ? Team.T
                    : gameRules.CTTimeOutActive ? Team.CT
                    : null;
                var teamWhichPaused =
                    sideWhichPaused != null
                        ? Rules.Team1.CurrentTeam == sideWhichPaused
                            ? Rules.Team1
                            : Rules.Team2.Opposition
                        : null;
                var pauseType =
                    isTeamPaused ? "team"
                    : isTechnicalPaused ? "technical"
                    : "admin";
                if (teamWhichPaused != null)
                    Runtime.Core.PlayerManager.SendChat(
                        Runtime.Core.Localizer[
                            "match.pause_start",
                            Rules.GetChatPrefix(),
                            teamWhichPaused.FormattedName
                        ]
                    );
                Rules.SendEvent(OnMatchPausedEvent.Create(team: teamWhichPaused, pauseType));
                Rules.SendEvent(OnPauseBeganEvent.Create(team: teamWhichPaused, pauseType));
                _teamWhichPaused = teamWhichPaused;
                _wasPausedType = pauseType;
            }
            else
                Rules.SendEvent(
                    OnMatchUnpausedEvent.Create(team: _teamWhichPaused, pauseType: _wasPausedType)
                );
        }
        _wasPaused = isPaused;
    }

    public void OnPauseCommand(ICommandContext context)
    {
        var player = context.Sender;
        var playerState = player?.GetState();
        if (playerState != null)
        {
            if (ConVars.IsFriendlyPause.Value)
            {
                if (Runtime.Core.EntitySystem.GetGameRules()?.MatchWaitingForResume == true)
                    return;
                Rules.ClearAllTeamUnpauseFlags();
                Runtime.Core.PlayerManager.SendChat(
                    Runtime.Core.Localizer[
                        "match.pause_start",
                        Rules.GetChatPrefix(),
                        playerState.Team.FormattedName
                    ]
                );
                Runtime.Core.Engine.ExecuteCommand("mp_pause_match");
                Rules.SendEvent(
                    OnMatchPausedEvent.Create(team: playerState.Team, pauseType: "tactical")
                );
                return;
            }
            player?.ExecuteCommand("callvote StartTimeOut");
        }
    }

    public void OnUnpauseCommand(ICommandContext context)
    {
        var player = context.Sender;
        var playerState = player?.GetState();
        if (
            playerState != null
            && ConVars.IsFriendlyPause.Value
            && Runtime.Core.EntitySystem.GetGameRules()?.MatchWaitingForResume == true
        )
        {
            var askedForUnpause = playerState.Team.IsUnpauseMatch;
            playerState.Team.IsUnpauseMatch = true;
            if (!Rules.AreAllTeamsReadyToUnpause())
            {
                if (!askedForUnpause)
                    Timers.SetEveryChatInterval(
                        "FriendlyUnpauseInstructions",
                        () =>
                            Runtime.Core.PlayerManager.SendChat(
                                Runtime.Core.Localizer[
                                    "match.pause_unpause1",
                                    Rules.GetChatPrefix(),
                                    playerState.Team.FormattedName,
                                    playerState.Team.Opposition.FormattedName
                                ]
                            )
                    );
                return;
            }
            else
                Runtime.Core.PlayerManager.SendChat(
                    Runtime.Core.Localizer[
                        "match.pause_unpause2",
                        Rules.GetChatPrefix(),
                        playerState.Team.FormattedName
                    ]
                );
            Timers.Clear("FriendlyUnpauseInstructions");
            Runtime.Core.Engine.ExecuteCommand("mp_unpause_match");
            return;
        }
        if (
            player == null
            || (
                player != null
                && Runtime.Core.Permission.PlayerHasPermissions(player.SteamID, ["@css/config"])
            )
        )
        {
            Runtime.Log(
                sendToChat: true,
                message: Runtime.Core.Localizer[
                    "match.admin_unpause",
                    Rules.GetChatPrefix(true),
                    player?.Controller.PlayerName ?? "Console"
                ]
            );
            Timers.Clear("FriendlyUnpauseInstructions");
            Runtime.Core.Engine.ExecuteCommand("mp_unpause_match");
            return;
        }
    }
}
