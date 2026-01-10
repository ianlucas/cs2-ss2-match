/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnPreLoadMatchConfigEvent : Get5Event
{
    public override string EventName => "preload_match_config";

    [JsonPropertyName("filename")]
    public string Filename { get; init; } = string.Empty;

    public static OnPreLoadMatchConfigEvent Create(string filename) =>
        new() { Filename = filename };
}
