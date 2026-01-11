/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Commands;

namespace Match;

public static class ICommandServiceExtensions
{
    extension(ICommandService self)
    {
        public List<Guid> Register(List<string> names, ICommandService.CommandListener handler)
        {
            return
            [
                .. names.Select(name => self.RegisterCommand(name, handler, registerRaw: true)),
            ];
        }
    }
}
