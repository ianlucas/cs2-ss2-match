/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Memory;

namespace Match;

public static partial class Natives
{
    public delegate bool CCSGameRules_IsLastRoundBeforeHalfTimeDelegate(nint thisPtr);

    public static readonly IUnmanagedFunction<CCSGameRules_IsLastRoundBeforeHalfTimeDelegate> CCSGameRules_IsLastRoundBeforeHalfTime =
        GetFunctionBySignature<CCSGameRules_IsLastRoundBeforeHalfTimeDelegate>(
            "CCSGameRules::IsLastRoundBeforeHalfTime"
        );
}
