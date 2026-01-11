/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Players;

namespace Match;

public static partial class TeamExtensions
{
    extension(Team self)
    {
        public Team Toggle()
        {
            return self > Team.Spectator
                ? self == Team.T
                    ? Team.CT
                    : Team.T
                : self;
        }
    }
}
