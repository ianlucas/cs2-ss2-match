/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

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
    }
}
