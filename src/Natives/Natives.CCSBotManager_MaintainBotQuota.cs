/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Memory;

namespace Match;

public static partial class Natives
{
    public delegate byte CCSBotManager_MaintainBotQuotaDelegate(nint thisPtr);

    public static readonly IUnmanagedFunction<CCSBotManager_MaintainBotQuotaDelegate> CCSBotManager_MaintainBotQuota =
        GetFunctionBySignature<CCSBotManager_MaintainBotQuotaDelegate>(
            "CCSBotManager::MaintainBotQuota"
        );
}
