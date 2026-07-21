using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
using CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
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
        private Vector2 CoreBodyPosition => Projectile.Center + (Projectile.Center - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction) * 13f;

        // ================= 星芒/核心绘制参数自定义配置 =================
        public static string CoreBloomTexture = "CalamityMod/Particles/BloomCircle";
        public static string CoreStarTexture = "CalamityLegendsComeBack/Texture/KsTexture/star_06";  // 原：CalamityMod/Particles/RoundedStar（128×128）；新贴图 512×512，scale 已 ×0.25
        public static string CoreFlowerTexture = "CalamityLegendsComeBack/Texture/KsTexture/star_08"; // 原：CalamityMod/Particles/MiniFlower（128×128）；新贴图 512×512，scale 已 ×0.25

        // ================= 描边颜色配置 =================
        // 外层/内层描边与白色的混合比（0=纯主题色，1=纯白）；值越小颜色越纯、越跟随战术主题色
        // =================================================

        // 旋转速度基数
        public static float CoreRotationSpeedMultiplier = 1.0f;

        // 核心星芒大小/缩放比例（star_06 512×512，原 RoundedStar 128×128，×0.25 补偿尺寸差）
        public static float CoreStarScaleX = 0.0033f; // 原 0.022f（RoundedStar）；×0.25×0.6 → 512×512 等效宽度
        public static float CoreStarScaleY = 0.0186f; // 原 0.125f（RoundedStar）；×0.25×0.6 → 512×512 等效高度
        public static float CoreBloomScaleX = 0.18f; // 背景光晕宽度（原 0.30f，×0.6）
        public static float CoreBloomScaleY = 0.168f; // 背景光晕高度（原 0.28f，×0.6）
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

            // 瘟疫形态专属：左键开火时在弓口叠一层六边形生化排放阀光圈。
            if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_EPlague && !rightChargeActive)
                DrawPlagueVentIris(leftFlash);

            if (rightChargeActive && reloadTimer <= 0)
            {
                DrawSHPCRightChargeVisuals(CurrentPreset, chargeGlow);
                UpdateRightChargeHaloSpawning(ChargeCompletion);
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

            return false;
        }

        private static Color SampleAllPresetColors(float t)
        {
            // 在5种形态颜色间平滑循环，t ∈ [0, 1)
            float scaled = t * 5f;
            int idx = (int)scaled % 5;
            float frac = scaled % 1f;
            Color a = BFArrowCommon.GetPresetColor((BlossomFluxChloroplastPresetType)idx);
            Color b = BFArrowCommon.GetPresetColor((BlossomFluxChloroplastPresetType)((idx + 1) % 5));
            return Color.Lerp(a, b, frac);
        }

        private static Color SampleAllPresetAccentColors(float t)
        {
            float scaled = t * 5f;
            int idx = (int)scaled % 5;
            float frac = scaled % 1f;
            Color a = BFArrowCommon.GetPresetAccentColor((BlossomFluxChloroplastPresetType)idx);
            Color b = BFArrowCommon.GetPresetAccentColor((BlossomFluxChloroplastPresetType)((idx + 1) % 5));
            return Color.Lerp(a, b, frac);
        }

        private void DrawSHPCWeaponOutline(Texture2D weaponTexture, Vector2 drawPosition, float rotation, Vector2 origin, SpriteEffects effects, float leftFlash, float chargeGlow)
        {
            float rightPulse = MathHelper.Clamp(rightOutlinePulseTimer / (float)RightOutlinePulseFrames, 0f, 1f);
            float activity = MathHelper.Clamp(Math.Max(chargeGlow, rightPulse * 0.72f), 0f, 1f);
            if (activity <= 0.02f && rightPulse <= 0.05f)
                return;

            float time = Main.GlobalTimeWrappedHourly;
            float idlePulse = 0.78f + 0.22f * (float)Math.Sin(time * 5.1f + Projectile.identity * 0.37f);
            float outlineDistance = 1.35f + activity * 2.65f + rightPulse * 2.2f;
            int outlineDraws = 8 + (int)(activity * 5f);
            float outerAlpha = (0.18f + activity * 0.34f) * idlePulse;
            float innerAlpha = 0.12f + activity * 0.22f;
            Color themeColor = BFArrowCommon.GetPresetColor(CurrentPreset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(CurrentPreset);
            Color chargedOuter = Color.Lerp(themeColor, accentColor, 0.28f);
            Color chargedInner = Color.Lerp(accentColor, Color.White, 0.18f);

            // 外层描边：每个拷贝按角度索引循环取5种形态颜色，形成"各色亮纯色"包边
            for (int i = 0; i < outlineDraws; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / outlineDraws + time * 0.75f).ToRotationVector2() * outlineDistance;
                Main.EntitySpriteDraw(weaponTexture, drawPosition + offset, null, chargedOuter * outerAlpha, rotation, origin, Projectile.scale, effects, 0);
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = (MathHelper.PiOver2 * i - time * 0.5f).ToRotationVector2() * (0.8f + activity * 0.9f);
                Main.EntitySpriteDraw(weaponTexture, drawPosition + offset, null, chargedInner * innerAlpha, rotation, origin, Projectile.scale * (1f + activity * 0.02f), effects, 0);
            }

            if (rightPulse > 0.05f)
            {
                float pulseRadius = 2.4f + rightPulse * 4.2f;
                float pulseOpacity = rightPulse * 0.42f;
                int pulseDraws = 18;
                for (int i = 0; i < pulseDraws; i++)
                {
                    float completion = i / (float)pulseDraws;
                    float angle = MathHelper.TwoPi * completion + time * 1.45f;
                    float wave = 0.9f + 0.1f * (float)Math.Sin(time * 7.5f + i * 0.73f);
                    Vector2 offset = angle.ToRotationVector2() * pulseRadius * wave;
                    Color pulseColor = Color.Lerp(themeColor, accentColor, 0.35f + 0.25f * completion);
                    pulseColor.A = 0;

                    Main.EntitySpriteDraw(
                        weaponTexture,
                        drawPosition + offset,
                        null,
                        pulseColor * pulseOpacity,
                        rotation,
                        origin,
                        Projectile.scale,
                        effects,
                        0f);
                }
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
            Color naturalGlow = themeColor;
            Color white = Color.Lerp(accentColor, Color.White, 0.60f);

            float charge = MathHelper.Clamp(power, 0f, 1f);
            float flashPulse = idleCore ? 0f : charge;
            float time = Main.GlobalTimeWrappedHourly * CoreRotationSpeedMultiplier;

            // 绘制底光背景 BloomCircle
            // 激活状态参考 ApoctosisArray 使用随机抖动旋转，制造"能量不稳定"感；闲置时平滑旋转。
            float bloomOpacity = idleCore
                ? 0.14f + charge * 0.105f
                : 0.182f + charge * 0.154f + flashPulse * 0.21f;

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

            // star_06 単層自転
            {
                float pulseRate = idleCore ? 15f : (rightCharge ? 35f : 20f);
                float sine = MathHelper.Lerp((float)Math.Sin(Main.GlobalTimeWrappedHourly * pulseRate / MathHelper.Pi), 0.5f, 0.6f);
                float chargeRotationBoost = rightCharge ? time * 0.45f : 0f;
                float starRotation = direction.ToRotation() + chargeRotationBoost + MathHelper.PiOver4 + phaseKick;
                float starOpacity = idleCore
                    ? 0.224f + charge * 0.098f
                    : 0.336f + charge * 0.21f + flashPulse * 0.294f;
                Vector2 starScale = new Vector2(CoreStarScaleX, CoreStarScaleY * sine) * (idleCore ? 0.85f : 1.35f + charge * 0.55f);

                Main.EntitySpriteDraw(star, core, null,
                    Color.Lerp(naturalGlow, white, 0.55f) * starOpacity,
                    starRotation, star.Size() * 0.5f, starScale, SpriteEffects.None, 0);
            }

            // star_08 単層自転
            {
                float spinAngle = time * 0.85f;
                float flowerOpacity = idleCore ? 0.294f + charge * 0.14f : 0.42f + charge * 0.196f + flashPulse * 0.154f;
                float flowerScale = (0.7f + charge * 0.5f) * (idleCore ? 0.80f : 1f) * 0.25f * 0.6f; // ×0.25×0.6：star_08 512×512

                Main.EntitySpriteDraw(flower, core, null,
                    Color.Lerp(themeColor, Color.White, 0.58f) * flowerOpacity,
                    spinAngle + MathHelper.PiOver2, flower.Size() * 0.5f, flowerScale, SpriteEffects.None, 0);
            }
        }
    }
}
