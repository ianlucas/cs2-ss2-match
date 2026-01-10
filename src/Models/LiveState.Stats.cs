/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Match.Get5.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
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
    private bool _hadFirstDeath = false;
    private bool _hadFirstKill = false;

    // KAST
    private readonly Dictionary<ulong, bool> _playerDied = [];
    private readonly Dictionary<ulong, bool> _playerKilledOrAssistedOrTradedKill = [];
    private readonly Dictionary<ulong, bool> _playerPlayedRound = [];

    public HookResult Stats_OnRoundStart(EventRoundStart @event)
    {
        _isTeamClutching.Clear();
        _roundClutchingCount.Clear();
        _playerKilledBy.Clear();
        _hadFirstDeath = false;
        _hadFirstKill = false;
        _playerDied.Clear();
        _playerKilledOrAssistedOrTradedKill.Clear();
        _playerPlayedRound.Clear();
        foreach (var player in Game.Teams.SelectMany(t => t.Players))
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
        var playerState = @event.UserIdPlayer.GetState();
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
        var attackerState = Swiftly.Core.PlayerManager.GetPlayer(@event.Attacker)?.GetState();
        var victimState = @event.UserIdPlayer.GetState();
        if (attackerState != null && victimState != null)
        {
            var friendlyFire = attackerState.Team == victimState.Team;
            if (@event.BlindDuration > 2.5f)
                if (friendlyFire)
                    attackerState.Stats.FriendliesFlashed += 1;
                else
                    attackerState.Stats.EnemiesFlashed += 1;

            var entityId = (uint)@event.EntityID;
            var victims = _utilityVictims.TryGetValue(entityId, out var v) ? v : [];
            var theVictim = victims.TryGetValue(victimState.SteamID, out var p)
                ? p
                : new(victimState);
            theVictim.FriendlyFire = friendlyFire;
            theVictim.BindDuration = @event.BlindDuration;
            victims[victimState.SteamID] = theVictim;
            _utilityVictims[entityId] = victims;
        }
        return HookResult.Continue;
    }

    public void Stats_OnPlayerHurt(EventPlayerHurt @event, int damage)
    {
        var attackerState = Swiftly.Core.PlayerManager.GetPlayer(@event.Attacker)?.GetState();
        var victimState = @event.UserIdPlayer.GetState();
        if (attackerState != null && victimState != null && attackerState != victimState)
        {
            attackerState.Stats.Damage += damage;
            if (ItemHelper.IsUtilityDesignerName(@event.Weapon))
                attackerState.Stats.UtilDamage += damage;
        }
        if (attackerState != null && attackerState != victimState && @event.Weapon != "world")
        {
            var weaponStats = attackerState.Stats.GetWeaponStats(
                ItemHelper.NormalizeDesignerName(@event.Weapon, attackerState.Handle?.Controller)
            );
            weaponStats.Hits += 1;
            weaponStats.Damage += damage;
            switch ((HitGroup_t)@event.HitGroup)
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
    }

    public HookResult Stats_OnPlayerDeath(EventPlayerDeath @event)
    {
        var attackerState = Swiftly.Core.PlayerManager.GetPlayer(@event.Attacker)?.GetState();
        var victimState = @event.UserIdPlayer.GetState();
        if (victimState == null)
            return HookResult.Continue;
        var victimTeam = victimState.Team.CurrentTeam;
        if (
            !_isTeamClutching.ContainsKey(victimTeam)
            && Swiftly.Core.PlayerManager.GetAliveInTeam(victimTeam).Count() == 1
        )
        {
            _isTeamClutching[victimTeam] = true;
            var clutcher = Swiftly.Core.PlayerManager.GetAliveInTeam(victimTeam).FirstOrDefault();
            if (clutcher != null)
                _roundClutchingCount[clutcher.SteamID] = Swiftly
                    .Core.PlayerManager.GetAliveInTeam(TeamHelper.ToggleTeam(victimTeam))
                    .Count();
        }
        var killedByBomb = @event.Weapon == "planted_c4";
        var killedWithKnife = ItemHelper.IsMeleeDesignerName(@event.Weapon);
        var isSuicide = (attackerState == null || attackerState == victimState) && !killedByBomb;
        var headshot = @event.Headshot;
        var normalizedWeapon = ItemHelper.NormalizeDesignerName(
            @event.Weapon,
            attackerState?.Handle?.Controller
        );
        PlayerState? assisterState = null;
        victimState.Stats.Deaths += 1;
        _playerDied[victimState.SteamID] = true;
        if (!_hadFirstDeath)
        {
            _hadFirstDeath = true;
            if (victimTeam == Team.T)
                victimState.Stats.FirstDeathsT += 1;
            else
                victimState.Stats.FirstDeathsCT += 1;
        }
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
                if (!_hadFirstKill)
                {
                    _hadFirstKill = true;
                    if (attackerTeam == Team.T)
                        attackerState.Stats.FirstKillsT += 1;
                    else
                        attackerState.Stats.FirstKillsCT += 1;
                }
                _roundKills[attackerState.SteamID] += 1;
                _playerKilledBy[victimState.SteamID] = (
                    victimTeam,
                    attackerState.SteamID,
                    attackerTeam,
                    TimeHelper.Now()
                );
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
                        attackerState.Stats.TradeKills += 1;
                        _playerKilledOrAssistedOrTradedKill[aVictim] = true;
                    }
                attackerState.Stats.Kills += 1;
                _playerKilledOrAssistedOrTradedKill[attackerState.SteamID] = true;
                if (headshot)
                    attackerState.Stats.HeadshotKills += 1;
                if (killedWithKnife)
                    attackerState.Stats.KnifeKills += 1;
                assisterState = Swiftly.Core.PlayerManager.GetPlayer(@event.Assister)?.GetState();
                if (assisterState != null)
                {
                    var friendlyFire = assisterState.Team == victimState.Team;
                    var assistedFlash = @event.AssistedFlash;
                    if (!friendlyFire)
                        if (assistedFlash)
                            assisterState.Stats.FlashbangAssists += 1;
                        else
                        {
                            assisterState.Stats.Assists += 1;
                            _playerKilledOrAssistedOrTradedKill[assisterState.SteamID] = true;
                        }
                }
            }
        }
        Game.SendEvent(
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
        var playerState = @event.UserIdPlayer.GetState();
        if (playerState != null)
        {
            playerState.Stats.BombPlants += 1;
            Game.SendEvent(OnBombPlantedEvent.Create(playerState, site: _lastPlantedBombZone));
        }
        return HookResult.Continue;
    }

    public HookResult Stats_OnBombDefused(EventBombDefused @event)
    {
        var playerState = @event.UserIdPlayer.GetState();
        if (playerState != null)
        {
            playerState.Stats.BombDefuses += 1;

            var timeToDefuse = TimeHelper.Now() - _bombPlantedAt;
            var c4Timer = (Swiftly.Core.ConVar.Find<int>("mp_c4timer")?.Value ?? 0) * 1000;
            var bombTimeRemaining = c4Timer - timeToDefuse;
            if (bombTimeRemaining < 0)
            {
                Game.Log($"bombTimeRemaining={bombTimeRemaining} is negative!");
                bombTimeRemaining = 0;
            }
            Game.SendEvent(
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
        var playerState = @event.UserIdPlayer.GetState();
        if (playerState != null)
        {
            playerState.Stats.MVPs += 1;
            Game.SendEvent(OnPlayerBecameMVPEvent.Create(playerState, reason: @event.Reason));
        }
        return HookResult.Continue;
    }

    public HookResult Stats_OnRoundEnd(EventRoundEnd @event)
    {
        var gameRules = Swiftly.Core.EntitySystem.GetGameRules();
        if (gameRules == null)
            return HookResult.Continue;
        var winner = (Team)@event.Winner;
        var winnerTeam = Game.Teams.FirstOrDefault(t => t.CurrentTeam == winner);
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
        foreach (var team in Game.Teams)
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
        Game.SendEvent(OnRoundEndEvent.Create(winner: winnerTeam, reason: @event.Reason));
        Game.SendEvent(OnRoundStatsUpdatedEvent.Create());
        return HookResult.Continue;
    }
}
