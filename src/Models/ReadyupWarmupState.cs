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
        Runtime.Log($"Matchmaking mode: {Rules.IsMatchmaking()}");
        Cstv.Stop();
        Cstv.Set(ConVars.IsTvRecord.Value);
        if (Rules.EnsureCorrectMap())
            return /* Map will be changed. */
            ;
        base.Load();
        HookCoreEvent<EventDelegates.OnTick>(OnTick);
        HookGameEvent<EventPlayerSpawn>(OnPlayerSpawn);
        HookGameEvent<EventPlayerTeam>(OnPlayerTeam);
        HookGameEvent<EventPlayerDisconnect>(OnPlayerDisconnect);
        HookGameEvent<EventRoundPrestart>(OnRoundPrestart);
        HookGameEvent<EventCsWinPanelMatch>(OnCsWinPanelMatch);
        RegisterCommand(ReadyCmds, OnReadyCommand);
        RegisterCommand(UnreadyCmds, OnUnreadyCommand);
        Rules.ResetTeamsForNewMatch();
        if (this is not NoneState && ConVars.IsMatchmaking.Value)
        {
            var nextMap = Rules.GetNextMap();
            Runtime.Core.ConVar.Find<string>("nextlevel")?.Value = nextMap?.MapName ?? "";
            _warmupStart = TimeHelper.NowSeconds();
            Timers.SetEverySecond("ReadyStatusReminder", SendReadyStatusReminder);
            Timers.Set(
                "MatchmakingReadyTimeout",
                ConVars.MatchmakingReadyTimeout.Value,
                OnMatchCancelled
            );
        }
        Timers.SetEveryChatInterval("WarmupInstructions", SendWarmupInstructions);
        Runtime.Log("Executing warm-up commands...");
        Config.ExecWarmup(
            warmupTime: Rules.IsMatchmaking() ? ConVars.MatchmakingReadyTimeout.Value : -1,
            isLockTeams: Rules.AreTeamsLocked()
        );
        _matchCancelled = false;
        if (this is not NoneState && ConVars.IsMatchmaking.Value && Rules.IsFirstMap())
            Runtime.Log("The series has begun.", force: true);
    }

    public void OnTick()
    {
        bool didUpdatePlayers = false;
        foreach (var player in Runtime.Core.PlayerManager.GetPlayersInTeams())
            if (
                !player.IsFakeClient
                && player.SetPlayerClan(
                    Runtime.Core.Localizer[
                        player.GetState()?.IsReady == true ? "match.ready" : "match.not_ready"
                    ]
                )
            )
                didUpdatePlayers = true;
        if (didUpdatePlayers)
            Runtime.Core.PlayerManager.UpdateScoreboard();
    }

    public void SendWarmupInstructions()
    {
        var needed = Rules.GetNeededPlayersCount() - Rules.GetReadyPlayersCount();
        foreach (var player in Runtime.Core.PlayerManager.GetPlayersInTeams())
        {
            var localize = Runtime.Core.Localizer;
            var playerState = player.GetState();
            player.SendChat(localize["match.commands", Rules.GetChatPrefix()]);
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
        var unreadyTeams = Rules.GetUnreadyTeams();
        if (timeleft == 0)
            Timers.Clear("ReadyStatusReminder");
        else
            switch (unreadyTeams.Count())
            {
                case 1:
                    var team = unreadyTeams.First();
                    Runtime.Core.PlayerManager.SendChat(
                        Runtime.Core.Localizer[
                            "match.match_waiting_team",
                            Rules.GetChatPrefix(stripColors: true),
                            team.FormattedName,
                            formattedTimeleft
                        ]
                    );
                    break;

                case 2:
                    Runtime.Core.PlayerManager.SendChat(
                        Runtime.Core.Localizer[
                            "match.match_waiting_players",
                            Rules.GetChatPrefix(stripColors: true),
                            formattedTimeleft
                        ]
                    );
                    break;
            }
    }

    public HookResult OnPlayerTeam(EventPlayerTeam @event)
    {
        var player = @event.UserIdPlayer;
        if (!Rules.IsLoadedFromFile && player != null)
            player.GetState()?.LeaveTeam();
        return HookResult.Continue;
    }

    public HookResult OnPlayerSpawn(EventPlayerSpawn @event)
    {
        var player = @event.UserIdPlayer;
        // This fixes warmup time not matching the actual warmup time when the
        // first player connects.
        if (
            player != null
            && !player.IsFakeClient
            && ConVars.IsMatchmaking.Value
            && Runtime.Core.PlayerManager.GetActualPlayers().Count() == 1
        )
            Runtime.Core.Engine.ExecuteCommand(
                $"mp_warmuptime {Math.Max(
                1,
                ConVars.MatchmakingReadyTimeout.Value - (TimeHelper.NowSeconds() - _warmupStart)
            )}"
            );
        return HookResult.Continue;
    }

    public void OnReadyCommand(ICommandContext context)
    {
        var player = context.Sender;
        if (player != null && !_matchCancelled)
        {
            Runtime.Log($"{player.Controller.PlayerName} sent !ready.");
            var playerState = player.GetState();
            if (playerState == null && !Rules.IsLoadedFromFile)
            {
                var team = Rules.GetTeam(player.Controller.Team);
                if (team != null && team.CanAddPlayer())
                {
                    playerState = new(player.SteamID, player.Controller.PlayerName, team, player);
                    team.AddPlayer(playerState);
                }
            }
            if (playerState != null)
            {
                playerState.IsReady = true;
                Rules.SendEvent(OnTeamReadyStatusChangedEvent.Create(team: playerState.Team));
                TryStartMatch();
            }
        }
    }

    public void OnUnreadyCommand(ICommandContext context)
    {
        var player = context.Sender;
        Runtime.Log($"{player?.Controller.PlayerName} sent !unready.");
        var playerState = player?.GetState();
        if (playerState != null)
        {
            playerState.IsReady = false;
            Rules.SendEvent(OnTeamReadyStatusChangedEvent.Create(team: playerState.Team));
        }
    }

    public void TryStartMatch()
    {
        var players = Rules.GetAllPlayers();
        if (players.Count() == Rules.GetNeededPlayersCount() && Rules.AreAllPlayersReady())
        {
            if (!Rules.IsLoadedFromFile)
                Rules.Setup();
            Rules.SetState(
                ConVars.IsKnifeRoundEnabled.Value ? new KnifeRoundState() : new LiveState()
            );
        }
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var player = @event.UserIdPlayer;
        if (!Rules.IsLoadedFromFile && player != null)
            player.GetState()?.LeaveTeam();
        return HookResult.Continue;
    }
}
