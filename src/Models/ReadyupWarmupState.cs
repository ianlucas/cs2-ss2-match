/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Match.Get5.Events;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace Match;

public class ReadyupWarmupState : WarmupState
{
    public override string Name => "warmup";
    public static readonly List<string> ReadyCmds = ["sw_ready", "sw_r", "sw_pronto"];
    public static readonly List<string> UnreadyCmds = ["sw_unready", "sw_ur", "sw_naopronto"];
    private long _warmupStart = 0;

    public override void Load()
    {
        Swiftly.Log($"Matchmaking mode: {Game.IsMatchmaking()}");
        Cstv.Stop();
        Cstv.Set(ConVars.IsTvRecord.Value);
        if (Game.EnsureCorrectMap())
            return /* Map will be changed. */
            ;
        base.Load();
        HookCoreEvent<EventDelegates.OnTick>(OnTick);
        HookGameEvent<EventPlayerTeam>(OnPlayerTeam);
        HookGameEvent<EventPlayerDisconnect>(OnPlayerDisconnect);
        HookGameEvent<EventRoundPrestart>(OnRoundPrestart);
        HookGameEvent<EventCsWinPanelMatch>(OnCsWinPanelMatch);
        RegisterCommand(ReadyCmds, OnReadyCommand);
        RegisterCommand(UnreadyCmds, OnUnreadyCommand);
        Game.ResetTeamsForNewMatch();
        if (ConVars.IsMatchmaking.Value)
        {
            _warmupStart = TimeHelper.Now();
            Timers.SetEverySecond("ReadyStatusReminder", SendReadyStatusReminder);
            Timers.Set(
                "MatchmakingReadyTimeout",
                ConVars.MatchmakingReadyTimeout.Value,
                OnMatchCancelled
            );
        }
        Timers.SetEveryChatInterval("WarmupInstructions", SendWarmupInstructions);
        Swiftly.Log("Executing warm-up commands...");
        Config.ExecWarmup(
            warmupTime: Game.IsMatchmaking() ? ConVars.MatchmakingReadyTimeout.Value : -1,
            isLockTeams: Game.AreTeamsLocked()
        );
        _matchCancelled = false;
    }

    public void OnTick()
    {
        bool didUpdatePlayers = false;
        foreach (var player in Swiftly.Core.PlayerManager.GetPlayersInTeams())
            if (
                !player.IsFakeClient
                && player.SetPlayerClan(
                    Swiftly.Core.Localizer[
                        player.GetState()?.IsReady == true ? "match.ready" : "match.not_ready"
                    ]
                )
            )
                didUpdatePlayers = true;
        if (didUpdatePlayers)
            Swiftly.Core.PlayerManager.UpdateScoreboards();
    }

    public void SendWarmupInstructions()
    {
        var needed = Game.GetNeededPlayersCount() - Game.GetReadyPlayersCount();
        foreach (var player in Swiftly.Core.PlayerManager.GetPlayersInTeams())
        {
            var localize = Swiftly.Core.Localizer;
            var playerState = player.GetState();
            player.SendChat(localize["match.commands", Game.GetChatPrefix()]);
            if (needed > 0)
                player.SendChat(localize["match.commands_needed", needed]);
            if (playerState?.IsReady != true)
                player.SendChat(localize["match.commands_ready"]);
            player.SendChat(localize["match.commands_gg"]);
        }
    }

    public void SendReadyStatusReminder()
    {
        var timeleft = Math.Max(
            0,
            ConVars.MatchmakingReadyTimeout.Value - (TimeHelper.NowSeconds() - _warmupStart)
        );
        if (timeleft % 30 != 0)
            return;
        var formattedTimeleft = TimeHelper.FormatMmSs(timeleft);
        var unreadyTeams = Game.GetUnreadyTeams();
        if (timeleft == 0)
            Timers.Clear("ReadyStatusReminder");
        else
            switch (unreadyTeams.Count())
            {
                case 1:
                    var team = unreadyTeams.First();
                    Swiftly.Core.PlayerManager.SendChat(
                        Swiftly.Core.Localizer[
                            "match.match_waiting_team",
                            Game.GetChatPrefix(stripColors: true),
                            team.FormattedName,
                            formattedTimeleft
                        ]
                    );
                    break;

                case 2:
                    Swiftly.Core.PlayerManager.SendChat(
                        Swiftly.Core.Localizer[
                            "match.match_waiting_players",
                            Game.GetChatPrefix(stripColors: true),
                            formattedTimeleft
                        ]
                    );
                    break;
            }
    }

    public HookResult OnPlayerTeam(EventPlayerTeam @event)
    {
        if (!Game.IsLoadedFromFile)
            @event.UserIdPlayer.GetState()?.LeaveTeam();
        return HookResult.Continue;
    }

    public void OnReadyCommand(ICommandContext context)
    {
        var player = context.Sender;
        if (player != null && !_matchCancelled)
        {
            Swiftly.Log($"{player.Controller.PlayerName} sent !ready.");
            var playerState = player.GetState();
            if (playerState == null && !Game.IsLoadedFromFile)
            {
                var team = Game.GetTeam(player.Controller.Team);
                if (team != null && team.CanAddPlayer())
                {
                    playerState = new(player.SteamID, player.Controller.PlayerName, team, player);
                    team.AddPlayer(playerState);
                }
            }
            if (playerState != null)
            {
                playerState.IsReady = true;
                Game.SendEvent(OnTeamReadyStatusChangedEvent.Create(team: playerState.Team));
                TryStartMatch();
            }
        }
    }

    public void OnUnreadyCommand(ICommandContext context)
    {
        var player = context.Sender;
        Swiftly.Log($"{player?.Controller.PlayerName} sent !unready.");
        var playerState = player?.GetState();
        if (playerState != null)
        {
            playerState.IsReady = false;
            Game.SendEvent(OnTeamReadyStatusChangedEvent.Create(team: playerState.Team));
        }
    }

    public void TryStartMatch()
    {
        var players = Game.GetAllPlayers();
        if (players.Count() == Game.GetNeededPlayersCount() && Game.AreAllPlayersReady())
        {
            if (!Game.IsLoadedFromFile)
                Game.Setup();
            Game.SetState(
                ConVars.IsKnifeRoundEnabled.Value ? new KnifeRoundState() : new LiveState()
            );
        }
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        if (!Game.IsLoadedFromFile)
            @event.UserIdPlayer.GetState()?.LeaveTeam();
        return HookResult.Continue;
    }
}
