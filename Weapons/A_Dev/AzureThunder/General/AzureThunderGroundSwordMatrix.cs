using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    // 手持青霆剑时显示在玩家左侧的地剑数量矩阵。
    internal sealed class AzureThunderGroundSwordMatrix : ModProjectile, IScreenOverlayProjectile
    {
        private const float SlotSpacing = 12f;
        private const float SlotSize = 9f;
        private const float CenterLeftOffset = 92f;

        private static readonly Vector2[] SlotOffsets =
        {
            new(0f, -2f),
            new(-1f, -1f),
            new(1f, -1f),
            new(-2f, 0f),
            new(2f, 0f),
            new(-1f, 1f),
            new(1f, 1f),
            new(0f, 2f)
        };

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.Opacity = 0f;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (owner.HeldItem?.type != ModContent.ItemType<AzureThunder>())
                FadeOut = true;

            Projectile.Center = owner.Center;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.12f : 0.18f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            Vector2 matrixCenter = owner.Center - Main.screenPosition + new Vector2(-CenterLeftOffset, owner.gfxOffY - 8f);
            int swordCount = AzureThunderPlayer.CountOwnedAzureThunderSwords(owner);
            float opacity = Projectile.Opacity;
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5.6f);

            DrawConnectors(matrixCenter, opacity);

            for (int i = 0; i < SlotOffsets.Length; i++)
            {
                bool active = i < swordCount;
                Vector2 slotCenter = matrixCenter + SlotOffsets[i] * SlotSpacing;
                DrawSlot(slotCenter, active, opacity, pulse, i);
            }

            if (swordCount > SlotOffsets.Length)
                DrawOverflowCore(matrixCenter, opacity, pulse);

            return false;
        }

        private static void DrawConnectors(Vector2 matrixCenter, float opacity)
        {
            Color lineColor = new Color(58, 255, 214) * (0.18f * opacity);
            for (int i = 0; i < SlotOffsets.Length; i++)
            {
                Vector2 start = matrixCenter + SlotOffsets[i] * SlotSpacing;
                for (int j = i + 1; j < SlotOffsets.Length; j++)
                {
                    if (Vector2.DistanceSquared(SlotOffsets[i], SlotOffsets[j]) > 2.1f)
                        continue;

                    Vector2 end = matrixCenter + SlotOffsets[j] * SlotSpacing;
                    DrawLine(start, end, lineColor, 1f);
                }
            }
        }

        private static void DrawSlot(Vector2 center, bool active, float opacity, float pulse, int index)
        {
            Color inactiveFill = new(18, 28, 35, 210);
            Color inactiveBorder = new(70, 105, 118, 190);
            Color activeFill = Color.Lerp(new Color(35, 255, 192), new Color(225, 255, 238), pulse * 0.35f);
            Color activeBorder = Color.Lerp(new Color(96, 255, 226), Color.White, 0.22f + pulse * 0.18f);
            float wave = active ? 1f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f + index) : 1f;

            DrawDiamond(center, SlotSize * 1.42f * wave, active ? activeBorder * (0.38f * opacity) : inactiveBorder * (0.28f * opacity));
            DrawDiamond(center, SlotSize * wave, active ? activeFill * (0.78f * opacity) : inactiveFill * (0.62f * opacity));
            DrawDiamond(center, SlotSize * 0.48f * wave, active ? Color.White * (0.72f * opacity) : inactiveBorder * (0.35f * opacity));
        }

        private static void DrawOverflowCore(Vector2 center, float opacity, float pulse)
        {
            Color core = Color.Lerp(new Color(255, 238, 126), Color.White, pulse * 0.45f);
            DrawDiamond(center, SlotSize * 1.7f, core * (0.34f * opacity));
            DrawDiamond(center, SlotSize * 0.92f, core * (0.88f * opacity));
        }

        private static void DrawDiamond(Vector2 center, float size, Color color)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.EntitySpriteDraw(
                pixel,
                center,
                new Rectangle(0, 0, 1, 1),
                color,
                MathHelper.PiOver4,
                new Vector2(0.5f),
                new Vector2(size),
                SpriteEffects.None,
                0f);
        }

        private static void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 edge = end - start;
            if (edge.LengthSquared() <= 0.001f)
                return;

            Main.EntitySpriteDraw(
                TextureAssets.MagicPixel.Value,
                start,
                new Rectangle(0, 0, 1, 1),
                color,
                edge.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(edge.Length(), thickness),
                SpriteEffects.None,
                0f);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
        }
    }
}
