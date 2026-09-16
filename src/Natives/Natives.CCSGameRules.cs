/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Memory;

namespace Match;

public static partial class Natives
{
    public delegate bool CCSGameRules_AreTeamsPlayingSwitchedSidesDelegate(nint thisPtr);

    public static readonly IUnmanagedFunction<CCSGameRules_AreTeamsPlayingSwitchedSidesDelegate> CCSGameRules_AreTeamsPlayingSwitchedSides =
        ResolveFunction<CCSGameRules_AreTeamsPlayingSwitchedSidesDelegate>(
            "CCSGameRules::AreTeamsPlayingSwitchedSides"
        );

    public delegate nint CCSGameRules_HandleSwapTeamsDelegate(nint thisPtr);

    public static readonly IUnmanagedFunction<CCSGameRules_HandleSwapTeamsDelegate> CCSGameRules_HandleSwapTeams =
        ResolveFunction<CCSGameRules_HandleSwapTeamsDelegate>("CCSGameRules::HandleSwapTeams");

    public delegate bool CCSGameRules_IsLastRoundBeforeHalfTimeDelegate(nint thisPtr);

    public static readonly IUnmanagedFunction<CCSGameRules_IsLastRoundBeforeHalfTimeDelegate> CCSGameRules_IsLastRoundBeforeHalfTime =
        ResolveFunction<CCSGameRules_IsLastRoundBeforeHalfTimeDelegate>(
            "CCSGameRules::IsLastRoundBeforeHalfTime"
        );

    public delegate void CCSGameRules_TerminateRoundWindowsDelegate(
        nint a1,
        float a2,
        uint a3,
        nint a4
    );

    public delegate void CCSGameRules_TerminateRoundLinuxDelegate(
        nint a1,
        uint a2,
        nint a3,
        float a4
    );

    public static readonly IUnmanagedFunction<CCSGameRules_TerminateRoundWindowsDelegate> CCSGameRules_TerminateRoundWindows =
        OperatingSystem.IsWindows()
            ? ResolveFunction<CCSGameRules_TerminateRoundWindowsDelegate>(
                "CGameRules::TerminateRound"
            )
            : null!;

    public static readonly IUnmanagedFunction<CCSGameRules_TerminateRoundLinuxDelegate> CCSGameRules_TerminateRoundLinux =
        !OperatingSystem.IsWindows()
            ? ResolveFunction<CCSGameRules_TerminateRoundLinuxDelegate>(
                "CGameRules::TerminateRound"
            )
            : null!;
}
