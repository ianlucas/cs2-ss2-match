/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Memory;

namespace Match;

public static partial class Natives
{
    public delegate void CCSGameRules_TerminateRoundDelegate(
        nint a1,
        uint a2,
        uint a3,
        nint a4,
        uint a5
    );

    private static readonly Lazy<
        IUnmanagedFunction<CCSGameRules_TerminateRoundDelegate>
    > _lazyTerminateRound = new(() =>
        FromSignature<CCSGameRules_TerminateRoundDelegate>("CGameRules::TerminateRound")
    );

    public static IUnmanagedFunction<CCSGameRules_TerminateRoundDelegate> CCSGameRules_TerminateRound =>
        _lazyTerminateRound.Value;
}
