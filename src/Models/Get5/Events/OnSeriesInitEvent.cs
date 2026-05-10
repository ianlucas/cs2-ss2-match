/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnSeriesInitEvent : Get5Event
{
    [JsonPropertyName("event")]
    public override string EventName => "series_start";

    [JsonPropertyName("num_maps")]
    public int NumMaps { get; init; }

    [JsonPropertyName("team1")]
    public object Team1 { get; init; } = null!;

    [JsonPropertyName("team2")]
    public object Team2 { get; init; } = null!;

    public static OnSeriesInitEvent Create() =>
        new()
        {
            MatchId = Rules.Id,
            NumMaps = Rules.Maps.Count,
            Team1 = new { id = Rules.Team1.Id, name = Rules.Team1.Name },
            Team2 = new { id = Rules.Team2.Id, name = Rules.Team2.Name },
        };
}
