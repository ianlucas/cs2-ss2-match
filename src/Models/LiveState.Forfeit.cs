/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace Match;

public partial class LiveState
{
    public HookResult OnPlayerConnect(EventPlayerConnect @event)
    {
        OnPlayerConnected(@event.UserIdPlayer);
        return HookResult.Continue;
    }

    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        OnPlayerConnected(@event.UserIdPlayer);
        return HookResult.Continue;
    }

    public void OnPlayerConnected(IPlayer player)
    {
        var playerState = player.GetState();
        if (playerState != null && Game.HasTeamsWithAnyPlayerConnected())
        {
            _isForfeiting = false;
            Timers.Clear("ForfeitTimeout");
            Swiftly.Log("Match forfeit cancelled");
        }
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var playerState = @event.UserIdPlayer.GetState();
        if (playerState != null)
            TryForfeitMatch(playerState);
        return HookResult.Continue;
    }

    public void TryForfeitMatch(PlayerState? disconnecting = null)
    {
        if (!_isForfeiting && ConVars.IsForfeitEnabled.Value && Game.MapEndResult == null)
            foreach (var team in Game.Teams)
                if (
                    team.Players.Count > 0
                    && team.Players.All(p =>
                        p.SteamID == disconnecting?.SteamID || p.Handle == null
                    )
                )
                {
                    _isForfeiting = true;
                    Timers.Set("ForfeitTimeout", ConVars.ForfeitTimeout.Value, OnMatchCancelled);
                    Swiftly.Log("A team is forfeiting the match.");
                    // @todo: Notify players a team is forfeiting.
                    return;
                }
    }
}
