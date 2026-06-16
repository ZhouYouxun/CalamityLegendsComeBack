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
        // 弓体核心位置：矢台（箭搭位）在弓身中心处。
        private Vector2 CoreBodyPosition => Projectile.Center;

        // ================= 星芒/核心绘制参数自定义配置 =================
        public static string CoreBloomTexture = "CalamityMod/Particles/BloomCircle";
        public static string CoreStarTexture = "CalamityMod/Particles/RoundedStar";  // 圆角柔和星，有机自然感
        public static string CoreFlowerTexture = "CalamityMod/Particles/MiniFlower"; // 小花瓣公转点缀，强化森林主题

        // 旋转速度基数
        public static float CoreRotationSpeedMultiplier = 1.0f;

        // 核心星芒大小/缩放比例（RoundedStar 400×400，比 HalfStar 33×33 大约 12× 维度，需相应缩小 scale）
        public static float CoreStarScaleX = 0.022f; // 修长尖端感（约对应 HalfStar 0.35 的视觉宽度）
        public static float CoreStarScaleY = 0.125f; // 长度系数（约对应 HalfStar 1.0 × 1.5 的视觉高度）
        public static float CoreBloomScaleX = 0.30f; // 背景光晕宽度（改为接近正圆，去掉激光炮口焰扁感）
        public static float CoreBloomScaleY = 0.28f; // 背景光晕高度
        // =============================================================

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

        // 弓体能量星芒核心，位置在弓矢台（弓身中心），自然森林主题配色与有机造型。
        private void DrawSHPCMagicCore(BlossomFluxChloroplastPresetType preset, float power, float phaseKick, bool rightCharge, bool idleCore)
        {
            if (Main.dedServ)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>(CoreBloomTexture).Value;
            Texture2D star = ModContent.Request<Texture2D>(CoreStarTexture).Value;
            Texture2D flower = ModContent.Request<Texture2D>(CoreFlowerTexture).Value;

            Vector2 direction = AimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 core = CoreBodyPosition - Main.screenPosition;

            Color themeColor = BFArrowCommon.GetPresetColor(preset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(preset);
            // 嫩绿混合主题色，替换原来的科幻青蓝
            Color naturalGlow = Color.Lerp(themeColor, new Color(160, 235, 120), 0.28f);
            Color white = Color.Lerp(accentColor, new Color(235, 255, 230), 0.60f);

            float charge = MathHelper.Clamp(power, 0f, 1f);
            float flashPulse = idleCore ? 0f : charge;
            float time = Main.GlobalTimeWrappedHourly * CoreRotationSpeedMultiplier;

            // 绘制底光背景 BloomCircle
            // 激活状态参考 ApoctosisArray 使用随机抖动旋转，制造"能量不稳定"感；闲置时平滑旋转。
            float bloomOpacity = idleCore
                ? 0.20f + charge * 0.15f
                : 0.26f + charge * 0.22f + flashPulse * 0.30f;

            for (int i = 0; i < 3; i++)
            {
                float iMult = 1f - 0.15f * i;
                float bloomRot = idleCore
                    ? time * 0.35f * (i % 2 == 0 ? 1f : -1f)
                    : Main.rand.NextFloat(-4f, 4f);
                Vector2 bloomScale = new Vector2(CoreBloomScaleX, CoreBloomScaleY) * (idleCore ? 1.0f : 1.25f + charge * 0.5f) * iMult;

                Main.EntitySpriteDraw(
                    bloom,
                    core,
                    null,
                    Color.Lerp(naturalGlow, white, charge) * bloomOpacity * (1f - 0.2f * i),
                    bloomRot,
                    bloom.Size() * 0.5f,
                    bloomScale,
                    SpriteEffects.None,
                    0);
            }

            // 绘制圆角星芒层级（RoundedStar，比 HalfStar 更柔和有机），5 层双向交叉旋转
            int layers = 5;
            for (int i = 0; i < layers; i++)
            {
                float iMult = 1f - 0.12f * i;

                for (int b = -1; b <= 1; b += 2)
                {
                    float pulseRate = idleCore ? 15f : (rightCharge ? 35f : 20f);
                    float sine = MathHelper.Lerp((float)Math.Sin(Main.GlobalTimeWrappedHourly * pulseRate / MathHelper.Pi), 0.5f * b, 0.6f);

                    float chargeRotationBoost = rightCharge ? time * 0.45f : 0f;
                    float rotation = direction.ToRotation()
                        + time * charge * Math.Max(i - 2, 0) * (rightCharge ? 0.4f : 0.15f)
                        + chargeRotationBoost
                        + MathHelper.PiOver4 * b
                        + phaseKick
                        + (MathHelper.TwoPi * i / layers);

                    float starOpacity = idleCore
                        ? 0.32f + charge * 0.14f
                        : 0.48f + charge * 0.30f + flashPulse * 0.42f;

                    Vector2 starScale = new Vector2(CoreStarScaleX, CoreStarScaleY * sine * b)
                        * (idleCore ? 0.85f : 1.35f + charge * 0.55f)
                        * iMult;

                    Main.EntitySpriteDraw(
                        star,
                        core,
                        null,
                        Color.Lerp(naturalGlow, white, 0.55f) * starOpacity * (1f - 0.10f * i),
                        rotation,
                        star.Size() * 0.5f,
                        starScale,
                        SpriteEffects.None,
                        0);
                }
            }

            // 小花瓣公转层：3 朵 MiniFlower 绕核心匀速公转，强化森林/自然主题身份。
            int flowerCount = 3;
            for (int i = 0; i < flowerCount; i++)
            {
                float orbitAngle = time * 0.85f + MathHelper.TwoPi * i / flowerCount;
                float orbitRadius = (5f + charge * 7f) * (idleCore ? 0.65f : 1f);
                Vector2 flowerPos = CoreBodyPosition + orbitAngle.ToRotationVector2() * orbitRadius - Main.screenPosition;
                float flowerOpacity = idleCore ? 0.42f + charge * 0.20f : 0.60f + charge * 0.28f + flashPulse * 0.22f;
                float flowerScale = (0.7f + charge * 0.5f) * (idleCore ? 0.80f : 1f);

                Main.EntitySpriteDraw(
                    flower,
                    flowerPos,
                    null,
                    Color.Lerp(themeColor, Color.White, 0.58f) * flowerOpacity,
                    orbitAngle + MathHelper.PiOver2,
                    flower.Size() * 0.5f,
                    flowerScale,
                    SpriteEffects.None,
                    0);
            }
        }
    }
}
