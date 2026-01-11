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
        var gameRules = Swiftly.Core.EntitySystem.GetGameRules();
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
                        ? Game.Team1.CurrentTeam == sideWhichPaused
                            ? Game.Team1
                            : Game.Team2.Opposition
                        : null;
                var pauseType =
                    isTeamPaused ? "team"
                    : isTechnicalPaused ? "technical"
                    : "admin";
                if (teamWhichPaused != null)
                    Swiftly.Core.PlayerManager.SendChat(
                        Swiftly.Core.Localizer[
                            "match.pause_start",
                            Game.GetChatPrefix(),
                            teamWhichPaused.FormattedName
                        ]
                    );
                Game.SendEvent(OnMatchPausedEvent.Create(team: teamWhichPaused, pauseType));
                Game.SendEvent(OnPauseBeganEvent.Create(team: teamWhichPaused, pauseType));
                _teamWhichPaused = teamWhichPaused;
                _wasPausedType = pauseType;
            }
            else
                Game.SendEvent(
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
                if (Swiftly.Core.EntitySystem.GetGameRules()?.MatchWaitingForResume == true)
                    return;
                Game.ClearAllTeamUnpauseFlags();
                Swiftly.Core.PlayerManager.SendChat(
                    Swiftly.Core.Localizer[
                        "match.pause_start",
                        Game.GetChatPrefix(),
                        playerState.Team.FormattedName
                    ]
                );
                Swiftly.Core.Engine.ExecuteCommand("mp_pause_match");
                Game.SendEvent(
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
            && Swiftly.Core.EntitySystem.GetGameRules()?.MatchWaitingForResume == true
        )
        {
            var askedForUnpause = playerState.Team.IsUnpauseMatch;
            playerState.Team.IsUnpauseMatch = true;
            if (!Game.AreAllTeamsReadyToUnpause())
            {
                if (!askedForUnpause)
                    Timers.SetEveryChatInterval(
                        "FriendlyUnpauseInstructions",
                        () =>
                            Swiftly.Core.PlayerManager.SendChat(
                                Swiftly.Core.Localizer[
                                    "match.pause_unpause1",
                                    Game.GetChatPrefix(),
                                    playerState.Team.FormattedName,
                                    playerState.Team.Opposition.FormattedName
                                ]
                            )
                    );
                return;
            }
            else
                Swiftly.Core.PlayerManager.SendChat(
                    Swiftly.Core.Localizer[
                        "match.pause_unpause2",
                        Game.GetChatPrefix(),
                        playerState.Team.FormattedName
                    ]
                );
            Timers.Clear("FriendlyUnpauseInstructions");
            Swiftly.Core.Engine.ExecuteCommand("mp_unpause_match");
            return;
        }
        if (
            player == null
            || (
                player != null
                && Swiftly.Core.Permission.PlayerHasPermissions(player.SteamID, ["@css/config"])
            )
        )
        {
            Swiftly.Log(
                sendToChat: true,
                message: Swiftly.Core.Localizer[
                    "match.admin_unpause",
                    Game.GetChatPrefix(true),
                    player?.Controller.PlayerName ?? "Console"
                ]
            );
            Timers.Clear("FriendlyUnpauseInstructions");
            Swiftly.Core.Engine.ExecuteCommand("mp_unpause_match");
            return;
        }
    }
}
