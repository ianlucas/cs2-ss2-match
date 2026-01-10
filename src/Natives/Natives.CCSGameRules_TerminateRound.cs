/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Memory;

namespace Match;

public static partial class Natives
{
    public delegate void CCSGameRules_TerminateRoundWindowsDelegate(
        nint a1,
        float a2,
        uint a3,
        nint a4,
        uint a5
    );

    public delegate void CCSGameRules_TerminateRoundLinuxDelegate(
        nint a1,
        uint a2,
        nint a3,
        uint a4,
        float a5
    );

    private static readonly Lazy<
        IUnmanagedFunction<CCSGameRules_TerminateRoundWindowsDelegate>
    > _lazyTerminateRoundWindows = new(() =>
        GetFunctionBySignature<CCSGameRules_TerminateRoundWindowsDelegate>(
            "CGameRules::TerminateRound"
        )
    );

    public static IUnmanagedFunction<CCSGameRules_TerminateRoundWindowsDelegate> CCSGameRules_TerminateRoundWindows =>
        _lazyTerminateRoundWindows.Value;

    private static readonly Lazy<
        IUnmanagedFunction<CCSGameRules_TerminateRoundLinuxDelegate>
    > _lazyTerminateRoundLinux = new(() =>
        GetFunctionBySignature<CCSGameRules_TerminateRoundLinuxDelegate>(
            "CGameRules::TerminateRound"
        )
    );

    public static IUnmanagedFunction<CCSGameRules_TerminateRoundLinuxDelegate> CCSGameRules_TerminateRoundLinux =>
        _lazyTerminateRoundLinux.Value;
}
