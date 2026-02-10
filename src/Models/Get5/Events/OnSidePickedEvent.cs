/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnSidePickedEvent : Get5Event
{
    [JsonPropertyName("event")]
    public override string EventName => "side_picked";

    [JsonPropertyName("team")]
    public string Team { get; init; } = string.Empty;

    [JsonPropertyName("map_name")]
    public string? MapName { get; init; }

    [JsonPropertyName("side")]
    public string Side { get; init; } = string.Empty;

    [JsonPropertyName("map_number")]
    public int MapNumber { get; init; }

    public static OnSidePickedEvent Create(PlayerTeam team)
    {
        var map = Game.GetMap();
        return new()
        {
            MatchId = Game.Id,
            Team = Get5EventHelpers.ToTeamString(team),
            MapName = map?.MapName,
            Side = Get5EventHelpers.ToSideString(team.StartingTeam),
            MapNumber = Game.FindMapIndex(map),
        };
    }
}
