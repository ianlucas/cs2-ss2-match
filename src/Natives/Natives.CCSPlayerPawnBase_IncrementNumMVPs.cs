/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Memory;

namespace Match;

public static partial class Natives
{
    public delegate nint CCSPlayerPawnBase_IncrementNumMVPsDelegate(nint thisPtr, uint a2);

    private static readonly Lazy<
        IUnmanagedFunction<CCSPlayerPawnBase_IncrementNumMVPsDelegate>
    > _lazyIncrementNumMVPs = new(() =>
        GetFunctionBySignature<CCSPlayerPawnBase_IncrementNumMVPsDelegate>(
            "CCSPlayerPawnBase::IncrementNumMVPs"
        )
    );

    public static IUnmanagedFunction<CCSPlayerPawnBase_IncrementNumMVPsDelegate> CCSPlayerPawnBase_IncrementNumMVPs =>
        _lazyIncrementNumMVPs.Value;
}
