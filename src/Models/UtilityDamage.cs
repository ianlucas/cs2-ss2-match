/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace Match;

public class UtilityDamage(
    Player player,
    bool killed = false,
    int damage = 0,
    bool friendlyFire = false,
    float blindDuration = 0f
)
{
    public Player Player = player;
    public bool Killed = killed;
    public int Damage = damage;
    public bool FriendlyFire = friendlyFire;
    public float BindDuration = blindDuration;
}
