/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Services;

namespace Match;

public static class IEngineServiceExtensions
{
    extension(IEngineService self)
    {
        public void ExecuteCommand(List<string> commands)
        {
            foreach (var command in commands)
                self.ExecuteCommand(command);
        }

        public void SetTeamName(Team team, string name)
        {
            var index = team == Team.CT ? 1 : 2;
            self.ExecuteCommand($"mp_teamname_{index} {name}");
        }
    }
}
