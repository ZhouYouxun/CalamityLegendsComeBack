using System;
using CalamityLegendsComeBack.Weapons.BrinyBaron;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillC_QuickDash
{
    public class BrinyBaron_SkillSlashDash_SlashDash : BaseSwordHoldoutProjectile
    {
        public override bool useMeleeSpeed => true;
        public override bool useMeleeSize => false;
        public override int swingWidth => 310;
        public override Item BaseItem => ModContent.GetModItem(ModContent.ItemType<NewLegendBrinyBaron>()).Item;
        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron";
        public override SoundStyle? UseSound => SoundID.Item71 with { Volume = 0.85f };
        public override int StartupTime { get; set; }
        public override int CooldownTime { get; set; }
        public override int swingTime { get; set; }
        public override bool AlternateSwings { get => base.AlternateSwings; set => base.AlternateSwings = value; }
        public override float lineCollisionLength => 235f;

        // =========================
        // 卢克雷西亚主挥砍时序
        // =========================
        private const int StandardStartupTime = 8;
        private const int StandardSwingTime = 10;
        private const int StandardCooldownTime = 12;

        // =========================
        // 这把技能自己的状态
        // ai[0]：第几次挥砍，0=第一刀，1=第二刀
        // =========================
        private int SwingIndex => (int)Projectile.ai[0];
        private int DashDirection => Projectile.ai[1] == -1f ? -1 : 1;

        private bool trailFXTriggered = false;
        private bool particlesSpawned = false;
        private bool hitTriggered = false;
        private bool dashStarted = false;
        private bool spawnedFollowup = false;

        private Vector2 dashVelocity = Vector2.Zero;

        public override void Defaults()
        {
            Projectile.extraUpdates = 3;
            Projectile.noEnchantmentVisuals = true;
            Projectile.Opacity = 0.2f;
            Projectile.width = Projectile.height = 100;
            Projectile.scale = 1.25f;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void Spawn()
        {
            Player player = Main.player[Projectile.owner];

            // 这里不能直接信 itemAnimationMax，一些技能型弹幕生成时它可能不稳定
            int baseUseTime = BaseItem.useTime;
            int animMax = player.itemAnimationMax > 0 ? player.itemAnimationMax : baseUseTime;

            StartupTime = (int)(StandardStartupTime * (float)animMax / baseUseTime);
            CooldownTime = (int)(StandardSwingTime * (float)animMax / baseUseTime);
            swingTime = (int)(StandardCooldownTime * (float)animMax / baseUseTime);

            OffsetDistance = 64;
            RotateInStartup = 0.8f;
            RotateInCooldown = 0f;

            Projectile.knockBack = 8f;

            trailFXTriggered = false;
            particlesSpawned = false;
            hitTriggered = false;
            dashStarted = false;
            spawnedFollowup = false;
            dashVelocity = Vector2.Zero;

            if (Projectile.ai[1] != -1f && Projectile.ai[1] != 1f)
                Projectile.ai[1] = player.direction == -1 ? -1f : 1f;

            Projectile.spriteDirection = DashDirection;
            Projectile.direction = DashDirection;
            player.direction = DashDirection;
        }

        public override void AdditionalAI()
        {
            Player player = Main.player[Projectile.owner];

            // 标记当前玩家正在冲刺（只影响自己，不影响多人）
            player.GetModPlayer<Dash_Trigger>().IsUsingSlashDash = true;
            Vector2 fireDirection = new Vector2(DashDirection, 0f);

            Projectile.spriteDirection = DashDirection;
            Projectile.direction = DashDirection;
            player.direction = DashDirection;

            //Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            // =========================
            // 前摇：几乎照搬 Lucrecia primary
            // =========================
            if (inStartup)
            {
                trailFXTriggered = false;
                particlesSpawned = false;
                hitTriggered = false;
                dashStarted = false;
                dashVelocity = Vector2.Zero;

                Projectile.Opacity += 0.02f;
                Projectile.scale = baseScale * MathHelper.Lerp(0.625f, 0.8f, StartupCompletion);

                // 海蓝版启动闪光和抹刀光
                if (StartupCompletion > 0.12f && StartupCompletion < 0.44f)
                {
                    if (timer % 8 == 0)
                    {
                        Vector2 pos = player.MountedCenter + Projectile.rotation.ToRotationVector2() * 110f;
                        Particle sparkle = new CritSpark(
                            pos,
                            new Vector2(7f, 0f).RotatedBy(Projectile.rotation),
                            Color.Lerp(Color.DeepSkyBlue, Color.Cyan, Main.rand.NextFloat()),
                            Color.White * 0.33f,
                            1.2f,
                            12,
                            0.3f,
                            1.2f
                        );
                        GeneralParticleHandler.SpawnParticle(sparkle);
                    }

                    Particle smear = new CircularSmearVFX(
                        player.MountedCenter,
                        Color.DeepSkyBlue * 0.35f,
                        Projectile.rotation,
                        Projectile.scale * 1.25f
                    );
                    GeneralParticleHandler.SpawnParticle(smear);
                }
            }

            // =========================
            // 后摇：第一刀末尾自动接第二刀
            // =========================
            else if (inCooldown)
            {
                Projectile.Opacity -= 0.1f;
                Projectile.scale = baseScale * MathHelper.Lerp(0.85f, 0.625f, CooldownCompletion);

                // 给玩家一点减速收刀感
                player.velocity *= 0.89f;

                // 第一刀快结束时，自动生成第二刀
                if (!spawnedFollowup && SwingIndex == 0 && CooldownCompletion >= 0.82f && Main.myPlayer == Projectile.owner)
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        player.MountedCenter,
                        fireDirection,
                        Type,
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner,
                        1f,
                        DashDirection
                    );

                    spawnedFollowup = true;
                }
            }

            // =========================
            // 挥砍：完全以 Lucrecia primary 为骨架
            // 但去掉原作射出的弹幕，改成大冲刺 + 海蓝特效
            // =========================
            else if (inSwing)
            {
                if (!trailFXTriggered)
                {
                    Vector2 shootDir = fireDirection * 10f;
                    int dir = -DashDirection;
                    float scale = lineCollisionLength / 500f;

                    Color swipeColor = SwingIndex == 0 ? Color.DeepSkyBlue * 0.9f : Color.Cyan * 0.85f;

                    Particle swipe = new CustomSpark(
                        player.MountedCenter - shootDir * 8f,
                        shootDir.RotatedBy(0.075f * (dir * (SwingIndex % 2 == 0 ? 1 : -1))) * 1.22f,
                        "CalamityMod/Particles/VerticalSmearLarge",
                        false,
                        (int)(14f / player.GetAttackSpeed(DamageClass.Melee)),
                        scale,
                        swipeColor,
                        new Vector2(1.1f, 1.3f),
                        true,
                        false,
                        0,
                        false,
                        false
                    );
                    GeneralParticleHandler.SpawnParticle(swipe);

                    // 声音也按极速挥砍去走，但换成更冷更湿的味道
                    SoundEngine.PlaySound(
                        SoundID.Item71 with
                        {
                            Volume = 0.9f,
                            Pitch = Main.rand.NextFloat(0.18f, 0.28f)
                        },
                        Projectile.Center
                    );

                    SoundEngine.PlaySound(
                        SoundID.Splash with
                        {
                            Volume = 0.55f,
                            Pitch = Main.rand.NextFloat(-0.05f, 0.05f)
                        },
                        Projectile.Center
                    );

                    trailFXTriggered = true;
                }

                // 完全照 Lucrecia 主挥砍的位移曲线和缩放曲线
                float t = MathHelper.Clamp(SwingCompletion, 0f, 1f);
                float easedMovement = MathF.Pow(t, 0.4f);
                float parabola = 1f - MathF.Pow(easedMovement - 0.5f, 2f) * 4f;
                OffsetDistance = (int)MathHelper.Lerp(64f * 1f, 64f * 1.435f, parabola);

                float upPhase = easedMovement <= 0.5f ? easedMovement * 0.5f : (1f - easedMovement) * 0.5f;
                float scaleEase = MathF.Pow(upPhase, 2.6f);
                Projectile.scale = baseScale * MathHelper.Lerp(0.9f, 1.6f, scaleEase);

                // =========================
                // 每次挥砍开始都给一次大冲刺，并且带加速度
                // =========================
                if (!dashStarted)
                {
                    dashStarted = true;
                    dashVelocity = fireDirection * 24f;
                }

                dashVelocity += fireDirection * 1.9f;
                float maxDashSpeed = 54f;
                if (dashVelocity.Length() > maxDashSpeed)
                    dashVelocity = dashVelocity.SafeNormalize(Vector2.UnitX) * maxDashSpeed;

                player.velocity = dashVelocity;

                // 冲刺期间的海蓝拖尾
                if (timer % 3 == 0)
                {
                    Vector2 sprayPos = player.Center - fireDirection * Main.rand.NextFloat(22f, 50f);
                    Vector2 sprayVel = (-fireDirection).RotatedByRandom(0.75f) * Main.rand.NextFloat(3f, 10f);

                    Dust water = Dust.NewDustPerfect(sprayPos, DustID.Water, sprayVel);
                    water.noGravity = true;
                    water.scale = Main.rand.NextFloat(1.15f, 1.7f);
                    water.color = Color.DeepSkyBlue;

                    Dust frost = Dust.NewDustPerfect(sprayPos, DustID.Frost, sprayVel * 0.7f);
                    frost.noGravity = true;
                    frost.scale = Main.rand.NextFloat(0.95f, 1.45f);
                    frost.color = Color.Cyan;
                }

                // 卢克雷西亚这里会周期性放粒子，我们也照抄节奏，但改成海洋版
                if (t > 0.05f && t < 0.5f && timer % 5 == 0)
                {
                    SparkParticle orb = new SparkParticle(
                        player.Center + fireDirection * 6f,
                        (fireDirection * 14f).RotatedByRandom(1f),
                        true,
                        16,
                        0.5f,
                        Color.Lerp(Color.DeepSkyBlue, Color.Cyan, Main.rand.NextFloat()) * 0.72f,
                        true
                    );
                    GeneralParticleHandler.SpawnParticle(orb);
                }

                if (!particlesSpawned)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 particleOrigin = Projectile.Center;
                        Vector2 particleSpeed = fireDirection.RotatedByRandom(MathHelper.ToRadians(46f)) * Main.rand.NextFloat(20f, 37f);

                        Particle sparks = new CritSpark(
                            particleOrigin,
                            particleSpeed,
                            Color.Lerp(Color.DeepSkyBlue, Color.Cyan, Main.rand.NextFloat()),
                            Color.White * 0.55f,
                            Main.rand.NextFloat(0.9f, 2f),
                            Main.rand.Next(38, 51),
                            0.1f,
                            1.5f,
                            hueShift: 0.01f
                        );
                        GeneralParticleHandler.SpawnParticle(sparks);
                    }

                    particlesSpawned = true;
                }

                Lighting.AddLight(Projectile.Center, 0.05f, 0.24f, 0.34f);
            }

            //base.AdditionalAI();
        }

        public override float SwingFunction()
        {
            Player player = Main.player[Projectile.owner];

            float swingDirection = Projectile.spriteDirection;

            // 第二刀方向反过来
            if (SwingIndex == 1)
                swingDirection *= -1f;

            float easedCompletion = MathF.Pow(SwingCompletion, 0.4f);

            float startAngle = -swingWidth / 2.15f;
            float endAngle = swingWidth / 2.15f;
            float trueAngle = MathHelper.Lerp(startAngle, endAngle, easedCompletion);

            return MathHelper.ToRadians(trueAngle * -swingDirection);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 180);

            // 卢克雷西亚 primary 是一次挥砍打一组切割反馈，这里也照这个思路来
            if (!hitTriggered)
            {
                hitTriggered = true;

                SoundEngine.PlaySound(
                    SoundID.Item105 with
                    {
                        Volume = 0.6f,
                        Pitch = 0.15f
                    },
                    Projectile.Center
                );

                int points = 2;
                float radians = MathHelper.TwoPi / points;
                Vector2 spinningPoint = Vector2.Normalize(new Vector2(-1f, -1f)).RotatedByRandom(100f);

                Color useColor = SwingIndex == 0 ? Color.DeepSkyBlue : Color.Cyan;

                for (int k = 0; k < points; k++)
                {
                    Vector2 velocity = spinningPoint.RotatedBy(radians * k).RotatedBy(-0.45f);

                    Particle spark = new GlowSparkParticle(
                        target.Center + velocity * 7.5f,
                        velocity * 0.5f,
                        false,
                        9,
                        0.05f,
                        useColor,
                        new Vector2(0.5f, 0.6f),
                        true,
                        false
                    );
                    GeneralParticleHandler.SpawnParticle(spark);
                }

                for (int i = 0; i < 10; i++)
                {
                    Vector2 splashVel = Main.rand.NextVector2Circular(6f, 6f);

                    Dust d = Dust.NewDustPerfect(target.Center, DustID.Water, splashVel);
                    d.noGravity = true;
                    d.scale = Main.rand.NextFloat(1.1f, 1.7f);
                    d.color = Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.White;
            return base.PreDraw(ref lightColor);
        }
    }
}
