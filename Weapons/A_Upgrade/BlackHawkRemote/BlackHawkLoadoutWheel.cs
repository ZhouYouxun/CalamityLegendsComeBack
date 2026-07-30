using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Systems;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.BlackHawkRemote
{
    internal sealed class BlackHawkLoadoutWheel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        private const int SlotCount = 9;
        private const float SlotRadius = 112f;
        private const float DeadZone = 42f;
        private const int SlotSize = 44;

        private Vector2 screenCenter;
        private bool centerInitialized;
        private bool sawRightHeld;
        private bool committed;
        private int lastHovered = int.MinValue;

        public new string LocalizationCategory => "Projectiles.BlackHawk";
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
            Projectile.width = 320;
            Projectile.height = 320;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.Opacity = 0f;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (owner.HeldItem?.ModItem is not LegendaryBlackHawkRemote remote)
                FadeOut = true;

            if (Main.myPlayer == Projectile.owner)
            {
                if (!centerInitialized)
                {
                    screenCenter = Main.MouseScreen;
                    centerInitialized = true;
                }

                if (Main.mouseRight)
                    sawRightHeld = true;

                if (!committed && sawRightHeld && !Main.mouseRight)
                {
                    BlackHawkLoadout chosen = GetHoveredLoadout(screenCenter);
                    if (owner.HeldItem?.ModItem is LegendaryBlackHawkRemote heldRemote)
                    {
                        heldRemote.SetLoadout(owner, chosen);
                        CombatText.NewText(owner.Hitbox, BlackHawkLoadoutInfo.Color(chosen), BlackHawkLoadoutInfo.Name(chosen));
                        SoundEngine.PlaySound(SoundID.Item23 with { Volume = 0.54f, Pitch = 0.12f }, owner.Center);
                    }

                    committed = true;
                    FadeOut = true;
                }
            }

            Projectile.Center = Main.myPlayer == Projectile.owner
                ? Main.screenPosition + screenCenter
                : owner.Center;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.16f : 0.18f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner || !centerInitialized)
                return false;

            Player owner = Main.player[Projectile.owner];
            BlackHawkLoadout current = owner.HeldItem?.ModItem is LegendaryBlackHawkRemote remote
                ? remote.SelectedLoadout
                : BlackHawkLoadout.Auto;
            BlackHawkLoadout hovered = GetHoveredLoadout(screenCenter);
            float opacity = Projectile.Opacity;
            float time = Main.GlobalTimeWrappedHourly;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/HollowCircleHardEdge").Value;

            Main.EntitySpriteDraw(ring, screenCenter, null, new Color(82, 150, 186, 0) * (0.28f * opacity),
                -time * 0.25f, ring.Size() * 0.5f, 2.04f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(ring, screenCenter, null, new Color(255, 139, 70, 0) * (0.18f * opacity),
                time * 0.18f, ring.Size() * 0.5f, 1.62f, SpriteEffects.None, 0f);

            DrawSectorLines(screenCenter, hovered, opacity);

            for (int i = 0; i < SlotCount; i++)
            {
                BlackHawkLoadout loadout = (BlackHawkLoadout)i;
                Vector2 slotCenter = screenCenter + SlotDirection(i) * SlotRadius;
                bool isHovered = hovered == loadout;
                bool isCurrent = current == loadout;
                Color color = BlackHawkLoadoutInfo.Color(loadout);
                Rectangle area = Utils.CenteredRectangle(slotCenter, new Vector2(SlotSize));

                DrawSlot(area, color, isCurrent, isHovered, opacity);
                float glowScale = (isHovered ? 0.20f : isCurrent ? 0.16f : 0.11f) * (1f + 0.04f * (float)Math.Sin(time * 7f + i));
                Main.EntitySpriteDraw(bloom, slotCenter, null, BlackHawkVFX.Additive(color) * ((isHovered ? 0.72f : 0.38f) * opacity),
                    0f, bloom.Size() * 0.5f, glowScale, SpriteEffects.None, 0f);

                string code = BlackHawkLoadoutInfo.ShortCode(loadout);
                DrawCenteredText(code, area, isHovered ? Color.White : color, 0.48f, opacity);
            }

            Rectangle centerArea = Utils.CenteredRectangle(screenCenter, new Vector2(68f));
            Color autoColor = BlackHawkLoadoutInfo.Color(BlackHawkLoadout.Auto);
            DrawSlot(centerArea, autoColor, current == BlackHawkLoadout.Auto, hovered == BlackHawkLoadout.Auto, opacity);
            DrawCenteredText("AUTO", centerArea, hovered == BlackHawkLoadout.Auto ? Color.White : autoColor, 0.52f, opacity);

            string title = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.BlackHawkWheelTitle");
            string name = BlackHawkLoadoutInfo.Name(hovered);
            DrawCenteredText(title, new Rectangle((int)screenCenter.X - 180, (int)screenCenter.Y - 178, 360, 26),
                new Color(210, 230, 240), 0.58f, opacity);
            DrawCenteredText(name, new Rectangle((int)screenCenter.X - 190, (int)screenCenter.Y + 151, 380, 28),
                BlackHawkLoadoutInfo.Color(hovered), 0.62f, opacity);

            int hoveredIndex = (int)hovered;
            if (hoveredIndex != lastHovered && Projectile.Opacity >= 0.9f)
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.28f, Pitch = 0.22f }, owner.Center);
            lastHovered = hoveredIndex;

            if (Projectile.Opacity > 0.08f)
            {
                Main.blockMouse = true;
                owner.mouseInterface = true;
            }

            return false;
        }

        private static BlackHawkLoadout GetHoveredLoadout(Vector2 center)
        {
            Vector2 offset = Main.MouseScreen - center;
            if (offset.LengthSquared() <= DeadZone * DeadZone)
                return BlackHawkLoadout.Auto;

            Vector2 direction = offset.SafeNormalize(-Vector2.UnitY);
            int bestIndex = 0;
            float bestDot = -2f;
            for (int i = 0; i < SlotCount; i++)
            {
                float dot = Vector2.Dot(direction, SlotDirection(i));
                if (dot <= bestDot)
                    continue;
                bestDot = dot;
                bestIndex = i;
            }
            return (BlackHawkLoadout)bestIndex;
        }

        private static Vector2 SlotDirection(int index) =>
            (-MathHelper.PiOver2 + MathHelper.TwoPi * index / SlotCount).ToRotationVector2();

        private static void DrawSectorLines(Vector2 center, BlackHawkLoadout hovered, float opacity)
        {
            float radius = 148f;
            for (int i = 0; i < SlotCount; i++)
            {
                float boundaryAngle = -MathHelper.PiOver2 + MathHelper.TwoPi * (i - 0.5f) / SlotCount;
                Vector2 end = center + boundaryAngle.ToRotationVector2() * radius;
                DrawScreenLine(center + boundaryAngle.ToRotationVector2() * 48f, end,
                    new Color(105, 145, 164, 0) * (0.26f * opacity), 1.2f);
            }

            if (BlackHawkLoadoutInfo.IsWeapon(hovered))
            {
                Vector2 direction = SlotDirection((int)hovered);
                DrawScreenLine(center + direction * 44f, center + direction * 139f,
                    BlackHawkVFX.Additive(BlackHawkLoadoutInfo.Color(hovered)) * (0.62f * opacity), 2.8f);
            }
        }

        private static void DrawSlot(Rectangle area, Color color, bool current, bool hovered, float opacity)
        {
            Color fill = Color.Lerp(new Color(8, 14, 19), color, current ? 0.22f : 0.08f);
            Color border = Color.Lerp(new Color(72, 92, 106), color, current ? 0.62f : 0.30f);
            if (hovered)
            {
                fill = Color.Lerp(fill, color, 0.24f);
                border = Color.Lerp(border, Color.White, 0.38f);
            }

            DrawRectangle(area, fill * (0.92f * opacity));
            DrawRectangle(new Rectangle(area.X, area.Y, area.Width, 3), Color.Lerp(fill, Color.White, 0.22f) * opacity);
            DrawBorder(area, border * opacity, hovered || current ? 2 : 1);
        }

        private static void DrawCenteredText(string text, Rectangle area, Color color, float desiredScale, float opacity)
        {
            Vector2 size = FontAssets.MouseText.Value.MeasureString(text);
            float scale = Math.Min(desiredScale, area.Width / Math.Max(1f, size.X));
            Vector2 position = new(area.Center.X - size.X * scale * 0.5f, area.Center.Y - size.Y * scale * 0.5f);
            CalamityUtils.DrawBorderStringEightWay(Main.spriteBatch, FontAssets.MouseText.Value, text, position,
                color * opacity, Color.Black * (0.78f * opacity), scale);
        }

        private static void DrawScreenLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 edge = end - start;
            if (edge.LengthSquared() <= 0.01f)
                return;
            Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, start, new Rectangle(0, 0, 1, 1), color,
                edge.ToRotation(), new Vector2(0f, 0.5f), new Vector2(edge.Length(), thickness), SpriteEffects.None, 0f);
        }

        private static void DrawRectangle(Rectangle rectangle, Color color) =>
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rectangle, color);

        private static void DrawBorder(Rectangle rectangle, Color color, int thickness)
        {
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
            DrawRectangle(new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height), color);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
        }
    }
}
