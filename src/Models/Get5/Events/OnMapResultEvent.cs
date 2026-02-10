/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnMapResultEvent : Get5Event
{
    [JsonPropertyName("event")]
    public override string EventName => "map_result";

    [JsonPropertyName("map_number")]
    public int MapNumber { get; init; }

    [JsonPropertyName("team1")]
    public object Team1 { get; init; } = null!;

    [JsonPropertyName("team2")]
    public object Team2 { get; init; } = null!;

    [JsonPropertyName("winner")]
    public object? Winner { get; init; }

    [JsonPropertyName("result")]
    public MapResult Result { get; init; }

    public static OnMapResultEvent Create(Map map) =>
        new()
        {
            MatchId = Game.Id,
            MapNumber = Game.FindMapIndex(map),
            Team1 = Get5EventHelpers.ToStatsTeam(Game.Team1),
            Team2 = Get5EventHelpers.ToStatsTeam(Game.Team2),
            Winner = Get5EventHelpers.ToWinner(map.Winner),
            Result = map.Result,
        };
}
