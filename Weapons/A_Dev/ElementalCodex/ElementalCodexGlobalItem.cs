using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.ElementalCodex
{
    internal sealed class ElementalCodexGlobalItem : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            Player localPlayer = Main.LocalPlayer;
            if (localPlayer == null ||
                !localPlayer.active ||
                !localPlayer.GetModPlayer<ElementalCodexPlayer>().ElementalCodexEquipped ||
                !ElementalCodexWeaponDatabase.TryGetDefinition(item.type, out ElementalCodexWeaponDefinition definition))
                return;

            string text = Language.GetTextValue(
                "Mods.CalamityLegendsComeBack.ElementalCodex.WeaponTooltip",
                definition.ChineseName,
                definition.InternalName,
                definition.GetLocalizedElementList());

            tooltips.Add(new TooltipLine(Mod, "ElementalCodexWeaponElement", text)
            {
                OverrideColor = definition.Mixed ? new Color(226, 230, 255) : ElementalCodexContent.GetElementColor(definition.Elements[0])
            });
        }

        public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            ElementalCodexGlobalNPC.TryApplyWeaponElement(target, player, item);
        }
    }
}
