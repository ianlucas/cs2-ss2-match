/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;
using SwiftlyS2.Shared.Players;

namespace Match.Get5.Events;

public sealed class OnPlayerSayEvent : Get5Event
{
    [JsonPropertyName("event")]
    public override string EventName => "player_say";

    [JsonPropertyName("map_number")]
    public int MapNumber { get; init; }

    [JsonPropertyName("round_number")]
    public int RoundNumber { get; init; }

    [JsonPropertyName("round_time")]
    public long RoundTime { get; init; }

    [JsonPropertyName("player")]
    public object Player { get; init; } = null!;

    [JsonPropertyName("command")]
    public string Command { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    public static OnPlayerSayEvent Create(IPlayer player, string command, string message) =>
        new()
        {
            MatchId = Game.Id,
            MapNumber = Game.GetMapIndex(),
            RoundNumber = Game.GetRoundNumber(),
            RoundTime = Game.GetRoundTime(),
            Player = Get5EventHelpers.ToPlayer(player),
            Command = command,
            Message = message,
        };
}
