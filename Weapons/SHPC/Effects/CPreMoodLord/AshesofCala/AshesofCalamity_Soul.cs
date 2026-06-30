using CalamityMod;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.AshesofCala
{
    internal sealed class AshesofCalamity_Soul : ModProjectile, ILocalizedModType
    {
        private const float BaseSpeed = 15.5f;
        private const float HomingRange = 150f * 16f;
        private const float HomingStartFrames = 9f;
        private const float HomingWarmupFrames = 18f;
        private const float MinHomingSpeed = 10.5f;
        private const float MaxHomingSpeed = 16.5f;
        private const float HomingInertia = 22f;
        private const float FreeFlightDamping = 0.996f;
        private const float NoTargetDamping = 0.992f;
        private const float WanderingTurnStrength = 0.006f;
        private const int TotalRelayShots = 9;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private int ShotIndex => (int)Projectile.ai[1];
        private bool IsPiercingShot => Projectile.ai[0] > 0f;
        private float ShotCompletion => MathHelper.Clamp(ShotIndex / (float)(TotalRelayShots - 1), 0f, 1f);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 26;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 540;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (Projectile.velocity == Vector2.Zero)
                Projectile.velocity = Vector2.UnitX * BaseSpeed;

            if (!IsPiercingShot)
                return;

            Projectile.penetrate = -1;
            Projectile.extraUpdates = 0;
            Projectile.timeLeft = 720;
            Projectile.localNPCHitCooldown = 8;
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * MathHelper.Lerp(5.4f, 7.2f, ShotCompletion);
        }

        public override void AI()
        {
            // Projectile.extraUpdates = 1 会让 AI 一帧跑多次。
            // 这里只在每个真实游戏帧的最后一次更新时计时，避免延迟被 extraUpdates 缩短。
            if (Projectile.numUpdates == 0)
                Timer++;

            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            if (IsPiercingShot)
            {
                UpdatePiercingFlight(currentDirection);
            }
            else if (Timer >= HomingStartFrames)
            {
                NPC target = FindNearestTarget(HomingRange);
                if (target is not null)
                    SoftHomeTowardTarget(target, currentDirection);
                else
                    FreeDrift(NoTargetDamping);
            }
            else
            {
                // 前几个真实游戏帧不找目标，只保持柔和游移。
                FreeDrift(FreeFlightDamping);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.alpha = Math.Max(0, Projectile.alpha - 24);
            Lighting.AddLight(Projectile.Center, new Color(255, 120, 48).ToVector3() * 0.55f);

            if (!Main.dedServ)
            {
                SpawnFlightEffects();
                SpawnCalamitousDartMetaballs(Projectile.velocity.SafeNormalize(Vector2.UnitX));
            }
        }

        private void SoftHomeTowardTarget(NPC target, Vector2 fallbackDirection)
        {
            Vector2 currentVelocity = Projectile.velocity;
            float currentSpeed = currentVelocity.Length();
            if (currentSpeed < 0.1f)
                currentVelocity = (target.Center - Projectile.Center).SafeNormalize(fallbackDirection) * 4f;

            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(currentVelocity.SafeNormalize(fallbackDirection));

            float homingTimer = Timer - HomingStartFrames;
            float warmup = Utils.GetLerpValue(0f, HomingWarmupFrames, homingTimer, true);
            float closePressure = Utils.GetLerpValue(360f, 70f, Projectile.Distance(target.Center), true);
            float pullStrength = MathHelper.Lerp(0.35f, 1f, MathHelper.Max(warmup, closePressure * 0.75f));

            float targetSpeed = MathHelper.Lerp(MinHomingSpeed, MaxHomingSpeed, pullStrength);
            Vector2 desiredVelocity = desiredDirection * targetSpeed;
            Projectile.velocity = (currentVelocity * HomingInertia + desiredVelocity) / (HomingInertia + 1f);

            float sideSway = (float)Math.Sin((Timer + Projectile.identity * 7f) * 0.075f) *
                MathHelper.Lerp(0.012f, 0.004f, pullStrength);
            Projectile.velocity = Projectile.velocity.RotatedBy(sideSway);

            if (Projectile.velocity.Length() > MaxHomingSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(desiredDirection) * MaxHomingSpeed;
        }

        private void FreeDrift(float damping)
        {
            float wander = (float)Math.Sin((Timer + Projectile.identity * 5f) * 0.08f) * WanderingTurnStrength;
            Projectile.velocity = Projectile.velocity.RotatedBy(wander) * damping;
        }

        private void UpdatePiercingFlight(Vector2 currentDirection)
        {
            float targetSpeed = MathHelper.Lerp(5.2f, 7.1f, ShotCompletion);
            float wave = (float)Math.Sin((Timer + ShotIndex * 19f) * 0.12f) * 0.020f;
            wave += (float)Math.Sin((Timer + Projectile.identity * 3f) * 0.047f) * 0.006f;
            Projectile.velocity = currentDirection.RotatedBy(wave) * MathHelper.Lerp(Projectile.velocity.Length(), targetSpeed, 0.035f);
        }

        private NPC FindNearestTarget(float range)
        {
            NPC bestTarget = null;
            float bestDistance = range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestTarget = npc;
                bestDistance = distance;
            }

            return bestTarget;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!IsPiercingShot)
                Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            Color orange = new(255, 140, 42);
            Color red = new(180, 12, 8);

            int burstCount = IsPiercingShot ? 16 : 10;
            int bladeLifetime = IsPiercingShot ? Main.rand.Next(10, 16) : 1;
            float burstScale = IsPiercingShot ? 1.28f : 1f;

            for (int i = 0; i < burstCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 6f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center,
                    velocity,
                    "CalamityMod/Particles/VerticalSmear",
                    false,
                    bladeLifetime,
                    Main.rand.NextFloat(0.6f, 1.2f) * burstScale,
                    Color.Lerp(orange, red, Main.rand.NextFloat(0.15f, 0.65f)),
                    new Vector2(0.18f, 0.86f) * burstScale,
                    true,
                    true,
                    extraRotation: MathHelper.TwoPi * i / burstCount + Main.rand.NextFloat(-0.18f, 0.18f),
                    shrinkSpeed: IsPiercingShot ? 0.18f : 0.82f,
                    glowOpacity: IsPiercingShot ? 0.66f : 0.48f));
            }
        }

        private void SpawnFlightEffects()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Color orange = new(255, 140, 42);
            Color red = new(180, 12, 8);

            SpawnBladeDisc(direction, normal, orange, red);

            if (IsPiercingShot && Main.rand.NextBool(2))
            {
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(8f, 22f) + normal * Main.rand.NextFloat(-10f, 10f),
                    DustID.Torch,
                    -direction * Main.rand.NextFloat(0.4f, 1.6f) + normal * Main.rand.NextFloat(-0.7f, 0.7f),
                    100,
                    Color.Lerp(orange, red, Main.rand.NextFloat(0.2f, 0.7f)),
                    Main.rand.NextFloat(0.9f, 1.45f));
                ember.noGravity = true;
            }
        }

        private void SpawnBladeDisc(Vector2 direction, Vector2 normal, Color orange, Color red)
        {
            int bladeCount = IsPiercingShot ? 5 : 3;
            int lifetime = IsPiercingShot ? Main.rand.Next(12, 18) : 1;
            float discRotation = direction.ToRotation() + Timer * (IsPiercingShot ? 0.24f : 0.42f) + ShotIndex * 0.71f;
            float scale = IsPiercingShot ? Main.rand.NextFloat(1.45f, 2.15f) : Main.rand.NextFloat(1.1f, 1.55f);

            for (int i = 0; i < bladeCount; i++)
            {
                float rotation = discRotation + MathHelper.TwoPi * i / bladeCount;
                Vector2 bladeDirection = rotation.ToRotationVector2();
                Vector2 center = Projectile.Center
                    - direction * Main.rand.NextFloat(1f, IsPiercingShot ? 8f : 5f)
                    + normal * Main.rand.NextFloat(-3f, 3f);

                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    center,
                    Projectile.velocity * 0.015f + bladeDirection * Main.rand.NextFloat(0.08f, IsPiercingShot ? 0.42f : 0.18f),
                    "CalamityMod/Particles/VerticalSmear",
                    false,
                    lifetime,
                    scale,
                    Color.Lerp(orange, red, Main.rand.NextFloat(0.12f, 0.72f)),
                    new Vector2(IsPiercingShot ? 0.24f : 0.18f, IsPiercingShot ? 1.08f : 0.82f),
                    true,
                    true,
                    extraRotation: rotation,
                    shrinkSpeed: IsPiercingShot ? 0.16f : 0.82f,
                    glowOpacity: IsPiercingShot ? 0.7f : 0.52f,
                    spin: IsPiercingShot ? Main.rand.NextFloat(-0.045f, 0.045f) : 0f));
            }
        }

        private void SpawnCalamitousDartMetaballs(Vector2 direction)
        {
            CalamitasMetaball.Particle point = CalamitasMetaball.SpawnParticle(
                Projectile.Center + Projectile.velocity * 2f,
                Vector2.Zero,
                40f * Projectile.scale);

            point.rotation = direction.ToRotation() + MathHelper.PiOver2;
            point.TextureToUse = ModContent.Request<Texture2D>("CalamityMod/Particles/PointParticle").Value;
            point.SizeScaling = 0.65f;

            CalamitasMetaball.Particle body = CalamitasMetaball.SpawnParticle(
                Projectile.Center,
                Main.rand.NextVector2Circular(3f, 3f),
                24f * Projectile.scale);

            body.SizeScaling = 0.8f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmear").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(255f, 0f, Projectile.alpha, true);
            Color orange = new Color(255, 140, 42, 0) * opacity;
            Color red = new Color(170, 8, 8, 0) * opacity;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null, red * 0.52f, Projectile.rotation, bloom.Size() * 0.5f, 0.28f, SpriteEffects.None);
            Main.EntitySpriteDraw(smear, drawPosition, null, orange * 0.68f, Projectile.rotation, smear.Size() * 0.5f, new Vector2(0.12f, 0.58f), SpriteEffects.None);
            Main.EntitySpriteDraw(star, drawPosition, null, Color.White * (0.28f * opacity), Projectile.rotation, star.Size() * 0.5f, new Vector2(0.18f, 0.42f), SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
