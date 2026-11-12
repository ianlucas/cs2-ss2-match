/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace Match;

public static partial class Natives
{
    public static readonly int CEconItemDefinition_m_nDefIndex = ResolveOffset(
        "CEconItemDefinition::m_nDefIndex"
    );

    public static readonly int CEconItemDefinition_m_pszDefinitionName = ResolveOffset(
        "CEconItemDefinition::m_pszDefinitionName"
    );

    public static readonly int CEconItemDefinition_m_iDefaultLoadoutSlot = ResolveOffset(
        "CEconItemDefinition::m_iDefaultLoadoutSlot"
    );
}
