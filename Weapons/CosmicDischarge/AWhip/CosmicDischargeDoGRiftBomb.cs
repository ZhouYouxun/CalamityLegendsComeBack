using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeDoGRiftBomb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float DetonateDelay => ref Projectile.ai[0];
        private ref float Time => ref Projectile.ai[1];
        private bool detonated;

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 80;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => !detonated;

        public override bool? CanDamage() => detonated && Time <= DetonateDelay + 3f;

        public override void AI()
        {
            if (DetonateDelay <= 0f)
                DetonateDelay = 22f;

            Time++;
            Projectile.velocity *= 0.94f;
            Projectile.rotation += 0.08f;
            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.DoGSpecialColor.ToVector3() * 0.3f);

            if (!detonated && Time >= DetonateDelay)
                Detonate();

            if (detonated)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.Opacity = Utils.GetLerpValue(DetonateDelay + 15f, DetonateDelay, Time, true);
                if (Time >= DetonateDelay + 16f)
                    Projectile.Kill();
            }
            else
            {
                // 飞行拖尾走统一的 DoGFire 配比，不再为这个弹幕单造一套粒子。
                Vector2 back = Projectile.velocity.SafeNormalize(Vector2.UnitX) * -1f;
                CosmicDischargeCommon.SpawnTrailWake(Projectile.Center, back, CosmicDischargeCommon.RiftTwilight, 0.7f);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!detonated)
                return false;

            Vector2 closest = Vector2.Clamp(targetHitbox.Center.ToVector2(), targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.DistanceSquared(closest, Projectile.Center) <= 92f * 92f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CosmicDischargeCommon.ApplyDoGDebuffs(target, 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = bloom.Size() * 0.5f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            if (!detonated)
            {
                // ---- 飞行中的外观：一个小光晕球 ----
                float pulse = 0.7f + 0.18f * MathF.Sin(Time * 0.35f); // 脉冲系数：在0.52~0.88之间随正弦波动，让光晕呼吸闪烁【调大0.18让闪烁幅度更大】
                Main.EntitySpriteDraw(
                    bloom,                                               // 贴图：BloomCircle（圆形光晕）
                    Projectile.Center - Main.screenPosition,             // 绘制位置：弹幕世界坐标转屏幕坐标
                    null,
                    CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * 0.32f, // 颜色：青/洋红循环渐变色，透明度0.32【调大0.32让光晕更亮】
                    Projectile.rotation,                                 // 旋转：跟随弹幕自转
                    origin,                                              // 旋转/缩放的锚点：贴图中心
                    0.18f * pulse,                                       // 大小：基础0.18乘脉冲系数，最终约0.094~0.159【调大0.18让光晕球变大】
                    SpriteEffects.None);
                Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
                return false;
            }

            // ---- 爆炸后的外观：扩散光晕圆 ----
            float progress = Utils.GetLerpValue(DetonateDelay, DetonateDelay + 16f, Time, true); // 扩散进度：爆炸后16帧内从0到1
            float fade = Utils.GetLerpValue(DetonateDelay + 16f, DetonateDelay + 4f, Time, true); // 淡出系数：第4~16帧之间从1到0（逐渐消失）
            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.RiftMagenta) * 0.3f * fade, // 颜色：洋红，最高透明度0.3，随淡出降低【调大0.3让爆炸余晖更亮】
                0f,
                origin,
                MathHelper.Lerp(0.35f, 1.45f, progress), // 大小：从0.35扩散到1.45（爆炸膨胀效果）【调大1.45让扩散圈更大】
                SpriteEffects.None);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private void Detonate()
        {
            detonated = true;
            Projectile.Resize(184, 184);
            Projectile.Damage();
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftOpen") { Volume = 0.42f, Pitch = 0.25f, MaxInstances = 4 }, Projectile.Center);
            ApplyScreenShake(4.4f);

            // 裂隙炸弹是"量大"的弹幕（一次挥鞭放 3~4 颗），所以只给 Light 档。
            // 单颗看着克制，成组炸开时靠数量堆出规模 —— 而不是每颗都自带一套大爆炸。
            CosmicDischargeCommon.SpawnRiftBurst(Projectile.Center, RiftTier.Light, default, CosmicDischargeCommon.RiftTwilight);
            CosmicDischargeCommon.SpawnDistortionBurst(Projectile.Center, 4, 22f, 30f);
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1100f, 100f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }
    }
}
