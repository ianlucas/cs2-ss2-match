/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.SchemaDefinitions;

namespace Match;

public static class ItemHelper
{
    public static bool IsMeleeDesignerName(string designerName)
    {
        return designerName.Contains("bayonet") || designerName.Contains("knife");
    }

    public static uint GetItemDefIndex(string designerName)
    {
        return SchemaHelper.GetItemSchema()?.GetItemDefinitionByName(designerName)?.DefIndex ?? 0;
    }

    public static string GetItemDesignerName(uint defIndex)
    {
        return SchemaHelper.GetItemSchema()?.GetItemDefinition(defIndex)?.DefinitionName ?? "";
    }

    public static bool IsUtilityDesignerName(string designerName)
    {
        var itemDef = SchemaHelper.GetItemSchema()?.GetItemDefinitionByName(designerName);
        if (itemDef == null)
            return false;
        var slot = itemDef.DefaultLoadoutSlot;
        return slot >= loadout_slot_t.LOADOUT_SLOT_GRENADE0
            && slot <= loadout_slot_t.LOADOUT_SLOT_GRENADE5;
    }

    public static string NormalizeDesignerName(
        string designerName,
        CCSPlayerController? owner = null
    )
    {
        if (IsMeleeDesignerName(designerName))
            return "knife";
        var activeWeapon = owner?.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (activeWeapon != null)
            designerName = GetItemDesignerName(
                activeWeapon.AttributeManager.Item.ItemDefinitionIndex
            );
        return designerName.Replace("weapon_", "");
    }
}
