/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match;

public class PlayerWeaponStats
{
    [JsonPropertyName("kills")]
    public int Kills { get; set; } = 0;

    [JsonPropertyName("shots")]
    public int Shots { get; set; } = 0;

    [JsonPropertyName("hits")]
    public int Hits { get; set; } = 0;

    [JsonPropertyName("damage")]
    public int Damage { get; set; } = 0;

    [JsonPropertyName("headshots")]
    public int Headshots { get; set; } = 0;

    [JsonPropertyName("head_hits")]
    public int HeadHits { get; set; } = 0;

    [JsonPropertyName("neck_hits")]
    public int NeckHits { get; set; } = 0;

    [JsonPropertyName("chest_hits")]
    public int ChestHits { get; set; } = 0;

    [JsonPropertyName("stomach_hits")]
    public int StomachHits { get; set; } = 0;

    [JsonPropertyName("left_arm_hits")]
    public int LeftArmHits { get; set; } = 0;

    [JsonPropertyName("right_arm_hits")]
    public int RightArmHits { get; set; } = 0;

    [JsonPropertyName("left_leg_hits")]
    public int LeftLegHits { get; set; } = 0;

    [JsonPropertyName("right_leg_hits")]
    public int RightLegHits { get; set; } = 0;

    [JsonPropertyName("gear_hits")]
    public int GearHits { get; set; } = 0;

    public PlayerWeaponStats Clone() => (PlayerWeaponStats)MemberwiseClone();
}
