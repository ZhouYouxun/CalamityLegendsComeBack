using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.TheExoPrism
{
    internal class ExoPrism_Lazer : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles";

        // 这些参数不再用 const，而是字段，便于 OnSpawn 调整
        private int Lifetime;
        private float MaxBeamScale;
        private float MaxBeamLength;
        private float BeamTileCollisionWidth;
        private float BeamHitboxCollisionWidth;
        private int NumSamplePoints;
        private float BeamLengthChangeFactor;

        private Vector2 beamVector = Vector2.Zero;

        public override void OnSpawn(IEntitySource source)
        {
            // 在这里统一赋值，方便运行时热重载修改
            Lifetime = 14;
            MaxBeamScale = 1.2f;
            MaxBeamLength = 1200f;
            BeamTileCollisionWidth = 1f;
            BeamHitboxCollisionWidth = 16f;
            NumSamplePoints = 3;
            BeamLengthChangeFactor = 0.75f;

            Projectile.timeLeft = Lifetime;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
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

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // ================= 在敌人下方生成几何体 =================

            Vector2 baseCenter = target.Center + new Vector2(0f, 16f * 16f); // 下方16

            float radius = 10f * 16f; // 半径

            // 随机一个圆内点
            Vector2 spawnOffset = Main.rand.NextVector2Circular(radius, radius);
            Vector2 spawnPos = baseCenter + spawnOffset;

            // 朝敌人方向
            Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 10f;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPos,
                velocity,
                ModContent.ProjectileType<ExoPrism_Geometry>(),
                (int)(Projectile.damage * 2f), // 2倍伤害
                Projectile.knockBack,
                Projectile.owner
            );
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
            // ================= 基础保护 =================
            if (beamVector == Vector2.Zero || Projectile.ai[0] <= 2f)
                return;

            // ================= 可调参数 =================
            float particleSpacing = 28f;   // 采样间距，略稀一点，给几何结构留空间
            int minPoints = 8;
            int maxPoints = 26;

            float bloomThickness = 0.95f;
            int bloomLifetime = 14;

            Vector2 start = Projectile.Center;
            Vector2 end = Projectile.Center + beamVector * Projectile.ai[0];
            Vector2 normal = beamVector.RotatedBy(MathHelper.PiOver2);

            float globalTime = Main.GlobalTimeWrappedHourly;
            float beamLength = Projectile.ai[0];

            // ================= EXO调色板（同源风格） =================
            Color[] exoPalette =
            {
        Color.OrangeRed,
        Color.MediumTurquoise,
        Color.Orange,
        Color.LawnGreen
    };

            Color GetExoColor(float offset)
            {
                float rate = globalTime * 8f + offset;
                int colorIndex = (int)(rate / 2f % exoPalette.Length);
                Color currentColor = exoPalette[colorIndex];
                Color nextColor = exoPalette[(colorIndex + 1) % exoPalette.Length];
                float lerpValue = rate % 2f;
                if (lerpValue > 1f)
                    lerpValue = 1f;

                return Color.Lerp(currentColor, nextColor, lerpValue);
            }

            // ================= 主外晕：整条激光只保留一条 =================
            BloomLineVFX bloomLine = new BloomLineVFX(
                start,
                beamVector * beamLength,
                bloomThickness,
                Color.Lerp(Color.White, GetExoColor(0.15f), 0.55f) * (Projectile.Opacity * 0.30f),
                bloomLifetime
            );
            GeneralParticleHandler.SpawnParticle(bloomLine);

            // ================= 路径采样 =================
            int points = Math.Clamp((int)(beamLength / particleSpacing), minPoints, maxPoints);

            for (int i = 0; i < points; i++)
            {
                float completion = points == 1 ? 0f : i / (float)(points - 1);
                Vector2 basePos = Vector2.Lerp(start, end, completion);

                // =========================================================
                // ① 双轨螺旋偏移
                // 沿激光两侧做数学化摆动，形成“展开中的几何光轨”
                // =========================================================
                float phase = globalTime * 5.2f + completion * MathHelper.TwoPi * 3.1f + Projectile.identity * 0.17f;
                float sideAmplitude = MathHelper.Lerp(3f, 10f, (float)Math.Sin(completion * MathHelper.Pi));
                Vector2 sideOffsetA = normal * (float)Math.Sin(phase) * sideAmplitude;
                Vector2 sideOffsetB = -sideOffsetA;

                Vector2 posA = basePos + sideOffsetA;
                Vector2 posB = basePos + sideOffsetB;

                Color beamColorA = GetExoColor(completion * 1.2f + 0.15f);
                Color beamColorB = GetExoColor(completion * 1.2f + 0.65f);

                // =========================================================
                // ② 轨道节点：SquishyLightParticle
                // 两侧小节点，密度不高，但规律明确
                // =========================================================
                if (Main.rand.NextBool(2))
                {
                    SquishyLightParticle exoNodeA = new SquishyLightParticle(
                        posA,
                        normal * Main.rand.NextFloat(0.35f, 1.1f),
                        0.23f,
                        beamColorA,
                        20,
                        opacity: 0.9f,
                        squishStrenght: 1f,
                        maxSquish: 2.8f,
                        hueShift: 0f
                    );
                    GeneralParticleHandler.SpawnParticle(exoNodeA);
                }

                if (Main.rand.NextBool(2))
                {
                    SquishyLightParticle exoNodeB = new SquishyLightParticle(
                        posB,
                        -normal * Main.rand.NextFloat(0.35f, 1.1f),
                        0.23f,
                        beamColorB,
                        20,
                        opacity: 0.9f,
                        squishStrenght: 1f,
                        maxSquish: 2.8f,
                        hueShift: 0f
                    );
                    GeneralParticleHandler.SpawnParticle(exoNodeB);
                }

                // =========================================================
                // ③ 中轴亮刺：CustomSpark
                // 像几何体投影中心线，强调“数学骨架”
                // =========================================================
                if (Main.rand.NextBool(3))
                {
                    Vector2 sparkDir = beamVector.RotatedByRandom(0.10f) * Main.rand.NextFloat(0.15f, 0.55f);

                    Particle centerSpark = new CustomSpark(
                        basePos,
                        sparkDir,
                        "CalamityMod/Particles/SmallBloom",
                        false,
                        8,
                        Main.rand.NextFloat(0.14f, 0.24f),
                        Color.Lerp(Color.White, GetExoColor(completion + 0.33f), 0.7f),
                        new Vector2(1f, 1f),
                        true,
                        true,
                        0f,
                        false,
                        false,
                        0.20f
                    );
                    GeneralParticleHandler.SpawnParticle(centerSpark);
                }

                // =========================================================
                // ④ 对称斜向切线：GlowSparkParticle
                // 不是乱撒，而是两边镜像切出，保持几何感
                // =========================================================
                if (i % 3 == 0)
                {
                    Vector2 tangentVelA = (beamVector + normal * 0.35f).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(1.2f, 2.4f);
                    Vector2 tangentVelB = (beamVector - normal * 0.35f).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(1.2f, 2.4f);

                    Particle glowA = new GlowSparkParticle(
                        posA,
                        tangentVelA,
                        false,
                        8,
                        Main.rand.NextFloat(0.010f, 0.015f) * 2f,
                        beamColorA,
                        new Vector2(2f, 1f),
                        true,
                        false,
                        1.25f
                    );
                    GeneralParticleHandler.SpawnParticle(glowA);

                    Particle glowB = new GlowSparkParticle(
                        posB,
                        tangentVelB,
                        false,
                        8,
                        Main.rand.NextFloat(0.010f, 0.015f) * 2f,
                        beamColorB,
                        new Vector2(2f, 1f),
                        true,
                        false,
                        1.25f
                    );
                    GeneralParticleHandler.SpawnParticle(glowB);
                }

                // =========================================================
                // ⑤ 端点火花：GenericSparkle
                // 周期性在双轨中点附近放一点“星形数学节点”
                // =========================================================
                if (i % 4 == 1 && Main.rand.NextBool(2))
                {
                    Vector2 sparklePos = basePos + normal * (float)Math.Cos(phase * 0.8f) * (sideAmplitude * 0.35f);

                    GenericSparkle sparkle = new GenericSparkle(
                        sparklePos,
                        Vector2.Zero,
                        beamColorA,
                        Color.Lerp(Color.White, beamColorB, 0.45f),
                        Main.rand.NextFloat(1.15f, 1.75f),
                        7,
                        Main.rand.NextFloat(-0.02f, 0.02f),
                        1.55f
                    );
                    GeneralParticleHandler.SpawnParticle(sparkle);
                }

                // =========================================================
                // ⑥ 少量 RainbowTorch Dust
                // 用最少量的 Dust 把同源色味道补回来
                // =========================================================
                if (Main.rand.NextBool(3))
                {
                    Vector2 dustPos = basePos + normal * Main.rand.NextFloatDirection() * Main.rand.NextFloat(1f, sideAmplitude * 0.5f);

                    Dust exoDust = Dust.NewDustPerfect(
                        dustPos,
                        DustID.RainbowTorch,
                        beamVector.RotatedByRandom(0.25f) * Main.rand.NextFloat(0.2f, 0.8f),
                        0,
                        GetExoColor(completion + Main.rand.NextFloat(0.5f)),
                        Main.rand.NextFloat(0.8f, 1.2f)
                    );
                    exoDust.noGravity = true;
                    exoDust.velocity *= 0.35f;
                }
            }
        }







    }
}
