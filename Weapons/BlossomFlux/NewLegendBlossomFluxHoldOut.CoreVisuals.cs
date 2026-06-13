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
        // 弓体核心位置：稍微靠后，贴近握把/矢台区域，而不是在弓弦前端。
        private Vector2 CoreBodyPosition => Projectile.Center - AimDirection * 4f;

        public override bool PreDraw(ref Color lightColor)
        {
            if (Owner is null)
                return false;

            Texture2D weaponTexture = BlossomFluxTacticalTextures.GetWeaponTexture(CurrentPreset);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = weaponTexture.Size() * 0.5f;
            float rotation = Projectile.rotation;
            SpriteEffects effects = SpriteEffects.None;
            float leftFlash = leftStarFlashTimer / (float)LeftStarFlashFrames;
            float chargeGlow = rightChargeActive && reloadTimer <= 0
                ? MathHelper.SmoothStep(0f, 1f, ChargeCompletion)
                : 0f;

            if (Owner.gravDir == 1f)
            {
                if (Projectile.spriteDirection == -1)
                    effects = SpriteEffects.FlipVertically;
            }
            else
            {
                origin.Y = weaponTexture.Height - origin.Y;
                if (Projectile.spriteDirection == 1)
                    effects = SpriteEffects.FlipVertically;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            DrawSHPCWeaponOutline(weaponTexture, drawPosition, rotation, origin, effects, leftFlash, chargeGlow);
            Main.EntitySpriteDraw(weaponTexture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, effects, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            // 常驻底光：始终在弓体绘制一个低强度星芒核心，给武器"活着"的感觉。
            DrawSHPCMagicCore(CurrentPreset, 0.18f, 0f, false, true);

            if (rightChargeActive && reloadTimer <= 0)
                DrawSHPCRightChargeVisuals(CurrentPreset, chargeGlow);
            else
                DrawSHPCLeftAttackVisuals(CurrentPreset, leftFlash);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private void DrawSHPCWeaponOutline(Texture2D weaponTexture, Vector2 drawPosition, float rotation, Vector2 origin, SpriteEffects effects, float leftFlash, float chargeGlow)
        {
            Color mainColor = BFArrowCommon.GetPresetColor(CurrentPreset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(CurrentPreset);
            float leftPulse = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(leftFlash, 0f, 1f));
            float leftBuild = GetLeftAttackBuildGlow();
            float rightPulse = rightOutlinePulseTimer / (float)RightOutlinePulseFrames;
            float activity = MathHelper.Clamp(Math.Max(Math.Max(leftPulse * 0.65f, leftBuild), chargeGlow), 0f, 1f);
            float time = Main.GlobalTimeWrappedHourly;
            float idlePulse = 0.78f + 0.22f * (float)Math.Sin(time * 5.1f + Projectile.identity * 0.37f);
            float outlineDistance = 1.35f + activity * 2.65f + rightPulse * 2.2f;
            int outlineDraws = 8 + (int)(activity * 5f);
            Color outerColor = Color.Lerp(mainColor, Color.White, 0.45f) * (0.18f + activity * 0.34f) * idlePulse;
            Color innerColor = Color.Lerp(accentColor, Color.White, 0.55f) * (0.12f + activity * 0.22f);

            for (int i = 0; i < outlineDraws; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / outlineDraws + time * 0.75f).ToRotationVector2() * outlineDistance;
                Main.EntitySpriteDraw(weaponTexture, drawPosition + offset, null, outerColor, rotation, origin, Projectile.scale, effects, 0);
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = (MathHelper.PiOver2 * i - time * 0.5f).ToRotationVector2() * (0.8f + activity * 0.9f);
                Main.EntitySpriteDraw(weaponTexture, drawPosition + offset, null, innerColor, rotation, origin, Projectile.scale * (1f + activity * 0.02f), effects, 0);
            }

            if (rightPulse > 0.05f)
            {
                HoldoutOutlineHelper.DrawStarmadaRainbowOutline(
                    weaponTexture,
                    drawPosition,
                    rotation,
                    origin,
                    Vector2.One * Projectile.scale,
                    effects,
                    2.4f + rightPulse * 4.2f,
                    rightPulse * 0.38f,
                    time + Projectile.identity * 0.17f,
                    18,
                    manageBlendState: false);
            }
        }

        // 弓体能量星芒核心，类 SHPC 右键绘制风格，位置在弓握把/矢台处。
        // power: 0..1 控制强度；phaseKick: 额外旋转偏移；rightCharge: 右键蓄力模式（4臂）；idleCore: 常驻底光模式。
        private void DrawSHPCMagicCore(BlossomFluxChloroplastPresetType preset, float power, float phaseKick, bool rightCharge, bool idleCore)
        {
            if (Main.dedServ)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2 direction = AimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            // 核心绘制在弓体位置，而非弓弦前端，与 SHPC 的弹夹核心位置对应。
            Vector2 core = CoreBodyPosition - Main.screenPosition;
            Color themeColor = BFArrowCommon.GetPresetColor(preset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(preset);
            Color cyan = Color.Lerp(themeColor, new Color(90, 210, 255), 0.35f);
            Color white = Color.Lerp(accentColor, new Color(230, 255, 255), 0.62f);
            float charge = MathHelper.Clamp(power, 0f, 1f);
            float flashPulse = idleCore ? 0f : charge;
            float time = Main.GlobalTimeWrappedHourly;

            float bloomOpacity = idleCore
                ? 0.18f + charge * 0.15f
                : 0.22f + charge * 0.20f + flashPulse * 0.32f;
            Vector2 bloomScale = idleCore
                ? new Vector2(0.28f + charge * 0.14f, 0.14f + charge * 0.07f)
                : new Vector2(0.36f + charge * 0.20f + flashPulse * 0.24f, 0.18f + charge * 0.10f);

            Main.EntitySpriteDraw(
                bloom,
                core,
                null,
                Color.Lerp(cyan, white, charge) * bloomOpacity,
                0f,
                bloom.Size() * 0.5f,
                bloomScale,
                SpriteEffects.None,
                0);

            int starCount = rightCharge ? 4 : 3;
            for (int i = 0; i < starCount; i++)
            {
                float rotation = direction.ToRotation() + MathHelper.TwoPi * i / starCount + time * (rightCharge ? 1.25f + i * 0.14f : 1.08f + i * 0.10f) + phaseKick;
                float starOpacity = idleCore
                    ? 0.32f + charge * 0.16f
                    : 0.48f + charge * 0.32f + flashPulse * 0.52f;
                Vector2 starScale = idleCore
                    ? new Vector2(0.20f, 0.92f + charge * 0.28f)
                    : new Vector2(0.26f + flashPulse * 0.22f, 1.55f + charge * 0.60f + flashPulse * 1.20f);

                Main.EntitySpriteDraw(
                    star,
                    core,
                    null,
                    Color.Lerp(cyan, white, 0.58f) * starOpacity,
                    rotation,
                    star.Size() * 0.5f,
                    starScale,
                    SpriteEffects.None,
                    0);
            }
        }
    }
}
