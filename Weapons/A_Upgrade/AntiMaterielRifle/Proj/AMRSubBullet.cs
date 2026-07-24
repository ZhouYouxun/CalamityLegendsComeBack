using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle.Proj
{
    internal sealed class AMRSubBullet : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/Ranged/AMRShot";

        private bool initialized;
        private int visualAge;

        // 深黑与黑黄主配色：以黑曜石墨黑做底，缀以高饱和暗金与燃金
        private static readonly Color DarkObsidian = new(25, 22, 18);
        private static readonly Color DarkGold = new(218, 165, 32);
        private static readonly Color VibrantGold = new(255, 195, 40);
        private static readonly Color DeepCharcoalGold = new(45, 38, 18);

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.light = 0.45f;
            Projectile.alpha = 255;
            Projectile.extraUpdates = 10;
            Projectile.scale = 1.18f;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = ProjAIStyleID.Arrow;
            AIType = ProjectileID.BulletHighVelocity;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.alpha > 0)
                Projectile.alpha -= (int)(Projectile.velocity.Length() * 0.9f);
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (!initialized)
            {
                initialized = true;
                SpawnLaunchSparks();
            }

            if (!CalamityUtils.FinalExtraUpdate(Projectile))
                return;

            visualAge++;
            if (Main.dedServ || visualAge > 20)
                return;

            Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitX);

            // Rubico 风格：沿着弹道反向发射深黑/暗金细微火花
            GeneralParticleHandler.SpawnParticle(new AltSparkParticle(
                Projectile.Center,
                backward * Main.rand.NextFloat(0.6f, 1.2f),
                false,
                14,
                Main.rand.NextFloat(0.65f, 0.95f),
                Main.rand.NextBool() ? DeepCharcoalGold * 0.4f : DarkGold * 0.25f));

            if (visualAge % 2 == 0)
            {
                // 弹道黑黄内辉核心
                Color orbColor = visualAge % 4 == 0 ? DarkObsidian : VibrantGold;
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    backward * 0.45f,
                    false,
                    10,
                    0.26f,
                    orbColor,
                    true,
                    false,
                    true));
            }
        }

        // 首帧发射时的枪口/召唤黑金火花爆裂 (类似 Rubico 首帧火花)
        private void SpawnLaunchSparks()
        {
            if (Main.dedServ)
                return;

            Vector2 launchDir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 6; i++)
            {
                float sparkScale = Main.rand.NextFloat(0.35f, 0.85f);
                Vector2 sparkVelocity = launchDir.RotatedByRandom(0.4f) * Main.rand.NextFloat(2f, 7f);
                Color sparkColor = Main.rand.NextBool() ? DarkObsidian : DarkGold;

                SparkParticle spark = new SparkParticle(
                    Projectile.Center,
                    sparkVelocity,
                    false,
                    8,
                    sparkScale,
                    sparkColor);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GemTopaz,
                    Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(24f)) * Main.rand.NextFloat(0.08f, 0.42f),
                    0,
                    new Color(255, 207, 91),
                    Main.rand.NextFloat(0.58f, 0.98f));
                dust.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Main.dedServ)
                return;

            // 彻底引入 Rubico Prime 核心受击线状火花 (LineParticle)
            // 在受击点沿弹道反向、左右两侧飞溅出黑黄/暗金的对称线状粒子
            Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 3; i++)
            {
                // 左侧飞溅
                LineParticle sparkLeft = new LineParticle(
                    Projectile.Center,
                    backward.RotatedBy(Main.rand.NextFloat(0.18f, 0.44f)) * Main.rand.NextFloat(1.2f, 3.5f),
                    false,
                    9,
                    0.95f,
                    Main.rand.NextBool() ? DarkObsidian : DarkGold);
                GeneralParticleHandler.SpawnParticle(sparkLeft);

                // 右侧飞溅
                LineParticle sparkRight = new LineParticle(
                    Projectile.Center,
                    backward.RotatedBy(Main.rand.NextFloat(-0.18f, -0.44f)) * Main.rand.NextFloat(1.2f, 3.5f),
                    false,
                    9,
                    0.95f,
                    Main.rand.NextBool() ? VibrantGold : DeepCharcoalGold);
                GeneralParticleHandler.SpawnParticle(sparkRight);
            }

            // 补充击中时散射的黑金 Spark Particle
            for (int i = 0; i < 5; i++)
            {
                int sparkLifetime = Main.rand.Next(14, 24);
                float sparkScale = Main.rand.NextFloat(0.7f, 1.1f);
                Color sparkColor = Color.Lerp(DarkGold, DarkObsidian, Main.rand.NextFloat());

                Vector2 sparkVel = backward.RotatedByRandom(0.75f) * Main.rand.NextFloat(6f, 14f);
                sparkVel.Y -= Main.rand.NextFloat(2f, 5f);

                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center,
                    sparkVel,
                    true,
                    sparkLifetime,
                    sparkScale,
                    sparkColor));
            }

            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GemTopaz,
                    Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(15f)) * Main.rand.NextFloat(0.08f, 0.65f),
                    0,
                    new Color(255, 215, 112),
                    Main.rand.NextFloat(0.6f, 1.05f));
                dust.noGravity = true;
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            if (Projectile.alpha < 140)
                return new Color(255, 255, 255, 100);

            return Color.Transparent;
        }

        public override bool PreDraw(ref Color lightColor) => Projectile.timeLeft < 600;
    }
}
