/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace Match;

public class ThrownMolotov(int roundNumber, long roundTime, Player player)
{
    public int RoundNumber = roundNumber;
    public long RoundTime = roundTime;
    public Player Player = player;
}
