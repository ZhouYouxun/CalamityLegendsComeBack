using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        private const int PortalReadyDelay = FireInterval * 2;
        private const float PortalInitialSpeed = 34.5f;
        private const float PortalSlowdown = 0.875f;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float ShotsFired => ref Projectile.localAI[1];

        private readonly Vector2[] portalOffsets = new Vector2[ShotCount];
        private readonly Vector2[] portalVelocities = new Vector2[ShotCount];
        private readonly float[] portalRotations = new float[ShotCount];
        private readonly float[] portalScales = new float[ShotCount];
        private bool portalsInitialized;

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
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Player owner = Main.player[Projectile.owner];
            Vector2 forward = Projectile.velocity.SafeNormalize(GetOwnerAimDirection(owner, Vector2.UnitX * owner.direction));
            Projectile.velocity = forward;
            Projectile.rotation = forward.ToRotation();
            InitializePortals(forward);
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

            if (!portalsInitialized)
                InitializePortals(Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction));

            UpdatePortals();

            Vector2 aimDirection = GetShotDirection(owner, Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction), out _);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity.SafeNormalize(aimDirection), aimDirection, 0.18f).SafeNormalize(aimDirection);
            Projectile.rotation = Projectile.velocity.ToRotation();

            int timer = (int)Timer;
            int fireTimer = timer - PortalReadyDelay;
            if (Projectile.owner == Main.myPlayer &&
                ShotsFired < ShotCount &&
                fireTimer >= 0 &&
                fireTimer % FireInterval == 0)
            {
                int shotIndex = (int)ShotsFired;
                FireSolarLaser(owner, shotIndex, GetPortalPosition(shotIndex));
                ShotsFired++;
            }

            Lighting.AddLight(Projectile.Center, new Color(255, 222, 96).ToVector3() * 0.28f);
            for (int i = 0; i < ShotCount; i++)
                Lighting.AddLight(GetPortalPosition(i), new Color(255, 190, 80).ToVector3() * 0.22f);

            SpawnPortalEffects();
            Timer++;
        }

        private void InitializePortals(Vector2 forward)
        {
            float baseAngle = forward.SafeNormalize(Vector2.UnitX).ToRotation();
            float goldenAngle = MathHelper.TwoPi * 0.381966f;

            for (int i = 0; i < ShotCount; i++)
            {
                float angle = baseAngle + goldenAngle * i + (i - ShotCount / 2f) * 0.065f + Main.rand.NextFloat(-0.36f, 0.36f);
                Vector2 outward = angle.ToRotationVector2();
                portalOffsets[i] = Vector2.Zero;
                portalVelocities[i] = outward * PortalInitialSpeed * Main.rand.NextFloat(0.62f, 1.38f);
                portalRotations[i] = angle + MathHelper.PiOver2 + Main.rand.NextFloat(-0.32f, 0.32f);
                portalScales[i] = Main.rand.NextFloat(0.62f, 0.92f);
            }

            portalsInitialized = true;
        }

        private void UpdatePortals()
        {
            for (int i = 0; i < ShotCount; i++)
            {
                portalOffsets[i] += portalVelocities[i];
                portalVelocities[i] *= PortalSlowdown;

                if (portalVelocities[i].LengthSquared() < 0.04f)
                    portalVelocities[i] = Vector2.Zero;

                float spinDirection = (i & 1) == 0 ? 1f : -1f;
                float slowFactor = Utils.GetLerpValue(PortalReadyDelay, 0f, Timer, true);
                portalRotations[i] += spinDirection * MathHelper.Lerp(0.035f, 0.16f, slowFactor);
            }
        }

        private Vector2 GetPortalPosition(int index)
        {
            index = Utils.Clamp(index, 0, ShotCount - 1);
            return Projectile.Center + portalOffsets[index];
        }

        private void FireSolarLaser(Player owner, int shotIndex, Vector2 spawnPosition)
        {
            Vector2 baseDir = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            NPC target = FindTarget(TargetRange, baseDir, spawnPosition);
            if (target is not null)
            {
                baseDir = (target.Center - spawnPosition).SafeNormalize(baseDir);
            }

            int damage = Math.Max(1, (int)(Projectile.damage * 0.5f));
            float side = Main.rand.NextBool() ? -1f : 1f;

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
            target = FindTarget(TargetRange, fallback, Projectile.Center);
            if (target is not null)
            {
                Vector2 predictedCenter = target.Center + target.velocity * 3f;
                return (predictedCenter - Projectile.Center).SafeNormalize(fallback);
            }

            return GetOwnerAimDirection(owner, fallback);
        }

        private NPC FindTarget(float range, Vector2 aimDirection, Vector2 origin)
        {
            NPC bestTarget = null;
            float bestScore = range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                float distance = Vector2.Distance(origin, npc.Center);
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

        private void SpawnPortalEffects()
        {
            if (Main.dedServ || Main.rand.NextBool(2))
                return;

            int portalIndex = ((int)Timer + Projectile.identity) % ShotCount;
            Vector2 portalPosition = GetPortalPosition(portalIndex);
            Vector2 outward = (portalPosition - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
            Vector2 normal = outward.RotatedBy(MathHelper.PiOver2);

            Dust dust = Dust.NewDustPerfect(
                portalPosition + normal * Main.rand.NextFloat(-8f, 8f),
                Main.rand.NextBool() ? DustID.SolarFlare : DustID.Torch,
                -outward * Main.rand.NextFloat(0.4f, 1.5f) + normal * Main.rand.NextFloat(-0.4f, 0.4f),
                0,
                new Color(255, 220, 100),
                Main.rand.NextFloat(0.65f, 0.95f));

            dust.noGravity = true;

            if (Timer < PortalReadyDelay && Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    portalPosition,
                    portalVelocities[portalIndex] * 0.08f,
                    new Color(255, 214, 88) * 0.55f,
                    new Vector2(0.42f, 0.16f),
                    portalRotations[portalIndex],
                    0.025f,
                    0.26f,
                    10));
            }
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

        public override bool PreDraw(ref Color lightColor)
        {
            if (!portalsInitialized)
                InitializePortals(Projectile.velocity.SafeNormalize(Vector2.UnitX));

            Texture2D portal = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/StreamGougePortal").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 portalOrigin = portal.Size() * 0.5f;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            float fadeIn = Utils.GetLerpValue(0f, PortalReadyDelay, Timer, true);
            float fadeOut = Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);

            for (int i = 0; i < ShotCount; i++)
            {
                float fireTime = PortalReadyDelay + i * FireInterval;
                float firedAge = Timer - fireTime;
                float fireFade = firedAge <= 0f ? 1f : MathHelper.Clamp(1f - firedAge / 12f, 0f, 1f);
                float opacity = fadeIn * fadeOut * fireFade;
                if (opacity <= 0f)
                    continue;

                Vector2 drawPosition = GetPortalPosition(i) - Main.screenPosition;
                float pulse = 0.92f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 11f + i);
                Color portalColor = Color.Lerp(new Color(255, 166, 48), new Color(255, 246, 170), i / (float)(ShotCount - 1)) * opacity;
                Color bloomColor = new Color(255, 210, 82, 0) * opacity * 0.45f;
                float scale = portalScales[i] * 0.34f * pulse;

                Main.EntitySpriteDraw(
                    bloom,
                    drawPosition,
                    null,
                    bloomColor,
                    0f,
                    bloomOrigin,
                    scale * 1.65f,
                    SpriteEffects.None);

                Main.EntitySpriteDraw(
                    portal,
                    drawPosition,
                    null,
                    portalColor,
                    portalRotations[i],
                    portalOrigin,
                    scale,
                    SpriteEffects.None);
            }

            return false;
        }
    }
}
