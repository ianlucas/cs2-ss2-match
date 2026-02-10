/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;
using SwiftlyS2.Shared.Players;

namespace Match.Get5.Events;

public sealed class OnPlayerConnectedEvent : Get5Event
{
    [JsonPropertyName("event")]
    public override string EventName => "player_connect";

    [JsonPropertyName("player")]
    public object Player { get; init; } = null!;

    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; init; }

    public static OnPlayerConnectedEvent Create(IPlayer player) =>
        new()
        {
            MatchId = Game.Id,
            Player = Get5EventHelpers.ToPlayer(player),
            IpAddress = player.IPAddress,
        };
}
