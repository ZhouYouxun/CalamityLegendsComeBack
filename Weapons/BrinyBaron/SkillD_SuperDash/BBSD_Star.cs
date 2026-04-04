using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal class BBSD_Star : ModProjectile
    {
        public override string Texture => "Terraria/Images/Extra_89";

        private const int StartupFrames = 30;
        private const int LifetimeFrames = 300;
        private const float SearchRange = 2200f;
        private const float MinSpeed = 7.5f;
        private const float MaxSpeed = 18f;

        private int frameCounter;
        private int targetRefreshCooldown;
        private int cachedTargetIndex = -1;
        private bool impactBurstSpawned;
        private float phaseOffset;
        private float pulseOffset;
        private float swirlDirection;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 14;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = LifetimeFrames * 3 + 10;
            Projectile.extraUpdates = 2;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(IEntitySource source)
        {
            phaseOffset = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            pulseOffset = Main.rand.NextFloat(0f, MathHelper.TwoPi);
            swirlDirection = Main.rand.NextBool() ? 1f : -1f;

            if (Projectile.velocity.LengthSquared() < 0.01f)
                Projectile.velocity = -Vector2.UnitY * MinSpeed;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Projectile.scale = Main.rand.NextFloat(0.95f, 1.15f);
        }

        public override bool? CanDamage() => frameCounter >= StartupFrames ? null : false;

        public override void AI()
        {
            if (Projectile.numUpdates == 0)
            {
                frameCounter++;
                if (targetRefreshCooldown > 0)
                    targetRefreshCooldown--;

                if (frameCounter >= LifetimeFrames)
                {
                    Projectile.Kill();
                    return;
                }
            }

            UpdateMotion();
            SpawnFlightEffects();
            Lighting.AddLight(Projectile.Center, 0.05f, 0.22f, 0.34f);
        }

        private void UpdateMotion()
        {
            Vector2 currentDirection = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
            Vector2 right = currentDirection.RotatedBy(MathHelper.PiOver2);
            float speedProgress = Utils.GetLerpValue(0f, 90f, frameCounter, true);
            float desiredSpeed = MathHelper.Lerp(MinSpeed, MaxSpeed, speedProgress);
            float weaveOffset = (float)Math.Sin(frameCounter * 0.24f + phaseOffset) * MathHelper.Lerp(24f, 8f, speedProgress);

            NPC target = AcquireTarget();
            if (target != null)
            {
                Vector2 targetPoint = target.Center + target.velocity * 0.35f + right * weaveOffset;
                Vector2 desiredDirection = (targetPoint - Projectile.Center).SafeNormalize(currentDirection);
                float maxTurn = MathHelper.ToRadians(MathHelper.Lerp(0.9f, 2.9f, speedProgress));
                float newAngle = currentDirection.ToRotation().AngleTowards(desiredDirection.ToRotation(), maxTurn);
                currentDirection = newAngle.ToRotationVector2();
                desiredSpeed = MathHelper.Lerp(desiredSpeed, MaxSpeed + 2.5f, 0.18f);
            }
            else
            {
                currentDirection = currentDirection.RotatedBy((float)Math.Sin(frameCounter * 0.22f + phaseOffset) * 0.006f * swirlDirection);
            }

            Projectile.velocity = currentDirection * desiredSpeed;
            Projectile.rotation = currentDirection.ToRotation() + MathHelper.PiOver4;
        }

        private NPC AcquireTarget()
        {
            if (IsTargetValid(cachedTargetIndex))
                return Main.npc[cachedTargetIndex];

            if (targetRefreshCooldown > 0)
                return null;

            cachedTargetIndex = FindBestTargetIndex();
            targetRefreshCooldown = 6;

            return IsTargetValid(cachedTargetIndex) ? Main.npc[cachedTargetIndex] : null;
        }

        private bool IsTargetValid(int index)
        {
            if (index < 0 || index >= Main.maxNPCs)
                return false;

            NPC npc = Main.npc[index];
            return npc.active && npc.CanBeChasedBy() && Vector2.Distance(Projectile.Center, npc.Center) <= SearchRange;
        }

        private int FindBestTargetIndex()
        {
            int bestIndex = -1;
            float bestDistance = SearchRange;
            bool bestIsBoss = false;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance > SearchRange)
                    continue;

                if (bestIndex == -1 || (npc.boss && !bestIsBoss) || (npc.boss == bestIsBoss && distance < bestDistance))
                {
                    bestIndex = i;
                    bestDistance = distance;
                    bestIsBoss = npc.boss;
                }
            }

            return bestIndex;
        }

        private void SpawnFlightEffects()
        {
            if (Main.dedServ || Projectile.numUpdates != 0)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float orbitPhase = frameCounter * 0.34f + phaseOffset;
            float helixRadius = 8f;
            float pulse = 0.5f + 0.5f * (float)Math.Sin(frameCounter * 0.28f + pulseOffset);

            Vector2 firstHelix =
                right * ((float)Math.Sin(orbitPhase) * helixRadius) +
                forward * ((float)Math.Cos(orbitPhase) * 3f);
            Vector2 secondHelix =
                right * ((float)Math.Sin(orbitPhase + MathHelper.Pi) * helixRadius) +
                forward * ((float)Math.Cos(orbitPhase + MathHelper.Pi) * 3f);

            GlowOrbParticle firstOrb = new GlowOrbParticle(
                Projectile.Center + firstHelix,
                right * ((float)Math.Cos(orbitPhase) * 0.2f),
                false,
                9,
                0.68f + pulse * 0.22f,
                Color.Lerp(new Color(70, 185, 255), Color.White, 0.35f + 0.25f * pulse),
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(firstOrb);

            GlowOrbParticle secondOrb = new GlowOrbParticle(
                Projectile.Center + secondHelix,
                -right * ((float)Math.Cos(orbitPhase) * 0.2f),
                false,
                9,
                0.68f + (1f - pulse) * 0.22f,
                Color.Lerp(new Color(100, 220, 255), Color.White, 0.28f + 0.28f * (1f - pulse)),
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(secondOrb);

            if (frameCounter % 2 == 0)
            {
                Dust water = Dust.NewDustPerfect(
                    Projectile.Center - forward * 4f,
                    DustID.Water,
                    -forward * Main.rand.NextFloat(0.4f, 1.6f),
                    100,
                    new Color(90, 205, 255),
                    Main.rand.NextFloat(0.85f, 1.15f));
                water.noGravity = true;
                water.fadeIn = 1.08f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            impactBurstSpawned = true;
            target.AddBuff(BuffID.Frostburn, 180);
            SpawnImpactEffects(target.Center, 1f);
        }

        public override void OnKill(int timeLeft)
        {
            if (!impactBurstSpawned)
                SpawnImpactEffects(Projectile.Center, 0.7f);
        }

        private void SpawnImpactEffects(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 10; i++)
            {
                float angle = MathHelper.TwoPi * i / 10f;
                Vector2 direction = (forward * (float)Math.Cos(angle) + right * (float)Math.Sin(angle)).SafeNormalize(forward);

                Dust water = Dust.NewDustPerfect(
                    center,
                    DustID.Water,
                    direction * Main.rand.NextFloat(3.5f, 8f) * intensity,
                    100,
                    new Color(75, 180, 255),
                    Main.rand.NextFloat(0.95f, 1.35f) * intensity);
                water.noGravity = true;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(
                        center,
                        DustID.Frost,
                        direction * Main.rand.NextFloat(2.2f, 5.5f) * intensity,
                        100,
                        new Color(210, 248, 255),
                        Main.rand.NextFloat(0.8f, 1.15f) * intensity);
                    frost.noGravity = true;
                }
            }

            for (int arm = 0; arm < 2; arm++)
            {
                float sign = arm == 0 ? 1f : -1f;
                for (int i = 0; i < 8; i++)
                {
                    float t = i / 7f;
                    float theta = sign * t * MathHelper.TwoPi * 0.85f + forward.ToRotation();
                    float radius = MathHelper.Lerp(6f, 26f, t) * intensity;
                    Vector2 position = center + theta.ToRotationVector2() * radius;

                    GlowOrbParticle orb = new GlowOrbParticle(
                        position,
                        Vector2.Zero,
                        false,
                        8 + Main.rand.Next(4),
                        0.75f + (1f - t) * 0.35f * intensity,
                        Color.Lerp(new Color(70, 185, 255), Color.White, 0.25f + 0.45f * t),
                        true,
                        false,
                        true);
                    GeneralParticleHandler.SpawnParticle(orb);
                }
            }

            for (int i = 0; i < 3; i++)
            {
                DirectionalPulseRing pulse = new DirectionalPulseRing(
                    center,
                    Vector2.Zero,
                    Color.Lerp(new Color(70, 190, 255), Color.White, 0.22f + i * 0.2f),
                    new Vector2(0.55f + i * 0.08f, 1.25f + i * 0.2f),
                    forward.ToRotation(),
                    0.1f + i * 0.03f,
                    0.014f,
                    10 + i * 3);
                GeneralParticleHandler.SpawnParticle(pulse);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D ringTexture = TextureAssets.Extra[89].Value;
            Vector2 origin = ringTexture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float time = Main.GlobalTimeWrappedHourly * 8f + pulseOffset;
            float pulse = 1f + 0.1f * (float)Math.Sin(time);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float t = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Color trailColor = Color.Lerp(new Color(55, 150, 255, 0), new Color(175, 245, 255, 0), t) * (0.42f * t);

                Main.spriteBatch.Draw(
                    ringTexture,
                    trailPos,
                    null,
                    trailColor,
                    Projectile.rotation,
                    origin,
                    new Vector2(0.55f, 0.55f) * Projectile.scale * (0.7f + 0.45f * t),
                    SpriteEffects.None,
                    0f);
            }

            Color crossColor = new Color(85, 195, 255, 0) * 0.95f;
            Color diagonalColor = new Color(215, 250, 255, 0) * 0.75f;
            Color coreColor = new Color(130, 225, 255, 0) * 1.05f;

            Main.spriteBatch.Draw(
                ringTexture,
                drawPosition,
                null,
                crossColor,
                Projectile.rotation,
                origin,
                new Vector2(1.7f, 0.24f) * Projectile.scale * pulse,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.Draw(
                ringTexture,
                drawPosition,
                null,
                crossColor,
                Projectile.rotation + MathHelper.PiOver2,
                origin,
                new Vector2(1.45f, 0.22f) * Projectile.scale * pulse,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.Draw(
                ringTexture,
                drawPosition,
                null,
                diagonalColor,
                Projectile.rotation + MathHelper.PiOver4,
                origin,
                new Vector2(1.1f, 0.16f) * Projectile.scale * pulse,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.Draw(
                ringTexture,
                drawPosition,
                null,
                diagonalColor,
                Projectile.rotation - MathHelper.PiOver4,
                origin,
                new Vector2(1.1f, 0.16f) * Projectile.scale * pulse,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.Draw(
                ringTexture,
                drawPosition,
                null,
                coreColor,
                0f,
                origin,
                new Vector2(0.62f, 0.62f) * Projectile.scale * (0.95f + 0.08f * pulse),
                SpriteEffects.None,
                0f);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
