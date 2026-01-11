/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace Match;

public class DamageReport(PlayerState player)
{
    public Damage To = new();
    public Damage From = new();
    public PlayerState Player = player;

    public void Reset()
    {
        To = new();
        From = new();
    }
}
