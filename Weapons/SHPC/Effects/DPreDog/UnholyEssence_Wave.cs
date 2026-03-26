using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    internal class UnholyEssence_Wave : ModProjectile
    {
        // 使用自身贴图，如果你后面有专门贴图就直接放同名文件即可

        // 自定义计时器，禁止使用 localAI
        private int lifeTimer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 52;
            Projectile.height = 52;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;
            Projectile.light = 0.45f;
            Projectile.scale = 0.9f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;

            Texture2D tex = ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;

            // ======== 邪异黑绿调色盘 ========
            Color[] unholyPalette = new Color[]
            {
                new Color(210, 255, 180), // 浅绿高光
                new Color(140, 255, 120), // 亮绿
                new Color(70, 220, 90),   // 深绿
                new Color(18, 28, 18),    // 黑绿核心
            };

            sb.End();
            sb.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            // ======== Primitive 宽度函数 ========
            float PrimitiveWidthFunction(float completionRatio, Vector2 vertexPos)
            {
                float width = Projectile.width * 2.9f;

                width *= MathHelper.SmoothStep(
                    0.45f,
                    1f,
                    Utils.GetLerpValue(0f, 0.22f, completionRatio, true)
                );

                width *= MathHelper.Lerp(1f, 0.68f, completionRatio);
                return width;
            }

            // ======== Primitive 颜色函数 ========
            Color PrimitiveTrailColor(float completionRatio, Vector2 vertexPos)
            {
                int colorIndex = (int)(completionRatio * (unholyPalette.Length - 1));
                colorIndex = Utils.Clamp(colorIndex, 0, unholyPalette.Length - 1);

                Color c = unholyPalette[colorIndex];

                c *= Projectile.Opacity * (1f - completionRatio);

                float speedBoost = Utils.GetLerpValue(4f, 16f, Projectile.velocity.Length(), true);
                c *= MathHelper.Lerp(0.6f, 1.15f, speedBoost);

                c.A = 0;
                return c;
            }

            // ======== 拖尾整体前推，让丝带更贴近前沿 ========
            Vector2 frontOffset = Projectile.velocity.SafeNormalize(Vector2.UnitX) * (Projectile.width * 1.15f);
            Vector2[] shiftedOldPos = new Vector2[Projectile.oldPos.Length];

            for (int i = 0; i < Projectile.oldPos.Length; i++)
                shiftedOldPos[i] = Projectile.oldPos[i] + frontOffset;

            // ======== 偏移函数：略微抬升，增强“半月冲切感” ========
            Vector2 PrimitiveOffsetFunction(float t, Vector2 vertexPos)
            {
                Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                return Projectile.Size * 0.5f + forward * Projectile.scale * 2f;
            }

            GameShaders.Misc["CalamityMod:SideStreakTrail"].UseImage1("Images/Misc/Perlin");

            PrimitiveRenderer.RenderTrail(
                shiftedOldPos,
                new PrimitiveSettings(
                    PrimitiveWidthFunction,
                    PrimitiveTrailColor,
                    PrimitiveOffsetFunction,
                    shader: GameShaders.Misc["CalamityMod:SideStreakTrail"]
                ),
                60
            );

            sb.End();
            sb.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            // ======== 主体绘制 ========
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(
                tex,
                drawPos,
                null,
                lightColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0f
            );

            // ======== 第二层常规虚化拖尾 ========
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float fade = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;

                Color c = Color.Lerp(new Color(20, 30, 20), new Color(120, 255, 120), fade) * 0.42f * fade;
                c.A = 0;

                float scale = Projectile.scale * (0.62f + fade * 0.38f);

                Main.spriteBatch.Draw(
                    tex,
                    pos,
                    null,
                    c,
                    Projectile.rotation,
                    origin,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }

            return false;
        }

        public override void AI()
        {
            Projectile.velocity *= 1.02f;

            lifeTimer++;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 backDir = -forward;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            // 轻微衰减，让它更像滑行的冲击波
            Projectile.velocity *= 0.992f;

            // 照明
            Lighting.AddLight(Projectile.Center, new Vector3(0.12f, 0.42f, 0.12f) * 1.2f);

            // 波头位置
            Vector2 headPos = Projectile.Center + forward * (Projectile.width * 0.45f);

            // 数学节奏参数
            float t = Main.GameUpdateCount * 0.14f;
            float sideSwing = (float)Math.Sin(t * 2.2f) * 6f;
            float pulse = (float)Math.Sin(t * 1.35f) * 0.5f + 0.5f;
            float squeeze = (float)Math.Cos(t * 2.7f) * 2.2f;

            // ================= 主体黑绿弧焰 =================
            if (Main.rand.NextBool(2))
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 sideOffset = right * (sideSwing + Main.rand.NextFloat(-2f, 2f)) * 0.28f;
                    Vector2 spawnPos = headPos + sideOffset;

                    Vector2 vel = backDir.RotatedBy(Main.rand.NextFloat(-0.26f, 0.26f)) * Main.rand.NextFloat(0.8f, 2.7f);

                    SquishyLightParticle flare = new SquishyLightParticle(
                        spawnPos,
                        vel,
                        Main.rand.NextFloat(0.42f, 0.7f + pulse * 0.15f),
                        Main.rand.NextBool(3) ? new Color(180, 255, 170) : new Color(80, 220, 100),
                        Main.rand.Next(14, 24),
                        1f,
                        Main.rand.NextFloat(1.15f, 1.8f)
                    );
                    GeneralParticleHandler.SpawnParticle(flare);
                }
            }

            // ================= 外沿邪雾 =================
            if (Main.rand.NextBool(3))
            {
                Vector2 ringOffset = right * Main.rand.NextFloat(-10f, 10f) + forward * Main.rand.NextFloat(-4f, 4f);

                WaterFlavoredParticle mist = new WaterFlavoredParticle(
                    Projectile.Center + ringOffset,
                    backDir * Main.rand.NextFloat(0.2f, 0.55f),
                    false,
                    Main.rand.Next(16, 24),
                    0.65f + Main.rand.NextFloat(0.2f),
                    Main.rand.NextBool(2) ? new Color(35, 60, 35) : new Color(90, 255, 120)
                );
                GeneralParticleHandler.SpawnParticle(mist);
            }

            // ================= 两侧交错火花，不照抄 Sunset 的喷流 =================
            if (lifeTimer % 2 == 0)
            {
                Vector2 sideA = headPos + right * (4.5f + squeeze);
                Vector2 sideB = headPos - right * (4.5f + squeeze);

                Dust dustA = Dust.NewDustPerfect(
                    sideA,
                    Main.rand.NextBool(2) ? DustID.GreenTorch : DustID.GemEmerald,
                    backDir * Main.rand.NextFloat(0.7f, 1.3f) + right * Main.rand.NextFloat(-0.35f, 0.35f),
                    0,
                    new Color(170, 255, 180),
                    1.1f + pulse * 0.15f
                );
                dustA.noGravity = true;

                Dust dustB = Dust.NewDustPerfect(
                    sideB,
                    Main.rand.NextBool(2) ? DustID.GreenTorch : DustID.GemEmerald,
                    backDir * Main.rand.NextFloat(0.7f, 1.3f) - right * Main.rand.NextFloat(-0.35f, 0.35f),
                    0,
                    new Color(90, 220, 100),
                    1.05f + pulse * 0.15f
                );
                dustB.noGravity = true;
            }

            // ================= 中轴高能点火花 =================
            if (Main.rand.NextBool(2))
            {
                PointParticle spark = new PointParticle(
                    headPos,
                    backDir.RotatedByRandom(0.22f) * Main.rand.NextFloat(1.2f, 2.8f),
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.75f, 1.1f),
                    Main.rand.NextBool(2) ? new Color(160, 255, 170) : new Color(60, 180, 80)
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // ================= 半月刀盘感：围绕主体做一小段弧线 Dust =================
            if (lifeTimer % 3 == 0)
            {
                float baseAngle = Projectile.velocity.ToRotation();

                for (int i = -1; i <= 1; i++)
                {
                    float angle = baseAngle + i * 0.35f;
                    Vector2 arcOffset = angle.ToRotationVector2() * (10f + pulse * 3f);

                    Dust arcDust = Dust.NewDustPerfect(
                        Projectile.Center + arcOffset,
                        DustID.GreenTorch,
                        backDir * Main.rand.NextFloat(0.3f, 0.75f),
                        0,
                        new Color(140, 255, 140),
                        1.0f
                    );
                    arcDust.noGravity = true;
                    arcDust.velocity *= 0.6f;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 pos = Projectile.Center;

            // ================= 爆心主爆 =================
            Particle explosion = new DetailedExplosion(
                pos,
                Vector2.Zero,
                new Color(80, 140, 80) * 0.9f,
                Vector2.One,
                Main.rand.NextFloat(-3f, 3f),
                0.18f,
                0.42f,
                10
            );
            GeneralParticleHandler.SpawnParticle(explosion);

            // ================= 黑绿脉冲环 =================
            Particle pulse = new CustomPulse(
                pos,
                Vector2.Zero,
                new Color(140, 255, 140),
                "CalamityMod/Particles/BloomCircle",
                Vector2.One * 1.15f,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.04f,
                0.24f,
                20,
                true
            );
            GeneralParticleHandler.SpawnParticle(pulse);

            // ================= 爆散火花 =================
            for (int i = 0; i < 12; i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 6f);

                GlowSparkParticle spark = new GlowSparkParticle(
                    pos,
                    vel,
                    false,
                    Main.rand.Next(8, 12),
                    Main.rand.NextFloat(0.08f, 0.14f),
                    Main.rand.NextBool(2) ? new Color(180, 255, 180) : new Color(70, 220, 90),
                    new Vector2(1.9f, 0.45f),
                    true,
                    false,
                    1
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // ================= 爆心高光 =================
            for (int i = 0; i < 6; i++)
            {
                SparkleParticle sparkle = new SparkleParticle(
                    pos + Main.rand.NextVector2Circular(14f, 14f),
                    Vector2.Zero,
                    new Color(220, 255, 220),
                    new Color(100, 255, 120),
                    1.4f + Main.rand.NextFloat(0.35f),
                    10 + Main.rand.Next(4),
                    Main.rand.NextFloat(-0.05f, 0.05f),
                    1.9f
                );
                GeneralParticleHandler.SpawnParticle(sparkle);
            }

            // ================= Dust 爆散 =================
            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    pos,
                    Main.rand.NextBool(2) ? DustID.GreenTorch : DustID.GemEmerald,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 5.8f),
                    0,
                    Main.rand.NextBool(2) ? new Color(170, 255, 170) : new Color(90, 220, 100),
                    Main.rand.NextFloat(1f, 1.55f)
                );
                dust.noGravity = true;
            }

            // ================= 原地爆炸弹幕 =================
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                pos,
                Vector2.Zero,
                ModContent.ProjectileType<FuckYou>(),
                (int)(Projectile.damage * 1.15f),
                Projectile.knockBack,
                Projectile.owner
            );

            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            // 死亡补一点收束余辉，避免空掉
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(2) ? DustID.GreenTorch : DustID.GemEmerald,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 3.4f),
                    0,
                    new Color(120, 255, 120),
                    Main.rand.NextFloat(0.9f, 1.3f)
                );
                dust.noGravity = true;
            }
        }









    }
}