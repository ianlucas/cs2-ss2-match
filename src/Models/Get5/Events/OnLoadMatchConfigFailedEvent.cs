/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnLoadMatchConfigFailedEvent : Get5Event
{
    public override string EventName => "match_config_load_fail";

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    public static OnLoadMatchConfigFailedEvent Create(string reason) => new() { Reason = reason };
}
