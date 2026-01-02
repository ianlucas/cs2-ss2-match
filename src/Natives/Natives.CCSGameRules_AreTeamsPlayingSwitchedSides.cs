/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Memory;

namespace Match;

public static partial class Natives
{
    public delegate bool CCSGameRules_AreTeamsPlayingSwitchedSidesDelegate(nint thisPtr);

    private static readonly Lazy<
        IUnmanagedFunction<CCSGameRules_AreTeamsPlayingSwitchedSidesDelegate>
    > _lazyAreTeamsPlayingSwitchedSides = new(() =>
        FromSignature<CCSGameRules_AreTeamsPlayingSwitchedSidesDelegate>(
            "CCSGameRules::AreTeamsPlayingSwitchedSides"
        )
    );

    public static IUnmanagedFunction<CCSGameRules_AreTeamsPlayingSwitchedSidesDelegate> CCSGameRules_AreTeamsPlayingSwitchedSides =>
        _lazyAreTeamsPlayingSwitchedSides.Value;
}
