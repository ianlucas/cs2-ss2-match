/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Players;

namespace Match;

public class PlayerState(
    ulong steamId,
    string name,
    PlayerTeam team,
    IPlayer? handle = null,
    bool isBot = false
)
{
    public bool IsReady = false;

    public IPlayer? Handle = handle;

    public Dictionary<string, DamageReport> DamageReport = [];

    public string Name = name;

    public PlayerTeam Team = team;

    public ulong SteamID = steamId;

    public bool IsBot = isBot;

    public string Key => IsBot ? Name : SteamID.ToString();

    public ulong EventSteamID => IsBot ? Rules.GetBotSteamID(Name) : SteamID;

    public KnifeRoundVote KnifeRoundVote = KnifeRoundVote.None;

    public PlayerStats Stats = new(isBot ? Rules.GetBotSteamID(name) : steamId);

    public void LeaveTeam()
    {
        Team.RemovePlayer(this);
    }
}
