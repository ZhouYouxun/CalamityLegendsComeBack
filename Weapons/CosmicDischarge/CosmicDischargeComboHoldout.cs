using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeComboHoldout : ModProjectile, ILocalizedModType
    {
        private const int WhipArcDuration = 50;
        private const int WhipArcWindup = 13;
        private const int WhipArcSnap = 8;
        private const int WhipArcHold = 5;
        private const int WhipThrustDuration = 56;
        private const int WhipThrustWindup = 14;
        private const int SwordSwingDuration = 38;
        private const int SwordSwingWindup = 10;
        private const int SwordFinisherDuration = 74;
        private const int SwordFinisherWindup = 34;
        private const int SwordFinisherSlamFrame = 43;
        private const int QuickDrawDuration = 41;
        private const int QuickDrawWindup = 4;

        private const float WhipReach = 510f;
        private const float ThrustReach = 540f;
        private const float QuickDrawReach = 610f;
        private const float SwordReach = 286f;
        private const float FinisherReach = 348f;

        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private readonly List<Vector2> bladePoints = new();
        private readonly List<Vector2> frozenBladePoints = new();
        private readonly List<Vector2> oldTips = new();
        private readonly HashSet<int> tipHitTargets = new();

        private bool wasRightHeld;
        private bool releaseSoundPlayed;
        private bool impactEffectsPlayed;
        private bool spawnedSwordWave;
        private int spawnedBombBursts;
        private int hitStopTimer;
        private int impactFlashTimer;
        private float currentCollisionWidth = 30f;
        private float currentReachRatio;

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
            (Kind == CosmicDischargeAttackKind.WhipThrust && Time <= WhipThrustWindup) ||
            (Kind == CosmicDischargeAttackKind.SwordFinisher && Time <= SwordFinisherWindup);

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 9;
            Projectile.ownerHitCheck = true;
            Projectile.coldDamage = true;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = 2;
            AimAngle = Projectile.ai[1];
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
                currentCollisionWidth = 0f;
                HoldStillDuringHitstop();
                return;
            }

            Time++;
            if (impactFlashTimer > 0)
                impactFlashTimer--;

            Projectile.localNPCHitCooldown = Kind == CosmicDischargeAttackKind.QuickDraw ? 3 : IsRetracting() ? 16 : 9;

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

            if (bladePoints.Count > 1)
            {
                Projectile.Center = bladePoints[^1];
                Projectile.rotation = (bladePoints[^1] - bladePoints[^2]).ToRotation() + MathHelper.PiOver2;
                RecordTip();
            }

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
            tipHitTargets.Clear();
            oldTips.Clear();
            spawnedBombBursts = 0;
            spawnedSwordWave = false;
            releaseSoundPlayed = false;
            impactEffectsPlayed = false;
            Projectile.localNPCHitCooldown = 3;
            Projectile.netUpdate = true;

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

        private void HoldStillDuringHitstop()
        {
            bladePoints.Clear();
            bladePoints.AddRange(frozenBladePoints);
            if (bladePoints.Count < 2)
                return;

            Vector2 direction = (bladePoints[^1] - Owner.MountedCenter).SafeNormalize(AimDirection);
            CosmicDischargeCommon.HoldPlayer(Owner, Projectile, direction, 0.08f * Owner.direction);
            Projectile.Center = bladePoints[^1];
            Projectile.rotation = (bladePoints[^1] - bladePoints[^2]).ToRotation() + MathHelper.PiOver2;

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    bladePoints[^1] + Main.rand.NextVector2Circular(12f, 12f),
                    DustID.SnowflakeIce,
                    Main.rand.NextVector2Circular(1.8f, 1.8f),
                    100,
                    CosmicDischargeCommon.FrostWhiteColor,
                    Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }
        }

        private void UpdateWhipArc(float side)
        {
            int snapEnd = WhipArcWindup + WhipArcSnap;
            int holdEnd = snapEnd + WhipArcHold;
            float sign = side * Math.Sign(Owner.direction == 0 ? 1 : Owner.direction);

            if (Time <= WhipArcWindup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            Vector2 direction = AimDirection;
            CosmicDischargeCommon.HoldPlayer(Owner, Projectile, direction, -0.18f * sign);

            if (Time <= WhipArcWindup)
            {
                float t = Time / WhipArcWindup;
                float coil = EaseOutCubic(t);
                BuildBlade(direction.RotatedBy(-sign * MathHelper.Lerp(0.42f, 0.12f, coil)), MathHelper.Lerp(70f, 168f, coil), sign * MathHelper.Lerp(230f, 310f, coil), sign * 170f, 0.74f);
            }
            else if (Time <= snapEnd)
            {
                float t = (Time - WhipArcWindup) / WhipArcSnap;
                float snap = EaseOutBack(t);
                float over = MathF.Sin(MathHelper.Pi * t);
                float angleOffset = MathHelper.Lerp(-0.18f * sign, 0.06f * sign, snap);
                BuildBlade(direction.RotatedBy(angleOffset), MathHelper.Lerp(168f, WhipReach + 42f, snap), sign * MathHelper.Lerp(265f, -54f, snap), sign * MathHelper.Lerp(150f, 20f, snap), 1.05f + over * 0.12f, 24);
                PlayReleaseOnce(SoundID.Item71, 0.86f, side < 0f ? -0.22f : 0.05f, 4.1f);

                if (!impactEffectsPlayed && t >= 0.7f)
                    EmitAirCrack(bladePoints.Count > 0 ? bladePoints[^1] : Owner.MountedCenter, direction, 0.8f);
            }
            else if (Time <= holdEnd)
            {
                float pulse = 1f + 0.05f * MathF.Sin(Time * 1.7f);
                BuildBlade(direction, WhipReach * pulse, -sign * 40f, sign * 12f, 1.12f, 24);
            }
            else
            {
                float t = Utils.GetLerpValue(holdEnd, WhipArcDuration, Time, true);
                float retract = EaseInCubic(t);
                BuildBlade(direction.RotatedBy(sign * MathHelper.Lerp(0.08f, 0.32f, retract)), MathHelper.Lerp(WhipReach, 62f, retract), sign * MathHelper.Lerp(-42f, 120f, retract), sign * MathHelper.Lerp(14f, -70f, retract), MathHelper.Lerp(0.92f, 0.5f, retract));
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
            CosmicDischargeCommon.HoldPlayer(Owner, Projectile, direction);

            if (Time <= windup)
            {
                float t = Time / windup;
                float coil = quickDraw ? 1f : EaseOutCubic(t);
                float side = Owner.direction * (quickDraw ? 34f : 138f);
                BuildBlade(direction.RotatedBy(-0.1f * Owner.direction), MathHelper.Lerp(58f, quickDraw ? 128f : 155f, coil), side, Owner.direction * 80f, quickDraw ? 0.75f : 0.85f, 20);
            }
            else if (Time <= snapEnd)
            {
                float t = (Time - windup) / snapFrames;
                float snap = EaseOutExpo(t);
                float over = MathF.Sin(MathHelper.Pi * t);
                BuildBlade(direction, MathHelper.Lerp(128f, maxReach + (quickDraw ? 70f : 36f), snap), Owner.direction * MathHelper.Lerp(42f, -12f, snap), Owner.direction * MathHelper.Lerp(42f, 0f, snap), 1.08f + over * 0.16f, quickDraw ? 26 : 23);
                PlayReleaseOnce(SoundID.Item122, quickDraw ? 0.9f : 0.72f, quickDraw ? 0.28f : 0.06f, quickDraw ? 6.2f : 4.4f);

                if (!impactEffectsPlayed && t >= 0.72f)
                    EmitAirCrack(bladePoints.Count > 0 ? bladePoints[^1] : Owner.MountedCenter, direction, quickDraw ? 1.35f : 0.92f);
            }
            else if (Time <= holdEnd)
            {
                BuildBlade(direction, maxReach + (quickDraw ? 38f : 8f), Owner.direction * 6f * MathF.Sin(Time * 0.7f), 0f, quickDraw ? 1.16f : 1.05f, quickDraw ? 26 : 23);
                if (quickDraw)
                    SpawnQuickDrawBombs();
            }
            else
            {
                float t = Utils.GetLerpValue(holdEnd, duration, Time, true);
                float retract = quickDraw ? EaseInExpo(t) : EaseInCubic(t);
                BuildBlade(direction.RotatedBy(Owner.direction * MathHelper.Lerp(0f, 0.22f, retract)), MathHelper.Lerp(maxReach, 48f, retract), Owner.direction * MathHelper.Lerp(8f, -94f, retract), Owner.direction * MathHelper.Lerp(0f, -58f, retract), MathHelper.Lerp(1f, 0.45f, retract), quickDraw ? 22 : 18);
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

            Vector2 baseDirection = AimDirection;
            CosmicDischargeCommon.HoldPlayer(Owner, Projectile, baseDirection, second ? 0.12f : -0.12f);

            float swingSign = (second ? -1f : 1f) * Math.Sign(Owner.direction == 0 ? 1 : Owner.direction);
            int strikeEnd = SwordSwingWindup + 8;
            int holdEnd = strikeEnd + 3;

            if (Time <= SwordSwingWindup)
            {
                float t = Time / SwordSwingWindup;
                float prep = EaseOutCubic(t);
                float angle = AimAngle + swingSign * MathHelper.Lerp(-1.48f, -0.88f, prep);
                BuildBlade(angle.ToRotationVector2(), MathHelper.Lerp(112f, 190f, prep), -swingSign * 58f, -swingSign * 34f, 0.82f, 15);
            }
            else if (Time <= strikeEnd)
            {
                float t = (Time - SwordSwingWindup) / 8f;
                float strike = EaseOutBack(t);
                float angle = AimAngle + MathHelper.Lerp(-0.88f * swingSign, 1.12f * swingSign, strike);
                float reach = MathHelper.Lerp(205f, SwordReach + 30f, strike);
                BuildBlade(angle.ToRotationVector2(), reach, swingSign * MathHelper.Lerp(42f, -18f, strike), 0f, 1.12f, 16);
                PlayReleaseOnce(SoundID.Item71, 0.82f, second ? 0.14f : -0.08f, 3.8f);

                if (!impactEffectsPlayed && t >= 0.58f)
                    EmitAirCrack(bladePoints.Count > 0 ? bladePoints[^1] : Owner.MountedCenter, angle.ToRotationVector2(), 0.74f);
            }
            else if (Time <= holdEnd)
            {
                float angle = AimAngle + 1.2f * swingSign;
                BuildBlade(angle.ToRotationVector2(), SwordReach, -swingSign * 18f, 0f, 1.04f, 16);
            }
            else
            {
                float t = Utils.GetLerpValue(holdEnd, SwordSwingDuration, Time, true);
                float recover = EaseInCubic(t);
                float angle = AimAngle + MathHelper.Lerp(1.2f * swingSign, 0.22f * swingSign, recover);
                BuildBlade(angle.ToRotationVector2(), MathHelper.Lerp(SwordReach, 82f, recover), -swingSign * MathHelper.Lerp(18f, 66f, recover), swingSign * -28f, MathHelper.Lerp(0.92f, 0.52f, recover), 13);
            }

            if (Time >= SwordSwingDuration)
                Projectile.Kill();
        }

        private void UpdateSwordFinisher()
        {
            if (Time <= SwordFinisherWindup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            Vector2 direction = AimDirection;
            CosmicDischargeCommon.HoldPlayer(Owner, Projectile, direction, 0.05f * Owner.direction);

            if (Time <= SwordFinisherWindup)
            {
                float t = Time / SwordFinisherWindup;
                float spinAngle = AimAngle + Owner.direction * (MathHelper.TwoPi * 2f * t - MathHelper.PiOver2);
                float chargeBump = 0.5f + 0.5f * MathF.Sin(MathHelper.Pi * t);
                BuildBlade(spinAngle.ToRotationVector2(), MathHelper.Lerp(130f, 224f, chargeBump), Owner.direction * MathHelper.Lerp(40f, 16f, t), Owner.direction * 12f, 0.95f + chargeBump * 0.12f, 16);

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
                float t = Utils.GetLerpValue(SwordFinisherWindup, SwordFinisherSlamFrame, Time, true);
                float lift = EaseOutCubic(t);
                float angle = AimAngle - Owner.direction * MathHelper.Lerp(1.4f, 1.08f, lift);
                BuildBlade(angle.ToRotationVector2(), MathHelper.Lerp(220f, 300f, lift), Owner.direction * MathHelper.Lerp(42f, 24f, lift), 0f, 1.08f, 18);
            }
            else if (Time <= strikeEnd)
            {
                float t = (Time - SwordFinisherSlamFrame) / strikeFrames;
                float slam = EaseOutBack(t);
                float angle = AimAngle + MathHelper.Lerp(-1.08f * Owner.direction, 1.22f * Owner.direction, slam);
                BuildBlade(angle.ToRotationVector2(), FinisherReach + MathF.Sin(MathHelper.Pi * t) * 36f, Owner.direction * MathHelper.Lerp(26f, -34f, slam), 0f, 1.22f, 20);

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
                    EmitAirCrack(bladePoints.Count > 0 ? bladePoints[^1] : Owner.MountedCenter, direction, 1.35f);
                }
            }
            else
            {
                float t = Utils.GetLerpValue(strikeEnd, SwordFinisherDuration, Time, true);
                float recover = EaseInCubic(t);
                float angle = AimAngle + MathHelper.Lerp(1.22f * Owner.direction, 0.16f * Owner.direction, recover);
                BuildBlade(angle.ToRotationVector2(), MathHelper.Lerp(FinisherReach, 78f, recover), Owner.direction * MathHelper.Lerp(-34f, 78f, recover), Owner.direction * -32f, MathHelper.Lerp(1f, 0.46f, recover), 16);
            }

            if (Time >= SwordFinisherDuration)
                Projectile.Kill();
        }

        private void BuildBlade(Vector2 direction, float reach, float bend, float curl, float scale, int points = 18)
        {
            currentReachRatio = MathHelper.Clamp(reach / QuickDrawReach, 0f, 1.25f);
            currentCollisionWidth = MathHelper.Lerp(26f, 48f, MathHelper.Clamp((scale - 0.65f) / 0.55f, 0f, 1f));
            bladePoints.Clear();
            bladePoints.AddRange(CosmicDischargeCommon.BuildCurvedBlade(Owner, direction.SafeNormalize(Vector2.UnitX * Owner.direction), reach, bend, curl, points));
        }

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
            if (bombCount <= 0 || Main.myPlayer != Projectile.owner || bladePoints.Count < 4)
                return;

            if (spawnedBombBursts >= CosmicDischargeProgression.QuickDrawIceBombBursts)
                return;

            bool shouldBurst = Time == 10f || Time == 16f || Time == 23f;
            if (!shouldBurst)
                return;

            spawnedBombBursts++;
            for (int i = 0; i < bombCount; i++)
            {
                float t = (i + 0.5f) / bombCount;
                int pointIndex = (int)MathHelper.Clamp(t * (bladePoints.Count - 1), 1, bladePoints.Count - 1);
                Vector2 spawnPosition = bladePoints[pointIndex] + Main.rand.NextVector2Circular(26f, 26f);

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
            if (Main.dedServ || bladePoints.Count < 3)
                return;

            int dustCount = impactFlashTimer > 0 ? 3 : 1;
            for (int i = 0; i < dustCount; i++)
            {
                if (!Main.rand.NextBool(impactFlashTimer > 0 ? 1 : 2))
                    continue;

                Vector2 point = bladePoints[Main.rand.Next(1, bladePoints.Count)];
                Vector2 direction = (point - Owner.MountedCenter).SafeNormalize(AimDirection);
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

        private void RecordTip()
        {
            oldTips.Insert(0, bladePoints[^1]);
            if (oldTips.Count > 12)
                oldTips.RemoveAt(oldTips.Count - 1);
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
            return CosmicDischargeCommon.CheckCurveCollision(bladePoints, targetHitbox, currentCollisionWidth);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            bool tip = CosmicDischargeCommon.TargetIntersectsTip(bladePoints, target.Hitbox, Kind == CosmicDischargeAttackKind.QuickDraw ? 58f : 46f);

            switch (Kind)
            {
                case CosmicDischargeAttackKind.WhipOver:
                case CosmicDischargeAttackKind.WhipUnder:
                    if (IsRetracting())
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
                    modifiers.FinalDamage *= tip && !tipHitTargets.Contains(target.whoAmI) ? 2.75f : 0.72f;
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
                    modifiers.FinalDamage *= tip && !tipHitTargets.Contains(target.whoAmI) ? 3.35f : 0.46f;
                    modifiers.Knockback *= tip ? 1.9f : 0.28f;
                    break;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            bool tip = CosmicDischargeCommon.TargetIntersectsTip(bladePoints, target.Hitbox, Kind == CosmicDischargeAttackKind.QuickDraw ? 58f : 46f);
            bool heavy = tip || Kind == CosmicDischargeAttackKind.SwordFinisher || Kind == CosmicDischargeAttackKind.SwordSwingOne || Kind == CosmicDischargeAttackKind.SwordSwingTwo;

            CosmicDischargeCommon.ApplyColdDebuffs(target, Kind == CosmicDischargeAttackKind.QuickDraw ? 210 : 150);
            if (tip)
                tipHitTargets.Add(target.whoAmI);

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

        private void ApplyHitStop(int frames)
        {
            hitStopTimer = Math.Max(hitStopTimer, frames);
            frozenBladePoints.Clear();
            frozenBladePoints.AddRange(bladePoints);
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

            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                target.Center,
                Vector2.Zero,
                CosmicDischargeCommon.FrostCoreColor * (heavy ? 0.55f : 0.34f),
                heavy ? 0.62f : 0.42f,
                heavy ? 22 : 16));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                target.Center,
                direction,
                CosmicDischargeCommon.FrostWhiteColor * (heavy ? 0.46f : 0.28f),
                Vector2.One,
                direction.ToRotation(),
                0.04f,
                heavy ? 0.32f : 0.18f,
                heavy ? 18 : 12));

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

        private bool IsRetracting()
        {
            return Kind switch
            {
                CosmicDischargeAttackKind.WhipOver or CosmicDischargeAttackKind.WhipUnder => Time > WhipArcWindup + WhipArcSnap + WhipArcHold,
                CosmicDischargeAttackKind.WhipThrust => Time > WhipThrustWindup + 14f,
                CosmicDischargeAttackKind.QuickDraw => Time > QuickDrawWindup + 13f,
                _ => false
            };
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
            if (bladePoints.Count < 2)
                return false;

            DrawTipAfterimages();
            DrawBladeSmear();

            Color chainColor = Color.Lerp(CosmicDischargeCommon.FrostDarkColor, CosmicDischargeCommon.FrostCoreColor, 0.74f);
            float chainScale = MathHelper.Lerp(0.92f, 1.12f, MathHelper.Clamp(currentReachRatio, 0f, 1f));
            CosmicDischargeCommon.DrawCurvedChain(Main.spriteBatch, bladePoints, chainColor, chainScale, Owner.gfxOffY);

            if (CanBecomeQuickDraw)
                CosmicDischargeCommon.DrawRightHoldIndicator(Main.spriteBatch, Owner, 1f + 0.18f * MathF.Sin(Time * 0.45f));

            return false;
        }

        private void DrawBladeSmear()
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float flash = impactFlashTimer > 0 ? impactFlashTimer / 8f : 0f;

            for (int i = 0; i < bladePoints.Count - 1; i++)
            {
                Vector2 start = bladePoints[i] - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
                Vector2 end = bladePoints[i + 1] - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
                Vector2 segment = end - start;
                float length = segment.Length();
                if (length < 2f)
                    continue;

                float along = i / (float)(bladePoints.Count - 1);
                float width = MathHelper.Lerp(10f, 28f, along) * MathHelper.Lerp(0.75f, 1.25f, currentReachRatio);
                Color outer = CosmicDischargeCommon.FrostDarkColor * (0.18f + flash * 0.12f) * along;
                Color glow = CosmicDischargeCommon.FrostGlowColor * (0.25f + flash * 0.25f) * along;
                Color core = CosmicDischargeCommon.FrostWhiteColor * (0.35f + flash * 0.4f) * along;
                DrawLine(pixel, start, segment, outer, width * 1.55f);
                DrawLine(pixel, start, segment, glow, width * 0.82f);
                DrawLine(pixel, start, segment, core, MathHelper.Clamp(width * 0.22f, 2f, 5f));
            }
        }

        private void DrawTipAfterimages()
        {
            if (oldTips.Count < 2)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = bloom.Size() * 0.5f;
            for (int i = oldTips.Count - 1; i >= 0; i--)
            {
                float completion = 1f - i / (float)oldTips.Count;
                Color color = Color.Lerp(CosmicDischargeCommon.FrostDarkColor, CosmicDischargeCommon.FrostCoreColor, completion) * (0.12f + completion * 0.18f);
                Main.EntitySpriteDraw(
                    bloom,
                    oldTips[i] - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY,
                    null,
                    color,
                    0f,
                    origin,
                    MathHelper.Lerp(0.12f, 0.34f, completion),
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

        private static float EaseOutBack(float value)
        {
            value = MathHelper.Clamp(value, 0f, 1f);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float shifted = value - 1f;
            return 1f + c3 * shifted * shifted * shifted + c1 * shifted * shifted;
        }
    }
}
