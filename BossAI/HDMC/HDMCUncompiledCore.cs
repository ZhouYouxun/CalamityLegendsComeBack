using System;
using CalamityMod;
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
        private static readonly Vector3[] PrismVertices =
        {
            new Vector3(0f, -1f, -0.65f),
            new Vector3(0.866f, 0.5f, -0.65f),
            new Vector3(-0.866f, 0.5f, -0.65f),
            new Vector3(0f, -1f, 0.65f),
            new Vector3(0.866f, 0.5f, 0.65f),
            new Vector3(-0.866f, 0.5f, 0.65f)
        };

        private static readonly (int Start, int End)[] PrismEdges =
        {
            (0, 1), (1, 2), (2, 0),
            (3, 4), (4, 5), (5, 3),
            (0, 3), (1, 4), (2, 5)
        };

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
            DrawTriangularPrism(position, scale * 0.9f, true);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
            ref float rotation, ref float scale, int whoAmI)
        {
            Vector2 center = Item.Center - Main.screenPosition + Vector2.UnitY *
                MathF.Sin(Main.GlobalTimeWrappedHourly * 2.1f) * 1.6f * scale;
            DrawTriangularPrism(center, scale * 0.78f, false);
            return false;
        }

        // The Matrix Core's 3D rotation and perspective projection, reduced to one triangular prism and nine edges.
        private static void DrawTriangularPrism(Vector2 center, float scale, bool inventory)
        {
            float iconScale = MathHelper.Clamp(scale, 0.55f, 1.15f);
            float opacity = inventory ? 1f : 0.76f;
            Color lineColor = new Color(91, 224, 255) * opacity;
            Vector2[] projected = ProjectPrism(center, 18f * iconScale, Main.GlobalTimeWrappedHourly);

            foreach ((int start, int end) in PrismEdges)
                DrawProjectedEdge(projected[start], projected[end], lineColor, 1.2f * iconScale);
        }

        private static Vector2[] ProjectPrism(Vector2 center, float radius, float time)
        {
            Matrix rotation = Matrix.CreateFromYawPitchRoll(time * 0.94f, time * 0.67f, time * 0.43f);
            Vector2[] projected = new Vector2[PrismVertices.Length];
            Vector2 projectedCenter = Vector2.Zero;

            for (int i = 0; i < PrismVertices.Length; i++)
            {
                Vector3 normalized = PrismVertices[i];
                normalized.Normalize();
                Vector3 point = Vector3.Transform(normalized * radius, rotation);
                float perspective = 620f / Math.Max(180f, 620f + point.Z);
                projected[i] = center + new Vector2(point.X, point.Y) * perspective;
                projectedCenter += projected[i];
            }

            Vector2 correction = center - projectedCenter / projected.Length;
            for (int i = 0; i < projected.Length; i++)
                projected[i] += correction;

            return projected;
        }

        // DrawLineBetter is the Matrix Core's endpoint-bounded renderer; no line can extend beyond start and end.
        private static void DrawProjectedEdge(Vector2 start, Vector2 end, Color color, float width)
        {
            if (Vector2.DistanceSquared(start, end) <= 0.0001f)
                return;

            Main.spriteBatch.DrawLineBetter(start, end, color, width);
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
                .AddRecipeGroup("AnyGoldBar", 10)
                .AddIngredient(ItemID.Wire, 25)
                .Register();
        }
    }
}
