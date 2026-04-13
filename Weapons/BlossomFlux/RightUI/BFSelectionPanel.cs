using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI
{
        internal class BFSelectionPanel : ModProjectile, ILocalizedModType
        {
            private const float IconScale = 0.75f;

        private static readonly Vector2[] IconOffsets =
        {
            new(-56f, 0f),
            new(-28f, 0f),
            new(0f, 0f),
            new(28f, 0f),
            new(56f, 0f)
        };

        private sealed class IconState
        {
            public int ClickFeedbackTimer;
            public bool BeingHoveredOver;
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

        private Vector2 playerOffset;
        private bool offsetInitialized;

        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/RightUI/BF_Panel";
        public new string LocalizationCategory => "Projectiles.BlossomFlux";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        public static Rectangle MouseRectangle => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 2, 2);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 170;
            Projectile.height = 62;
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

            // 面板本质上仍然是一个弹幕，只在手持本武器时保留。
            if (owner.HeldItem.type != ModContent.ItemType<NewLegendBlossomFlux>())
                FadeOut = true;

            if (!offsetInitialized && Main.myPlayer == Projectile.owner)
            {
                playerOffset = Main.MouseWorld - owner.Center;
                offsetInitialized = true;
            }

            Projectile.Center = owner.Center + playerOffset;
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
            Texture2D panelTexture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = (owner.Center + playerOffset - Main.screenPosition).Floor();

            Main.EntitySpriteDraw(
                panelTexture,
                drawPosition,
                null,
                Projectile.GetAlpha(Color.White),
                0f,
                panelTexture.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None,
                0f);

            // 这里按你的要求固定使用单帧贴图，不再适配 EXO 那套多帧按钮逻辑。
            Vector2 frameSize = icons[0].IconTexture.Size();
            bool hoveringOverAnySlot = false;
            bool clickedOutside = Main.mouseLeft && Main.mouseLeftRelease;
            bool clickedLockedPreset = false;
            BlossomFluxChloroplastPresetType? clickedPreset = null;

            for (int i = 0; i < icons.Length; i++)
            {
                IconState icon = icons[i];
                icon.BeingHoveredOver = false;
                icon.Unlocked = rightUIPlayer.IsPresetUnlocked(icon.Preset);
                icon.SelectedAsCurrent = icon.Preset == rightUIPlayer.CurrentPreset;

                Vector2 iconCenter = drawPosition + IconOffsets[i] * Projectile.scale;
                Rectangle iconArea = Utils.CenteredRectangle(iconCenter, frameSize * IconScale * Projectile.scale);

                if (iconArea.Intersects(MouseRectangle))
                {
                    hoveringOverAnySlot = true;
                    clickedOutside = false;
                    icon.BeingHoveredOver = true;

                    if (Main.mouseLeft && Main.mouseLeftRelease && Projectile.Opacity >= 1f)
                    {
                        if (icon.Unlocked)
                        {
                            clickedPreset = icon.Preset;
                            icon.ClickFeedbackTimer = 10;
                        }
                        else
                        {
                            clickedLockedPreset = true;
                        }
                    }
                }

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

                icon.Update();
            }

            if (clickedPreset.HasValue)
            {
                rightUIPlayer.TrySetPreset(clickedPreset.Value);
                FadeOut = true;
                SoundEngine.PlaySound(SoundID.MenuTick with { Pitch = 0.1f, Volume = 0.7f }, owner.Center);
            }
            else if (clickedLockedPreset)
            {
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.42f, Pitch = 0.2f }, owner.Center);
            }
            else if (clickedOutside && Projectile.Opacity >= 1f)
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

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }

        public override bool? CanDamage() => false;
    }
}
