using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class BossSoulofFrightEffect : DefaultEffect
    {
        private const float MaxTravelDistance = 75f * 16f;
        private const int SplitCount = 16;
        private const int MaxDamagingHits = 3;

        public override int EffectID => 12;
        public override int AmmoType => ItemID.SoulofFright;

        // ===== 赤红色 =====
        public override Color ThemeColor => new Color(200, 40, 40);
        public override Color StartColor => new Color(255, 80, 80);
        public override Color EndColor => new Color(120, 10, 10);

        public override bool EnableDefaultSlowdown => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.localAI[0] = 0f;
            projectile.localAI[1] = 0f;
            projectile.ai[1] = 0f;
            projectile.ai[2] = 0f;
            projectile.timeLeft = System.Math.Max(45, (int)(MaxTravelDistance / System.Math.Max(projectile.velocity.Length(), 1f)) + 30);
            projectile.penetrate = -1;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 14;
            projectile.Resize(48, 48);
        }

        public override bool? CanDamage(Projectile projectile, Player owner) => projectile.localAI[1] < MaxDamagingHits ? null : false;

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.ai[1] = 0f;
            projectile.ai[2] = 0f;
            projectile.Resize(48, 48);
            projectile.localAI[0] += projectile.velocity.Length();

            if (projectile.owner != Main.myPlayer || projectile.localAI[0] < MaxTravelDistance)
                return;

            SpawnEvenSplit(projectile);
            projectile.Kill();
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 1.54f;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            projectile.localAI[1]++;
        }

        private static void SpawnEvenSplit(Projectile projectile)
        {
            float baseAngle = projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
            float offset = Main.rand.NextFloat(MathHelper.TwoPi / SplitCount);
            int splitDamage = System.Math.Max(1, (int)(projectile.damage * 0.77f));

            for (int i = 0; i < SplitCount; i++)
            {
                float angle = baseAngle + offset + MathHelper.TwoPi * i / SplitCount;
                Vector2 direction = angle.ToRotationVector2();
                float speed = 7.5f + (i % 2) * 1.2f;

                int soulIndex = Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + direction * 18f,
                    direction * speed,
                    ModContent.ProjectileType<NewSHPS>(),
                    splitDamage,
                    projectile.knockBack,
                    projectile.owner,
                    3);

                if (Main.projectile.IndexInRange(soulIndex))
                    Main.projectile[soulIndex].timeLeft = 110;
            }
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // ===== 光粒子核心：中心炸亮，制造“灵魂爆裂”感 =====
            for (int i = 0; i < 16; i++)
            {
                Vector2 dir = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 6f);

                SquishyLightParticle light = new(
                    projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    dir,
                    Main.rand.NextFloat(0.45f, 0.95f),
                    Color.Lerp(ThemeColor, StartColor, Main.rand.NextFloat(0.2f, 0.6f)),
                    Main.rand.Next(18, 30),
                    opacity: 1f,
                    squishStrenght: Main.rand.NextFloat(0.8f, 1.35f),
                    maxSquish: Main.rand.NextFloat(2.2f, 3.8f),
                    hueShift: 0f
                );

                GeneralParticleHandler.SpawnParticle(light);
            }

            // ===== 外层光粒子：向外拉出一圈凌厉感 =====
            for (int i = 0; i < 10; i++)
            {
                Vector2 dir = Main.rand.NextVector2Unit();
                Vector2 spawnPos = projectile.Center + dir * Main.rand.NextFloat(12f, 26f);
                Vector2 velocity = dir * Main.rand.NextFloat(3.5f, 7f);

                SquishyLightParticle light = new(
                    spawnPos,
                    velocity,
                    Main.rand.NextFloat(0.35f, 0.7f),
                    Color.Lerp(EndColor, ThemeColor, Main.rand.NextFloat(0.4f, 0.85f)),
                    Main.rand.Next(16, 24),
                    opacity: 1f,
                    squishStrenght: Main.rand.NextFloat(0.7f, 1.2f),
                    maxSquish: Main.rand.NextFloat(2f, 3.2f),
                    hueShift: 0f
                );

                GeneralParticleHandler.SpawnParticle(light);
            }

            // ===== 保留一个冲击波 =====
            Particle expandingPulse = new DirectionalPulseRing(
                projectile.Center,
                Vector2.Zero,
                ThemeColor,
                new Vector2(1.2f, 1.2f),
                0f,
                0.5f,
                6.0f,
                20
            );

            GeneralParticleHandler.SpawnParticle(expandingPulse);
        }



















    }
}
