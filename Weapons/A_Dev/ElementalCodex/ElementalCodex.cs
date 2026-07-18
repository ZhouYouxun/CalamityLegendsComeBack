using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.A_Tools.DebugTools;

namespace CalamityLegendsComeBack.Weapons.A_Dev.ElementalCodex
{
    public sealed class ElementalCodex : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Accessories";
        public override string Texture => "Terraria/Images/Item_" + ItemID.Book;

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            DebugToolOutline.Draw(spriteBatch, TextureAssets.Item[Type].Value, position, frame, origin, scale, new Color(120, 190, 255));
            return true;
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = Item.sellPrice(gold: 8);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            ElementalCodexPlayer codexPlayer = player.GetModPlayer<ElementalCodexPlayer>();
            codexPlayer.ElementalCodexEquipped = true;

            player.manaFlower = true;
            player.manaMagnet = true;
            player.manaCost -= 0.08f;
            player.GetDamage(DamageClass.Magic) += 0.08f;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (!Main.keyState.PressingShift())
                return;

            tooltips.RemoveAll(line => line.Mod == "Terraria" &&
                line.Name.StartsWith("Tooltip", StringComparison.Ordinal));
            tooltips.Add(new TooltipLine(Mod, "ElementalCodexShiftDetails", this.GetLocalizedValue("ShiftDetails"))
            {
                OverrideColor = new Color(210, 236, 255)
            });
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.ManaFlower)
                .AddIngredient(ItemID.CelestialMagnet)
                .AddIngredient(ItemID.FallenStar, 15)
                .AddTile(TileID.CrystalBall)
                .Register();
        }
    }
}
