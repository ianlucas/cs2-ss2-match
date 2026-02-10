/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Reflection;
using Match.Get5.Events;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;

namespace Match;

public static class Game
{
    public static readonly List<PlayerTeam> Teams = [];
    public static readonly List<Map> Maps = [];
    public static readonly PlayerTeam Team1;
    public static readonly PlayerTeam Team2;
    public static string? Id { get; set; } = null;
    public static bool IsClinchSeries { get; set; } = true;
    public static BaseState State { get; set; } = new();
    public static bool IsLoadedFromFile { get; set; } = false;
    public static bool IsSeriesStarted { get; set; } = false;
    public static bool DidRestartFirstMap { get; set; } = false;
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

    public static void Setup()
    {
        if (Id == "" || !IsLoadedFromFile)
            Id = Guid.NewGuid().ToString();
        if (!IsLoadedFromFile)
            Maps.Add(new(Swiftly.Core.Engine.GlobalVars.MapName));
        var idsInMatch = GetAllPlayers().Select(p => p.SteamID);
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
        SendEvent(OnSeriesInitEvent.Create());
    }

    public static void SetState(BaseState newState)
    {
        if (newState is not ReadyupWarmupState && State.GetType() == newState.GetType())
            return;
        SendEvent(OnGameStateChangedEvent.Create(oldState: State, newState));
        State.Unload();
        Swiftly.Log($"Unloaded {State.GetType().FullName}");
        State = newState;
        State.Load();
        Swiftly.Log($"Loaded {State.GetType().FullName}");
    }

    public static bool EnsureCorrectMap()
    {
        var currentInGameMap = Swiftly.Core.Engine.GlobalVars.MapName;
        var currentMap = GetMap();
        if (
            (currentMap != null && (currentInGameMap != currentMap.MapName))
            || (ConVars.IsRestartFirstMap.Value && !DidRestartFirstMap)
        )
        {
            // Next level change will be handled by the game.
            if (Swiftly.Core.ConVar.Find<string>("nextlevel")?.Value != "")
                return true;
            DidRestartFirstMap = true;
            var currentMapName = currentMap?.MapName ?? currentInGameMap;
            Swiftly.Log($"Need to change map to {currentMapName}");
            Swiftly.Core.Engine.ExecuteCommand($"changelevel {currentMapName}");
            return true;
        }
        return false;
    }

