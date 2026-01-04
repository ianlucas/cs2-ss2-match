/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace Match;

public class BaseState
{
    public virtual string Name { get; set; } = "default_state";
    protected bool _matchCancelled = false;

    public virtual void Load() { }

    public virtual void Unload() { }

    public HookResult OnRoundPrestart(EventRoundPrestart _)
    {
        if (Swiftly.Core.EntitySystem.GetGameRules()?.GamePhase == 5) { }
        // OnMapEnd(); // Map result should be computed at State::OnCsWinPanelRound.
        return HookResult.Continue;
    }

    public HookResult OnCsWinPanelMatch(EventCsWinPanelMatch _)
    {
        Timers.ClearAll();
        var result = MapResult.None;
        PlayerTeam? winner = null;
        foreach (var team in Game.Teams)
        {
            if (team.IsSurrended)
            {
                result = MapResult.Forfeited;
                winner = team.Opposition;
                Game.Log($"forfeited, result={result}, winner={winner.Index}");
                break;
            }
            if (team.Score > team.Opposition.Score)
            {
                result = MapResult.Completed;
                winner = team;
                Game.Log($"completed, result={result}, winner={winner.Index}");
            }
        }
        // OnMapResult(result, winner);
        return HookResult.Continue;
    }
}
