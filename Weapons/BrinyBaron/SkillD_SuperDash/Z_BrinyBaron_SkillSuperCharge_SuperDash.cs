using System;
using System.IO;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using CalamityLegendsComeBack.Weapons.BrinyBaron.POWER;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    public class Z_BrinyBaron_SkillSuperCharge_SuperDash : ModProjectile
    {
        private enum SuperDashPhase : byte
        {
            Charging,
            Locked,
            Teleporting,
            Striking
        }

        private const int ChargeFrames = 120;
        private const int ChargeSearchInterval = 6;
        private const int LockSearchInterval = 10;
        private const int TeleportWindupFrames = 5;
        private const int StrikeFrames = 8;
        private const float ChargeHoldDistance = 22f;
        private const float LockHoldDistance = 28f;
        private const float StrikeHoldDistance = 34f;
        private const float ChargeTurnRate = 0.14f;
        private const float LockTurnRate = 0.24f;
        private const float StrikeCollisionWidth = 84f;
        private const float TeleportRadiusBase = 280f;
        private const float TeleportRadiusWave = 46f;
        private const float DashOvershootDistance = 220f;
        private const float StrikeSlashDamageFactor = 0.75f;
        private const float StrikeSlashScale = 1.45f;
        private const float FinalMarkDamageFactor = 0.9f;
        private const float GoldenAngle = 2.39996323f;

        private Player Owner => Main.player[Projectile.owner];
        private Vector2 DefaultDirection => Vector2.UnitX * (Owner.direction == 0 ? 1 : Owner.direction);
        private Vector2 BladeDirection => (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2();
        private Vector2 WeaponTip => Projectile.Center + BladeDirection * (50f * Projectile.scale);
        private NPC CurrentTarget => BBSuperDashTargeting.IsTargetValid(targetNpcIndex) ? Main.npc[targetNpcIndex] : null;

        private SuperDashPhase phase;
        private int phaseTimer;
        private int totalStrikes;
        private int strikeIndex;
        private int targetNpcIndex = -1;
        private int targetSearchCooldown;
        private bool initialized;
        private bool hadLockedTarget;
        private bool impactTriggeredThisStrike;
        private bool finishedNormally;
        private Vector2 lockedDirection = Vector2.UnitX;
        private Vector2 focusPoint;
        private Vector2 strikeStart;
        private Vector2 strikeEnd;
        private Vector2 collisionStart;
        private Vector2 collisionEnd;

        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron";
        public new string LocalizationCategory => "Projectiles.BrinyBaron";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 5000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 36000;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => phase == SuperDashPhase.Striking ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (phase != SuperDashPhase.Striking)
                return false;

            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                collisionStart,
                collisionEnd,
                StrikeCollisionWidth * Projectile.scale,
                ref collisionPoint);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)phase);
            writer.Write(phaseTimer);
            writer.Write(totalStrikes);
            writer.Write(strikeIndex);
            writer.Write(targetNpcIndex);
            writer.Write(targetSearchCooldown);
            writer.Write(initialized);
            writer.Write(hadLockedTarget);
            writer.Write(impactTriggeredThisStrike);
            writer.Write(finishedNormally);
            writer.WriteVector2(lockedDirection);
            writer.WriteVector2(focusPoint);
            writer.WriteVector2(strikeStart);
            writer.WriteVector2(strikeEnd);
            writer.WriteVector2(collisionStart);
            writer.WriteVector2(collisionEnd);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            phase = (SuperDashPhase)reader.ReadByte();
            phaseTimer = reader.ReadInt32();
            totalStrikes = reader.ReadInt32();
            strikeIndex = reader.ReadInt32();
            targetNpcIndex = reader.ReadInt32();
            targetSearchCooldown = reader.ReadInt32();
            initialized = reader.ReadBoolean();
            hadLockedTarget = reader.ReadBoolean();
            impactTriggeredThisStrike = reader.ReadBoolean();
            finishedNormally = reader.ReadBoolean();
            lockedDirection = reader.ReadVector2();
            focusPoint = reader.ReadVector2();
            strikeStart = reader.ReadVector2();
            strikeEnd = reader.ReadVector2();
            collisionStart = reader.ReadVector2();
            collisionEnd = reader.ReadVector2();
        }

        public override void AI()
        {
            Player owner = Owner;
            if (!owner.active || owner.dead)
            {
                AbortAndKill(restartCooldown: true);
                return;
            }

            if (!initialized)
                Initialize(owner);

            owner.GetModPlayer<BBSuperDashLeviathanFilterPlayer>().EnableFilter();
            UpdateFocusPoint(owner);
            MaintainCameraLock(owner);

            if (targetSearchCooldown > 0)
                targetSearchCooldown--;

            NPC target = ResolveTarget(owner);

            switch (phase)
            {
                case SuperDashPhase.Charging:
                    DoCharging(owner, target);
                    break;

                case SuperDashPhase.Locked:
                    DoLocked(owner, target);
                    break;

                case SuperDashPhase.Teleporting:
                    if (target is null)
                    {
                        AbortAndKill(restartCooldown: true);
                        return;
                    }

                    DoTeleportWindup(owner, target);
                    break;

                case SuperDashPhase.Striking:
                    if (target is null)
                    {
                        AbortAndKill(restartCooldown: true);
                        return;
                    }

                    DoStrike(owner, target);
                    break;
            }
        }

        private void Initialize(Player owner)
        {
            initialized = true;
            phase = SuperDashPhase.Charging;
            phaseTimer = 0;
            strikeIndex = 0;
            totalStrikes = 5 + Math.Max(1, (int)Projectile.ai[0]) * 2;
            targetNpcIndex = -1;
            targetSearchCooldown = 0;
            hadLockedTarget = false;
            impactTriggeredThisStrike = false;
            finishedNormally = false;
            lockedDirection = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(DefaultDirection);
            focusPoint = Main.MouseWorld;
            strikeStart = owner.Center;
            strikeEnd = owner.Center;
            collisionStart = owner.Center;
            collisionEnd = owner.Center;
            Projectile.scale = 1f;
            Projectile.Center = owner.RotatedRelativePoint(owner.MountedCenter, true) + lockedDirection * ChargeHoldDistance;
            Projectile.rotation = lockedDirection.ToRotation() + MathHelper.PiOver4;
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Item29 with
            {
                Volume = 0.75f,
                Pitch = -0.2f
            }, owner.Center);
        }

        private void UpdateFocusPoint(Player owner)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            focusPoint = Main.MouseWorld;
        }

        private NPC ResolveTarget(Player owner)
        {
            if (hadLockedTarget && phase != SuperDashPhase.Charging && !BBSuperDashTargeting.IsTargetValid(targetNpcIndex))
                return null;

            bool shouldKeepLockedTarget = hadLockedTarget && phase != SuperDashPhase.Charging;
            if (Main.myPlayer == Projectile.owner && targetSearchCooldown <= 0 && !shouldKeepLockedTarget)
            {
                int nextTarget = BBSuperDashTargeting.FindBestTargetIndex(owner, focusPoint, targetNpcIndex);
                if (nextTarget != targetNpcIndex)
                {
                    targetNpcIndex = nextTarget;
                    Projectile.netUpdate = true;
                }

                targetSearchCooldown = phase == SuperDashPhase.Charging ? ChargeSearchInterval : LockSearchInterval;
            }

            return CurrentTarget;
        }

        private void DoCharging(Player owner, NPC target)
        {
            phaseTimer++;
            Projectile.friendly = false;
            Projectile.velocity = Vector2.Zero;

            Vector2 desiredDirection = target is not null
                ? (target.Center - owner.MountedCenter).SafeNormalize(lockedDirection)
                : (focusPoint - owner.MountedCenter).SafeNormalize(lockedDirection);

            RotateHeldDirectionToward(desiredDirection, ChargeTurnRate);
            ApplyHeldBlade(owner, lockedDirection, ChargeHoldDistance, 1f + phaseTimer / (float)ChargeFrames * 0.12f, 0.35f + phaseTimer / (float)ChargeFrames * 0.75f);

            owner.velocity *= 0.82f;
            owner.immune = true;
            owner.immuneTime = 2;

            float chargeCompletion = Utils.GetLerpValue(0f, ChargeFrames, phaseTimer, true);
            BBSD_Charge_Effects.SpawnChargeEffects(Projectile, owner, focusPoint, target, chargeCompletion, phaseTimer);
            ApplyChargeShake(owner, chargeCompletion);
            Lighting.AddLight(WeaponTip, new Vector3(0.12f, 0.38f, 0.52f) * (0.55f + chargeCompletion * 0.65f));

            if (phaseTimer < ChargeFrames)
                return;

            phase = SuperDashPhase.Locked;
            phaseTimer = 0;
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Item122 with
            {
                Volume = 0.9f,
                Pitch = -0.25f
            }, WeaponTip);
        }

        private void DoLocked(Player owner, NPC target)
        {
            phaseTimer++;
            Projectile.friendly = false;
            Projectile.velocity = Vector2.Zero;

            if (target is not null && !hadLockedTarget)
            {
                hadLockedTarget = true;
                Projectile.netUpdate = true;
                BBSD_Lock_Effects.SpawnTargetAcquireEffects(target.Center, owner.Center);
                SoundEngine.PlaySound(SoundID.Item92 with
                {
                    Volume = 0.85f,
                    Pitch = -0.05f
                }, target.Center);
            }

            if (hadLockedTarget && target is null)
            {
                AbortAndKill(restartCooldown: true);
                return;
            }

            Vector2 desiredDirection = target is not null
                ? (target.Center - owner.MountedCenter).SafeNormalize(lockedDirection)
                : (focusPoint - owner.MountedCenter).SafeNormalize(lockedDirection);

            RotateHeldDirectionToward(desiredDirection, LockTurnRate);
            ApplyHeldBlade(owner, lockedDirection, LockHoldDistance, 1.12f, 0.15f);

            owner.velocity *= 0.88f;
            owner.immune = true;
            owner.immuneTime = 2;

            BBSD_Lock_Effects.SpawnLockingEffects(Projectile, owner, focusPoint, target, phaseTimer, hadLockedTarget);
            Lighting.AddLight(WeaponTip, new Vector3(0.14f, 0.44f, 0.6f) * 0.78f);

            if (Main.myPlayer != Projectile.owner || target is null)
                return;

            if (!Main.mouseLeft || !Main.mouseLeftRelease || owner.mouseInterface || Main.blockMouse || Main.mapFullscreen)
                return;

            BeginTeleport(owner, target);
        }

        private void BeginTeleport(Player owner, NPC target)
        {
            float orbitAngle = GoldenAngle * strikeIndex + (strikeIndex % 2 == 0 ? 0f : MathHelper.Pi);
            float radius = TeleportRadiusBase + (float)Math.Sin(strikeIndex * 0.78f) * TeleportRadiusWave;
            Vector2 orbitDirection = orbitAngle.ToRotationVector2();

            strikeStart = target.Center + orbitDirection * radius;
            Vector2 dashDirection = (target.Center - strikeStart).SafeNormalize(DefaultDirection);
            Vector2 tangent = dashDirection.RotatedBy(MathHelper.PiOver2) * ((strikeIndex % 2 == 0 ? 1f : -1f) * 18f);
            strikeStart += tangent;
            strikeEnd = target.Center + dashDirection * DashOvershootDistance;
            collisionStart = strikeStart;
            collisionEnd = strikeStart;
            impactTriggeredThisStrike = false;

            TeleportOwner(owner, strikeStart);
            phase = SuperDashPhase.Teleporting;
            phaseTimer = 0;
            lockedDirection = dashDirection;
            Projectile.friendly = false;
            Projectile.netUpdate = true;

            BBSD_Teleport_Effects.SpawnTeleportBurst(strikeStart, target.Center, dashDirection, strikeIndex, totalStrikes);
            SoundEngine.PlaySound(SoundID.Item8 with
            {
                Volume = 0.95f,
                Pitch = -0.15f
            }, strikeStart);
        }

        private void DoTeleportWindup(Player owner, NPC target)
        {
            phaseTimer++;
            Projectile.friendly = false;
            Projectile.velocity = Vector2.Zero;

            lockedDirection = (target.Center - owner.Center).SafeNormalize(lockedDirection);
            ApplyHeldBlade(owner, lockedDirection, StrikeHoldDistance, 1.22f, 0.95f);

            owner.velocity = Vector2.Zero;
            owner.immune = true;
            owner.immuneTime = 2;
            owner.noKnockback = true;

            BBSD_Teleport_Effects.SpawnTeleportHoldEffects(Projectile, target.Center, phaseTimer, TeleportWindupFrames);
            Lighting.AddLight(WeaponTip, new Vector3(0.18f, 0.55f, 0.75f) * 0.92f);

            if (phaseTimer < TeleportWindupFrames)
                return;

            phase = SuperDashPhase.Striking;
            phaseTimer = 0;
            Projectile.friendly = true;
            Projectile.netUpdate = true;

            BBSD_Strike_Effects.SpawnStrikeLaunchEffects(Projectile, strikeStart, target.Center, lockedDirection, strikeIndex);
            SoundEngine.PlaySound(SoundID.Item71 with
            {
                Volume = 1.05f,
                Pitch = -0.28f
            }, strikeStart);
        }

        private void DoStrike(Player owner, NPC target)
        {
            Vector2 previousCenter = owner.Center;

            phaseTimer++;
            float progress = Utils.GetLerpValue(0f, StrikeFrames, phaseTimer, true);
            float easedProgress = 1f - (float)Math.Pow(1f - progress, 2f);
            Vector2 currentCenter = Vector2.Lerp(strikeStart, strikeEnd, easedProgress);
            Vector2 dashVelocity = currentCenter - previousCenter;

            owner.Center = currentCenter;
            owner.velocity = dashVelocity;
            owner.immune = true;
            owner.immuneTime = 2;
            owner.noKnockback = true;

            collisionStart = previousCenter;
            collisionEnd = currentCenter;
            Projectile.velocity = dashVelocity;

            if (dashVelocity.LengthSquared() > 1f)
                lockedDirection = dashVelocity.SafeNormalize(lockedDirection);

            ApplyHeldBlade(owner, lockedDirection, StrikeHoldDistance, 1.24f, 0f);
            BBSD_Strike_Effects.SpawnStrikeTravelEffects(Projectile, previousCenter, currentCenter, lockedDirection, phaseTimer, strikeIndex);
            Lighting.AddLight(WeaponTip, new Vector3(0.2f, 0.62f, 0.82f));

            if (!impactTriggeredThisStrike && SegmentHitsTarget(target, previousCenter, currentCenter))
                HandleStrikeImpact(target);

            if (phaseTimer < StrikeFrames)
                return;

            Projectile.friendly = false;
            strikeIndex++;
            Projectile.netUpdate = true;

            if (strikeIndex >= totalStrikes)
            {
                finishedNormally = true;
                Projectile.Kill();
                return;
            }

            BeginTeleport(owner, target);
        }

        private bool SegmentHitsTarget(NPC target, Vector2 lineStart, Vector2 lineEnd)
        {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                target.Hitbox.TopLeft(),
                target.Hitbox.Size(),
                lineStart,
                lineEnd,
                StrikeCollisionWidth,
                ref collisionPoint);
        }

        private void HandleStrikeImpact(NPC target)
        {
            impactTriggeredThisStrike = true;
            Projectile.netUpdate = true;

            Vector2 strikeDirection = lockedDirection.SafeNormalize(DefaultDirection);
            int slashDamage = Math.Max(1, (int)(Projectile.damage * StrikeSlashDamageFactor));

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                strikeDirection * 7f,
                ModContent.ProjectileType<BBSwing_Slash>(),
                slashDamage,
                Projectile.knockBack,
                Projectile.owner,
                StrikeSlashScale,
                Main.rand.NextFloat(-0.2f, 0.2f));

            if (Main.myPlayer == Projectile.owner && strikeIndex == totalStrikes - 1)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    strikeDirection,
                    ModContent.ProjectileType<BBSD_Final_INV>(),
                    Math.Max(1, (int)(Projectile.damage * FinalMarkDamageFactor)),
                    Projectile.knockBack,
                    Projectile.owner,
                    target.whoAmI,
                    strikeDirection.ToRotation());
            }

            BBSD_Strike_Effects.SpawnStrikeImpactEffects(Projectile, target.Center, strikeDirection, strikeIndex, totalStrikes);
            float shakePower = 15f;
            float distanceFactor = Utils.GetLerpValue(1000f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                Main.LocalPlayer.Calamity().GeneralScreenShakePower,
                shakePower * distanceFactor);

            SoundEngine.PlaySound(SoundID.Item84 with
            {
                Volume = 1.1f,
                Pitch = -0.18f
            }, target.Center);
            SoundEngine.PlaySound(SoundID.Item14 with
            {
                Volume = 0.85f,
                Pitch = -0.34f
            }, target.Center);
        }

        private void ApplyChargeShake(Player owner, float chargeCompletion)
        {
            if (Main.myPlayer != owner.whoAmI)
                return;

            float shakePower = MathHelper.Lerp(2f, 18f, chargeCompletion);
            owner.Calamity().GeneralScreenShakePower = Math.Max(owner.Calamity().GeneralScreenShakePower, shakePower);
        }

        private void RotateHeldDirectionToward(Vector2 targetDirection, float maxTurn)
        {
            float currentAngle = lockedDirection.SafeNormalize(Vector2.UnitX).ToRotation();
            float targetAngle = targetDirection.SafeNormalize(Vector2.UnitX).ToRotation();
            lockedDirection = currentAngle.AngleTowards(targetAngle, maxTurn).ToRotationVector2();
        }

        private void ApplyHeldBlade(Player owner, Vector2 direction, float holdDistance, float scale, float shakeAmount)
        {
            direction = direction.SafeNormalize(DefaultDirection);
            Vector2 armPosition = owner.RotatedRelativePoint(owner.MountedCenter, true);
            Vector2 jitter = shakeAmount > 0f ? Main.rand.NextVector2Circular(shakeAmount, shakeAmount) : Vector2.Zero;

            Projectile.Center = armPosition + direction * holdDistance + jitter + new Vector2(0f, 6f);
            Projectile.rotation = direction.ToRotation() + MathHelper.PiOver4;
            Projectile.scale = scale;
            Projectile.direction = direction.X >= 0f ? 1 : -1;

            owner.ChangeDir(Projectile.direction);
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
            owner.itemRotation = (direction * owner.direction).ToRotation();
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, direction.ToRotation() - MathHelper.PiOver2);
        }

        private void MaintainCameraLock(Player owner)
        {
            BBSuperDashCameraPlayer cameraPlayer = owner.GetModPlayer<BBSuperDashCameraPlayer>();

            if (phase == SuperDashPhase.Teleporting || phase == SuperDashPhase.Striking)
            {
                if (CurrentTarget is not null)
                    cameraPlayer.LockToTarget(targetNpcIndex);
                else
                    cameraPlayer.ClearLock();
            }
            else
                cameraPlayer.ClearLock();
        }

        private void TeleportOwner(Player owner, Vector2 destination)
        {
            owner.Center = destination;
            owner.velocity = Vector2.Zero;
            owner.fallStart = (int)(owner.position.Y / 16f);
            owner.immune = true;
            owner.immuneTime = 2;
            owner.noKnockback = true;
        }

        private void AbortAndKill(bool restartCooldown)
        {
            if (restartCooldown)
                Owner.GetModPlayer<BBSuperDashCooldownPlayer>().StartCooldown();

            Projectile.Kill();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (phase == SuperDashPhase.Locked && CurrentTarget is not null)
                BBSD_Lock_Effects.DrawLockBeam(Owner.MountedCenter, CurrentTarget.Center, 1f);

            if (phase == SuperDashPhase.Charging || phase == SuperDashPhase.Locked)
                BBSD_Lock_Effects.DrawTargetingReticle(focusPoint, CurrentTarget, phase == SuperDashPhase.Locked && CurrentTarget is not null);

            Texture2D weaponTexture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            Vector2 origin = new Vector2(0f, weaponTexture.Height * 0.5f);
            Color bladeColor = Projectile.GetAlpha(lightColor);

            if (phase == SuperDashPhase.Striking)
            {
                for (int i = 0; i < 3; i++)
                {
                    float trailFactor = 1f - i / 3f;
                    Vector2 trailOffset = -Projectile.velocity.SafeNormalize(lockedDirection) * (12f + i * 8f);
                    Main.EntitySpriteDraw(
                        weaponTexture,
                        drawPosition + trailOffset,
                        null,
                        bladeColor * (0.16f * trailFactor),
                        Projectile.rotation,
                        origin,
                        Projectile.scale * (0.98f - i * 0.05f),
                        Projectile.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None,
                        0f);
                }
            }

            Main.EntitySpriteDraw(
                weaponTexture,
                drawPosition,
                null,
                bladeColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                Projectile.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None,
                0f);

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            Owner.GetModPlayer<BBSuperDashCameraPlayer>().ClearLock();
            Projectile.friendly = false;

            if (!finishedNormally)
            {
                BBSD_Teleport_Effects.SpawnAbortEffects(Projectile.Center, lockedDirection);
                return;
            }

            BBSD_Strike_Effects.SpawnFinalBurst(Projectile.Center, lockedDirection, totalStrikes);
            SoundEngine.PlaySound(SoundID.Item74 with
            {
                Volume = 1f,
                Pitch = -0.28f
            }, Projectile.Center);
        }
    }
}
