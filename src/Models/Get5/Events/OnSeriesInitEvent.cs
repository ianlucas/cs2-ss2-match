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
            MatchId = MatchCtx.Id,
            NumMaps = MatchCtx.Maps.Count,
            Team1 = new { id = MatchCtx.Team1.Id, name = MatchCtx.Team1.Name },
            Team2 = new { id = MatchCtx.Team2.Id, name = MatchCtx.Team2.Name },
        };
}
