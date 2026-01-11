/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnGrenadeThrownEvent : Get5Event
{
    public override string EventName => "grenade_thrown";

    [JsonPropertyName("map_number")]
    public int MapNumber { get; init; }

    [JsonPropertyName("round_number")]
    public int RoundNumber { get; init; }

    [JsonPropertyName("round_time")]
    public long RoundTime { get; init; }

    [JsonPropertyName("player")]
    public object Player { get; init; } = null!;

    [JsonPropertyName("weapon")]
    public object Weapon { get; init; } = null!;

    public static OnGrenadeThrownEvent Create(PlayerState player, string weapon) =>
        new()
        {
            MatchId = Game.Id,
            MapNumber = Game.GetMapIndex(),
            RoundNumber = Game.GetRoundNumber(),
            RoundTime = Game.GetRoundTime(),
            Player = Get5EventHelpers.ToPlayer(player),
            Weapon = Get5EventHelpers.ToWeapon(weapon),
        };
}
