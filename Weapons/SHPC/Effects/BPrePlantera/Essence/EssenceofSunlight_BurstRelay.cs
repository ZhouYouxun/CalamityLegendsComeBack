using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera.Essence
{
    internal sealed class EssenceofSunlight_BurstRelay : ModProjectile, ILocalizedModType
    {
        private const int ShotCount = 7;
        private const int FireInterval = 6;
        private const float TargetRange = 2200f;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float ShotsFired => ref Projectile.localAI[1];

        private Vector2 RelativeOffset => new(Projectile.ai[0], Projectile.ai[1]);

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.timeLeft = (ShotCount - 1) * FireInterval + 30;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Player owner = Main.player[Projectile.owner];
            Vector2 offset = RelativeOffset;
            if (offset.LengthSquared() < 16f)
                offset = Projectile.Center - owner.Center;

            Projectile.ai[0] = offset.X;
            Projectile.ai[1] = offset.Y;
            Projectile.Center = owner.Center + offset;

            Vector2 forward = Projectile.velocity.SafeNormalize(GetOwnerAimDirection(owner, Vector2.UnitX * owner.direction));
            Projectile.velocity = forward;
            Projectile.rotation = forward.ToRotation();
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f, Pitch = 0.14f, MaxInstances = 4 }, Projectile.Center);
            SpawnOpeningFlash(forward);
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center + RelativeOffset;
            Vector2 aimDirection = GetShotDirection(owner, Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction), out _);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity.SafeNormalize(aimDirection), aimDirection, 0.18f).SafeNormalize(aimDirection);
            Projectile.rotation = Projectile.velocity.ToRotation();

            int timer = (int)Timer;
            if (Projectile.owner == Main.myPlayer && ShotsFired < ShotCount && timer % FireInterval == 0)
            {
                FireSolarLaser(owner, (int)ShotsFired);
                ShotsFired++;
            }

            Lighting.AddLight(Projectile.Center, new Color(255, 222, 96).ToVector3() * 0.48f);
            SpawnRelayEffects();

            Timer++;
        }

        private void FireSolarLaser(Player owner, int shotIndex)
        {
            Vector2 baseDir = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            NPC target = FindTarget(TargetRange, baseDir);
            if (target is not null)
            {
                baseDir = (target.Center - Projectile.Center).SafeNormalize(baseDir);
            }

            Vector2 normal = baseDir.RotatedBy(MathHelper.PiOver2);
            Vector2 spawnPosition = Projectile.Center + baseDir * 18f + normal * (float)Math.Sin(shotIndex * 1.618034f) * 12f;
            int damage = Math.Max(1, (int)(Projectile.damage * 0.5f));
            float side = Main.rand.NextBool() ? -1f : 1f;

            // Aim exactly at target from spawnPosition, predicting slightly
            Vector2 laserDir = baseDir;
            if (target is not null)
            {
                Vector2 targetPos = target.Center + target.velocity * 3f;
                laserDir = (targetPos - spawnPosition).SafeNormalize(baseDir);
            }

            int beamIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                laserDir * Main.rand.NextFloat(35f, 39f),
                ModContent.ProjectileType<EssenceofSunlight_Lighting>(),
                damage,
                Projectile.knockBack,
                Projectile.owner,
                target?.whoAmI ?? -1,
                side);

            if (Main.projectile.IndexInRange(beamIndex))
                Main.projectile[beamIndex].netUpdate = true;

            SoundEngine.PlaySound(SoundID.Item94 with
            {
                Volume = 0.084f,
                Pitch = 0.25f + shotIndex * 0.025f,
                PitchVariance = 0.06f,
                MaxInstances = 6
            }, spawnPosition);

            SpawnShotFlash(spawnPosition, laserDir, shotIndex);
        }

        private Vector2 GetShotDirection(Player owner, Vector2 fallback, out NPC target)
        {
            target = FindTarget(TargetRange, fallback);
            if (target is not null)
            {
                Vector2 predictedCenter = target.Center + target.velocity * 3f;
                return (predictedCenter - Projectile.Center).SafeNormalize(fallback);
            }

            return GetOwnerAimDirection(owner, fallback);
        }

        private NPC FindTarget(float range, Vector2 aimDirection)
        {
            NPC bestTarget = null;
            float bestScore = range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance > range)
                    continue;

                float score = distance;
                if (npc.boss)
                    score *= 0.82f;

                if (score >= bestScore)
                    continue;

                bestTarget = npc;
                bestScore = score;
            }

            return bestTarget;
        }

        private static Vector2 GetOwnerAimDirection(Player owner, Vector2 fallback)
        {
            Vector2 mouseWorld = owner.whoAmI == Main.myPlayer && !Main.dedServ ? Main.MouseWorld : owner.Calamity().mouseWorld;
            return (mouseWorld - owner.Center).SafeNormalize(fallback.SafeNormalize(Vector2.UnitX * owner.direction));
        }

        private void SpawnOpeningFlash(Vector2 forward)
        {
            if (Main.dedServ)
                return;

            Color sun = new(255, 210, 76);
            Color white = new(255, 248, 188);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                forward * 0.3f,
                sun,
                new Vector2(0.8f, 0.34f),
                forward.ToRotation(),
                0.07f,
                1.25f,
                18));

            for (int i = 0; i < 14; i++)
            {
                Vector2 velocity = forward.RotatedByRandom(0.58f) * Main.rand.NextFloat(3.2f, 8.8f);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    velocity,
                    false,
                    Main.rand.Next(7, 11),
                    Main.rand.NextFloat(0.1f, 0.17f),
                    Main.rand.NextBool(3) ? white : sun,
                    new Vector2(1.35f, 0.28f),
                    true,
                    false,
                    1f));
            }
        }

        private void SpawnRelayEffects()
        {
            if (Main.dedServ || Main.rand.NextBool(2))
                return;

            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = Projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.UnitY);
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + normal * Main.rand.NextFloat(-11f, 11f),
                Main.rand.NextBool() ? DustID.SolarFlare : DustID.Torch,
                back * Main.rand.NextFloat(0.4f, 1.5f) + normal * Main.rand.NextFloat(-0.4f, 0.4f),
                0,
                new Color(255, 220, 100),
                Main.rand.NextFloat(0.85f, 1.18f));

            dust.noGravity = true;
        }

        private void SpawnShotFlash(Vector2 position, Vector2 direction, int shotIndex)
        {
            if (Main.dedServ)
                return;

            Color mainColor = shotIndex == ShotCount - 1 ? new Color(255, 246, 170) : new Color(255, 214, 88);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                position,
                direction * 0.5f,
                mainColor,
                new Vector2(0.62f, 0.22f),
                direction.ToRotation(),
                0.045f,
                1.08f,
                12));

            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    position + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextBool(3) ? DustID.SolarFlare : DustID.Torch,
                    -direction.RotatedByRandom(0.38f) * Main.rand.NextFloat(0.8f, 2.7f),
                    0,
                    mainColor,
                    Main.rand.NextFloat(0.7f, 1.05f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
