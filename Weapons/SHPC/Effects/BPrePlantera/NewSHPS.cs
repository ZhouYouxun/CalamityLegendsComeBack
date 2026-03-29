using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class NewSHPS : ModProjectile, IPixelatedPrimitiveRenderer
    {
        private int presetIndex;
        private Color themeColor;
        private int timer;

        private static readonly Color[] PresetColors = new Color[]
        {
            new Color(255, 120, 200),  // 0 光明之魂（亮粉）
            new Color(90, 0, 120),     // 1 黑暗之魂（暗紫）
            new Color(120, 200, 255),  // 2 飞翔之魂（天蓝）
            new Color(255, 220, 80),
            new Color(200, 120, 255),
            new Color(255, 120, 200)
        };

        // ===== 可选：保留原版拾取灵魂烟雾逻辑入口 =====
        public bool IsPickupSoul
        {
            get => Projectile.ai[2] == 1f;
            set => Projectile.ai[2] = value ? 1f : 0f;
        }

        // ===== 光明之魂：绑定主弹幕 =====
        private int boundMainProjectileID = -1;
        private float orbitAngle;
        private float ellipseRotation;

        // ===== 黑暗之魂 =====
        private int sinTimer;
        private bool startedHoming;
        private NPC target;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 180;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // ai[0] = 预设编号
            presetIndex = (int)Projectile.ai[0];
            if (presetIndex < 0 || presetIndex >= 6)
                presetIndex = Main.rand.Next(6);

            // 生命周期固定主题色
            themeColor = PresetColors[presetIndex];

            // ai[1] = 绑定主弹幕ID
            boundMainProjectileID = (int)Projectile.ai[1];
        }

        public override void AI()
        {
            timer++;

            switch (presetIndex)
            {
                case 0:
                    AI_Preset0();
                    break;

                case 1:
                    AI_Preset1();
                    break;

                case 2:
                    AI_Preset2();
                    break;

                case 3:
                    AI_Preset3();
                    break;

                case 4:
                    AI_Preset4();
                    break;

                case 5:
                    AI_Preset5();
                    break;
            }

            // 原版飞行特效
            DoFlyEffects();
        }

        public override void OnKill(int timeLeft)
        {
            DoKillEffects();
        }

        public override bool PreDraw(ref Color lightColor) => false;

        // =========================
        // ===== 预设0：光明之魂 =====
        // =========================
        private void AI_Preset0()
        {
            if (Main.projectile.IndexInRange(boundMainProjectileID))
            {
                Projectile mainProj = Main.projectile[boundMainProjectileID];
                if (mainProj.active)
                {
                    float a = 60f;
                    float b = 30f;

                    orbitAngle += 0.05f;
                    ellipseRotation += 0.02f;

                    Vector2 ellipse = new Vector2(
                        (float)Math.Cos(orbitAngle) * a,
                        (float)Math.Sin(orbitAngle) * b
                    ).RotatedBy(ellipseRotation);

                    Projectile.Center = mainProj.Center + ellipse;

                    Vector2 futurePos = mainProj.Center + new Vector2(
                        (float)Math.Cos(orbitAngle + 0.1f) * a,
                        (float)Math.Sin(orbitAngle + 0.1f) * b
                    ).RotatedBy(ellipseRotation);

                    Projectile.velocity = futurePos - Projectile.Center;
                    return;
                }
            }

            // 失去绑定后正前方飞行并持续加速
            Projectile.velocity *= 1.01f;
        }

        // =========================
        // ===== 预设1：黑暗之魂 =====
        // =========================
        private void AI_Preset1()
        {
            sinTimer++;

            if (!startedHoming)
            {
                Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 normal = forward.RotatedBy(MathHelper.Pi / 2f);

                float wave = (float)Math.Sin(sinTimer * 0.25f) * 8f;
                Projectile.Center += forward * 10f + normal * wave;

                if (sinTimer > 30)
                {
                    startedHoming = true;

                    float dist = 800f;
                    int index = -1;
                    foreach (NPC n in Main.npc)
                    {
                        if (!n.CanBeChasedBy(Projectile))
                            continue;

                        float d = Vector2.Distance(n.Center, Projectile.Center);
                        if (d < dist)
                        {
                            dist = d;
                            index = n.whoAmI;
                        }
                    }

                    if (index != -1)
                        target = Main.npc[index];
                }

                return;
            }

            if (target != null && target.active)
            {
                Vector2 desiredDir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);

                Projectile.velocity = (
                    Projectile.velocity * 17f +
                    desiredDir * (20f * 1.25f)
                ) / 18f;

                float speed = Projectile.velocity.Length();
                speed = MathHelper.Lerp(speed, 14f, 0.08f);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
            }
            else
            {
                Projectile.velocity *= 1.01f;
            }
        }

        // =========================
        // ===== 预设2：飞翔之魂 =====
        // =========================
        private void AI_Preset2()
        {
            float gravity = 0.18f;
            float maxFallSpeed = 16f;

            Projectile.velocity.Y += gravity;

            if (Projectile.velocity.Y > maxFallSpeed)
                Projectile.velocity.Y = maxFallSpeed;

            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 1.020408f;
        }

        // =========================
        // ===== 预设3：恐惧之魂 =====
        // =========================
        private float randomAnglingStrength;
        private void AI_Preset3()
        {
            Projectile.extraUpdates = 0;

            // 每30帧改变一次转向强度
            if (timer % 30 == 1)
                randomAnglingStrength = Main.rand.NextFloat(-0.16f, 0.16f);

            // 持续微偏转
            Projectile.velocity = Projectile.velocity.RotatedBy(randomAnglingStrength);

            // 稍微限制速度（保持灵魂那种飘）
            if (Projectile.velocity.Length() > 2.75f)
                Projectile.velocity *= 0.96f;
        }



        // =========================
        // ===== 预设4：力量之魂 =====[这里留空，因为没有]
        // =========================
        private void AI_Preset4() { }




        // =========================
        // ===== 预设5：视觉之魂 =====
        // =========================
        private NPC target5;
        private bool hasTarget5;
        private void AI_Preset5()
        {
            // ===== 初次锁定 =====
            if (!hasTarget5)
            {
                float dist = 800f;
                int index = -1;

                foreach (NPC n in Main.npc)
                {
                    if (!n.CanBeChasedBy(Projectile))
                        continue;

                    float d = Vector2.Distance(n.Center, Projectile.Center);
                    if (d < dist)
                    {
                        dist = d;
                        index = n.whoAmI;
                    }
                }

                if (index != -1)
                {
                    target5 = Main.npc[index];
                    hasTarget5 = true;
                }
            }

            // ===== 追踪 =====
            if (target5 != null && target5.active)
            {
                Vector2 desiredDir = (target5.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);

                Projectile.velocity = (
                    Projectile.velocity * 17f +
                    desiredDir * (20f * 1.25f)
                ) / 18f;

                float speed = Projectile.velocity.Length();
                speed = MathHelper.Lerp(speed, 14f, 0.08f);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
            }
            else
            {
                Projectile.velocity *= 1.01f;
            }
        }
        // =========================
        // ===== 原版飞行特效 =====
        // =========================
        private void DoFlyEffects()
        {
            // 原版：每次少量彩色尾尘
            if (Main.rand.NextBool(12))
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 dustSpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(6f, 6f);
                    Vector2 dustVelocity = Projectile.velocity * -1.2f;
                    float dustScale = Main.rand.NextFloat(0.6f, 0.8f);

                    Dust dust = Dust.NewDustDirect(
                        dustSpawnPosition,
                        1,
                        1,
                        DustID.TintableDustLighted,
                        dustVelocity.X,
                        dustVelocity.Y,
                        0,
                        themeColor,
                        dustScale
                    );

                    dust.noGravity = true;
                    dust.noLight = false;
                    dust.noLightEmittence = false;
                }
            }

            // 原版：拾取灵魂额外烟雾
            if (Main.rand.NextBool(6) && IsPickupSoul)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 smokeVelocity = Main.rand.NextVector2Circular(1f, 1f) * 0.65f;
                    int smokeLifetime = Main.rand.Next(30, 45);
                    float smokeScale = Main.rand.NextFloat(0.15f, 0.3f);
                    float smokeOpacity = Main.rand.NextFloat(0.75f, 0.9f);

                    HeavySmokeParticle ghastlySmoke = new HeavySmokeParticle(
                        Projectile.Center,
                        smokeVelocity,
                        themeColor,
                        smokeLifetime,
                        smokeScale,
                        smokeOpacity,
                        0.02f,
                        true
                    );

                    GeneralParticleHandler.SpawnParticle(ghastlySmoke);
                }
            }
        }

        // =========================
        // ===== 原版死亡特效 =====
        // =========================
        private void DoKillEffects()
        {
            BezierCurve curve = new BezierCurve(Projectile.oldPos);

            // 沿拖尾曲线炸开
            for (int i = 0; i < 35; i++)
            {
                Vector2 dustSpawnPosition = curve.Evaluate(Main.rand.NextFloat());
                Vector2 dustVelocity = Main.rand.NextVector2Circular(1f, 1f) * 3f;
                float dustScale = Main.rand.NextFloat(1.2f, 1.8f);

                Dust dust = Dust.NewDustDirect(
                    dustSpawnPosition,
                    1,
                    1,
                    DustID.TintableDustLighted,
                    dustVelocity.X,
                    dustVelocity.Y,
                    0,
                    themeColor,
                    dustScale
                );

                dust.noGravity = true;
                dust.noLight = false;
                dust.noLightEmittence = false;
            }

            // 中心爆发
            for (int i = 0; i < 12; i++)
            {
                Vector2 dustVelocity = Main.rand.NextVector2Circular(1f, 1f) * 6f;
                float dustScale = Main.rand.NextFloat(1.8f, 2.4f);

                Dust dust = Dust.NewDustDirect(
                    Projectile.Center,
                    1,
                    1,
                    DustID.TintableDustLighted,
                    dustVelocity.X,
                    dustVelocity.Y,
                    0,
                    themeColor,
                    dustScale
                );

                dust.noGravity = true;
                dust.noLight = false;
                dust.noLightEmittence = false;
            }
        }

        // =========================
        // ===== 原版着色器拖尾 =====
        // =========================
        public float SoulWidthFunction(float completion, Vector2 _)
        {
            float width;
            float maxBodyWidth = Projectile.scale * 24f;
            float curveRatio = 0.15f;

            if (completion < curveRatio)
                width = MathF.Sin(completion / curveRatio * MathHelper.PiOver2) * maxBodyWidth + curveRatio;
            else
                width = Utils.Remap(completion, curveRatio, 1f, maxBodyWidth, 0f);

            return width;
        }

        public Color SoulColorFunction(float completion, Vector2 _)
        {
            Color tipColor = Color.Lerp(themeColor, Color.Transparent, Utils.GetLerpValue(0.8f, 1f, completion, true));
            return Color.Lerp(themeColor, tipColor, completion);
        }

        public float SoulCoreWidthFunction(float completion, Vector2 _)
        {
            float width;
            float maxBodyWidth = Projectile.scale * 14f;
            float curveRatio = 0.15f;

            if (completion < curveRatio)
                width = MathF.Sin(completion / curveRatio * MathHelper.PiOver2) * maxBodyWidth + curveRatio;
            else
                width = Utils.Remap(completion, curveRatio, 1f, maxBodyWidth, 0f);

            return width;
        }

        public Color SoulCoreColorFunction(float completion, Vector2 _)
        {
            Color tipColor = Color.Lerp(Color.White, Color.Transparent, Utils.GetLerpValue(0.8f, 1f, completion, true));
            return Color.Lerp(Color.White, tipColor, completion);
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            // 外层彩色灵魂拖尾
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak")
            );

            PrimitiveRenderer.RenderTrail(
                Projectile.oldPos,
                new PrimitiveSettings(
                    SoulWidthFunction,
                    SoulColorFunction,
                    (_, _) => Projectile.Size * 0.5f,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]
                ),
                Projectile.oldPos.Length * 2
            );

            // 内层白色核心拖尾
            Vector2[] soulCoreLength = Projectile.oldPos.Take(8).ToArray();

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak")
            );

            PrimitiveRenderer.RenderTrail(
                soulCoreLength,
                new PrimitiveSettings(
                    SoulCoreWidthFunction,
                    SoulCoreColorFunction,
                    (_, _) => Projectile.Size * 0.5f,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]
                ),
                soulCoreLength.Length * 2
            );
        }
    }
}