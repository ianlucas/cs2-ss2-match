/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnBombDefusedEvent : Get5Event
{
    public override string EventName => "bomb_defused";

    [JsonPropertyName("map_number")]
    public int MapNumber { get; init; }

    [JsonPropertyName("round_number")]
    public int RoundNumber { get; init; }

    [JsonPropertyName("round_time")]
    public long RoundTime { get; init; }

    [JsonPropertyName("player")]
    public object Player { get; init; } = null!;

    [JsonPropertyName("site")]
    public string Site { get; init; } = "none";

    [JsonPropertyName("bomb_time_remaining")]
    public long BombTimeRemaining { get; init; }

    public static OnBombDefusedEvent Create(
        PlayerState player,
        int? site,
        long bombTimeRemaining
    ) =>
        new()
        {
            MatchId = Game.Id,
            MapNumber = Game.GetMapIndex(),
            RoundNumber = Game.GetRoundNumber(),
            RoundTime = Game.GetRoundTime(),
            Player = Get5EventHelpers.ToPlayer(player),
            Site = Get5EventHelpers.ToSite(site),
            BombTimeRemaining = bombTimeRemaining,
        };
}
