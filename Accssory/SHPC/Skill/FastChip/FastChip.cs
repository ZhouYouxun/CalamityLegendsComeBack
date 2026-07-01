using CalamityLegendsComeBack.Weapons.SHPC.EXSkill;
using CalamityMod;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.Materials;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.FastChip
{
    public class FastChip : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 8);
            Item.rare = ItemRarityID.Yellow;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            FastChipPlayer fastChip = player.GetModPlayer<FastChipPlayer>();
            fastChip.FastChipEquipped = true;
            fastChip.FastChipDrawbackActive = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            bool unlocked = Main.LocalPlayer.GetModPlayer<NewLegend_EXPlayer>().EXUnlocked;
            string text = this.GetLocalizedValue(unlocked ? "TooltipUnlocked" : "TooltipLocked");
            tooltips.FindAndReplace("[GFB]", text);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<MysteriousCircuitry>(10)
                .AddIngredient<DubiousPlating>(10)
                .AddIngredient(ItemID.SoulofNight, 7)
                .AddIngredient(ItemID.DarkShard)
                .AddIngredient(ItemID.Wire, 50)
                .AddIngredient<WulfrumBattery>()
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
