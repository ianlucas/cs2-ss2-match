/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Match;

public class Get5
{
    public object OnGameStateChanged(BaseState oldState, BaseState newState) =>
        new
        {
            @event = "game_state_changed",
            new_state = newState.Name,
            old_state = oldState.Name,
        };

    public object OnPreLoadMatchConfig(string filename) =>
        new { @event = "preload_match_config", filename };

    public object OnLoadMatchConfigFailed(string reason) =>
        new { @event = "match_config_load_fail", reason };

    public object OnSeriesInit() =>
        new
        {
            @event = "series_start",
            matchid = Game.Id,
            num_maps = Game.Maps.Count,
            team1 = new { id = Game.Team1.Id, name = Game.Team1.Name },
            team2 = new { id = Game.Team2.Id, name = Game.Team2.Name },
        };

    public object OnMapResult(Map map) =>
        new
        {
            @event = "map_result",
            matchid = Game.Id,
            map_number = Game.FindMapIndex(map),
            team1 = ToGet5StatsTeam(Game.Team1),
            team2 = ToGet5StatsTeam(Game.Team2),
            winner = ToGet5Winner(map.Winner),
            result = map.Result,
        };

    public object OnSeriesResult(PlayerTeam? winner, Map map) =>
        new
        {
            @event = "series_end",
            matchid = Game.Id,
            team1_series_score = Game.Team1.SeriesScore,
            team2_series_score = Game.Team2.SeriesScore,
            winner = ToGet5Winner(winner),
            time_until_restore = 0,
            last_map_number = Game.FindMapIndex(map),
        };

    public object OnSidePicked(PlayerTeam team)
    {
        var map = Game.GetMap();
        return new
        {
            @event = "side_picked",
            matchid = Game.Id,
            team = ToGet5TeamString(team),
            map_name = map?.MapName,
            side = ToGet5SideString(team.StartingTeam),
            map_number = Game.FindMapIndex(map),
        };
    }

    public object OnBackupRestore(string filename) =>
        new
        {
            @event = "backup_loaded",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            round_number = Game.GetRoundNumber(),
            filename,
        };

    public object OnDemoFinished(string filename) =>
        new
        {
            @event = "demo_finished",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            filename,
        };

    public object OnMatchPaused(PlayerTeam? team, string pauseType) =>
        new
        {
            @event = "game_paused",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            team = ToGet5TeamString(team),
            pause_type = pauseType,
        };

    public object OnMatchUnpaused(PlayerTeam? team, string pauseType) =>
        new
        {
            @event = "game_unpaused",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            team = ToGet5TeamString(team),
            pause_type = pauseType,
        };

    public object OnPauseBegan(PlayerTeam? team, string pauseType) =>
        new
        {
            @event = "pause_began",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            team = ToGet5TeamString(team),
            pause_type = pauseType,
        };

    public object OnKnifeRoundStarted() =>
        new
        {
            @event = "knife_start",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
        };

    public object OnKnifeRoundWon(PlayerTeam team, KnifeRoundVote decision) =>
        new
        {
            @event = "knife_won",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            team = ToGet5TeamString(team),
            side = ToGet5SideString(team.StartingTeam),
            swapped = decision == KnifeRoundVote.Switch,
        };

    public object OnTeamReadyStatusChanged(PlayerTeam team) =>
        new
        {
            @event = "team_ready_status_changed",
            matchid = Game.Id,
            team = ToGet5TeamString(team),
            ready = team.Players.All(p => p.IsReady),
            game_state = Game.State.Name,
        };

    public object OnGoingLive() =>
        new
        {
            @event = "going_live",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
        };

    public object OnRoundStart() =>
        new
        {
            @event = "round_start",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            round_number = Game.GetRoundNumber(),
        };

    public object OnRoundEnd(PlayerTeam? winner, int reason) =>
        new
        {
            @event = "round_end",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            round_number = Game.GetRoundNumber(),
            round_time = Game.GetRoundTime(),
            reason,
            winner = ToGet5Winner(winner),
            team1 = ToGet5StatsTeam(Game.Team1),
            team2 = ToGet5StatsTeam(Game.Team2),
        };

