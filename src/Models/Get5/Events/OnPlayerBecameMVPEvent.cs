/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnPlayerBecameMVPEvent : Get5Event
{
    [JsonPropertyName("event")]
    public override string EventName => "round_mvp";

    [JsonPropertyName("map_number")]
    public int MapNumber { get; init; }

    [JsonPropertyName("round_number")]
    public int RoundNumber { get; init; }

    [JsonPropertyName("player")]
    public object Player { get; init; } = null!;

    [JsonPropertyName("reason")]
    public int Reason { get; init; }

    public static OnPlayerBecameMVPEvent Create(PlayerState player, int reason) =>
        new()
        {
            MatchId = Game.Id,
            MapNumber = Game.GetMapIndex(),
            RoundNumber = Game.GetRoundNumber(),
            Player = Get5EventHelpers.ToPlayer(player),
            Reason = reason,
        };
}
