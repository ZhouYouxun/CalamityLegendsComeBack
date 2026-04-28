using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace CalamityLegendsComeBack.Weapons.A_Dev.SHPBow
{
    internal sealed class SHPBowSelectionPanel : ModProjectile, ILocalizedModType
    {
        private const float IconScale = 0.78f;

        private static readonly SHPBowMode[] Modes =
        {
            SHPBowMode.Pierce,
            SHPBowMode.Ricochet,
            SHPBowMode.Scatter,
            SHPBowMode.Homing
        };

        private static readonly Vector2[] IconOffsets =
        {
            new(0f, -56f),
            new(56f, 0f),
            new(0f, 56f),
            new(-56f, 0f)
        };

        private readonly int[] clickFeedbackTimers = new int[SHPBowModeHelpers.Count];
        private readonly bool[] hoveredLastFrame = new bool[SHPBowModeHelpers.Count];

        private Vector2 playerOffset;
        private bool offsetInitialized;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.A_Dev";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static Rectangle MouseRectangle => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 2, 2);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 148;
            Projectile.height = 148;
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

            if (owner.HeldItem.type != ModContent.ItemType<SHPBow>())
                FadeOut = true;

            if (!offsetInitialized && Main.myPlayer == Projectile.owner)
            {
                playerOffset = Main.MouseWorld - owner.Center;
                offsetInitialized = true;
            }

            Projectile.Center = owner.Center + playerOffset;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.14f : 0.16f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            SHPBowPlayer bowPlayer = owner.GetModPlayer<SHPBowPlayer>();
            Vector2 drawPosition = (owner.Center + playerOffset - Main.screenPosition).Floor();
            DrawPanelRings(drawPosition, bowPlayer.SequenceAccentMode, Projectile.Opacity);
            DrawSequencePreview(drawPosition, bowPlayer, Projectile.Opacity);

            bool hoveringOverAnySlot = false;
            bool leftClickPressed = Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClickPressed = Main.mouseRight && Main.mouseRightRelease;
            SHPBowMode? appendMode = null;
            SHPBowMode? resetMode = null;

            for (int i = 0; i < Modes.Length; i++)
            {
                SHPBowMode mode = Modes[i];
                Texture2D iconTexture = ModContent.Request<Texture2D>(SHPBowModeHelpers.IconTexturePath(mode)).Value;
                Vector2 iconCenter = drawPosition + IconOffsets[i] * Projectile.scale;
                Rectangle iconArea = Utils.CenteredRectangle(iconCenter, iconTexture.Size() * IconScale * Projectile.scale);
                bool hovered = iconArea.Intersects(MouseRectangle);
                int coreCount = bowPlayer.CountMode(mode);
                bool selected = coreCount > 0;

                if (hovered)
                {
                    hoveringOverAnySlot = true;
                    if (!hoveredLastFrame[i] && Projectile.Opacity >= 0.95f)
                        SoundEngine.PlaySound(SoundID.Item55 with { Volume = 0.36f, Pitch = 0.12f }, owner.Center);

                    if (leftClickPressed && Projectile.Opacity >= 0.95f)
                    {
                        appendMode = mode;
                        clickFeedbackTimers[i] = 10;
                    }
                    else if (rightClickPressed && Projectile.Opacity >= 0.95f)
                    {
                        resetMode = mode;
                        clickFeedbackTimers[i] = 10;
                    }
                }

                float scale = IconScale;
                if (selected)
                    scale += 0.06f + coreCount * 0.025f;
                if (hovered)
                    scale += 0.08f;
                if (clickFeedbackTimers[i] > 0)
                    scale += 0.06f;

                Color iconColor = selected ? SHPBowModeHelpers.AccentColor(mode) : Color.White;
                if (hovered)
                    iconColor = Color.Lerp(iconColor, Color.White, 0.35f);
                if (clickFeedbackTimers[i] > 0)
                    iconColor = Color.Lerp(iconColor, new Color(255, 240, 170), 0.4f);

                DrawIconEnergyOutline(iconTexture, iconCenter, mode, scale * Projectile.scale, selected, hovered, clickFeedbackTimers[i], Projectile.Opacity);

                Main.EntitySpriteDraw(
                    iconTexture,
                    iconCenter,
                    null,
                    iconColor * Projectile.Opacity,
                    selected ? Main.GlobalTimeWrappedHourly * 0.5f : 0f,
                    iconTexture.Size() * 0.5f,
                    scale * Projectile.scale,
                    SpriteEffects.None,
                    0);

                DrawModePips(iconCenter, mode, coreCount, Projectile.Opacity);

                hoveredLastFrame[i] = hovered;
                if (clickFeedbackTimers[i] > 0)
                    clickFeedbackTimers[i]--;
            }

            if (appendMode.HasValue)
            {
                bowPlayer.AppendMode(appendMode.Value);
                string modeName = Language.GetTextValue($"Mods.CalamityLegendsComeBack.Items.Weapons.SHPBow.ModeName{(int)appendMode.Value}");
                CombatText.NewText(owner.Hitbox, SHPBowModeHelpers.MainColor(appendMode.Value), modeName, dramatic: false, dot: false);
                SoundEngine.PlaySound(SoundID.Item23 with { Volume = 0.55f, Pitch = 0.02f }, owner.Center);
            }
            else if (resetMode.HasValue)
            {
                bowPlayer.ResetSequence(resetMode.Value);
                string modeName = Language.GetTextValue($"Mods.CalamityLegendsComeBack.Items.Weapons.SHPBow.ModeName{(int)resetMode.Value}");
                CombatText.NewText(owner.Hitbox, SHPBowModeHelpers.AccentColor(resetMode.Value), modeName, dramatic: false, dot: false);
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.58f, Pitch = -0.1f }, owner.Center);
            }
            else if (Projectile.Opacity >= 0.95f && (rightClickPressed || (leftClickPressed && !hoveringOverAnySlot)))
            {
                FadeOut = true;
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.5f }, owner.Center);
            }

            if (hoveringOverAnySlot)
            {
                Main.blockMouse = true;
                owner.mouseInterface = true;
            }

            return false;
        }

        private static void DrawSequencePreview(Vector2 drawPosition, SHPBowPlayer bowPlayer, float opacity)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 firstSlot = drawPosition + new Vector2(-33f, 0f);
            Vector2 lastSlot = drawPosition + new Vector2(33f, 0f);
            DrawLine(firstSlot, lastSlot, MakeAdditive(SHPBowModeHelpers.MainColor(bowPlayer.SequenceAccentMode)) * (0.34f * opacity), 2f, additive: true);

            for (int i = 0; i < SHPBowModeHelpers.MaxSequenceLength; i++)
            {
                Vector2 slotCenter = drawPosition + new Vector2((i - 1.5f) * 22f, 0f);
                if (i >= bowPlayer.SequenceLength)
                {
                    Main.spriteBatch.SetBlendState(BlendState.Additive);
                    Main.EntitySpriteDraw(
                        bloom,
                        slotCenter,
                        null,
                        new Color(62, 78, 92, 0) * (0.16f * opacity),
                        0f,
                        bloom.Size() * 0.5f,
                        0.055f,
                        SpriteEffects.None,
                        0f);
                    Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
                    continue;
                }

                SHPBowMode mode = bowPlayer.GetSequenceMode(i);
                Texture2D iconTexture = ModContent.Request<Texture2D>(SHPBowModeHelpers.IconTexturePath(mode)).Value;
                Color coreColor = Color.Lerp(SHPBowModeHelpers.MainColor(mode), SHPBowModeHelpers.AccentColor(mode), 0.28f);
                float pulse = 0.92f + 0.08f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 8f + i);

                Main.spriteBatch.SetBlendState(BlendState.Additive);
                Main.EntitySpriteDraw(
                    bloom,
                    slotCenter,
                    null,
                    MakeAdditive(coreColor) * (0.3f * opacity),
                    0f,
                    bloom.Size() * 0.5f,
                    0.075f * pulse,
                    SpriteEffects.None,
                    0f);
                Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

                Main.EntitySpriteDraw(
                    iconTexture,
                    slotCenter,
                    null,
                    Color.White * opacity,
                    0f,
                    iconTexture.Size() * 0.5f,
                    0.31f * pulse,
                    SpriteEffects.None,
                    0f);
            }
        }

        private static void DrawModePips(Vector2 iconCenter, SHPBowMode mode, int count, float opacity)
        {
            if (count <= 0)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color color = SHPBowModeHelpers.AccentColor(mode);
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < count; i++)
            {
                float angle = -MathHelper.PiOver2 + i * MathHelper.TwoPi / SHPBowModeHelpers.MaxSequenceLength;
                Vector2 pipCenter = iconCenter + angle.ToRotationVector2() * 24f;
                Main.EntitySpriteDraw(
                    bloom,
                    pipCenter,
                    null,
                    MakeAdditive(color) * (0.36f * opacity),
                    0f,
                    bloom.Size() * 0.5f,
                    0.045f,
                    SpriteEffects.None,
                    0f);
            }
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private static void DrawPanelRings(Vector2 drawPosition, SHPBowMode currentMode, float opacity)
        {
            Texture2D outerRing = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_03").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color mainColor = SHPBowModeHelpers.MainColor(currentMode);
            Color accentColor = SHPBowModeHelpers.AccentColor(currentMode);
            float time = Main.GlobalTimeWrappedHourly;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                MakeAdditive(mainColor) * (0.32f * opacity),
                0f,
                bloom.Size() * 0.5f,
                0.62f,
                SpriteEffects.None,
                0f);

            for (int i = 0; i < 18; i++)
            {
                float completion = i / 18f;
                Vector2 offset = (MathHelper.TwoPi * completion + time * 1.2f).ToRotationVector2() * (2.4f + 1.2f * (float)System.Math.Sin(time * 4f + i));
                Color outlineColor = Color.Lerp(mainColor, RainbowColor(completion + time * 0.14f), 0.42f);

                Main.EntitySpriteDraw(
                    outerRing,
                    drawPosition + offset,
                    null,
                    MakeAdditive(outlineColor) * (0.18f * opacity),
                    time * 0.72f,
                    outerRing.Size() * 0.5f,
                    0.43f,
                    SpriteEffects.None,
                    0f);
            }

            Main.EntitySpriteDraw(
                outerRing,
                drawPosition,
                null,
                MakeAdditive(mainColor) * (0.46f * opacity),
                time * 0.72f,
                outerRing.Size() * 0.5f,
                0.42f,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                outerRing,
                drawPosition,
                null,
                MakeAdditive(accentColor) * (0.34f * opacity),
                -time * 0.48f,
                outerRing.Size() * 0.5f,
                0.32f,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private static void DrawIconEnergyOutline(Texture2D texture, Vector2 iconCenter, SHPBowMode mode, float scale, bool selected, bool hovered, int clickTimer, float opacity)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float intensity = 0.18f + (selected ? 0.18f : 0f) + (hovered ? 0.28f : 0f) + (clickTimer > 0 ? 0.24f : 0f);
            float radius = 1.8f + (selected ? 0.8f : 0f) + (hovered ? 1.4f : 0f) + clickTimer * 0.12f;
            Color mainColor = SHPBowModeHelpers.MainColor(mode);
            Color accentColor = SHPBowModeHelpers.AccentColor(mode);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                bloom,
                iconCenter,
                null,
                MakeAdditive(Color.Lerp(mainColor, accentColor, 0.45f)) * (opacity * intensity * 0.62f),
                0f,
                bloom.Size() * 0.5f,
                0.17f + intensity * 0.08f,
                SpriteEffects.None,
                0f);

            for (int i = 0; i < 12; i++)
            {
                float completion = i / 12f;
                Vector2 offset = (MathHelper.TwoPi * completion + Main.GlobalTimeWrappedHourly * 1.5f).ToRotationVector2() * radius;
                Color outlineColor = Color.Lerp(mainColor, RainbowColor(completion + Main.GlobalTimeWrappedHourly * 0.18f), hovered ? 0.55f : 0.34f);

                Main.EntitySpriteDraw(
                    texture,
                    iconCenter + offset,
                    null,
                    MakeAdditive(outlineColor) * (opacity * intensity),
                    0f,
                    texture.Size() * 0.5f,
                    scale * 1.02f,
                    SpriteEffects.None,
                    0f);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private static Color MakeAdditive(Color color)
        {
            color.A = 0;
            return color;
        }

        private static Color RainbowColor(float completion)
        {
            completion -= (float)System.Math.Floor(completion);
            Color color = Main.hslToRgb(completion, 0.94f, 0.68f, byte.MaxValue);
            color.A = 0;
            return color;
        }

        private static void DrawLine(Vector2 start, Vector2 end, Color color, float width, bool additive = false)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 edge = end - start;
            if (edge.LengthSquared() <= 0.01f)
                return;

            if (additive)
                Main.spriteBatch.SetBlendState(BlendState.Additive);

            Main.spriteBatch.Draw(
                pixel,
                start,
                new Rectangle(0, 0, 1, 1),
                color,
                edge.ToRotation(),
                Vector2.Zero,
                new Vector2(edge.Length(), width),
                SpriteEffects.None,
                0f);

            if (additive)
                Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }
    }
}
