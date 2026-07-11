using CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.HDMC
{
    /// <summary>
    /// 未编译的矩阵核心：召唤超维矩阵主宰的 Boss 召唤物。
    /// 图标沿用武器的程序化全息绘制。
    /// </summary>
    public sealed class HDMCUncompiledCore : ModItem
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 12;
        }

        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 48;
            Item.useTime = 45;
            Item.useAnimation = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.consumable = false;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Purple;
            Item.UseSound = SoundID.Item119;
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            HyperdimensionalMatrixVisuals.DrawInventoryIcon(position, scale * 0.85f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
            ref float rotation, ref float scale, int whoAmI)
        {
            HyperdimensionalMatrixVisuals.DrawWorldIcon(Item.Center - Main.screenPosition, scale * 0.85f);
            return false;
        }

        public override bool CanUseItem(Player player)
            => !NPC.AnyNPCs(ModContent.NPCType<HDMCSovereign>());

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                SoundEngine.PlaySound(SoundID.Roar, player.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPos = player.Center - Vector2.UnitY * 420f;
                    int npc = NPC.NewNPC(player.GetSource_ItemUse(Item),
                        (int)spawnPos.X, (int)spawnPos.Y, ModContent.NPCType<HDMCSovereign>());
                    if (npc >= 0 && npc < Main.maxNPCs)
                    {
                        Main.npc[npc].target = player.whoAmI;
                        Main.npc[npc].netUpdate = true;
                    }
                }
                else
                {
                    NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<HDMCSovereign>());
                }
            }

            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.LunarBar, 10)
                .AddIngredient(ItemID.FragmentSolar, 6)
                .AddIngredient(ItemID.FragmentVortex, 6)
                .AddIngredient(ItemID.FragmentNebula, 6)
                .AddIngredient(ItemID.FragmentStardust, 6)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
