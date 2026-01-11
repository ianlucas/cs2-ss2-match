/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnKnifeRoundWonEvent : Get5Event
{
    public override string EventName => "knife_won";

    [JsonPropertyName("map_number")]
    public int MapNumber { get; init; }

    [JsonPropertyName("team")]
    public string Team { get; init; } = string.Empty;

    [JsonPropertyName("side")]
    public string Side { get; init; } = string.Empty;

    [JsonPropertyName("swapped")]
    public bool Swapped { get; init; }

    public static OnKnifeRoundWonEvent Create(PlayerTeam team, KnifeRoundVote decision) =>
        new()
        {
            MatchId = Game.Id,
            MapNumber = Game.GetMapIndex(),
            Team = Get5EventHelpers.ToTeamString(team),
            Side = Get5EventHelpers.ToSideString(team.StartingTeam),
            Swapped = decision == KnifeRoundVote.Switch,
        };
}
