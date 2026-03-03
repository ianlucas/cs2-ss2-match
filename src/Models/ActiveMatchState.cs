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
        var winners = MatchCtx.GetTeamsWithConnectedPlayers();
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
        var map = MatchCtx.GetMap() ?? new(Swiftly.Core.Engine.GlobalVars.MapName);
        var stats = MatchCtx.Teams.Select(t => t.Players.Select(p => p.Stats).ToList()).ToList();
        var demoFilename = Cstv.GetFilename();
        var scores = MatchCtx.Teams.Select(t => t.Score).ToList();
        var team1 = MatchCtx.Teams.First();
        var team2 = team1.Opposition;
        map.DemoFilename = demoFilename;
        map.KnifeRoundWinner = MatchCtx.KnifeRoundWinner?.Index;
        map.Result = result;
        map.Stats = stats;
        map.Winner = winner;
        map.Scores = scores;
        if (winner != null)
            winner.SeriesScore += 1;
        var mapCount = MatchCtx.GetTotalMapCount();
        if (mapCount % 2 == 0)
            mapCount += 1;
        var seriesScoreToWin = (int)Math.Round(mapCount / 2.0, MidpointRounding.AwayFromZero);
        var isSeriesCancelled = result != MapResult.Completed;
        var isSeriesOver =
            isSeriesCancelled
            || (
                MatchCtx.IsClinchSeries
                && MatchCtx.Teams.Any(t => t.SeriesScore >= seriesScoreToWin)
            )
            || MatchCtx.GetMap() == null;
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
            if (!MatchCtx.IsClinchSeries)
                winner =
                    team1.SeriesScore > team2.SeriesScore ? team1
                    : team2.SeriesScore > team1.SeriesScore ? team2
                    : null;
        }
        MatchCtx.MapEndResult = new MapEndResult
        {
            Map = map,
            IsSeriesOver = isSeriesOver,
            Winner = winner,
        };
        if (Cstv.IsRecording())
        {
            var filename = MatchCtx.GetDemoFilename();
            if (filename != null)
                MatchCtx.SendEvent(OnDemoFinishedEvent.Create(filename));
        }
        Cstv.Stop();
    }

    public HookResult OnCsWinPanelMatch(EventCsWinPanelMatch _)
    {
        Timers.ClearAll();
        var result = MapResult.None;
        PlayerTeam? winner = null;
        foreach (var team in MatchCtx.Teams)
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
        if (MatchCtx.MapEndResult == null)
        {
            Swiftly.Log("Map result not found, defaulting to None state.");
            MatchCtx.SetState(new NoneState());
            return;
        }
        var map = MatchCtx.MapEndResult.Map;
        var isSeriesOver = MatchCtx.MapEndResult.IsSeriesOver;
        var winner = MatchCtx.MapEndResult.Winner;
        var maps = (MatchCtx.GetTotalMapCount() > 0 ? MatchCtx.Maps : [map]).Where(m =>
            m.Result != MapResult.None
        );
        // Even with Get5 Events, we still store results in json for further debugging.
        // @todo Maybe only save if `match_verbose` is enabled in the future.
        IoHelper.WriteJson(
            Swiftly.Core.GetConfigPath($"{MatchCtx.GetMatchFolder()}/results.json"),
            maps
        );
        MatchCtx.SendEvent(OnMapResultEvent.Create(map));
        if (isSeriesOver)
        {
            MatchCtx.SendEvent(OnSeriesResultEvent.Create(winner, map));
            MatchCtx.Reset();
            MatchCtx.EnforceMatchmakingRestrictions();
        }
        else
            MatchCtx.MapEndResult = null;
        MatchCtx.SetState(isSeriesOver ? new NoneState() : new ReadyupWarmupState());
    }
}
