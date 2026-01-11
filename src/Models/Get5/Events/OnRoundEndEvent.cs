/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnRoundEndEvent : Get5Event
{
    public override string EventName => "round_end";

    [JsonPropertyName("map_number")]
    public int MapNumber { get; init; }

    [JsonPropertyName("round_number")]
    public int RoundNumber { get; init; }

    [JsonPropertyName("round_time")]
    public long RoundTime { get; init; }

    [JsonPropertyName("reason")]
    public int Reason { get; init; }

    [JsonPropertyName("winner")]
    public object? Winner { get; init; }

    [JsonPropertyName("team1")]
    public object Team1 { get; init; } = null!;

    [JsonPropertyName("team2")]
    public object Team2 { get; init; } = null!;

    public static OnRoundEndEvent Create(PlayerTeam? winner, int reason) =>
        new()
        {
            MatchId = Game.Id,
            MapNumber = Game.GetMapIndex(),
            RoundNumber = Game.GetRoundNumber(),
            RoundTime = Game.GetRoundTime(),
            Reason = reason,
            Winner = Get5EventHelpers.ToWinner(winner),
            Team1 = Get5EventHelpers.ToStatsTeam(Game.Team1),
            Team2 = Get5EventHelpers.ToStatsTeam(Game.Team2),
        };
}
