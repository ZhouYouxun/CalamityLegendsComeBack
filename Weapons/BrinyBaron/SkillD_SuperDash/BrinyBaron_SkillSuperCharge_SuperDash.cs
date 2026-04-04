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
        private const float DashSpeed = 55f;
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
        private int readyTimer;
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
                    BBSD_Charge_Effects.SpawnChargeEffects(Projectile, owner, WeaponTip, timer);
                    Lighting.AddLight(WeaponTip, 0.08f, 0.28f, 0.38f);

                    if (timer >= ChargeTime)
                        EnterReadyState(owner);
                }
                else
                {
                    readyTimer++;
                    UpdateReadyAim(owner);
                    UpdateHeldBlade(owner, false, true);

                    BBSD_Charge_Effects.SpawnReadyHoldEffects(Projectile, WeaponTip, readyTimer);
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

                BBSD_Fly_Effects.SpawnDashWakeEffects(Projectile, owner, BladeDirection, WeaponTip, dashTimer);
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
            readyTimer = 0;
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
            if (Main.myPlayer != Projectile.owner)
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

            float chargeProgress = chargeReady ? 1f : Utils.GetLerpValue(0f, ChargeTime, timer, true);
            float holdDistance = readyState
                ? HoldDistanceReady
                : (dashing ? HoldDistanceDash : HoldDistanceCharge) + chargeProgress * (dashing ? 0f : 8f);

            Projectile.Center = armPosition + forward * holdDistance + new Vector2(0f, 6f);

            Projectile.rotation = forward.ToRotation() + MathHelper.PiOver4;
            Projectile.scale = readyState
                ? 1.08f
                : dashing
                ? 1.08f
                : 1f + chargeProgress * 0.08f;

            Projectile.direction = forward.X >= 0f ? 1 : -1;
            owner.ChangeDir(Projectile.direction);
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
            owner.itemRotation = (forward * owner.direction).ToRotation();

            float armRotation = forward.ToRotation() - MathHelper.PiOver2;
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
        }

        private void EnterReadyState(Player owner)
        {
            chargeReady = true;
            readyTimer = 0;
            Projectile.friendly = false;
            Projectile.velocity = Vector2.Zero;

            SoundEngine.PlaySound(SoundID.Item29 with
            {
                Volume = 0.85f,
                Pitch = -0.05f
            }, WeaponTip);

            BBSD_Ready_Effects.SpawnChargeReadyBurst(Projectile, WeaponTip);
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
            BBSD_Fly_Effects.SpawnDashStartBurst(Projectile, BladeDirection, WeaponTip);
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

            BBSD_Fly_Effects.SpawnSupportStarLaunchEffects(spawnPosition, launchVelocity, 0.9f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 300);

            Vector2 impactCenter = Vector2.Lerp(WeaponTip, target.Center, 0.4f);
            bool majorImpact = hitFeedbackCooldown <= 0;

            BBSD_Hit_Effects.SpawnImpactBurst(Projectile, Owner, AimDirection, timer, impactCenter, majorImpact);
            BBSD_Hit_Effects.SpawnImpactStars(Projectile, AimDirection, GoldenAngle, SupportStarImpactDamageFactor, SupportStarDashDamageFactor, impactCenter, majorImpact);
            BBSD_Hit_Effects.SpawnImpactSlash(Projectile, AimDirection, ImpactSlashDamageFactor, ImpactSlashScale, impactCenter, isDashing);
            BBSD_Hit_Effects.ApplyImpactScreenShake(impactCenter, majorImpact ? 32f : 12f);

            if (majorImpact)
            {
                hitFeedbackCooldown = 8;
                BBSD_Hit_Effects.PlayImpactSounds(impactCenter);
            }
        }

        public override void OnKill(int timeLeft)
        {
            BBSD_Fly_Effects.SpawnOnKillEffects(Projectile, BladeDirection);
        }
    }
}
