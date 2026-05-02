using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;

namespace CalamityLegendsComeBack.AdditionalText
{
    public class SHPCAmmoTooltipGlobal : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            // 如果这个物品是注册过的SHPC弹药
            if (Weapons.SHPC.Effects.AAARules.EffectRegistry.IsRegisteredAmmo(item.type))
            {
                int effectID = Weapons.SHPC.Effects.AAARules.EffectRegistry.GetEffectIDByAmmo(item.type);
                BalanceSHPC balance = new();
                int panelDamage = balance.GetMaterialLeftClickDamage(effectID);
                int shotsPerAmmo = Weapons.SHPC.SHPCAmmoCapacity.GetCapacity(effectID);

                // 通用提示
                tooltips.Add(new TooltipLine(Mod, "SHPCAmmoPrompt",
                    Language.GetTextValue("Mods.CalamityLegendsComeBack.AMMO.SHPCAmmoPrompt"))
                {
                    OverrideColor = Microsoft.Xna.Framework.Color.LightSkyBlue
                });

                tooltips.Add(new TooltipLine(Mod, "SHPCAmmoPanel",
                    Language.GetTextValue("Mods.CalamityLegendsComeBack.AMMO.SHPCAmmoPanel", panelDamage, shotsPerAmmo))
                {
                    OverrideColor = Microsoft.Xna.Framework.Color.Gold
                });

                // 专属提示（如果存在）
                string extraKey = $"Mods.CalamityLegendsComeBack.AMMO.SHPCAmmo{effectID}";
                string extraText = Language.GetTextValue(extraKey);

                if (extraText != extraKey) // 防止没写本地化时显示key本身
                {
                    tooltips.Add(new TooltipLine(Mod, "SHPCAmmoExtra", extraText)
                    {
                        OverrideColor = Microsoft.Xna.Framework.Color.LightGreen
                    });
                }

                


            }
        }
    }
}
