using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.UI
{
    // 轮盘键按住时显示的5格印记选择轮盘，释放时选中悬停项作为当前印记。
    // 布局与 BFSelectionPanel 相同（正五边形），每个格显示boss头像。
    internal sealed class PFMarkSelectionWheel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        // Polygon offsets for up to 7 slots arranged in a regular polygon (radius 66).
        // Slots 0-4 = pentagon; slots 5-6 = heptagon positions added when lily >= 3.
        private static readonly Vector2[] SlotOffsets =
        {
            new(0f, -66f),       // top
            new(63f, -20f),      // upper-right
            new(39f, 54f),       // lower-right
            new(-39f, 54f),      // lower-left
            new(-63f, -20f),     // upper-left
            new(-52f, -41f),     // heptagon slot 6 (upper-left extension)
            new(52f, -41f),      // heptagon slot 7 (upper-right extension)
        };

        private const float DeadZone = 16f;
        private const int SlotFrameSize = 52;
        private const int InnerFrameSize = 40;
        private const int BorderThickness = 2;
        private const float IconScale = 1.0f;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private Vector2 screenCenter;
        private bool centerInitialized;
        private readonly int[] clickFeedbackTimers = new int[PristineFuryPlayer.MaxMarkSlots];
        private readonly bool[] hoveredLastFrame = new bool[PristineFuryPlayer.MaxMarkSlots];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 160;
            Projectile.height = 160;
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

            if (owner.HeldItem?.ModItem is not NewLegendPristineFury)
                FadeOut = true;

            if (Main.myPlayer == Projectile.owner &&
                KeybindSystem.LegendaryWeaponFormSwitch?.Current != true &&
                KeybindSystem.LegendaryWeaponFormSwitch?.JustReleased != true)
            {
                FadeOut = true;
            }

            if (!centerInitialized && Main.myPlayer == Projectile.owner)
            {
                screenCenter = Main.MouseScreen;
                centerInitialized = true;
            }

            Projectile.Center = Main.myPlayer == Projectile.owner
                ? Main.screenPosition + screenCenter
                : owner.Center;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.14f : 0.14f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            PristineFuryPlayer pfPlayer = owner.GetModPlayer<PristineFuryPlayer>();
            float opacity = Projectile.Opacity;
            float time = Main.GlobalTimeWrappedHourly;
            Vector2 drawPos = screenCenter.Floor();

            Texture2D innerRing = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_03").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D halfStar = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;

            int hoveredIndex = GetHoveredIndex(drawPos);
            bool keyReleased = KeybindSystem.LegendaryWeaponFormSwitch?.JustReleased == true;

            // Center decoration (PristineFury dark-gold theme).
            Color decorInner = new Color(255, 200, 80, 0) * (0.22f * opacity);
            Main.EntitySpriteDraw(innerRing, drawPos, null, decorInner, -time * 0.5f,
                innerRing.Size() * 0.5f, 0.38f * Projectile.scale, SpriteEffects.None, 0f);

            // Center bloom highlight for selected mark.
            if (pfPlayer.SelectedMarkIndex >= 0 && pfPlayer.SelectedMarkIndex < pfPlayer.MarkQueueCount)
            {
                Color selColor = PristineFuryMarkHelper.GetColor(pfPlayer.MarkQueue[pfPlayer.SelectedMarkIndex]);
                Main.EntitySpriteDraw(bloom, drawPos, null,
                    new Color(selColor.R, selColor.G, selColor.B, 140) * (0.38f * opacity), 0f,
                    bloom.Size() * 0.5f, 0.20f, SpriteEffects.None, 0f);
            }

            // Sector separator lines.
            DrawSectorLines(drawPos, hoveredIndex, pfPlayer, opacity);

            int wheelSlots = Math.Min(pfPlayer.EffectiveMaxSlots, SlotOffsets.Length);

            // Slot icons.
            for (int i = 0; i < wheelSlots; i++)
            {
                bool filled = i < pfPlayer.MarkQueueCount;
                bool selected = (i == pfPlayer.SelectedMarkIndex);
                bool hovered = (i == hoveredIndex);
                Vector2 slotCenter = drawPos + SlotOffsets[i] * Projectile.scale;

                if (hovered && !hoveredLastFrame[i] && Projectile.Opacity >= 1f)
                    SoundEngine.PlaySound(SoundID.Item55 with { Volume = 0.38f, Pitch = 0.15f }, owner.Center);

                Rectangle slotArea = Utils.CenteredRectangle(slotCenter, new Vector2(SlotFrameSize) * Projectile.scale);
                DrawSlotFrame(slotArea, filled ? PristineFuryMarkHelper.GetColor(pfPlayer.MarkQueue[i < pfPlayer.MarkQueueCount ? i : 0]) : new Color(70, 65, 90),
                    filled, selected, hovered, clickFeedbackTimers[i], opacity);

                // Boss head icon or colored bloom fallback inside slot.
                if (filled)
                {
                    PristineFuryMark mark = pfPlayer.MarkQueue[i];
                    Texture2D icon = PristineFuryMarkHelper.TryGetMarkBossHeadTexture(mark);
                    Color markColor = PristineFuryMarkHelper.GetColor(mark);
                    float slotOpacity = hovered ? opacity : opacity * 0.85f;
                    float slotScale = 1f + (selected ? 0.06f : 0f) + (hovered ? 0.06f : 0f) + (clickFeedbackTimers[i] > 0 ? 0.04f : 0f);

                    if (icon != null)
                    {
                        float maxDim = Math.Max(icon.Width, icon.Height);
                        float iconDrawScale = (InnerFrameSize * 0.82f) / maxDim * slotScale * Projectile.scale;
                        Main.EntitySpriteDraw(icon, slotCenter, null, Color.White * slotOpacity,
                            0f, icon.Size() * 0.5f, iconDrawScale, SpriteEffects.None, 0);
                    }
                    else
                    {
                        // Fallback: colored bloom + rotating HalfStars.
                        Main.EntitySpriteDraw(bloom, slotCenter, null,
                            new Color(markColor.R, markColor.G, markColor.B, 160) * (0.62f * slotOpacity),
                            time * 0.4f, bloom.Size() * 0.5f, 0.20f * slotScale * Projectile.scale,
                            SpriteEffects.None, 0);

                        // Two thin HalfStars as the visual identifier.
                        for (int s = 0; s < 2; s++)
                        {
                            float rot = time * 0.9f + s * MathHelper.Pi;
                            Main.EntitySpriteDraw(halfStar, slotCenter, null,
                                new Color(markColor.R, markColor.G, markColor.B, 200) * (0.72f * slotOpacity),
                                rot, halfStar.Size() * 0.5f,
                                new Vector2(0.12f, 0.50f * slotScale) * Projectile.scale,
                                SpriteEffects.None, 0);
                        }
                    }

                    // Mark name label below the slot.
                    if (hovered || selected)
                    {
                        string name = PristineFuryMarkHelper.GetName(mark);
                        Vector2 namePos = slotCenter + new Vector2(0f, SlotFrameSize * 0.5f * Projectile.scale + 10f);
                        Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, name,
                            namePos.X, namePos.Y, markColor * opacity, Color.Black * opacity, Vector2.One * 0.5f, 0.82f);
                    }
                }
                else
                {
                    // Empty slot: "—" text.
                    Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, "—",
                        slotCenter.X, slotCenter.Y, new Color(80, 70, 100) * opacity,
                        Color.Black * opacity, Vector2.One * 0.5f, 0.8f);
                }

                hoveredLastFrame[i] = hovered;
                if (clickFeedbackTimers[i] > 0)
                    clickFeedbackTimers[i]--;
            }

            // On key release: commit selection.
            if (keyReleased && Projectile.Opacity > 0.08f)
            {
                if (hoveredIndex >= 0 && hoveredIndex < pfPlayer.MarkQueueCount)
                {
                    pfPlayer.SelectMark(hoveredIndex);
                    clickFeedbackTimers[hoveredIndex] = 10;
                    PristineFuryMark chosen = pfPlayer.MarkQueue[hoveredIndex];
                    string name = PristineFuryMarkHelper.GetName(chosen);
                    CombatText.NewText(owner.Hitbox, PristineFuryMarkHelper.GetColor(chosen), name, dramatic: false);
                    SoundEngine.PlaySound(SoundID.Item23 with { Pitch = 0.06f, Volume = 0.68f }, owner.Center);
                }
                else
                {
                    SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.38f, Pitch = 0.18f }, owner.Center);
                }

                FadeOut = true;
            }

            if (Projectile.Opacity > 0.08f)
            {
                Main.blockMouse = true;
                owner.mouseInterface = true;
            }

            return false;
        }

        private int GetHoveredIndex(Vector2 center)
        {
            Vector2 mouseOffset = Main.MouseScreen - center;
            if (mouseOffset.LengthSquared() < DeadZone * DeadZone)
                return -1;

            Vector2 mouseDir = mouseOffset.SafeNormalize(-Vector2.UnitY);
            int best = 0;
            float bestDot = -2f;
            for (int i = 0; i < SlotOffsets.Length; i++)
            {
                float dot = Vector2.Dot(mouseDir, SlotOffsets[i].SafeNormalize(-Vector2.UnitY));
                if (dot > bestDot) { bestDot = dot; best = i; }
            }
            return best;
        }

        private void DrawSectorLines(Vector2 center, int hoveredIndex, PristineFuryPlayer pfPlayer, float opacity)
        {
            Color lineColor = new Color(180, 150, 70, 0) * (0.24f * opacity);
            float radius = 86f * Projectile.scale;

            for (int i = 0; i < SlotOffsets.Length; i++)
            {
                Vector2 cur = SlotOffsets[i].SafeNormalize(-Vector2.UnitY);
                Vector2 nxt = SlotOffsets[(i + 1) % SlotOffsets.Length].SafeNormalize(-Vector2.UnitY);
                Vector2 boundary = (cur + nxt).SafeNormalize(cur);
                DrawScreenLine(center, center + boundary * radius, lineColor, 1.4f);
            }

            if (hoveredIndex >= 0 && hoveredIndex < pfPlayer.MarkQueueCount)
            {
                Color markColor = PristineFuryMarkHelper.GetColor(pfPlayer.MarkQueue[hoveredIndex]);
                Vector2 dir = SlotOffsets[hoveredIndex].SafeNormalize(-Vector2.UnitY);
                DrawScreenLine(center, center + dir * (radius * 0.9f),
                    (markColor with { A = 0 }) * (0.52f * opacity), 3.0f);
            }
        }

        private static void DrawSlotFrame(Rectangle slotArea, Color markColor, bool filled,
            bool selected, bool hovered, int clickTimer, float opacity)
        {
            Color slotBack = filled
                ? Color.Lerp(new Color(18, 12, 28), markColor, selected ? 0.26f : 0.14f)
                : new Color(30, 26, 40);
            Color slotBorder = filled
                ? Color.Lerp(new Color(110, 88, 140), markColor, selected ? 0.55f : 0.30f)
                : new Color(72, 65, 90);

            if (hovered)
            {
                slotBack = Color.Lerp(slotBack, new Color(70, 58, 98), 0.45f);
                slotBorder = Color.Lerp(slotBorder, Color.White, 0.32f);
            }
            if (clickTimer > 0)
            {
                slotBack = Color.Lerp(slotBack, new Color(120, 108, 58), 0.36f);
                slotBorder = Color.Lerp(slotBorder, new Color(255, 230, 140), 0.52f);
            }

            DrawRectangle(slotArea, slotBack * (opacity * 0.82f));
            DrawBorder(slotArea, slotBorder * (opacity * 0.92f), BorderThickness);

            Rectangle innerArea = Utils.CenteredRectangle(slotArea.Center.ToVector2(), new Vector2(InnerFrameSize));
            Color innerBack = filled
                ? Color.Lerp(new Color(10, 8, 18), markColor, 0.09f)
                : new Color(16, 14, 22);
            DrawRectangle(innerArea, innerBack * (opacity * 0.76f));
            DrawBorder(innerArea, slotBorder * (opacity * 0.68f), 1);
        }

        private static void DrawScreenLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 edge = end - start;
            if (edge.LengthSquared() < 0.01f) return;
            Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, start, new Rectangle(0, 0, 1, 1),
                color, edge.ToRotation(), new Vector2(0f, 0.5f), new Vector2(edge.Length(), thickness),
                SpriteEffects.None, 0);
        }

        private static void DrawRectangle(Rectangle r, Color c)
            => Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, r, c);

        private static void DrawBorder(Rectangle r, Color c, int t)
        {
            DrawRectangle(new Rectangle(r.X, r.Y, r.Width, t), c);
            DrawRectangle(new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
            DrawRectangle(new Rectangle(r.X, r.Y, t, r.Height), c);
            DrawRectangle(new Rectangle(r.Right - t, r.Y, t, r.Height), c);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
        }
    }
}
