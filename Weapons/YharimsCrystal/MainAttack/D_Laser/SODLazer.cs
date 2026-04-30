using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.Graphics.Effects;
using Terraria.GameContent.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using CalamityMod.Particles;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.D_Laser
{
    internal class SODLazer : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/Magic/YharimsCrystalBeam";

        // 这些参数不再用 const，而是字段，便于 OnSpawn 调整
        private int Lifetime = 14;
        private float MaxBeamScale = 1.2f;
        private float MaxBeamLength = 1200f;
        private float BeamHitboxCollisionWidth = 16f;

        private Vector2 beamVector = Vector2.Zero;

        public override void OnSpawn(IEntitySource source)
        {
            // 在这里统一赋值，方便运行时热重载修改
            Lifetime = 14;
            MaxBeamScale = 1.2f;
            MaxBeamLength = 1200f;
            BeamHitboxCollisionWidth = 16f;

            Projectile.timeLeft = Lifetime;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.alpha = 0;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            // 第一帧确定方向向量
            if (Projectile.velocity != Vector2.Zero)
            {
                beamVector = Vector2.Normalize(Projectile.velocity);
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.velocity = Vector2.Zero;
            }

            // 激光随时间衰减
            float power = (float)Projectile.timeLeft / Lifetime;
            Projectile.scale = MaxBeamScale * power;

            // 碰到方块就停下
            //{
            //    // 用 LaserScan 探测最大长度
            //    float[] laserScanResults = new float[NumSamplePoints];
            //    float scanWidth = Projectile.scale < 1f ? 1f : Projectile.scale;
            //    Collision.LaserScan(Projectile.Center, beamVector, BeamTileCollisionWidth * scanWidth, MaxBeamLength, laserScanResults);
            //    float avg = 0f;
            //    for (int i = 0; i < laserScanResults.Length; ++i)
            //        avg += laserScanResults[i];
            //    avg /= NumSamplePoints;
            //    Projectile.ai[0] = MathHelper.Lerp(Projectile.ai[0], avg, BeamLengthChangeFactor);
            //}

            // ★ 无视方块：光束直接使用最大长度，不做 Tile 扫描
            Projectile.ai[0] = MaxBeamLength;


            // 生成额外特效：在激光路径上撒粒子与光晕
            ProduceBeamDust();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (projHitbox.Intersects(targetHitbox))
                return true;

            float _ = float.NaN;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + beamVector * Projectile.ai[0],
                BeamHitboxCollisionWidth * Projectile.scale,
                ref _
            );
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = (Projectile.Center.X < target.Center.X).ToDirectionInt();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (beamVector == Vector2.Zero || Projectile.velocity != Vector2.Zero)
                return false;

            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            float beamLength = Projectile.ai[0];
            Vector2 centerFloored = Projectile.Center.Floor() + beamVector * Projectile.scale * 10f;
            Vector2 scaleVec = new Vector2(Projectile.scale);

            // 颜色：偏金白色的主体光束
            Color beamColor = Color.Lerp(Color.White, Color.Gold, 0.55f);

            DelegateMethods.f_1 = 1f;
            Vector2 beamStartPos = centerFloored - Main.screenPosition;
            Vector2 beamEndPos = beamStartPos + beamVector * beamLength;
            Utils.LaserLineFraming llf = new Utils.LaserLineFraming(DelegateMethods.RainbowLaserDraw);

            // 外层主束：略透明的金白色
            DelegateMethods.c_1 = beamColor * 0.85f * Projectile.Opacity;
            Utils.DrawLaser(Main.spriteBatch, tex, beamStartPos, beamEndPos, scaleVec, llf);

            // 内层叠加几层逐渐更细更白的光束
            for (int i = 0; i < 4; ++i)
            {
                beamColor = Color.Lerp(beamColor, Color.White, 0.5f);
                scaleVec *= 0.8f;
                DelegateMethods.c_1 = beamColor * 0.5f * Projectile.Opacity;
                Utils.DrawLaser(Main.spriteBatch, tex, beamStartPos, beamEndPos, scaleVec, llf);
            }

            return false;
        }

        public void ProduceBeamDust()
        {
            // ========== 可调参数 ==========
            float particleSpacing = 26f;    // 粒子间隔（越小越紧凑）
            int minPoints = 6;              // 最少采样点数
            int maxPoints = 30;             // 最多采样点数

            // BloomLine 设置（外层柔和金光，透明一点）
            float bloomThickness = 0.9f;    // BloomLine 粗细
            int bloomLifetime = 16;         // BloomLine 寿命
            Color bloomColor = Color.Lerp(Color.White, Color.Gold, 0.75f);

            // 其他粒子设置
            float exoVelocityMult = 2.6f;   // “EXO之光”速度倍率
            float pointVelocityMult = 1.8f; // 点刺粒子速度倍率
            // ==============================

            if (beamVector == Vector2.Zero || Projectile.ai[0] <= 2f)
                return;

            Vector2 start = Projectile.Center;
            Vector2 end = Projectile.Center + beamVector * Projectile.ai[0];

            // ====== BloomLine：覆盖整条激光的淡金色外晕 ======
            BloomLineVFX bloomLine = new BloomLineVFX(
                start,
                beamVector * Projectile.ai[0], // 覆盖整条激光
                bloomThickness,
                bloomColor * (Projectile.Opacity * 0.35f), // 透明度稍低，避免完全吞掉本体
                bloomLifetime
            );
            GeneralParticleHandler.SpawnParticle(bloomLine);

            // ====== 计算路径采样点 ======
            int points = Math.Clamp((int)(Projectile.ai[0] / particleSpacing), minPoints, maxPoints);

            for (int i = 0; i < points; i++)
            {
                float t = (points == 1) ? 0f : i / (float)(points - 1);
                Vector2 pos = Vector2.Lerp(start, end, t);

                // 在光束附近做一点随机偏移，让覆盖层不那么规则
                Vector2 randomOffset = Main.rand.NextVector2Circular(6f, 6f);
                Vector2 sparkPos = pos + randomOffset;

                // ① EXO风格软星光：白金色主光（沿着激光两侧乱跳）
                if (Main.rand.NextBool(2))
                {
                    SquishyLightParticle exoEnergy = new SquishyLightParticle(
                        sparkPos,
                        // 朝光束法线方向随机散射
                        (beamVector.RotatedBy(MathHelper.PiOver2 * (Main.rand.NextBool() ? 1 : -1)))
                            * Main.rand.NextFloat(0.8f, 2.0f) * exoVelocityMult,
                        0.26f,
                        Color.Lerp(Color.White, Color.Gold, 0.6f),
                        22,
                        opacity: 0.95f,
                        squishStrenght: 1f,
                        maxSquish: 3f,
                        hueShift: 0f
                    );
                    GeneralParticleHandler.SpawnParticle(exoEnergy);
                }

                // ② 十字星火花：沿激光铺一层细碎的“星爆颗粒”
                if (Main.rand.NextBool(3))
                {
                    GenericSparkle sparker = new GenericSparkle(
                        sparkPos,
                        Vector2.Zero,
                        Color.Gold,                                // 主色：金色
                        Color.Lerp(Color.White, Color.Gold, 0.4f), // 光晕：偏暖白
                        Main.rand.NextFloat(1.4f, 2.2f),          // 随机缩放
                        7,                                         // 寿命略长一点
                        Main.rand.NextFloat(-0.02f, 0.02f),       // 轻微旋转
                        1.75f                                      // 光晕扩散
                    );
                    GeneralParticleHandler.SpawnParticle(sparker);
                }

                // ③ 点刺型粒子：像碎光针一样贴着光束刺出
                if (Main.rand.NextBool(2))
                {
                    Vector2 backDir = -beamVector.RotatedByRandom(0.35f);
                    PointParticle leftSpark = new PointParticle(
                        sparkPos,
                        backDir * Main.rand.NextFloat(0.8f, 2.0f) * pointVelocityMult,
                        false,
                        16,
                        1.05f + Main.rand.NextFloat(0.4f),
                        Color.Lerp(Color.Gold, Color.Orange, 0.5f)
                    );
                    GeneralParticleHandler.SpawnParticle(leftSpark);
                }

                // ④ 少量 Dust 小碎光，增强“气雾”感（数量控制得比较节制）
                if (Main.rand.NextBool(3))
                {
                    Dust d1 = Dust.NewDustPerfect(
                        sparkPos,
                        DustID.GoldFlame,
                        beamVector.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.4f, 1.0f)
                    );
                    d1.noGravity = true;
                    d1.scale = 1.0f + Main.rand.NextFloat(0.4f);
                    d1.fadeIn = 1.1f;
                }
            }
        }
    }
}
