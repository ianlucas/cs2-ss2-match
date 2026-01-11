/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnGoingLiveEvent : Get5Event
{
    public override string EventName => "going_live";

    [JsonPropertyName("map_number")]
    public int MapNumber { get; init; }

    public static OnGoingLiveEvent Create() =>
        new() { MatchId = Game.Id, MapNumber = Game.GetMapIndex() };
}
