using CalamityMod;
using CalamityMod.Particles;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    public class BrinyBaron_SkillSuperCharge_SuperDash : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron";

        private const int ChargeTime = 90;
        private const int DashTime = 90;
        private const float DashSpeed = 27.5f;
        private const float ChargeAimTurnRate = 0.13f;
        private const float ReadyAimTurnRate = 0.24f;
        private const float DashTurnRate = MathHelper.Pi / 180f;
        private const float HoldDistanceCharge = 20f;
        private const float HoldDistanceReady = 24f;
        private const float HoldDistanceDash = 24f;
        private const int SupportStarMinInterval = 2;
        private const int SupportStarMaxInterval = 6;
        private const float SupportStarDashDamageFactor = 0.28f;
        private const float SupportStarImpactDamageFactor = 0.42f;
        private const float ImpactSlashDamageFactor = 0.34f;
        private const float ImpactSlashScale = 1.35f;
        private const float GoldenAngle = 2.39996323f;

        private Player Owner => Main.player[Projectile.owner];
        private Vector2 AimDirection => lockedDirection.SafeNormalize(DefaultDirection(Owner));
        private Vector2 BladeDirection => (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2();
        private Vector2 WeaponTip => Projectile.Center + BladeDirection * 52f;

        private int timer;
        private int dashTimer;
        private bool initialized;
        private bool chargeReady;
        private bool isDashing;
        private int hitFeedbackCooldown;
        private int supportStarCooldown;
        private Vector2 lockedDirection = Vector2.UnitX;

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3600;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => isDashing ? null : false;

        public override void AI()
        {
            Player owner = Owner;
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!initialized)
                Initialize(owner);

            ReadSyncedDirection(owner);

            if (hitFeedbackCooldown > 0)
                hitFeedbackCooldown--;

            if (!isDashing)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.friendly = false;

                if (!chargeReady)
                {
                    timer++;
                    UpdateChargeAim(owner);
                    UpdateHeldBlade(owner, false, false);

                    owner.velocity *= 0.82f;
                    SpawnChargeFunnelEffects(owner);
                    Lighting.AddLight(WeaponTip, 0.08f, 0.28f, 0.38f);

                    if (timer >= ChargeTime)
                        EnterReadyState(owner);
                }
                else
                {
                    UpdateReadyAim(owner);
                    UpdateHeldBlade(owner, false, true);

                    owner.velocity *= 0.88f;
                    SpawnReadyHoldEffects();
                    Lighting.AddLight(WeaponTip, 0.1f, 0.32f, 0.44f);
                    TryStartDashFromLeftClick(owner);
                }
            }
            else
            {
                dashTimer++;
                UpdateDashTurning(owner);
                UpdateHeldBlade(owner, true, false);

                Projectile.friendly = true;
                Projectile.velocity = AimDirection * DashSpeed;
                owner.velocity = Projectile.velocity;

                SpawnDashWakeEffects(owner);
                TrySpawnDashSupportStars(owner);
                Lighting.AddLight(WeaponTip, 0.12f, 0.34f, 0.5f);

                if (dashTimer >= DashTime)
                    Projectile.Kill();
            }
        }

        private void Initialize(Player owner)
        {
            lockedDirection = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(DefaultDirection(owner));
            SyncLockedDirection();

            Projectile.rotation = lockedDirection.ToRotation() + MathHelper.PiOver4;
            Projectile.Center = owner.RotatedRelativePoint(owner.MountedCenter, true) + lockedDirection * HoldDistanceCharge + new Vector2(0f, 6f);
            Projectile.scale = 1f;

            timer = 0;
            dashTimer = 0;
            hitFeedbackCooldown = 0;
            supportStarCooldown = 0;
            chargeReady = false;
            isDashing = false;
            initialized = true;
        }

        private Vector2 DefaultDirection(Player owner)
        {
            int direction = owner.direction == 0 ? 1 : owner.direction;
            return Vector2.UnitX * direction;
        }

        private int DashTimer => dashTimer;

        private void ReadSyncedDirection(Player owner)
        {
            if (Main.myPlayer == Projectile.owner)
                return;

            Vector2 syncedDirection = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            if (syncedDirection.LengthSquared() > 0.01f)
                lockedDirection = syncedDirection.SafeNormalize(DefaultDirection(owner));
        }

        private void SyncLockedDirection()
        {
            Projectile.ai[0] = lockedDirection.X;
            Projectile.ai[1] = lockedDirection.Y;
            Projectile.netUpdate = true;
        }

        private void RotateToward(Vector2 targetDirection, float maxTurn)
        {
            float currentAngle = AimDirection.ToRotation();
            float targetAngle = targetDirection.ToRotation();
            Vector2 newDirection = currentAngle.AngleTowards(targetAngle, maxTurn).ToRotationVector2();

            if (Vector2.DistanceSquared(newDirection, lockedDirection) > 0.0001f)
            {
                lockedDirection = newDirection;
                SyncLockedDirection();
            }
        }

        private void UpdateChargeAim(Player owner)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 targetDirection = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(AimDirection);
            RotateToward(targetDirection, ChargeAimTurnRate);
        }

        private void UpdateDashTurning(Player owner)
        {
            if (Main.myPlayer != Projectile.owner || DashTimer % 2 != 0)
                return;

            Vector2 targetDirection = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(AimDirection);
            RotateToward(targetDirection, DashTurnRate);
        }

        private void UpdateReadyAim(Player owner)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 targetDirection = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(AimDirection);
            RotateToward(targetDirection, ReadyAimTurnRate);
        }

        private void UpdateHeldBlade(Player owner, bool dashing, bool readyState)
        {
            Vector2 armPosition = owner.RotatedRelativePoint(owner.MountedCenter, true);
            Vector2 forward = AimDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            float chargeProgress = chargeReady ? 1f : Utils.GetLerpValue(0f, ChargeTime, timer, true);
            float swayA = (float)Math.Sin(timer * 0.34f + Projectile.identity * 0.11f);
            float swayB = (float)Math.Cos(timer * 0.57f + Projectile.identity * 0.08f);
            float swayAngle = dashing ? 0f : readyState ? 0f : MathHelper.ToRadians((1.25f + chargeProgress * 3.75f) * swayA);
            float sideSway = dashing ? 0f : readyState ? 0f : swayA * (4f + chargeProgress * 8f);
            float depthSway = dashing ? 0f : readyState ? 0f : swayB * (2f + chargeProgress * 4.5f);
            float holdDistance = readyState
                ? HoldDistanceReady
                : (dashing ? HoldDistanceDash : HoldDistanceCharge) + depthSway + chargeProgress * (dashing ? 0f : 8f);

            Projectile.Center = armPosition + forward * holdDistance + right * sideSway + new Vector2(0f, 6f);
            if (!dashing && !readyState && chargeProgress > 0.72f)
                Projectile.Center += Main.rand.NextVector2Circular(1.5f, 1.5f) * chargeProgress;

            Projectile.rotation = forward.ToRotation() + MathHelper.PiOver4 + swayAngle;
            Projectile.scale = readyState
                ? 1.08f
                : dashing
                ? 1.08f
                : 1f + chargeProgress * 0.12f + 0.02f * swayB;

            Projectile.direction = forward.X >= 0f ? 1 : -1;
            owner.ChangeDir(Projectile.direction);
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
            owner.itemRotation = (forward * owner.direction).ToRotation();

            float armRotation = forward.ToRotation() - MathHelper.PiOver2 + swayAngle * 0.35f;
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
        }

        private void EnterReadyState(Player owner)
        {
            chargeReady = true;
            Projectile.friendly = false;
            Projectile.velocity = Vector2.Zero;
            owner.velocity *= 0.7f;

            SoundEngine.PlaySound(SoundID.Item29 with
            {
                Volume = 0.85f,
                Pitch = -0.05f
            }, WeaponTip);

            SpawnChargeReadyBurst();
        }

        private void TryStartDashFromLeftClick(Player owner)
        {
            if (Main.myPlayer != Projectile.owner || !Main.mouseLeft || !Main.mouseLeftRelease || owner.mouseInterface || Main.blockMouse || Main.mapFullscreen)
                return;

            StartDash(owner);
        }

        private void StartDash(Player owner)
        {
            chargeReady = false;
            isDashing = true;
            dashTimer = 0;
            Projectile.friendly = true;
            Projectile.velocity = AimDirection * DashSpeed;
            owner.velocity = Projectile.velocity;

            SoundEngine.PlaySound(SoundID.Item74 with
            {
                Volume = 1.2f,
                Pitch = -0.22f
            }, WeaponTip);

            SoundEngine.PlaySound(SoundID.Splash with
            {
                Volume = 0.75f,
                Pitch = -0.12f
            }, Projectile.Center);

            ResetSupportStarCooldown();
            SpawnDashStartBurst();
        }

        private void SpawnChargeFunnelEffects(Player owner)
        {
            if (Main.dedServ)
                return;

            float chargeProgress = Utils.GetLerpValue(0f, ChargeTime, timer, true);
            Vector2 ownerCenter = owner.RotatedRelativePoint(owner.MountedCenter, true);
            Vector2 forward = AimDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 tip = WeaponTip;
            Vector2 lowerFocus = owner.Bottom + Vector2.UnitY * MathHelper.Lerp(26f, 48f, chargeProgress);

            float pulseA = 0.5f + 0.5f * (float)Math.Sin(timer * 0.31f);
            float pulseB = 0.5f + 0.5f * (float)Math.Cos(timer * 0.17f + 1.2f);
            float pulseC = 0.5f + 0.5f * (float)Math.Sin(timer * 0.46f + pulseB * MathHelper.TwoPi);

            int funnelLanes = 5 + (chargeProgress > 0.55f ? 2 : 0);
            for (int lane = 0; lane < funnelLanes; lane++)
            {
                float laneRatio = lane / (float)Math.Max(1, funnelLanes - 1);
                float side = MathHelper.Lerp(-1f, 1f, laneRatio);
                float spiral = timer * 0.22f + lane * 0.74f + Projectile.identity * 0.17f;
                float verticalDepth = MathHelper.Lerp(18f, 92f, 0.35f + 0.65f * pulseB);
                float horizontalSpread = MathHelper.Lerp(26f, 92f, chargeProgress) * (0.45f + 0.55f * (0.5f + 0.5f * (float)Math.Sin(spiral)));

                Vector2 spawnPosition =
                    lowerFocus +
                    right * side * horizontalSpread +
                    Vector2.UnitY * Main.rand.NextFloat(-6f, 14f) -
                    forward * Main.rand.NextFloat(8f, 24f) +
                    right * (float)Math.Sin(spiral * 1.5f) * 18f +
                    Main.rand.NextVector2Circular(4f, 4f);

                Vector2 toTip = tip - spawnPosition;
                Vector2 inward = toTip.SafeNormalize(forward);
                Vector2 curl = inward.RotatedBy(MathHelper.PiOver2 * (side >= 0f ? 1f : -1f));
                Vector2 flowVelocity =
                    inward * MathHelper.Lerp(4.6f, 13.2f, chargeProgress) +
                    curl * MathHelper.Lerp(2f, 6.8f, chargeProgress) * (0.42f + 0.58f * pulseA) +
                    -Vector2.UnitY * MathHelper.Lerp(0.4f, 2.8f, chargeProgress) +
                    owner.velocity * 0.08f;

                Dust water = Dust.NewDustPerfect(
                    spawnPosition,
                    DustID.Water,
                    flowVelocity,
                    100,
                    Color.Lerp(new Color(60, 160, 255), new Color(150, 235, 255), pulseA),
                    Main.rand.NextFloat(1.05f, 1.45f));
                water.noGravity = true;
                water.fadeIn = 1.1f;

                Dust frost = Dust.NewDustPerfect(
                    spawnPosition - inward * 4f,
                    DustID.Frost,
                    flowVelocity * 0.72f,
                    100,
                    Color.Lerp(new Color(180, 235, 255), Color.White, 0.35f + 0.35f * pulseC),
                    Main.rand.NextFloat(0.95f, 1.25f));
                frost.noGravity = true;

                if (Main.rand.NextBool(3))
                {
                    Dust gem = Dust.NewDustPerfect(
                        spawnPosition + curl * 3f,
                        DustID.GemSapphire,
                        flowVelocity * 0.4f,
                        100,
                        new Color(110, 210, 255),
                        Main.rand.NextFloat(0.9f, 1.2f));
                    gem.noGravity = true;
                }

                if ((timer + lane) % 2 == 0)
                {
                    WaterFlavoredParticle mist = new WaterFlavoredParticle(
                        spawnPosition,
                        flowVelocity * 0.55f,
                        false,
                        Main.rand.Next(18, 26),
                        0.9f + Main.rand.NextFloat(0.3f),
                        Color.LightBlue * 0.92f);
                    GeneralParticleHandler.SpawnParticle(mist);
                }

                if ((timer + lane) % 3 == 0)
                {
                    SparkParticle sparkLine = new SparkParticle(
                        spawnPosition - flowVelocity / 0.18f,
                        flowVelocity * 0.01f,
                        false,
                        5,
                        1.45f + chargeProgress * 0.55f,
                        Color.Lerp(Color.SeaGreen, Color.Cyan, pulseC),
                        true);
                    GeneralParticleHandler.SpawnParticle(sparkLine);
                }
            }

            int directAbsorbers = 2 + (chargeProgress > 0.4f ? 1 : 0);
            for (int i = 0; i < directAbsorbers; i++)
            {
                Vector2 absorberSpawn =
                    lowerFocus +
                    right * Main.rand.NextFloat(-48f, 48f) +
                    Vector2.UnitY * Main.rand.NextFloat(-4f, 20f) +
                    Main.rand.NextVector2Circular(5f, 5f);

                Vector2 absorberVelocity = (tip - absorberSpawn).SafeNormalize(forward) * Main.rand.NextFloat(6f, 12.5f);

                WaterFlavoredParticle mist = new WaterFlavoredParticle(
                    absorberSpawn,
                    absorberVelocity * 0.55f,
                    false,
                    Main.rand.Next(20, 28),
                    0.95f + Main.rand.NextFloat(0.35f),
                    Color.LightBlue * 0.92f);
                GeneralParticleHandler.SpawnParticle(mist);
            }

            if (timer % 2 == 0)
            {
                Vector2 sparkVelocity = forward.RotatedBy(Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4)) * Main.rand.NextFloat(4.5f, 7.5f);
                CritSpark spark = new CritSpark(
                    tip + Main.rand.NextVector2Circular(4f, 4f),
                    sparkVelocity + owner.velocity * 0.15f,
                    Color.White,
                    Color.LightBlue,
                    0.95f + chargeProgress * 0.45f,
                    16 + Main.rand.Next(6));
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (timer % 3 == 0)
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    tip - forward * Main.rand.NextFloat(0f, 10f) + right * Main.rand.NextFloat(-3f, 3f),
                    Vector2.Zero,
                    false,
                    5 + Main.rand.Next(3),
                    0.8f + chargeProgress * 0.45f,
                    Color.Lerp(new Color(40, 175, 255), Color.White, 0.35f + 0.4f * pulseC),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }

            if (timer % 4 == 0)
            {
                HeavySmokeParticle smoke = new HeavySmokeParticle(
                    Projectile.Center - forward * 12f + right * Main.rand.NextFloat(-6f, 6f),
                    -forward * Main.rand.NextFloat(0.35f, 1.2f) + right * Main.rand.NextFloat(-0.6f, 0.6f),
                    Color.WhiteSmoke,
                    18,
                    Main.rand.NextFloat(0.9f, 1.25f),
                    0.35f,
                    Main.rand.NextFloat(-0.25f, 0.25f),
                    false);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (timer % 10 == 0)
            {
                DirectionalPulseRing tipPulse = new DirectionalPulseRing(
                    tip - forward * 4f,
                    forward * 0.35f,
                    Color.Lerp(new Color(70, 190, 255), Color.White, 0.3f + 0.35f * pulseB),
                    new Vector2(0.5f + 0.08f * pulseC, 1.2f + 0.2f * pulseA),
                    Projectile.rotation,
                    0.12f + pulseB * 0.02f,
                    0.014f,
                    10);
                GeneralParticleHandler.SpawnParticle(tipPulse);
            }
        }

        private void SpawnChargeReadyBurst()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = AimDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 tip = WeaponTip;

            for (int i = 0; i < 16; i++)
            {
                float t = i / 15f;
                Vector2 burstVelocity =
                    forward.RotatedBy(MathHelper.Lerp(-0.75f, 0.75f, t)) * Main.rand.NextFloat(3.5f, 8.5f) +
                    right * MathHelper.Lerp(-2f, 2f, t);

                Dust water = Dust.NewDustPerfect(tip, DustID.Water, burstVelocity, 100, new Color(80, 185, 255), Main.rand.NextFloat(1.05f, 1.45f));
                water.noGravity = true;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(tip, DustID.Frost, burstVelocity * 0.68f, 100, new Color(215, 250, 255), Main.rand.NextFloat(0.95f, 1.2f));
                    frost.noGravity = true;
                }
            }

            GlowOrbParticle orb = new GlowOrbParticle(
                tip,
                Vector2.Zero,
                false,
                9,
                1.15f,
                new Color(110, 225, 255),
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(orb);
        }

        private void SpawnReadyHoldEffects()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = AimDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 tip = WeaponTip;

            if (Main.rand.NextBool(2))
            {
                Vector2 orbitPos = tip - forward * Main.rand.NextFloat(2f, 8f) + right * Main.rand.NextFloat(-3f, 3f);
                GlowOrbParticle orb = new GlowOrbParticle(
                    orbitPos,
                    Vector2.Zero,
                    false,
                    6,
                    0.82f + Main.rand.NextFloat(0.18f),
                    Color.Lerp(new Color(65, 180, 255), Color.White, 0.32f),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }

            if ((Main.GameUpdateCount + Projectile.identity) % 6 == 0)
            {
                CritSpark spark = new CritSpark(
                    tip + Main.rand.NextVector2Circular(3f, 3f),
                    forward.RotatedBy(Main.rand.NextFloat(-0.35f, 0.35f)) * Main.rand.NextFloat(3.2f, 5.6f),
                    Color.White,
                    Color.LightBlue,
                    0.9f,
                    12);
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        private void SpawnDashStartBurst()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = BladeDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 tip = WeaponTip;

            for (int i = 0; i < 20; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(1f, 1f).RotatedBy(forward.ToRotation()) * Main.rand.NextFloat(4f, 14f);

                Dust water = Dust.NewDustPerfect(tip, DustID.Water, burstVelocity, 100, new Color(70, 175, 255), Main.rand.NextFloat(1.2f, 1.75f));
                water.noGravity = true;

                Dust frost = Dust.NewDustPerfect(tip, DustID.Frost, burstVelocity * 0.72f, 100, new Color(210, 248, 255), Main.rand.NextFloat(1f, 1.35f));
                frost.noGravity = true;

                if (i % 4 == 0)
                {
                    Dust gem = Dust.NewDustPerfect(tip + right * Main.rand.NextFloat(-4f, 4f), DustID.GemSapphire, burstVelocity * 0.45f, 100, new Color(120, 220, 255), Main.rand.NextFloat(1f, 1.25f));
                    gem.noGravity = true;
                }
            }

            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 5; i++)
                {
                    float t = i / 4f;
                    Vector2 jetVelocity =
                        -forward * MathHelper.Lerp(2f, 8f, t) +
                        right * side * MathHelper.Lerp(0.75f, 3.2f, t);

                    WaterFlavoredParticle mist = new WaterFlavoredParticle(
                        Projectile.Center + right * side * 5f,
                        jetVelocity,
                        false,
                        Main.rand.Next(18, 24),
                        0.95f + Main.rand.NextFloat(0.2f),
                        new Color(150, 225, 255) * 0.95f);
                    GeneralParticleHandler.SpawnParticle(mist);
                }
            }

            GlowOrbParticle orb = new GlowOrbParticle(
                tip,
                Vector2.Zero,
                false,
                7,
                1.15f,
                new Color(110, 225, 255),
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(orb);
        }

        private void SpawnDashWakeEffects(Player owner)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = BladeDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 rear = Projectile.Center - forward * 18f;
            Vector2 tip = WeaponTip;

            float pulseA = 0.5f + 0.5f * (float)Math.Sin(DashTimer * 0.42f);
            float pulseB = 0.5f + 0.5f * (float)Math.Cos(DashTimer * 0.29f + 0.6f);

            for (int side = -1; side <= 1; side += 2)
            {
                float ribbonPhase = DashTimer * 0.46f + side * 0.9f + Projectile.identity * 0.14f;
                float ribbonOffset = side * (7f + 4f * (float)Math.Sin(ribbonPhase * 1.45f));
                Vector2 spawnPosition = rear - forward * Main.rand.NextFloat(8f, 32f) + right * ribbonOffset;
                Vector2 wakeVelocity =
                    -forward * Main.rand.NextFloat(2.6f, 7.8f) +
                    right * side * Main.rand.NextFloat(0.45f, 2.3f) +
                    owner.velocity * 0.12f;

                Dust water = Dust.NewDustPerfect(spawnPosition, DustID.Water, wakeVelocity, 100, new Color(70, 170, 255), Main.rand.NextFloat(1.05f, 1.45f));
                water.noGravity = true;

                Dust frost = Dust.NewDustPerfect(spawnPosition, DustID.Frost, wakeVelocity * 0.72f, 100, new Color(205, 246, 255), Main.rand.NextFloat(0.9f, 1.2f));
                frost.noGravity = true;

                if (Main.rand.NextBool(2))
                {
                    SparkParticle sparkLine = new SparkParticle(
                        spawnPosition - wakeVelocity / 0.18f,
                        wakeVelocity * 0.01f,
                        false,
                        5,
                        1.45f,
                        Color.Lerp(Color.SeaGreen, Color.DeepSkyBlue, pulseB),
                        true);
                    GeneralParticleHandler.SpawnParticle(sparkLine);
                }
            }

            if (DashTimer % 2 == 0)
            {
                WaterFlavoredParticle mist = new WaterFlavoredParticle(
                    rear + right * Main.rand.NextFloat(-8f, 8f),
                    -forward * Main.rand.NextFloat(2.2f, 4.8f) + right * Main.rand.NextFloat(-1.5f, 1.5f),
                    false,
                    Main.rand.Next(18, 24),
                    0.85f + Main.rand.NextFloat(0.25f),
                    Color.LightBlue * 0.92f);
                GeneralParticleHandler.SpawnParticle(mist);
            }

            if (DashTimer % 5 == 0)
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    tip - forward * Main.rand.NextFloat(4f, 10f),
                    owner.velocity * 0.05f,
                    false,
                    5,
                    0.85f + pulseA * 0.25f,
                    Color.Lerp(new Color(65, 180, 255), Color.White, 0.35f + 0.35f * pulseB),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);

                CritSpark spark = new CritSpark(
                    tip + Main.rand.NextVector2Circular(3f, 3f),
                    forward.RotatedBy(Main.rand.NextFloat(-0.45f, 0.45f)) * Main.rand.NextFloat(4f, 7f),
                    Color.White,
                    Color.LightBlue,
                    1f,
                    14);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (DashTimer % 12 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item88 with
                {
                    Volume = 0.4f,
                    Pitch = -0.25f + pulseA * 0.15f
                }, Projectile.Center);
            }
        }

        private void ResetSupportStarCooldown()
        {
            supportStarCooldown = Main.rand.Next(SupportStarMinInterval, SupportStarMaxInterval + 1);
        }

        private void TrySpawnDashSupportStars(Player owner)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            if (supportStarCooldown > 0)
            {
                supportStarCooldown--;
                return;
            }

            ResetSupportStarCooldown();

            Vector2 forward = AimDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 spawnPosition =
                owner.Bottom +
                Vector2.UnitY * Main.rand.NextFloat(10f, 26f) +
                right * Main.rand.NextFloat(-24f, 24f) -
                forward * Main.rand.NextFloat(4f, 16f);

            Vector2 launchDirection = (
                -Vector2.UnitY * Main.rand.NextFloat(0.9f, 1.2f) +
                right * Main.rand.NextFloat(-0.16f, 0.16f) +
                forward * Main.rand.NextFloat(-0.08f, 0.24f)).SafeNormalize(-Vector2.UnitY);

            Vector2 launchVelocity = launchDirection * Main.rand.NextFloat(7.5f, 12.5f);
            int starDamage = Math.Max(1, (int)(Projectile.damage * SupportStarDashDamageFactor));

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                launchVelocity,
                ModContent.ProjectileType<BBSD_Star>(),
                starDamage,
                Projectile.knockBack * 0.35f,
                Projectile.owner);

            SpawnSupportStarLaunchEffects(spawnPosition, launchVelocity, 0.9f);
        }

        private void SpawnImpactStars(Vector2 impactCenter, bool majorImpact)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(AimDirection);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            int starCount = majorImpact ? 11 : 4;
            float damageFactor = majorImpact ? SupportStarImpactDamageFactor : SupportStarDashDamageFactor;
            int starDamage = Math.Max(1, (int)(Projectile.damage * damageFactor));

            for (int i = 0; i < starCount; i++)
            {
                float t = (i + 0.5f) / starCount;
                float spiralAngle = i * GoldenAngle;
                float spiralRadius = MathHelper.Lerp(6f, 28f, (float)Math.Sqrt(t));

                Vector2 spiralOffset =
                    forward * ((float)Math.Cos(spiralAngle) * spiralRadius * 0.65f) +
                    right * ((float)Math.Sin(spiralAngle) * spiralRadius);

                Vector2 launchDirection = (
                    -Vector2.UnitY * 0.95f +
                    forward * 0.42f +
                    spiralOffset.SafeNormalize(forward) * 0.55f +
                    Main.rand.NextVector2Circular(0.08f, 0.08f)).SafeNormalize(-Vector2.UnitY);

                Vector2 launchVelocity = launchDirection * Main.rand.NextFloat(8f, 15.5f);
                Vector2 spawnPosition = impactCenter + spiralOffset + Main.rand.NextVector2Circular(2f, 2f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromAI(),
                    spawnPosition,
                    launchVelocity,
                    ModContent.ProjectileType<BBSD_Star>(),
                    starDamage,
                    Projectile.knockBack * 0.35f,
                    Projectile.owner);

                SpawnSupportStarLaunchEffects(spawnPosition, launchVelocity, majorImpact ? 1.15f : 0.82f);
            }
        }

        private void SpawnSupportStarLaunchEffects(Vector2 spawnPosition, Vector2 launchVelocity, float intensity)
        {
            if (Main.dedServ)
                return;

            Dust water = Dust.NewDustPerfect(
                spawnPosition,
                DustID.Water,
                launchVelocity * 0.28f,
                100,
                new Color(70, 175, 255),
                Main.rand.NextFloat(1f, 1.35f) * intensity);
            water.noGravity = true;
            water.fadeIn = 1.08f;

            Dust frost = Dust.NewDustPerfect(
                spawnPosition,
                DustID.Frost,
                launchVelocity * 0.2f,
                100,
                new Color(210, 248, 255),
                Main.rand.NextFloat(0.85f, 1.15f) * intensity);
            frost.noGravity = true;

            if (Main.rand.NextBool(2))
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    spawnPosition,
                    Vector2.Zero,
                    false,
                    6 + Main.rand.Next(3),
                    0.7f + Main.rand.NextFloat(0.2f) * intensity,
                    Color.Lerp(new Color(65, 185, 255), Color.White, 0.35f),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }

            if (intensity > 1f || Main.rand.NextBool(3))
            {
                CritSpark spark = new CritSpark(
                    spawnPosition,
                    launchVelocity.SafeNormalize(-Vector2.UnitY).RotatedBy(Main.rand.NextFloat(-0.45f, 0.45f)) * Main.rand.NextFloat(3.5f, 6.5f),
                    Color.White,
                    Color.LightBlue,
                    0.9f * intensity,
                    12 + Main.rand.Next(5));
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 300);

            Vector2 impactCenter = Vector2.Lerp(WeaponTip, target.Center, 0.4f);
            bool majorImpact = hitFeedbackCooldown <= 0;

            SpawnImpactBurst(impactCenter, majorImpact);
            SpawnImpactStars(impactCenter, majorImpact);
            SpawnImpactSlash(impactCenter);
            ApplyImpactScreenShake(impactCenter, majorImpact ? 32f : 12f);

            if (majorImpact)
            {
                hitFeedbackCooldown = 8;

                PlayImpactSounds(impactCenter);
            }
        }

        private void SpawnImpactSlash(Vector2 impactCenter)
        {
            if (Main.myPlayer != Projectile.owner || !isDashing)
                return;

            Vector2 slashVelocity = Projectile.velocity.SafeNormalize(AimDirection) * 8f;
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                impactCenter,
                slashVelocity,
                ModContent.ProjectileType<BBSwing_Slash>(),
                Math.Max(1, (int)(Projectile.damage * ImpactSlashDamageFactor)),
                Projectile.knockBack * 0.45f,
                Projectile.owner,
                ImpactSlashScale,
                Main.rand.NextFloat(-0.26f, 0.26f));
        }

        private void ApplyImpactScreenShake(Vector2 impactCenter, float shakePower)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1400f, 0f, Vector2.Distance(impactCenter, Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                Main.LocalPlayer.Calamity().GeneralScreenShakePower,
                shakePower * distanceFactor);
        }

        private void PlayImpactSounds(Vector2 impactCenter)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item14 with
            {
                Volume = 1.2f,
                Pitch = -0.22f
            }, impactCenter);

            SoundEngine.PlaySound(SoundID.Item74 with
            {
                Volume = 1.05f,
                Pitch = -0.34f
            }, impactCenter);

            SoundEngine.PlaySound(SoundID.Splash with
            {
                Volume = 1.15f,
                Pitch = -0.16f
            }, impactCenter);

            SoundEngine.PlaySound(SoundID.Item88 with
            {
                Volume = 0.75f,
                Pitch = -0.32f
            }, impactCenter);
        }

        private void SpawnImpactBurst(Vector2 impactCenter, bool majorImpact)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(AimDirection);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float intensity = majorImpact ? 1f : 0.68f;

            SpawnImpactDustGeometry(impactCenter, forward, right, intensity, majorImpact);
            SpawnImpactParticleGeometry(impactCenter, forward, right, intensity, majorImpact);
            SpawnImpactAftermath(impactCenter, forward, right, intensity, majorImpact);
        }

        private void SpawnImpactDustGeometry(Vector2 impactCenter, Vector2 forward, Vector2 right, float intensity, bool majorImpact)
        {
            int coneCount = majorImpact ? 28 : 16;
            int ellipsePoints = majorImpact ? 40 : 24;

            for (int i = 0; i < coneCount; i++)
            {
                float t = i / (float)Math.Max(1, coneCount - 1);
                float centered = t * 2f - 1f;
                float wave = (float)Math.Sin(t * MathHelper.TwoPi * 2.5f + timer * 0.18f) * 0.06f;
                float coneOffset = MathHelper.Lerp(-0.9f, 0.9f, t) + wave;
                float centralFocus = 1f - Math.Abs(centered);
                float speed = MathHelper.Lerp(10f, 26f, (float)Math.Sqrt(Math.Max(0f, centralFocus))) * intensity;
                Vector2 jetVelocity =
                    forward.RotatedBy(coneOffset) * speed +
                    right * centered * 2.4f +
                    Owner.velocity * 0.12f;

                Dust water = Dust.NewDustPerfect(
                    impactCenter + right * centered * 6f,
                    DustID.Water,
                    jetVelocity,
                    100,
                    new Color(75, 180, 255),
                    Main.rand.NextFloat(1.15f, 1.8f) * intensity);
                water.noGravity = true;
                water.fadeIn = 1.15f;

                Dust frost = Dust.NewDustPerfect(
                    impactCenter + forward * 2f,
                    DustID.Frost,
                    jetVelocity * 0.72f,
                    100,
                    new Color(210, 248, 255),
                    Main.rand.NextFloat(0.95f, 1.35f) * intensity);
                frost.noGravity = true;

                if (i % 3 == 0)
                {
                    Dust gem = Dust.NewDustPerfect(
                        impactCenter + right * centered * 10f,
                        DustID.GemSapphire,
                        jetVelocity * 0.46f,
                        100,
                        new Color(130, 220, 255),
                        Main.rand.NextFloat(0.95f, 1.25f) * intensity);
                    gem.noGravity = true;
                }
            }

            float ellipseForwardRadius = MathHelper.Lerp(22f, 40f, intensity);
            float ellipseSideRadius = MathHelper.Lerp(34f, 58f, intensity);
            for (int i = 0; i < ellipsePoints; i++)
            {
                float angle = MathHelper.TwoPi * i / ellipsePoints;
                float pulse = 0.75f + 0.25f * (float)Math.Sin(angle * 3f + timer * 0.14f);

                Vector2 ellipseOffset =
                    forward * (float)Math.Cos(angle) * ellipseForwardRadius +
                    right * (float)Math.Sin(angle) * ellipseSideRadius;

                Vector2 ringPosition = impactCenter + ellipseOffset;
                Vector2 ringVelocity =
                    ellipseOffset.SafeNormalize(forward) * MathHelper.Lerp(3.5f, 9f, pulse) * intensity +
                    forward * 1.75f;

                Dust water = Dust.NewDustPerfect(
                    ringPosition,
                    DustID.Water,
                    ringVelocity,
                    100,
                    new Color(70, 170, 255),
                    Main.rand.NextFloat(0.95f, 1.4f) * intensity);
                water.noGravity = true;

                if (i % 2 == 0)
                {
                    Dust frost = Dust.NewDustPerfect(
                        ringPosition,
                        DustID.Frost,
                        ringVelocity * 0.65f,
                        100,
                        new Color(205, 246, 255),
                        Main.rand.NextFloat(0.8f, 1.15f) * intensity);
                    frost.noGravity = true;
                }
            }
        }

        private void SpawnImpactParticleGeometry(Vector2 impactCenter, Vector2 forward, Vector2 right, float intensity, bool majorImpact)
        {
            int pulseCount = majorImpact ? 3 : 2;
            for (int i = 0; i < pulseCount; i++)
            {
                float pulseScale = 0.18f + i * 0.05f;
                DirectionalPulseRing pulse = new DirectionalPulseRing(
                    impactCenter - forward * (4f + i * 5f),
                    forward * (0.45f + i * 0.12f),
                    Color.Lerp(new Color(70, 190, 255), Color.White, 0.25f + i * 0.18f),
                    new Vector2(0.6f + i * 0.12f, 1.8f + i * 0.35f),
                    forward.ToRotation(),
                    pulseScale,
                    0.016f,
                    12 + i * 3);
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            int starPoints = majorImpact ? 10 : 6;
            for (int i = 0; i < starPoints; i++)
            {
                float angle = forward.ToRotation() + MathHelper.TwoPi * i / starPoints;
                Vector2 sparkVelocity = angle.ToRotationVector2() * Main.rand.NextFloat(5.5f, 12f) * intensity + Owner.velocity * 0.08f;
                CritSpark spark = new CritSpark(
                    impactCenter + angle.ToRotationVector2() * Main.rand.NextFloat(2f, 8f),
                    sparkVelocity,
                    Color.White,
                    Color.LightBlue,
                    Main.rand.NextFloat(0.95f, 1.35f) * intensity,
                    14 + Main.rand.Next(8));
                GeneralParticleHandler.SpawnParticle(spark);
            }

            int spiralArms = 2;
            int spiralPoints = majorImpact ? 18 : 11;
            for (int arm = 0; arm < spiralArms; arm++)
            {
                float sign = arm == 0 ? 1f : -1f;
                Vector2 previousPosition = impactCenter;

                for (int i = 0; i < spiralPoints; i++)
                {
                    float t = i / (float)Math.Max(1, spiralPoints - 1);
                    float theta = forward.ToRotation() + sign * t * MathHelper.TwoPi * 1.12f;
                    float radius = (10f + 54f * (float)Math.Pow(t, 1.12f)) * intensity;
                    Vector2 spiralPosition = impactCenter + forward * t * 14f + theta.ToRotationVector2() * radius;
                    Vector2 tangent = (theta + sign * MathHelper.PiOver2).ToRotationVector2();

                    GlowOrbParticle orb = new GlowOrbParticle(
                        spiralPosition,
                        tangent * Main.rand.NextFloat(0.15f, 0.75f),
                        false,
                        7 + Main.rand.Next(4),
                        Main.rand.NextFloat(0.85f, 1.2f) * intensity,
                        Color.Lerp(new Color(65, 180, 255), Color.White, 0.3f + 0.45f * t),
                        true,
                        false,
                        true);
                    GeneralParticleHandler.SpawnParticle(orb);

                    if (i > 0)
                    {
                        Vector2 segmentVelocity = (spiralPosition - previousPosition) * 0.1f;
                        SparkParticle sparkLine = new SparkParticle(
                            previousPosition - segmentVelocity / 0.18f,
                            segmentVelocity * 0.01f,
                            false,
                            5,
                            1.6f + 0.25f * t,
                            Color.Lerp(Color.SeaGreen, Color.DeepSkyBlue, t),
                            true);
                        GeneralParticleHandler.SpawnParticle(sparkLine);
                    }

                    previousPosition = spiralPosition;
                }
            }

            for (int side = -1; side <= 1; side += 2)
            {
                int jetSegments = majorImpact ? 6 : 4;
                for (int i = 0; i < jetSegments; i++)
                {
                    float t = i / (float)Math.Max(1, jetSegments - 1);
                    float bendAngle = MathHelper.Lerp(0.14f, 0.78f, t) * side;
                    Vector2 jetVelocity =
                        forward.RotatedBy(bendAngle) * MathHelper.Lerp(4.5f, 14f, t) * intensity +
                        right * side * MathHelper.Lerp(0.8f, 4f, t);

                    WaterFlavoredParticle mist = new WaterFlavoredParticle(
                        impactCenter + right * side * (4f + t * 8f),
                        jetVelocity * 0.48f,
                        false,
                        Main.rand.Next(18, 28),
                        0.9f + Main.rand.NextFloat(0.35f) * intensity,
                        Color.LightBlue * 0.95f);
                    GeneralParticleHandler.SpawnParticle(mist);
                }
            }
        }

        private void SpawnImpactAftermath(Vector2 impactCenter, Vector2 forward, Vector2 right, float intensity, bool majorImpact)
        {
            int smokeCount = majorImpact ? 7 : 4;
            for (int i = 0; i < smokeCount; i++)
            {
                Vector2 smokeVelocity =
                    -forward * Main.rand.NextFloat(0.35f, 1.8f) +
                    right * Main.rand.NextFloat(-2f, 2f) +
                    Main.rand.NextVector2Circular(0.4f, 0.4f);

                HeavySmokeParticle smoke = new HeavySmokeParticle(
                    impactCenter + Main.rand.NextVector2Circular(8f, 8f),
                    smokeVelocity,
                    Color.WhiteSmoke,
                    18 + Main.rand.Next(6),
                    Main.rand.NextFloat(1f, 1.55f) * intensity,
                    0.35f,
                    Main.rand.NextFloat(-0.45f, 0.45f),
                    false);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            int lineCount = majorImpact ? 8 : 5;
            for (int i = 0; i < lineCount; i++)
            {
                float sideBias = MathHelper.Lerp(-0.42f, 0.42f, i / (float)Math.Max(1, lineCount - 1));
                Vector2 lineVelocity =
                    forward.RotatedBy(sideBias) * Main.rand.NextFloat(6f, 12f) * intensity +
                    right * sideBias * 2.2f;

                SparkParticle sparkLine = new SparkParticle(
                    impactCenter - lineVelocity / 0.18f,
                    lineVelocity * 0.01f,
                    false,
                    5,
                    1.85f,
                    Color.SeaGreen,
                    true);
                GeneralParticleHandler.SpawnParticle(sparkLine);
            }

            int bloodCount = majorImpact ? 7 : 4;
            for (int i = 0; i < bloodCount; i++)
            {
                Vector2 bloodVelocity =
                    forward.RotatedBy(Main.rand.NextFloat(-0.95f, 0.95f)) * Main.rand.NextFloat(3f, 11f) * intensity +
                    right * Main.rand.NextFloat(-2.5f, 2.5f);

                BloodParticle blood = new BloodParticle(
                    impactCenter + Main.rand.NextVector2Circular(8f, 8f),
                    bloodVelocity,
                    Main.rand.Next(20, 34),
                    Main.rand.NextFloat(0.75f, 1.2f) * intensity,
                    Color.Lerp(new Color(90, 12, 26), new Color(170, 32, 55), Main.rand.NextFloat()));
                GeneralParticleHandler.SpawnParticle(blood);
            }

            int bubbleCount = majorImpact ? 7 : 4;
            for (int i = 0; i < bubbleCount; i++)
            {
                Gore bubble = Gore.NewGorePerfect(
                    Projectile.GetSource_FromAI(),
                    impactCenter + Main.rand.NextVector2Circular(8f, 8f),
                    Projectile.velocity * 0.2f + Main.rand.NextVector2Circular(1.2f, 1.2f),
                    411);
                bubble.timeLeft = 6 + Main.rand.Next(7);
                bubble.scale = Main.rand.NextFloat(0.6f, 0.85f) * intensity;
                bubble.type = Main.rand.NextBool(3) ? 412 : 411;
            }
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 forward = BladeDirection;

            for (int i = 0; i < 36; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(1f, 1f).RotatedBy(forward.ToRotation()) * Main.rand.NextFloat(4f, 16f);

                Dust water = Dust.NewDustPerfect(Projectile.Center, DustID.Water, burstVelocity, 100, new Color(70, 170, 255), Main.rand.NextFloat(1.2f, 1.8f));
                water.noGravity = true;

                Dust frost = Dust.NewDustPerfect(Projectile.Center, DustID.Frost, burstVelocity * 0.72f, 100, new Color(210, 245, 255), Main.rand.NextFloat(0.95f, 1.4f));
                frost.noGravity = true;
            }

            if (!Main.dedServ)
            {
                for (int i = 0; i < 3; i++)
                {
                    GlowOrbParticle orb = new GlowOrbParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                        Main.rand.NextVector2Circular(1f, 1f),
                        false,
                        6,
                        0.95f + Main.rand.NextFloat(0.3f),
                        new Color(90, 205, 255),
                        true,
                        false,
                        true);
                    GeneralParticleHandler.SpawnParticle(orb);
                }
            }

            SoundEngine.PlaySound(SoundID.Item74 with
            {
                Volume = 1.15f,
                Pitch = -0.3f
            }, Projectile.Center);
        }
    }
}
