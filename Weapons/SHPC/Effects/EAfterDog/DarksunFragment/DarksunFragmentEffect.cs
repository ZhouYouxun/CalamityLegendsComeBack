using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityDarksunFragment = CalamityMod.Items.Materials.DarksunFragment;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.DarksunFragment
{
    internal class DarksunFragmentEffect : DefaultEffect
    {
        public const int DarksunEffectID = 42;
        private const float NormalSpeed = 24f;
        private const float HomingMaxSpeed = 30f;
        private const int HomingDelay = 8;
        private const float HomingInertia = 24f;
        private const float FreeFlightDamping = 0.996f;
        private const float WanderingTurnStrength = 0.006f;

        public override int EffectID => DarksunEffectID;
        public override int AmmoType => ModContent.ItemType<CalamityDarksunFragment>();

        public override Color ThemeColor => new(30, 22, 10);
        public override Color StartColor => new(255, 210, 72);
        public override Color EndColor => new(5, 4, 3);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override float GlowIntensityFactor => 0f;
        public override bool EnableDefaultSlowdown => false;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = 1;
            projectile.timeLeft = 100;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            SetDefaults(projectile);
            projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction) * NormalSpeed;
            projectile.localAI[0] = 0f;
            projectile.GetGlobalProjectile<DarksunFragmentOrbGlobalProjectile>().hitSomething = false;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.localAI[0]++;
            projectile.ai[1] = 0f;
            projectile.ai[2] = 0f;

            Projectile targetSun = FindNearestBlackSun(projectile);
            if (targetSun is null)
                FreeDrift(projectile, owner);
            else
                HomeTowardBlackSun(projectile, owner, targetSun);

            projectile.rotation += 0.38f * Math.Sign(projectile.velocity.X == 0f ? owner.direction : projectile.velocity.X);

            if (projectile.owner == Main.myPlayer && TryAbsorbNearbyBlackSun(projectile, targetSun))
                return;

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center - projectile.velocity.SafeNormalize(Vector2.UnitX) * 12f + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.GoldFlame,
                    -projectile.velocity.RotatedByRandom(0.28f) * Main.rand.NextFloat(0.08f, 0.22f),
                    0,
                    Main.rand.NextBool() ? new Color(255, 200, 55) : Color.Black,
                    Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }

            Lighting.AddLight(projectile.Center, new Vector3(1f, 0.68f, 0.12f) * 0.45f);
        }

        private static bool TryAbsorbNearbyBlackSun(Projectile projectile, Projectile preferredSun = null)
        {
            if (TryAbsorbBlackSun(projectile, preferredSun))
                return true;

            foreach (Projectile other in Main.ActiveProjectiles)
            {
                if (TryAbsorbBlackSun(projectile, other))
                    return true;
            }

            return false;
        }

        private static bool TryAbsorbBlackSun(Projectile projectile, Projectile sun)
        {
            if (sun is null || !sun.active || sun.type != ModContent.ProjectileType<DarksunFragmentBlackSun>() || sun.owner != projectile.owner)
                return false;

            float absorbRadius = DarksunFragmentBlackSun.GetRadiusForLevel((int)sun.ai[0]);
            if (Vector2.Distance(sun.Center, projectile.Center) > absorbRadius)
                return false;

            projectile.GetGlobalProjectile<DarksunFragmentOrbGlobalProjectile>().hitSomething = true;
            DarksunFragmentBlackSun.UpgradeOrExplode(sun);
            projectile.Kill();
            return true;
        }

        public override bool? CanHitNPC(Projectile projectile, Player owner, NPC target)
        {
            return FindNearestBlackSun(projectile) is null ? null : false;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            projectile.GetGlobalProjectile<DarksunFragmentOrbGlobalProjectile>().hitSomething = true;
            if (projectile.owner == Main.myPlayer)
                SpawnOrUpgradeBlackSun(projectile, owner);

            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                projectile.Center,
                forward * 0.6f,
                new Color(255, 190, 48),
                new Vector2(1f, 2.2f),
                forward.ToRotation(),
                0.12f,
                0.025f,
                16));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                new Color(255, 190, 48),
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(-0.3f, 0.3f),
                0.04f,
                0.2f,
                14));

            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = forward.RotatedByRandom(0.9f) * Main.rand.NextFloat(1.6f, 5.8f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    projectile.Center,
                    velocity,
                    "CalamityMod/Particles/ForwardSmear",
                    false,
                    Main.rand.Next(9, 16),
                    Main.rand.NextFloat(0.08f, 0.16f),
                    Main.rand.NextBool(3) ? new Color(18, 12, 3) : new Color(255, 198, 54),
                    new Vector2(0.32f, 1.2f)));
            }
        }

        public override void PostDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch)
        {
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D softRing = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_03").Value;
            Texture2D reticle = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_04").Value;
            Vector2 drawPos = projectile.Center - Main.screenPosition;
            float opacity = MathHelper.Clamp(projectile.timeLeft / 18f, 0f, 1f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPos, null, new Color(255, 190, 44, 0) * (0.34f * opacity), projectile.rotation, bloom.Size() * 0.5f, projectile.scale * 0.28f, SpriteEffects.None);
            for (int i = 0; i < 4; i++)
            {
                float rotation = Main.GlobalTimeWrappedHourly * (2.8f + i * 0.4f) + i * MathHelper.PiOver2;
                Color color = new Color(255, 205, 68) * (0.48f - i * 0.065f);
                color.A = 0;
                Main.EntitySpriteDraw(ring, drawPos, null, color, rotation, ring.Size() * 0.5f, projectile.scale * (0.2f + i * 0.035f), SpriteEffects.None);
            }
            Main.EntitySpriteDraw(softRing, drawPos, null, new Color(255, 150, 34, 0) * (0.28f * opacity), projectile.rotation * 0.7f, softRing.Size() * 0.5f, projectile.scale * 0.13f, SpriteEffects.None);
            Main.EntitySpriteDraw(reticle, drawPos, null, new Color(255, 218, 84, 0) * (0.18f * opacity), -projectile.rotation * 0.55f, reticle.Size() * 0.5f, projectile.scale * 0.11f, SpriteEffects.FlipHorizontally);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private static void SpawnOrUpgradeBlackSun(Projectile projectile, Player owner)
        {
            int sunType = ModContent.ProjectileType<DarksunFragmentBlackSun>();
            foreach (Projectile other in Main.ActiveProjectiles)
            {
                if (other.type != sunType || other.owner != projectile.owner)
                    continue;

                float otherRadius = DarksunFragmentBlackSun.GetRadiusForLevel((int)other.ai[0]);
                if (Vector2.Distance(other.Center, projectile.Center) > otherRadius)
                    continue;

                DarksunFragmentBlackSun.UpgradeOrExplode(other);
                return;
            }

            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                sunType,
                Math.Max(1, (int)(projectile.damage * 1f)),
                projectile.knockBack,
                owner.whoAmI,
                1f);
        }

        private static Projectile FindNearestBlackSun(Projectile projectile)
        {
            int sunType = ModContent.ProjectileType<DarksunFragmentBlackSun>();
            Projectile closestSun = null;
            float closestDistance = float.MaxValue;

            foreach (Projectile other in Main.ActiveProjectiles)
            {
                if (other.type != sunType || other.owner != projectile.owner)
                    continue;

                float distance = Vector2.Distance(projectile.Center, other.Center);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestSun = other;
            }

            return closestSun;
        }

        private static void HomeTowardBlackSun(Projectile projectile, Player owner, Projectile targetSun)
        {
            if (projectile.localAI[0] <= HomingDelay)
            {
                FreeDrift(projectile, owner, FreeFlightDamping);
                return;
            }

            Vector2 currentVelocity = projectile.velocity;
            float currentSpeed = currentVelocity.Length();
            if (currentSpeed < 0.1f)
                currentVelocity = (targetSun.Center - projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction) * 4f;

            Vector2 desiredDirection = (targetSun.Center - projectile.Center).SafeNormalize(currentVelocity.SafeNormalize(Vector2.UnitX * owner.direction));
            float warmup = Utils.GetLerpValue(HomingDelay, HomingDelay + 36f, projectile.localAI[0], true);
            float closePressure = Utils.GetLerpValue(420f, 70f, projectile.Distance(targetSun.Center), true);
            float pullStrength = MathHelper.Lerp(0.35f, 1f, MathHelper.Max(warmup, closePressure * 0.75f));
            float targetSpeed = MathHelper.Lerp(16f, HomingMaxSpeed, pullStrength);
            Vector2 desiredVelocity = desiredDirection * targetSpeed;

            projectile.velocity = (currentVelocity * HomingInertia + desiredVelocity) / (HomingInertia + 1f);

            float sideSway = (float)Math.Sin((projectile.localAI[0] + projectile.identity * 7f) * 0.075f) *
                MathHelper.Lerp(0.012f, 0.004f, pullStrength);
            projectile.velocity = projectile.velocity.RotatedBy(sideSway);

            if (projectile.velocity.Length() > HomingMaxSpeed)
                projectile.velocity = projectile.velocity.SafeNormalize(desiredDirection) * HomingMaxSpeed;
        }

        private static void FreeDrift(Projectile projectile, Player owner, float damping = 1f)
        {
            Vector2 fallback = Vector2.UnitX * owner.direction;
            float wander = (float)Math.Sin((projectile.localAI[0] + projectile.identity * 5f) * 0.08f) * WanderingTurnStrength;
            projectile.velocity = projectile.velocity.SafeNormalize(fallback) * NormalSpeed;
            projectile.velocity = projectile.velocity.RotatedBy(wander) * damping;
        }
    }

    internal class DarksunFragmentOrbGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool hitSomething;
    }
}
