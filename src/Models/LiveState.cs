/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Runtime.CompilerServices;
using Match.Get5.Events;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Match;

public partial class LiveState : ActiveMatchState
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
    private readonly Dictionary<uint, ThrownUtility> _thrownUtilities = [];
    private readonly List<CancellationTokenSource> _utilityDetonateTimers = [];

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
        AddHook(Natives.CCSPlayerPawn_OnTakeDamage_Alive, OnTakeDamage_Alive);
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
        MatchCtx.SendEvent(OnGoingLiveEvent.Create());
        Config.ExecLive(
            maxRounds: ConVars.MaxRounds.Value,
            otMaxRounds: ConVars.OtMaxRounds.Value,
            isFriendlyPause: ConVars.IsFriendlyPause.Value,
            backupPath: MatchCtx.GetBackupPrefix()
        );
        var localize = Swiftly.Core.Localizer;
        Swiftly.Core.PlayerManager.SendChatRepeat(localize["match.live", MatchCtx.GetChatPrefix()]);
        Swiftly.Core.PlayerManager.SendChat(
            localize["match.live_disclaimer", MatchCtx.GetChatPrefix()]
        );
        MatchCtx.ClearAllSurrenderFlags();
        if (!ConVars.IsKnifeRoundEnabled.Value)
            Cstv.Record(MatchCtx.GetDemoFilename());
        Swiftly.Core.PlayerManager.RemovePlayerClans();
        TryForfeitMatch();
    }

    public void OnTick()
    {
        CheckPauseEvents();
        if (ConVars.ServerGraphicUrl.Value != "")
            foreach (var player in MatchCtx.GetAllPlayers())
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
        foreach (var cts in _utilityDetonateTimers)
            cts.Cancel();
        _utilityDetonateTimers.Clear();
        foreach (var entityId in _thrownUtilities.Keys.ToList())
            SendOnUtilityDetonatedEvent(entityId);
        _thrownUtilities.Clear();
        _lastThrownSmokegrenade = 0;
        _didSmokeExtinguishMolotov.Clear();
        MatchCtx.SendEvent(OnRoundStartEvent.Create());
        return HookResult.Continue;
    }

    public HookResult OnGrenadeThrown(EventGrenadeThrown @event)
    {
        var playerState = @event.UserIdPlayer?.GetState();
        if (playerState != null)
            MatchCtx.SendEvent(OnGrenadeThrownEvent.Create(playerState, weapon: @event.Weapon));
        return HookResult.Continue;
    }

    public HookResult OnDecoyStarted(EventDecoyStarted @event)
    {
        var playerState = @event.UserIdPlayer?.GetState();
        if (playerState != null)
            MatchCtx.SendEvent(OnDecoyStartedEvent.Create(playerState, weapon: "weapon_decoy"));
        return HookResult.Continue;
    }

    public HookResult OnHegrenadeDetonate(EventHegrenadeDetonate @event)
    {
        var playerState = @event.UserIdPlayer?.GetState();
        if (playerState != null)
        {
            var entityId = (uint)@event.EntityID;
            _thrownUtilities[entityId] = new(
                MatchCtx.GetRoundNumber(),
                MatchCtx.GetRoundTime(),
                playerState,
                "weapon_hegrenade"
            );
            _utilityDetonateTimers.Add(
                Swiftly.Core.Scheduler.DelayBySeconds(
                    0.1f,
                    () => SendOnUtilityDetonatedEvent(entityId)
                )
            );
        }
        return HookResult.Continue;
    }

    public HookResult OnSmokegrenadeDetonate(EventSmokegrenadeDetonate @event)
    {
        var playerState = @event.UserIdPlayer?.GetState();
        if (playerState != null)
        {
            var entityId = (uint)@event.EntityID;
            var roundNumber = MatchCtx.GetRoundNumber();
            var roundTime = MatchCtx.GetRoundTime();
            _lastThrownSmokegrenade = entityId;
            Swiftly.Core.Scheduler.DelayBySeconds(
                0.1f,
                () =>
                {
                    MatchCtx.SendEvent(
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
            _thrownUtilities[entity.Index] = new(
                MatchCtx.GetRoundNumber(),
                MatchCtx.GetRoundTime(),
                playerState,
                "weapon_molotov"
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
        SendOnUtilityDetonatedEvent((uint)@event.EntityID);
        return HookResult.Continue;
    }

    public HookResult OnFlashbangDetonate(EventFlashbangDetonate @event)
    {
        var playerState = @event.UserIdPlayer?.GetState();
        if (playerState != null)
        {
            var entityId = (uint)@event.EntityID;
            _thrownUtilities[entityId] = new(
                MatchCtx.GetRoundNumber(),
                MatchCtx.GetRoundTime(),
                playerState,
                "weapon_flashbang"
            );
            _utilityDetonateTimers.Add(
                Swiftly.Core.Scheduler.DelayBySeconds(
                    0.1f,
                    () => SendOnUtilityDetonatedEvent(entityId)
                )
            );
        }
        return HookResult.Continue;
    }

    public unsafe Natives.CCSPlayerPawn_OnTakeDamage_AliveDelegate OnTakeDamage_Alive(
        Func<Natives.CCSPlayerPawn_OnTakeDamage_AliveDelegate> next
    ) =>
        (a1, a2) =>
        {
            var ret = next()(a1, a2);
            var victimPawn = Swiftly.Core.Memory.ToSchemaClass<CCSPlayerPawn>(a1);
            ref CTakeDamageResult result = ref Unsafe.AsRef<CTakeDamageResult>((void*)a2);
            var info = result.OriginatingInfo;
            var attacker = info->Attacker;
            Swiftly.Log($"OnTakeDamage -> {info->GetInflictorDesignerName()}");
            if (attacker.Value?.DesignerName != "player")
                return ret;
            var victimController = victimPawn.OriginalController.Value?.As<CCSPlayerController>();
            var victimState = victimController?.GetState();
            var attackerState = attacker
                .Value.As<CCSPlayerPawn>()
                .OriginalController.Value?.As<CCSPlayerController>()
                .GetState();
            if (victimController == null || victimState == null || attackerState == null)
                return ret;
            var inflictor = info->Inflictor.Value?.As<CBaseEntity>();
            var weaponDesignerName = info->GetInflictorDesignerName();
            if (inflictor == null || weaponDesignerName == null || weaponDesignerName == "world")
                return ret;
            var damage = result.HealthLost;
            var isFriendlyFire = victimState.Team == attackerState.Team;
            if (
                ItemHelper.IsUtilityDesignerName(weaponDesignerName)
                && _thrownUtilities.TryGetValue(inflictor.Index, out var utility)
            )
            {
                var victim = utility.GetValueOrDefault(victimState.SteamID, new(victimState));
                if (victimController.GetHealth() <= 0)
                    victim.Killed = true;
                victim.Damage += damage;
                victim.FriendlyFire = isFriendlyFire;
                utility[victimState.SteamID] = victim;
            }
            if (isFriendlyFire)
                return ret;
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
            Stats_OnTakeDamage_Alive(
                attackerState,
                weaponDesignerName,
                damage,
                info->ActualHitGroup
            );
            return ret;
        };

    public HookResult OnBombExploded(EventBombExploded _)
    {
        MatchCtx.SendEvent(OnBombExplodedEvent.Create(_lastPlantedBombZone));
        return HookResult.Continue;
    }

    public HookResult OnRoundEndPre(EventRoundEnd @event)
    {
        _canSurrender = false;
        var localize = Swiftly.Core.Localizer;
        var home = MatchCtx.Teams.First();
        var away = MatchCtx.Teams.Last();
        Swiftly.Core.PlayerManager.SendChat(
            localize[
                "match.round_end_score",
                MatchCtx.GetChatPrefix(),
                home.FormattedName,
                home.Score,
                away.Score,
                away.FormattedName
            ]
        );
        foreach (var playerState in MatchCtx.Teams.SelectMany(t => t.Players))
        {
            foreach (var report in playerState.DamageReport.Values)
            {
                playerState.Handle?.SendChat(
                    localize[
                        "match.round_end_damage",
                        MatchCtx.GetChatPrefix(),
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

    public void SendOnUtilityDetonatedEvent(uint entityId)
    {
        if (!_thrownUtilities.TryGetValue(entityId, out var thrown))
            return;
        switch (thrown.Weapon)
        {
            case "weapon_hegrenade":
                MatchCtx.SendEvent(OnHEGrenadeDetonatedEvent.Create(thrown));
                break;
            case "weapon_flashbang":
                MatchCtx.SendEvent(OnFlashbangDetonatedEvent.Create(thrown));
                break;
            case "weapon_molotov":
                MatchCtx.SendEvent(OnMolotovDetonatedEvent.Create(thrown));
                break;
        }
        _thrownUtilities.Remove(entityId);
    }
}
