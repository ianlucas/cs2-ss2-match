/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Events;

namespace Match;

public partial class Match
{
    public void OnConVarValueChanged(IOnConVarValueChanged @event)
    {
        switch (@event.ConVarName)
        {
            case "match_bots":
                HandleBotsChanged();
                return;
            case "match_matchmaking":
                HandleIsMatchmakingChanged();
                return;
        }
    }

    public void OnMapLoad(IOnMapLoadEvent @event)
    {
        Core.Engine.ExecuteCommand("sv_hibernate_when_empty 0");
        PendingInternalPush = true;
    }

    public void OnTick()
    {
        if (PendingInternalPush)
        {
            PendingInternalPush = false;
            OnConfigsExecuted();
        }
    }

    public void OnConfigsExecuted() { }
}
