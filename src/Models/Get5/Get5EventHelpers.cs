/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Players;

namespace Match.Get5;

public static class Get5EventHelpers
{
    public static string ToSideString(Team team) => team == Team.T ? "t" : "ct";

    public static string ToTeamString(PlayerTeam? team) =>
        team != null ? $"team{team.Index + 1}" : "spec";

    public static object ToStatsTeam(PlayerTeam team) =>
        new
        {
            id = team.Id,
            name = team.Name,
            series_score = team.SeriesScore,
            score = team.Score,
            score_ct = team.Stats.ScoreCT,
            score_t = team.Stats.ScoreT,
            side = ToSideString(team.CurrentTeam),
            starting_side = ToSideString(team.StartingTeam),
            players = team
                .Players.Select(player => new
                {
                    steamid = player.SteamID.ToString(),
                    name = player.Name,
                    stats = player.Stats,
                    ping = player.Handle?.Controller.Ping,
                })
                .ToList(),
        };

    public static object? ToWinner(PlayerTeam? team) =>
        team != null
            ? new { side = ToSideString(team.CurrentTeam), team = ToTeamString(team) }
            : null;

    public static object ToPlayer(PlayerState player) =>
        new
        {
            steamid = player.SteamID.ToString(),
            name = player.Name,
            user_id = player.Handle?.UserID,
            side = ToSideString(player.Team.CurrentTeam),
            is_bot = player.Handle?.IsFakeClient ?? false,
            ping = player.Handle?.Controller.Ping,
        };

    public static object ToPlayer(IPlayer player) =>
        new
        {
            steamid = player.Controller.SteamID.ToString(),
            name = player.Controller.PlayerName,
            user_id = player.UserID,
            side = ToSideString(player.Controller.Team),
            is_bot = player.IsFakeClient,
            ping = player.Controller.Ping,
        };

    public static object ToWeapon(string weapon) =>
        new { name = weapon.Replace("weapon_", ""), id = ItemHelper.GetItemDefIndex(weapon) };

    public static string ToSite(int? site) =>
        site switch
        {
            1 => "a",
            2 => "b",
            _ => "none",
        };
}
