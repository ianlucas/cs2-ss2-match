/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Match.Get5.Events;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Match;

public partial class LiveState : BaseState
{
    public override string Name => "live";
    public static readonly List<string> PauseCmds = ["sw_pause", "sw_p", "sw_pausar"];
    public static readonly List<string> UnpauseCmds = ["sw_unpause", "sw_up", "sw_despausar"];
    public static readonly List<string> SurrenderCmds = ["sw_gg", "sw_desistir"];
    public long RoundStartedAt = 0;
    public int Round = -1;
    private bool _isForfeiting = false;
    private uint _lastThrownSmokegrenade = 0;
    private long _bombPlantedAt = 0;
    private int? _lastPlantedBombZone = null;
    private readonly Dictionary<ulong, int> _playerHealth = [];
    private readonly Dictionary<uint, UtilityVictim> _utilityVictims = [];
    private readonly Dictionary<uint, ThrownMolotov> _thrownMolotovs = [];

    // smokegrenade_detonate -> inferno_extinguish -> inferno_expire (always called)
    private readonly Dictionary<uint, bool> _didSmokeExtinguishMolotov = [];

    public override void Load()
    {
        RegisterCommand(SurrenderCmds, OnSurrenderCommand);
        RegisterCommand(PauseCmds, OnPauseCommand);
        RegisterCommand(UnpauseCmds, OnUnpauseCommand);
        RegisterCommand(["sw_restore"], OnRestoreCommand);
        HookCoreEvent<EventDelegates.OnTick>(OnTick);
        HookGameEvent<EventPlayerConnect>(OnPlayerConnect);
        HookGameEvent<EventPlayerConnectFull>(OnPlayerConnectFull);
        HookGameEvent<EventRoundPrestart>(OnRoundPrestart);
        HookGameEvent<EventRoundStart>(OnRoundStart);
        HookGameEvent<EventRoundStart>(Stats_OnRoundStart);
        HookGameEvent<EventWeaponFire>(Stats_OnWeaponFire);
        HookGameEvent<EventGrenadeThrown>(OnGrenadeThrown);
        HookGameEvent<EventDecoyStarted>(OnDecoyStarted);
        HookGameEvent<EventHegrenadeDetonate>(OnHegrenadeDetonate);
        HookGameEvent<EventSmokegrenadeDetonate>(OnSmokegrenadeDetonate);
        HookGameEvent<EventInfernoStartburn>(OnInfernoStartburn);
        HookGameEvent<EventInfernoExtinguish>(OnInfernoExtinguish);
        HookGameEvent<EventInfernoExpire>(OnInfernoExpire);
        HookGameEvent<EventFlashbangDetonate>(OnFlashbangDetonate);
        HookGameEvent<EventPlayerBlind>(Stats_OnPlayerBlind);
        HookCoreEvent<EventDelegates.OnEntityTakeDamage>(OnEntityTakeDamage);
        HookGameEvent<EventPlayerHurt>(OnPlayerHurt);
        HookGameEvent<EventPlayerDeath>(Stats_OnPlayerDeath);
        HookGameEvent<EventBombPlanted>(Stats_OnBombPlanted);
        HookGameEvent<EventBombDefused>(Stats_OnBombDefused);
        HookGameEvent<EventBombExploded>(OnBombExploded);
        HookGameEvent<EventRoundMvp>(Stats_OnRoundMvp);
        HookGameEvent<EventRoundEnd>(OnRoundEndPre, HookMode.Pre);
        HookGameEvent<EventRoundEnd>(Stats_OnRoundEnd);
        HookGameEvent<EventCsWinPanelMatch>(OnCsWinPanelMatch);
        HookGameEvent<EventPlayerDisconnect>(OnPlayerDisconnect);
        Swiftly.Log("Executing live match configuration");
        Game.SendEvent(OnGoingLiveEvent.Create());
        Config.ExecLive(
            maxRounds: ConVars.MaxRounds.Value,
            otMaxRounds: ConVars.OtMaxRounds.Value,
            isFriendlyPause: ConVars.IsFriendlyPause.Value,
            backupPath: Game.BackupPrefix
        );
        var localize = Swiftly.Core.Localizer;
        Swiftly.Core.PlayerManager.SendChatRepeat(localize["match.live", Game.GetChatPrefix()]);
        Swiftly.Core.PlayerManager.SendChat(
            localize["match.live_disclaimer", Game.GetChatPrefix()]
        );
        foreach (var team in Game.Teams)
            team.IsSurrended = false;
        if (!ConVars.IsKnifeRoundEnabled.Value)
            Cstv.Record(Game.DemoFilename);
        Swiftly.Core.PlayerManager.RemovePlayerClans();
        TryForfeitMatch();
    }

