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
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class NewSHPS : ModProjectile, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
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
        private int homingTimer;

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
            if (Projectile.ai[2] == 2f)
                themeColor = new Color(96, 255, 156);
            if (Projectile.ai[2] == 3f)
                themeColor = presetIndex == 5
                    ? new Color(96, 255, 156)
                    : Main.rand.NextBool() ? new Color(255, 210, 64) : new Color(44, 28, 8);

            if (presetIndex == 4)
            {
                Color preset4Accent = Projectile.ai[2] == 2f ? new Color(118, 255, 196) : new Color(70, 215, 255);
                themeColor = Color.Lerp(themeColor, preset4Accent, 0.65f);
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

            if (Projectile.ai[2] == 3f)
            {
                Projectile.timeLeft = Main.rand.Next(54, 84);
                Projectile.extraUpdates = 1;
                orbitOffset = Main.rand.NextFloat(MathHelper.TwoPi);
                orbitAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                ellipseRotation = Main.rand.NextFloat(MathHelper.TwoPi);
                darksunOrbitSpeed = Main.rand.NextFloat(0.22f, 0.39f) * (Main.rand.NextBool() ? 1f : -1f);
                darksunOrbitRadiusA = Main.rand.NextFloat(88f, 154f);
                darksunOrbitRadiusB = Main.rand.NextFloat(28f, 78f);
                darksunOrbitTwist = Main.rand.NextFloat(0.045f, 0.095f) * (Main.rand.NextBool() ? 1f : -1f);
                darksunOrbitSeed = Main.rand.NextFloat(MathHelper.TwoPi);
            }
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
        private float darksunOrbitSpeed;
        private float darksunOrbitRadiusA;
        private float darksunOrbitRadiusB;
        private float darksunOrbitTwist;
        private float darksunOrbitSeed;

        private void AI_Preset0()
        {
            if (Projectile.ai[2] == 3f)
            {
                AI_DarksunOrbit();
                return;
            }

            if (Projectile.ai[2] == 4f)
            {
                hasDetached = true;
            }

            // ===== 检查主弹幕是否存活 =====
            bool mainProjActive = false;
            Projectile mainProj = null;
            if (Main.projectile.IndexInRange(boundMainProjectileID))
            {
                mainProj = Main.projectile[boundMainProjectileID];
                if (mainProj.active && mainProj.type == ModContent.ProjectileType<NewLegendSHPB>())
                {
                    mainProjActive = true;
                }
            }

            if (!mainProjActive || hasDetached)
            {
                // 主弹幕消失或被命令脱离：开始强力追踪
                hasDetached = true;

                // 锁敌范围为 50 格方块 (50 * 16 = 800 像素)
                NPC target = null;
                float closestDist = 800f;
                foreach (NPC n in Main.npc)
                {
                    if (n.CanBeChasedBy(Projectile))
                    {
                        float d = Vector2.Distance(n.Center, Projectile.Center);
                        if (d < closestDist)
                        {
                            closestDist = d;
                            target = n;
                        }
                    }
                }

                // 如果范围里没有敌人，直接自毁！
                if (target == null)
                {
                    Projectile.Kill();
                    return;
                }

                // 加速冲向目标
                Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
                float speed = MathHelper.Lerp(18f, 28f, Utils.GetLerpValue(420f, 36f, Projectile.Distance(target.Center), true));
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredDirection * speed, 0.28f);
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.timeLeft = Math.Max(Projectile.timeLeft, 2);
                return;
            }

            // ===== 正常绑定逻辑 =====
            if (mainProjActive && mainProj != null)
            {
                float syncedPulse = 0.5f + 0.5f * (float)Math.Sin(Main.GameUpdateCount * 0.15f + boundMainProjectileID * 0.23f);
                float orbitScale = MathHelper.SmoothStep(0.58f, 1.45f, syncedPulse);
                float a = 62f * orbitScale;
                float b = 30f * orbitScale;

                orbitAngle += 0.128f;
                ellipseRotation += 0.048f;

                Vector2 ellipse = new Vector2(
                    (float)Math.Cos(orbitAngle + orbitOffset) * a,
                    (float)Math.Sin(orbitAngle + orbitOffset) * b
                ).RotatedBy(ellipseRotation);

                Projectile.Center = mainProj.Center + ellipse;

                Vector2 futurePos = mainProj.Center + new Vector2(
                    (float)Math.Cos(orbitAngle + orbitOffset + 0.24f) * a,
                    (float)Math.Sin(orbitAngle + orbitOffset + 0.24f) * b
                ).RotatedBy(ellipseRotation);

                Projectile.velocity = futurePos - Projectile.Center;
                return;
            }
        }

        // =========================
        // ===== 预设1：黑暗之魂 =====
        // =========================
        private void AI_DarksunOrbit()
        {
            if (!Main.projectile.IndexInRange(boundMainProjectileID) || !Main.projectile[boundMainProjectileID].active)
            {
                Projectile.velocity *= 0.98f;
                if (Projectile.timeLeft > 18)
                    Projectile.timeLeft = 18;
                return;
            }

            Projectile boundProjectile = Main.projectile[boundMainProjectileID];
            float pulse = 1f + (float)Math.Sin(timer * 0.31f + darksunOrbitSeed) * 0.18f;
            float skew = (float)Math.Sin(timer * 0.19f + darksunOrbitSeed * 1.7f) * 20f;

            orbitAngle += darksunOrbitSpeed + (float)Math.Sin(timer * 0.13f + darksunOrbitSeed) * 0.035f;
            ellipseRotation += darksunOrbitTwist;

            Vector2 ellipse = new Vector2(
                (float)Math.Cos(orbitAngle + orbitOffset) * (darksunOrbitRadiusA * pulse + skew),
                (float)Math.Sin(orbitAngle + orbitOffset) * (darksunOrbitRadiusB / pulse)
            ).RotatedBy(ellipseRotation);

            Vector2 noise = new Vector2(
                (float)Math.Sin(timer * 0.53f + darksunOrbitSeed),
                (float)Math.Cos(timer * 0.41f + darksunOrbitSeed * 0.6f)
            ) * 13f;

            Vector2 previous = Projectile.Center;
            Projectile.Center = boundProjectile.Center + ellipse + noise;
            Vector2 nextEllipse = new Vector2(
                (float)Math.Cos(orbitAngle + darksunOrbitSpeed + orbitOffset) * darksunOrbitRadiusA,
                (float)Math.Sin(orbitAngle + darksunOrbitSpeed + orbitOffset) * darksunOrbitRadiusB
            ).RotatedBy(ellipseRotation + darksunOrbitTwist);
            Projectile.velocity = (boundProjectile.Center + nextEllipse - Projectile.Center) * 0.72f + (Projectile.Center - previous) * 0.6f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.scale = 0.86f + 0.2f * (0.5f + 0.5f * (float)Math.Sin(timer * 0.38f + darksunOrbitSeed));
        }

        private void AI_Preset1()
        {
            sinTimer++;

            if (!startedHoming)
            {
                Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 normal = forward.RotatedBy(MathHelper.Pi / 2f);

                float wave = (float)Math.Sin(sinTimer * 0.48f) * 11f;
                Projectile.Center += forward * 14f + normal * wave;

                if (sinTimer > 18)
                {
                    startedHoming = true;

                    float dist = 1100f;
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
                homingTimer++;

                float loosen = Utils.GetLerpValue(0f, 72f, homingTimer, true);
                float closeLoosen = Utils.GetLerpValue(210f, 42f, Projectile.Distance(target.Center), true);
                float turnSpeed = MathHelper.Lerp(0.045f, 0.28f, MathHelper.Max(loosen, closeLoosen));
                float targetSpeed = MathHelper.Lerp(18f, 27f, loosen);
                float acceleration = MathHelper.Lerp(0.22f, 0.52f, MathHelper.Max(loosen, closeLoosen));

                Vector2 currentDir = Projectile.velocity.SafeNormalize(desiredDir);
                Vector2 steeredDir = currentDir.ToRotation().AngleTowards(desiredDir.ToRotation(), turnSpeed).ToRotationVector2();
                float speed = MathHelper.Lerp(Projectile.velocity.Length(), targetSpeed, acceleration);
                Projectile.velocity = steeredDir * speed;
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
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.penetrate += 1;
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
                float dist = Projectile.ai[2] == 3f ? 1600f : 800f;
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

                bool sightBurstPreset = Projectile.ai[2] == 3f;
                float loosen = sightBurstPreset ? Utils.GetLerpValue(0f, 90f, timer, true) : 0f;
                float closeTargetBoost = sightBurstPreset ? Utils.GetLerpValue(180f, 34f, Projectile.Distance(target5.Center), true) : 0f;

                float targetSpeed = sightBurstPreset ? MathHelper.Lerp(24f, 30f, loosen) : 20f * 1.25f;
                float inertia = sightBurstPreset ? MathHelper.Lerp(11f, 2f, MathHelper.Max(loosen, closeTargetBoost)) : 17f;

                Projectile.velocity = (
                    Projectile.velocity * inertia +
                    desiredDir * targetSpeed
                ) / (inertia + 1f);

                float speed = Projectile.velocity.Length();
                float targetFinalSpeed = sightBurstPreset ? MathHelper.Lerp(20f, 28f, loosen) : 14f;
                float speedCorrection = sightBurstPreset ? MathHelper.Lerp(0.12f, 0.34f, MathHelper.Max(loosen, closeTargetBoost)) : 0.08f;
                speed = MathHelper.Lerp(speed, targetFinalSpeed, speedCorrection);
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
            // 暗影之魂小灵魂追踪命中死亡时爆炸音
            if (presetIndex == 1)
                SoundEngine.PlaySound(new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/反步兵地雷爆炸"), Projectile.Center);
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
