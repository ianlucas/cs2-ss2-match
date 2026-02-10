/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnBombPlantedEvent : Get5Event
{
    [JsonPropertyName("event")]
    public override string EventName => "bomb_planted";

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

    public static OnBombPlantedEvent Create(PlayerState player, int? site) =>
        new()
        {
            MatchId = Game.Id,
            MapNumber = Game.GetMapIndex(),
            RoundNumber = Game.GetRoundNumber(),
            RoundTime = Game.GetRoundTime(),
            Player = Get5EventHelpers.ToPlayer(player),
            Site = Get5EventHelpers.ToSite(site),
        };
}
