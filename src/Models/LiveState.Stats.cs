/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Match.Get5.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Match;

public partial class LiveState
{
    private readonly Dictionary<int, List<(PlayerState, PlayerStats)>> _statsBackup = [];
    private readonly Dictionary<int, List<(PlayerTeam, TeamStats)>> _teamStatsBackup = [];
    private readonly Dictionary<Team, bool> _isTeamClutching = [];
    private readonly Dictionary<ulong, int> _roundClutchingCount = [];
    private readonly Dictionary<ulong, int> _roundKills = [];
    private readonly Dictionary<ulong, (Team, ulong, Team, long)> _playerKilledBy = [];

    // Last weapon that damaged each victim, resolved from the inflictor. Needed because
    // `player_death.weapon` reports fire kills as the raw `inferno` entity name, never
    // distinguishing molotov from incendiary.
    private readonly Dictionary<ulong, string> _lastDamageWeapon = [];

    // Deduplicates damage ticks into hits: shotgun pellets land as one call per pellet in the same
    // server tick, and a single grenade or inferno damages the same victim across many ticks.
    private readonly Dictionary<(ulong, ulong, string), int> _lastHitToken = [];
    private bool _hadOpeningDuel = false;

    // `mp_backup_restore_load_file` fires a real `round_end` for the aborted round; `Stats_OnRoundEnd`
    // must skip it while this is set. Cleared by the `round_start` that follows the restore.
    private bool _isRestoring = false;

    // KAST
    private readonly Dictionary<ulong, bool> _playerDied = [];
    private readonly Dictionary<ulong, bool> _playerKilledOrAssistedOrTradedKill = [];
    private readonly Dictionary<ulong, bool> _playerPlayedRound = [];

    public HookResult Stats_OnRoundStart(EventRoundStart @event)
    {
        _isRestoring = false;
        _isTeamClutching.Clear();
        _roundClutchingCount.Clear();
        _playerKilledBy.Clear();
        _lastDamageWeapon.Clear();
        _lastHitToken.Clear();
        _hadOpeningDuel = false;
        _playerDied.Clear();
        _playerKilledOrAssistedOrTradedKill.Clear();
        _playerPlayedRound.Clear();
        foreach (var player in Rules.GetAllPlayers())
        {
            _roundKills[player.SteamID] = 0;
            if (player.Handle != null)
            {
                player.Stats.RoundsPlayed += 1;
                _playerPlayedRound[player.SteamID] = true;
            }
        }
        return HookResult.Continue;
    }

    public HookResult Stats_OnWeaponFire(EventWeaponFire @event)
    {
        var playerState = @event.UserIdPlayer?.GetState();
        if (
            playerState != null
            && @event.Weapon != "world"
            && !ItemHelper.IsMeleeDesignerName(@event.Weapon)
        )
            playerState?.Stats
                .GetWeaponStats(
                    ItemHelper.NormalizeDesignerName(@event.Weapon, playerState.Handle?.Controller)
                )
                .Shots += 1;
        return HookResult.Continue;
    }

    public HookResult Stats_OnPlayerBlind(EventPlayerBlind @event)
    {
        var attackerState = Runtime.Core.PlayerManager.GetPlayer(@event.Attacker)?.GetState();
        var victimState = @event.UserIdPlayer?.GetState();
        if (attackerState != null && victimState != null)
        {
            var friendlyFire = attackerState.Team == victimState.Team;
            if (@event.BlindDuration > 2.5f && attackerState != victimState)
                if (friendlyFire)
                    attackerState.Stats.FriendliesFlashed += 1;
                else
                    attackerState.Stats.EnemiesFlashed += 1;

            var entityId = (uint)@event.EntityID;
            if (_thrownUtilities.TryGetValue(entityId, out var utility))
            {
                var theVictim = utility.GetValueOrDefault(victimState.SteamID, new(victimState));
                theVictim.FriendlyFire = friendlyFire;
                theVictim.BlindDuration = @event.BlindDuration;
                utility[victimState.SteamID] = theVictim;
            }
        }
        return HookResult.Continue;
    }

