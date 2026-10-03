using System.Collections.Generic;
using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.DataStructures;
using CalamityMod.Items.Accessories;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.World;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.Barrier
{
    // Only registered when Fables replaces the original Rover Drive's acquisition path.
    public sealed class CL_RoverDrive : ModItem, IDyeableShaderRenderer
    {
        private const string RoverDriveLocalization = "Mods.CalamityMod.Items.Accessories.RoverDrive";
        private static readonly RoverDrive ShieldRenderer = new();

        public override bool IsLoadingEnabled(Mod mod) => ModLoader.HasMod("CalamityFables");

        public override string Texture => "CalamityMod/Items/Accessories/RoverDrive";
        public override LocalizedText DisplayName => Language.GetText(RoverDriveLocalization + ".DisplayName");
        public override LocalizedText Tooltip => Language.GetText(RoverDriveLocalization + ".Tooltip")
            .WithFormatArgs(RoverDrive.ShieldDurabilityMax,
                RoverDrive.ShieldRechargeDelay.FramesToSeconds(),
                RoverDrive.TotalShieldRechargeTime.FramesToSeconds());

        public int OwnerPlayer { get; set; }
        public float RenderDepth => IDyeableShaderRenderer.RoverDriveDepth;

        public bool ShouldDrawDyeableShader
        {
            get
            {
                ShieldRenderer.OwnerPlayer = OwnerPlayer;
                return ShieldRenderer.ShouldDrawDyeableShader;
            }
        }

        public void DrawDyeableShader(SpriteBatch spriteBatch)
        {
            ShieldRenderer.OwnerPlayer = OwnerPlayer;
            ShieldRenderer.DrawDyeableShader(spriteBatch);
        }

        public override void SetStaticDefaults() => ItemID.Sets.ExtractinatorMode[Type] = Item.type;

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 30;
            Item.value = CalamityGlobalItem.RarityBlueBuyPrice;
            Item.rare = ItemRarityID.Blue;
            Item.accessory = true;
            Item.MakeUsableWithChlorophyteExtractinator();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityPlayer calamityPlayer = player.Calamity();
            calamityPlayer.roverDrive = true;
            calamityPlayer.roverDriveShieldVisible = !hideVisual;
        }

        public override void UpdateVanity(Player player) => player.Calamity().roverDriveShieldVisible = true;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string adrenalineText = CalamityWorld.revenge
                ? Language.GetTextValue(RoverDriveLocalization + ".ShieldAdren")
                : string.Empty;
            tooltips.FindAndReplace("[ADREN]", adrenalineText);
        }

        public override void ExtractinatorUse(int extractinatorBlockType, ref int resultType, ref int resultStack)
        {
            resultType = ModContent.ItemType<WulfrumMetalScrap>();
            resultStack = Main.rand.Next(3, 6);

            if (Main.rand.NextFloat() > 0.8f)
            {
                resultStack = 1;
                resultType = ModContent.ItemType<EnergyCore>();
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<EnergyCore>()
                .AddIngredient<WulfrumMetalScrap>(2)
                .AddIngredient(ItemID.Chain, 5)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }

    // The pair check applies in either equip order, including modded accessory slots.
    internal sealed class CLRoverDriveConflict : GlobalItem
    {
        public override bool IsLoadingEnabled(Mod mod) => ModLoader.HasMod("CalamityFables");

        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
        {
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity) ||
                !calamity.TryFind<ModItem>("RoverDrive", out ModItem original))
                return true;

            int cloneType = ModContent.ItemType<CL_RoverDrive>();
            int originalType = original.Type;
            return !((equippedItem.type == cloneType && incomingItem.type == originalType) ||
                     (equippedItem.type == originalType && incomingItem.type == cloneType));
        }
    }
}