    public object OnRoundStatsUpdated() =>
        new
        {
            @event = "stats_updated",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            round_number = Game.GetRoundNumber(),
        };

    public object OnPlayerBecameMVP(Player player, int reason) =>
        new
        {
            @event = "round_mvp",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            round_number = Game.GetRoundNumber(),
            player = ToGet5Player(player),
            reason,
        };

    public object OnGrenadeThrown(Player player, string weapon) =>
        new
        {
            @event = "grenade_thrown",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            round_number = Game.GetRoundNumber(),
            round_time = Game.GetRoundTime(),
            player = ToGet5Player(player),
            weapon = ToGet5Weapon(weapon),
        };

    public object OnPlayerDeath(
        Player player,
        Player? attacker,
        Player? assister,
        string weapon,
        bool isKilledByBomb,
        bool isHeadshot,
        bool isThruSmoke,
        int isPenetrated,
        bool isAttackerBlind,
        bool isNoScope,
        bool isSuicide,
        bool isFriendlyFire,
        bool isFlashAssist
    ) =>
        new
        {
            @event = "player_death",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            round_number = Game.GetRoundNumber(),
            round_time = Game.GetRoundTime(),
            player = ToGet5Player(player),
            weapon = ToGet5Weapon(weapon),
            bomb = isKilledByBomb,
            headshot = isHeadshot,
            thru_smoke = isThruSmoke,
            penetrated = isPenetrated,
            attacker_blind = isAttackerBlind,
            no_scope = isNoScope,
            suicide = isSuicide,
            friendly_fire = isFriendlyFire,
            attacker = attacker != null ? ToGet5Player(attacker) : null,
            assist = assister != null
                ? new
                {
                    player = ToGet5Player(assister),
                    friendly_fire = player.Team == assister.Team,
                    flash_assist = isFlashAssist,
                }
                : null,
        };

    public object OnHEGrenadeDetonated(
        int roundNumber,
        long roundTime,
        Player player,
        string weapon,
        UtilityVictim victims
    ) =>
        new
        {
            @event = "hegrenade_detonated",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            round_number = roundNumber,
            round_time = roundTime,
            player = ToGet5Player(player),
            weapon = ToGet5Weapon(weapon),
            victims = victims
                .Values.Select(victim => new
                {
                    player = ToGet5Player(victim.Player),
                    killed = victim.Killed,
                    damage = victim.Damage,
                })
                .ToList(),
            damage_enemies = victims
                .Values.Where(v => v.Player.Team != player.Team)
                .Select(v => v.Damage)
                .Sum(),
            damage_friendlies = victims
                .Values.Where(v => v.Player.Team == player.Team)
                .Select(v => v.Damage)
                .Sum(),
        };

    public object OnMolotovDetonated(
        int roundNumber,
        long roundTime,
        Player player,
        string weapon,
        UtilityVictim victims
    ) =>
        new
        {
            @event = "molotov_detonated",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            round_number = roundNumber,
            round_time = roundTime,
            player = ToGet5Player(player),
            weapon = ToGet5Weapon(weapon),
            victims = victims
                .Values.Select(victim => new
                {
                    player = ToGet5Player(victim.Player),
                    killed = victim.Killed,
                    damage = victim.Damage,
                })
                .ToList(),
            damage_enemies = victims
                .Values.Where(v => v.Player.Team != player.Team)
                .Select(v => v.Damage)
                .Sum(),
            damage_friendlies = victims
                .Values.Where(v => v.Player.Team == player.Team)
                .Select(v => v.Damage)
                .Sum(),
        };

    public object OnFlashbangDetonated(
        int roundNumber,
        long roundTime,
        Player player,
        string weapon,
        UtilityVictim victims
    ) =>
        new
        {
            @event = "flashbang_detonated",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            round_number = roundNumber,
            round_time = roundTime,
            player = ToGet5Player(player),
            weapon = ToGet5Weapon(weapon),
            victims = victims
                .Values.Select(victim => new
                {
                    player = ToGet5Player(victim.Player),
                    friendly_fire = victim.FriendlyFire,
                    blind_duration = victim.BindDuration,
                })
                .ToList(),
        };

