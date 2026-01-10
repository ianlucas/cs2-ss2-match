/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;

namespace Match;

public static class Game
{
    public static readonly Get5 Get5 = new();
    public static readonly List<PlayerTeam> Teams = [];
    public static readonly List<Map> Maps = [];
    public static readonly PlayerTeam Team1;
    public static readonly PlayerTeam Team2;
    public static string? Id { get; set; } = null;
    public static bool IsClinchSeries { get; set; } = true;
    public static BaseState State { get; set; } = new();
    public static bool IsLoadedFromFile { get; set; } = false;
    public static bool IsSeriesStarted { get; set; } = false;
    public static PlayerTeam? KnifeRoundWinner { get; set; }
    public static MapEndResult? MapEndResult { get; set; } = null;

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

    public static void Reset()
    {
        Id = null;
        IsClinchSeries = true;
        IsLoadedFromFile = false;
        IsSeriesStarted = false;
        KnifeRoundWinner = null;
        MapEndResult = null;
        Maps.Clear();
        foreach (var team in Teams)
            team.Reset();
    }

    public static void SetState(BaseState newState)
    {
        if (newState is not ReadyupWarmupState && State.GetType() == newState.GetType())
            return;
        SendEvent(Get5.OnGameStateChanged(oldState: State, newState));
        State.Unload();
        Log($"Unloaded {State.GetType().FullName}");
        State = newState;
        State.Load();
        Log($"Loaded {State.GetType().FullName}");
    }

    public static void Setup()
    {
        if (Id == "" || !IsLoadedFromFile)
            Id = Guid.NewGuid().ToString();
        if (!IsLoadedFromFile)
            Maps.Add(new(Swiftly.Core.Engine.GlobalVars.MapName));
        var idsInMatch = Teams.SelectMany(t => t.Players).Select(p => p.SteamID);
        foreach (var player in Swiftly.Core.PlayerManager.GetActualPlayers())
            if (!idsInMatch.Contains(player.SteamID))
                if (
                    !ConVars.IsMatchmaking.Value
                    || !ConVars.IsMatchmakingKick.Value
                    || Swiftly.Core.Permission.PlayerHasPermission(player.SteamID, "@css/config")
                )
                    player.ChangeTeam(Team.Spectator);
                else
                    player.Kick(
                        "Match is reserved for a lobby.",
                        ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_RESERVED_FOR_LOBBY
                    );
        foreach (var team in Teams)
        {
            Swiftly.Core.Engine.SetTeamName(team.StartingTeam, team.ServerName);
            foreach (var player in team.Players)
            {
                player.DamageReport.Clear();
                foreach (var opponent in team.Opposition.Players)
                    player.DamageReport.Add(opponent.SteamID, new(opponent));
            }
        }
        IsSeriesStarted = true;
        CreateMatchFolder();
        SendEvent(Get5.OnSeriesInit());
    }

    public static bool CheckCurrentMap()
    {
        var currentMap = GetMap();
        if (currentMap != null && (Swiftly.Core.Engine.GlobalVars.MapName != currentMap.MapName))
        {
            var currentMapName = currentMap?.MapName ?? Swiftly.Core.Engine.GlobalVars.MapName;
            Log($"Need to change map to {currentMapName}");
            Swiftly.Core.Engine.ExecuteCommand($"changelevel {currentMapName}");
            return true;
        }
        return false;
    }

    public static void EvaluateMatchmakingCondicion()
    {
        if (ConVars.IsMatchmaking.Value)
            foreach (var player in Swiftly.Core.PlayerManager.GetActualPlayers())
                if (player.GetState() == null)
                    if (Swiftly.Core.Permission.PlayerHasPermission(player.SteamID, "@css/root"))
                        player.ChangeTeam(Team.Spectator);
                    else
                        player.Kick(
                            "Match is reserved for a lobby.",
                            ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_RESERVED_FOR_LOBBY
                        );
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
        return IsLoadedFromFile || State is not ReadyupWarmupState;
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

    public static Map? GetMap()
    {
        return Maps.FirstOrDefault(m => m.Result == MapResult.None);
    }

    public static int FindMapIndex(Map? map)
    {
        return map != null ? Maps.IndexOf(map) : 0;
    }

    public static int GetMapIndex()
    {
        var map = GetMap();
        if (map == null)
            return 0;
        return Maps.IndexOf(map);
    }

    public static long GetRoundTime()
    {
        return State is LiveState state ? TimeHelper.Now() - state.RoundStartedAt : 0;
    }

    public static int GetRoundNumber() =>
        State is LiveState state
            ? state.Round > -1
                ? state.Round
                : 0
            : 0;

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

    public static string MatchFolder =>
        Id != null ? $"/{(IsLoadedFromFile ? "M_" : "S_")}{Id}" : "";

    public static DirectoryInfo CreateMatchFolder() =>
        Directory.CreateDirectory(Swiftly.Core.GetConfigPath(MatchFolder));

    public static string? BackupPrefix =>
        Id != null
            ? Swiftly.Core.GetConfigPath($"{MatchFolder}/{Swiftly.Core.Engine.GlobalVars.MapName}")
            : null;

    public static string? DemoFilename =>
        Id != null
            ? Swiftly.Core.GetConfigPath(
                $"{MatchFolder}/{Swiftly.Core.Engine.GlobalVars.MapName}.dem"
            )
            : null;

    public static void SendEvent(object data)
    {
        var url = ConVars.RemoteLogUrl.Value;
        PropertyInfo? propertyInfo = data.GetType().GetProperty("event");
        Log($"RemoteLogUrl='{url}' event='{propertyInfo?.GetValue(data)}'");
        if (url != "")
        {
            var headers = new Dictionary<string, string>();
            if (ConVars.ServerId.Value != "")
                headers.Add("Get5-ServerId", ConVars.ServerId.Value);
            if (ConVars.RemoteLogHeaderKey.Value != "" && ConVars.RemoteLogHeaderValue.Value != "")
                headers.Add(ConVars.RemoteLogHeaderKey.Value, ConVars.RemoteLogHeaderValue.Value);
            HttpHelper.SendJson(url, data, headers);
        }
    }

    public static void Log(string message, bool printToChat = false)
    {
        if (printToChat)
            Swiftly.Core.PlayerManager.SendChat(message);
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
