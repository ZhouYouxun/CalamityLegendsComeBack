using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.TheNightmareFuel
{
    internal class NightmareFuel_ARC : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int timer;
        private int lockedTargetIndex = -1;
        private bool hasSplit;
        private Color arcColor = new(88, 24, 160);

        private bool IsSplit => Projectile.ai[1] == 1f;
        private int CurveDirection => IsSplit ? Math.Sign(Projectile.ai[2]) : 0;

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.extraUpdates = 12;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            arcColor = CurveDirection < 0
                ? new Color(72, 16, 132)
                : CurveDirection > 0
                    ? new Color(126, 36, 205)
                    : new Color(92, 20, 162);

            lockedTargetIndex = FindInitialTarget(1800f);
        }

        public override void AI()
        {
            timer++;

            if (!TargetIsValid(lockedTargetIndex) || timer % 12 == 0)
                lockedTargetIndex = FindInitialTarget(1800f);

            Vector2 safeVel = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            if (TargetIsValid(lockedTargetIndex))
            {
                NPC target = Main.npc[lockedTargetIndex];
                Vector2 toTarget = (target.Center - Projectile.Center).SafeNormalize(safeVel);
                float curveAngle = 0.018f * CurveDirection;
                float distance = Vector2.Distance(Projectile.Center, target.Center);
                float curveFade = Utils.GetLerpValue(120f, 540f, distance, true);
                Vector2 finalDirection = Vector2.Lerp(safeVel, toTarget.RotatedBy(curveAngle * curveFade), 0.18f).SafeNormalize(safeVel);
                float speed = MathHelper.Clamp(Projectile.velocity.Length() * 1.006f, 10f, IsSplit ? 22f : 20f);
                Projectile.velocity = finalDirection * speed;
            }
            else
            {
                if (CurveDirection != 0)
                    Projectile.velocity = Projectile.velocity.RotatedBy(0.014f * CurveDirection);

                Projectile.velocity *= 1.002f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            SpawnArcEffects();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ShadowboltReflect") { Volume = 0.32f, Pitch = -0.24f, PitchVariance = 0.12f, MaxInstances = 5 }, target.Center);

            if (!IsSplit && !hasSplit && Projectile.owner == Main.myPlayer)
            {
                hasSplit = true;
                float[] angles = { -0.28f, 0f, 0.28f };

                for (int i = 0; i < angles.Length; i++)
                {
                    Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(angles[i]) * 15f;
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        target.Center + velocity.SafeNormalize(Vector2.UnitX) * 14f,
                        velocity,
                        ModContent.ProjectileType<NightmareFuel_ARC>(),
                        (int)(Projectile.damage * 0.72f),
                        Projectile.knockBack,
                        Projectile.owner,
                        Projectile.ai[0],
                        1f,
                        i - 1f);
                }

            }

            for (int i = 0; i < 9; i++)
            {
                SquishyLightParticle particle = new(
                    target.Center,
                    Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.8f) * Main.rand.NextFloat(3f, 10f),
                    Main.rand.NextFloat(0.7f, 1.1f),
                    Main.rand.NextBool() ? arcColor : new Color(38, 6, 86),
                    Main.rand.Next(14, 24)
                );

                GeneralParticleHandler.SpawnParticle(particle);
            }
        }

        private void SpawnArcEffects()
        {
            Lighting.AddLight(Projectile.Center, arcColor.ToVector3() * 0.44f);

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float waveTime = Main.GameUpdateCount * 0.22f + Projectile.whoAmI * 0.37f;

            int flameCount = IsSplit ? 1 : 2;
            for (int i = 0; i < flameCount; i++)
            {
                float side = i == 0 ? -1f : 1f;
                float wave = (float)System.Math.Sin(waveTime + i * 1.4f) * 7f;

                for (int j = 0; j < 2; j++)
                {
                    Vector2 spawnPos = Projectile.Center - forward * Main.rand.NextFloat(20f, 24f) + right * (wave * side + Main.rand.NextFloat(-1.5f, 1.5f));
                    Vector2 velocity = -forward * Main.rand.NextFloat(0.7f, 1.8f) + right * side * Main.rand.NextFloat(0.08f, 0.42f);

                    SquishyLightParticle particle = new(
                        spawnPos,
                        velocity,
                        Main.rand.NextFloat(0.62f, 1f),
                        Color.Lerp(arcColor, new Color(190, 112, 255), Main.rand.NextFloat(0.25f, 0.65f)),
                        Main.rand.Next(14, 22)
                    );

                    GeneralParticleHandler.SpawnParticle(particle);
                }
            }

            if (timer % 3 == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    Particle mist = new MediumMistParticle(
                        Projectile.Center - forward * Main.rand.NextFloat(10f, 30f) + right * Main.rand.NextFloat(-8f, 8f),
                        -forward * Main.rand.NextFloat(0.4f, 1.2f),
                        new Color(82, 24, 132),
                        Color.Transparent,
                        Main.rand.NextFloat(0.48f, 0.72f),
                        Main.rand.NextFloat(90f, 130f)
                    );

                    GeneralParticleHandler.SpawnParticle(mist);
                }
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch,
                    -forward * Main.rand.NextFloat(0.2f, 1.1f),
                    0,
                    Main.rand.NextBool() ? arcColor : new Color(52, 8, 94),
                    Main.rand.NextFloat(0.9f, 1.2f)
                );
                dust.noGravity = true;
            }
        }

        private int FindInitialTarget(float maxDistance)
        {
            int targetIndex = -1;
            float nearestScore = maxDistance;
            Vector2 mouse = Main.MouseWorld;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.lifeMax <= 5)
                    continue;

                int lineTargets = CountTargetsNearLine(npc.Center, 96f);
                float score = Vector2.Distance(npc.Center, Projectile.Center) * 0.38f +
                              Vector2.Distance(npc.Center, mouse) * 0.44f -
                              lineTargets * 280f;
                if (score > nearestScore)
                    continue;

                targetIndex = npc.whoAmI;
                nearestScore = score;
            }

            return targetIndex;
        }

        private int CountTargetsNearLine(Vector2 endpoint, float width)
        {
            int count = 0;
            Vector2 start = Projectile.Center;
            float lengthSquared = Vector2.DistanceSquared(start, endpoint);
            if (lengthSquared <= 1f)
                return 0;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.lifeMax <= 5)
                    continue;

                Vector2 toNPC = npc.Center - start;
                float progress = MathHelper.Clamp(Vector2.Dot(toNPC, endpoint - start) / lengthSquared, 0f, 1f);
                Vector2 closest = Vector2.Lerp(start, endpoint, progress);
                if (Vector2.DistanceSquared(npc.Center, closest) <= width * width)
                    count++;
            }

            return count;
        }

        private static bool TargetIsValid(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
                return false;

            NPC npc = Main.npc[targetIndex];
            return npc.active && !npc.friendly && !npc.dontTakeDamage && npc.lifeMax > 5;
        }
    }
}