    public void OnTick()
    {
        CheckPauseEvents();
        if (ConVars.ServerGraphicUrl.Value != "")
            foreach (var player in Game.Teams.SelectMany(t => t.Players))
            {
                var deathTime = player.Handle?.PlayerPawn?.DeathTime?.Value;
                if (
                    Swiftly.Core.Engine.GlobalVars.CurrentTime - deathTime
                    < ConVars.ServerGraphicDuration.Value
                )
                    player.Handle?.SendCenterHTML($"<img src='{ConVars.ServerGraphicUrl.Value}'>");
            }
    }

    public HookResult OnRoundStart(EventRoundStart @event)
    {
        Round += 1;
        RoundStartedAt = TimeHelper.Now();
        _canSurrender = true;
        _playerHealth.Clear();
        foreach (var molotovEntityId in _thrownMolotovs.Keys)
            SendOnMolotovDetonatedEvent(molotovEntityId);
        _lastThrownSmokegrenade = 0;
        _utilityVictims.Clear();
        _didSmokeExtinguishMolotov.Clear();
        Game.SendEvent(OnRoundStartEvent.Create());
        return HookResult.Continue;
    }

    public HookResult OnGrenadeThrown(EventGrenadeThrown @event)
    {
        var playerState = @event.UserIdPlayer.GetState();
        if (playerState != null)
            Game.SendEvent(OnGrenadeThrownEvent.Create(playerState, weapon: @event.Weapon));
        return HookResult.Continue;
    }

    public HookResult OnDecoyStarted(EventDecoyStarted @event)
    {
        var playerState = @event.UserIdPlayer.GetState();
        if (playerState != null)
            Game.SendEvent(OnDecoyStartedEvent.Create(playerState, weapon: "weapon_decoy"));
        return HookResult.Continue;
    }

    public HookResult OnHegrenadeDetonate(EventHegrenadeDetonate @event)
    {
        var playerState = @event.UserIdPlayer.GetState();
        if (playerState != null)
        {
            var entityId = (uint)@event.EntityID;
            var roundNumber = Game.GetRoundNumber();
            var roundTime = Game.GetRoundTime();
            Swiftly.Core.Scheduler.DelayBySeconds(
                0.1f,
                () =>
                {
                    var victims = _utilityVictims.TryGetValue(entityId, out var v) ? v : [];
                    Game.SendEvent(
                        OnHEGrenadeDetonatedEvent.Create(
                            roundNumber,
                            roundTime,
                            playerState,
                            weapon: "weapon_hegrenade",
                            victims
                        )
                    );
                    _utilityVictims.Remove(entityId);
                }
            );
        }
        return HookResult.Continue;
    }

    public HookResult OnSmokegrenadeDetonate(EventSmokegrenadeDetonate @event)
    {
        var playerState = @event.UserIdPlayer.GetState();
        if (playerState != null)
        {
            var entityId = (uint)@event.EntityID;
            var roundNumber = Game.GetRoundNumber();
            var roundTime = Game.GetRoundTime();
            _lastThrownSmokegrenade = entityId;
            Swiftly.Core.Scheduler.DelayBySeconds(
                0.1f,
                () =>
                {
                    Game.SendEvent(
                        OnSmokeGrenadeDetonatedEvent.Create(
                            roundNumber,
                            roundTime,
                            playerState,
                            weapon: "weapon_smokegrenade",
                            didExtinguishMolotovs: _didSmokeExtinguishMolotov.ContainsKey(entityId)
                        )
                    );
                }
            );
        }
        return HookResult.Continue;
    }

    public HookResult OnInfernoStartburn(EventInfernoStartburn @event)
    {
        var entity = Swiftly.Core.EntitySystem.GetEntityByIndex<CBaseEntity>((uint)@event.EntityID);
        var pawn = entity?.OwnerEntity.Value?.As<CCSPlayerPawn>();
        var controller = pawn?.Controller.Value?.As<CCSPlayerController>();
        var playerState = controller?.GetState();
        if (entity != null && playerState != null)
            _thrownMolotovs[entity.Index] = new(
                Game.GetRoundNumber(),
                Game.GetRoundTime(),
                playerState
            );
        return HookResult.Continue;
    }

    public HookResult OnInfernoExtinguish(EventInfernoExtinguish @event)
    {
        _didSmokeExtinguishMolotov[_lastThrownSmokegrenade] = true;
        return HookResult.Continue;
    }

    public HookResult OnInfernoExpire(EventInfernoExpire @event)
    {
        SendOnMolotovDetonatedEvent((uint)@event.EntityID);
        return HookResult.Continue;
    }

