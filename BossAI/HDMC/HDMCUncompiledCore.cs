using System;
using CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
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
            DrawTriangularCore(position, scale * 0.9f, true);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
            ref float rotation, ref float scale, int whoAmI)
        {
            Vector2 center = Item.Center - Main.screenPosition + Vector2.UnitY *
                MathF.Sin(Main.GlobalTimeWrappedHourly * 2.1f) * 1.6f * scale;
            DrawTriangularCore(center, scale * 0.78f, false);
            return false;
        }

        // The summon uses a deliberately spare triangular silhouette so it cannot be mistaken for the Matrix Core weapon.
        private static void DrawTriangularCore(Vector2 center, float scale, bool inventory)
        {
            float time = Main.GlobalTimeWrappedHourly;
            float iconScale = MathHelper.Clamp(scale, 0.55f, 1.25f);
            float outerRadius = 22f * iconScale;
            float innerRadius = outerRadius * 0.48f;
            float rotation = -MathHelper.PiOver2 + time * 0.18f;
            float opacity = inventory ? 1f : 0.76f;

            Color outerColor = Color.Lerp(new Color(44, 224, 255), new Color(174, 94, 255),
                0.5f + 0.5f * MathF.Sin(time * 1.4f)) * opacity;
            Color innerColor = Color.Lerp(new Color(255, 124, 226), new Color(80, 245, 255),
                0.5f + 0.5f * MathF.Sin(time * 1.4f + 1.7f)) * opacity;

            Vector2[] outer = new Vector2[3];
            Vector2[] inner = new Vector2[3];
            for (int i = 0; i < 3; i++)
            {
                float angle = rotation + MathHelper.TwoPi * i / 3f;
                outer[i] = center + angle.ToRotationVector2() * outerRadius;
                inner[i] = center + (angle + MathHelper.Pi / 3f).ToRotationVector2() * innerRadius;
            }

            for (int i = 0; i < 3; i++)
            {
                int next = (i + 1) % 3;
                DrawHologramLine(outer[i], outer[next], outerColor * 0.2f, 5.2f * iconScale);
                DrawHologramLine(outer[i], outer[next], outerColor, 1.5f * iconScale);
                DrawHologramLine(inner[i], inner[next], innerColor * 0.7f, 1.15f * iconScale);

                float nodePulse = 1f + 0.25f * MathF.Sin(time * 4f + i * 2.1f);
                Vector2 node = outer[i];
                DrawHologramLine(node - Vector2.UnitX * 2.5f * iconScale,
                    node + Vector2.UnitX * 2.5f * iconScale, Color.White * (0.8f * opacity), 1.1f * nodePulse);
                DrawHologramLine(node - Vector2.UnitY * 2.5f * iconScale,
                    node + Vector2.UnitY * 2.5f * iconScale, Color.White * (0.8f * opacity), 1.1f * nodePulse);
            }

            float scanProgress = (time * 0.7f) % 1f;
            Vector2 scanStart = Vector2.Lerp(outer[1], outer[2], scanProgress);
            DrawHologramLine(scanStart, center, innerColor * 0.6f, 0.9f * iconScale);
            DrawHologramLine(center - Vector2.UnitX * 2.2f * iconScale,
                center + Vector2.UnitX * 2.2f * iconScale, Color.White * (0.9f * opacity), 1.3f * iconScale);
        }

        private static void DrawHologramLine(Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 edge = end - start;
            float length = edge.Length();
            if (length <= 0.01f)
                return;

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, start, null, color, edge.ToRotation(),
                Vector2.Zero, new Vector2(length, width), SpriteEffects.None, 0f);
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
