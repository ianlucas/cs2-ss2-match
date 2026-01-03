/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace Match;

using System.Text.Json.Serialization;

public class Map(string mapName)
{
    [JsonPropertyName("mapName")]
    public string MapName { get; set; } = mapName;

    [JsonIgnore]
    public PlayerTeam? Winner { get; set; }

    [JsonPropertyName("winner")]
    public int? WinnerIndex => Winner?.Index;

    [JsonPropertyName("scores")]
    public List<int> Scores { get; set; } = [];

    [JsonPropertyName("result")]
    public MapResult Result { get; set; } = MapResult.None;

    [JsonPropertyName("stats")]
    public List<List<PlayerStats>> Stats { get; set; } = [];

    [JsonPropertyName("demoFilename")]
    public string? DemoFilename { get; set; }

    [JsonPropertyName("knifeRoundWinner")]
    public int? KnifeRoundWinner { get; set; }
}
