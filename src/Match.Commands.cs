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
        message += $"State: {Rules.State.GetType().Name}\n";
        message += $"Id: {Rules.Id ?? "(No ID)"}\n";
        message += $"Loaded from file?: {Rules.IsLoadedFromFile}\n";
        message += $"Is matchmaking?: {Rules.IsMatchmaking()}\n";
        message += "\n";
        foreach (var team in Rules.Teams)
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
        if (Rules.State is not ReadyupWarmupState)
            return;
        if (!Rules.IsLoadedFromFile)
        {
            foreach (var player in Core.PlayerManager.GetPlayersInTeams())
            {
                var playerState = player.GetState();
                if (playerState == null)
                {
                    var team = Rules.GetTeam(player.Controller.Team);
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
            Rules.Setup();
        }
        else
            foreach (var player in Rules.GetAllPlayers())
                player.IsReady = true;
        Runtime.Log(
            sendToChat: true,
            message: Core.Localizer[
                "match.admin_start",
                Rules.GetChatPrefix(true),
                caller?.Controller.PlayerName ?? "Console"
            ]
        );
        Rules.SetState(ConVars.IsKnifeRoundEnabled.Value ? new KnifeRoundState() : new LiveState());
    }

    public void OnMapCommand(ICommandContext context)
    {
        var caller = context.Sender;
        if (
            caller != null
            && !Core.Permission.PlayerHasPermissions(caller.SteamID, ["@css/config"])
        )
            return;
        if (context.Args.Length != 1)
            return;
        var mapname = context.Args[0].ToLower().Trim();
        if (!mapname.StartsWith("de_"))
            return;
        if (Rules.AreTeamsLocked())
            return;
        Runtime.Log(
            sendToChat: true,
            message: Core.Localizer[
                "match.admin_map",
                Rules.GetChatPrefix(true),
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
        Runtime.Log(
            sendToChat: true,
            message: Core.Localizer[
                "match.admin_restart",
                Rules.GetChatPrefix(true),
                caller?.Controller.PlayerName ?? "Console"
            ]
        );
        Rules.Reset();
        Rules.SetState(new NoneState());
    }

    public void OnMatchLoadCommand(ICommandContext context)
    {
        var caller = context.Sender;
        if (
            caller != null
            && !Core.Permission.PlayerHasPermissions(caller.SteamID, ["@css/config"])
        )
            return;
        if (Rules.State is not ReadyupWarmupState)
            return;
        if (context.Args.Length != 1)
            return;
        var name = context.Args[0].Trim();
        var file = Get5Match.Read(name);
        if (file.Error != null)
            Rules.SendEvent(OnLoadMatchConfigFailedEvent.Create(reason: file.Error));
        var match = file.Contents;
        if (match == null || file.Path == null)
            return;
        Rules.SendEvent(OnPreLoadMatchConfigEvent.Create(filename: file.Path));
        Rules.Reset();
        Rules.IsLoadedFromFile = true;
        Rules.Id = match.Matchid;
        Rules.IsClinchSeries = match.ClinchSeries ?? true;
        // Maps
        var maplist = match.Maplist.Get();
        if (maplist != null)
            foreach (var mapName in maplist)
                Rules.AddMap(mapName);
        else
        {
            Rules.Reset();
            return;
        }
        // Teams
        Rules.Team1.StartingTeam = Team.T;
        Rules.Team2.StartingTeam = Team.CT;
        for (var index = 0; index < Rules.Teams.Count; index++)
        {
            var teamSchema = (index == 0 ? match.Team1 : match.Team2)?.Get();
            if (teamSchema == null)
                continue;
            ulong? leaderId = ulong.TryParse(teamSchema.Leaderid, out ulong li) ? li : null;
            Rules.ConfigureTeamFromSchema(index, teamSchema, leaderId);
        }
        if (match.Cvars != null)
            foreach (var (key, value) in match.Cvars)
            {
                var cmd = $"{key} {value}";
                Runtime.Log($"Executing command: {cmd}");
                Core.Engine.ExecuteCommand(cmd);
            }
        Runtime.Core.Scheduler.NextWorldUpdate(() =>
        {
            Rules.Setup();
            Rules.SetState(new ReadyupWarmupState());
        });
    }
}
