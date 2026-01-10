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
        Game.KnifeRoundWinner = null;
        HookGameEvent<EventRoundStart>(OnRoundStart);
        AddHook(Natives.CCSPlayerPawnBase_IncrementNumMVPs, OnIncrementNumMVPs);
        if (OperatingSystem.IsWindows())
            AddHook(Natives.CCSGameRules_TerminateRoundWindows, OnTerminateRoundWindows);
        else
            AddHook(Natives.CCSGameRules_TerminateRoundLinux, OnTerminateRoundLinux);
        Game.Log("Execing Knife Round");
        Config.ExecKnife();
        Cstv.Record(Game.DemoFilename);
        Swiftly.Core.PlayerManager.RemovePlayerClans();
    }

    public HookResult OnRoundStart(EventRoundStart @event)
    {
        if (Game.KnifeRoundWinner != null)
            Swiftly.Core.Scheduler.NextWorldUpdate(() => Game.SetState(new KnifeVoteWarmupState()));
        else
        {
            Swiftly.Core.PlayerManager.SendChatRepeat(
                Swiftly.Core.Localizer["match.knife", Game.GetChatPrefix()]
            );
            Game.SendEvent(OnKnifeRoundStartedEvent.Create());
        }
        return HookResult.Continue;
    }

    public Natives.CCSPlayerPawnBase_IncrementNumMVPsDelegate OnIncrementNumMVPs(
        Func<Natives.CCSPlayerPawnBase_IncrementNumMVPsDelegate> next
    )
    {
        return (a1, a2) => 0;
    }

    public Natives.CCSGameRules_TerminateRoundWindowsDelegate OnTerminateRoundWindows(
        Func<Natives.CCSGameRules_TerminateRoundWindowsDelegate> next
    )
    {
        return (a1, a2, a3, a4, a5) =>
        {
            var winner = Swiftly.Core.EntitySystem.GetGameRules()?.DetermineWinnerBySurvival();
            if (winner != null)
            {
                Game.KnifeRoundWinner = Game.GetTeam(winner.Value);
                var reason = (uint)(
                    winner == Team.T ? RoundEndReason.TerroristsWin : RoundEndReason.CTsWin
                );
                a5 = reason;
            }
            next()(a1, a2, a3, a4, a5);
        };
    }

    public Natives.CCSGameRules_TerminateRoundLinuxDelegate OnTerminateRoundLinux(
        Func<Natives.CCSGameRules_TerminateRoundLinuxDelegate> next
    )
    {
        return (a1, a2, a3, a4, a5) =>
        {
            var winner = Swiftly.Core.EntitySystem.GetGameRules()?.DetermineWinnerBySurvival();
            if (winner != null)
            {
                Game.KnifeRoundWinner = Game.GetTeam(winner.Value);
                var reason = (uint)(
                    winner == Team.T ? RoundEndReason.TerroristsWin : RoundEndReason.CTsWin
                );
                a2 = reason;
            }
            next()(a1, a2, a3, a4, a5);
        };
    }
}
