/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Match.Get5.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace Match;

public class KnifeRoundState : ReadyupWarmupState
{
    public override string Name => "knife";

    public override void Load()
    {
        Rules.KnifeRoundWinner = null;
        HookGameEvent<EventRoundStart>(OnRoundStart);
        AddHook(Natives.CCSPlayerPawnBase_IncrementNumMVPs, OnIncrementNumMVPs);
        if (OperatingSystem.IsWindows())
            AddHook(Natives.CCSGameRules_TerminateRoundWindows, OnTerminateRoundWindows);
        else
            AddHook(Natives.CCSGameRules_TerminateRoundLinux, OnTerminateRoundLinux);
        Runtime.Log("Executing knife round configuration");
        Config.ExecKnife();
        Cstv.Record(Rules.GetDemoFilename());
        Runtime.Core.PlayerManager.RemovePlayerClans();
    }

    private uint? TryGetKnifeWinnerReason()
    {
        var winner = Runtime.Core.EntitySystem.GetGameRules()?.DetermineWinnerBySurvival();
        if (winner == null)
            return null;
        Rules.KnifeRoundWinner = Rules.GetTeam(winner.Value);
        return (uint)(winner == Team.T ? RoundEndReason.TerroristsWin : RoundEndReason.CTsWin);
    }

    public HookResult OnRoundStart(EventRoundStart @event)
    {
        if (Rules.KnifeRoundWinner != null)
            Runtime.Core.Scheduler.NextWorldUpdate(() =>
                Rules.SetState(new KnifeVoteWarmupState())
            );
        else
        {
            Runtime.Core.PlayerManager.SendChatRepeat(
                Runtime.Core.Localizer["match.knife", Rules.GetChatPrefix()]
            );
            Rules.SendEvent(OnKnifeRoundStartedEvent.Create());
        }
        return HookResult.Continue;
    }

    public Natives.CCSPlayerPawnBase_IncrementNumMVPsDelegate OnIncrementNumMVPs(
        Func<Natives.CCSPlayerPawnBase_IncrementNumMVPsDelegate> next
    ) => (a1, a2) => 0;

    public Natives.CCSGameRules_TerminateRoundWindowsDelegate OnTerminateRoundWindows(
        Func<Natives.CCSGameRules_TerminateRoundWindowsDelegate> next
    ) =>
        (a1, a2, a3, a4) =>
        {
            if (TryGetKnifeWinnerReason() is uint reason)
                a3 = reason;
            next()(a1, a2, a3, a4);
        };

    public Natives.CCSGameRules_TerminateRoundLinuxDelegate OnTerminateRoundLinux(
        Func<Natives.CCSGameRules_TerminateRoundLinuxDelegate> next
    ) =>
        (a1, a2, a3, a4) =>
        {
            if (TryGetKnifeWinnerReason() is uint reason)
                a2 = reason;
            next()(a1, a2, a3, a4);
        };
}
