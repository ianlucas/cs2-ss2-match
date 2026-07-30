/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Match.Get5.Events;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Misc;

namespace Match;

public partial class LiveState
{
    private class StatsBackup
    {
        [JsonPropertyName("teams")]
        public Dictionary<int, TeamStats> Teams { get; set; } = [];

        [JsonPropertyName("players")]
        public Dictionary<string, PlayerStats> Players { get; set; } = [];
    }

    private static string? GetStatsBackupFilename(int round) =>
        Rules.GetBackupPrefix() is string prefix ? $"{prefix}_round{round:00}.stats.json" : null;

    private void WriteStatsBackupToDisk(int round)
    {
        var filename = GetStatsBackupFilename(round);
        if (filename == null)
            return;
        try
        {
            var backup = new StatsBackup();
            foreach (var (team, teamStats) in _teamStatsBackup[round])
                backup.Teams[team.Index] = teamStats;
            foreach (var (playerState, playerStats) in _statsBackup[round])
                backup.Players[playerState.SteamID.ToString()] = playerStats;
            File.WriteAllText(filename, JsonSerializer.Serialize(backup));
        }
        catch (Exception e)
        {
            Runtime.Log($"Failed to write stats backup {filename}: {e.Message}", force: true);
        }
    }

    private bool TryRestoreStatsFromDisk(int round)
    {
        var filename = GetStatsBackupFilename(round);
        if (filename == null || !File.Exists(filename))
            return false;
        try
        {
            var backup = JsonSerializer.Deserialize<StatsBackup>(File.ReadAllText(filename));
            if (backup == null)
                return false;
            foreach (var team in Rules.Teams)
                if (backup.Teams.TryGetValue(team.Index, out var teamStats))
                    team.Stats = teamStats;
            foreach (var playerState in Rules.GetAllPlayers())
                if (backup.Players.TryGetValue(playerState.SteamID.ToString(), out var playerStats))
                    playerState.Stats = playerStats;
            return true;
        }
        catch (Exception e)
        {
            Runtime.Log($"Failed to read stats backup {filename}: {e.Message}", force: true);
            return false;
        }
    }

    private void RestoreStats(int round)
    {
        if (_statsBackup.TryGetValue(round, out var playerSnapshots))
        {
            foreach (var (playerState, playerStats) in playerSnapshots)
                playerState.Stats = playerStats.Clone();
            if (_teamStatsBackup.TryGetValue(round, out var teamSnapshots))
                foreach (var (team, teamStats) in teamSnapshots)
                    team.Stats = teamStats.Clone();
        }
        else if (!TryRestoreStatsFromDisk(round))
            Runtime.Log(
                sendToChat: true,
                force: true,
                message: Runtime.Core.Localizer[
                    "match.admin_restore_stats_warning",
                    Rules.GetChatPrefix(true),
                    round
                ]
            );
    }

    private void PrepareForRestore(int roundAsInt)
    {
        foreach (var report in Rules.GetAllPlayers().SelectMany(p => p.DamageReport.Values))
            report.Reset();
        if (roundAsInt == 0)
            Rules.ResetAllPlayerAndTeamStats();
        else
            RestoreStats(roundAsInt);
        // Because we increment at OnRoundStart.
        Round = roundAsInt - 1;
        _thrownUtilities.Clear();
        _roundKills.Clear();
        _playerDied.Clear();
        _playerKilledOrAssistedOrTradedKill.Clear();
        _playerPlayedRound.Clear();
        _roundClutchingCount.Clear();
        _isTeamClutching.Clear();
        _playerKilledBy.Clear();
        _lastDamageWeapon.Clear();
        _lastHitToken.Clear();
        _hadOpeningDuel = false;
        _isRestoring = true;
    }

    public void OnCommandExecuteHook(IOnCommandExecuteHookEvent @event)
    {
        if (
            @event.HookMode != HookMode.Pre
            || _isRestoring
            || @event.Command.Arg(0) != "mp_backup_restore_load_file"
        )
            return;
        var filename = @event.Command.Arg(1) ?? "";
        var match = Regex.Match(filename, @"_round(\d+)\.txt$", RegexOptions.IgnoreCase);
        if (match.Success && File.Exists(filename))
        {
            Runtime.Log(
                $"Out-of-band backup restore detected ({filename}), restoring stats.",
                force: true
            );
            PrepareForRestore(int.Parse(match.Groups[1].Value));
            Rules.SendEvent(OnBackupRestoreEvent.Create(filename));
        }
        else
            Runtime.Log(
                $"Out-of-band backup restore detected ({filename}), but the round could not be determined. Stats may be desynced.",
                force: true
            );
    }

    public void OnRestoreCommand(ICommandContext context)
    {
        var player = context.Sender;
        if (
            player != null
            && !Runtime.Core.Permission.PlayerHasPermissions(player.SteamID, ["@css/config"])
        )
            return;
        if (context.Args.Length != 1)
        {
            player?.SendChat(
                Runtime.Core.Localizer["match.admin_restore_syntax", Rules.GetChatPrefix(true)]
            );
            return;
        }
        var round = context.Args[0].ToLower().Trim().PadLeft(2, '0');
        var filename = $"{Rules.GetBackupPrefix()}_round{round}.txt";
        if (File.Exists(filename) && int.TryParse(round, out var roundAsInt))
        {
            Runtime.Log(
                sendToChat: true,
                message: Runtime.Core.Localizer[
                    "match.admin_restore",
                    Rules.GetChatPrefix(true),
                    player?.Controller.PlayerName ?? "Console"
                ]
            );
            // We load the stats before trying to restore the round. Most cases should work as
            // `mp_backup_restore_load_file` can only fail when the file is not found, but we already had a check
            // for that.
            PrepareForRestore(roundAsInt);
            Rules.SendEvent(OnBackupRestoreEvent.Create(filename));
            Runtime.Core.Engine.ExecuteCommand($"mp_backup_restore_load_file \"{filename}\"");
        }
        else
            player?.SendChat(
                Runtime.Core.Localizer[
                    "match.admin_restore_error",
                    Rules.GetChatPrefix(true),
                    round
                ]
            );
    }
}
