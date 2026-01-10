/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Memory;

namespace Match;

public static partial class Natives
{
    public delegate byte CCSBotManager_MaintainBotQuotaDelegate(nint thisPtr);

    private static readonly Lazy<
        IUnmanagedFunction<CCSBotManager_MaintainBotQuotaDelegate>
    > _lazyMaintainBotQuota = new(() =>
        GetFunctionBySignature<CCSBotManager_MaintainBotQuotaDelegate>(
            "CCSBotManager::MaintainBotQuota"
        )
    );

    public static IUnmanagedFunction<CCSBotManager_MaintainBotQuotaDelegate> CCSBotManager_MaintainBotQuota =>
        _lazyMaintainBotQuota.Value;
}
