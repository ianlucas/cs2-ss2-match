/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnPlayerDeathEvent : Get5Event
{
    [JsonPropertyName("event")]
    public override string EventName => "player_death";

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

    [JsonPropertyName("bomb")]
    public bool Bomb { get; init; }

    [JsonPropertyName("headshot")]
    public bool Headshot { get; init; }

    [JsonPropertyName("thru_smoke")]
    public bool ThruSmoke { get; init; }

    [JsonPropertyName("penetrated")]
    public int Penetrated { get; init; }

    [JsonPropertyName("attacker_blind")]
    public bool AttackerBlind { get; init; }

    [JsonPropertyName("no_scope")]
    public bool NoScope { get; init; }

    [JsonPropertyName("suicide")]
    public bool Suicide { get; init; }

    [JsonPropertyName("friendly_fire")]
    public bool FriendlyFire { get; init; }

    [JsonPropertyName("attacker")]
    public object? Attacker { get; init; }

    [JsonPropertyName("assist")]
    public object? Assist { get; init; }

    public static OnPlayerDeathEvent Create(
        PlayerState player,
        PlayerState? attacker,
        PlayerState? assister,
        string weapon,
        bool isKilledByBomb,
        bool isHeadshot,
        bool isThruSmoke,
        int isPenetrated,
        bool isAttackerBlind,
        bool isNoScope,
        bool isSuicide,
        bool isFriendlyFire,
        bool isFlashAssist
    ) =>
        new()
        {
            MatchId = Game.Id,
            MapNumber = Game.GetMapIndex(),
            RoundNumber = Game.GetRoundNumber(),
            RoundTime = Game.GetRoundTime(),
            Player = Get5EventHelpers.ToPlayer(player),
            Weapon = Get5EventHelpers.ToWeapon(weapon),
            Bomb = isKilledByBomb,
            Headshot = isHeadshot,
            ThruSmoke = isThruSmoke,
            Penetrated = isPenetrated,
            AttackerBlind = isAttackerBlind,
            NoScope = isNoScope,
            Suicide = isSuicide,
            FriendlyFire = isFriendlyFire,
            Attacker = attacker != null ? Get5EventHelpers.ToPlayer(attacker) : null,
            Assist =
                assister != null
                    ? new
                    {
                        player = Get5EventHelpers.ToPlayer(assister),
                        friendly_fire = player.Team == assister.Team,
                        flash_assist = isFlashAssist,
                    }
                    : null,
        };
}
