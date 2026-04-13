using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal class BBSD_Star : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
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
        private Color mainColor;
        private Color accentColor;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 18;
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
            mainColor = Color.Lerp(new Color(255, 229, 145), Color.White, Main.rand.NextFloat(0.1f, 0.38f));
            accentColor = Color.Lerp(new Color(105, 205, 255), new Color(255, 245, 185), Main.rand.NextFloat(0.28f, 0.72f));

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
            Lighting.AddLight(Projectile.Center, 0.11f, 0.24f, 0.33f);
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

            GeneralParticleHandler.SpawnParticle(
                new GlowOrbParticle(
                    Projectile.Center + firstHelix,
                    right * ((float)Math.Cos(orbitPhase) * 0.2f),
                    false,
                    9,
                    0.7f + pulse * 0.24f,
                    Color.Lerp(accentColor, Color.White, 0.22f + 0.35f * pulse),
                    true,
                    false,
                    true));

            GeneralParticleHandler.SpawnParticle(
                new GlowOrbParticle(
                    Projectile.Center + secondHelix,
                    -right * ((float)Math.Cos(orbitPhase) * 0.2f),
                    false,
                    9,
                    0.68f + (1f - pulse) * 0.24f,
                    Color.Lerp(mainColor, Color.White, 0.2f + 0.35f * (1f - pulse)),
                    true,
                    false,
                    true));

            if (frameCounter % 2 == 0)
            {
                Dust wakeDust = Dust.NewDustPerfect(
                    Projectile.Center - forward * 5f,
                    Main.rand.NextBool(3) ? DustID.Frost : DustID.YellowTorch,
                    -forward * Main.rand.NextFloat(0.6f, 1.8f),
                    100,
                    Color.Lerp(accentColor, mainColor, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.85f, 1.15f));
                wakeDust.noGravity = true;
                wakeDust.fadeIn = 1.08f;
            }

            if (frameCounter % 6 == 0)
            {
                Vector2 backgroundPos =
                    Projectile.Center -
                    forward * Main.rand.NextFloat(8f, 22f) +
                    right * Main.rand.NextFloat(-16f, 16f);

                GeneralParticleHandler.SpawnParticle(
                    new GlowOrbParticle(
                        backgroundPos,
                        -forward * Main.rand.NextFloat(0.2f, 0.8f),
                        false,
                        12,
                        Main.rand.NextFloat(0.24f, 0.42f),
                        Color.Lerp(mainColor, Color.White, 0.3f),
                        true,
                        false,
                        true));
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
                    accentColor,
                    Main.rand.NextFloat(0.95f, 1.35f) * intensity);
                water.noGravity = true;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(
                        center,
                        DustID.Frost,
                        direction * Main.rand.NextFloat(2.2f, 5.5f) * intensity,
                        100,
                        mainColor,
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

                    GeneralParticleHandler.SpawnParticle(
                        new GlowOrbParticle(
                            position,
                            Vector2.Zero,
                            false,
                            8 + Main.rand.Next(4),
                            0.75f + (1f - t) * 0.35f * intensity,
                            Color.Lerp(accentColor, Color.White, 0.22f + 0.45f * t),
                            true,
                            false,
                            true));
                }
            }

            for (int i = 0; i < 3; i++)
            {
                DirectionalPulseRing pulse = new DirectionalPulseRing(
                    center,
                    Vector2.Zero,
                    Color.Lerp(accentColor, Color.White, 0.22f + i * 0.2f),
                    new Vector2(0.55f + i * 0.08f, 1.25f + i * 0.2f),
                    forward.ToRotation(),
                    0.1f + i * 0.03f,
                    0.014f,
                    10 + i * 3);
                GeneralParticleHandler.SpawnParticle(pulse);
            }
        }

        private float TrailWidthFunction(float completionRatio, Vector2 vertexPosition)
        {
            float width = 22f * Projectile.scale;
            float envelope = (float)Math.Sin(completionRatio * MathHelper.Pi);
            envelope = (float)Math.Pow(envelope, 0.65f);
            return MathHelper.Lerp(2f, width, envelope);
        }

        private Color TrailColorFunction(float completionRatio, Vector2 vertexPosition)
        {
            float fadeToEnd = Utils.GetLerpValue(0f, 0.82f, completionRatio, true);
            Color baseColor = Color.Lerp(mainColor, accentColor, 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f + completionRatio * 4f));
            Color color = Color.Lerp(baseColor, Color.White, Utils.GetLerpValue(0f, 0.22f, completionRatio, true) * 0.45f);
            color *= fadeToEnd * 0.95f;
            color.A = 0;
            return color;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D starTexture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/StarofJudgement").Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ringTexture = TextureAssets.Extra[89].Value;
            Vector2 starOrigin = starTexture.Size() * 0.5f;
            Vector2 bloomOrigin = bloomTexture.Size() * 0.5f;
            Vector2 ringOrigin = ringTexture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float pulse = 1f + 0.11f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f + pulseOffset);

            Vector2[] trailPositions = new Vector2[Projectile.oldPos.Length];
            for (int i = 0; i < Projectile.oldPos.Length; i++)
                trailPositions[i] = Projectile.oldPos[i] == Vector2.Zero ? Projectile.Center : Projectile.oldPos[i] + Projectile.Size * 0.5f;

            GameShaders.Misc["CalamityMod:TrailStreak"].UseImage1("Images/Misc/Perlin");
            PrimitiveRenderer.RenderTrail(
                trailPositions,
                new PrimitiveSettings(
                    TrailWidthFunction,
                    TrailColorFunction,
                    (completionRatio, vertexPos) => Vector2.Zero,
                    shader: GameShaders.Misc["CalamityMod:TrailStreak"]),
                40);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float t = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Color ghostColor = Color.Lerp(accentColor, mainColor, 0.5f) * (0.18f + 0.3f * t);
                ghostColor.A = 0;

                Main.spriteBatch.Draw(
                    starTexture,
                    trailPos,
                    null,
                    ghostColor,
                    Projectile.rotation,
                    starOrigin,
                    Projectile.scale * (0.4f + 0.35f * t),
                    SpriteEffects.None,
                    0f);
            }

            Main.spriteBatch.Draw(
                bloomTexture,
                drawPosition,
                null,
                accentColor * 0.4f,
                0f,
                bloomOrigin,
                Projectile.scale * new Vector2(1.5f, 1.5f) * pulse,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.Draw(
                ringTexture,
                drawPosition,
                null,
                mainColor * 0.46f,
                Projectile.rotation,
                ringOrigin,
                Projectile.scale * new Vector2(1.5f, 0.23f) * pulse,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.Draw(
                ringTexture,
                drawPosition,
                null,
                accentColor * 0.42f,
                Projectile.rotation + MathHelper.PiOver2,
                ringOrigin,
                Projectile.scale * new Vector2(1.3f, 0.2f) * pulse,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.Draw(
                starTexture,
                drawPosition,
                null,
                Color.Lerp(mainColor, Color.White, 0.18f) with { A = 0 },
                Projectile.rotation,
                starOrigin,
                Projectile.scale * pulse,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.Draw(
                starTexture,
                drawPosition,
                null,
                Color.White with { A = 0 },
                Projectile.rotation,
                starOrigin,
                Projectile.scale * 0.74f * pulse,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
