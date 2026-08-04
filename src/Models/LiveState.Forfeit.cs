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
        var player = @event.UserIdPlayer;
        if (player != null)
            OnPlayerConnected(player);
        return HookResult.Continue;
    }

    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        var player = @event.UserIdPlayer;
        if (player != null)
            OnPlayerConnected(player);
        return HookResult.Continue;
    }

    public void OnPlayerConnected(IPlayer player)
    {
        if (player.IsFakeClient)
            Rules.SynchronizeBots();
        var playerState = player.GetState();
        if (playerState is { IsBot: false } && Rules.HasTeamsWithAnyHumanConnected())
        {
            _isForfeiting = false;
            Timers.Clear("ForfeitTimeout");
            Runtime.Log("Match forfeit cancelled");
        }
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var playerState = @event.UserIdPlayer?.GetState();
        if (playerState != null)
        {
            playerState.Handle = null;
            TryForfeitMatch();
        }
        return HookResult.Continue;
    }

    public void TryForfeitMatch()
    {
        if (!_isForfeiting && ConVars.IsForfeitEnabled.Value && Rules.MapEndResult == null)
            foreach (var team in Rules.Teams)
            {
                var humans = team.Players.Where(player => !player.IsBot);
                if (
                    humans.Any()
                    && !humans.Any(player => player.Handle != null)
                    && team.Opposition.Players.Any(player => !player.IsBot && player.Handle != null)
                )
                {
                    _isForfeiting = true;
                    Timers.Set("ForfeitTimeout", ConVars.ForfeitTimeout.Value, OnMatchCancelled);
                    Runtime.Log("A team is forfeiting the match.");
                    Runtime.Core.PlayerManager.SendChat(
                        Runtime.Core.Localizer[
                            "match.forfeit_start",
                            Rules.GetChatPrefix(true),
                            team.FormattedName,
                            ConVars.ForfeitTimeout.Value
                        ]
                    );
                    return;
                }
            }
    }
}