    public object OnSmokeGrenadeDetonated(
        int roundNumber,
        long roundTime,
        Player player,
        string weapon,
        bool didExtingishMolotovs
    ) =>
        new
        {
            @event = "smokegrenade_detonated",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            round_number = roundNumber,
            round_time = roundTime,
            player = ToGet5Player(player),
            weapon = ToGet5Weapon(weapon),
            extinguished_molotov = didExtingishMolotovs,
        };

    public object OnDecoyStarted(Player player, string weapon) =>
        new
        {
            @event = "decoygrenade_started",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            round_number = Game.GetRoundNumber(),
            round_time = Game.GetRoundTime(),
            player = ToGet5Player(player),
            weapon = ToGet5Weapon(weapon),
        };

    public object OnBombPlanted(Player player, int? site) =>
        new
        {
            @event = "bomb_planted",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            round_number = Game.GetRoundNumber(),
            round_time = Game.GetRoundTime(),
            player = ToGet5Player(player),
            site = ToGet5Site(site),
        };

    public object OnBombDefused(Player player, int? site, long bombTimeRemaining) =>
        new
        {
            @event = "bomb_defused",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            round_number = Game.GetRoundNumber(),
            round_time = Game.GetRoundTime(),
            player = ToGet5Player(player),
            site = ToGet5Site(site),
            bomb_time_remaining = bombTimeRemaining,
        };

    public object OnBombExploded(int? site) =>
        new
        {
            @event = "bomb_exploded",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            round_number = Game.GetRoundNumber(),
            round_time = Game.GetRoundTime(),
            site = ToGet5Site(site),
        };

    public object OnPlayerConnected(IPlayer player, string ipAddress) =>
        new
        {
            @event = "player_connect",
            matchid = Game.Id,
            player = ToGet5Player(player),
            ip_address = ipAddress,
        };

    public object OnPlayerDisconnected(IPlayer player) =>
        new
        {
            @event = "player_disconnect",
            matchid = Game.Id,
            player = ToGet5Player(player),
        };

    public object OnPlayerSay(IPlayer player, string command, string message) =>
        new
        {
            @event = "player_say",
            matchid = Game.Id,
            map_number = Game.GetMapIndex(),
            round_number = Game.GetRoundNumber(),
            round_time = Game.GetRoundTime(),
            player = ToGet5Player(player),
            command,
            message,
        };

    private string ToGet5SideString(Team team) => team == Team.T ? "t" : "ct";

    private string ToGet5TeamString(PlayerTeam? team) =>
        team != null ? $"team{team.Index + 1}" : "spec";

    private object ToGet5StatsTeam(PlayerTeam team) =>
        new
        {
            id = team.Id,
            name = team.Name,
            series_score = team.SeriesScore,
            score = team.Score,
            score_ct = team.Stats.ScoreCT,
            score_t = team.Stats.ScoreT,
            side = ToGet5SideString(team.CurrentTeam),
            starting_side = ToGet5SideString(team.StartingTeam),
            players = team
                .Players.Select(player => new
                {
                    steamid = player.SteamID.ToString(),
                    name = player.Name,
                    stats = player.Stats,
                    ping = player.Handle?.Controller.Ping,
                })
                .ToList(),
        };

    private object? ToGet5Winner(PlayerTeam? team) =>
        team != null
            ? new { side = ToGet5SideString(team.CurrentTeam), team = ToGet5TeamString(team) }
            : null;

    private object ToGet5Player(Player player) =>
        new
        {
            steamid = player.SteamID.ToString(),
            name = player.Name,
            user_id = player.Handle?.UserID,
            side = ToGet5SideString(player.Team.CurrentTeam),
            is_bot = player.Handle?.IsFakeClient ?? false,
            ping = player.Handle?.Controller.Ping,
        };

    private object ToGet5Player(IPlayer player) =>
        new
        {
            steamid = player.Controller.SteamID.ToString(),
            name = player.Controller.PlayerName,
            user_id = player.UserID,
            side = ToGet5SideString(player.Controller.Team),
            is_bot = player.IsFakeClient,
            ping = player.Controller.Ping,
        };

