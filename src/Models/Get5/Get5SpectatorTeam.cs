/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5;

public class Get5SpectatorTeam
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("players")]
    [JsonConverter(typeof(Get5PlayerSetJsonConverter))]
    public Get5PlayerSet? Players { get; set; }

    [JsonPropertyName("fromfile")]
    public string? Fromfile { get; set; }
}
