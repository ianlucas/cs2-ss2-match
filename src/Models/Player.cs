/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Players;

namespace Match;

public class Player(ulong steamId, string name, PlayerTeam team, IPlayer? handle = null)
{
    public bool IsReady = false;

    public IPlayer? Handle = handle;

    public Dictionary<ulong, DamageReport> DamageReport = [];

    public string Name = name;

    public PlayerTeam Team = team;

    public ulong SteamID = steamId;

    public KnifeRoundVote KnifeRoundVote = KnifeRoundVote.None;

    public PlayerStats Stats = new(steamId);
}
