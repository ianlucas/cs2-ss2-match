/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnHEGrenadeDetonatedEvent : Get5Event
{
    [JsonPropertyName("event")]
    public override string EventName => "hegrenade_detonated";

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

    [JsonPropertyName("damage_enemies")]
    public int DamageEnemies { get; init; }

    [JsonPropertyName("damage_friendlies")]
    public int DamageFriendlies { get; init; }

    public static OnHEGrenadeDetonatedEvent Create(
        int roundNumber,
        long roundTime,
        PlayerState player,
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
                    killed = victim.Killed,
                    damage = victim.Damage,
                })
                .Cast<object>()
                .ToList(),
            DamageEnemies = victims
                .Values.Where(v => v.Player.Team != player.Team)
                .Select(v => v.Damage)
                .Sum(),
            DamageFriendlies = victims
                .Values.Where(v => v.Player.Team == player.Team)
                .Select(v => v.Damage)
                .Sum(),
        };
}
