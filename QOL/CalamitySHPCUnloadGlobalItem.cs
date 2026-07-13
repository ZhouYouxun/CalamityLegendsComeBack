using System.Collections.Generic;
using CalamityMod.Items.Weapons.Magic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL
{
    /// <summary>
    /// External quality-of-life support for Calamity's original SHPC.
    /// The Calamity item already stores its loaded ammunition on the item instance,
    /// so the loaded charge can be cleared without changing CalamityMod's source.
    /// </summary>
    internal sealed class CalamitySHPCUnloadGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<SHPC>();

        // The original SHPC only allows this while Shift is held. The external hook
        // enables the same unload action with a normal inventory right click.
        public override bool CanRightClick(Item item) => true;

        public override void RightClick(Item item, Player player)
        {
            if (item.ModItem is not SHPC shpc)
                return;

            shpc.storedSoulpower = 0;
            item.NetStateChanged();
        }

        // Keep the SHPC itself when the inventory right-click hook runs.
        // This mirrors NewLegendSHPC's ConsumeItem protection without touching CalamityMod.
        public override bool ConsumeItem(Item item, Player player) => false;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            bool chinese = Language.ActiveCulture.Name.StartsWith("zh");
            string hint = chinese
                ? "[c/9DA8B8:背包内右键点击此武器，可倒出当前装填的弹药。]"
                : "[c/9DA8B8:Right-click this weapon in your inventory to unload its current ammunition.]";

            tooltips.Add(new TooltipLine(Mod, "CalamitySHPCUnloadHint", hint)
            {
                OverrideColor = new Color(157, 168, 184)
            });
        }
    }
}
