/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Match.Get5.Events;
using SwiftlyS2.Shared.Commands;

namespace Match;

public partial class LiveState
{
    public void OnRestoreCommand(ICommandContext context)
    {
        var player = context.Sender;
        if (
            player != null
            && !Swiftly.Core.Permission.PlayerHasPermissions(player.SteamID, ["@css/config"])
        )
            return;
        if (context.Args.Count() != 2)
        {
            player?.SendChat(
                Swiftly.Core.Localizer["match.admin_restore_syntax", Game.GetChatPrefix(true)]
            );
            return;
        }
        var round = context.Args[1].ToLower().Trim().PadLeft(2, '0');
        var filenameAsArg = $"{Game.BackupPrefix}_round{round}.txt";
        var filename = Swiftly.Core.GetCSGOPath(filenameAsArg);
        if (File.Exists(filename))
        {
            Swiftly.Log(
                sendToChat: true,
                message: Swiftly.Core.Localizer[
                    "match.admin_restore",
                    Game.GetChatPrefix(true),
                    player?.Controller.PlayerName ?? "Console"
                ]
            );
            // We load the stats before trying to restore the round. Most cases should work as
            // `mp_backup_restore_load_file` can only fail when the file is not found, but we already had a check
            // for that.
            var players = Game.Teams.SelectMany(t => t.Players);
            foreach (var report in players.SelectMany(p => p.DamageReport.Values))
                report.Reset();
            if (int.TryParse(round, out var roundAsInt))
            {
                if (roundAsInt == 0)
                {
                    foreach (var p in players)
                        p.Stats = new(p.SteamID);
                    foreach (var t in Game.Teams)
                        t.Stats = new();
                }
                else
                {
                    if (_statsBackup.TryGetValue(roundAsInt, out var playerSnapshots))
                        foreach (var (playerState, playerStats) in playerSnapshots)
                            playerState.Stats = playerStats.Clone();
                    if (_teamStatsBackup.TryGetValue(roundAsInt, out var teamSnapshots))
                        foreach (var (team, teamStats) in teamSnapshots)
                            team.Stats = teamStats.Clone();
                }
                // Because we increment at OnRoundStart.
                Round = roundAsInt - 1;
                _thrownMolotovs.Clear();
                Game.SendEvent(OnBackupRestoreEvent.Create(filename));
                Swiftly.Core.Engine.ExecuteCommand($"mp_backup_restore_load_file {filenameAsArg}");
            }
            else
                player?.SendChat(
                    Swiftly.Core.Localizer[
                        "match.admin_restore_error",
                        Game.GetChatPrefix(true),
                        round
                    ]
                );
        }
        else
            player?.SendChat(
                Swiftly.Core.Localizer["match.admin_restore_error", Game.GetChatPrefix(true), round]
            );
    }
}
