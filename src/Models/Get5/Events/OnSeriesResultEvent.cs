/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnSeriesResultEvent : Get5Event
{
    public override string EventName => "series_end";

    [JsonPropertyName("team1_series_score")]
    public int Team1SeriesScore { get; init; }

    [JsonPropertyName("team2_series_score")]
    public int Team2SeriesScore { get; init; }

    [JsonPropertyName("winner")]
    public object? Winner { get; init; }

    [JsonPropertyName("time_until_restore")]
    public int TimeUntilRestore { get; init; }

    [JsonPropertyName("last_map_number")]
    public int LastMapNumber { get; init; }

    public static OnSeriesResultEvent Create(PlayerTeam? winner, Map map) =>
        new()
        {
            MatchId = Game.Id,
            Team1SeriesScore = Game.Team1.SeriesScore,
            Team2SeriesScore = Game.Team2.SeriesScore,
            Winner = Get5EventHelpers.ToWinner(winner),
            TimeUntilRestore = 0,
            LastMapNumber = Game.FindMapIndex(map),
        };
}