    private object ToGet5Weapon(string weapon) =>
        new { name = weapon.Replace("weapon_", ""), id = ItemHelper.GetItemDefIndex(weapon) };

    private string ToGet5Site(int? site) =>
        site switch
        {
            1 => "a",
            2 => "b",
            _ => "none",
        };
}

public class Get5PlayerSet
{
    public Dictionary<ulong, string>? AsDictionary { get; set; }
    public List<ulong>? AsList { get; set; }

    public Dictionary<ulong, string>? Get()
    {
        if (AsDictionary != null)
            return AsDictionary;

        if (AsList != null)
        {
            var dictionary = new Dictionary<ulong, string>(AsList.Count);
            foreach (var steamId in AsList)
                dictionary[steamId] = "";
        }

        return null;
    }
}

public class Get5PlayerSetJsonConverter : JsonConverter<Get5PlayerSet>
{
    public override Get5PlayerSet? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
                var dictionary = JsonSerializer.Deserialize<Dictionary<ulong, string>>(
                    ref reader,
                    options
                );
                return new Get5PlayerSet { AsDictionary = dictionary };

            case JsonTokenType.StartArray:
                var list = JsonSerializer.Deserialize<List<ulong>>(ref reader, options);
                return new Get5PlayerSet { AsList = list };
        }

        throw new JsonException("Expected an array or an object for Get5PlayerSet.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        Get5PlayerSet value,
        JsonSerializerOptions options
    )
    {
        if (value.AsDictionary != null)
        {
            JsonSerializer.Serialize(writer, value.AsDictionary, options);
        }
        else if (value.AsList != null)
        {
            JsonSerializer.Serialize(writer, value.AsList, options);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public class Get5MapListFromList
{
    [JsonPropertyName("fromfile")]
    public required string Fromfile { get; set; }
}

public class Get5Maplist
{
    public List<string>? AsList { get; set; }
    public Get5MapListFromList? AsObject { get; set; }

    public List<string>? Get()
    {
        try
        {
            if (AsList != null)
                return AsList;
            var name = AsObject?.Fromfile ?? "";
            if (!name.EndsWith(".json"))
                name += ".json";
            var filepath = Swiftly.Core.GetConfigPath($"/{name}");
            if (!File.Exists(filepath))
                filepath = Swiftly.Core.GetCSGOPath(filepath);
            return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(filepath));
        }
        catch (Exception ex)
        {
            Swiftly.Core.Logger.LogWarning($"Error reading match map list file: {ex.Message}");
            return null;
        }
    }
}

public class Get5MaplistJsonConverter : JsonConverter<Get5Maplist>
{
    public override Get5Maplist? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
                var @object = JsonSerializer.Deserialize<Get5MapListFromList>(ref reader, options);
                return new Get5Maplist { AsObject = @object };

            case JsonTokenType.StartArray:
                var list = JsonSerializer.Deserialize<List<string>>(ref reader, options);
                return new Get5Maplist { AsList = list };
        }

        throw new JsonException("Expected an array or an object for Get5Map.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        Get5Maplist value,
        JsonSerializerOptions options
    )
    {
        if (value.AsObject != null)
        {
            JsonSerializer.Serialize(writer, value.AsObject, options);
        }
        else if (value.AsList != null)
        {
            JsonSerializer.Serialize(writer, value.AsList, options);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public class Get5MatchTeam
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("players")]
    [JsonConverter(typeof(Get5PlayerSetJsonConverter))]
    public required Get5PlayerSet Players { get; set; }

    [JsonPropertyName("coaches")]
    [JsonConverter(typeof(Get5PlayerSetJsonConverter))]
    public Get5PlayerSet? Coaches { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    [JsonPropertyName("flag")]
    public string? Flag { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    [JsonPropertyName("series_score")]
    public int? SeriesScore { get; set; }

    [JsonPropertyName("matchtext")]
    public string? Matchtext { get; set; }

    [JsonPropertyName("fromfile")]
    public string? Fromfile { get; set; }

    [JsonPropertyName("leaderid")]
    public string? Leaderid { get; set; }

    public Get5MatchTeam? Get()
    {
        try
        {
            if (Fromfile == null)
                return this;
            var name = Fromfile ?? "";
            if (!name.EndsWith(".json"))
                name += ".json";
            var filepath = Swiftly.Core.GetConfigPath($"/{name}");
            if (!File.Exists(filepath))
                filepath = Swiftly.Core.GetCSGOPath(filepath);
            return JsonSerializer.Deserialize<Get5MatchTeam>(File.ReadAllText(filepath));
        }
        catch (Exception ex)
        {
            Swiftly.Core.Logger.LogWarning($"Error reading match team file: {ex.Message}");
            return null;
        }
    }
}

public class Get5SpectatorTeam
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("players")]
    [JsonConverter(typeof(Get5PlayerSetJsonConverter))]
    public Get5PlayerSet? Players { get; set; }

    [JsonPropertyName("fromfile")]
    public string? Fromfile { get; set; }
}

public class Get5Match
{
    [JsonPropertyName("match_title")]
    public string? MatchTitle { get; set; }

    [JsonPropertyName("matchid")]
    public string? Matchid { get; set; }

    [JsonPropertyName("clinch_series")]
    public bool? ClinchSeries { get; set; }

    [JsonPropertyName("num_maps")]
    public int? NumMaps { get; set; }

    [JsonPropertyName("scrim")]
    public bool? Scrim { get; set; }

    [JsonPropertyName("wingman")]
    public bool? Wingman { get; set; }

    [JsonPropertyName("players_per_team")]
    public int? PlayersPerTeam { get; set; }

    [JsonPropertyName("coaches_per_team")]
    public int? CoachesPerTeam { get; set; }

    [JsonPropertyName("coaches_must_ready")]
    public bool? CoachesMustReady { get; set; }

    [JsonPropertyName("min_players_to_ready")]
    public int? MinPlayersToReady { get; set; }

    [JsonPropertyName("min_spectators_to_ready")]
    public int? MinSpectatorsToReady { get; set; }

    [JsonPropertyName("skip_veto")]
    public bool? SkipVeto { get; set; }

    [JsonPropertyName("veto_first")]
    public string? VetoFirst { get; set; }

    [JsonPropertyName("veto_mode")]
    public List<string>? VetoMode { get; set; }

    [JsonPropertyName("side_type")]
    public string? SideType { get; set; }

    [JsonPropertyName("map_sides")]
    public List<string>? MapSides { get; set; }

    [JsonPropertyName("spectators")]
    public Get5SpectatorTeam? Spectators { get; set; }

    [JsonPropertyName("maplist")]
    [JsonConverter(typeof(Get5MaplistJsonConverter))]
    public required Get5Maplist Maplist { get; set; }

    [JsonPropertyName("favored_percentage_team1")]
    public int? FavoredPercentageTeam1 { get; set; }

    [JsonPropertyName("favored_percentage_text")]
    public string? FavoredPercentageText { get; set; }

    [JsonPropertyName("team1")]
    public required Get5MatchTeam team1 { get; set; }

    [JsonPropertyName("team2")]
    public Get5MatchTeam? Team2 { get; set; }

    [JsonPropertyName("cvars")]
    public Dictionary<string, JsonElement>? Cvars { get; set; }

    public static Get5MatchFile Read(string name)
    {
        try
        {
            if (!name.EndsWith(".json"))
                name += ".json";
            var filepath = Swiftly.Core.GetConfigPath($"/{name}");
            if (!File.Exists(filepath))
                filepath = Swiftly.Core.GetCSGOPath(filepath);
            return new Get5MatchFile
            {
                Path = filepath,
                Contents = JsonSerializer.Deserialize<Get5Match>(File.ReadAllText(filepath)),
            };
        }
        catch (Exception ex)
        {
            Swiftly.Core.Logger.LogWarning($"Error reading match file: {ex.Message}");
            return new Get5MatchFile { Error = ex.Message };
        }
    }
}

public class Get5MatchFile
{
    public string? Path;
    public Get5Match? Contents = null;
    public string? Error = null;
}
