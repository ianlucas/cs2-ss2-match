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
        message += $"State: {MatchCtx.State.GetType().Name}\n";
        message += $"Id: {MatchCtx.Id ?? "(No ID)"}\n";
        message += $"Loaded from file?: {MatchCtx.IsLoadedFromFile}\n";
        message += $"Is matchmaking?: {MatchCtx.IsMatchmaking()}\n";
        message += "\n";
        foreach (var team in MatchCtx.Teams)
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
        if (MatchCtx.State is not ReadyupWarmupState)
            return;
        if (!MatchCtx.IsLoadedFromFile)
        {
            foreach (var player in Core.PlayerManager.GetPlayersInTeams())
            {
                var playerState = player.GetState();
                if (playerState == null)
                {
                    var team = MatchCtx.GetTeam(player.Controller.Team);
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
            MatchCtx.Setup();
        }
        else
            foreach (var player in MatchCtx.GetAllPlayers())
                player.IsReady = true;
        Swiftly.Log(
            sendToChat: true,
            message: Core.Localizer[
                "match.admin_start",
                MatchCtx.GetChatPrefix(true),
                caller?.Controller.PlayerName ?? "Console"
            ]
        );
        MatchCtx.SetState(
            ConVars.IsKnifeRoundEnabled.Value ? new KnifeRoundState() : new LiveState()
        );
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
        if (MatchCtx.AreTeamsLocked())
            return;
        Swiftly.Log(
            sendToChat: true,
            message: Core.Localizer[
                "match.admin_map",
                MatchCtx.GetChatPrefix(true),
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
        Swiftly.Log(
            sendToChat: true,
            message: Core.Localizer[
                "match.admin_restart",
                MatchCtx.GetChatPrefix(true),
                caller?.Controller.PlayerName ?? "Console"
            ]
        );
        MatchCtx.Reset();
        MatchCtx.SetState(new NoneState());
    }

    public void OnMatchLoadCommand(ICommandContext context)
    {
        var caller = context.Sender;
        if (
            caller != null
            && !Core.Permission.PlayerHasPermissions(caller.SteamID, ["@css/config"])
        )
            return;
        if (MatchCtx.State is not ReadyupWarmupState)
            return;
        if (context.Args.Length != 1)
            return;
        var name = context.Args[0].Trim();
        var file = Get5Match.Read(name);
        if (file.Error != null)
            MatchCtx.SendEvent(OnLoadMatchConfigFailedEvent.Create(reason: file.Error));
        var match = file.Contents;
        if (match == null || file.Path == null)
            return;
        MatchCtx.SendEvent(OnPreLoadMatchConfigEvent.Create(filename: file.Path));
        MatchCtx.Reset();
        MatchCtx.IsLoadedFromFile = true;
        MatchCtx.Id = match.Matchid;
        MatchCtx.IsClinchSeries = match.ClinchSeries ?? true;
        // Maps
        var maplist = match.Maplist.Get();
        if (maplist != null)
            foreach (var mapName in maplist)
                MatchCtx.AddMap(mapName);
        else
        {
            MatchCtx.Reset();
            return;
        }
        // Teams
        MatchCtx.Team1.StartingTeam = Team.T;
        MatchCtx.Team2.StartingTeam = Team.CT;
        for (var index = 0; index < MatchCtx.Teams.Count; index++)
        {
            var teamSchema = (index == 0 ? match.Team1 : match.Team2)?.Get();
            if (teamSchema == null)
                continue;
            ulong? leaderId = ulong.TryParse(teamSchema.Leaderid, out ulong li) ? li : null;
            MatchCtx.ConfigureTeamFromSchema(index, teamSchema, leaderId);
        }
        if (match.Cvars != null)
            foreach (var (key, value) in match.Cvars)
            {
                var cmd = $"{key} {value}";
                Swiftly.Log($"Executing command: {cmd}");
                Core.Engine.ExecuteCommand(cmd);
            }
        Swiftly.Core.Scheduler.NextWorldUpdate(() =>
        {
            MatchCtx.Setup();
            MatchCtx.SetState(new ReadyupWarmupState());
        });
    }
}
