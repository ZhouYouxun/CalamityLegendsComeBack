using System.Collections.Generic;
using System.Globalization;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    public class SHPCAmmoTooltipGlobal : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!EffectRegistry.IsRegisteredAmmo(item.type))
                return;

            int effectID = EffectRegistry.GetEffectIDByAmmo(item.type);
            BalanceSHPC balance = new();
            float panelMultiplier = balance.GetLeftClickMaterialDamageMultiplier(effectID) * 1.25f;
            string panelMultiplierText = panelMultiplier.ToString("0.##", CultureInfo.InvariantCulture);
            int shotsPerAmmo = SHPCAmmoCapacity.GetCapacity(effectID);

            tooltips.Add(new TooltipLine(Mod, "SHPCAmmoPrompt",
                Language.GetTextValue("Mods.CalamityLegendsComeBack.AMMO.SHPCAmmoPrompt"))
            {
                OverrideColor = Color.LightSkyBlue
            });

            tooltips.Add(new TooltipLine(Mod, "SHPCAmmoPanel",
                Language.GetTextValue("Mods.CalamityLegendsComeBack.AMMO.SHPCAmmoPanel", panelMultiplierText, shotsPerAmmo))
            {
                OverrideColor = Color.Gold
            });

            string extraKey = $"Mods.CalamityLegendsComeBack.AMMO.SHPCAmmo{effectID}";
            string extraText = Language.GetTextValue(extraKey);

            if (extraText != extraKey)
            {
                tooltips.Add(new TooltipLine(Mod, "SHPCAmmoExtra", extraText)
                {
                    OverrideColor = Color.LightGreen
                });
            }
        }
    }
}
