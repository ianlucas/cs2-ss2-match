/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.ProtobufDefinitions;

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

    public void OnConfigsExecuted()
    {
        HandleBotsChanged();
        HandleIsMatchmakingChanged();
        Game.SetState(Game.IsSeriesStarted ? new ReadyupWarmupState() : new NoneState());
    }

    public void OnClientSteamAuthorize(IOnClientSteamAuthorizeEvent @event)
    {
        var player = Core.PlayerManager.GetPlayer(@event.PlayerId);
        if (player != null)
        {
            var playerState = player.GetState();
            if (playerState != null)
            {
                if (playerState.Name == "")
                    playerState.Name = player.Controller.PlayerName;
                playerState.Handle = player;
            }
            else if (
                !player.IsFakeClient
                && ConVars.IsMatchmaking.Value
                && ConVars.IsMatchmakingKick.Value
                && Core.Permission.PlayerHasPermissions(player.SteamID, ["@css/root"])
            )
                player.Kick(
                    "Match is reserved for a lobby.",
                    ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_RESERVED_FOR_LOBBY
                );
            Game.SendEvent(Game.Get5.OnPlayerConnected(player));
        }
    }

    public void OnClientDisconnected(IOnClientDisconnectedEvent @event)
    {
        var player = Core.PlayerManager.GetPlayer(@event.PlayerId);
        if (player != null)
        {
            player.GetState()?.Handle = null;
            Game.SendEvent(Game.Get5.OnPlayerDisconnected(player));
        }
    }
}
