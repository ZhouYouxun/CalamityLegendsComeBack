using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using CalamityMod.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.EXSkill
{
    internal class SHPC_SuperLazer : BaseLaserbeamProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";

        public int OwnerIndex
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public override float MaxScale => 0.7f;
        public override float MaxLaserLength => 2000f;
        public override float Lifetime => 1800000; // 持续 X 帧
        public override Color LaserOverlayColor => new Color(90, 200, 255, 130); // 科技蓝(带Alpha)
        public override Color LightCastColor => Color.White;
        public override Texture2D LaserBeginTexture => Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/AresLaserBeamMiddle", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/AresLaserBeamEnd", AssetRequestMode.ImmediateLoad).Value;
        public override string Texture => "CalamityMod/Projectiles/Boss/AresLaserBeamStart";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 180000;
            Projectile.usesLocalNPCImmunity = true; // 弹幕使用本地无敌帧
            Projectile.localNPCHitCooldown = 2; // 无敌帧冷却时间为1帧
        }



        // 替换 AttachToSomething() 为下面版本：
        // 功能：每帧检查父弹幕，父存在 -> 刷新 timeLeft（保持永生）；父不存在 -> Kill()
        // 已使用类内的 OwnerIndex 属性 (ai[0]) 与之前的类型兼容判断。
        public override void AttachToSomething()
        {
            int ownerIndex = OwnerIndex; // 从 ai[0] 读取父弹幕索引
                                         // 索引越界或无效直接自毁
            if (!ownerIndex.WithinBounds(Main.maxProjectiles))
            {
                Projectile.Kill();
                return;
            }

            Projectile ownerProj = Main.projectile[ownerIndex];

            // 允许的父弹幕类型（兼容旧类型与 TEM00Left）
            int exType = ModContent.ProjectileType<NL_SHPC_EXWeapon>();

            // 父弹幕存在且活跃且为我们接受的类型 -> 绑定位置和朝向，并刷新寿命
            if (ownerProj != null && ownerProj.active && ownerProj.type == exType)
            {
                // 绑定到父弹幕的“枪口”位置与朝向（与你原实现一致）
                Vector2 dir = ownerProj.velocity.SafeNormalize(Vector2.UnitX);

                Projectile.Center = ownerProj.Center + dir * 18f;
                Projectile.rotation = dir.ToRotation();

                // --------- 关键：只要父弹幕存在，就不断刷新自身寿命，确保永远不死 ---------
                // 这里把 timeLeft 设为 2，基类/引擎会每帧调用 AttachToSomething 并把 timeLeft 再设回 2
                // 因此只要父弹幕活着，本弹幕就会一直存活；父消失则下方分支会 Kill()
                Projectile.timeLeft = 2;

                // （可选）如果你需要把“持续存在”状态同步到客户端，可在首次绑定时做一次 netUpdate
                // 但频繁 netUpdate 会增加流量，所以这里不每帧调用
            }
            else
            {
                // 父弹幕不存在/类型不匹配 -> 自毁
                // 为了多人同步，先设置 netUpdate 再 Kill()
                Projectile.netUpdate = true;
                Projectile.Kill();
            }
        }


        public override void UpdateLaserMotion()
        {
            // 不再寻敌，逻辑完全交给 AttachToSomething()
            Projectile.velocity = Projectile.rotation.ToRotationVector2();
        }


        // 命中VFX节流（避免每帧命中都刷爆）
        private int impactVfxCooldown;


        public override void OnSpawn(IEntitySource source)
        {
            impactVfxCooldown = 0; // 初始化节流计时器
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 仍可保留你的Debuff
            SoundEngine.PlaySound(SoundID.Item132 with { Volume = 0.8f, Pitch = -0.0f }, Projectile.Center);

            //// 8帧节流，避免localNPCHitCooldown=1导致每帧都生成海量粒子
            //if (impactVfxCooldown > 0)
            //    return;
            //impactVfxCooldown = 8;

            // ===== 计算与光束轴对齐的“命中点” =====
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            float t = Vector2.Dot(target.Center - Projectile.Center, dir);
            t = MathHelper.Clamp(t, 0f, LaserLength); // 限制在光束范围
            Vector2 hitPos = Projectile.Center + dir * t;
            Vector2 nrm = dir.RotatedBy(MathHelper.PiOver2); // 法线（用于“方”感的左右分布）

            // ===== 颜色与随机工具 =====
            Color coreC = new Color(120, 220, 255) * 1.42f;
            Color edgeC = new Color(80, 190, 255) * 1.28f;
            Color flashC = new Color(200, 245, 255) * 1.58f;

            float time = Main.GlobalTimeWrappedHourly * 7.5f + Projectile.identity * 0.21f;
            int helixSamples = 8;
            float impactRadius = 28f;

            for (int i = 0; i < helixSamples; i++)
            {
                float sample = i / (float)(helixSamples - 1f);
                float envelope = 0.28f + 0.72f * (float)Math.Sin(sample * MathHelper.Pi);
                float angle = time + sample * MathHelper.TwoPi * 1.55f;
                float offset = (float)Math.Sin(angle) * impactRadius * envelope;

                for (int strand = 0; strand < 2; strand++)
                {
                    float strandSign = strand == 0 ? 1f : -1f;
                    Vector2 strandPos = hitPos + nrm * offset * strandSign;
                    Vector2 swirlVelocity =
                        nrm * strandSign * (3.4f + 4.4f * envelope) +
                        dir * ((float)Math.Cos(angle + strand * MathHelper.Pi) * 3.6f);

                    GlowSparkParticle spark = new GlowSparkParticle(
                        strandPos,
                        swirlVelocity,
                        false,
                        Main.rand.Next(9, 13),
                        Main.rand.NextFloat(0.022f, 0.034f),
                        Color.Lerp(edgeC, flashC, 0.35f + 0.45f * envelope),
                        new Vector2(Main.rand.NextFloat(1.8f, 2.6f), Main.rand.NextFloat(0.26f, 0.45f)),
                        true,
                        false,
                        1.08f);
                    GeneralParticleHandler.SpawnParticle(spark);

                    if (i % 2 == strand)
                    {
                        SquishyLightParticle light = new SquishyLightParticle(
                            strandPos + Main.rand.NextVector2Circular(3f, 3f),
                            swirlVelocity * 0.22f,
                            Main.rand.NextFloat(0.28f, 0.42f),
                            Color.Lerp(coreC, flashC, 0.45f + 0.35f * envelope),
                            Main.rand.Next(12, 18));
                        GeneralParticleHandler.SpawnParticle(light);
                    }
                }
            }

            int ruptureCount = 10;
            for (int i = 0; i < ruptureCount; i++)
            {
                float spread = i / (float)(ruptureCount - 1f) - 0.5f;
                float outward = (float)Math.Sin((spread + 0.5f) * MathHelper.Pi);
                Vector2 spawnPos = hitPos + nrm * spread * 42f;
                Vector2 velocity =
                    nrm * spread * Main.rand.NextFloat(5.2f, 9.4f) +
                    dir * Main.rand.NextFloat(-3.8f, 3.8f) * outward;

                SquareParticle shard = new SquareParticle(
                    spawnPos,
                    velocity,
                    false,
                    18 + Main.rand.Next(10),
                    1.35f + Main.rand.NextFloat(0.7f),
                    i % 3 == 0 ? flashC : edgeC);
                GeneralParticleHandler.SpawnParticle(shard);
            }

            GlowOrbParticle orb = new GlowOrbParticle(
                hitPos,
                Vector2.Zero,
                false,
                7,
                1.28f,
                flashC,
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(orb);

            // 可选：轻音效（与小激光区分开）
            SoundEngine.PlaySound(SoundID.Item122, hitPos);
        }

        private SlotId LaserSoundSlot;
        private int soundTimer;
        public override void ExtraBehavior()
        {
            if (Main.dedServ)
                return;

            // 基础光照
            Lighting.AddLight(Projectile.Center, 0.10f, 0.28f, 0.55f);
        
            if (Projectile.velocity == Vector2.Zero)
                return;

            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 nrm = dir.RotatedBy(MathHelper.PiOver2);
            float time = Main.GlobalTimeWrappedHourly;
            const int intervalScale = 3;

            // 光束理论半宽
            float halfWidth = (Projectile.scale * Projectile.width + 180f) * 0.5f;
            Vector2 endPos = Projectile.Center + dir * LaserLength;

            // 颜色层次
            Color edgeColor = new Color(80, 190, 255) * 1.12f;
            Color coreColor = new Color(130, 228, 255) * 1.18f;
            Color flashColor = new Color(205, 245, 255) * 1.30f;

            // 间隔目标：约 8 像素
            // 但给一个上限，避免满长度时粒子量炸掉
            const float spacing = 8f * intervalScale;
            int sampleCount = (int)(LaserLength / spacing);
            sampleCount = Utils.Clamp(sampleCount, 48, 180);

            if ((Main.GameUpdateCount + Projectile.identity) % 2 == 0)
            {
                Particle pulse = new DirectionalPulseRing(
                    Projectile.Center,
                    Projectile.velocity * 0.75f,
                    Color.Lerp(coreColor, flashColor, 0.4f),
                    new Vector2(1f, 2.5f),
                    Projectile.rotation,
                    0.2f * 7f,
                    0.03f * 7f,
                    20);
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            for (int i = 0; i <= sampleCount; i++)
            {
                float completion = i / (float)sampleCount;

                // 基础采样点：沿光束线性推进
                Vector2 basePos = Vector2.Lerp(Projectile.Center, endPos, completion);

                // 中段更饱满，两端更收
                float envelope = 0.24f + 0.76f * (float)Math.Pow(Math.Sin(completion * MathHelper.Pi), 0.88f);

                // =========================
                // 1. 中轴波纹：给“每组之间”持续变化
                // =========================
                float centerWave =
                    (float)Math.Sin(time * 9.2f - completion * 22f + Projectile.identity * 0.31f) *
                    (2.0f + 4.5f * envelope);

                basePos += nrm * centerWave;

                // =========================
                // 2. 线性代数变换：把单位圆点做缩放+旋转，形成动态椭圆螺旋轨道
                // =========================
                float orbitPhase = time * 7.4f + completion * MathHelper.TwoPi * 5.2f + Projectile.identity * 0.17f;
                float matrixRotation =
                    time * 1.15f +
                    completion * MathHelper.TwoPi * 2.8f +
                    (float)Math.Sin(time * 2.6f + completion * 11f) * 0.22f;

                float scaleX =
                    halfWidth * (0.085f + 0.10f * envelope) *
                    (0.92f + 0.08f * (float)Math.Sin(time * 4.1f + completion * 15f));

                float scaleY =
                    (2.8f + 6.0f * envelope) *
                    (0.90f + 0.10f * (float)Math.Cos(time * 3.4f - completion * 13f));

                Matrix transform =
                    Matrix.CreateScale(scaleX, scaleY, 1f) *
                    Matrix.CreateRotationZ(matrixRotation);

                Vector2 unitCirclePoint = new Vector2(
                    (float)Math.Cos(orbitPhase),
                    (float)Math.Sin(orbitPhase));

                Vector2 ellipsePoint = Vector2.Transform(unitCirclePoint, transform);

                // 把局部椭圆坐标投影到世界空间：
                // X 轴映射到法线，Y 轴映射到光束方向
                Vector2 worldOffset = nrm * ellipsePoint.X + dir * ellipsePoint.Y;

                Vector2 primaryPos = basePos + worldOffset;
                Vector2 secondaryPos = basePos - worldOffset * 0.92f;

                float sineSign = (float)Math.Sin(orbitPhase) >= 0f ? 1f : -1f;
                Vector2 swirlDir = worldOffset.SafeNormalize(nrm).RotatedBy(MathHelper.PiOver2 * sineSign);

                // =========================
                // 3. 主细火花：每个采样点都生成，形成非常密的连续感
                // =========================
                GlowSparkParticle primarySpark = new GlowSparkParticle(
                    primaryPos,
                    swirlDir * (0.30f + 0.55f * envelope) + dir * (0.12f + 0.25f * envelope),
                    false,
                    Main.rand.Next(7, 10),
                    Main.rand.NextFloat(0.012f, 0.018f),
                    Color.Lerp(edgeColor, flashColor, 0.22f + 0.25f * envelope),
                    new Vector2(
                        Main.rand.NextFloat(1.00f, 1.35f),
                        Main.rand.NextFloat(0.14f, 0.22f)),
                    true,
                    false,
                    1.02f);
                //GeneralParticleHandler.SpawnParticle(primarySpark);

                // =========================
                // 4. 副轨火花：隔点补一次，做双股但不塞满
                // =========================
                if (i % (2 * intervalScale) == 0)
                {
                    GlowSparkParticle secondarySpark = new GlowSparkParticle(
                        secondaryPos,
                        -swirlDir * (0.22f + 0.40f * envelope) + dir * (0.08f + 0.20f * envelope),
                        false,
                        Main.rand.Next(6, 9),
                        Main.rand.NextFloat(0.010f, 0.016f),
                        Color.Lerp(coreColor, flashColor, 0.16f + 0.20f * envelope),
                        new Vector2(
                            Main.rand.NextFloat(0.90f, 1.20f),
                            Main.rand.NextFloat(0.12f, 0.18f)),
                        true,
                        false,
                        1.01f);
                    //GeneralParticleHandler.SpawnParticle(secondarySpark);
                }

                // =========================
                // 5. 柔光节点：降低频率，但参数持续变化，不再像老版本那样“隔很大一团”
                // =========================
                if (i % (4 * intervalScale) == 0)
                {
                    float lightDrift =
                        (float)Math.Cos(time * 6.5f + completion * 17f) *
                        (1.4f + 2.2f * envelope);

                    SquishyLightParticle light = new SquishyLightParticle(
                        basePos + nrm * lightDrift,
                        dir * 0.06f + swirlDir * 0.08f,
                        Main.rand.NextFloat(0.12f, 0.20f) * (0.90f + envelope * 0.35f),
                        Color.Lerp(coreColor, flashColor, 0.20f + 0.18f * envelope),
                        Main.rand.Next(9, 13));
                    //GeneralParticleHandler.SpawnParticle(light);
                }

                // =========================
                // 6. 中轴核心点：每 6 个采样点一个，但会跟着波纹轻微摆动
                // =========================
                if (i % (6 * intervalScale) == 0)
                {
                    float orbOffset =
                        (float)Math.Sin(time * 5.1f - completion * 19f + Projectile.identity * 0.29f) *
                        (1.0f + 2.6f * envelope);

                    GlowOrbParticle coreOrb = new GlowOrbParticle(
                        basePos + nrm * orbOffset,
                        Vector2.Zero,
                        false,
                        4,
                        0.46f + envelope * 0.14f,
                        Color.Lerp(coreColor, flashColor, 0.16f + 0.16f * envelope),
                        true,
                        false,
                        true);
                    //GeneralParticleHandler.SpawnParticle(coreOrb);
                }
            }

            // =========================
            // 末端：保留，但做得更精致，不要一大坨
            // =========================
            float tipPhase = time * 8.4f + Projectile.identity * 0.23f;
            Vector2 tipWaveOffset = nrm * ((float)Math.Sin(tipPhase) * halfWidth * 0.12f);

            GlowOrbParticle tipOrb = new GlowOrbParticle(
                endPos + tipWaveOffset,
                -dir * Main.rand.NextFloat(0.08f, 0.18f),
                false,
                5,
                0.82f + Main.rand.NextFloat(0.08f),
                Color.Lerp(coreColor, flashColor, 0.34f),
                true,
                false,
                true);
            //GeneralParticleHandler.SpawnParticle(tipOrb);

            if (Main.rand.NextBool(2 * intervalScale))
            {
                Vector2 tipSide = nrm * ((float)Math.Cos(tipPhase * 1.37f) * halfWidth * 0.16f);
                GlowSparkParticle tipSpark = new GlowSparkParticle(
                    endPos + tipSide,
                    tipSide.SafeNormalize(nrm) * 0.35f - dir * 0.12f,
                    false,
                    Main.rand.Next(7, 10),
                    Main.rand.NextFloat(0.013f, 0.018f),
                    Color.Lerp(edgeColor, flashColor, 0.30f),
                    new Vector2(Main.rand.NextFloat(1.10f, 1.45f), Main.rand.NextFloat(0.15f, 0.22f)),
                    true,
                    false,
                    1.03f);
                //GeneralParticleHandler.SpawnParticle(tipSpark);
            }
        }

        public float LaserWidthFunction(float completionRatio, Vector2 vertexPos) => Projectile.scale * Projectile.width + 180;

        public static Color LaserColorFunction(float completionRatio, Vector2 vertexPos)
        {
            // 轻呼吸 + 扭曲，色相在浅青(靠近白) 与 天蓝之间摆动
            float osc = (float)Math.Sin(Main.GlobalTimeWrappedHourly * -3.2f + completionRatio * 23f) * 0.5f + 0.5f;
            Color c1 = new Color(170, 235, 255);  // 浅青蓝（近白的科技光）
            Color c2 = new Color(70, 160, 255);   // 天蓝（偏冷，金属感）
            return Color.Lerp(c1, c2, osc * 0.75f);
        }


        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero)
                return false;

            Vector2 laserEnd = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * LaserLength;
            Vector2[] baseDrawPoints = new Vector2[8];
            for (int i = 0; i < baseDrawPoints.Length; i++)
                baseDrawPoints[i] = Vector2.Lerp(Projectile.Center, laserEnd, i / (float)(baseDrawPoints.Length - 1f));

            GameShaders.Misc["CalamityMod:ArtemisLaser"].UseColor(new Color(90, 200, 255));
            GameShaders.Misc["CalamityMod:ArtemisLaser"].UseImage1("Images/Extra_191"); // 横向的黑色背景纹理
            GameShaders.Misc["CalamityMod:ArtemisLaser"].UseImage2("Images/Misc/Perlin");

            PrimitiveRenderer.RenderTrail(baseDrawPoints, new(LaserWidthFunction, LaserColorFunction, shader: GameShaders.Misc["CalamityMod:ArtemisLaser"]), 64);
            return false;
        }

    }
}
