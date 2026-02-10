/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnTeamReadyStatusChangedEvent : Get5Event
{
    [JsonPropertyName("event")]
    public override string EventName => "team_ready_status_changed";

    [JsonPropertyName("team")]
    public string Team { get; init; } = string.Empty;

    [JsonPropertyName("ready")]
    public bool Ready { get; init; }

    [JsonPropertyName("game_state")]
    public string GameState { get; init; } = string.Empty;

    public static OnTeamReadyStatusChangedEvent Create(PlayerTeam team) =>
        new()
        {
            MatchId = Game.Id,
            Team = Get5EventHelpers.ToTeamString(team),
            Ready = team.Players.All(p => p.IsReady),
            GameState = Game.State.Name,
        };
}
