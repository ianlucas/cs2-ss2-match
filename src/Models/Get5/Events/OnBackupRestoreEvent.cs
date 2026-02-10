/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json.Serialization;

namespace Match.Get5.Events;

public sealed class OnBackupRestoreEvent : Get5Event
{
    [JsonPropertyName("event")]
    public override string EventName => "backup_loaded";

    [JsonPropertyName("map_number")]
    public int MapNumber { get; init; }

    [JsonPropertyName("round_number")]
    public int RoundNumber { get; init; }

    [JsonPropertyName("filename")]
    public string Filename { get; init; } = string.Empty;

    public static OnBackupRestoreEvent Create(string filename) =>
        new()
        {
            MatchId = Game.Id,
            MapNumber = Game.GetMapIndex(),
            RoundNumber = Game.GetRoundNumber(),
            Filename = filename,
        };
}
