/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Runtime.InteropServices;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Match;

public static class CInfernoExtensions
{
    extension(CInferno self)
    {
        public ushort GetSourceWeaponDefIndex()
        {
            return (ushort)
                Marshal.ReadInt16(self.Address + Natives.CInferno_m_nSourceWeaponDefIndex);
        }
    }
}
