/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Memory;

namespace Match;

public static partial class Natives
{
    public delegate nint CCSPlayerPawnBase_IncrementNumMVPsDelegate(nint thisPtr, uint a2);

    public static readonly IUnmanagedFunction<CCSPlayerPawnBase_IncrementNumMVPsDelegate> CCSPlayerPawnBase_IncrementNumMVPs =
        GetFunctionBySignature<CCSPlayerPawnBase_IncrementNumMVPsDelegate>(
            "CCSPlayerPawnBase::IncrementNumMVPs"
        );
}
