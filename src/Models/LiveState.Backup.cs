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
        if (context.Args.Length != 1)
        {
            player?.SendChat(
                Swiftly.Core.Localizer["match.admin_restore_syntax", MatchCtx.GetChatPrefix(true)]
            );
            return;
        }
        var round = context.Args[0].ToLower().Trim().PadLeft(2, '0');
        var filename = $"{MatchCtx.GetBackupPrefix()}_round{round}.txt";
        if (File.Exists(filename))
        {
            Swiftly.Log(
                sendToChat: true,
                message: Swiftly.Core.Localizer[
                    "match.admin_restore",
                    MatchCtx.GetChatPrefix(true),
                    player?.Controller.PlayerName ?? "Console"
                ]
            );
            // We load the stats before trying to restore the round. Most cases should work as
            // `mp_backup_restore_load_file` can only fail when the file is not found, but we already had a check
            // for that.
            var players = MatchCtx.GetAllPlayers();
            foreach (var report in players.SelectMany(p => p.DamageReport.Values))
                report.Reset();
            if (int.TryParse(round, out var roundAsInt))
            {
                if (roundAsInt == 0)
                {
                    MatchCtx.ResetAllPlayerAndTeamStats();
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
                MatchCtx.SendEvent(OnBackupRestoreEvent.Create(filename));
                Swiftly.Core.Engine.ExecuteCommand($"mp_backup_restore_load_file {filename}");
            }
            else
                player?.SendChat(
                    Swiftly.Core.Localizer[
                        "match.admin_restore_error",
                        MatchCtx.GetChatPrefix(true),
                        round
                    ]
                );
        }
        else
            player?.SendChat(
                Swiftly.Core.Localizer[
                    "match.admin_restore_error",
                    MatchCtx.GetChatPrefix(true),
                    round
                ]
            );
    }
}
