/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Match;

public static class CTakeDamageInfoExtensions
{
    extension(CTakeDamageInfo self)
    {
        public string? GetInflictorDesignerName()
        {
            var inflictor = self.Inflictor.Value?.As<CBaseEntity>();
            if (inflictor == null)
                return null;
            if (inflictor.DesignerName == "molotov_projectile")
                return inflictor.As<CMolotovProjectile>().IsIncGrenade
                    ? "weapon_incgrenade"
                    : "weapon_molotov";
            if (inflictor.DesignerName.Contains("_projectile"))
                return $"weapon_{inflictor.DesignerName.Replace("_projectile", "")}";
            uint? defIndex = null;
            if (inflictor.DesignerName == "player")
                defIndex = inflictor
                    .As<CCSPlayerPawn>()
                    .WeaponServices?.ActiveWeapon.Value?.AttributeManager.Item.ItemDefinitionIndex;
            if (inflictor.DesignerName == "inferno")
                defIndex = inflictor.As<CInferno>().GetSourceWeaponDefIndex();
            if (defIndex != null)
            {
                var item = SchemaHelper.GetItemSchema()?.GetItemDefinition(defIndex.Value);
                if (item != null)
                    return item.DefinitionName;
            }
            return inflictor.DesignerName;
        }
    }
}
