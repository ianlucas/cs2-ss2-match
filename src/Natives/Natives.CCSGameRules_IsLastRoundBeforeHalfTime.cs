/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Memory;

namespace Match;

public static partial class Natives
{
    public delegate bool CCSGameRules_IsLastRoundBeforeHalfTimeDelegate(nint thisPtr);

    private static readonly Lazy<
        IUnmanagedFunction<CCSGameRules_IsLastRoundBeforeHalfTimeDelegate>
    > _lazyIsLastRoundBeforeHalfTime = new(() =>
        GetFunctionBySignature<CCSGameRules_IsLastRoundBeforeHalfTimeDelegate>(
            "CCSGameRules::IsLastRoundBeforeHalfTime"
        )
    );

    public static IUnmanagedFunction<CCSGameRules_IsLastRoundBeforeHalfTimeDelegate> CCSGameRules_IsLastRoundBeforeHalfTime =>
        _lazyIsLastRoundBeforeHalfTime.Value;
}
