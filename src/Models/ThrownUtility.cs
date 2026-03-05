/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace Match;

public class ThrownUtility(int roundNumber, long roundTime, PlayerState player, string weapon)
    : Dictionary<ulong, UtilityDamage>
{
    public int RoundNumber = roundNumber;
    public long RoundTime = roundTime;
    public PlayerState Player = player;
    public string Weapon = weapon;
}
