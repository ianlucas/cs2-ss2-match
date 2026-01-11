match_chat_prefix "[{red}Match{default}]"
    Prefix displayed before chat messages.

match_server_graphic_url ""
    URL of the image displayed when a player dies.

match_server_graphic_duration 5
    Duration in seconds to display the server graphic.

get5_server_id ""
    Unique identifier for this server.

match_verbose true
    Enable verbose debug logging.

match_tv_record true
    Enable automatic demo recording.

match_tv_delay 105
    SourceTV broadcast delay in seconds.

match_restart_first_map false
    Whether to restart the first map on load.

match_max_rounds 24
    Maximum number of rounds per match.

match_ot_max_rounds 6
    Number of overtime rounds to determine the winner.

match_players_needed 10
    Total number of players required to start a match.

match_players_needed_per_team 5
    Number of players required per team to start a match.

match_bots false
    Allow bots to join and fill empty player slots.

match_matchmaking false
    Enable matchmaking mode.

match_matchmaking_kick true
    Kick players who are not part of the current match.

match_matchmaking_ready_timeout 300
    Time in seconds for players to ready up.

match_knife_round_enabled true
    Enable knife rounds for side selection.

match_knife_vote_timeout 60
    Time in seconds to decide which side to start on after knife round.

match_friendly_pause false
    Allow teams to pause the match at any time.

match_forfeit_enabled true
    Automatically forfeit teams with disconnected players.

match_forfeit_timeout 120
    Time in seconds before forfeiting a team with disconnected players.

match_surrender_timeout 30
    Time in seconds allowed for surrender voting.

get5_remote_log_url ""
    URL endpoint for sending match events.

get5_remote_log_header_key ""
    Header key name for remote logging requests.

get5_remote_log_header_value ""
    Header value for remote logging requests.

match_load <filename>
get5_loadmatch <filename>
    Load a match configuration from a JSON file.
    Requires @css/config permission.

match_status
    Display the current match status, including state, teams, and players.
    Requires @css/config permission.

sw_start
    Force start the match, marking all connected players as ready.
    Requires @css/config permission.

sw_restart
    Reset and restart the match plugin.
    Requires @css/config permission.

sw_map <mapname>
    Change to the specified map (must start with "de_").
    Requires @css/config permission.

sw_restore
    Restore match state from backup (available during live match).
    Requires @css/config permission.

sw_ready
sw_r
sw_pronto
    Mark yourself as ready to start the match.

sw_unready
sw_ur
sw_naopronto
    Mark yourself as not ready.

sw_stay
sw_ficar
    Vote to stay on the current side after winning the knife round.

sw_switch
sw_trocar
    Vote to switch sides after winning the knife round.

sw_pause
sw_p
sw_pausar
    Request to pause the match.

sw_unpause
sw_up
sw_despausar
    Request to unpause the match.

sw_gg
sw_desistir
    Vote to surrender the match.