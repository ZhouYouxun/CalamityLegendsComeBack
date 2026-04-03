using System;
using CalamityLegendsComeBack.Weapons.BrinyBaron.POWER;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillB_SpinDash
{
    public class BrinyBaron_SkillSpinRush_SpinBlade : BaseCustomUseStyleProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron";
        public override int AssignedItemID => ModContent.ItemType<NewLegendBrinyBaron>();
        public override float HitboxOutset => 110f * Projectile.scale;
        public override Vector2 HitboxSize => new Vector2(190f, 190f) * Projectile.scale;
        public override float HitboxRotationOffset => MathHelper.ToRadians(-45f);
        public override Vector2 SpriteOrigin => new(0f, 102f);

        private const int ChargeFrames = 32;
        private const int DashFrames = 32;
        private const float DashStartSpeed = 18f;
        private const float DashTopSpeed = 36f;
        private const float ChargeDistance = 18f;
        private const float ChargeDistanceMax = 28f;
        private const float DashHoldDistance = 14f;
        private const float DashSwooshScale = 1.18f;

        private const int ChargeState = 0;
        private const int DashState = 1;

        private Vector2 lockedDirection = Vector2.UnitX;
        private int stateTimer;
        private int currentState;
        private float fadeIn;
        private float oceanPhase;
        private bool chargeFullEffectsPlayed;
        private bool dashStarted;
        private bool dashImpactPlayed;
        private Vector2 dashVelocity;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = ChargeFrames + DashFrames + 90;
            Projectile.scale = 1f;
        }

        public override void WhenSpawned()
        {
            IgnoreActiveAnimation = true;
            DrawUnconditionally = true;
            Projectile.knockBack = 0f;
            Projectile.ai[1] = -1f;
            Projectile.scale = 1f;

            lockedDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            if (lockedDirection == Vector2.Zero)
                lockedDirection = (Owner.Calamity().mouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction);

            currentState = ChargeState;
            stateTimer = 0;
            fadeIn = 0f;
            oceanPhase = Main.rand.NextFloat(MathHelper.TwoPi);
            chargeFullEffectsPlayed = false;
            dashStarted = false;
            dashImpactPlayed = false;
            dashVelocity = Vector2.Zero;

            Owner.direction = lockedDirection.X >= 0f ? 1 : -1;
            FlipAsSword = Owner.direction == -1;
            Projectile.rotation = lockedDirection.ToRotation() + MathHelper.ToRadians(45f);
            RotationOffset = MathHelper.ToRadians(110f * Owner.direction);
            Offset = lockedDirection * ChargeDistance;
        }

        public override void UseStyle()
        {
            Owner.itemAnimation = 2;
            Owner.itemTime = 2;
            Owner.heldProj = Projectile.whoAmI;
            Owner.ChangeDir(lockedDirection.X >= 0f ? 1 : -1);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + RotationOffset + ArmRotationOffset);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + RotationOffset + ArmRotationOffsetBack);
            Owner.Calamity().mouseWorldListener = true;
            Owner.Calamity().rightClickListener = true;

            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 2);
            oceanPhase += 0.22f;

            switch (currentState)
            {
                case ChargeState:
                    DoChargeState();
                    break;
                case DashState:
                    DoDashState();
                    break;
            }

            ArmRotationOffset = MathHelper.ToRadians(-140f);
            ArmRotationOffsetBack = MathHelper.ToRadians(-140f);
            Lighting.AddLight(Projectile.Center, 0.05f, 0.22f, 0.3f);
        }

        private void DoChargeState()
        {
            if (!IsChargeHeld())
            {
                SpawnChargeCancelBurst();
                Projectile.Kill();
                return;
            }

            stateTimer++;
            CanHit = false;
            Projectile.velocity = Vector2.Zero;
            AbsolutePosition = Vector2.Zero;

            Vector2 aimWorld = Owner.Calamity().mouseWorld;
            Vector2 targetDirection = (aimWorld - Owner.Center).SafeNormalize(lockedDirection);
            lockedDirection = Vector2.Lerp(lockedDirection, targetDirection, 0.18f).SafeNormalize(targetDirection);

            float chargeProgress = Utils.GetLerpValue(0f, ChargeFrames, stateTimer, true);
            float easedCharge = EvaluateFluidCharge(chargeProgress);
            float chargeDistance = MathHelper.Lerp(ChargeDistance, ChargeDistanceMax, easedCharge);
            float swayAngle = MathHelper.ToRadians(14f * (float)Math.Sin(oceanPhase * 1.25f));

            Offset = lockedDirection * chargeDistance;
            Projectile.rotation = Projectile.rotation.AngleLerp(lockedDirection.ToRotation() + MathHelper.ToRadians(45f), 0.22f);
            RotationOffset = MathHelper.Lerp(
                RotationOffset,
                MathHelper.ToRadians(MathHelper.Lerp(40f, 118f, easedCharge) * -Owner.direction + MathHelper.ToDegrees(swayAngle)),
                0.16f);

            FlipAsSword = lockedDirection.X < 0f;
            fadeIn = MathHelper.Lerp(fadeIn, 0.92f, 0.12f);

            SpawnChargeParticles(easedCharge);

            if (stateTimer >= ChargeFrames && !chargeFullEffectsPlayed)
            {
                chargeFullEffectsPlayed = true;
                SpawnChargeReadyBurst();
                StartDash();
            }
        }

        private void StartDash()
        {
            currentState = DashState;
            stateTimer = 0;
            CanHit = false;
            dashStarted = true;
            Offset = lockedDirection * DashHoldDistance;
            AbsolutePosition = Vector2.Zero;
            dashVelocity = lockedDirection * DashStartSpeed;
            Projectile.velocity = Vector2.Zero;
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Item122 with
            {
                Volume = 0.95f,
                Pitch = -0.18f
            }, Projectile.Center);

            SoundEngine.PlaySound(SoundID.Item84 with
            {
                Volume = 0.7f,
                Pitch = -0.25f
            }, Projectile.Center);
        }

        private void DoDashState()
        {
            stateTimer++;

            float dashProgress = Utils.GetLerpValue(0f, DashFrames, stateTimer, true);
            float easedDash = EvaluateFluidCharge(dashProgress);
            float dashSpeed = MathHelper.Lerp(DashStartSpeed, DashTopSpeed, easedDash);
            float spinDegrees = MathHelper.Lerp(135f, 915f, dashProgress) * -Owner.direction;

            dashVelocity = lockedDirection * dashSpeed;
            Projectile.velocity = Vector2.Zero;
            AbsolutePosition = Vector2.Zero;
            Offset = lockedDirection * DashHoldDistance;
            Owner.Center += dashVelocity;
            Owner.velocity = dashVelocity;

            Projectile.rotation = Projectile.rotation.AngleLerp(lockedDirection.ToRotation() + MathHelper.ToRadians(45f), 0.35f);
            RotationOffset = MathHelper.Lerp(RotationOffset, MathHelper.ToRadians(spinDegrees), 0.26f);
            FlipAsSword = lockedDirection.X < 0f;
            fadeIn = MathHelper.Lerp(fadeIn, 1f, 0.2f);

            CanHit = stateTimer >= 3 && stateTimer <= DashFrames - 3;
            SpawnDashTrailEffects(easedDash);

            if (stateTimer >= DashFrames)
                Projectile.Kill();
        }

        private bool IsChargeHeld()
        {
            if (Owner.whoAmI != Main.myPlayer)
                return true;

            return Owner.Calamity().mouseRight &&
                   !Owner.noItems &&
                   !Owner.CCed &&
                   !Owner.mouseInterface &&
                   !Main.mapFullscreen &&
                   !Main.blockMouse;
        }

        private static float EvaluateFluidCharge(float progress)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            float smootherStep = progress * progress * progress * (progress * (progress * 6f - 15f) + 10f);
            float sineEase = 0.5f - 0.5f * (float)Math.Cos(progress * MathHelper.Pi);
            return MathHelper.Lerp(sineEase, smootherStep, 0.7f);
        }

        private void SpawnChargeParticles(float chargeProgress)
        {
            Vector2 forward = lockedDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 bladeTip = Projectile.Center + forward * 74f * Projectile.scale;

            if (stateTimer % 2 == 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    float side = i - 1f;
                    float spiral = oceanPhase * 1.8f + i * 0.8f;
                    Vector2 spawnPos = bladeTip - forward * MathHelper.Lerp(22f, 68f, chargeProgress) + right * (float)Math.Sin(spiral) * MathHelper.Lerp(6f, 18f, chargeProgress) * side;
                    Vector2 travelVelocity = (bladeTip - spawnPos).SafeNormalize(forward) * Main.rand.NextFloat(1.8f, 4.4f);

                    Dust water = Dust.NewDustPerfect(spawnPos, DustID.Water, travelVelocity, 100, new Color(70, 170, 255), Main.rand.NextFloat(0.95f, 1.25f));
                    water.noGravity = true;

                    if (Main.rand.NextBool(2))
                    {
                        Dust frost = Dust.NewDustPerfect(spawnPos, DustID.Frost, travelVelocity * 0.68f, 100, new Color(220, 250, 255), Main.rand.NextFloat(0.8f, 1.05f));
                        frost.noGravity = true;
                    }
                }
            }

            if (Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(
                    new GlowOrbParticle(
                        bladeTip + Main.rand.NextVector2Circular(6f, 6f),
                        Main.rand.NextVector2Circular(0.4f, 0.4f),
                        false,
                        7,
                        MathHelper.Lerp(0.45f, 0.85f, chargeProgress),
                        Main.rand.NextBool() ? Color.Cyan : Color.DeepSkyBlue,
                        true,
                        false,
                        true));
            }
        }

        private void SpawnChargeReadyBurst()
        {
            Vector2 forward = lockedDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 burstCenter = Owner.Center + forward * 70f;

            for (int i = 0; i < 20; i++)
            {
                float ratio = i / 19f;
                float angle = MathHelper.Lerp(-MathHelper.PiOver2, MathHelper.PiOver2, ratio);
                Vector2 velocity = forward.RotatedBy(angle * 0.35f) * Main.rand.NextFloat(4f, 10f) + right * (float)Math.Sin(angle * 2f) * Main.rand.NextFloat(0.5f, 2.8f);

                Dust water = Dust.NewDustPerfect(burstCenter, DustID.Water, velocity, 100, new Color(65, 170, 255), Main.rand.NextFloat(1.1f, 1.45f));
                water.noGravity = true;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(burstCenter, DustID.Frost, velocity * 0.65f, 100, new Color(210, 250, 255), Main.rand.NextFloat(0.95f, 1.2f));
                    frost.noGravity = true;
                }
            }

            for (int i = 0; i < 5; i++)
            {
                Vector2 crossVelocity = forward.RotatedBy(MathHelper.ToRadians(-25f + 12.5f * i)) * 6.4f;
                GeneralParticleHandler.SpawnParticle(
                    new SparkParticle(
                        burstCenter - forward * 6f,
                        crossVelocity,
                        false,
                        6,
                        1.5f,
                        i % 2 == 0 ? Color.DeepSkyBlue : Color.Cyan,
                        true));
            }

            GeneralParticleHandler.SpawnParticle(
                new HeavySmokeParticle(
                    burstCenter,
                    -forward * 1.8f,
                    Color.WhiteSmoke,
                    22,
                    1.1f,
                    0.35f,
                    Main.rand.NextFloat(-0.04f, 0.04f),
                    false));

            ApplyScreenShake(7f);
        }

        private void SpawnChargeCancelBurst()
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 dustVelocity = Main.rand.NextVector2Circular(1.6f, 1.6f);
                Dust water = Dust.NewDustPerfect(Projectile.Center, DustID.Water, dustVelocity, 100, new Color(90, 175, 255), Main.rand.NextFloat(0.8f, 1.05f));
                water.noGravity = true;
            }
        }

        private void SpawnDashTrailEffects(float dashProgress)
        {
            Vector2 forward = dashVelocity.SafeNormalize(lockedDirection);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 lead = Projectile.Center + forward * 54f;

            for (int i = 0; i < 3; i++)
            {
                float side = i - 1f;
                float helix = oceanPhase * 2.3f + i * 0.85f;
                Vector2 spawnPos = Projectile.Center - forward * (18f + i * 10f) + right * side * (12f + 6f * (float)Math.Sin(helix));
                Vector2 velocity = -forward * Main.rand.NextFloat(2.4f, 5.8f) + right * side * Main.rand.NextFloat(0.4f, 2f);

                Dust water = Dust.NewDustPerfect(spawnPos, DustID.Water, velocity, 100, new Color(70, 170, 255), Main.rand.NextFloat(0.95f, 1.3f));
                water.noGravity = true;

                if (i != 1)
                {
                    Dust frost = Dust.NewDustPerfect(spawnPos, DustID.Frost, velocity * 0.62f, 100, new Color(210, 250, 255), Main.rand.NextFloat(0.82f, 1.08f));
                    frost.noGravity = true;
                }
            }

            if (stateTimer % 3 == 0)
            {
                GeneralParticleHandler.SpawnParticle(
                    new CustomSpark(
                        lead,
                        forward * Main.rand.NextFloat(0.2f, 0.8f),
                        "CalamityMod/Particles/BloomCircle",
                        false,
                        14,
                        MathHelper.Lerp(0.35f, 0.58f, dashProgress),
                        Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan,
                        new Vector2(1.4f, 0.6f),
                        true,
                        false,
                        0f,
                        false,
                        false,
                        0.18f));
            }
        }

        private void SpawnHitEffects(NPC target)
        {
            Vector2 forward = dashVelocity.SafeNormalize(lockedDirection);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 18; i++)
            {
                float angle = MathHelper.Lerp(-MathHelper.ToRadians(70f), MathHelper.ToRadians(70f), i / 17f);
                Vector2 sprayVelocity = forward.RotatedBy(angle) * Main.rand.NextFloat(5f, 12f) + right * (float)Math.Sin(i * 0.7f) * Main.rand.NextFloat(0.5f, 3f);

                Dust water = Dust.NewDustPerfect(target.Center, DustID.Water, sprayVelocity, 100, new Color(75, 175, 255), Main.rand.NextFloat(1.1f, 1.5f));
                water.noGravity = true;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(target.Center, DustID.Frost, sprayVelocity * 0.7f, 100, new Color(220, 250, 255), Main.rand.NextFloat(0.95f, 1.25f));
                    frost.noGravity = true;
                }
            }

            GeneralParticleHandler.SpawnParticle(
                new HeavySmokeParticle(
                    target.Center,
                    -forward * 1.4f,
                    Color.WhiteSmoke,
                    24,
                    1.2f,
                    0.4f,
                    Main.rand.NextFloat(-0.06f, 0.06f),
                    false));

            ApplyScreenShake(9f);
        }

        private void ApplyScreenShake(float power)
        {
            float distanceFactor = Utils.GetLerpValue(1200f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                Main.LocalPlayer.Calamity().GeneralScreenShakePower,
                power * distanceFactor);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 180);
            Owner.GetModPlayer<BBEXPlayer>().AddTide();

            if (!dashImpactPlayed)
            {
                dashImpactPlayed = true;
                SpawnHitEffects(target);

                SoundEngine.PlaySound(SoundID.Item14 with
                {
                    Volume = 0.55f,
                    Pitch = 0.15f
                }, target.Center);

                SoundEngine.PlaySound(SoundID.Splash with
                {
                    Volume = 0.75f,
                    Pitch = -0.1f
                }, target.Center);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (dashStarted && Owner.active && !Owner.dead)
                Owner.velocity *= 0.82f;

            dashVelocity = Vector2.Zero;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Asset<Texture2D> tex = ModContent.Request<Texture2D>(Texture);
            Asset<Texture2D> swoosh = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge");

            float r = FlipAsSword ? MathHelper.ToRadians(90f) : 0f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            SpriteEffects effects = spriteEffects != SpriteEffects.None ? spriteEffects : (FlipAsSword ? SpriteEffects.FlipHorizontally : SpriteEffects.None);

            if (currentState == DashState)
            {
                for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                        continue;

                    float factor = 1f - i / (float)Projectile.oldPos.Length;
                    Color trailColor = Color.Lerp(new Color(35, 95, 145, 0), new Color(140, 235, 255, 0), factor) * factor * 0.55f;

                    Main.EntitySpriteDraw(
                        tex.Value,
                        Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
                        tex.Frame(1, FrameCount, 0, Frame),
                        trailColor,
                        Projectile.rotation + RotationOffset + r,
                        FlipAsSword ? new Vector2(tex.Width() - SpriteOrigin.X, SpriteOrigin.Y) : SpriteOrigin,
                        Projectile.scale,
                        effects,
                        0);
                }

                Main.EntitySpriteDraw(
                    swoosh.Value,
                    drawPos,
                    null,
                    Color.DeepSkyBlue with { A = 0 } * fadeIn * 0.68f,
                    FinalRotation + MathHelper.ToRadians(135f),
                    swoosh.Size() * 0.5f,
                    Projectile.scale * DashSwooshScale,
                    SpriteEffects.None,
                    0);
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 18f).ToRotationVector2() * 4f * fadeIn;
                Main.EntitySpriteDraw(
                    tex.Value,
                    drawPos + drawOffset,
                    tex.Frame(1, FrameCount, 0, Frame),
                    Color.Aqua with { A = 0 } * 0.11f * fadeIn,
                    Projectile.rotation + RotationOffset + r,
                    FlipAsSword ? new Vector2(tex.Width() - SpriteOrigin.X, SpriteOrigin.Y) : SpriteOrigin,
                    Projectile.scale,
                    effects,
                    0);
            }

            Main.EntitySpriteDraw(
                tex.Value,
                drawPos,
                tex.Frame(1, FrameCount, 0, Frame),
                lightColor,
                Projectile.rotation + RotationOffset + r,
                FlipAsSword ? new Vector2(tex.Width() - SpriteOrigin.X, SpriteOrigin.Y) : SpriteOrigin,
                Projectile.scale,
                effects,
                0);

            return false;
        }
    }
}
