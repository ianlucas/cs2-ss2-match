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
        var gameRules = Runtime.Core.EntitySystem.GetGameRules();
        if (gameRules?.GamePhaseEnum == GamePhase.GAMEPHASE_MATCH_ENDED)
        {
            OnMapEnd(); // Map result should be computed at State::OnCsWinPanelRound.
            gameRules.GamePhaseEnum = GamePhase.GAMEPHASE_WARMUP_ROUND;
        }
        return HookResult.Continue;
    }

    public void OnMatchCancelled()
    {
        Runtime.Log("Match cancelled.");
        _matchCancelled = true;
        Timers.ClearAll();
        var winners = Rules.GetTeamsWithConnectedPlayers();
        if (winners.Count() == 1)
        {
            var winner = winners.First();
            var loser = winner.Opposition;
            loser.IsSurrended = true;
            Runtime.Log(
                $"Terminating match due to cancellation: winner={winner.Index}, forfeited={loser.Index}"
            );
            Runtime
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
        Runtime.Log($"Computing map result: {result}");
        var map = Rules.GetMap() ?? new(Runtime.Core.Engine.GlobalVars.MapName);
        var stats = Rules.Teams.Select(t => t.Players.Select(p => p.Stats).ToList()).ToList();
        var demoFilename = Cstv.GetFilename();
        var scores = Rules.Teams.Select(t => t.Score).ToList();
        var team1 = Rules.Teams.First();
        var team2 = team1.Opposition;
        map.DemoFilename = demoFilename;
        map.KnifeRoundWinner = Rules.KnifeRoundWinner?.Index;
        map.Result = result;
        map.Stats = stats;
        map.Winner = winner;
        map.Scores = scores;
        if (winner != null)
            winner.SeriesScore += 1;
        var mapCount = Rules.GetTotalMapCount();
        if (mapCount % 2 == 0)
            mapCount += 1;
        var seriesScoreToWin = (int)Math.Round(mapCount / 2.0, MidpointRounding.AwayFromZero);
        var isSeriesCancelled = result != MapResult.Completed;
        var isSeriesOver =
            isSeriesCancelled
            || (Rules.IsClinchSeries && Rules.Teams.Any(t => t.SeriesScore >= seriesScoreToWin))
            || Rules.GetMap() == null;
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
            if (!Rules.IsClinchSeries)
                winner =
                    team1.SeriesScore > team2.SeriesScore ? team1
                    : team2.SeriesScore > team1.SeriesScore ? team2
                    : null;
        }
        Rules.MapEndResult = new MapEndResult
        {
            Map = map,
            IsSeriesOver = isSeriesOver,
            Winner = winner,
        };
        if (Cstv.IsRecording())
        {
            var filename = Rules.GetDemoFilename();
            if (filename != null)
                Rules.SendEvent(OnDemoFinishedEvent.Create(filename));
        }
        Cstv.Stop();
    }

    public HookResult OnCsWinPanelMatch(EventCsWinPanelMatch _)
    {
        Timers.ClearAll();
        var result = MapResult.None;
        PlayerTeam? winner = null;
        foreach (var team in Rules.Teams)
        {
            if (team.IsSurrended)
            {
                result = MapResult.Forfeited;
                winner = team.Opposition;
                Runtime.Log($"Map forfeited: result={result}, winner={winner.Index}");
                break;
            }
            if (team.Score > team.Opposition.Score)
            {
                result = MapResult.Completed;
                winner = team;
                Runtime.Log($"Map completed: result={result}, winner={winner.Index}");
            }
        }
        OnMapResult(result, winner);
        return HookResult.Continue;
    }

    public void OnMapEnd()
    {
        if (Rules.MapEndResult == null)
        {
            Runtime.Log("Map result not found, defaulting to None state.");
            Rules.SetState(new NoneState());
            return;
        }
        var map = Rules.MapEndResult.Map;
        var isSeriesOver = Rules.MapEndResult.IsSeriesOver;
        var winner = Rules.MapEndResult.Winner;
        var maps = (Rules.GetTotalMapCount() > 0 ? Rules.Maps : [map]).Where(m =>
            m.Result != MapResult.None
        );
        if (ConVars.IsResultStore.Value)
            IoHelper.WriteJson(Rules.GetMatchPath("results.json"), maps);
        Rules.SendEvent(OnMapResultEvent.Create(map));
        if (isSeriesOver)
        {
            Rules.SendEvent(OnSeriesResultEvent.Create(winner, map));
            Rules.FinalizeEventLog();
            Rules.Reset();
            Rules.EnforceMatchmakingRestrictions();
        }
        else
            Rules.MapEndResult = null;
        Rules.SetState(isSeriesOver ? new NoneState() : new ReadyupWarmupState());
    }
}
