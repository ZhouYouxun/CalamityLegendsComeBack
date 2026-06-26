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
            else if (!Main.dedServ && Main.rand.NextBool(3))
            {
                // ---- 飞行途中的粒子特效（每3帧随机触发一次） ----

                // 方块光点：弹幕飞行时拖出的小方块光
                GeneralParticleHandler.SpawnParticle(new GlowSquareParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), // 位置：弹幕中心周围±10像素随机偏移
                    Main.rand.NextVector2Circular(1.4f, 1.4f),                  // 速度：向随机方向漂移，最大1.4像素/帧
                    false,                                                        // 不受重力影响
                    12,                                                           // 持续时间：12帧
                    Main.rand.NextFloat(0.05f, 0.1f) * 0.3f,                      // 大小：0.05~0.1 * 0.3f
                    CosmicDischargeCommon.ThreeColorSpark,                        // 颜色：青/洋红/爱丽丝蓝三色随机
                    rotation: Main.rand.NextFloat(0.05f, 0.12f)));               // 旋转速度：0.05~0.12弧度/帧

                // 电弧火花：弹幕飞行时向后飞散的电弧粒子
                GeneralParticleHandler.SpawnParticle(new ElectricSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), // 位置：弹幕中心周围±12像素随机偏移
                    Projectile.velocity.RotatedByRandom(0.7f) * -0.35f,         // 速度：沿弹幕方向反向飞散，速度0.35
                    CosmicDischargeCommon.DoGCyanColor,                           // 颜色1（内层）：青色
                    CosmicDischargeCommon.DoGFuchsiaColor,                        // 颜色2（外层）：洋红色
                    0.45f * 0.3f,                                                 // 大小：0.45 * 0.3f
                    10,                                                            // 持续时间：10帧
                    MathHelper.PiOver4,                                            // 形状角度：45度
                    5f));                                                           // 亮度：5.0【调大让电弧更亮】
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
                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.3f * fade, // 颜色：洋红，最高透明度0.3，随淡出降低【调大0.3让爆炸余晖更亮】
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

            if (Main.dedServ)
                return;

            // ---- 爆炸时的粒子特效 ----

            // 扩散光环：整个爆炸的"冲击波光圈"
            GeneralParticleHandler.SpawnParticle(new PulseRing(
                Projectile.Center,
                Vector2.Zero,                                                               // 速度：静止不动，只是扩散
                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * 0.58f, // 颜色：青/洋红循环，透明度0.58
                0.05f * 0.3f,                                                                // 初始半径比例
                1.2f * 0.3f * CosmicDischargeCommon.ShockwaveFinalScaleMultiplier,            // 最大半径比例 (1.2 * 0.3f)
                18));                                                                        // 持续时间：18帧

            // 强烈光晕：爆炸中心的白热化光球
            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                Projectile.Center,
                Vector2.Zero,
                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.42f, // 颜色：洋红
                0.5f * 0.3f,                                                                        // 大小：0.5 * 0.3f
                16));                                                                               // 持续时间：16帧

            // 详细爆炸效果1：横向拉伸的青色爆炸云
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(
                Projectile.Center,
                Vector2.Zero,
                CosmicDischargeCommon.DoGCyanColor,          // 颜色：青色
                new Vector2(1.2f, 0.8f),                     // 拉伸比例
                0f,                                           // 旋转角度
                0.15f * 0.3f,                                 // 初始大小
                0.95f * 0.3f,                                 // 最终大小
                16));                                         // 持续时间：16帧

            // 详细爆炸效果2：纵向拉伸的洋红色爆炸云，与第一个错开60度
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(
                Projectile.Center,
                Vector2.Zero,
                CosmicDischargeCommon.DoGFuchsiaColor * 0.8f, // 颜色：洋红
                new Vector2(0.8f, 1.25f),                      // 拉伸比例
                MathHelper.Pi / 3f,                            // 旋转角度
                0.12f * 0.3f,                                   // 初始大小
                0.78f * 0.3f,                                   // 最终大小
                14));                                           // 持续时间：14帧

            // 裂隙裂缝线：从爆炸中心向外射出的裂纹弹幕
            // 参数依次：来源、位置、owner、数量5、最小速度3、最大速度8、最短存活14帧、最长存活22帧
            CosmicDischargeCommon.SpawnRiftCrackProjectiles(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.owner, 5, 3f, 8f, 14f, 22f);

            // 扭曲效果：空间扭曲波
            // 参数依次：位置、粒子数6、分组数3、外扩半径38、扭曲强度25【调小38减小扭曲范围，调小25减弱扭曲感】
            CosmicDischargeCommon.SpawnDistortionBurst(Projectile.Center, 6, 3, 38f, 25f);

            // 发光火花：12个向四周等间距飞散的细长光条
            for (int i = 0; i < 12; i++) // 12个火花，均匀分布一圈
            {
                Vector2 velocity = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * Main.rand.NextFloat(2.4f, 6.2f); // 速度：向外扩散2.4~6.2格/帧
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), // 位置：中心±8像素随机偏移
                    velocity,
                    true,                                    // 受重力影响
                    Main.rand.Next(12, 20),                  // 持续时间：12~20帧
                    Main.rand.NextFloat(0.55f, 0.95f) * 0.05f, // 大小：缩放到5%大小 (0.05f)
                    CosmicDischargeCommon.ThreeColorSpark,    // 颜色：三色随机
                    new Vector2(0.25f, 1.7f),                // 形状拉伸：宽0.25、高1.7
                    true));                                   // 发光
            }
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
