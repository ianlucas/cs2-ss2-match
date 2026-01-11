/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Match.Get5;

public class Get5Match
{
    [JsonPropertyName("match_title")]
    public string? MatchTitle { get; set; }

    [JsonPropertyName("matchid")]
    public string? Matchid { get; set; }

    [JsonPropertyName("clinch_series")]
    public bool? ClinchSeries { get; set; }

    [JsonPropertyName("num_maps")]
    public int? NumMaps { get; set; }

    [JsonPropertyName("scrim")]
    public bool? Scrim { get; set; }

    [JsonPropertyName("wingman")]
    public bool? Wingman { get; set; }

    [JsonPropertyName("players_per_team")]
    public int? PlayersPerTeam { get; set; }

    [JsonPropertyName("coaches_per_team")]
    public int? CoachesPerTeam { get; set; }

    [JsonPropertyName("coaches_must_ready")]
    public bool? CoachesMustReady { get; set; }

    [JsonPropertyName("min_players_to_ready")]
    public int? MinPlayersToReady { get; set; }

    [JsonPropertyName("min_spectators_to_ready")]
    public int? MinSpectatorsToReady { get; set; }

    [JsonPropertyName("skip_veto")]
    public bool? SkipVeto { get; set; }

    [JsonPropertyName("veto_first")]
    public string? VetoFirst { get; set; }

    [JsonPropertyName("veto_mode")]
    public List<string>? VetoMode { get; set; }

    [JsonPropertyName("side_type")]
    public string? SideType { get; set; }

    [JsonPropertyName("map_sides")]
    public List<string>? MapSides { get; set; }

    [JsonPropertyName("spectators")]
    public Get5SpectatorTeam? Spectators { get; set; }

    [JsonPropertyName("maplist")]
    [JsonConverter(typeof(Get5MaplistJsonConverter))]
    public required Get5Maplist Maplist { get; set; }

    [JsonPropertyName("favored_percentage_team1")]
    public int? FavoredPercentageTeam1 { get; set; }

    [JsonPropertyName("favored_percentage_text")]
    public string? FavoredPercentageText { get; set; }

    [JsonPropertyName("team1")]
    public required Get5MatchTeam Team1 { get; set; }

    [JsonPropertyName("team2")]
    public Get5MatchTeam? Team2 { get; set; }

    [JsonPropertyName("cvars")]
    public Dictionary<string, JsonElement>? Cvars { get; set; }

    public static Get5MatchFile Read(string name)
    {
        try
        {
            if (!name.EndsWith(".json"))
                name += ".json";
            var filepath = Swiftly.Core.GetConfigPath($"/{name}");
            if (!File.Exists(filepath))
                filepath = Swiftly.Core.GetCSGOPath(filepath);
            return new Get5MatchFile
            {
                Path = filepath,
                Contents = JsonSerializer.Deserialize<Get5Match>(File.ReadAllText(filepath)),
            };
        }
        catch (Exception ex)
        {
            Swiftly.Core.Logger.LogWarning($"Error reading match file: {ex.Message}");
            return new Get5MatchFile { Error = ex.Message };
        }
    }
}
