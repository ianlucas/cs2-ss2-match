/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace Match;

public class WarmupReadyState : StateWarmup
{
    public override string Name => "warmup";
    public static readonly List<string> ReadyCmds = ["css_ready", "css_r", "css_pronto"];
    public static readonly List<string> UnreadyCmds = ["css_unready", "css_ur", "css_naopronto"];
    private long _warmupStart = 0;
    private readonly List<Guid> _commands = [];
    private readonly List<Guid> _gameEvents = [];

    public override void Load()
    {
        base.Load();
        Swiftly.Core.Event.OnTick += OnTick;
        _gameEvents.Add(Swiftly.Core.GameEvent.HookPost<EventPlayerTeam>(OnPlayerTeam));
        _gameEvents.Add(Swiftly.Core.GameEvent.HookPost<EventPlayerDisconnect>(OnPlayerDisconnect));
        _gameEvents.Add(Swiftly.Core.GameEvent.HookPost<EventRoundPrestart>(OnRoundPrestart));
        _gameEvents.Add(Swiftly.Core.GameEvent.HookPost<EventCsWinPanelMatch>(OnCsWinPanelMatch));
        foreach (var cmd in ReadyCmds)
            _commands.Add(Swiftly.Core.Command.RegisterCommand(cmd, OnReadyCommand));
        foreach (var cmd in UnreadyCmds)
            _commands.Add(Swiftly.Core.Command.RegisterCommand(cmd, OnUnreadyCommand));
        Game.ResetTeamsForNewMatch();
        if (ConVars.IsMatchmaking.Value)
        {
            _warmupStart = TimeHelper.Now();
            Timers.SetEverySecond("PrintWaitingPlayersReady", OnPrintMatchmakingReady);
            Timers.Set("MatchmakingReadyTimeout", ConVars.MatchmakingReadyTimeout.Value, () => { });
        }
        Timers.SetEveryChatInterval("PrintWarmupCommands", OnPrintWarmupCommands);
        Game.Log("Executing warm-up commands...");
        Config.ExecWarmup(
            warmupTime: Game.IsMatchmaking() ? ConVars.MatchmakingReadyTimeout.Value : -1,
            isLockTeams: Game.AreTeamsLocked()
        );
        _matchCancelled = false;
    }

    public override void Unload()
    {
        base.Unload();
        Swiftly.Core.Event.OnTick -= OnTick;
        foreach (var guid in _gameEvents)
            Swiftly.Core.GameEvent.Unhook(guid);
        foreach (var guid in ReadyCmds)
            Swiftly.Core.Command.UnregisterCommand(guid);
        Timers.Clear("PrintWaitingPlayersReady");
        Timers.Clear("MatchmakingReadyTimeout");
        Timers.Clear("PrintWarmupCommands");
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

    public void OnPrintWarmupCommands()
    {
        var needed = Game.GetNeededPlayersCount() - Game.GetReadyPlayersCount();
        foreach (var player in Swiftly.Core.PlayerManager.GetPlayersInTeams())
        {
            var localize = Swiftly.Core.Localizer;
            var state = player.GetState();
            player.SendChat(localize["match.commands", Game.GetChatPrefix()]);
            if (needed > 0)
                player.SendChat(localize["match.commands_needed", needed]);
            if (state?.IsReady != true)
                player.SendChat(localize["match.commands_ready"]);
            player.SendChat(localize["match.commands_gg"]);
        }
    }

    public void OnPrintMatchmakingReady()
    {
        var timeleft = Math.Max(
            0,
            ConVars.MatchmakingReadyTimeout.Value - (TimeHelper.NowSeconds() - _warmupStart)
        );
        if (timeleft % 30 != 0)
            return;
        var formattedTimeleft = TimeHelper.Format(timeleft);
        var unreadyTeams = Game.Teams.Where(t => t.Players.Any(p => !p.IsReady));
        if (timeleft == 0)
            Timers.Clear("PrintWaitingPlayersReady");
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
            Game.Log($"{player.Controller.PlayerName} sent !ready.");
            var state = player.GetState();
            if (state == null && !Game.IsLoadedFromFile)
            {
                var team = Game.GetTeam(player.Controller.Team);
                if (team != null && team.CanAddPlayer())
                {
                    state = new(player.SteamID, player.Controller.PlayerName, team, player);
                    team.AddPlayer(state);
                }
            }
            if (state != null)
            {
                state.IsReady = true;
                TryStartMatch();
            }
        }
    }

    public void OnUnreadyCommand(ICommandContext context)
    {
        var player = context.Sender;
        Game.Log($"{player?.Controller.PlayerName} sent !unready.");
        var state = player?.GetState();
        if (state != null)
        {
            state.IsReady = false;
        }
    }

    public void TryStartMatch()
    {
        var players = Game.Teams.SelectMany(t => t.Players);
        if (players.Count() == Game.GetNeededPlayersCount() && players.All(p => p.IsReady))
        {
            // if (!Match.IsLoadedFromFile)
            //     Match.Setup();
            // Match.SetState(
            //     Match.knife_round_enabled.Value ? new StateKnifeRound() : new StateLive()
            // );
        }
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        if (!Game.IsLoadedFromFile)
            @event.UserIdPlayer.GetState()?.LeaveTeam();
        return HookResult.Continue;
    }
}
