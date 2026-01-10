/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnFlashbangDetonatedEvent : Get5Event
{
    public override string EventName => "flashbang_detonated";

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

    [JsonPropertyName("victims")]
    public List<object> Victims { get; init; } = [];

    public static OnFlashbangDetonatedEvent Create(
        int roundNumber,
        long roundTime,
        Player player,
        string weapon,
        UtilityVictim victims
    ) =>
        new()
        {
            MatchId = Game.Id,
            MapNumber = Game.GetMapIndex(),
            RoundNumber = roundNumber,
            RoundTime = roundTime,
            Player = Get5EventHelpers.ToPlayer(player),
            Weapon = Get5EventHelpers.ToWeapon(weapon),
            Victims = victims
                .Values.Select(victim => new
                {
                    player = Get5EventHelpers.ToPlayer(victim.Player),
                    friendly_fire = victim.FriendlyFire,
                    blind_duration = victim.BindDuration,
                })
                .Cast<object>()
                .ToList(),
        };
}
