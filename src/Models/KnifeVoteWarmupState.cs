/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Match.Get5.Events;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace Match;

public class KnifeVoteWarmupState : WarmupState
{
    public override string Name => "waiting_for_knife_decision";
    public static readonly List<string> StayCmds = ["sw_stay", "sw_ficar"];
    public static readonly List<string> SwitchCmds = ["sw_switch", "sw_trocar"];
    public static readonly List<KnifeRoundVote> KnifeRoundVotes =
    [
        KnifeRoundVote.Stay,
        KnifeRoundVote.Switch,
    ];

    public override void Load()
    {
        base.Load();
        HookGameEvent<EventPlayerTeam>(OnPlayerTeamPre, HookMode.Pre);
        RegisterCommand(StayCmds, OnStayCommand);
        RegisterCommand(SwitchCmds, OnSwitchCommand);
        Timers.SetEveryChatInterval("KnifeVoteInstructions", SendKnifeVoteInstructions);
        Timers.Set("KnifeVoteTimeout", ConVars.KnifeVoteTimeout.Value - 1, OnKnifeVoteTimeout);
        MatchCtx.ResetAllKnifeRoundVotes();
        Swiftly.Log("Executing knife vote warmup");
        Config.ExecWarmup(warmupTime: ConVars.KnifeVoteTimeout.Value, isLockTeams: true);
    }

    public HookResult OnPlayerTeamPre(EventPlayerTeam @event)
    {
        return HookResult.Stop;
    }

    public void SendKnifeVoteInstructions()
    {
        var team = MatchCtx.KnifeRoundWinner;
        var leader = team?.InGameLeader;
        if (team != null && leader != null)
            Swiftly.Core.PlayerManager.SendChat(
                Swiftly.Core.Localizer[
                    "match.knife_vote",
                    MatchCtx.GetChatPrefix(),
                    team.FormattedName,
                    leader.Name
                ]
            );
    }

    public void OnStayCommand(ICommandContext context)
    {
        var player = context.Sender;
        var playerState = player?.GetState();
        if (playerState != null)
        {
            Swiftly.Log($"{player?.Controller.PlayerName} voted !stay.");
            playerState.KnifeRoundVote = KnifeRoundVote.Stay;
            CheckLeaderVote();
        }
    }

    public void OnSwitchCommand(ICommandContext context)
    {
        var player = context.Sender;
        var playerState = player?.GetState();
        if (playerState != null)
        {
            Swiftly.Log($"{player?.Controller.PlayerName} voted !switch.");
            playerState.KnifeRoundVote = KnifeRoundVote.Switch;
            CheckLeaderVote();
        }
    }

    public void CheckLeaderVote()
    {
        var team = MatchCtx.KnifeRoundWinner;
        if (team != null)
            foreach (var vote in KnifeRoundVotes)
                if (
                    team.Players.Any(p =>
                        p.KnifeRoundVote == vote && p.SteamID == team.InGameLeader?.SteamID
                    )
                )
                {
                    Swiftly.Log("Team leader has chosen a side.");
                    ProcessKnifeVote(vote);
                    return;
                }
    }

    public void OnKnifeVoteTimeout()
    {
        Swiftly.Log("Knife vote timed out");
        ProcessKnifeVote(KnifeRoundVote.None);
    }

    public void ProcessKnifeVote(KnifeRoundVote decision)
    {
        Swiftly.Log($"Processing knife vote decision: {decision}");
        var winnerTeam = MatchCtx.KnifeRoundWinner;
        if (winnerTeam == null)
            return;
        if (decision != KnifeRoundVote.None)
        {
            var localize = Swiftly.Core.Localizer;
            var decisionLabel = localize[
                decision == KnifeRoundVote.Switch
                    ? "match.knife_decision_switch"
                    : "match.knife_decision_stay"
            ];
            Swiftly.Core.PlayerManager.SendChat(
                localize[
                    "match.knife_decision",
                    MatchCtx.GetChatPrefix(),
                    winnerTeam.FormattedName,
                    decisionLabel
                ]
            );
        }
        if (decision == KnifeRoundVote.Switch)
        {
            MatchCtx.SwapTeamSides();
            Swiftly.Core.EntitySystem.GetGameRules()?.HandleSwapTeams();
        }
        MatchCtx.SendEvent(OnSidePickedEvent.Create(team: winnerTeam));
        MatchCtx.SendEvent(OnKnifeRoundWonEvent.Create(team: winnerTeam, decision));
        MatchCtx.SetState(new LiveState());
    }
}
