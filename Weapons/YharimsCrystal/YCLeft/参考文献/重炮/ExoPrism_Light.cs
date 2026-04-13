using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.TheExoPrism
{
    public class ExoPrism_Light : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";

        public override string Texture => "Terraria/Images/Projectile_0"; // 透明占位

        public static Asset<Texture2D> TrailTex;

        private int timer;
        private int trailTimer;
        private int middleStreakTimer = 18;
        private bool initializedOldPos;

        // 双螺旋轨迹记录
        private readonly List<Vector2> oldPositionsLeft = new();
        private readonly List<Vector2> oldPositionsRight = new();

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 13;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;

            Projectile.friendly = true;
            Projectile.hostile = false;

            Projectile.penetrate = 6;
            Projectile.timeLeft = 300;

            Projectile.tileCollide = true;
            Projectile.extraUpdates = 15; // 保留原先高更新频率
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.velocity *= 1.2f;

            timer = 0;
            trailTimer = 0;
            middleStreakTimer = 18;
            initializedOldPos = false;

            oldPositionsLeft.Clear();
            oldPositionsRight.Clear();
        }

        public override void AI()
        {
            timer++;
            trailTimer++;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);

            Projectile.rotation = Projectile.velocity.ToRotation();

            // 轻微环境光，颜色偏亮白
            Lighting.AddLight(Projectile.Center, new Color(235, 245, 255).ToVector3() * 0.42f);

            // 首次把 oldPos 填满，避免刚生成时 Trail 抽风
            if (!initializedOldPos)
            {
                for (int i = 0; i < Projectile.oldPos.Length; i++)
                    Projectile.oldPos[i] = Projectile.position;

                for (int i = 0; i < 24; i++)
                {
                    oldPositionsLeft.Add(Projectile.Center);
                    oldPositionsRight.Add(Projectile.Center);
                }

                initializedOldPos = true;
            }




            // ================= 双螺旋主结构 =================
            // 不是死的正弦，做一点呼吸，让它在高速飞行里更像“活的几何轨道”
            float breathe = 0.88f + 0.12f * (float)Math.Sin(timer * 0.08f);
            float amplitude = 18f * breathe;
            float frequency = 0.19f;

            float sineWave = (float)Math.Cos(trailTimer * frequency);
            Vector2 offsetLeft = normal * amplitude * sineWave;
            Vector2 offsetRight = -normal * amplitude * sineWave;

            oldPositionsLeft.Add(Projectile.Center + offsetLeft);
            oldPositionsRight.Add(Projectile.Center + offsetRight);

            const int maxTrailLength = 150;
            if (oldPositionsLeft.Count > maxTrailLength)
                oldPositionsLeft.RemoveAt(0);
            if (oldPositionsRight.Count > maxTrailLength)
                oldPositionsRight.RemoveAt(0);

            // ================= 中轴主亮刺 =================
            // 这是 DNA 中心线的“骨架闪光”，频率不高，避免太刺眼
            middleStreakTimer--;
            if (middleStreakTimer <= 0)
            {
                middleStreakTimer = 3;

                Particle centerSparkA = new CustomSpark(
                    Projectile.Center,
                    (-forward).RotatedBy(MathHelper.ToRadians(170f)) * 1.1f,
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    18,
                    0.18f,
                    Color.White * 0.95f,
                    new Vector2(1f, 2.35f),
                    true,
                    true,
                    shrinkSpeed: 0.25f,
                    glowOpacity: 0.30f
                );
                GeneralParticleHandler.SpawnParticle(centerSparkA);

                Particle centerSparkB = new CustomSpark(
                    Projectile.Center + normal,
                    (-forward).RotatedBy(MathHelper.ToRadians(-170f)) * 1.1f,
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    18,
                    0.18f,
                    new Color(220, 240, 255) * 0.95f,
                    new Vector2(1f, 2.35f),
                    true,
                    true,
                    shrinkSpeed: 0.25f,
                    glowOpacity: 0.30f
                );
                GeneralParticleHandler.SpawnParticle(centerSparkB);
            }

            // ================= 数学化锯齿中轴 =================
            // 这里保留你想要的“折线释放”，但不是乱喷，而是严格沿法线做锯齿摆动
            //if (timer % 4 == 0)
            //{
            //    const int zigzagPeriod = 20;
            //    float zigzagPhase = (timer % zigzagPeriod) / (float)zigzagPeriod;
            //    float saw = zigzagPhase * 2f - 1f; // -1 到 1 的锯齿波

            //    Vector2 axisPos = Projectile.Center + normal * saw * 6f;
            //    Vector2 axisVel =
            //        (-forward * Main.rand.NextFloat(2.6f, 5.4f) +
            //         normal * saw * Main.rand.NextFloat(1.2f, 2.8f))
            //        .RotatedByRandom(0.10f);

            //    Particle axisSpark = new CustomSpark(
            //        axisPos,
            //        axisVel,
            //        "CalamityMod/Particles/ProvidenceMarkParticle",
            //        false,
            //        22,
            //        Main.rand.NextFloat(0.82f, 0.98f),
            //        Main.rand.NextBool(3) ? Color.White : new Color(225, 240, 255),
            //        new Vector2(1.25f, 0.5f),
            //        true,
            //        false,
            //        0f,
            //        false,
            //        false,
            //        Main.rand.NextFloat(0.08f, 0.14f)
            //    );
            //    GeneralParticleHandler.SpawnParticle(axisSpark);
            //}

            // ================= 双螺旋侧节点 =================
            // 这里是飞行主体的侧向发光节点，故意把亮度和尺寸压小，避免整个弹幕糊成灯泡
            if (timer % 3 == 0)
            {
                Vector2 purpleTrailOrigin = Projectile.Center + offsetLeft;
                Vector2 blueTrailOrigin = Projectile.Center + offsetRight;

                Particle leftNode = new SquishyLightParticle(
                    purpleTrailOrigin,
                    -Projectile.velocity.RotatedByRandom((MathHelper.Pi / 4f) * 0.5f) * Main.rand.NextFloat(0.55f, 1.25f),
                    Main.rand.NextFloat(0.16f, 0.24f), // 大小压低，避免中途过曝
                    Color.White * 0.80f,
                    Main.rand.Next(14, 24), // 寿命缩短，降低画面残留
                    opacity: 0.75f,
                    squishStrenght: 1f,
                    maxSquish: 2.2f,
                    hueShift: 0f
                );
                GeneralParticleHandler.SpawnParticle(leftNode);

                Particle rightNode = new SquishyLightParticle(
                    blueTrailOrigin,
                    -Projectile.velocity.RotatedByRandom((MathHelper.Pi / 4f) * 0.5f) * Main.rand.NextFloat(0.55f, 1.25f),
                    Main.rand.NextFloat(0.16f, 0.24f), // 同上，专门削弱体积和亮度
                    new Color(220, 240, 255) * 0.80f,
                    Main.rand.Next(14, 24),
                    opacity: 0.75f,
                    squishStrenght: 1f,
                    maxSquish: 2.2f,
                    hueShift: 0f
                );
                GeneralParticleHandler.SpawnParticle(rightNode);
            }

            // ================= 张扬的双螺旋喷射 =================
            // 这里不是简单向后拖尾，而是两侧镜像外甩，喷射角度夸张一点
            //if (timer % 3 == 0)
            //{
            //    Vector2 leftOrigin = Projectile.Center + offsetLeft * 0.75f;
            //    Vector2 rightOrigin = Projectile.Center + offsetRight * 0.75f;

            //    Vector2 leftVel =
            //        (-forward + normal * 0.95f).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(1.8f, 3.4f);
            //    Vector2 rightVel =
            //        (-forward - normal * 0.95f).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(1.8f, 3.4f);

            //    Particle glowA = new GlowSparkParticle(
            //        leftOrigin,
            //        leftVel.RotatedByRandom(0.10f),
            //        false,
            //        8,
            //        Main.rand.NextFloat(0.018f, 0.026f),
            //        Color.White,
            //        new Vector2(2.2f, 1f),
            //        true,
            //        false,
            //        1.20f
            //    );
            //    GeneralParticleHandler.SpawnParticle(glowA);

            //    Particle glowB = new GlowSparkParticle(
            //        rightOrigin,
            //        rightVel.RotatedByRandom(0.10f),
            //        false,
            //        8,
            //        Main.rand.NextFloat(0.018f, 0.026f),
            //        new Color(215, 235, 255),
            //        new Vector2(2.2f, 1f),
            //        true,
            //        false,
            //        1.20f
            //    );
            //    GeneralParticleHandler.SpawnParticle(glowB);
            //}

            // ================= 低亮度补充 Dust =================
            // 少量 Dust 只负责补空气感，不抢主视觉
            if (Main.rand.NextBool(3))
            {
                Vector2 dustPos = Projectile.Center + normal * Main.rand.NextFloatDirection() * Main.rand.NextFloat(1f, 8f);

                Dust dust = Dust.NewDustPerfect(
                    dustPos,
                    ModContent.DustType<LightDust>(),
                    (-forward * Main.rand.NextFloat(0.8f, 2.4f) + normal * Main.rand.NextFloat(-0.7f, 0.7f)).RotatedByRandom(0.12f)
                );
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.45f, 0.72f);
                dust.color = Main.rand.NextBool(2) ? Color.White : new Color(220, 240, 255);
                dust.noLightEmittence = true;
            }







        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // 保留原来的严格轴向反弹
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X;

            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y;

            return false;
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
        }

        public override Color? GetAlpha(Color lightColor) => new Color(235, 245, 255, 0);




        // ================= LucreciaBolt 同款主 Trail =================

        private float TrailWidth(float completionRatio, Vector2 vertexPos)
        {
            float width = Utils.GetLerpValue(1f, 0.4f, completionRatio, true) *
                          (float)Math.Sin(Math.Acos(1 - Utils.GetLerpValue(0f, 0.08f, completionRatio, true)));

            return width * (30f * 0.265f);
        }

        private Color TrailColor(float completionRatio, Vector2 vertexPos)
        {
            Color baseColor = Color.Lerp(Color.White, new Color(215, 235, 255), completionRatio);
            return baseColor * 0.50f;
        }
        private Color MiniTrailColor(float completionRatio, Vector2 vertexPos)
        {
            Color baseColor = Color.Lerp(Color.White, new Color(215, 235, 255), completionRatio);
            return baseColor * 1f;
        }
        private float MiniTrailWidth(float completionRatio, Vector2 vertexPos) => TrailWidth(completionRatio, vertexPos) * 5.5f;



        // ================= DNA 双螺旋 Trail =================

        private float HelixWidth(float completionRatio, Vector2 vertexPos)
        {
            return 10f;
        }
        private Color LeftHelixColor(float completionRatio, Vector2 vertexPos)
        {
            return Color.White * 0.95f;
        }

        private Color RightHelixColor(float completionRatio, Vector2 vertexPos)
        {
            return new Color(215, 235, 255) * 0.95f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!initializedOldPos)
                return false;

            // ================= 主干双层 Trail：完全沿 LucreciaBolt 的结构 =================
            Main.spriteBatch.EnterShaderRegion();

            if (TrailTex == null)
                TrailTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/BasicTrail");

            Color mainColor = Color.White;
            Color secondaryColor = new Color(215, 235, 255);

            GameShaders.Misc["CalamityMod:ExobladePierce"].SetShaderTexture(TrailTex);
            GameShaders.Misc["CalamityMod:ExobladePierce"].UseImage2("Images/Extra_189");
            GameShaders.Misc["CalamityMod:ExobladePierce"].UseColor(mainColor);
            GameShaders.Misc["CalamityMod:ExobladePierce"].UseSecondaryColor(secondaryColor);
            GameShaders.Misc["CalamityMod:ExobladePierce"].Apply();

            Vector2 offset = Projectile.Size * 0.5f;
            Vector2[] oldPosWithOffset = new Vector2[Projectile.oldPos.Length];
            for (int i = 0; i < Projectile.oldPos.Length; i++)
                oldPosWithOffset[i] = Projectile.oldPos[i] - offset;

            PrimitiveRenderer.RenderTrail(
                oldPosWithOffset,
                new PrimitiveSettings(TrailWidth, TrailColor, (_, _) => Projectile.Size, shader: GameShaders.Misc["CalamityMod:ExobladePierce"]),
                30
            );

            GameShaders.Misc["CalamityMod:ExobladePierce"].UseColor(mainColor);
            GameShaders.Misc["CalamityMod:ExobladePierce"].UseSecondaryColor(secondaryColor);

            PrimitiveRenderer.RenderTrail(
                oldPosWithOffset,
                new PrimitiveSettings(MiniTrailWidth, MiniTrailColor, (_, _) => Projectile.Size, shader: GameShaders.Misc["CalamityMod:ExobladePierce"]),
                30
            );

            Main.spriteBatch.ExitShaderRegion();

            // ================= 双螺旋 Trail：合并进同一文件 =================
            MiscShaderData helixShader = GameShaders.Misc["CalamityMod:TrailStreak"];
            helixShader.SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                oldPositionsLeft,
                new PrimitiveSettings(HelixWidth, LeftHelixColor, (_, _) => Projectile.Size, pixelate: false, shader: helixShader)
            );

            PrimitiveRenderer.RenderTrail(
                oldPositionsRight,
                new PrimitiveSettings(HelixWidth, RightHelixColor, (_, _) => Projectile.Size, pixelate: false, shader: helixShader)
            );

            return false;
        }
    }
}