/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnMatchUnpausedEvent : Get5Event
{
    [JsonPropertyName("event")]
    public override string EventName => "game_unpaused";

    [JsonPropertyName("map_number")]
    public int MapNumber { get; init; }

    [JsonPropertyName("team")]
    public string Team { get; init; } = string.Empty;

    [JsonPropertyName("pause_type")]
    public string PauseType { get; init; } = string.Empty;

    public static OnMatchUnpausedEvent Create(PlayerTeam? team, string pauseType) =>
        new()
        {
            MatchId = Game.Id,
            MapNumber = Game.GetMapIndex(),
            Team = Get5EventHelpers.ToTeamString(team),
            PauseType = pauseType,
        };
}
