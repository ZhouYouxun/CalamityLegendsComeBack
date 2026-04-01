using CalamityMod.Buffs.DamageOverTime;
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
            target.AddBuff(ModContent.BuffType<MiracleBlight>(), 300); // 超位崩解

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


            // ================= 上方激光（ExoPrism_Light） =================

            // 上方距离：比下方更远一点（比如 24格）
            Vector2 topCenter = target.Center - new Vector2(0f, 24f * 16f);

            // 同样做一个随机圆分布
            float radiusTop = 12f * 16f;
            Vector2 topOffset = Main.rand.NextVector2Circular(radiusTop, radiusTop);
            Vector2 spawnPosTop = topCenter + topOffset;

            // 向目标射下来
            Vector2 velocityTop = (target.Center - spawnPosTop).SafeNormalize(Vector2.UnitY) * 12f;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosTop,
                velocityTop,
                ModContent.ProjectileType<ExoPrism_Light>(), // ← 你的光线类型
                (int)(Projectile.damage * 1.6f), // 稍微低一点，避免太爆
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
            float particleSpacing = 13f; // 原来 28，这里直接翻倍以上采样密度，把中间空隙填满
            int minPoints = 16;
            int maxPoints = 52;

            float bloomThickness = 0.92f;
            int bloomLifetime = 10; // 原来 14，缩短约 30%

            Vector2 start = Projectile.Center;
            float beamLength = Projectile.ai[0];
            Vector2 normal = beamVector.RotatedBy(MathHelper.PiOver2);

            float globalTime = Main.GlobalTimeWrappedHourly;

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

            // ================= 主外晕：整条激光保留一条主光骨架 =================
            BloomLineVFX bloomLine = new BloomLineVFX(
                start,
                beamVector * beamLength,
                bloomThickness,
                Color.Lerp(Color.White, GetExoColor(0.15f), 0.58f) * (Projectile.Opacity * 0.30f),
                bloomLifetime
            );
            GeneralParticleHandler.SpawnParticle(bloomLine);

            // ================= 路径采样 =================
            int points = Math.Clamp((int)(beamLength / particleSpacing), minPoints, maxPoints);

            for (int i = 0; i < points; i++)
            {
                float completion = points == 1 ? 0f : i / (float)(points - 1);
                Vector2 basePos = Vector2.Lerp(start, start + beamVector * beamLength, completion);

                // =========================================================
                // ① 单螺旋主轨
                // 不是双边对称撒点，而是一条沿光束缠绕前进的主螺旋
                // =========================================================
                float profile = (float)Math.Sin(completion * MathHelper.Pi); // 中段最强，两端自然收束
                float helixAmplitude = MathHelper.Lerp(2.4f, 9.2f, profile);
                float helixPhase = globalTime * 8.8f + completion * MathHelper.TwoPi * 4.35f + Projectile.identity * 0.19f;

                Vector2 helixOffset = normal * (float)Math.Sin(helixPhase) * helixAmplitude;
                Vector2 helixPos = basePos + helixOffset;

                // 颜色沿路径持续前进，每个采样点都错开一点
                Color beamColor = GetExoColor(completion * 2.2f + 0.15f);
                Color beamColor2 = GetExoColor(completion * 2.2f + 0.55f);
                Color beamColor3 = GetExoColor(completion * 2.2f + 0.95f);

                // =========================================================
                // ② 高频侧节点：SquishyLightParticle
                // 频率翻倍后改成几乎每点都补节点，但寿命缩短
                // =========================================================
                Vector2 nodeVelocity =
                    (-beamVector * 0.55f + normal * (float)Math.Cos(helixPhase) * 0.75f) *
                    MathHelper.Lerp(0.75f, 1.35f, profile);

                SquishyLightParticle exoNode = new SquishyLightParticle(
                    helixPos,
                    nodeVelocity,
                    0.20f,
                    beamColor,
                    14, // 原先 20，这里缩短约 30%
                    opacity: 0.92f,
                    squishStrenght: 1f,
                    maxSquish: 2.55f,
                    hueShift: 0f
                );
                GeneralParticleHandler.SpawnParticle(exoNode);

                // =========================================================
                // ③ 中轴骨架：SmallBloom 中轴亮刺
                // 规律性更强，不用随机，让整条光束有明确“数学骨架”
                // =========================================================
                if ((i & 1) == 0)
                {
                    float centerSwing = (float)Math.Sin(helixPhase * 0.5f) * (MathHelper.Pi / 14f);
                    Vector2 centerDir = beamVector.RotatedBy(centerSwing) * 0.48f;

                    Particle centerSpark = new CustomSpark(
                        basePos,
                        centerDir,
                        "CalamityMod/Particles/SmallBloom",
                        false,
                        6, // 原先 8，缩短约 25%~30%
                        0.17f,
                        Color.Lerp(Color.White, beamColor2, 0.72f),
                        new Vector2(1f, 1f),
                        true,
                        true,
                        0f,
                        false,
                        false,
                        0.16f
                    );
                    GeneralParticleHandler.SpawnParticle(centerSpark);
                }

                // =========================================================
                // ④ 单螺旋切割：GlowSparkParticle
                // 角度不是简单固定斜切，而是沿螺旋相位来回摆动，形成循环切割
                // =========================================================
                float cutSwing = (float)Math.Sin(helixPhase * 1.65f);
                float cutAngle = cutSwing * (MathHelper.Pi / 3.6f); // 往复摆动角
                Vector2 cutDir = beamVector.RotatedBy(cutAngle) * MathHelper.Lerp(1.55f, 2.95f, (cutSwing + 1f) * 0.5f);

                Particle glowCut = new GlowSparkParticle(
                    helixPos,
                    cutDir,
                    false,
                    6, // 原先 8，缩短约 25%~30%
                    0.022f,
                    beamColor2,
                    new Vector2(2.25f, 1f),
                    true,
                    false,
                    1.18f
                );
                GeneralParticleHandler.SpawnParticle(glowCut);

                // =========================================================
                // ⑤ 数学节点：菱形四向节点
                // 用规则四向 CustomSpark 替代 GenericSparkle，几何感会强很多
                // =========================================================
                if (i % 2 == 1)
                {
                    float nodeRotation = (float)Math.Sin(helixPhase * 0.55f) * (MathHelper.Pi / 6f);
                    Vector2 nodeCenter = basePos + normal * (float)Math.Cos(helixPhase * 0.5f) * (helixAmplitude * 0.22f);

                    for (int k = 0; k < 4; k++)
                    {
                        float localAngle = nodeRotation + (MathHelper.PiOver2 * k);
                        Vector2 localDir = beamVector.RotatedBy(localAngle) * (k % 2 == 0 ? 0.95f : 1.15f);

                        Particle diamondNode = new CustomSpark(
                            nodeCenter,
                            localDir,
                            "CalamityMod/Particles/SmallBloom",
                            false,
                            5, // 更短，更密
                            0.13f,
                            GetExoColor(completion * 2.2f + k * 0.22f),
                            new Vector2(1f, 1f),
                            true,
                            true,
                            0f,
                            false,
                            false,
                            0.14f
                        );
                        GeneralParticleHandler.SpawnParticle(diamondNode);
                    }
                }

                // =========================================================
                // ⑥ 螺旋补切：第二道短切线
                // 用更短、更浅的切线把前面空隙补满，让画面连续
                // =========================================================
                if (i % 3 != 1)
                {
                    float backSwing = (float)Math.Cos(helixPhase * 1.35f);
                    float backAngle = backSwing * (MathHelper.Pi / 5.5f);
                    Vector2 backCutDir = (-beamVector).RotatedBy(backAngle) * 1.15f;

                    Particle backCut = new GlowSparkParticle(
                        helixPos - beamVector * 2f,
                        backCutDir,
                        false,
                        5,
                        0.015f,
                        beamColor3,
                        new Vector2(1.65f, 1f),
                        true,
                        false,
                        1.10f
                    );
                    GeneralParticleHandler.SpawnParticle(backCut);
                }
            }
        }




    }
}
