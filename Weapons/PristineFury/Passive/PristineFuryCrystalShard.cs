using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.Passive
{
    internal sealed class PristineFuryCrystalShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Boss/ProvidenceCrystalShard";

        private const float Gravity = 0.24f;
        private const float ShardFallSpeed = 36f;
        private const float MaxFallSpeed = 45f;
        private const float HomingRange = 1080f;
        private const int HomingDelay = 10;
        private const float HomingInertia = 27f;
        private const float FreeFlightDamping = 0.996f;
        private const float NoTargetDamping = 0.992f;
        private const float WanderingTurnStrength = 0.006f;

        private ref float Hue => ref Projectile.ai[0];
        private ref float TargetIndexPlusOne => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private ref float HasBurst => ref Projectile.localAI[1];

        private static readonly Color ShardGold = new(255, 224, 72);
        private static readonly Color ShardWhite = new(255, 248, 198);
        private static readonly Color ShardOrange = new(255, 142, 36);
        private Color ShardColor
        {
            get
            {
                float pulse = (float)Math.Sin(Timer * 0.14f + Hue * MathHelper.TwoPi) * 0.5f + 0.5f;
                Color warm = Color.Lerp(ShardGold, ShardOrange, 0.22f + Hue * 0.24f);
                return Color.Lerp(warm, ShardWhite, 0.14f + pulse * 0.22f);
            }
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 150;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool? CanDamage() => Timer > 6f ? null : false;

        public override void AI()
        {
            if (Timer == 0f)
            {
                Projectile.scale = Main.rand.NextFloat(0.92f, 1.16f);
                SpawnArrivalSpark();
            }

            Timer++;
            Projectile.alpha = Math.Max(0, Projectile.alpha - 38);
            HomeTowardTarget();

            if (Projectile.velocity.LengthSquared() > 0.01f)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.Pi;

            if (!Main.dedServ)
            {
                if (Timer % 2 == 0)
                    SpawnTrailParticle();
                if (Main.rand.NextBool(5))
                {
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                        DustID.GoldFlame,
                        -Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.35f) * Main.rand.NextFloat(0.2f, 0.8f),
                        100, Main.rand.NextBool(3) ? ShardWhite : ShardGold, Main.rand.NextFloat(0.65f, 0.95f));
                    d.noGravity = true;
                }
            }

            Lighting.AddLight(Projectile.Center, ShardColor.ToVector3() * 0.55f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Burst();

        public override void OnKill(int timeLeft) => Burst();

        private void SpawnArrivalSpark()
        {
            if (Main.dedServ)
                return;

            Color color = ShardColor;
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, Vector2.Zero, color with { A = 0 },
                new Vector2(0.75f, 0.75f), Projectile.velocity.ToRotation(), 0.22f, 0.08f, 10));

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = Projectile.Center - forward * (18f + i * 24f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    pos,
                    forward * Main.rand.NextFloat(3f, 5.5f),
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.18f, 0.28f),
                    Color.Lerp(color, ShardOrange, i / 3f) with { A = 0 }));
            }
        }

        private void SpawnTrailParticle()
        {
            Color color = ShardColor;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float helix = (float)Math.Sin(Timer * 0.42f + Hue * MathHelper.TwoPi);
            float counterHelix = (float)Math.Cos(Timer * 0.34f + Projectile.identity * 0.13f);
            Vector2 pos =
                Projectile.Center
                - forward * Main.rand.NextFloat(4f, 12f)
                + right * (helix * Main.rand.NextFloat(3.5f, 8f) + Main.rand.NextFloat(-2f, 2f));
            Vector2 vel =
                -forward * Main.rand.NextFloat(0.65f, 1.55f)
                + right * (counterHelix * 0.28f + Main.rand.NextFloat(-0.18f, 0.18f));

            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                pos,
                vel * 0.45f,
                "CalamityMod/Particles/DualTrail",
                false,
                Main.rand.Next(8, 13),
                Main.rand.NextFloat(0.018f, 0.032f) * Projectile.scale,
                Color.Lerp(color, ShardWhite, Main.rand.NextFloat(0.08f, 0.38f)) with { A = 0 },
                new Vector2(0.8f, 2.8f + Math.Abs(helix) * 0.7f),
                true,
                true,
                shrinkSpeed: 0.72f,
                glowOpacity: 0.45f));

            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                pos, vel, false, Main.rand.Next(8, 14),
                Main.rand.NextFloat(0.18f, 0.32f) * Projectile.scale,
                color with { A = 0 }, false, false, false));

            if (Timer % 4 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    pos, vel * 1.4f, false, Main.rand.Next(6, 10),
                    Main.rand.NextFloat(0.1f, 0.18f),
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.1f, 0.4f)) with { A = 0 }));
            }

            if (Main.rand.NextBool(3))
            {
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center + right * helix * Main.rand.NextFloat(3f, 8f),
                    ModContent.DustType<SquashDust>(),
                    -forward.RotatedBy(counterHelix * 0.28f) * Main.rand.NextFloat(0.35f, 1.15f),
                    0,
                    Color.Lerp(ShardOrange, color, Main.rand.NextFloat(0.2f, 0.8f)),
                    Main.rand.NextFloat(0.42f, 0.72f) * Projectile.scale);
                ember.noGravity = true;
                ember.noLightEmittence = true;
            }
        }

        private void HomeTowardTarget()
        {
            if (Timer <= HomingDelay)
            {
                FreeDrift();
                return;
            }

            NPC target = FindHomingTarget();
            if (target == null)
            {
                FreeDrift(NoTargetDamping);
                return;
            }

            Vector2 currentVelocity = Projectile.velocity;
            float currentSpeed = currentVelocity.Length();
            if (currentSpeed < 0.1f)
                currentVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * ShardFallSpeed;

            Vector2 currentDirection = currentVelocity.SafeNormalize(Vector2.UnitY);
            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(currentDirection);
            float warmup = Utils.GetLerpValue(HomingDelay, HomingDelay + 32f, Timer, true);
            float closePressure = Utils.GetLerpValue(420f, 80f, Projectile.Distance(target.Center), true);
            float pullStrength = MathHelper.Lerp(0.35f, 1f, MathHelper.Max(warmup, closePressure * 0.8f));
            float targetSpeed = MathHelper.Lerp(ShardFallSpeed * 0.88f, MaxFallSpeed, pullStrength);

            Vector2 desiredVelocity = desiredDirection * targetSpeed;
            Projectile.velocity = (currentVelocity * HomingInertia + desiredVelocity) / (HomingInertia + 1f);

            float sideSway =
                (float)Math.Sin((Timer + Projectile.identity * 7f) * 0.075f + Hue * MathHelper.TwoPi)
                * MathHelper.Lerp(0.014f, 0.004f, pullStrength);
            Projectile.velocity = Projectile.velocity.RotatedBy(sideSway);

            LimitVelocity(desiredDirection);
        }

        private void FreeDrift(float damping = FreeFlightDamping)
        {
            float wander = (float)Math.Sin((Timer + Projectile.identity * 5f) * 0.08f + Hue * MathHelper.TwoPi) * WanderingTurnStrength;
            Projectile.velocity = Projectile.velocity.RotatedBy(wander) * damping;
            Projectile.velocity.Y = Math.Min(MaxFallSpeed, Projectile.velocity.Y + Gravity);
            LimitVelocity(Vector2.UnitY);
        }

        private void LimitVelocity(Vector2 fallbackDirection)
        {
            if (Projectile.velocity.LengthSquared() > MaxFallSpeed * MaxFallSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(fallbackDirection) * MaxFallSpeed;
        }

        private NPC FindHomingTarget()
        {
            int targetIndex = (int)TargetIndexPlusOne - 1;
            if (targetIndex >= 0 && targetIndex < Main.maxNPCs)
            {
                NPC lockedTarget = Main.npc[targetIndex];
                if (lockedTarget.active && lockedTarget.CanBeChasedBy(Projectile) && Projectile.Distance(lockedTarget.Center) <= HomingRange * 1.25f)
                    return lockedTarget;
            }

            NPC closestTarget = null;
            float closestDistance = HomingRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestTarget = npc;
            }

            return closestTarget;
        }

        private void Burst()
        {
            if (Main.dedServ || HasBurst == 1f)
                return;

            HasBurst = 1f;
            Color color = ShardColor;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 offset = Main.rand.NextVector2Circular(8f, 8f);
            Vector2 center = Projectile.Center + offset;
            float baseRotation = forward.ToRotation();

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center, Vector2.Zero, color with { A = 0 },
                new Vector2(1.1f, 1.1f), baseRotation, 0.1f, 0.3f, 10));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center, Vector2.Zero, ShardWhite with { A = 0 },
                "CalamityMod/Particles/BloomCircle",
                Vector2.One, 0f, 0.32f, 0.03f, 7));

            float crystalAngle = baseRotation + Hue * MathHelper.PiOver2 + Main.rand.NextFloat(-0.08f, 0.08f);
            const float goldenAngle = 2.3999631f;

            for (int i = 0; i < 7; i++)
            {
                float angle = crystalAngle + i * goldenAngle;
                float speed = MathHelper.Lerp(2.4f, 6.2f, (i + 1f) / 7f) * Main.rand.NextFloat(0.75f, 1.1f);
                Vector2 velocity = angle.ToRotationVector2().RotatedBy(Main.rand.NextFloat(-0.16f, 0.16f)) * speed;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    center,
                    velocity,
                    Texture,
                    true,
                    Main.rand.Next(28, 42),
                    Main.rand.NextFloat(0.14f, 0.24f) * Projectile.scale,
                    Color.Lerp(color, ShardWhite, Main.rand.NextFloat(0.05f, 0.35f)),
                    new Vector2(0.55f, 0.55f),
                    false,
                    glowCenter: true,
                    glowOpacity: 0.4f,
                    spin: Main.rand.NextFloat(-0.3f, 0.3f)));
            }

            for (int i = 0; i < 4; i++)
            {
                float angle = crystalAngle + i * goldenAngle + goldenAngle * 3.5f;
                float speed = MathHelper.Lerp(0.9f, 2.2f, (i + 1f) / 4f);
                Vector2 velocity = angle.ToRotationVector2().RotatedBy(Main.rand.NextFloat(-0.1f, 0.1f)) * speed;
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    center + velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(2f, 6f), velocity, false,
                    Main.rand.Next(6, 10), Main.rand.NextFloat(0.18f, 0.3f),
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.25f, 0.5f)) with { A = 0 },
                    false, false, false));
            }

            for (int i = 0; i < 4; i++)
            {
                int dustType = Main.rand.NextBool(3) ? ModContent.DustType<DiamondDust>() : ModContent.DustType<LightDust>();
                float angle = crystalAngle + i * goldenAngle + Main.rand.NextFloat(-0.12f, 0.12f);
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(3f, 3f),
                    dustType,
                    angle.ToRotationVector2() * Main.rand.NextFloat(1.2f, 3f),
                    Main.rand.Next(90, 180),
                    Main.rand.NextBool(3) ? ShardWhite : color,
                    Main.rand.NextFloat(0.32f, 0.56f));
                dust.noGravity = true;
                dust.noLight = true;
                dust.noLightEmittence = true;
                dust.fadeIn = Main.rand.NextFloat(2f, 5f);
            }

            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.38f, Pitch = 0.35f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = 0.22f, Pitch = 0.28f, MaxInstances = 8 }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;
            Texture2D square = ModContent.Request<Texture2D>("CalamityMod/Particles/SquareRotated").Value;

            float opacity = 1f - Projectile.alpha / 255f;
            Color shardColor = ShardColor;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None,
                Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float t = (float)i / Projectile.oldPos.Length;
                Color trailColor = Color.Lerp(shardColor with { A = 0 }, new Color(255, 174, 42) with { A = 0 }, t)
                    * ((1f - t) * 0.55f * opacity);
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float bloomScale = Projectile.scale * MathHelper.Lerp(0.08f, 0.22f, 1f - t);
                float starScale = Projectile.scale * MathHelper.Lerp(0.04f, 0.14f, 1f - t);

                Main.EntitySpriteDraw(bloom, trailPos, null, trailColor * 0.42f,
                    Projectile.oldRot[i], bloom.Size() * 0.5f, bloomScale, SpriteEffects.None);
                Main.EntitySpriteDraw(star, trailPos, null, trailColor * 0.28f,
                    Projectile.oldRot[i] + t * 0.6f, star.Size() * 0.5f,
                    new Vector2(starScale * 0.9f, starScale * 0.32f), SpriteEffects.None);
            }

            Main.EntitySpriteDraw(bloom, drawPos, null,
                shardColor with { A = 0 } * (0.26f * opacity),
                0f, bloom.Size() * 0.5f, Projectile.scale * 0.14f, SpriteEffects.None);

            for (int i = 0; i < 3; i++)
            {
                float phase = Timer * 0.05f + i * MathHelper.TwoPi / 3f + Hue * MathHelper.TwoPi;
                float twinkle = MathHelper.Clamp((float)Math.Sin(phase * 2.3f) * 1.6f, -1f, 1f) * 0.5f + 0.5f;
                if (twinkle < 0.05f)
                    continue;

                float orbitRadius = Projectile.scale * MathHelper.Lerp(9f, 14f, 0.5f + 0.5f * (float)Math.Sin(phase * 0.6f));
                Vector2 facetPos = drawPos + phase.ToRotationVector2() * orbitRadius;
                float facetScale = Projectile.scale * MathHelper.Lerp(0.05f, 0.1f, twinkle);

                Main.EntitySpriteDraw(star, facetPos, null,
                    Color.Lerp(shardColor, Color.White, 0.6f) with { A = 0 } * (twinkle * 0.5f * opacity),
                    phase * 1.7f, star.Size() * 0.5f,
                    new Vector2(facetScale, facetScale * 0.35f), SpriteEffects.None);
            }

            float spin = Timer * 0.045f + Hue * MathHelper.TwoPi;
            Main.EntitySpriteDraw(square, drawPos, null,
                Color.Lerp(shardColor, Color.White, 0.5f) with { A = 0 } * (0.22f * opacity),
                spin, square.Size() * 0.5f,
                new Vector2(Projectile.scale * 0.07f, Projectile.scale * 0.62f), SpriteEffects.None);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None,
                Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Color bodyColor = new Color(
                (int)(shardColor.R * opacity),
                (int)(shardColor.G * opacity),
                (int)(shardColor.B * opacity),
                (int)(200 * opacity));
            Main.EntitySpriteDraw(texture, drawPos, texture.Frame(), bodyColor, Projectile.rotation,
                texture.Frame().Center(), Projectile.scale, SpriteEffects.None);

            return false;
        }
    }
}
