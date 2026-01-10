/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;

namespace Match;

public class BaseState
{
    public virtual string Name { get; set; } = "default_state";
    protected bool _matchCancelled = false;
    private readonly List<Guid> _commands = [];
    private readonly List<Guid> _gameEvents = [];
    private readonly List<Action> _coreEvents = [];

    public virtual void Load() { }

    public virtual void Unload()
    {
        Timers.ClearAll();
        foreach (var cleanup in _coreEvents)
            cleanup();
        foreach (var guid in _commands)
            Swiftly.Core.Command.UnregisterCommand(guid);
        foreach (var guid in _gameEvents)
            Swiftly.Core.GameEvent.Unhook(guid);
    }

    protected void RegisterCommand(
        List<string> commandNames,
        ICommandService.CommandListener handler
    )
    {
        foreach (var name in commandNames)
            _commands.Add(Swiftly.Core.Command.RegisterCommand(name, handler, registerRaw: true));
    }

    protected void HookGameEvent<T>(
        IGameEventService.GameEventHandler<T> handler,
        HookMode mode = HookMode.Post
    )
        where T : IGameEvent<T>
    {
        _gameEvents.Add(
            mode == HookMode.Pre
                ? Swiftly.Core.GameEvent.HookPre(handler)
                : Swiftly.Core.GameEvent.HookPost(handler)
        );
    }

    protected void HookCoreEvent<THandler>(THandler handler)
        where THandler : Delegate
    {
        var eventName = typeof(THandler).Name;
        var eventSource = Swiftly.Core.Event;
        var eventInfo =
            eventSource.GetType().GetEvent(eventName) ?? throw new ArgumentException(
                $"Event '{eventName}' not found on type '{eventSource.GetType().Name}'"
            );
        eventInfo.AddEventHandler(eventSource, handler);
        _coreEvents.Add(() => eventInfo.RemoveEventHandler(eventSource, handler));
    }

    public HookResult OnRoundPrestart(EventRoundPrestart _)
    {
        if (Swiftly.Core.EntitySystem.GetGameRules()?.GamePhase == 5)
            OnMapEnd(); // Map result should be computed at State::OnCsWinPanelRound.
        return HookResult.Continue;
    }

    public void OnMatchCancelled()
    {
        Game.Log("Match was cancelled.");
        _matchCancelled = true;
        Timers.ClearAll();
        var winners = Game.Teams.Where(t => t.Players.Any(p => p.Handle != null));
        if (winners.Count() == 1)
        {
            var winner = winners.First();
            var loser = winner.Opposition;
            loser.IsSurrended = true;
            Game.Log($"Terminating by Cancelled, winner={winner.Index}, forfeited={loser.Index}");
            Swiftly
                .Core.EntitySystem.GetGameRules()
                ?.TerminateRound(
                    loser.CurrentTeam == Team.T
                        ? RoundEndReason.TerroristsSurrender
                        : RoundEndReason.CTsSurrender,
                    0
                );
        }
        else
        {
            OnMapResult(MapResult.Cancelled);
            OnMapEnd();
        }
    }

    public void OnMapResult(MapResult result = MapResult.None, PlayerTeam? winner = null)
    {
        Game.Log($"Computing map end, result={result}.");
        var map = Game.GetMap() ?? new(Swiftly.Core.Engine.GlobalVars.MapName);
        var stats = Game.Teams.Select(t => t.Players.Select(p => p.Stats).ToList()).ToList();
        var demoFilename = Cstv.GetFilename();
        var scores = Game.Teams.Select(t => t.Score).ToList();
        var team1 = Game.Teams.First();
        var team2 = team1.Opposition;
        map.DemoFilename = demoFilename;
        map.KnifeRoundWinner = Game.KnifeRoundWinner?.Index;
        map.Result = result;
        map.Stats = stats;
        map.Winner = winner;
        map.Scores = scores;
        if (winner != null)
            winner.SeriesScore += 1;
        var mapCount = Game.Maps.Count;
        if (mapCount % 2 == 0)
            mapCount += 1;
        var seriesScoreToWin = (int)Math.Round(mapCount / 2.0, MidpointRounding.AwayFromZero);
        var isSeriesCancelled = result != MapResult.Completed;
        var isSeriesOver =
            isSeriesCancelled
            || (Game.IsClinchSeries && Game.Teams.Any(t => t.SeriesScore >= seriesScoreToWin))
            || Game.GetMap() == null;
        if (isSeriesOver)
        {
            // If match doesn't end normally, we already decided which side won.
            if (isSeriesCancelled)
            {
                team1.SeriesScore = 0;
                team2.SeriesScore = 0;
                if (winner != null)
                    winner.SeriesScore = 1;
            }
            // Team with most series score wins the series for non clinch series.
            if (!Game.IsClinchSeries)
                winner =
                    team1.SeriesScore > team2.SeriesScore ? team1
                    : team2.SeriesScore > team1.SeriesScore ? team2
                    : null;
        }
        Game.MapEndResult = new MapEndResult
        {
            Map = map,
            IsSeriesOver = isSeriesOver,
            Winner = winner,
        };
        if (isSeriesOver)
        {
            var isMatchmaking = ConVars.IsMatchmaking.Value;
            Timers.Set(
                "KickPlayers",
                15.0f,
                () =>
                {
                    if (isMatchmaking)
                    {
                        Game.Log("Match is over, kicking players.");
                        foreach (var player in Swiftly.Core.PlayerManager.GetActualPlayers())
                            player.Kick(
                                "Match is reserved for a lobby.",
                                ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_RESERVED_FOR_LOBBY
                            );
                    }
                }
            );
        }
        if (Cstv.IsRecording())
        {
            var filename = Game.DemoFilename;
            if (filename != null)
                Game.SendEvent(Game.Get5.OnDemoFinished(filename));
        }
        Cstv.Stop();
    }

    public HookResult OnCsWinPanelMatch(EventCsWinPanelMatch _)
    {
        Timers.ClearAll();
        var result = MapResult.None;
        PlayerTeam? winner = null;
        foreach (var team in Game.Teams)
        {
            if (team.IsSurrended)
            {
                result = MapResult.Forfeited;
                winner = team.Opposition;
                Game.Log($"forfeited, result={result}, winner={winner.Index}");
                break;
            }
            if (team.Score > team.Opposition.Score)
            {
                result = MapResult.Completed;
                winner = team;
                Game.Log($"completed, result={result}, winner={winner.Index}");
            }
        }
        OnMapResult(result, winner);
        return HookResult.Continue;
    }

    public void OnMapEnd()
    {
        if (Game.MapEndResult == null)
        {
            Game.Log("Map result not found, defaulting to state none.");
            Game.SetState(new NoneState());
            return;
        }
        var map = Game.MapEndResult.Map;
        var isSeriesOver = Game.MapEndResult.IsSeriesOver;
        var winner = Game.MapEndResult.Winner;
        var maps = (Game.Maps.Count > 0 ? Game.Maps : [map]).Where(m => m.Result != MapResult.None);
        // Even with Get5 Events, we still store results in json for further debugging.
        // @todo Maybe only save if `match_verbose` is enabled in the future.
        IoHelper.WriteJson(Swiftly.Core.GetConfigPath($"{Game.MatchFolder}/results.json"), maps);
        Game.SendEvent(Game.Get5.OnMapResult(map));
        if (isSeriesOver)
        {
            Game.SendEvent(Game.Get5.OnSeriesResult(winner, map));
            Game.Reset();
            Game.EvaluateMatchmakingCondicion();
        }
        Game.SetState(isSeriesOver ? new NoneState() : new ReadyUpWarmupState());
    }
}
