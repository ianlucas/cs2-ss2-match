/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Memory;

namespace Match;

public static partial class Natives
{
    public delegate nint CCSGameRules_HandleSwapTeamsDelegate(nint thisPtr);

    private static readonly Lazy<
        IUnmanagedFunction<CCSGameRules_HandleSwapTeamsDelegate>
    > _lazyHandleSwapTeams = new(() =>
        FromSignature<CCSGameRules_HandleSwapTeamsDelegate>("CCSGameRules::HandleSwapTeams")
    );

    public static IUnmanagedFunction<CCSGameRules_HandleSwapTeamsDelegate> CCSGameRules_HandleSwapTeams =>
        _lazyHandleSwapTeams.Value;
}
