/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Convars;

namespace Match;

public static class ConVars
{
    public static readonly IConVar<string> ChatPrefix = Swiftly.Core.ConVar.Create(
        "match_chat_prefix",
        "Prefix displayed before chat messages.",
        "[{red}Match{default}]"
    );

    public static readonly IConVar<string> ServerGraphicUrl = Swiftly.Core.ConVar.Create(
        "match_server_graphic_url",
        "URL of the image displayed when a player dies.",
        ""
    );

    public static readonly IConVar<int> ServerGraphicDuration = Swiftly.Core.ConVar.Create(
        "match_server_graphic_duration",
        "Duration in seconds to display the server graphic.",
        5
    );

    public static readonly IConVar<string> ServerId = Swiftly.Core.ConVar.Create(
        "get5_server_id",
        "Unique identifier for this server.",
        ""
    );

    public static readonly IConVar<bool> IsVerbose = Swiftly.Core.ConVar.Create(
        "match_verbose",
        "Enable verbose debug logging.",
        true
    );

    public static readonly IConVar<bool> IsTvRecord = Swiftly.Core.ConVar.Create(
        "match_tv_record",
        "Enable automatic demo recording.",
        true
    );

    public static readonly IConVar<int> TvDelay = Swiftly.Core.ConVar.Create(
        "match_tv_delay",
        "SourceTV broadcast delay in seconds.",
        105
    );

    public static readonly IConVar<bool> IsRestartFirstMap = Swiftly.Core.ConVar.Create(
        "match_restart_first_map",
        "Whether to restart the first map on load.",
        false
    );

    public static readonly IConVar<int> MaxRounds = Swiftly.Core.ConVar.Create(
        "match_max_rounds",
        "Maximum number of rounds per match.",
        24
    );

    public static readonly IConVar<int> OtMaxRounds = Swiftly.Core.ConVar.Create(
        "match_ot_max_rounds",
        "Number of overtime rounds to determine the winner.",
        6
    );

    public static readonly IConVar<int> PlayersNeeded = Swiftly.Core.ConVar.Create(
        "match_players_needed",
        "Total number of players required to start a match.",
        10
    );

    public static readonly IConVar<int> PlayersNeededPerTeam = Swiftly.Core.ConVar.Create(
        "match_players_needed_per_team",
        "Number of players required per team to start a match.",
        5
    );

    public static readonly IConVar<bool> IsBots = Swiftly.Core.ConVar.Create(
        "match_bots",
        "Allow bots to join and fill empty player slots.",
        false
    );

    public static readonly IConVar<bool> IsMatchmaking = Swiftly.Core.ConVar.Create(
        "match_matchmaking",
        "Enable matchmaking mode.",
        false
    );

    public static readonly IConVar<bool> IsMatchmakingKick = Swiftly.Core.ConVar.Create(
        "match_matchmaking_kick",
        "Kick players who are not part of the current match.",
        true
    );

    public static readonly IConVar<int> MatchmakingReadyTimeout = Swiftly.Core.ConVar.Create(
        "match_matchmaking_ready_timeout",
        "Time in seconds for players to ready up.",
        300
    );

    public static readonly IConVar<bool> IsKnifeRoundEnabled = Swiftly.Core.ConVar.Create(
        "match_knife_round_enabled",
        "Enable knife rounds for side selection.",
        true
    );

    public static readonly IConVar<int> KnifeVoteTimeout = Swiftly.Core.ConVar.Create(
        "match_knife_vote_timeout",
        "Time in seconds to decide which side to start on after knife round.",
        60
    );

    public static readonly IConVar<bool> IsFriendlyPause = Swiftly.Core.ConVar.Create(
        "match_friendly_pause",
        "Allow teams to pause the match at any time.",
        false
    );

    public static readonly IConVar<bool> IsForfeitEnabled = Swiftly.Core.ConVar.Create(
        "match_forfeit_enabled",
        "Automatically forfeit teams with disconnected players.",
        true
    );

    public static readonly IConVar<int> ForfeitTimeout = Swiftly.Core.ConVar.Create(
        "match_forfeit_timeout",
        "Time in seconds before forfeiting a team with disconnected players.",
        120
    );

    public static readonly IConVar<int> SurrenderTimeout = Swiftly.Core.ConVar.Create(
        "match_surrender_timeout",
        "Time in seconds allowed for surrender voting.",
        30
    );

    public static readonly IConVar<string> RemoteLogUrl = Swiftly.Core.ConVar.Create(
        "get5_remote_log_url",
        "URL endpoint for sending match events.",
        ""
    );

    public static readonly IConVar<string> RemoteLogHeaderKey = Swiftly.Core.ConVar.Create(
        "get5_remote_log_header_key",
        "Header key name for remote logging requests.",
        ""
    );

    public static readonly IConVar<string> RemoteLogHeaderValue = Swiftly.Core.ConVar.Create(
        "get5_remote_log_header_value",
        "Header value for remote logging requests.",
        ""
    );

    public static void Initialize()
    {
        _ = ChatPrefix;
        _ = ServerGraphicUrl;
        _ = ServerGraphicDuration;
        _ = ServerId;
        _ = IsVerbose;
        _ = IsTvRecord;
        _ = TvDelay;
        _ = IsRestartFirstMap;
        _ = MaxRounds;
        _ = OtMaxRounds;
        _ = PlayersNeeded;
        _ = PlayersNeededPerTeam;
        _ = IsBots;
        _ = IsMatchmaking;
        _ = IsMatchmakingKick;
        _ = MatchmakingReadyTimeout;
        _ = IsKnifeRoundEnabled;
        _ = KnifeVoteTimeout;
        _ = IsFriendlyPause;
        _ = IsForfeitEnabled;
        _ = ForfeitTimeout;
        _ = SurrenderTimeout;
        _ = RemoteLogUrl;
        _ = RemoteLogHeaderKey;
        _ = RemoteLogHeaderValue;
    }
}
