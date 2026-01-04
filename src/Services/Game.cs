/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Match;

public static class Game
{
    public static readonly List<PlayerTeam> Teams = [];
    public static readonly PlayerTeam Team1;
    public static readonly PlayerTeam Team2;
    public static string? Id { get; } = null;
    public static bool IsClinchSeries { get; } = true;
    public static BaseState State { get; } = new();
    public static bool IsLoadedFromFile { get; } = false;
    public static bool IsSeriesStarted { get; } = false;
    public static bool IsFirstMapRestarted { get; } = false;
    public static Team? KnifeRoundWinner { get; }
    public static MapEndResult? MapEndResult { get; } = null;

    static Game()
    {
        var team1 = new PlayerTeam(Team.T);
        var team2 = new PlayerTeam(Team.CT);
        team1.Opposition = team2;
        team2.Opposition = team1;
        Teams = [team1, team2];
        Team1 = team1;
        Team2 = team2;
    }

    public static string GetChatPrefix(bool stripColors = false)
    {
        return stripColors
            ? ConVars.ChatPrefix.Value.StripColors()
            : ConVars.ChatPrefix.Value.ApplyColors();
    }

    public static bool IsMatchmaking()
    {
        return IsLoadedFromFile && ConVars.IsMatchmaking.Value;
    }

    public static bool AreTeamsLocked()
    {
        return IsLoadedFromFile || State is not WarmupReadyState;
    }

    public static int GetNeededPlayersCount()
    {
        return IsLoadedFromFile
            ? Teams.SelectMany(t => t.Players).Count()
            : ConVars.PlayersNeeded.Value;
    }

    public static int GetReadyPlayersCount()
    {
        return Teams.SelectMany(t => t.Players).Count(p => p.IsReady);
    }

    public static PlayerTeam? GetTeam(Team team)
    {
        return Teams.FirstOrDefault(t => t.StartingTeam == team);
    }

    public static void ResetTeamsForNewMatch()
    {
        foreach (var team in Teams)
        {
            team.Stats = new();
            foreach (var player in team.Players)
            {
                player.IsReady = false;
                player.Stats = new(player.SteamID);
            }
        }
    }

    public static void Log(string message)
    {
        if (!ConVars.IsVerbose.Value)
            return;
        var stackTrace = new StackTrace();
        var frame = stackTrace.GetFrame(1);
        var method = frame?.GetMethod();
        var className = method?.DeclaringType?.Name;
        var methodName = method?.Name;
        var prefix =
            className != null && methodName != null ? $"{className}::{methodName}" : "Match";
        Swiftly.Core.Logger.LogInformation("{Prefix} {Message}", prefix, message);
    }
}
