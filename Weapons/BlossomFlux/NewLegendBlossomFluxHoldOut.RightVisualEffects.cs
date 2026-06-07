using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
using CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.Visuals;
using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    internal sealed partial class NewLegendBlossomFluxHoldOut
    {
        private void TriggerTacticalOutlinePulse(bool rightClick)
        {
            if (rightClick)
                rightOutlinePulseTimer = RightOutlinePulseFrames + 15;
            else
                leftOutlinePulseTimer = LeftOutlinePulseFrames + 10;
        }

        private void PlayChargeReadyBurst()
        {
            if (readyBurstPlayed)
                return;

            readyBurstPlayed = true;
            rightOutlinePulseTimer = RightOutlinePulseFrames + 15;

            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f, Pitch = 0.25f }, GunTipPosition);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.4f, Pitch = -0.15f }, GunTipPosition);
        }

        private void PlayBreakthroughArrowLoadedBurst()
        {
            SoundEngine.PlaySound(SoundID.Item108 with { Volume = 0.22f, Pitch = 0.35f }, GunTipPosition);
        }

        private void PlayRightReleaseSound()
        {
            if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb)
            {
                SoundStyle bombardFire = new("CalamityMod/Sounds/Item/LauncherHeavyShot");
                SoundEngine.PlaySound(bombardFire with { Volume = 0.82f, Pitch = -0.08f, PitchVariance = 0.08f }, GunTipPosition);
            }
            else
            {
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.72f, Pitch = -0.05f }, GunTipPosition);
            }
        }

        private void DrawSHPCRightChargeVisuals(BlossomFluxChloroplastPresetType preset, float chargeGlow)
        {
            // 右键唯一绘制外包：共享 SHPC 星芒，不生成粒子；额外绘制当前形态的装填弹幕贴图。
            float chargePower = MathHelper.Clamp(chargeGlow, 0f, 1f);
            DrawSHPCMagicCore(preset, MathHelper.Lerp(0.28f, 0.74f, chargePower), 0f, true, false);
            DrawSHPCRightLoadedProjectile(preset, Math.Max(chargePower, 0.12f));
        }

        private void DrawSHPCRightLoadedProjectile(BlossomFluxChloroplastPresetType preset, float chargePower)
        {
            Texture2D loadedTexture = ModContent.Request<Texture2D>(BFArrowCommon.GetTexturePathForPreset(preset)).Value;
            Vector2 forward = AimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2) * Owner.gravDir;
            Vector2 origin = loadedTexture.Size() * 0.5f;
            float arrowRotation = forward.ToRotation() + MathHelper.PiOver2 + MathHelper.Pi;
            Color loadedColor = Color.Lerp(Color.White, BFArrowCommon.GetPresetColor(preset), 0.42f + chargePower * 0.24f);

            if (preset == BlossomFluxChloroplastPresetType.Chlo_ABreak)
            {
                int maxArrows = Math.Max(1, BreakthroughMaxLoadedArrows);
                int loadedArrows = Utils.Clamp(breakthroughLoadedArrows, 0, maxArrows);
                bool fullyLoaded = loadedArrows >= maxArrows;
                int drawCount = fullyLoaded ? loadedArrows : Math.Min(loadedArrows + 1, maxArrows);
                if (drawCount <= 0)
                    return;

                for (int i = 0; i < drawCount; i++)
                {
                    bool loadingArrow = !fullyLoaded && i == loadedArrows;
                    float visibility = loadingArrow ? BreakthroughCurrentArrowCompletion : 1f;
                    if (visibility <= 0.025f)
                        continue;

                    float stackOffset = i - (drawCount - 1) * 0.5f;
                    float loadFlash = !loadingArrow && breakthroughLoadFlashTimer > 0 && i == loadedArrows - 1
                        ? breakthroughLoadFlashTimer / (float)BreakthroughLoadFlashFrames
                        : 0f;
                    Vector2 drawWorld = Projectile.Center + forward * MathHelper.Lerp(20f, 29f, visibility) - forward * i * 1.8f + normal * stackOffset * 3.1f;
                    float pulse = 1f + loadFlash * 0.08f;

                    Main.EntitySpriteDraw(
                        loadedTexture,
                        drawWorld - Main.screenPosition,
                        null,
                        loadedColor * visibility,
                        arrowRotation,
                        origin,
                        MathHelper.Lerp(0.68f, 0.88f, visibility) * pulse * Projectile.scale,
                        SpriteEffects.None,
                        0);
                }

                return;
            }

            Vector2 drawPosition = Projectile.Center + forward * MathHelper.Lerp(18f, 26f, chargePower) + normal * MathHelper.Lerp(-3f, -1f, chargePower) - Main.screenPosition;
            float readyPulse = readyBurstPlayed ? 0.03f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f) : 0f;
            float scale = (0.68f + chargePower * 0.2f + readyPulse) * Projectile.scale;

            Main.EntitySpriteDraw(
                loadedTexture,
                drawPosition,
                null,
                loadedColor * MathHelper.Lerp(0.58f, 1f, chargePower),
                arrowRotation,
                origin,
                scale,
                SpriteEffects.None,
                0);
        }
    }
}