    public HookResult OnFlashbangDetonate(EventFlashbangDetonate @event)
    {
        var playerState = @event.UserIdPlayer.GetState();
        if (playerState != null)
        {
            var entityId = (uint)@event.EntityID;
            var roundNumber = Game.GetRoundNumber();
            var roundTime = Game.GetRoundTime();
            Swiftly.Core.Scheduler.DelayBySeconds(
                0.1f,
                () =>
                {
                    var victims = _utilityVictims.TryGetValue(entityId, out var v) ? v : [];
                    Game.SendEvent(
                        OnFlashbangDetonatedEvent.Create(
                            roundNumber,
                            roundTime,
                            playerState,
                            weapon: "weapon_flashbang",
                            victims
                        )
                    );
                    _utilityVictims.Remove(entityId);
                }
            );
        }
        return HookResult.Continue;
    }

    public HookResult OnPlayerHurt(EventPlayerHurt @event)
    {
        var attackerState = Swiftly.Core.PlayerManager.GetPlayer(@event.Attacker)?.GetState();
        var victimState = @event.UserIdPlayer.GetState();
        if (attackerState != null && victimState != null)
        {
            var damage = Math.Max(
                0,
                Math.Min(
                    @event.DmgHealth,
                    _playerHealth.TryGetValue(victimState.SteamID, out var health) ? health : 100
                )
            );
            if (
                victimState.DamageReport.TryGetValue(
                    attackerState.SteamID,
                    out var attackerDamageReport
                )
            )
            {
                attackerDamageReport.From.Value += damage;
                attackerDamageReport.From.Hits += 1;
            }
            if (
                attackerState.DamageReport.TryGetValue(
                    victimState.SteamID,
                    out var victimDamageReport
                )
            )
            {
                victimDamageReport.To.Value += damage;
                victimDamageReport.To.Hits += 1;
            }
            Stats_OnPlayerHurt(@event, damage);
            _playerHealth[victimState.SteamID] = Math.Max(0, (int)@event.Health);
        }
        return HookResult.Continue;
    }

    public void OnEntityTakeDamage(IOnEntityTakeDamageEvent @event)
    {
        var entity = @event.Entity;
        if (entity.DesignerName != "player")
            return;
        var pawn = entity.As<CCSPlayerPawn>();
        var controller = pawn.OriginalController.Value;
        if (controller?.SteamID == 0)
            return;
        var info = @event.Info;
        var inflictor = info.Inflictor.Value;
        if (inflictor == null || !ItemHelper.IsUtilityDesignerName(inflictor.DesignerName))
            return;
        var playerState = controller?.GetState();
        if (playerState != null && controller != null)
        {
            var victims = _utilityVictims.TryGetValue(inflictor.Index, out var v) ? v : [];
            var victim = victims.TryGetValue(playerState.SteamID, out var p) ? p : new(playerState);
            if (controller.GetHealth() <= 0)
                victim.Killed = true;
            victim.Damage += (int)info.Damage;
            victims[playerState.SteamID] = victim;
            _utilityVictims[inflictor.Index] = victims;
        }
    }

    public HookResult OnBombExploded(EventBombExploded _)
    {
        Game.SendEvent(OnBombExplodedEvent.Create(_lastPlantedBombZone));
        return HookResult.Continue;
    }

    public HookResult OnRoundEndPre(EventRoundEnd @event)
    {
        _canSurrender = false;
        var localize = Swiftly.Core.Localizer;
        var home = Game.Teams.First();
        var away = Game.Teams.Last();
        Swiftly.Core.PlayerManager.SendChat(
            localize[
                "match.round_end_score",
                Game.GetChatPrefix(),
                home.FormattedName,
                home.Score,
                away.Score,
                away.FormattedName
            ]
        );
        foreach (var playerState in Game.Teams.SelectMany(t => t.Players))
        {
            foreach (var report in playerState.DamageReport.Values)
            {
                playerState.Handle?.SendChat(
                    localize[
                        "match.round_end_damage",
                        Game.GetChatPrefix(),
                        report.To.Value,
                        report.To.Hits,
                        report.From.Value,
                        report.From.Hits,
                        report.Player.Name,
                        report.Player.Handle?.GetHealth() ?? 0
                    ]
                );
                report.Reset();
            }
        }
        return HookResult.Continue;
    }

    public void SendOnMolotovDetonatedEvent(uint entityId)
    {
        if (_thrownMolotovs.TryGetValue(entityId, out var thrown))
        {
            var victims = _utilityVictims.TryGetValue(entityId, out var v) ? v : [];
            Game.SendEvent(
                OnMolotovDetonatedEvent.Create(
                    thrown.RoundNumber,
                    thrown.RoundTime,
                    thrown.Player,
                    "weapon_molotov",
                    victims
                )
            );
            _utilityVictims.Remove(entityId);
            _thrownMolotovs.Remove(entityId);
        }
    }
}