    public HookResult Stats_OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var playerState = @event.UserIdPlayer?.GetState();
        if (playerState != null)
            _playerDied[playerState.SteamID] = true;
        return HookResult.Continue;
    }

    public HookResult Stats_OnPlayerDeath(EventPlayerDeath @event)
    {
        var attacker = Runtime.Core.PlayerManager.GetPlayer(@event.Attacker);
        var isBotAttacker = attacker?.IsFakeClient == true;
        var attackerState = isBotAttacker ? null : attacker?.GetState();
        var victimState = @event.UserIdPlayer?.GetState();
        if (victimState == null)
            return HookResult.Continue;
        var victimTeam = victimState.Team.CurrentTeam;
        if (!_isTeamClutching.ContainsKey(victimTeam))
        {
            var aliveTeammates = Runtime.Core.EntitySystem.GetAlivePawnsInTeam(victimTeam).ToList();
            if (aliveTeammates.Count == 1)
            {
                _isTeamClutching[victimTeam] = true;
                var clutcherState = aliveTeammates[0]
                    .OriginalController.Value?.As<CCSPlayerController>()
                    .GetState();
                if (clutcherState != null)
                    _roundClutchingCount[clutcherState.SteamID] = Runtime
                        .Core.EntitySystem.GetAlivePawnsInTeam(victimTeam.Toggle())
                        .Count();
            }
        }
        var killedByBomb = @event.Weapon == "planted_c4";
        var killedWithKnife = ItemHelper.IsMeleeDesignerName(@event.Weapon);
        var isSuicide =
            (attacker == null || attacker.SteamID == victimState.SteamID)
            && !killedByBomb
            && !isBotAttacker;
        var headshot = @event.Headshot;
        var eventWeapon = @event.Weapon;
        if (
            eventWeapon == "inferno"
            && _lastDamageWeapon.TryGetValue(victimState.SteamID, out var lastDamageWeapon)
        )
            eventWeapon = lastDamageWeapon;
        var normalizedWeapon = ItemHelper.NormalizeDesignerName(eventWeapon);
        var activeWeapon = attackerState
            ?.Handle
            ?.Controller
            .PlayerPawn
            .Value
            ?.WeaponServices
            ?.ActiveWeapon
            .Value;
        Runtime.Log(
            $"player_death weapon={@event.Weapon} normalized={normalizedWeapon} active={(activeWeapon != null ? ItemHelper.GetItemDesignerName(activeWeapon.AttributeManager.Item.ItemDefinitionIndex) : "none")}"
        );
        var assisterState = Runtime.Core.PlayerManager.GetPlayer(@event.Assister)?.GetState();
        if (assisterState != null && assisterState.Team != victimState.Team)
            if (@event.AssistedFlash)
                assisterState.Stats.FlashbangAssists += 1;
            else
            {
                assisterState.Stats.Assists += 1;
                _playerKilledOrAssistedOrTradedKill[assisterState.SteamID] = true;
            }
        victimState.Stats.Deaths += 1;
        _playerDied[victimState.SteamID] = true;
        if (isSuicide)
            victimState.Stats.Suicides += 1;
        else if (!killedByBomb)
        {
            if (attackerState?.Team == victimState.Team)
                attackerState.Stats.Teamkills += 1;
            else if (attackerState != null)
            {
                var weaponStats = attackerState.Stats.GetWeaponStats(normalizedWeapon);
                weaponStats.Kills += 1;
                if (headshot)
                    weaponStats.Headshots += 1;
                var attackerTeam = attackerState.Team.CurrentTeam;
                if (!_hadOpeningDuel)
                {
                    _hadOpeningDuel = true;
                    if (attackerTeam == Team.T)
                        attackerState.Stats.FirstKillsT += 1;
                    else
                        attackerState.Stats.FirstKillsCT += 1;
                    if (victimTeam == Team.T)
                        victimState.Stats.FirstDeathsT += 1;
                    else
                        victimState.Stats.FirstDeathsCT += 1;
                }
                _roundKills[attackerState.SteamID] += 1;
                _playerKilledBy[victimState.SteamID] = (
                    victimTeam,
                    attackerState.SteamID,
                    attackerTeam,
                    TimeHelper.Now()
                );
                var isTradeKill = false;
                foreach (
                    var (
                        aVictim,
                        (theVictimTeam, theVictimAttacker, theVictimAttackerTeam, theVictimKilledAt)
                    ) in _playerKilledBy
                )
                    if (
                        attackerTeam == theVictimTeam
                        && victimState.SteamID == theVictimAttacker
                        && victimTeam == theVictimAttackerTeam
                        && (TimeHelper.Now() - theVictimKilledAt) <= 2_000
                    )
                    {
                        isTradeKill = true;
                        _playerKilledOrAssistedOrTradedKill[aVictim] = true;
                    }
                if (isTradeKill)
                    attackerState.Stats.TradeKills += 1;
                attackerState.Stats.Kills += 1;
                _playerKilledOrAssistedOrTradedKill[attackerState.SteamID] = true;
                if (headshot)
                    attackerState.Stats.HeadshotKills += 1;
                if (killedWithKnife)
                    attackerState.Stats.KnifeKills += 1;
            }
        }
        Rules.SendEvent(
            OnPlayerDeathEvent.Create(
                player: victimState,
                attackerState,
                assisterState,
                weapon: normalizedWeapon,
                isKilledByBomb: killedByBomb,
                isHeadshot: headshot,
                isThruSmoke: @event.ThruSmoke,
                isPenetrated: @event.Penetrated,
                isAttackerBlind: @event.AttackerBlind,
                isNoScope: @event.NoScope,
                isSuicide: isSuicide,
                isFriendlyFire: victimState.Team == attackerState?.Team,
                isFlashAssist: @event.AssistedFlash
            )
        );
        return HookResult.Continue;
    }

    public HookResult Stats_OnBombPlanted(EventBombPlanted @event)
    {
        _bombPlantedAt = TimeHelper.Now();
        _lastPlantedBombZone = @event.UserIdPawn.WhichBombZone;
        var playerState = @event.UserIdPlayer?.GetState();
        if (playerState != null)
        {
            playerState.Stats.BombPlants += 1;
            Rules.SendEvent(OnBombPlantedEvent.Create(playerState, site: _lastPlantedBombZone));
        }
        return HookResult.Continue;
    }

    public HookResult Stats_OnBombDefused(EventBombDefused @event)
    {
        var playerState = @event.UserIdPlayer?.GetState();
        if (playerState != null)
        {
            playerState.Stats.BombDefuses += 1;
            var plantedC4 = Runtime
                .Core.EntitySystem.GetAllEntitiesByDesignerName<CPlantedC4>("planted_c4")
                .FirstOrDefault();
            long bombTimeRemaining;
            if (plantedC4 != null)
                bombTimeRemaining = (long)(
                    (plantedC4.C4Blow.Value - Runtime.Core.Engine.GlobalVars.CurrentTime) * 1000
                );
            else
            {
                Runtime.Log("No planted_c4 entity found, falling back to wall clock.");
                var timeToDefuse = TimeHelper.Now() - _bombPlantedAt;
                var c4Timer = (Runtime.Core.ConVar.Find<int>("mp_c4timer")?.Value ?? 0) * 1000;
                bombTimeRemaining = c4Timer - timeToDefuse;
            }
            if (bombTimeRemaining < 0)
            {
                Runtime.Log($"bombTimeRemaining={bombTimeRemaining} is negative!");
                bombTimeRemaining = 0;
            }
            Rules.SendEvent(
                OnBombDefusedEvent.Create(
                    playerState,
                    site: _lastPlantedBombZone,
                    bombTimeRemaining
                )
            );
        }
        return HookResult.Continue;
    }

    public HookResult Stats_OnRoundMvp(EventRoundMvp @event)
    {
        var playerState = @event.UserIdPlayer?.GetState();
        if (playerState != null)
        {
            playerState.Stats.MVPs += 1;
            Rules.SendEvent(OnPlayerBecameMVPEvent.Create(playerState, reason: @event.Reason));
        }
        return HookResult.Continue;
    }

    public void Stats_OnTakeDamage_Alive(
        PlayerState attackerState,
        PlayerState victimState,
        string weaponDesignerName,
        int damage,
        HitGroup_t hitGroup,
        int hitToken
    )
    {
        if (ItemHelper.IsUtilityDesignerName(weaponDesignerName))
            attackerState.Stats.UtilDamage += damage;
        attackerState.Stats.Damage += damage;
        var weaponStats = attackerState.Stats.GetWeaponStats(
            ItemHelper.NormalizeDesignerName(weaponDesignerName, null)
        );
        weaponStats.Damage += damage;
        var hitKey = (attackerState.SteamID, victimState.SteamID, weaponDesignerName);
        if (_lastHitToken.TryGetValue(hitKey, out var lastToken) && lastToken == hitToken)
            return;
        _lastHitToken[hitKey] = hitToken;
        weaponStats.Hits += 1;
        switch (hitGroup)
        {
            case HitGroup_t.HITGROUP_HEAD:
                weaponStats.HeadHits += 1;
                break;
            case HitGroup_t.HITGROUP_NECK:
                weaponStats.NeckHits += 1;
                break;
            case HitGroup_t.HITGROUP_CHEST:
                weaponStats.ChestHits += 1;
                break;
            case HitGroup_t.HITGROUP_STOMACH:
                weaponStats.StomachHits += 1;
                break;
            case HitGroup_t.HITGROUP_LEFTARM:
                weaponStats.LeftArmHits += 1;
                break;
            case HitGroup_t.HITGROUP_RIGHTARM:
                weaponStats.RightArmHits += 1;
                break;
            case HitGroup_t.HITGROUP_LEFTLEG:
                weaponStats.LeftLegHits += 1;
                break;
            case HitGroup_t.HITGROUP_RIGHTLEG:
                weaponStats.RightLegHits += 1;
                break;
            case HitGroup_t.HITGROUP_GEAR:
                weaponStats.GearHits += 1;
                break;
        }
    }

    public HookResult Stats_OnRoundEnd(EventRoundEnd @event)
    {
        if (_isRestoring)
            return HookResult.Continue;
        // `Game_Commencing` is a full match reset, not a played round; and any `round_end` arriving
        // after the map result was recorded must not mutate it retroactively.
        if ((RoundEndReason)@event.Reason == RoundEndReason.GameCommencing)
            return HookResult.Continue;
        if (Rules.MapEndResult != null)
            return HookResult.Continue;
        var gameRules = Runtime.Core.EntitySystem.GetGameRules();
        if (gameRules == null)
            return HookResult.Continue;
        var winner = (Team)@event.Winner;
        var winnerTeam = Rules.Teams.FirstOrDefault(t => t.CurrentTeam == winner);
        switch (winnerTeam?.CurrentTeam)
        {
            case Team.T:
                winnerTeam.Stats.ScoreT += 1;
                break;
            case Team.CT:
                winnerTeam.Stats.ScoreCT += 1;
                break;
        }
        _statsBackup[gameRules.TotalRoundsPlayed] = [];
        _teamStatsBackup[gameRules.TotalRoundsPlayed] = [];
        foreach (var team in Rules.Teams)
        {
            _teamStatsBackup[gameRules.TotalRoundsPlayed].Add((team, team.Stats.Clone()));
            foreach (var player in team.Players)
            {
                if (player.Handle != null)
                    player.Stats.Score = player.Handle.Controller.Score;
                if (_roundKills.TryGetValue(player.SteamID, out var kills))
                    switch (kills)
                    {
                        case 1:
                            player.Stats.K1 += 1;
                            break;
                        case 2:
                            player.Stats.K2 += 1;
                            break;
                        case 3:
                            player.Stats.K3 += 1;
                            break;
                        case 4:
                            player.Stats.K4 += 1;
                            break;
                        case 5:
                            player.Stats.K5 += 1;
                            break;
                    }
                if (
                    player.Team.CurrentTeam == winner
                    && _roundClutchingCount.TryGetValue(player.SteamID, out var opponents)
                )
                    switch (opponents)
                    {
                        case 1:
                            player.Stats.V1 += 1;
                            break;
                        case 2:
                            player.Stats.V2 += 1;
                            break;
                        case 3:
                            player.Stats.V3 += 1;
                            break;
                        case 4:
                            player.Stats.V4 += 1;
                            break;
                        case 5:
                            player.Stats.V5 += 1;
                            break;
                    }

                if (_playerPlayedRound.ContainsKey(player.SteamID))
                    if (
                        _playerKilledOrAssistedOrTradedKill.ContainsKey(player.SteamID)
                        || !_playerDied.ContainsKey(player.SteamID)
                    )
                        player.Stats.KAST += 1;

                _statsBackup[gameRules.TotalRoundsPlayed].Add((player, player.Stats.Clone()));
            }
        }
        WriteStatsBackupToDisk(gameRules.TotalRoundsPlayed);
        Rules.SendEvent(OnRoundEndEvent.Create(winner: winnerTeam, reason: @event.Reason));
        Rules.SendEvent(OnRoundStatsUpdatedEvent.Create());
        return HookResult.Continue;
    }
}
