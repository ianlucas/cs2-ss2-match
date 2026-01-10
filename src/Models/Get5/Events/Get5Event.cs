/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public abstract class Get5Event
{
    [JsonPropertyName("event")]
    public abstract string EventName { get; }

    [JsonPropertyName("matchid")]
    public string? MatchId { get; init; }
}
