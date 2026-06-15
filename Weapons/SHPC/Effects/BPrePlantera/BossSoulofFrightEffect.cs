using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Particles;
using CalamityLegendsComeBack.Weapons.SHPC;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class BossSoulofFrightEffect : DefaultEffect
    {
        private const float MaxTravelDistance = 18f * 16f;
        private const int SplitCount = 9;
        private const int SecondaryCount = 3;
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

        internal static int GetSplitDamage(Projectile projectile) => System.Math.Max(1, (int)(projectile.damage * 0.88f));

        internal static void SpawnEvenSplit(Projectile projectile, int splitCount)
        {
            float baseAngle = projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
            float offset = Main.rand.NextFloat(MathHelper.TwoPi / splitCount);
            int splitDamage = GetSplitDamage(projectile);

            for (int i = 0; i < splitCount; i++)
            {
                float angle = baseAngle + offset + MathHelper.TwoPi * i / splitCount;
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
                    Main.projectile[soulIndex].timeLeft = 105;
            }
        }

        private static void SpawnMainExplosion(Projectile projectile)
        {
            int explosionIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                System.Math.Max(1, (int)(GetSplitDamage(projectile) * 2.5f)),
                projectile.knockBack,
                projectile.owner);

            if (!Main.projectile.IndexInRange(explosionIndex))
                return;

            Projectile explosion = Main.projectile[explosionIndex];
            int explosionSize = new BalanceSHPC().GetDefaultOrbExplosionSize();
            explosion.Resize(explosionSize, explosionSize);
            explosion.Center = projectile.Center;
            explosion.DamageType = DamageClass.Magic;
            explosion.netUpdate = true;
        }

        private static void SpawnSecondaryProjectiles(Projectile projectile)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            List<float> usedAngles = new(SecondaryCount);
            int remainingSplits = SplitCount;

            for (int i = 0; i < SecondaryCount; i++)
            {
                float angle = PickSecondaryAngle(usedAngles);
                usedAngles.Add(angle);

                Vector2 direction = forward.RotatedBy(angle).SafeNormalize(forward);
                int slotsLeft = SecondaryCount - i;
                int splitCount = System.Math.Max(1, remainingSplits / slotsLeft);
                remainingSplits -= splitCount;
                float travelDistance = Main.rand.NextFloat(8f * 16f, 14f * 16f);
                float speed = 11.5f;

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + direction * 18f,
                    direction * speed,
                    ModContent.ProjectileType<BossSoulofFright_SecondarySoul>(),
                    (int)(projectile.damage * 1.13f),
                    projectile.knockBack,
                    projectile.owner,
                    splitCount,
                    travelDistance);
            }
        }

        private static float PickSecondaryAngle(List<float> usedAngles)
        {
            for (int attempt = 0; attempt < 80; attempt++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                bool valid = true;

                foreach (float usedAngle in usedAngles)
                {
                    if (System.Math.Abs(MathHelper.WrapAngle(angle - usedAngle)) < MathHelper.PiOver4)
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid)
                    return angle;
            }

            return usedAngles.Count * MathHelper.TwoPi / SecondaryCount;
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/CalamitasClone/CalClone_Explosion", 3), projectile.Center);

            if (projectile.owner == Main.myPlayer)
            {
                SpawnMainExplosion(projectile);
                SpawnSecondaryProjectiles(projectile);
            }

            // ===== 光粒子核心：中心炸亮，制造"灵魂爆裂"感 =====
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
                2.5f,
                20
            );

            GeneralParticleHandler.SpawnParticle(expandingPulse);
        }



















    }

    internal class BossSoulofFright_SecondarySoul : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float DistanceTraveled => ref Projectile.localAI[0];
        private int SplitCount => System.Math.Max(1, (int)Projectile.ai[0]);
        private float MaxDistance => Projectile.ai[1] <= 0f ? 10f * 16f : Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 38;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            DistanceTraveled += Projectile.velocity.Length();
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Color(200, 40, 40).ToVector3() * 0.35f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.RedTorch,
                    -Projectile.velocity * Main.rand.NextFloat(0.08f, 0.24f),
                    120,
                    new Color(220, 45, 45),
                    Main.rand.NextFloat(0.65f, 1.05f));
                dust.noGravity = true;
            }

            if (Projectile.owner == Main.myPlayer && DistanceTraveled >= MaxDistance)
                Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
                BossSoulofFrightEffect.SpawnEvenSplit(Projectile, SplitCount);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                new Color(200, 40, 40),
                Vector2.One,
                0f,
                0.22f,
                1.8f,
                14));
        }
    }
}
