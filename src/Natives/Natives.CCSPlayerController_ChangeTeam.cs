/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Memory;

namespace Match;

public static partial class Natives
{
    public delegate void CCSPlayerController_ChangeTeamDelegate(nint a1, int a2);

    public static readonly IUnmanagedFunction<CCSPlayerController_ChangeTeamDelegate> CCSGameRules_ChangeTeam =
        GetFunctionByOffset<CCSPlayerController_ChangeTeamDelegate>(
            "CCSPlayerController",
            "CCSPlayerController::ChangeTeam"
        );
}
