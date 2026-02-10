/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Match.Get5.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace Match;

public partial class Match
{
    public HookResult OnPlayerChat(EventPlayerChat @event)
    {
        var player = @event.UserIdPlayer;
        if (player == null)
            return HookResult.Continue;
        var message = @event.Text.Trim();
        if (message.Length == 0)
            return HookResult.Continue;
        Game.SendEvent(
            OnPlayerSayEvent.Create(player, @event.TeamOnly ? "say_team" : "team", message)
        );
        return HookResult.Continue;
    }
}
