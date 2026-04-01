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
            new Color(255, 60, 60),    // 3 恐惧之魂（赤红）
            new Color(40, 80, 200),    // 4 力量之魂（深蓝）
            new Color(120, 255, 120)   // 5 视觉之魂（荧光绿）
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

        // ===== 力量之魂 =====
        private int preset4State;
        private int preset4StateTimer;
        private float preset4OrbitRadius;
        private float preset4OrbitAngle;
        private float preset4AngularVelocity;
        private float preset4OrbitTravel;
        private float preset4TargetOrbitTravel;
        private float preset4SpinDirection;
        private float preset4WaveSeed;

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

            if (presetIndex == 4)
            {
                themeColor = Color.Lerp(themeColor, new Color(70, 215, 255), 0.65f);
                Projectile.timeLeft = Main.rand.Next(84, 112);
                Projectile.extraUpdates = 1;
                preset4SpinDirection = Main.rand.NextBool() ? 1f : -1f;
                preset4OrbitAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                preset4WaveSeed = Main.rand.NextFloat(MathHelper.TwoPi);
                preset4TargetOrbitTravel = Main.rand.NextFloat(MathHelper.TwoPi * 2.25f, MathHelper.TwoPi * 3.4f);
            }

            // ai[1] = 绑定主弹幕ID
            boundMainProjectileID = (int)Projectile.ai[1];

            // 让他平分角度，占满三个轨道
            orbitOffset = (Projectile.whoAmI % 3) * MathHelper.TwoPi / 3f;
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
        private bool hasDetached; // 是否已经进入冲锋状态（锁死）
        private float orbitOffset; // 每个实例的初始相位偏移
        private void AI_Preset0()
        {
            // ===== 如果已经脱离过，就永远冲锋 =====
            if (hasDetached)
            {
                Projectile.velocity *= 1.01f;
                return;
            }

            // ===== 正常绑定逻辑 =====
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
                        (float)Math.Cos(orbitAngle + orbitOffset) * a,
                        (float)Math.Sin(orbitAngle + orbitOffset) * b
                    ).RotatedBy(ellipseRotation);

                    Projectile.Center = mainProj.Center + ellipse;

                    Vector2 futurePos = mainProj.Center + new Vector2(
                        (float)Math.Cos(orbitAngle + orbitOffset + 0.1f) * a,
                        (float)Math.Sin(orbitAngle + orbitOffset + 0.1f) * b
                    ).RotatedBy(ellipseRotation);

                    Projectile.velocity = futurePos - Projectile.Center;
                    return;
                }
            }

            // ===== 一旦进入这里 → 标记为永久脱离 =====
            hasDetached = true;

            // ===== 冲锋 =====
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
        private void AI_Preset4()
        {
            Projectile.extraUpdates = 1;
            Projectile.scale = 0.92f + 0.12f * (0.5f + 0.5f * (float)Math.Sin(timer * 0.22f + preset4WaveSeed));

            if (!Main.projectile.IndexInRange(boundMainProjectileID))
            {
                Projectile.velocity *= 0.98f;
                if (Projectile.timeLeft > 24)
                    Projectile.timeLeft = 24;
                return;
            }

            Projectile boundProjectile = Main.projectile[boundMainProjectileID];
            if (!boundProjectile.active)
            {
                Projectile.velocity *= 0.98f;
                if (Projectile.timeLeft > 24)
                    Projectile.timeLeft = 24;
                return;
            }

            Vector2 boundForward = boundProjectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 gunTip = boundProjectile.Center + boundForward * 56f;
            Vector2 toGunTip = gunTip - Projectile.Center;
            float distanceToGunTip = toGunTip.Length();
            Vector2 towardGunTip = toGunTip.SafeNormalize(boundForward);
            Vector2 sideways = towardGunTip.RotatedBy(MathHelper.PiOver2 * preset4SpinDirection);

            switch (preset4State)
            {
                case 0:
                    preset4StateTimer++;

                    float corkscrewA = (float)Math.Sin(timer * 0.37f + preset4WaveSeed);
                    float corkscrewB = (float)Math.Cos(timer * 0.18f + preset4WaveSeed * 1.6f);
                    float distanceFactor = Utils.GetLerpValue(220f, 30f, distanceToGunTip, true);
                    float wantedSpeed = MathHelper.Lerp(7f, 22f, distanceFactor);
                    float twistStrength = MathHelper.Lerp(13f, 3.5f, distanceFactor);

                    Vector2 desiredVelocity =
                        towardGunTip * wantedSpeed +
                        sideways * (corkscrewA * twistStrength + corkscrewB * twistStrength * 0.55f) +
                        boundForward * (2.5f * corkscrewB);

                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.14f);

                    if (distanceToGunTip < 42f || (preset4StateTimer > 18 && distanceToGunTip < 78f))
                    {
                        preset4State = 1;
                        preset4StateTimer = 0;
                        preset4OrbitRadius = MathHelper.Clamp(distanceToGunTip, 18f, 44f);
                        preset4OrbitAngle = (Projectile.Center - gunTip).ToRotation();
                        preset4AngularVelocity = 0.26f * preset4SpinDirection;
                        preset4OrbitTravel = 0f;
                    }
                    break;

                case 1:
                    preset4StateTimer++;
                    preset4OrbitRadius = MathHelper.Lerp(preset4OrbitRadius, 12f, 0.055f);
                    preset4AngularVelocity = MathHelper.Lerp(
                        preset4AngularVelocity,
                        (0.44f + 0.06f * (float)Math.Sin(timer * 0.16f + preset4WaveSeed)) * preset4SpinDirection,
                        0.08f);

                    preset4OrbitAngle += preset4AngularVelocity;
                    preset4OrbitTravel += Math.Abs(preset4AngularVelocity);

                    Vector2 orbitOffset = new Vector2(preset4OrbitRadius, 0f).RotatedBy(preset4OrbitAngle);
                    Vector2 orbitNormal = orbitOffset.SafeNormalize(Vector2.UnitX);
                    Vector2 orbitTangent = orbitNormal.RotatedBy(MathHelper.PiOver2 * preset4SpinDirection);
                    Vector2 desiredPosition =
                        gunTip +
                        orbitOffset +
                        orbitTangent * ((float)Math.Sin(timer * 0.33f + preset4WaveSeed) * 5f) +
                        boundForward * ((float)Math.Cos(timer * 0.27f + preset4WaveSeed) * 3f);

                    Vector2 orbitVelocity =
                        (desiredPosition - Projectile.Center) * 0.5f +
                        orbitTangent * (5.5f + preset4OrbitRadius * 0.08f);

                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, orbitVelocity, 0.18f);

                    if (preset4OrbitTravel >= preset4TargetOrbitTravel || Projectile.timeLeft < 22)
                    {
                        preset4State = 2;
                        preset4StateTimer = 0;
                    }
                    break;

                case 2:
                    preset4StateTimer++;

                    float collapseInterpolant = Utils.GetLerpValue(0f, 18f, preset4StateTimer, true);
                    float collapseTwist = MathHelper.Lerp(4.5f, 0f, collapseInterpolant);
                    Vector2 collapseVelocity =
                        towardGunTip * MathHelper.Lerp(10f, 24f, collapseInterpolant) +
                        towardGunTip.RotatedBy(MathHelper.PiOver2 * preset4SpinDirection) *
                        (float)Math.Sin(timer * 0.6f + preset4WaveSeed) *
                        collapseTwist;

                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, collapseVelocity, 0.22f);

                    if (distanceToGunTip < 10f)
                    {
                        Projectile.Kill();
                        return;
                    }
                    break;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
        }




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
