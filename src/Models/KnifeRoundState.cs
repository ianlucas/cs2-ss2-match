/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Match;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

public class KnifeRoundState : ReadyUpWarmupState
{
    public override string Name => "knife";

    public override void Load()
    {
        Game.KnifeRoundWinner = null;
        HookGameEvent<EventRoundStart>(OnRoundStart);
        Natives.CCSPlayerPawnBase_IncrementNumMVPs.AddHook(OnIncrementNumMVPs);
        Natives.CCSGameRules_TerminateRound.AddHook(OnTerminateRound);
        Game.Log("Execing Knife Round");
        Config.ExecKnife();
        Cstv.Record(Game.DemoFilename);
        Swiftly.Core.PlayerManager.RemoveAllClans();
    }

    public HookResult OnRoundStart(EventRoundStart @event)
    {
        if (Game.KnifeRoundWinner != null)
            Swiftly.Core.Scheduler.NextWorldUpdate(() => Game.SetState(new KnifeVoteWarmupState()));
        else
        {
            Swiftly.Core.PlayerManager.SendAllRepeat(
                Swiftly.Core.Localizer["match.knife", Game.GetChatPrefix()]
            );
            Game.SendEvent(Game.Get5.OnKnifeRoundStarted());
        }
        return HookResult.Continue;
    }

    public Natives.CCSPlayerPawnBase_IncrementNumMVPsDelegate OnIncrementNumMVPs(
        Func<Natives.CCSPlayerPawnBase_IncrementNumMVPsDelegate> next
    )
    {
        return (a1, a2) => 0;
    }

    public Natives.CCSGameRules_TerminateRoundDelegate OnTerminateRound(
        Func<Natives.CCSGameRules_TerminateRoundDelegate> next
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
                if (OperatingSystem.IsWindows())
                    a5 = reason;
                else
                    a2 = reason;
            }
            next()(a1, a2, a3, a4, a5);
        };
    }
}
