/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Match.Get5.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace Match;

public class ActiveMatchState : BaseState
{
    public HookResult OnRoundPrestart(EventRoundPrestart _)
    {
        var gameRules = Swiftly.Core.EntitySystem.GetGameRules();
        if (gameRules?.GamePhaseEnum == GamePhase.GAMEPHASE_MATCH_ENDED)
        {
            OnMapEnd(); // Map result should be computed at State::OnCsWinPanelRound.
            gameRules.GamePhaseEnum = GamePhase.GAMEPHASE_WARMUP_ROUND;
        }
        return HookResult.Continue;
    }

    public void OnMatchCancelled()
    {
        Swiftly.Log("Match cancelled.");
        _matchCancelled = true;
        Timers.ClearAll();
        var winners = Game.GetTeamsWithConnectedPlayers();
        if (winners.Count() == 1)
        {
            var winner = winners.First();
            var loser = winner.Opposition;
            loser.IsSurrended = true;
            Swiftly.Log(
                $"Terminating match due to cancellation: winner={winner.Index}, forfeited={loser.Index}"
            );
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
        Swiftly.Log($"Computing map result: {result}");
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
        var mapCount = Game.GetTotalMapCount();
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
        if (Cstv.IsRecording())
        {
            var filename = Game.GetDemoFilename();
            if (filename != null)
                Game.SendEvent(OnDemoFinishedEvent.Create(filename));
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
                Swiftly.Log($"Map forfeited: result={result}, winner={winner.Index}");
                break;
            }
            if (team.Score > team.Opposition.Score)
            {
                result = MapResult.Completed;
                winner = team;
                Swiftly.Log($"Map completed: result={result}, winner={winner.Index}");
            }
        }
        OnMapResult(result, winner);
        return HookResult.Continue;
    }

    public void OnMapEnd()
    {
        if (Game.MapEndResult == null)
        {
            Swiftly.Log("Map result not found, defaulting to None state.");
            Game.SetState(new NoneState());
            return;
        }
        var map = Game.MapEndResult.Map;
        var isSeriesOver = Game.MapEndResult.IsSeriesOver;
        var winner = Game.MapEndResult.Winner;
        var maps = (Game.GetTotalMapCount() > 0 ? Game.Maps : [map]).Where(m =>
            m.Result != MapResult.None
        );
        // Even with Get5 Events, we still store results in json for further debugging.
        // @todo Maybe only save if `match_verbose` is enabled in the future.
        IoHelper.WriteJson(
            Swiftly.Core.GetConfigPath($"{Game.GetMatchFolder()}/results.json"),
            maps
        );
        Game.SendEvent(OnMapResultEvent.Create(map));
        if (isSeriesOver)
        {
            Game.SendEvent(OnSeriesResultEvent.Create(winner, map));
            Game.Reset();
            Game.EnforceMatchmakingRestrictions();
        }
        else
            Game.MapEndResult = null;
        Game.SetState(isSeriesOver ? new NoneState() : new ReadyupWarmupState());
    }
}
