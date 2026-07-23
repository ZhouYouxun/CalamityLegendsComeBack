using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.HDMC
{
    /// <summary>
    /// 未编译的矩阵核心：召唤超维矩阵主宰的 Boss 召唤物。
    /// 图标沿用武器的程序化全息绘制，但几何体简化为三棱柱。
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
            DrawTriangularPrismInventory(position, scale * 0.9f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
            ref float rotation, ref float scale, int whoAmI)
        {
            Vector2 center = Item.Center - Main.screenPosition + Vector2.UnitY *
                MathF.Sin(Main.GlobalTimeWrappedHourly * 2.1f) * 1.6f * scale;
            DrawTriangularPrismWorld(center, scale * 0.78f);
            return false;
        }

        // ── Inventory draw (uses Main.UIScaleMatrix, mirrors DrawInventoryIcon) ──────────────────
        private static void DrawTriangularPrismInventory(Vector2 center, float scale)
        {
            float iconScale = MathHelper.Clamp(scale, 0.55f, 1.15f);
            float time      = Main.GlobalTimeWrappedHourly;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;

            // Switch to Additive + UI matrix, same as HyperdimensionalMatrixVisuals.DrawInventoryIcon
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

            // Soft bloom glow
            Color primary   = GetColor(0.55f, 0.82f);   // cyan-teal base (slightly different hue from weapon)
            Color secondary = GetColor(0.38f, 0.52f);

            Main.spriteBatch.Draw(bloom, center, null, primary   * 0.58f, time * 0.38f, bloom.Size() * 0.5f, 0.36f * iconScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(bloom, center, null, secondary * 0.30f, -time * 0.52f, bloom.Size() * 0.5f, 0.60f * iconScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ring,  center, null, primary   * 0.76f, -time * 1.1f,  ring.Size()  * 0.5f, 0.45f * iconScale, SpriteEffects.None, 0f);

            // Scan rings (ellipse dash arcs)
            DrawInventoryScanRing(center, 22f * iconScale,  time * 1.4f,  primary   * 0.72f, 24, 1.2f * iconScale);
            DrawInventoryScanRing(center, 14f * iconScale, -time * 1.9f,  secondary * 0.58f, 18, 0.9f * iconScale);

            // Three-dimensional prism wireframe
            Vector2[] projected = ProjectPrism(center, 17f * iconScale, time);
            for (int i = 0; i < PrismEdges.Length; i++)
            {
                (int s, int e) = PrismEdges[i];
                float pulse = 0.62f + 0.38f * MathF.Sin(time * 4.4f + i * 0.71f);
                Color edgeColor = GetColor(i / (float)PrismEdges.Length, 0.88f * pulse);

                DrawScreenLine(projected[s], projected[e], edgeColor * 0.22f, 4.5f * iconScale);
                DrawScreenLine(projected[s], projected[e], edgeColor,         1.15f * iconScale);

                // Flowing data node along every other edge
                if ((i & 1) == 0)
                {
                    float flow = (time * 0.82f + i * 0.19f) % 1f;
                    DrawScreenNode(Vector2.Lerp(projected[s], projected[e], flow), edgeColor, 2.8f * iconScale);
                }
            }

            // Vertex nodes
            for (int i = 0; i < projected.Length; i++)
                DrawScreenNode(projected[i], GetColor(i / (float)projected.Length, 0.90f), (i < 3 ? 4f : 3f) * iconScale);

            // Centre pixel
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle core = new(
                (int)(center.X - 2f * iconScale),
                (int)(center.Y - 2f * iconScale),
                Math.Max(2, (int)(4f * iconScale)),
                Math.Max(2, (int)(4f * iconScale)));
            Main.spriteBatch.Draw(pixel, core, Color.White with { A = 0 } * 0.82f);

            // Restore alpha-blend + UI matrix
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
        }

        // ── World-drop draw (uses GameViewMatrix, mirrors DrawWorldIcon) ──────────────────────────
        private static void DrawTriangularPrismWorld(Vector2 center, float scale)
        {
            float iconScale = MathHelper.Clamp(scale, 0.45f, 1.05f);
            float time      = Main.GlobalTimeWrappedHourly;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;

            // Switch to Additive + world-space matrix, same as HyperdimensionalMatrixVisuals.DrawWorldIcon
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Color primary   = GetColor(0.55f, 0.72f);
            Color secondary = GetColor(0.38f, 0.44f);

            Main.spriteBatch.Draw(bloom, center, null, primary   * 0.44f, time * 0.38f, bloom.Size() * 0.5f, 0.26f * iconScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(bloom, center, null, secondary * 0.22f, -time * 0.52f, bloom.Size() * 0.5f, 0.50f * iconScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ring,  center, null, primary   * 0.64f, -time * 1.05f, ring.Size()  * 0.5f, 0.35f * iconScale, SpriteEffects.None, 0f);

            DrawInventoryScanRing(center, 18f * iconScale,  time * 1.3f,  primary   * 0.62f, 20, 1.0f * iconScale);
            DrawInventoryScanRing(center, 12f * iconScale, -time * 1.8f,  secondary * 0.48f, 16, 0.8f * iconScale);

            Vector2[] projected = ProjectPrism(center, 14f * iconScale, time);
            for (int i = 0; i < PrismEdges.Length; i++)
            {
                (int s, int e) = PrismEdges[i];
                float pulse = 0.62f + 0.38f * MathF.Sin(time * 4.4f + i * 0.71f);
                Color edgeColor = GetColor(i / (float)PrismEdges.Length, 0.78f * pulse);

                DrawScreenLine(projected[s], projected[e], edgeColor * 0.20f, 4f   * iconScale);
                DrawScreenLine(projected[s], projected[e], edgeColor,         1.05f * iconScale);

                if ((i & 1) == 0)
                {
                    float flow = (time * 0.78f + i * 0.19f) % 1f;
                    DrawScreenNode(Vector2.Lerp(projected[s], projected[e], flow), edgeColor, 2.4f * iconScale);
                }
            }

            for (int i = 0; i < projected.Length; i++)
                DrawScreenNode(projected[i], GetColor(i / (float)projected.Length, 0.82f), (i < 3 ? 3.5f : 2.8f) * iconScale);

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle core = new(
                (int)(center.X - 1.8f * iconScale),
                (int)(center.Y - 1.8f * iconScale),
                Math.Max(2, (int)(3.6f * iconScale)),
                Math.Max(2, (int)(3.6f * iconScale)));
            Main.spriteBatch.Draw(pixel, core, Color.White with { A = 0 } * 0.72f);

            // Restore alpha-blend + world-space matrix
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        // ── Held-item draw (queued into PlayerDrawSet, no SpriteBatch switch needed) ─────────────
        internal static void AddHeldDrawData(ref PlayerDrawSet drawInfo, Player player)
        {
            // The usual held-item pass only sees InvisibleProj, so queue the procedural prism in
            // the player draw cache instead of trying to draw from an item update hook.
            Vector2 center = player.MountedCenter - Main.screenPosition +
                new Vector2(player.direction * 11f, player.gfxOffY - 31f);
            Vector2[] projected = ProjectPrism(center, 16f, Main.GlobalTimeWrappedHourly);
            // PlayerDrawLayer uses the normal alpha-blended draw pass, so these must retain
            // a visible alpha channel (the weapon's additive-renderer convention uses A = 0).
            Color glow = new Color(28, 126, 255, 82);
            Color line = new Color(108, 238, 255, 255);

            foreach ((int start, int end) in PrismEdges)
            {
                AddHeldLine(ref drawInfo, projected[start], projected[end], glow, 5.2f);
                AddHeldLine(ref drawInfo, projected[start], projected[end], line, 1.35f);
            }
        }

        private static void AddHeldLine(ref PlayerDrawSet drawInfo, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 offset = end - start;
            float length = offset.Length();
            if (length <= 0.01f)
                return;

            drawInfo.DrawDataCache.Add(new DrawData(
                TextureAssets.MagicPixel.Value,
                start,
                new Rectangle(0, 0, 1, 1),
                color,
                offset.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(length, width),
                SpriteEffects.None,
                0));
        }

        // ── Geometry helpers ──────────────────────────────────────────────────────────────────────

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

        /// <summary>
        /// Cycling holo-colour, alpha=0 for additive blending (same convention as HyperdimensionalMatrixVisuals.GetDataColor).
        /// </summary>
        private static Color GetColor(float offset, float opacity = 1f)
        {
            float hue = (Main.GlobalTimeWrappedHourly * 0.18f + offset) % 1f;
            Color color = Main.hslToRgb(hue, 0.88f, 0.62f) * opacity;
            color.A = 0;
            return color;
        }

        // Draws a line segment in screen space using CalamityMod's Line texture.
        // Mirrors HyperdimensionalMatrixVisuals.DrawScreenLine exactly.
        private static void DrawScreenLine(Vector2 start, Vector2 end, Color color, float width)
        {
            if (start == end)
                return;

            // Ensure additive-blended colours are visible when alpha channel is zero.
            if (color.A == 0 && (color.R != 0 || color.G != 0 || color.B != 0))
                color.A = 255;

            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Line").Value;
            float rotation = (end - start).ToRotation();
            Vector2 lineScale = new(Vector2.Distance(start, end) / line.Width, width);
            Main.spriteBatch.Draw(line, start, null, color, rotation, line.Size() * Vector2.UnitY * 0.5f, lineScale, SpriteEffects.None, 0f);
        }

        // Draws a cross-hair node in screen space.
        // Mirrors HyperdimensionalMatrixVisuals.DrawScreenNode exactly.
        private static void DrawScreenNode(Vector2 screenPosition, Color color, float size)
        {
            if (color.A == 0 && (color.R != 0 || color.G != 0 || color.B != 0))
                color.A = 255;

            int width = Math.Max(1, (int)size);
            Rectangle horizontal = new(
                (int)(screenPosition.X - width * 0.5f),
                (int)(screenPosition.Y - 1f),
                width,
                2);
            Rectangle vertical = new(
                (int)(screenPosition.X - 1f),
                (int)(screenPosition.Y - width * 0.5f),
                2,
                width);

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, horizontal, color);
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, vertical, color);
        }

        // Ellipse dash-arc scan ring.
        // Mirrors HyperdimensionalMatrixVisuals.DrawInventoryScanRing exactly.
        private static void DrawInventoryScanRing(Vector2 center, float radius, float rotation, Color color, int segments, float width)
        {
            for (int i = 0; i < segments; i++)
            {
                if ((i + (int)(Main.GameUpdateCount / 4)) % 5 == 0)
                    continue;

                float angleA = MathHelper.TwoPi * i / segments + rotation;
                float angleB = MathHelper.TwoPi * (i + 0.68f) / segments + rotation;
                Vector2 a = center + new Vector2((float)Math.Cos(angleA) * radius, (float)Math.Sin(angleA) * radius * 0.52f);
                Vector2 b = center + new Vector2((float)Math.Cos(angleB) * radius, (float)Math.Sin(angleB) * radius * 0.52f);
                DrawScreenLine(a, b, color, width);
            }
        }

        // ── Item use logic ────────────────────────────────────────────────────────────────────────

        public override bool CanUseItem(Player player)
            => !Main.dayTime && !NPC.AnyNPCs(ModContent.NPCType<HDMCSovereign>());

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
