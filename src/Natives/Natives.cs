/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Memory;

namespace Match;

public static partial class Natives
{
    private static IUnmanagedFunction<TDelegate> GetFunctionBySignature<TDelegate>(
        string signatureName
    )
        where TDelegate : Delegate
    {
        nint? address = Runtime.Core.GameData.GetSignature(signatureName);
        if (address is null)
            throw new InvalidOperationException(
                $"Failed to locate game function signature '{signatureName}'. The function may not exist in the current game version or the signature pattern may be outdated."
            );
        return Runtime.Core.Memory.GetUnmanagedFunctionByAddress<TDelegate>(address.Value);
    }

    private static IUnmanagedFunction<TDelegate> GetFunctionByOffset<TDelegate>(
        string vtableName,
        string offsetName,
        string? library = null
    )
        where TDelegate : Delegate
    {
        var offset = Runtime.Core.GameData.GetOffset(offsetName);
        var vtable =
            Runtime.Core.Memory.GetVTableAddress(library ?? Library.Server, vtableName)
            ?? throw new InvalidOperationException($"Failed to locate {vtableName} vtable.");
        return Runtime.Core.Memory.GetUnmanagedFunctionByVTable<TDelegate>(vtable, offset);
    }

    private static int GetOffset(string offsetName)
    {
        return Runtime.Core.GameData.GetOffset(offsetName);
    }
}
