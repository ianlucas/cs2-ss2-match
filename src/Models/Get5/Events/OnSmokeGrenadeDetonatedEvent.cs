/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnSmokeGrenadeDetonatedEvent : Get5Event
{
    [JsonPropertyName("event")]
    public override string EventName => "smokegrenade_detonated";

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

    [JsonPropertyName("extinguished_molotov")]
    public bool ExtinguishedMolotov { get; init; }

    public static OnSmokeGrenadeDetonatedEvent Create(
        int roundNumber,
        long roundTime,
        PlayerState player,
        string weapon,
        bool didExtinguishMolotovs
    ) =>
        new()
        {
            MatchId = Game.Id,
            MapNumber = Game.GetMapIndex(),
            RoundNumber = roundNumber,
            RoundTime = roundTime,
            Player = Get5EventHelpers.ToPlayer(player),
            Weapon = Get5EventHelpers.ToWeapon(weapon),
            ExtinguishedMolotov = didExtinguishMolotovs,
        };
}
