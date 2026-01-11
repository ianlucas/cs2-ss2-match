/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Players;

namespace Match;

public class PlayerState(ulong steamId, string name, PlayerTeam team, IPlayer? handle = null)
{
    public bool IsReady = false;

    public IPlayer? Handle = handle;

    public Dictionary<ulong, DamageReport> DamageReport = [];

    public string Name = name;

    public PlayerTeam Team = team;

    public ulong SteamID = steamId;

    public KnifeRoundVote KnifeRoundVote = KnifeRoundVote.None;

    public PlayerStats Stats = new(steamId);

    public void LeaveTeam()
    {
        Team.RemovePlayer(this);
    }

    public void SendChatMessage(string message)
    {
        Handle?.SendChat(message);
    }

    public bool IsConnected()
    {
        return Handle != null;
    }

    public Team GetCurrentTeam()
    {
        return Team.CurrentTeam;
    }

    public bool IsAlive()
    {
        return Handle?.GetHealth() > 0;
    }

    public int GetHealth()
    {
        return Handle?.GetHealth() ?? 0;
    }
}
