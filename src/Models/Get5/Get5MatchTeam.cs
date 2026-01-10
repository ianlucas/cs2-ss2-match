/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Match.Get5;

public class Get5MatchTeam
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("players")]
    [JsonConverter(typeof(Get5PlayerSetJsonConverter))]
    public required Get5PlayerSet Players { get; set; }

    [JsonPropertyName("coaches")]
    [JsonConverter(typeof(Get5PlayerSetJsonConverter))]
    public Get5PlayerSet? Coaches { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    [JsonPropertyName("flag")]
    public string? Flag { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    [JsonPropertyName("series_score")]
    public int? SeriesScore { get; set; }

    [JsonPropertyName("matchtext")]
    public string? Matchtext { get; set; }

    [JsonPropertyName("fromfile")]
    public string? Fromfile { get; set; }

    [JsonPropertyName("leaderid")]
    public string? Leaderid { get; set; }

    public Get5MatchTeam? Get()
    {
        try
        {
            if (Fromfile == null)
                return this;
            var name = Fromfile ?? "";
            if (!name.EndsWith(".json"))
                name += ".json";
            var filepath = Swiftly.Core.GetConfigPath($"/{name}");
            if (!File.Exists(filepath))
                filepath = Swiftly.Core.GetCSGOPath(filepath);
            return JsonSerializer.Deserialize<Get5MatchTeam>(File.ReadAllText(filepath));
        }
        catch (Exception ex)
        {
            Swiftly.Core.Logger.LogWarning($"Error reading match team file: {ex.Message}");
            return null;
        }
    }
}
