/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Match.Get5;
using Match.Get5.Events;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;

namespace Match;

public partial class Match
{
    public void OnMatchStatusCommand(ICommandContext context)
    {
        var caller = context.Sender;
        if (
            caller != null
            && !Core.Permission.PlayerHasPermissions(caller.SteamID, ["@css/config"])
        )
            return;
        var message = "[MatchPlugin Status]\n\n";
        message += $"State: {Game.State.GetType().Name}\n";
        message += $"Id: {Game.Id ?? "(No ID)"}\n";
        message += $"Loaded from file?: {Game.IsLoadedFromFile}\n";
        message += $"Is matchmaking?: {Game.IsMatchmaking()}\n";
        message += "\n";
        foreach (var team in Game.Teams)
        {
            message += $"[Team {team.Index}]\n";
            if (team.Players.Count == 0)
                message += "No players.\n";
            foreach (var player in team.Players)
            {
                message += $"{player.Name}";
                if (team.InGameLeader == player)
                    message += "[L]";
                if (player.Handle != null)
                {
                    var playerTeam = player.Handle.Controller.Team switch
                    {
                        Team.T => "Terrorist",
                        Team.CT => "CT",
                        Team.Spectator => "Spectator",
                        _ => $"Other={player.Handle.Controller.Team}",
                    };
                    message += $" ({playerTeam})";
                }
                else
                    message += " (Disconnected)";
                message += "\n";
            }
        }
        caller?.SendConsole(message);
        Console.WriteLine(message);
    }

    public void OnStartCommand(ICommandContext context)
    {
        var caller = context.Sender;
        if (
            caller != null
            && !Core.Permission.PlayerHasPermissions(caller.SteamID, ["@css/config"])
        )
            return;
        if (Game.State is not ReadyupWarmupState)
            return;
        if (!Game.IsLoadedFromFile)
        {
            foreach (var player in Core.PlayerManager.GetPlayersInTeams())
            {
                var playerState = player.GetState();
                if (playerState == null)
                {
                    var team = Game.GetTeam(player.Controller.Team);
                    if (team == null)
                        player.ChangeTeam(Team.Spectator);
                    else
                    {
                        playerState = new(
                            player.SteamID,
                            player.Controller.PlayerName,
                            team,
                            player
                        );
                        team.AddPlayer(playerState);
                    }
                }
                if (playerState != null)
                    playerState.IsReady = true;
            }
            Game.Setup();
        }
        else
            foreach (var player in Game.Teams.SelectMany(t => t.Players))
                player.IsReady = true;
        Game.Log(
            printToChat: true,
            message: Core.Localizer[
                "match.admin_start",
                Game.GetChatPrefix(true),
                caller?.Controller.PlayerName ?? "Console"
            ]
        );
        Game.SetState(ConVars.IsKnifeRoundEnabled.Value ? new KnifeRoundState() : new LiveState());
    }

    public void OnMapCommand(ICommandContext context)
    {
        var caller = context.Sender;
        if (
            caller != null
            && !Core.Permission.PlayerHasPermissions(caller.SteamID, ["@css/config"])
        )
            return;
        if (context.Args.Length != 2)
            return;
        var mapname = context.Args[1].ToLower().Trim();
        if (!mapname.StartsWith("de_"))
            return;
        if (Game.AreTeamsLocked())
            return;
        Game.Log(
            printToChat: true,
            message: Core.Localizer[
                "match.admin_map",
                Game.GetChatPrefix(true),
                caller?.Controller.PlayerName ?? "Console"
            ]
        );
        Core.Engine.ExecuteCommand($"changelevel {mapname}");
    }

    public void OnRestartCommand(ICommandContext context)
    {
        var caller = context.Sender;
        if (
            caller != null
            && !Core.Permission.PlayerHasPermissions(caller.SteamID, ["@css/config"])
        )
            return;
        Game.Log(
            printToChat: true,
            message: Core.Localizer[
                "match.admin_restart",
                Game.GetChatPrefix(true),
                caller?.Controller.PlayerName ?? "Console"
            ]
        );
        Game.Reset();
        Game.SetState(new NoneState());
    }

    public void OnMatchLoadCommand(ICommandContext context)
    {
        var caller = context.Sender;
        if (
            caller != null
            && !Core.Permission.PlayerHasPermissions(caller.SteamID, ["@css/config"])
        )
            return;
        if (Game.State is not ReadyupWarmupState)
            return;
        if (context.Args.Length != 2)
            return;
        var name = context.Args[1].Trim();
        var file = Get5Match.Read(name);
        if (file.Error != null)
            Game.SendEvent(OnLoadMatchConfigFailedEvent.Create(reason: file.Error));
        var match = file.Contents;
        if (match == null || file.Path == null)
            return;
        Game.SendEvent(OnPreLoadMatchConfigEvent.Create(filename: file.Path));
        Game.Reset();
        Game.IsLoadedFromFile = true;
        Game.Id = match.Matchid;
        Game.IsClinchSeries = match.ClinchSeries ?? true;
        // Maps
        var maplist = match.Maplist.Get();
        if (maplist != null)
            foreach (var mapName in maplist)
                Game.Maps.Add(new(mapName));
        else
        {
            Game.Reset();
            return;
        }
        // Teams
        Game.Team1.StartingTeam = Team.T;
        Game.Team2.StartingTeam = Team.CT;
        for (var index = 0; index < Game.Teams.Count; index++)
        {
            var team = Game.Teams[index];
            var teamSchema = (index == 0 ? match.Team1 : match.Team2)?.Get();
            var players = teamSchema?.Players.Get();
            if (teamSchema == null || players == null)
                continue;
            var electedInGameLeader = false;
            ulong? leaderId = ulong.TryParse(teamSchema.Leaderid, out ulong li) ? li : null;
            team.Id = teamSchema.Id ?? "";
            team.Name = teamSchema.Name ?? "";
            team.SeriesScore = teamSchema.SeriesScore ?? 0;
            foreach (var playerSchema in players)
            {
                var steamId = playerSchema.Key;
                var player = new Player(
                    steamId,
                    playerSchema.Value,
                    team,
                    Core.PlayerManager.GetPlayerFromSteamID(steamId)
                );
                team.AddPlayer(player);
                if (!electedInGameLeader && (leaderId == null || steamId == leaderId))
                {
                    electedInGameLeader = true;
                    team.InGameLeader = player;
                }
            }
        }
        if (match.Cvars != null)
            foreach (var (key, value) in match.Cvars)
            {
                var cmd = $"{key} {value}";
                Game.Log($"Executing command: {cmd}");
                Core.Engine.ExecuteCommand(cmd);
            }
        Swiftly.Core.Scheduler.NextWorldUpdate(() =>
        {
            Game.Setup();
            Game.SetState(new ReadyupWarmupState());
        });
    }
}
