using System;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeComboHoldout : BaseFlailProjectile, ILocalizedModType
    {
        private const int WhipArcDuration = 46;
        private const int WhipArcWindup = 10;
        private const int WhipArcSnap = 9;
        private const int WhipArcHold = 4;
        private const int WhipThrustDuration = 52;
        private const int WhipThrustWindup = 13;
        private const int SwordSwingDuration = 36;
        private const int SwordSwingWindup = 9;
        private const int SwordFinisherDuration = 72;
        private const int SwordFinisherWindup = 34;
        private const int SwordFinisherSlamFrame = 43;
        private const int QuickDrawDuration = 40;
        private const int QuickDrawWindup = 4;

        private const float WhipReach = 510f;
        private const float ThrustReach = 545f;
        private const float QuickDrawReach = 620f;
        private const float SwordReach = 292f;
        private const float FinisherReach = 358f;

        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => CosmicDischargeCommon.ChainTexturePath;

        private bool wasRightHeld;
        private bool releaseSoundPlayed;
        private bool impactEffectsPlayed;
        private bool spawnedSwordWave;
        private int spawnedBombBursts;
        private int hitStopTimer;
        private int impactFlashTimer;
        private float currentCollisionWidth = 30f;
        private bool currentlyRetracting;
        private float currentArmRotationOffset;

        private CosmicDischargeAttackKind Kind
        {
            get => (CosmicDischargeAttackKind)(int)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        private ref float AimAngle => ref Projectile.ai[1];
        private ref float Time => ref Projectile.localAI[0];
        private ref float QuickDrawQueued => ref Projectile.localAI[1];
        private Player Owner => Main.player[Projectile.owner];
        private Vector2 AimDirection => AimAngle.ToRotationVector2();

        public bool CanBecomeQuickDraw =>
            Owner.GetModPlayer<CosmicDischargePlayer>().QuickDrawCooldownTimer <= 0 &&
            ((Kind == CosmicDischargeAttackKind.WhipThrust && Time <= WhipThrustWindup) ||
             (Kind == CosmicDischargeAttackKind.SwordFinisher && Time <= SwordFinisherWindup));

        public override Color SpecialDrawColor => CosmicDischargeCommon.FrostCoreColor;
        public override int ExudeDustType => DustID.SnowflakeIce;
        public override int WhipDustType => DustID.Frost;
        public override int HandleHeight => 62;
        public override int BodyType1StartY => 64;
        public override int BodyType1SectionHeight => 28;
        public override int BodyType2StartY => 94;
        public override int BodyType2SectionHeight => 18;
        public override int TailStartY => 114;
        public override int TailHeight => 84;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 9;
            Projectile.ownerHitCheck = true;
            Projectile.coldDamage = true;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = 2;
            if (AimAngle == 0f)
                AimAngle = Vector2.UnitX.RotatedByRandom(0.01f).ToRotation();

            for (int i = 0; i < Main.maxNPCs; i++)
                Projectile.localNPCImmunity[i] = 0;
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<NewLegendCosmicDischarge>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Owner.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == Projectile.owner)
                Owner.Calamity().rightClickListener = true;

            bool validMouse = !Main.mapFullscreen && !Main.blockMouse;
            bool rightHeld = Main.myPlayer == Projectile.owner && validMouse && (Main.mouseRight || Owner.Calamity().mouseRight);
            if (rightHeld && !wasRightHeld && CanBecomeQuickDraw)
                QuickDrawQueued = 1f;
            wasRightHeld = rightHeld;

            if (QuickDrawQueued > 0f && Kind != CosmicDischargeAttackKind.QuickDraw)
                BeginQuickDraw();

            if (hitStopTimer > 0)
            {
                hitStopTimer--;
                HoldBladeStill();
                return;
            }

            Time++;
            if (impactFlashTimer > 0)
                impactFlashTimer--;

            currentlyRetracting = false;
            Projectile.localNPCHitCooldown = Kind == CosmicDischargeAttackKind.QuickDraw ? 3 : 9;

            switch (Kind)
            {
                case CosmicDischargeAttackKind.WhipOver:
                    UpdateWhipArc(-1f);
                    break;
                case CosmicDischargeAttackKind.WhipUnder:
                    UpdateWhipArc(1f);
                    break;
                case CosmicDischargeAttackKind.WhipThrust:
                    UpdateThrust(false);
                    break;
                case CosmicDischargeAttackKind.SwordSwingOne:
                    UpdateSwordSwing(false);
                    break;
                case CosmicDischargeAttackKind.SwordSwingTwo:
                    UpdateSwordSwing(true);
                    break;
                case CosmicDischargeAttackKind.SwordFinisher:
                    UpdateSwordFinisher();
                    break;
                case CosmicDischargeAttackKind.QuickDraw:
                    UpdateThrust(true);
                    break;
            }

            if (Projectile.alpha > 0)
                Projectile.alpha = Math.Max(0, Projectile.alpha - 42);

            SpawnBladeWakeDust();
        }

        public bool TryRequestQuickDraw()
        {
            if (!CanBecomeQuickDraw)
                return false;

            QuickDrawQueued = 1f;
            Projectile.netUpdate = true;
            return true;
        }

        private void BeginQuickDraw()
        {
            Vector2 direction = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection);
            Kind = CosmicDischargeAttackKind.QuickDraw;
            AimAngle = direction.ToRotation();
            Time = 0f;
            QuickDrawQueued = 0f;
            spawnedBombBursts = 0;
            spawnedSwordWave = false;
            releaseSoundPlayed = false;
            impactEffectsPlayed = false;
            Projectile.localNPCHitCooldown = 3;
            Projectile.netUpdate = true;

            Owner.GetModPlayer<CosmicDischargePlayer>().QuickDrawCooldownTimer = 1800; // 30 seconds
            Owner.GetModPlayer<CosmicDischargePlayer>().AddUltimateEnergy(CosmicDischargePlayer.RightThrustEnergyGain);
            Owner.velocity += direction * 1.8f;
            Owner.SetImmuneTimeForAllTypes(8);
            ApplyScreenShake(5.8f);
            SoundEngine.PlaySound(SoundID.Item119 with { Volume = 0.64f, Pitch = 0.38f }, Owner.Center);

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Owner.MountedCenter,
                    direction * 1.8f,
                    CosmicDischargeCommon.FrostGlowColor * 0.48f,
                    Vector2.One,
                    direction.ToRotation(),
                    0.03f,
                    0.2f,
                    15));
            }
        }

        private void UpdateWhipArc(float side)
        {
            int snapEnd = WhipArcWindup + WhipArcSnap;
            int holdEnd = snapEnd + WhipArcHold;
            float sign = side * Math.Sign(Owner.direction == 0 ? 1 : Owner.direction);

            if (Time <= WhipArcWindup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            Vector2 baseDirection = AimDirection;
            if (Time <= WhipArcWindup)
            {
                float t = Time / WhipArcWindup;
                float prep = EaseOutCubic(t);
                SetBlade(baseDirection.RotatedBy(-sign * MathHelper.Lerp(0.82f, 0.24f, prep)), MathHelper.Lerp(76f, 178f, prep), -0.18f * sign, 24f);
            }
            else if (Time <= snapEnd)
            {
                float t = (Time - WhipArcWindup) / WhipArcSnap;
                float snap = EaseOutExpo(t);
                float over = MathF.Sin(MathHelper.Pi * t);
                SetBlade(baseDirection.RotatedBy(MathHelper.Lerp(-0.2f, 0.05f, snap) * sign), MathHelper.Lerp(178f, WhipReach + 30f, snap), -0.08f * sign, 38f + over * 8f);
                PlayReleaseOnce(SoundID.Item71, 0.86f, side < 0f ? -0.22f : 0.05f, 4.1f);

                if (!impactEffectsPlayed && t >= 0.7f)
                    EmitAirCrack(TipPosition, baseDirection, 0.8f);
            }
            else if (Time <= holdEnd)
            {
                SetBlade(baseDirection, WhipReach, 0f, 40f);
            }
            else
            {
                float t = Utils.GetLerpValue(holdEnd, WhipArcDuration, Time, true);
                currentlyRetracting = true;
                Projectile.localNPCHitCooldown = 4;
                float retract = EaseInCubic(t);
                SetBlade(baseDirection.RotatedBy(sign * MathHelper.Lerp(0.08f, 0.28f, retract)), MathHelper.Lerp(WhipReach, 64f, retract), 0.12f * sign, MathHelper.Lerp(30f, 18f, retract));
            }

            if (Time >= WhipArcDuration)
                Projectile.Kill();
        }

        private void UpdateThrust(bool quickDraw)
        {
            int duration = quickDraw ? QuickDrawDuration : WhipThrustDuration;
            int windup = quickDraw ? QuickDrawWindup : WhipThrustWindup;
            int snapFrames = quickDraw ? 5 : 8;
            int holdFrames = quickDraw ? 8 : 6;
            int snapEnd = windup + snapFrames;
            int holdEnd = snapEnd + holdFrames;
            float maxReach = quickDraw ? QuickDrawReach : ThrustReach;

            if (Time <= windup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            Vector2 direction = AimDirection;
            if (Time <= windup)
            {
                float t = quickDraw ? 1f : EaseOutCubic(Time / windup);
                SetBlade(direction.RotatedBy(-0.1f * Owner.direction), MathHelper.Lerp(58f, quickDraw ? 126f : 156f, t), -0.04f * Owner.direction, quickDraw ? 26f : 28f);
            }
            else if (Time <= snapEnd)
            {
                float t = (Time - windup) / snapFrames;
                float snap = EaseOutExpo(t);
                float over = MathF.Sin(MathHelper.Pi * t);
                SetBlade(direction, MathHelper.Lerp(126f, maxReach + (quickDraw ? 70f : 34f), snap), 0f, 42f + over * 10f);
                PlayReleaseOnce(SoundID.Item122, quickDraw ? 0.9f : 0.72f, quickDraw ? 0.28f : 0.06f, quickDraw ? 6.2f : 4.4f);

                if (!impactEffectsPlayed && t >= 0.72f)
                    EmitAirCrack(TipPosition, direction, quickDraw ? 1.35f : 0.92f);
            }
            else if (Time <= holdEnd)
            {
                SetBlade(direction, maxReach + (quickDraw ? 38f : 8f), 0f, quickDraw ? 48f : 40f);
                if (quickDraw)
                    SpawnQuickDrawBombs();
            }
            else
            {
                float t = Utils.GetLerpValue(holdEnd, duration, Time, true);
                currentlyRetracting = true;
                Projectile.localNPCHitCooldown = quickDraw ? 3 : 16;
                float retract = quickDraw ? EaseInExpo(t) : EaseInCubic(t);
                SetBlade(direction.RotatedBy(Owner.direction * MathHelper.Lerp(0f, 0.18f, retract)), MathHelper.Lerp(maxReach, 52f, retract), 0.08f * Owner.direction, MathHelper.Lerp(36f, 18f, retract));
                if (quickDraw)
                    SpawnQuickDrawBombs();
            }

            if (Time >= duration)
                Projectile.Kill();
        }

        private void UpdateSwordSwing(bool second)
        {
            if (Time <= SwordSwingWindup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            float swingSign = (second ? -1f : 1f) * Math.Sign(Owner.direction == 0 ? 1 : Owner.direction);
            int strikeEnd = SwordSwingWindup + 8;
            int holdEnd = strikeEnd + 3;

            if (Time <= SwordSwingWindup)
            {
                float prep = EaseOutCubic(Time / SwordSwingWindup);
                float angle = AimAngle + swingSign * MathHelper.Lerp(-1.42f, -0.86f, prep);
                SetBlade(angle.ToRotationVector2(), MathHelper.Lerp(112f, 194f, prep), second ? 0.12f : -0.12f, 28f);
            }
            else if (Time <= strikeEnd)
            {
                float t = (Time - SwordSwingWindup) / 8f;
                float strike = EaseOutCubic(t);
                float angle = AimAngle + MathHelper.Lerp(-0.86f * swingSign, 1.1f * swingSign, strike);
                SetBlade(angle.ToRotationVector2(), MathHelper.Lerp(205f, SwordReach + 30f, strike), second ? 0.12f : -0.12f, 38f);
                PlayReleaseOnce(SoundID.Item71, 0.82f, second ? 0.14f : -0.08f, 3.8f);

                if (!impactEffectsPlayed && t >= 0.58f)
                    EmitAirCrack(TipPosition, angle.ToRotationVector2(), 0.74f);
            }
            else if (Time <= holdEnd)
            {
                SetBlade((AimAngle + 1.14f * swingSign).ToRotationVector2(), SwordReach, 0f, 36f);
            }
            else
            {
                float t = Utils.GetLerpValue(holdEnd, SwordSwingDuration, Time, true);
                float recover = EaseInCubic(t);
                float angle = AimAngle + MathHelper.Lerp(1.14f * swingSign, 0.18f * swingSign, recover);
                SetBlade(angle.ToRotationVector2(), MathHelper.Lerp(SwordReach, 78f, recover), 0f, MathHelper.Lerp(30f, 18f, recover));
            }

            if (Time >= SwordSwingDuration)
                Projectile.Kill();
        }

        private void UpdateSwordFinisher()
        {
            if (Time <= SwordFinisherWindup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            Vector2 direction = AimDirection;
            if (Time <= SwordFinisherWindup)
            {
                float t = Time / SwordFinisherWindup;
                float spinAngle = AimAngle + Owner.direction * (MathHelper.TwoPi * 2f * t - MathHelper.PiOver2);
                float chargeBump = 0.5f + 0.5f * MathF.Sin(MathHelper.Pi * t);
                SetBlade(spinAngle.ToRotationVector2(), MathHelper.Lerp(128f, 228f, chargeBump), 0.05f * Owner.direction, 32f + chargeBump * 7f);

                if (Time == 1f)
                    SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.55f, Pitch = -0.12f }, Owner.Center);

                if (Time % 8f == 0f)
                    ApplyScreenShake(1.2f + t * 1.5f);

                SpawnSpinChargeDust(t);
                return;
            }

            int strikeFrames = 10;
            int strikeEnd = SwordFinisherSlamFrame + strikeFrames;

            if (Time <= SwordFinisherSlamFrame)
            {
                float lift = EaseOutCubic(Utils.GetLerpValue(SwordFinisherWindup, SwordFinisherSlamFrame, Time, true));
                float angle = AimAngle - Owner.direction * MathHelper.Lerp(1.38f, 1.05f, lift);
                SetBlade(angle.ToRotationVector2(), MathHelper.Lerp(220f, 304f, lift), 0.05f * Owner.direction, 38f);
            }
            else if (Time <= strikeEnd)
            {
                float t = (Time - SwordFinisherSlamFrame) / strikeFrames;
                float slam = EaseOutCubic(t);
                float angle = AimAngle + MathHelper.Lerp(-1.05f * Owner.direction, 1.22f * Owner.direction, slam);
                SetBlade(angle.ToRotationVector2(), FinisherReach + MathF.Sin(MathHelper.Pi * t) * 36f, 0f, 48f);

                if (!releaseSoundPlayed && t >= 0.12f)
                {
                    releaseSoundPlayed = true;
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.95f, Pitch = -0.35f }, Owner.Center);
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/HeavySwing") { Volume = 0.55f, Pitch = -0.18f }, Owner.Center);
                    ApplyScreenShake(7.2f);
                }

                if (!spawnedSwordWave && t >= 0.45f)
                {
                    spawnedSwordWave = true;
                    SpawnSwordWave(direction);
                    EmitAirCrack(TipPosition, direction, 1.35f);
                }
            }
            else
            {
                float recover = EaseInCubic(Utils.GetLerpValue(strikeEnd, SwordFinisherDuration, Time, true));
                float angle = AimAngle + MathHelper.Lerp(1.22f * Owner.direction, 0.16f * Owner.direction, recover);
                SetBlade(angle.ToRotationVector2(), MathHelper.Lerp(FinisherReach, 78f, recover), 0f, MathHelper.Lerp(38f, 18f, recover));
            }

            if (Time >= SwordFinisherDuration)
                Projectile.Kill();
        }

        private void SetBlade(Vector2 direction, float reach, float armRotationOffset, float collisionWidth)
        {
            direction = direction.SafeNormalize(Vector2.UnitX * Owner.direction);
            Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            Projectile.velocity = direction * Math.Max(12f, reach);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.spriteDirection = Owner.direction;
            currentCollisionWidth = collisionWidth;
            currentArmRotationOffset = armRotationOffset;

            CosmicDischargeCommon.HoldPlayer(Owner, Projectile, direction, armRotationOffset);
            Owner.itemRotation = (Projectile.velocity * Owner.direction).ToRotation();
        }

        private void HoldBladeStill()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(AimDirection);
            SetBlade(direction, Projectile.velocity.Length(), 0f, 0f);
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    TipPosition + Main.rand.NextVector2Circular(12f, 12f),
                    DustID.SnowflakeIce,
                    Main.rand.NextVector2Circular(1.8f, 1.8f),
                    100,
                    CosmicDischargeCommon.FrostWhiteColor,
                    Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }
        }

        private Vector2 TipPosition => Projectile.Center + Projectile.velocity;

        private void PlayReleaseOnce(SoundStyle sound, float volume, float pitch, float shake)
        {
            if (releaseSoundPlayed)
                return;

            releaseSoundPlayed = true;
            SoundEngine.PlaySound(sound with { Volume = volume, Pitch = pitch }, Owner.Center);
            ApplyScreenShake(shake);
        }

        private void EmitAirCrack(Vector2 center, Vector2 direction, float intensity)
        {
            impactEffectsPlayed = true;
            impactFlashTimer = 8;

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                direction * 0.7f,
                CosmicDischargeCommon.FrostGlowColor * (0.38f * intensity),
                Vector2.One,
                direction.ToRotation(),
                0.035f,
                0.22f * intensity,
                14));

            for (int i = 0; i < 6 + (int)(intensity * 8f); i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.72f) * Main.rand.NextFloat(3.2f, 9f) * intensity;
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    center + Main.rand.NextVector2Circular(24f, 18f),
                    velocity,
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.42f, 0.82f),
                    Main.rand.NextBool() ? CosmicDischargeCommon.FrostWhiteColor : CosmicDischargeCommon.FrostCoreColor));
            }
        }

        private void SpawnSwordWave(Vector2 direction)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.MountedCenter + direction * 78f,
                direction * 18f,
                ModContent.ProjectileType<CosmicDischargeSwordWave>(),
                (int)(Projectile.damage * 0.86f),
                Projectile.knockBack,
                Projectile.owner);
        }

        private void SpawnQuickDrawBombs()
        {
            int bombCount = CosmicDischargeProgression.QuickDrawIceBombCount;
            if (bombCount <= 0 || Main.myPlayer != Projectile.owner)
                return;

            if (spawnedBombBursts >= CosmicDischargeProgression.QuickDrawIceBombBursts)
                return;

            bool shouldBurst = Time == 10f || Time == 16f || Time == 23f;
            if (!shouldBurst)
                return;

            spawnedBombBursts++;
            Vector2 direction = Projectile.velocity.SafeNormalize(AimDirection);
            for (int i = 0; i < bombCount; i++)
            {
                float t = (i + 0.5f) / bombCount;
                Vector2 spawnPosition = Projectile.Center + direction * Projectile.velocity.Length() * t + Main.rand.NextVector2Circular(24f, 24f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    Main.rand.NextVector2Circular(1.8f, 1.8f),
                    ModContent.ProjectileType<CosmicDischargeIceBomb>(),
                    (int)(Projectile.damage * CosmicDischargeProgression.QuickDrawIceBombDamageFactor),
                    0f,
                    Projectile.owner,
                    Main.rand.NextFloat(12f, 24f));
            }
        }

        private void SpawnBladeWakeDust()
        {
            if (Main.dedServ || Projectile.velocity.LengthSquared() < 4f)
                return;

            int dustCount = impactFlashTimer > 0 ? 3 : 1;
            Vector2 direction = Projectile.velocity.SafeNormalize(AimDirection);
            for (int i = 0; i < dustCount; i++)
            {
                if (!Main.rand.NextBool(impactFlashTimer > 0 ? 1 : 2))
                    continue;

                Vector2 point = Projectile.Center + direction * Main.rand.NextFloat(32f, Projectile.velocity.Length());
                Dust dust = Dust.NewDustPerfect(
                    point + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextBool() ? DustID.Frost : DustID.SnowflakeIce,
                    direction.RotatedByRandom(0.65f) * Main.rand.NextFloat(0.35f, 2.4f),
                    120,
                    Main.rand.NextBool() ? CosmicDischargeCommon.FrostCoreColor : CosmicDischargeCommon.FrostWhiteColor,
                    Main.rand.NextFloat(0.85f, 1.32f));
                dust.noGravity = true;
            }
        }

        private void SpawnSpinChargeDust(float charge)
        {
            if (Main.dedServ || Main.rand.NextBool(2))
                return;

            Vector2 radius = Main.rand.NextVector2CircularEdge(58f + charge * 42f, 58f + charge * 42f);
            Dust dust = Dust.NewDustPerfect(
                Owner.MountedCenter + radius,
                DustID.SnowflakeIce,
                radius.RotatedBy(MathHelper.PiOver2 * Owner.direction).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.2f, 3.8f),
                110,
                CosmicDischargeCommon.FrostCoreColor,
                Main.rand.NextFloat(0.9f, 1.25f));
            dust.noGravity = true;
        }

        public override bool? CanDamage()
        {
            if (hitStopTimer > 0)
                return false;

            return Kind switch
            {
                CosmicDischargeAttackKind.WhipOver or CosmicDischargeAttackKind.WhipUnder
                    => Time >= WhipArcWindup + 2f && Time <= WhipArcDuration - 5f,
                CosmicDischargeAttackKind.WhipThrust
                    => Time >= WhipThrustWindup + 2f && Time <= WhipThrustDuration - 6f,
                CosmicDischargeAttackKind.SwordSwingOne or CosmicDischargeAttackKind.SwordSwingTwo
                    => Time >= SwordSwingWindup + 2f && Time <= SwordSwingDuration - 7f,
                CosmicDischargeAttackKind.SwordFinisher
                    => Time >= SwordFinisherSlamFrame + 1f && Time <= SwordFinisherSlamFrame + 16f,
                CosmicDischargeAttackKind.QuickDraw
                    => Time >= QuickDrawWindup + 2f && Time <= QuickDrawDuration - 4f,
                _ => false
            };
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(AimDirection);
            float reach = Projectile.velocity.Length();
            GetBendAndCurl(out float sideBend, out float curl);
            var points = CosmicDischargeCommon.BuildCurvedBlade(Owner, direction, reach, sideBend, curl, 18);
            return CosmicDischargeCommon.CheckCurveCollision(points, targetHitbox, currentCollisionWidth);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            bool tip = TargetNearTip(target, Kind == CosmicDischargeAttackKind.QuickDraw ? 58f : 46f);

            switch (Kind)
            {
                case CosmicDischargeAttackKind.WhipOver:
                case CosmicDischargeAttackKind.WhipUnder:
                    if (currentlyRetracting)
                    {
                        modifiers.FinalDamage *= 0.33f;
                        modifiers.Knockback *= 0.25f;
                    }
                    else
                    {
                        modifiers.FinalDamage *= 1.08f;
                        modifiers.Knockback *= 1.2f;
                    }
                    break;

                case CosmicDischargeAttackKind.WhipThrust:
                    modifiers.FinalDamage *= tip ? 2.75f : 0.72f;
                    modifiers.Knockback *= tip ? 1.6f : 0.55f;
                    break;

                case CosmicDischargeAttackKind.SwordSwingOne:
                case CosmicDischargeAttackKind.SwordSwingTwo:
                    modifiers.FinalDamage *= 1.12f;
                    modifiers.Knockback *= 1.25f;
                    break;

                case CosmicDischargeAttackKind.SwordFinisher:
                    modifiers.FinalDamage *= 1.9f;
                    modifiers.Knockback *= 1.75f;
                    break;

                case CosmicDischargeAttackKind.QuickDraw:
                    modifiers.FinalDamage *= tip ? 3.35f : 0.46f;
                    modifiers.Knockback *= tip ? 1.9f : 0.28f;
                    break;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            bool tip = TargetNearTip(target, Kind == CosmicDischargeAttackKind.QuickDraw ? 58f : 46f);
            bool heavy = tip || Kind == CosmicDischargeAttackKind.SwordFinisher || Kind == CosmicDischargeAttackKind.SwordSwingOne || Kind == CosmicDischargeAttackKind.SwordSwingTwo;

            CosmicDischargeCommon.ApplyColdDebuffs(target, Kind == CosmicDischargeAttackKind.QuickDraw ? 210 : 150);
            ApplyHitStop(heavy ? 5 : 3);
            ApplyScreenShake(heavy ? 8.4f : 4.6f);
            SpawnHitEffects(target, heavy, tip);

            if (Main.myPlayer == Projectile.owner && (tip || Kind == CosmicDischargeAttackKind.SwordFinisher))
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<CalamityMod.Projectiles.Melee.CosmicIceBurst>(),
                    (int)(Projectile.damage * (Kind == CosmicDischargeAttackKind.QuickDraw ? 0.64f : 0.44f)),
                    0f,
                    Projectile.owner,
                    0f,
                    heavy ? 1.12f : 0.88f);
            }
        }

        private bool TargetNearTip(NPC target, float radius)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(AimDirection);
            float reach = Projectile.velocity.Length();
            GetBendAndCurl(out float sideBend, out float curl);
            var points = CosmicDischargeCommon.BuildCurvedBlade(Owner, direction, reach, sideBend, curl, 18);
            return CosmicDischargeCommon.TargetIntersectsTip(points, target.Hitbox, radius);
        }

        private void ApplyHitStop(int frames)
        {
            hitStopTimer = Math.Max(hitStopTimer, frames);
            impactFlashTimer = Math.Max(impactFlashTimer, frames + 4);
        }

        private void SpawnHitEffects(NPC target, bool heavy, bool tip)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = (target.Center - Owner.MountedCenter).SafeNormalize(AimDirection);
            SoundEngine.PlaySound(new SoundStyle(heavy ? "CalamityMod/Sounds/Item/DemonSwordInsaneImpact" : "CalamityMod/Sounds/Item/LanceofDestinyStrong")
            {
                Volume = heavy ? 0.68f : 0.46f,
                Pitch = tip ? 0.12f : -0.08f,
                MaxInstances = 4
            }, target.Center);

            GeneralParticleHandler.SpawnParticle(new StrongBloom(target.Center, Vector2.Zero, CosmicDischargeCommon.FrostCoreColor * (heavy ? 0.55f : 0.34f), heavy ? 0.62f : 0.42f, heavy ? 22 : 16));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(target.Center, direction, CosmicDischargeCommon.FrostWhiteColor * (heavy ? 0.46f : 0.28f), Vector2.One, direction.ToRotation(), 0.04f, heavy ? 0.32f : 0.18f, heavy ? 18 : 12));

            int sparks = heavy ? 22 : 12;
            for (int i = 0; i < sparks; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.95f) * Main.rand.NextFloat(3f, heavy ? 14f : 8f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    target.Center + Main.rand.NextVector2Circular(18f, 18f),
                    velocity,
                    false,
                    Main.rand.Next(14, heavy ? 28 : 20),
                    Main.rand.NextFloat(0.35f, heavy ? 0.86f : 0.58f),
                    Main.rand.NextBool() ? CosmicDischargeCommon.FrostCoreColor : CosmicDischargeCommon.FrostWhiteColor));
            }
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1500f, 120f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero)
                return false;

            Vector2 direction = Projectile.velocity.SafeNormalize(AimDirection);
            float reach = Projectile.velocity.Length();
            GetBendAndCurl(out float sideBend, out float curl);
            var points = CosmicDischargeCommon.BuildCurvedBlade(Owner, direction, reach, sideBend, curl, 18);

            DrawCurvedBladeGlow(points);

            // Draw the curved chain segments
            CosmicDischargeCommon.DrawCurvedChain(Main.spriteBatch, points, lightColor, Projectile.scale, Owner.gfxOffY);

            // Draw the actual weapon hilt texture in player hand
            Texture2D itemTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/CosmicDischarge/CosmicDischarge").Value;
            SpriteEffects spriteEffects = SpriteEffects.None;
            float drawRotation;
            Vector2 origin;
            if (Owner.direction == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
                origin = new Vector2(itemTexture.Width, itemTexture.Height);
                drawRotation = direction.ToRotation() + 3f * MathHelper.PiOver4;
            }
            else
            {
                spriteEffects = SpriteEffects.None;
                origin = new Vector2(0f, itemTexture.Height);
                drawRotation = direction.ToRotation() + MathHelper.PiOver4;
            }

            Vector2 handPosition = Owner.MountedCenter + direction.RotatedBy(currentArmRotationOffset) * 12f;

            Main.EntitySpriteDraw(
                itemTexture,
                handPosition - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY,
                null,
                lightColor,
                drawRotation,
                origin,
                Projectile.scale,
                spriteEffects,
                0);

            if (CanBecomeQuickDraw)
                CosmicDischargeCommon.DrawRightHoldIndicator(Main.spriteBatch, Owner, 1f + 0.18f * MathF.Sin(Time * 0.45f));

            return false;
        }

        private void DrawCurvedBladeGlow(System.Collections.Generic.List<Vector2> points)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float flash = impactFlashTimer > 0 ? impactFlashTimer / 8f : 0f;
            Color outer = CosmicDischargeCommon.FrostDarkColor * (0.18f + flash * 0.12f);
            Color glow = CosmicDischargeCommon.FrostGlowColor * (0.26f + flash * 0.25f);
            Color core = CosmicDischargeCommon.FrostWhiteColor * (0.28f + flash * 0.4f);

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 start = points[i] - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
                Vector2 end = points[i + 1] - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
                Vector2 segment = end - start;
                if (segment.LengthSquared() < 0.1f)
                    continue;

                DrawLine(pixel, start, segment, outer, currentCollisionWidth * 1.4f);
                DrawLine(pixel, start, segment, glow, currentCollisionWidth * 0.72f);
                DrawLine(pixel, start, segment, core, MathHelper.Clamp(currentCollisionWidth * 0.16f, 2f, 5f));
            }

            if (points.Count > 0)
            {
                Main.EntitySpriteDraw(
                    bloom,
                    points[^1] - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY,
                    null,
                    CosmicDischargeCommon.FrostCoreColor * (0.22f + flash * 0.24f),
                    0f,
                    bloom.Size() * 0.5f,
                    (0.22f + flash * 0.08f) * Projectile.scale,
                    SpriteEffects.None);
            }
        }

        private static void DrawLine(Texture2D pixel, Vector2 start, Vector2 segment, Color color, float width)
        {
            Main.EntitySpriteDraw(
                pixel,
                start,
                new Rectangle(0, 0, 1, 1),
                color,
                segment.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(segment.Length(), width),
                SpriteEffects.None);
        }

        private void GetBendAndCurl(out float sideBend, out float curl)
        {
            sideBend = 0f;
            curl = 0f;

            Vector2 direction = Projectile.velocity.SafeNormalize(AimDirection);
            float reach = Projectile.velocity.Length();

            switch (Kind)
            {
                case CosmicDischargeAttackKind.WhipOver:
                    {
                        float sign = -1f * Math.Sign(Owner.direction == 0 ? 1 : Owner.direction);
                        int snapEnd = WhipArcWindup + WhipArcSnap;
                        int holdEnd = snapEnd + WhipArcHold;
                        if (Time <= WhipArcWindup)
                        {
                            float t = Time / WhipArcWindup;
                            sideBend = -120f * (1f - t) * sign;
                            curl = -60f * (1f - t);
                        }
                        else if (Time <= snapEnd)
                        {
                            float t = (Time - WhipArcWindup) / WhipArcSnap;
                            sideBend = MathHelper.Lerp(-40f, 60f, t) * sign;
                            curl = MathHelper.Lerp(-30f, 20f, t);
                        }
                        else if (Time <= holdEnd)
                        {
                            sideBend = 0f;
                            curl = 0f;
                        }
                        else
                        {
                            float t = Utils.GetLerpValue(holdEnd, WhipArcDuration, Time, true);
                            sideBend = MathHelper.Lerp(0f, -140f, t) * sign;
                            curl = MathHelper.Lerp(0f, -80f, t);
                        }
                    }
                    break;

                case CosmicDischargeAttackKind.WhipUnder:
                    {
                        float sign = 1f * Math.Sign(Owner.direction == 0 ? 1 : Owner.direction);
                        int snapEnd = WhipArcWindup + WhipArcSnap;
                        int holdEnd = snapEnd + WhipArcHold;
                        if (Time <= WhipArcWindup)
                        {
                            float t = Time / WhipArcWindup;
                            sideBend = 120f * (1f - t) * sign;
                            curl = 60f * (1f - t);
                        }
                        else if (Time <= snapEnd)
                        {
                            float t = (Time - WhipArcWindup) / WhipArcSnap;
                            sideBend = MathHelper.Lerp(40f, -60f, t) * sign;
                            curl = MathHelper.Lerp(30f, -20f, t);
                        }
                        else if (Time <= holdEnd)
                        {
                            sideBend = 0f;
                            curl = 0f;
                        }
                        else
                        {
                            float t = Utils.GetLerpValue(holdEnd, WhipArcDuration, Time, true);
                            sideBend = MathHelper.Lerp(0f, 140f, t) * sign;
                            curl = MathHelper.Lerp(0f, 80f, t);
                        }
                    }
                    break;

                case CosmicDischargeAttackKind.WhipThrust:
                case CosmicDischargeAttackKind.QuickDraw:
                    {
                        sideBend = 15f * MathF.Sin(Time * 0.8f) * Owner.direction;
                        curl = 10f * MathF.Cos(Time * 0.8f);
                    }
                    break;

                case CosmicDischargeAttackKind.SwordSwingOne:
                case CosmicDischargeAttackKind.SwordSwingTwo:
                    {
                        bool second = Kind == CosmicDischargeAttackKind.SwordSwingTwo;
                        float swingSign = (second ? -1f : 1f) * Math.Sign(Owner.direction == 0 ? 1 : Owner.direction);
                        int strikeEnd = SwordSwingWindup + 8;
                        int holdEnd = strikeEnd + 3;
                        if (Time <= SwordSwingWindup)
                        {
                            float t = Time / SwordSwingWindup;
                            sideBend = -15f * swingSign * t;
                        }
                        else if (Time <= strikeEnd)
                        {
                            float t = (Time - SwordSwingWindup) / 8f;
                            sideBend = MathHelper.Lerp(-15f, 25f, t) * swingSign;
                        }
                        else if (Time <= holdEnd)
                        {
                            sideBend = 0f;
                        }
                        else
                        {
                            float t = Utils.GetLerpValue(holdEnd, SwordSwingDuration, Time, true);
                            sideBend = MathHelper.Lerp(0f, -10f, t) * swingSign;
                        }
                    }
                    break;

                case CosmicDischargeAttackKind.SwordFinisher:
                    {
                        if (Time <= SwordFinisherWindup)
                        {
                            float t = Time / SwordFinisherWindup;
                            sideBend = 35f * MathF.Sin(t * MathHelper.Pi) * Owner.direction;
                        }
                        else
                        {
                            int strikeFrames = 10;
                            int strikeEnd = SwordFinisherSlamFrame + strikeFrames;
                            if (Time <= SwordFinisherSlamFrame)
                            {
                                float t = Utils.GetLerpValue(SwordFinisherWindup, SwordFinisherSlamFrame, Time, true);
                                sideBend = -20f * Owner.direction * t;
                            }
                            else if (Time <= strikeEnd)
                            {
                                float t = (Time - SwordFinisherSlamFrame) / strikeFrames;
                                sideBend = MathHelper.Lerp(-20f, 40f, t) * Owner.direction;
                            }
                            else
                            {
                                float t = Utils.GetLerpValue(strikeEnd, SwordFinisherDuration, Time, true);
                                sideBend = MathHelper.Lerp(40f, -15f, t) * Owner.direction;
                            }
                        }
                    }
                    break;
            }
        }

        private static float EaseOutCubic(float value)
        {
            value = MathHelper.Clamp(value, 0f, 1f);
            float inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }

        private static float EaseInCubic(float value)
        {
            value = MathHelper.Clamp(value, 0f, 1f);
            return value * value * value;
        }

        private static float EaseOutExpo(float value)
        {
            value = MathHelper.Clamp(value, 0f, 1f);
            return value >= 1f ? 1f : 1f - MathF.Pow(2f, -10f * value);
        }

        private static float EaseInExpo(float value)
        {
            value = MathHelper.Clamp(value, 0f, 1f);
            return value <= 0f ? 0f : MathF.Pow(2f, 10f * (value - 1f));
        }
    }
}