    public static void EnforceMatchmakingRestrictions()
    {
        if (!ConVars.IsMatchmaking.Value)
            return;
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

    public static void ConfigureTeamFromSchema(
        int teamIndex,
        Get5.Get5MatchTeam schema,
        ulong? leaderId = null
    )
    {
        if (teamIndex < 0 || teamIndex >= Teams.Count)
            return;

        var team = Teams[teamIndex];
        team.Id = schema.Id ?? "";
        team.Name = schema.Name ?? "";
        team.SeriesScore = schema.SeriesScore ?? 0;

        var players = schema.Players.Get();
        if (players == null)
            return;

        bool electedInGameLeader = false;

        foreach (var playerSchema in players)
        {
            var steamId = playerSchema.Key;
            var player = new PlayerState(
                steamId,
                playerSchema.Value,
                team,
                Swiftly.Core.PlayerManager.GetPlayerFromSteamID(steamId)
            );
            team.AddPlayer(player);

            if (!electedInGameLeader && (leaderId == null || steamId == leaderId))
            {
                electedInGameLeader = true;
                team.InGameLeader = player;
            }
        }
    }

    public static bool IsMatchmaking()
    {
        return IsLoadedFromFile && ConVars.IsMatchmaking.Value;
    }

    public static bool AreTeamsLocked()
    {
        return IsLoadedFromFile || State is not ReadyupWarmupState;
    }

    public static string GetChatPrefix(bool stripColors = false)
    {
        return stripColors
            ? ConVars.ChatPrefix.Value.StripColors()
            : ConVars.ChatPrefix.Value.ApplyColors();
    }

    public static IEnumerable<PlayerState> GetAllPlayers()
    {
        return Teams.SelectMany(t => t.Players);
    }

    public static PlayerState? GetPlayerStateFromSteamID(ulong steamId)
    {
        return GetAllPlayers().FirstOrDefault(p => p.SteamID == steamId);
    }

    public static int GetNeededPlayersCount()
    {
        return IsLoadedFromFile ? GetAllPlayers().Count() : ConVars.PlayersNeeded.Value;
    }

    public static int GetReadyPlayersCount()
    {
        return GetAllPlayers().Count(p => p.IsReady);
    }

    public static bool AreAllPlayersReady()
    {
        return GetAllPlayers().All(p => p.IsReady);
    }

    public static PlayerTeam? GetTeam(Team team)
    {
        return Teams.FirstOrDefault(t => t.StartingTeam == team);
    }

    public static bool HasTeamsWithAnyPlayerConnected()
    {
        return Teams.All(t => t.Players.Any(p => p.Handle != null));
    }

    public static IEnumerable<PlayerTeam> GetUnreadyTeams()
    {
        return Teams.Where(t => t.Players.Any(p => !p.IsReady));
    }

    public static IEnumerable<PlayerTeam> GetTeamsWithConnectedPlayers()
    {
        return Teams.Where(t => t.Players.Any(p => p.Handle != null));
    }

    public static bool AreAllTeamsReadyToUnpause()
    {
        return Teams.All(team => team.IsUnpauseMatch);
    }

    public static Map? GetMap()
    {
        return Maps.FirstOrDefault(m => m.Result == MapResult.None);
    }

    public static Map? GetNextMap()
    {
        var maps = Maps.Where(m => m.Result == MapResult.None).ToList();
        if (maps.Count > 1)
            return maps[1];
        return null;
    }

    public static int GetMapIndex()
    {
        var map = GetMap();
        if (map == null)
            return 0;
        return Maps.IndexOf(map);
    }

    public static int FindMapIndex(Map? map)
    {
        return map != null ? Maps.IndexOf(map) : 0;
    }

    public static void AddMap(string mapName)
    {
        Maps.Add(new Map(mapName));
    }

    public static int GetTotalMapCount()
    {
        return Maps.Count;
    }

    public static IEnumerable<Map> GetCompletedMaps()
    {
        var maps = Maps.Count > 0 ? Maps : new List<Map>();
        return maps.Where(m => m.Result != MapResult.None);
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

    public static string GetMatchFolder()
    {
        return Id != null ? $"/{(IsLoadedFromFile ? "Matches" : "Scrims")}/{Id}" : "";
    }

    public static DirectoryInfo CreateMatchFolder()
    {
        return Directory.CreateDirectory(Swiftly.Core.GetConfigPath(GetMatchFolder()));
    }

    public static string? GetBackupPrefix()
    {
        return Id != null
            ? Swiftly.Core.GetConfigPath(
                $"{GetMatchFolder()}/{Swiftly.Core.Engine.GlobalVars.MapName}"
            )
            : null;
    }

    public static string? GetDemoFilename()
    {
        return Id != null
            ? Swiftly.Core.GetConfigPath(
                $"{GetMatchFolder()}/{Swiftly.Core.Engine.GlobalVars.MapName}.dem"
            )
            : null;
    }

    public static void SwapTeamSides()
    {
        foreach (var team in Teams)
            team.StartingTeam = team.StartingTeam.Toggle();
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

    public static void ResetAllPlayerAndTeamStats()
    {
        foreach (var player in GetAllPlayers())
            player.Stats = new(player.SteamID);

        foreach (var team in Teams)
            team.Stats = new();
    }

    public static void ClearAllTeamUnpauseFlags()
    {
        foreach (var team in Teams)
            team.IsUnpauseMatch = false;
    }

    public static void ClearAllSurrenderFlags()
    {
        foreach (var team in Teams)
            team.IsSurrended = false;
    }

    public static void ResetAllKnifeRoundVotes()
    {
        foreach (var player in GetAllPlayers())
            player.KnifeRoundVote = KnifeRoundVote.None;
    }

    public static void SendEvent(Get5Event data)
    {
        var url = ConVars.RemoteLogUrl.Value;
        Swiftly.Log($"RemoteLogUrl='{url}' event='{data.EventName}'");
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
}
