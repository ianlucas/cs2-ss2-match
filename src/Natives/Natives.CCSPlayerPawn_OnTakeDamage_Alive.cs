/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Memory;

namespace Match;

public static partial class Natives
{
    public delegate nint CCSPlayerPawn_OnTakeDamage_AliveDelegate(nint a1, nint a2);

    public static readonly IUnmanagedFunction<CCSPlayerPawn_OnTakeDamage_AliveDelegate> CCSPlayerPawn_OnTakeDamage_Alive =
        GetFunctionByOffset<CCSPlayerPawn_OnTakeDamage_AliveDelegate>(
            "CCSPlayerPawn",
            "CCSPlayerPawn::OnTakeDamage_Alive"
        );
}
