/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace Match;

public partial class Match
{
    public void HandleBotsChanged()
    {
        if (ConVars.IsBots.Value)
            DidKickBots = false;
    }

    public void HandleIsMatchmakingChanged() { }
}
