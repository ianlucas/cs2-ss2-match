/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Memory;

namespace Match;

public static partial class Natives
{
    public delegate void CCSPlayerController_ChangeTeamDelegate(nint a1, int a2);

    private static readonly Lazy<
        IUnmanagedFunction<CCSPlayerController_ChangeTeamDelegate>
    > _lazyChangeTeam = new(() =>
        GetFunctionByOffset<CCSPlayerController_ChangeTeamDelegate>(
            "CCSPlayerController",
            "CCSPlayerController::ChangeTeam"
        )
    );

    public static IUnmanagedFunction<CCSPlayerController_ChangeTeamDelegate> CCSGameRules_ChangeTeam =>
        _lazyChangeTeam.Value;
}
