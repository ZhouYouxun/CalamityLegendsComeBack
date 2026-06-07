using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Systems;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI
{
    internal class BFSelectionPanel : ModProjectile, ILocalizedModType, IScreenOverlayProjectile
    {
        public const float FormSwitchMode = 1f;
        private const float IconScale = 0.75f;
        private const float FormSwitchDeadZone = 14f;
        private const int SlotFrameSize = 48;
        private const int InnerFrameSize = 38;
        private const int BorderThickness = 2;

        private static readonly Vector2[] IconOffsets =
        {
            new(0f, -58f),
            new(55f, -18f),
            new(34f, 48f),
            new(-34f, 48f),
            new(-55f, -18f)
        };

        private sealed class IconState
        {
            public int ClickFeedbackTimer;
            public bool BeingHoveredOver;
            public bool WasHoveredOverLastFrame;
            public bool SelectedAsCurrent;
            public bool Unlocked;
            public BlossomFluxChloroplastPresetType Preset;

            public IconState(BlossomFluxChloroplastPresetType preset)
            {
                Preset = preset;
            }

            public string TexturePath => Preset switch
            {
                BlossomFluxChloroplastPresetType.Chlo_ABreak => "CalamityLegendsComeBack/Weapons/BlossomFlux/RightUI/BFUI_ABreak",
                BlossomFluxChloroplastPresetType.Chlo_BRecov => "CalamityLegendsComeBack/Weapons/BlossomFlux/RightUI/BFUI_BRecov",
                BlossomFluxChloroplastPresetType.Chlo_CDetec => "CalamityLegendsComeBack/Weapons/BlossomFlux/RightUI/BFUI_CDetec",
                BlossomFluxChloroplastPresetType.Chlo_DBomb => "CalamityLegendsComeBack/Weapons/BlossomFlux/RightUI/BFUI_DBomb",
                BlossomFluxChloroplastPresetType.Chlo_EPlague => "CalamityLegendsComeBack/Weapons/BlossomFlux/RightUI/BFUI_EPlague",
                _ => "CalamityLegendsComeBack/Weapons/BlossomFlux/RightUI/BFUI_ABreak"
            };

            public Texture2D IconTexture => ModContent.Request<Texture2D>(TexturePath).Value;

            public float DrawScale
            {
                get
                {
                    float scale = 1f;
                    if (SelectedAsCurrent)
                        scale += 0.08f;

                    if (BeingHoveredOver)
                        scale += 0.08f;

                    if (ClickFeedbackTimer > 0)
                        scale += 0.05f;

                    return scale;
                }
            }

            public Color DrawColor(float opacity)
            {
                if (!Unlocked)
                {
                    Color lockedColor = new Color(78, 108, 88);
                    if (BeingHoveredOver)
                        lockedColor = Color.Lerp(lockedColor, new Color(150, 170, 150), 0.35f);

                    return lockedColor * opacity * 0.9f;
                }

                Color color = SelectedAsCurrent ? new Color(185, 255, 185) : Color.White;
                if (BeingHoveredOver)
                    color = Color.Lerp(color, Color.White, 0.35f);

                if (ClickFeedbackTimer > 0)
                    color = Color.Lerp(color, new Color(255, 240, 180), 0.45f);

                return color * opacity;
            }

            public void Update()
            {
                if (ClickFeedbackTimer > 0)
                    ClickFeedbackTimer--;
            }
        }

        private readonly IconState[] icons =
        {
            new(BlossomFluxChloroplastPresetType.Chlo_ABreak),
            new(BlossomFluxChloroplastPresetType.Chlo_BRecov),
            new(BlossomFluxChloroplastPresetType.Chlo_CDetec),
            new(BlossomFluxChloroplastPresetType.Chlo_DBomb),
            new(BlossomFluxChloroplastPresetType.Chlo_EPlague)
        };

        private Vector2 screenCenter;
        private bool offsetInitialized;

        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/RightUI/BF_Panel";
        public new string LocalizationCategory => "Projectiles.BlossomFlux";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private bool IsFormSwitchMode => Projectile.ai[1] == FormSwitchMode;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 150;
            Projectile.height = 150;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.Opacity = 0f;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!IsFormSwitchMode)
            {
                Projectile.Kill();
                return;
            }

            if (owner.HeldItem.type != ModContent.ItemType<NewLegendBlossomFlux>())
                FadeOut = true;

            if (Main.myPlayer == Projectile.owner &&
                KeybindSystem.LegendaryWeaponFormSwitch?.Current != true &&
                KeybindSystem.LegendaryWeaponFormSwitch?.JustReleased != true)
            {
                FadeOut = true;
            }

            if (!offsetInitialized && Main.myPlayer == Projectile.owner)
            {
                screenCenter = Main.MouseScreen;
                offsetInitialized = true;
            }

            Projectile.Center = Main.myPlayer == Projectile.owner ? Main.screenPosition + screenCenter : owner.Center;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.12f : 0.12f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            BFRightUIPlayer rightUIPlayer = owner.GetModPlayer<BFRightUIPlayer>();
            Vector2 drawPosition = screenCenter.Floor();
            Texture2D outerRing = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/flower_015").Value;
            Texture2D innerRing = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_03").Value;
            Color outerColor = new Color(82, 175, 110, 0) * (0.36f * Projectile.Opacity);
            Color innerColor = new Color(182, 255, 190, 0) * (0.28f * Projectile.Opacity);

            Main.EntitySpriteDraw(
                outerRing,
                drawPosition,
                null,
                outerColor,
                Main.GlobalTimeWrappedHourly * 0.8f,
                outerRing.Size() * 0.5f,
                0.22f * Projectile.scale,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                innerRing,
                drawPosition,
                null,
                innerColor,
                -Main.GlobalTimeWrappedHourly * 0.55f,
                innerRing.Size() * 0.5f,
                0.42f * Projectile.scale,
                SpriteEffects.None,
                0f);

            Vector2 frameSize = icons[0].IconTexture.Size();
            int formSwitchHoveredIndex = GetFormSwitchHoveredIndex(drawPosition);
            bool formSwitchReleased = KeybindSystem.LegendaryWeaponFormSwitch?.JustReleased == true;

                DrawFormSwitchSectorLines(drawPosition, formSwitchHoveredIndex);

            for (int i = 0; i < icons.Length; i++)
            {
                IconState icon = icons[i];
                icon.BeingHoveredOver = false;
                icon.Unlocked = rightUIPlayer.IsPresetUnlocked(icon.Preset);
                icon.SelectedAsCurrent = icon.Preset == rightUIPlayer.CurrentPreset;

                Vector2 iconCenter = drawPosition + IconOffsets[i] * Projectile.scale;
                bool hovered = i == formSwitchHoveredIndex;

                if (hovered)
                {
                    icon.BeingHoveredOver = true;

                    if (!icon.WasHoveredOverLastFrame && Projectile.Opacity >= 1f)
                        SoundEngine.PlaySound(SoundID.Item55 with { Volume = 0.42f, Pitch = 0.1f }, owner.Center);
                }

                Rectangle slotFrameArea = Utils.CenteredRectangle(iconCenter, new Vector2(SlotFrameSize) * Projectile.scale);
                DrawIconSlotFrame(icon, slotFrameArea, Projectile.Opacity);

                Main.EntitySpriteDraw(
                    icon.IconTexture,
                    iconCenter,
                    null,
                    icon.DrawColor(Projectile.Opacity),
                    0f,
                    frameSize * 0.5f,
                    IconScale * icon.DrawScale * Projectile.scale,
                    SpriteEffects.None,
                    0f);

                icon.WasHoveredOverLastFrame = icon.BeingHoveredOver;
                icon.Update();
            }

                if (formSwitchReleased && Projectile.Opacity > 0.08f)
                {
                    if (formSwitchHoveredIndex >= 0 && icons[formSwitchHoveredIndex].Unlocked)
                    {
                        BlossomFluxChloroplastPresetType preset = icons[formSwitchHoveredIndex].Preset;
                        rightUIPlayer.TrySetPreset(preset);
                        string presetName = Language.GetTextValue($"Mods.CalamityLegendsComeBack.Items.Weapons.NewLegendBlossomFlux.PresetName{(int)preset}");
                        string selectionText = Language.GetTextValue("Mods.CalamityLegendsComeBack.TheSpecialText.BlossomFluxPresetSelected", presetName);
                        CombatText.NewText(owner.Hitbox, ChloroplastCommon.PresetColor(preset), selectionText, dramatic: false, dot: false);
                        icons[formSwitchHoveredIndex].ClickFeedbackTimer = 10;
                        SoundEngine.PlaySound(SoundID.Item23 with { Pitch = 0.06f, Volume = 0.72f }, owner.Center);
                    }
                    else
                    {
                        SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.42f, Pitch = 0.2f }, owner.Center);
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

        private int GetFormSwitchHoveredIndex(Vector2 drawPosition)
        {
            Vector2 mouseOffset = Main.MouseScreen - drawPosition;
            if (mouseOffset.LengthSquared() < FormSwitchDeadZone * FormSwitchDeadZone)
                return -1;

            Vector2 mouseDirection = mouseOffset.SafeNormalize(-Vector2.UnitY);
            int bestIndex = 0;
            float bestDot = -2f;

            for (int i = 0; i < IconOffsets.Length; i++)
            {
                Vector2 iconDirection = IconOffsets[i].SafeNormalize(-Vector2.UnitY);
                float dot = Vector2.Dot(mouseDirection, iconDirection);
                if (dot <= bestDot)
                    continue;

                bestDot = dot;
                bestIndex = i;
            }

            return bestIndex;
        }

        private void DrawFormSwitchSectorLines(Vector2 drawPosition, int hoveredIndex)
        {
            Color lineColor = new Color(138, 255, 172, 0) * (0.28f * Projectile.Opacity);
            float radius = 82f * Projectile.scale;

            for (int i = 0; i < IconOffsets.Length; i++)
            {
                Vector2 current = IconOffsets[i].SafeNormalize(-Vector2.UnitY);
                Vector2 next = IconOffsets[(i + 1) % IconOffsets.Length].SafeNormalize(-Vector2.UnitY);
                Vector2 boundary = (current + next).SafeNormalize(current);
                DrawScreenLine(drawPosition, drawPosition + boundary * radius, lineColor, 1.4f);
            }

            if (hoveredIndex < 0)
                return;

            Color highlightColor = ChloroplastCommon.PresetColor(icons[hoveredIndex].Preset) with { A = 0 };
            Vector2 direction = IconOffsets[hoveredIndex].SafeNormalize(-Vector2.UnitY);
            DrawScreenLine(drawPosition, drawPosition + direction * (radius * 0.92f), highlightColor * (0.54f * Projectile.Opacity), 3.2f);
        }

        private static void DrawIconSlotFrame(IconState icon, Rectangle slotArea, float opacity)
        {
            Color presetColor = ChloroplastCommon.PresetColor(icon.Preset);
            Color slotBack = icon.Unlocked
                ? Color.Lerp(new Color(16, 30, 22), presetColor, icon.SelectedAsCurrent ? 0.24f : 0.14f)
                : new Color(28, 34, 31);
            Color slotBorder = icon.Unlocked
                ? Color.Lerp(new Color(86, 124, 92), presetColor, icon.SelectedAsCurrent ? 0.5f : 0.32f)
                : new Color(72, 88, 78);

            if (icon.BeingHoveredOver)
            {
                slotBack = Color.Lerp(slotBack, new Color(72, 92, 76), 0.42f);
                slotBorder = Color.Lerp(slotBorder, Color.White, 0.32f);
            }

            if (icon.ClickFeedbackTimer > 0)
            {
                slotBack = Color.Lerp(slotBack, new Color(130, 116, 70), 0.34f);
                slotBorder = Color.Lerp(slotBorder, new Color(255, 232, 156), 0.5f);
            }

            DrawRectangle(slotArea, slotBack * (opacity * 0.78f));
            DrawBorder(slotArea, slotBorder * (opacity * 0.92f), BorderThickness);

            Rectangle innerArea = Utils.CenteredRectangle(slotArea.Center.ToVector2(), new Vector2(InnerFrameSize));
            Color innerBack = icon.Unlocked ? Color.Lerp(new Color(8, 14, 12), presetColor, 0.08f) : new Color(14, 18, 17);
            DrawRectangle(innerArea, innerBack * (opacity * 0.72f));
            DrawBorder(innerArea, slotBorder * (opacity * 0.74f), 1);
        }

        private static void DrawScreenLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 edge = end - start;
            if (edge.LengthSquared() <= 0.001f)
                return;

            Main.EntitySpriteDraw(
                pixel,
                start,
                new Rectangle(0, 0, 1, 1),
                color,
                edge.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(edge.Length(), thickness),
                SpriteEffects.None,
                0f);
        }

        private static void DrawRectangle(Rectangle rectangle, Color color)
        {
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rectangle, color);
        }

        private static void DrawBorder(Rectangle rectangle, Color color, int thickness)
        {
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
            DrawRectangle(new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height), color);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
        }

        public override bool? CanDamage() => false;
    }
}
