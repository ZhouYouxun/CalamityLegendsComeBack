using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Holdout
{
    internal sealed partial class AethersWhisperHoldout
    {
        // 星芒核心落点：枪身机匣环芯处（略偏枪口一侧）。
        private Vector2 CoreBodyPosition => Projectile.Center + AimDirection * 6f;

        public override bool PreDraw(ref Color lightColor)
        {
            if (Owner is null) return false;

            Texture2D gun = TextureAssets.Projectile[Type].Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = gun.Size() * 0.5f;
            float rotation = Projectile.rotation;
            SpriteEffects fx = Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;

            float charge = ChargeFraction;
            float muzzle = Math.Max(muzzleFlashTimer / 14f, rightFlashTimer / 6f);
            float activity = MathHelper.Clamp(Math.Max(charge, muzzle), 0f, 1f);
            Vector2 aim = AimDirection;
            SpriteBatch sb = Main.spriteBatch;

            // —— 批次①：AlphaBlend 打底（发光包边 + 枪体本体）——
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            DrawWeaponOutline(gun, drawPos, rotation, origin, fx, activity);
            sb.Draw(gun, drawPos, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, fx, 0f);

            // —— 批次②：Additive（星芒核心 + 蓄力/枪口辉光）——
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // 常驻底光星芒——让武器「活着」；蓄力/开火时功率抬高、颜色向紫。
            float corePower = 0.18f + activity * 0.72f;
            AethersWhisperVisuals.DrawStarCore(sb, CoreBodyPosition, aim, corePower, charge, starPhaseKick);

            DrawChargeGlow(sb, aim, charge);
            DrawMuzzleGlow(sb, aim);

            // —— 恢复 AlphaBlend ——
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Owner.heldProj = Projectile.whoAmI;
            return false;
        }

        // 发光包边：把枪体多拷贝偏移绘制成一圈青紫光晕（军械库/BF 同款「描边」手法，零贴图）。
        private void DrawWeaponOutline(Texture2D gun, Vector2 drawPos, float rotation, Vector2 origin, SpriteEffects fx, float activity)
        {
            float idlePulse = 0.72f + 0.28f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.5f + Projectile.identity * 0.4f);
            float dist = 1.4f + activity * 2.8f;
            int copies = 8 + (int)(activity * 5f);
            float time = Main.GlobalTimeWrappedHourly;
            Color outer = AethersWhisperVisuals.ShimmerCyan with { A = 0 };
            Color inner = AethersWhisperVisuals.AetherPurple with { A = 0 };
            float outerA = (0.14f + activity * 0.34f) * idlePulse;
            float innerA = 0.1f + activity * 0.22f;

            for (int i = 0; i < copies; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / copies + time * 0.7f).ToRotationVector2() * dist;
                Main.EntitySpriteDraw(gun, drawPos + offset, null, outer * outerA, rotation, origin, Projectile.scale, fx, 0f);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = (MathHelper.PiOver2 * i - time * 0.5f).ToRotationVector2() * (0.7f + activity * 0.8f);
                Main.EntitySpriteDraw(gun, drawPos + offset, null, inner * innerA, rotation, origin, Projectile.scale, fx, 0f);
            }
        }

        // 蓄力辉光：冷青薄膜从准星方向被吸回枪口（环逐渐靠近枪口、收小），满蓄收成珠白点。
        private void DrawChargeGlow(SpriteBatch sb, Vector2 aim, float charge)
        {
            if (chargeTicks <= 0) return;
            Vector2 tip = GunTip;

            AethersWhisperVisuals.DrawShimmerRing(sb, tip, 18f, Main.GlobalTimeWrappedHourly * 1.5f, 0.4f + charge * 0.4f);

            if (!IsFullCharge)
            {
                int rings = 2 + (int)(charge * 2f);
                for (int i = 0; i < rings; i++)
                {
                    float phase = (i + 1f) / (rings + 1f);
                    float d = MathHelper.Lerp(120f, 16f, charge) * phase;
                    float r = MathHelper.Lerp(32f, 12f, charge) * (1.1f - phase * 0.3f);
                    AethersWhisperVisuals.DrawShimmerRing(sb, tip + aim * d, r, -Main.GlobalTimeWrappedHourly * (1f + i), 0.3f + charge * 0.35f);
                }
            }
            else
            {
                // 满蓄：一枚不断收缩的珠白微光卵
                float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
                AethersWhisperVisuals.DrawEnergyOrb(sb, tip, 26f * pulse, AethersWhisperVisuals.ToWhite(AethersWhisperVisuals.ShimmerCyan, 0.5f), 0.9f, new Vector2(1f, 1f));
            }
        }

        private void DrawMuzzleGlow(SpriteBatch sb, Vector2 aim)
        {
            float p = Math.Max(muzzleFlashTimer / 14f, rightFlashTimer / 6f);
            if (p <= 0.02f) return;
            Vector2 tip = GunTip;
            AethersWhisperVisuals.DrawEnergyOrb(sb, tip, 34f * p, AethersWhisperVisuals.ShimmerCyan, p, new Vector2(1.3f, 0.7f));
            Texture2D bloom = AethersWhisperVisuals.BloomCircle.Value;
            sb.Draw(bloom, tip - Main.screenPosition, null, AethersWhisperVisuals.PearlWhite with { A = 0 } * p,
                aim.ToRotation(), bloom.Size() * 0.5f, new Vector2(0.4f, 0.16f) * (1f + p), SpriteEffects.None, 0f);
        }
    }
}
