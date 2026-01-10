/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace Match;

public class DamageReport(Player player)
{
    public Damage To = new();
    public Damage From = new();
    public Player Player = player;

    public void Reset()
    {
        To = new();
        From = new();
    }
}
