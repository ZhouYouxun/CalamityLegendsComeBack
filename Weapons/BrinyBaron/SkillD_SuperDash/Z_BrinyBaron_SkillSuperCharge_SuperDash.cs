using System;
using System.IO;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken;
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
        // =========================
        // 婵犮垹鐖㈤崟顐ゅ經闂傚倸鍟抽崺鏍敊瀹€鍕櫖婵娅曢幆鍫ユ煕?-> 闂備礁銇樼粈渚€鎮?-> 闂佹椿鍓﹂崜娑楃昂婵☆偅婢樼€氼剟藝?-> 闂佸搫鍊甸弲婊冾焽閸愵喖绀冮柟缁樺笒閻?        // =========================
        private enum SuperDashPhase : byte
        {
            Charging,
            Locked,
            Teleporting,
            Striking,
            ImpactDrifting,
            FinalPause,
            FinalExecutionStrike
        }

        private const int ChargeFrames = 120;
        private const int TeleportWindupFrames = 1;
        private const int NormalStrikeCount = 24;
        private const int StartingStrikeFrames = 15;
        private const int EndingStrikeFrames = 4;
        private const int FocusDashFrames = 18;
        private const int StartingImpactDriftFrames = 12;
        private const int EndingImpactDriftFrames = 4;
        private const int FinalPauseFrames = 20;
        private const int FinalExecutionFrames = 9;
        private const float ChargeHoldDistance = 22f;
        private const float LockHoldDistance = 28f;
        private const float StrikeHoldDistance = 34f;
        private const float HitSlowStartFactor = 0.4f;
        private const float HitSlowEndFactor = 0.15f;
        private const float FinalStrikeExitSpeed = 4.75f;
        private const float ChargeTurnRate = 0.14f;
        private const float LockTurnRate = 0.24f;
        private const float StrikeCollisionWidth = 84f;
        private const float FinalStrikeCollisionWidth = 150f;
        private const float TeleportRadiusBase = 280f;
        private const float TeleportRadiusWave = 46f;
        private const float DashOvershootDistance = 220f;
        private const float StrikeSlashDamageFactor = 0.75f;
        private const float StrikeSlashScale = 1.45f;
        private const float ExecutionGlowGain = 0.16f;
        private const float ExecutionGlowMax = 1.4f;
        private const float ExecutionGlowDecay = 0.008f;
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
        private bool initialized;
        private bool hadLockedTarget;
        private bool impactTriggeredThisStrike;
        private bool finishedNormally;
        private bool focusDashMode;
        private bool finalMarkSpawned;
        private float executionGlowIntensity;
        private Vector2 lockedDirection = Vector2.UnitX;
        private Vector2 focusPoint;
        private Vector2 strikeStart;
        private Vector2 strikeEnd;
        private Vector2 collisionStart;
        private Vector2 collisionEnd;
        private Vector2 impactDriftVelocity;

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

        public override bool? CanDamage() =>
            phase == SuperDashPhase.Striking ||
            phase == SuperDashPhase.ImpactDrifting ||
            phase == SuperDashPhase.FinalExecutionStrike
                ? null
                : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (phase != SuperDashPhase.Striking &&
                phase != SuperDashPhase.ImpactDrifting &&
                phase != SuperDashPhase.FinalExecutionStrike)
            {
                return false;
            }

            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                collisionStart,
                collisionEnd,
                GetCurrentCollisionWidth() * Projectile.scale,
                ref collisionPoint);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)phase);
            writer.Write(phaseTimer);
            writer.Write(totalStrikes);
            writer.Write(strikeIndex);
            writer.Write(targetNpcIndex);
            writer.Write(initialized);
            writer.Write(hadLockedTarget);
            writer.Write(impactTriggeredThisStrike);
            writer.Write(finishedNormally);
            writer.Write(focusDashMode);
            writer.Write(finalMarkSpawned);
            writer.Write(executionGlowIntensity);
            writer.WriteVector2(lockedDirection);
            writer.WriteVector2(focusPoint);
            writer.WriteVector2(strikeStart);
            writer.WriteVector2(strikeEnd);
            writer.WriteVector2(collisionStart);
            writer.WriteVector2(collisionEnd);
            writer.WriteVector2(impactDriftVelocity);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            phase = (SuperDashPhase)reader.ReadByte();
            phaseTimer = reader.ReadInt32();
            totalStrikes = reader.ReadInt32();
            strikeIndex = reader.ReadInt32();
            targetNpcIndex = reader.ReadInt32();
            initialized = reader.ReadBoolean();
            hadLockedTarget = reader.ReadBoolean();
            impactTriggeredThisStrike = reader.ReadBoolean();
            finishedNormally = reader.ReadBoolean();
            focusDashMode = reader.ReadBoolean();
            finalMarkSpawned = reader.ReadBoolean();
            executionGlowIntensity = reader.ReadSingle();
            lockedDirection = reader.ReadVector2();
            focusPoint = reader.ReadVector2();
            strikeStart = reader.ReadVector2();
            strikeEnd = reader.ReadVector2();
            collisionStart = reader.ReadVector2();
            collisionEnd = reader.ReadVector2();
            impactDriftVelocity = reader.ReadVector2();
        }

        public override void AI()
        {
            Player owner = Owner;
            if (!owner.active || owner.dead)
            {
                AbortAndKill(restartCooldown: true);
                return;
            }

            Projectile.timeLeft = 2;

            if (!initialized)
                Initialize(owner);

            UpdateFocusPoint(owner);
            DecayExecutionGlow();
            MaintainCameraLock(owner);

            NPC target = ResolveTarget(owner);

            switch (phase)
            {
                case SuperDashPhase.Charging:
                    // ===== 闂佽棄鍟€氼剚鎱ㄨ箛娑樺珘闁绘洖鍊荤粣妤呮煙闂€鎰厡闁汇垹顭烽獮宥夊礈瑜庣粣妤呮煛瀹ュ懏鎼愰柤鑽ゅ枔缁棃顢涘鍛劸闂傚倸娲ゅú銈夋儓?=====
                    DoCharging(owner, target);
                    break;

                case SuperDashPhase.Locked:
                    // ===== 闂備礁銇樼粈渚€鎮炬ィ鍐ㄥ珘闁绘洖鍊荤粣妤冪磼濞戞﹩妲哥紒鍓佸枛閹娊鍩℃笟鍥ф櫃閻庡綊娼荤粻鎾诲极椤撶姵鍏滄い鏃€顑欓崥鍥ь熆鐠哄搫顏╅柛?=====
                    DoLocked(owner, target);
                    break;

                case SuperDashPhase.Teleporting:
                    if (target is null && !focusDashMode)
                    {
                        CancelLockedDashAndRefundCooldown();
                        return;
                    }

                    DoTeleportWindup(owner, target);
                    break;

                case SuperDashPhase.Striking:
                    if (target is null && !focusDashMode)
                    {
                        CancelLockedDashAndRefundCooldown();
                        return;
                    }

                    // ===== 闂佸搫鍊甸弲婊冾焽閸愵喖瀚夐柣鏇炲€荤粣妤呮偨椤栥倕顩繛鐓庡暣閹娊鍩℃笟鍥ф櫃婵°倕鍊归敃銏ゅ焵椤掑倸鏋旈柍瑙勭⊕濞煎宕堕埡鍌滅崶闂?=====
                    DoStrike(owner, target);
                    break;

                case SuperDashPhase.ImpactDrifting:
                    if (target is null && !focusDashMode)
                    {
                        CancelLockedDashAndRefundCooldown();
                        return;
                    }

                    DoImpactDrift(owner, target);
                    break;

                case SuperDashPhase.FinalPause:
                    if (target is null)
                    {
                        CancelLockedDashAndRefundCooldown();
                        return;
                    }

                    DoFinalPause(owner, target);
                    break;

                case SuperDashPhase.FinalExecutionStrike:
                    if (target is null)
                    {
                        CancelLockedDashAndRefundCooldown();
                        return;
                    }

                    DoFinalExecutionStrike(owner, target);
                    break;
            }
        }

        private void Initialize(Player owner)
        {
            // ===== 闂佸憡甯楃换鍌烇綖閹版澘绀岄柡宓椒娴烽梺褰掓敱缁嬫挻鎱ㄩ幖浣哥畱濞撴艾锕︾粈澶愭煕濞嗘ü绨绘繝鈧鍫熷仺闁绘梻顭堥悘鍥煕濮橆剚鎹ｆ繛鍫熷灩缁參顢栫捄顭戜紘闁汇埄鍨庨崟顐熸寖闁荤偞绋戞總鏃傜博閺夋垟鏋?=====
            initialized = true;
            phase = SuperDashPhase.Charging;
            phaseTimer = 0;
            strikeIndex = 0;
            totalStrikes = NormalStrikeCount;
            targetNpcIndex = -1;
            hadLockedTarget = false;
            impactTriggeredThisStrike = false;
            finishedNormally = false;
            focusDashMode = false;
            finalMarkSpawned = false;
            executionGlowIntensity = 0f;
            lockedDirection = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(DefaultDirection);
            focusPoint = Main.MouseWorld;
            strikeStart = owner.Center;
            strikeEnd = owner.Center;
            collisionStart = owner.Center;
            collisionEnd = owner.Center;
            impactDriftVelocity = Vector2.Zero;
            Projectile.scale = 1f;
            Projectile.Center = owner.RotatedRelativePoint(owner.MountedCenter, true) + lockedDirection * ChargeHoldDistance;
            Projectile.rotation = lockedDirection.ToRotation() + MathHelper.PiOver4;
            Projectile.netUpdate = true;

            BBSD_ChargeBegan_Effects.SpawnChargeStartEffects(Projectile, owner);

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
            // ===== 闂備礁銇樼粈渚€鎮炬ィ鍐ㄨЕ閹肩补鈧櫕閿柣鐘遍檷閸婃洜绱炵€ｎ喖绀堢€广儱娲︾粣妤呮煛瀹ュ懏绁紒閬嶄憾閹锋垿宕熼銏╂綕闂佸搫鐗忛崰搴ㄥ垂椤栫偛鐭楃€广儱鎷嬪Σ濠氭⒑閹绘帞校闁哄苯锕﹂幏鐘诲礋椤忓嫬鎸?=====
            // ===== 一旦选定就咬死不放：无论跑多远、无论哪个阶段，只有目标死亡才会换 =====
            if (BBSuperDashTargeting.IsTargetValid(targetNpcIndex))
                return Main.npc[targetNpcIndex];

            // ===== 目标死亡/尚未选定：立刻重新索敌，锁不到任何目标才返回 null =====
            if (Main.myPlayer == Projectile.owner)
            {
                int nextTarget = BBSuperDashTargeting.FindBestTargetIndex(owner, focusPoint);
                if (nextTarget != targetNpcIndex)
                {
                    targetNpcIndex = nextTarget;
                    Projectile.netUpdate = true;
                }
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
            BBSD_Charge_Effects.SpawnChargingEffects(Projectile, owner, focusPoint, target, chargeCompletion, phaseTimer);
            ApplyChargeShake(owner, chargeCompletion);
            Lighting.AddLight(WeaponTip, new Vector3(0.12f, 0.38f, 0.52f) * (0.55f + chargeCompletion * 0.65f));

            if (phaseTimer % 12 == 0)
            {
                float pitch = MathHelper.Lerp(-0.4f, 0.4f, chargeCompletion);
                SoundEngine.PlaySound(SoundID.Item13 with
                {
                    Volume = 0.45f + chargeCompletion * 0.45f,
                    Pitch = pitch
                }, owner.Center);
            }
            if (phaseTimer % 20 == 0)
            {
                SoundEngine.PlaySound(SoundID.Splash with
                {
                    Volume = 0.3f + chargeCompletion * 0.4f,
                    Pitch = MathHelper.Lerp(-0.2f, 0.3f, chargeCompletion)
                }, owner.Center);
            }

            if (phaseTimer < ChargeFrames)
                return;

            phase = SuperDashPhase.Locked;
            phaseTimer = 0;
            Projectile.netUpdate = true;

            BBSD_ChargeFiniah_Effects.SpawnChargeReadyEffects(Projectile, owner);

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
                CancelLockedDashAndRefundCooldown();
                return;
            }

            Vector2 desiredDirection = target is not null
                ? (target.Center - owner.MountedCenter).SafeNormalize(lockedDirection)
                : (focusPoint - owner.MountedCenter).SafeNormalize(lockedDirection);

            RotateHeldDirectionToward(desiredDirection, LockTurnRate);
            ApplyHeldBlade(owner, lockedDirection, LockHoldDistance, 1.12f, 0.15f);

            owner.immune = true;
            owner.immuneTime = 2;

            BBSD_Lock_Effects.SpawnLockingEffects(Projectile, owner, focusPoint, target, phaseTimer, hadLockedTarget);
            Lighting.AddLight(WeaponTip, new Vector3(0.14f, 0.44f, 0.6f) * 0.78f);

            if (Main.myPlayer != Projectile.owner)
                return;

            if (!Main.mouseLeft || !Main.mouseLeftRelease || owner.mouseInterface || Main.blockMouse || Main.mapFullscreen)
                return;

            if (target is not null)
                BeginTeleport(owner, target);
            else
                BeginFocusDash(owner);
        }

        private void BeginTeleport(Player owner, NPC target)
        {
            // ===== 濠殿噯绲界换鎴︻敃婵傜妫橀柍杞扮濮ｅ﹪鏌涢幘宕団枌缂佽鲸绻堝畷妤呭醇濠靛洩鍚梺鐑╂櫓閸犳鎮ラ敐澶婂窛闁靛鍎茬痪顖炴⒑椤愩埄妲风紒鏂跨摠缁嬪顢旈崶銊ュ姍闂?=====
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
            impactDriftVelocity = Vector2.Zero;

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

        private void BeginFocusDash(Player owner)
        {
            focusDashMode = true;
            strikeIndex = 0;
            impactTriggeredThisStrike = false;
            strikeStart = owner.Center;
            lockedDirection = (focusPoint - owner.Center).SafeNormalize(lockedDirection);
            strikeEnd = strikeStart + lockedDirection * (DashOvershootDistance + 420f);
            collisionStart = strikeStart;
            collisionEnd = strikeStart;
            impactDriftVelocity = Vector2.Zero;
            phase = SuperDashPhase.Striking;
            phaseTimer = 0;
            Projectile.friendly = true;
            Projectile.netUpdate = true;

            BBSD_Strike_Effects.SpawnStrikeLaunchEffects(Projectile, strikeStart, focusPoint, lockedDirection, 0);
            SoundEngine.PlaySound(SoundID.Item71 with
            {
                Volume = 1f,
                Pitch = -0.2f
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
            int dashFrames = focusDashMode ? FocusDashFrames : GetCurrentStrikeFrames();
            float progress = Utils.GetLerpValue(0f, dashFrames, phaseTimer, true);
            float easedProgress = 1f - (float)Math.Pow(1f - progress, 2f);
            Vector2 currentCenter = Vector2.Lerp(strikeStart, strikeEnd, easedProgress);
            Vector2 dashVelocity = currentCenter - previousCenter;

            if (phaseTimer == 1)
                FireForwardShuriken(owner, dashVelocity.SafeNormalize(lockedDirection));

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

            float swingProgress = Utils.GetLerpValue(0f, dashFrames, phaseTimer, true);
            float swingDir = (strikeIndex % 2 == 0 ? 1f : -1f);
            float easedSwing = CalamityUtils.ExpInOutEasing(swingProgress, 1);
            float swingAngleOffset = MathHelper.Lerp(-MathHelper.Pi, MathHelper.Pi, easedSwing) * swingDir;
            Vector2 swingDirection = lockedDirection.RotatedBy(swingAngleOffset);

            ApplyHeldBlade(owner, swingDirection, StrikeHoldDistance, 1.24f + executionGlowIntensity * 0.05f, 0f);
            BBSD_Strike_Effects.SpawnStrikeTravelEffects(Projectile, previousCenter, currentCenter, lockedDirection, phaseTimer, strikeIndex);
            Lighting.AddLight(WeaponTip, new Vector3(0.2f, 0.62f, 0.82f) * (1f + executionGlowIntensity * 0.3f));

            if (!impactTriggeredThisStrike && target is not null && SegmentHitsTarget(target, previousCenter, currentCenter))
            {
                HandleStrikeImpact(target);
                if (phase != SuperDashPhase.Striking)
                    return;
            }

            if (phaseTimer < dashFrames)
                return;

            AdvanceAfterStrike(owner, target);
        }

        private void DoImpactDrift(Player owner, NPC target)
        {
            Vector2 previousCenter = owner.Center;

            phaseTimer++;
            owner.Center += impactDriftVelocity;
            owner.velocity = impactDriftVelocity;
            owner.immune = true;
            owner.immuneTime = 2;
            owner.noKnockback = true;

            collisionStart = previousCenter;
            collisionEnd = owner.Center;
            Projectile.velocity = impactDriftVelocity;

            if (impactDriftVelocity.LengthSquared() > 1f)
                lockedDirection = impactDriftVelocity.SafeNormalize(lockedDirection);

            float swingDir = (strikeIndex % 2 == 0 ? 1f : -1f);
            float swingAngleOffset = MathHelper.Pi * swingDir;
            Vector2 swingDirection = lockedDirection.RotatedBy(swingAngleOffset);

            ApplyHeldBlade(owner, swingDirection, StrikeHoldDistance, 1.24f + executionGlowIntensity * 0.06f, 0f);
            BBSD_Strike_Effects.SpawnStrikeTravelEffects(Projectile, previousCenter, owner.Center, lockedDirection, GetCurrentStrikeFrames() + phaseTimer, strikeIndex);
            Lighting.AddLight(WeaponTip, new Vector3(0.18f, 0.54f, 0.74f) * (1f + executionGlowIntensity * 0.35f));

            impactDriftVelocity *= GetCurrentImpactDriftDecay();

            if (phaseTimer < GetCurrentImpactDriftFrames())
                return;

            AdvanceAfterStrike(owner, target);
        }

        private void BeginFinalPause(Player owner, NPC target)
        {
            phase = SuperDashPhase.FinalPause;
            phaseTimer = 0;
            Projectile.friendly = false;
            impactTriggeredThisStrike = false;
            lockedDirection = (target.Center - owner.Center).SafeNormalize(lockedDirection);
            strikeStart = target.Center - lockedDirection * 148f;
            strikeEnd = target.Center;
            collisionStart = strikeStart;
            collisionEnd = strikeStart;
            executionGlowIntensity = MathHelper.Max(executionGlowIntensity, 1.12f);
            TeleportOwner(owner, strikeStart);
            Projectile.netUpdate = true;

            BBSD_Strike_Effects.SpawnFinalPauseStartEffects(Projectile, target.Center, lockedDirection);
            SoundEngine.PlaySound(SoundID.Item122 with
            {
                Volume = 1f,
                Pitch = -0.42f
            }, target.Center);
        }

        private void DoFinalPause(Player owner, NPC target)
        {
            phaseTimer++;
            Projectile.friendly = false;
            Projectile.velocity = Vector2.Zero;

            Vector2 pausePosition = target.Center - lockedDirection.SafeNormalize(DefaultDirection) * 148f;
            owner.Center = Vector2.Lerp(owner.Center, pausePosition, 0.42f);
            owner.velocity = Vector2.Zero;
            owner.immune = true;
            owner.immuneTime = 2;
            owner.noKnockback = true;

            float pulse = 0.5f + 0.5f * (float)Math.Sin(phaseTimer * 0.42f);
            ApplyHeldBlade(owner, lockedDirection, StrikeHoldDistance + 8f, 1.38f + pulse * 0.08f + executionGlowIntensity * 0.04f, 0.45f);
            BBSD_Strike_Effects.SpawnFinalPauseEffects(Projectile, target.Center, lockedDirection, phaseTimer, FinalPauseFrames);
            Lighting.AddLight(WeaponTip, new Vector3(0.22f, 0.7f, 0.92f) * (1.2f + pulse * 0.35f));

            if (phaseTimer < FinalPauseFrames)
                return;

            BeginFinalExecutionStrike(owner, target);
        }

        private void BeginFinalExecutionStrike(Player owner, NPC target)
        {
            phase = SuperDashPhase.FinalExecutionStrike;
            phaseTimer = 0;
            impactTriggeredThisStrike = false;
            Projectile.friendly = true;
            lockedDirection = (target.Center - owner.Center).SafeNormalize(lockedDirection);
            strikeStart = owner.Center;
            strikeEnd = target.Center + lockedDirection * (DashOvershootDistance + 180f);
            collisionStart = strikeStart;
            collisionEnd = strikeStart;
            impactDriftVelocity = Vector2.Zero;
            executionGlowIntensity = ExecutionGlowMax;
            Projectile.netUpdate = true;

            BBSD_Strike_Effects.SpawnFinalExecutionLaunchEffects(Projectile, strikeStart, target.Center, lockedDirection);
            SoundEngine.PlaySound(SoundID.Item71 with
            {
                Volume = 1.25f,
                Pitch = -0.45f
            }, strikeStart);
        }

        private void DoFinalExecutionStrike(Player owner, NPC target)
        {
            Vector2 previousCenter = owner.Center;

            phaseTimer++;
            float progress = Utils.GetLerpValue(0f, FinalExecutionFrames, phaseTimer, true);
            float easedProgress = 1f - (float)Math.Pow(1f - progress, 3f);
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

            float swingSign = strikeIndex % 2 == 0 ? 1f : -1f;
            float swingAngleOffset = MathHelper.Lerp(-MathHelper.PiOver2, MathHelper.PiOver2, progress) * swingSign;
            Vector2 swingDirection = lockedDirection.RotatedBy(swingAngleOffset);

            ApplyHeldBlade(owner, swingDirection, StrikeHoldDistance + 10f, 1.64f + executionGlowIntensity * 0.05f, 0f);
            BBSD_Strike_Effects.SpawnStrikeTravelEffects(Projectile, previousCenter, currentCenter, lockedDirection, totalStrikes + phaseTimer, totalStrikes);
            Lighting.AddLight(WeaponTip, new Vector3(0.28f, 0.82f, 1f) * 1.75f);

            if (!impactTriggeredThisStrike && SegmentHitsTarget(target, previousCenter, currentCenter))
                HandleFinalExecutionImpact(target);

            if (phaseTimer < FinalExecutionFrames)
                return;

            if (!finalMarkSpawned)
                SpawnFinalMark(target);

            ApplyFinalStrikeSlowdown(owner);
            finishedNormally = true;
            Projectile.Kill();
        }

        private void HandleFinalExecutionImpact(NPC target)
        {
            impactTriggeredThisStrike = true;
            executionGlowIntensity = ExecutionGlowMax;
            SpawnImpactAfterimage(Owner, lockedDirection * FinalStrikeExitSpeed);
            SpawnFinalMark(target);
            BBSD_Strike_Effects.SpawnFinalExecutionImpactEffects(Projectile, target.Center, lockedDirection);

            float distanceFactor = Utils.GetLerpValue(1400f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                Main.LocalPlayer.Calamity().GeneralScreenShakePower,
                28f * distanceFactor);

            if (Main.myPlayer == Projectile.owner)
                Owner.GetModPlayer<BBSuperDashCameraPlayer>().AddImpactShake(28f);

            SoundEngine.PlaySound(SoundID.Item84 with
            {
                Volume = 1.25f,
                Pitch = -0.35f
            }, target.Center);
            SoundEngine.PlaySound(SoundID.Item14 with
            {
                Volume = 1.1f,
                Pitch = -0.48f
            }, target.Center);

            Projectile.netUpdate = true;
        }

        private void SpawnFinalMark(NPC target)
        {
            if (finalMarkSpawned)
                return;

            finalMarkSpawned = true;
            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<BBSD_Final_INV>(),
                Math.Max(1, (int)(Projectile.damage * 0.28f)),
                Projectile.knockBack * 0.2f,
                Projectile.owner,
                target.whoAmI,
                lockedDirection.ToRotation());
        }

        private void AdvanceAfterStrike(Player owner, NPC target)
        {
            Projectile.friendly = false;
            impactDriftVelocity = Vector2.Zero;
            strikeIndex++;
            Projectile.netUpdate = true;

            if (focusDashMode)
            {
                finishedNormally = true;
                Projectile.Kill();
                return;
            }

            if (target is null)
            {
                CancelLockedDashAndRefundCooldown();
                return;
            }

            if (strikeIndex >= totalStrikes)
            {
                BeginFinalPause(owner, target);
                return;
            }

            BeginTeleport(owner, target);
        }

        private void ApplyFinalStrikeSlowdown(Player owner)
        {
            Vector2 exitDirection = owner.velocity.LengthSquared() > 0.01f
                ? owner.velocity.SafeNormalize(lockedDirection)
                : lockedDirection.SafeNormalize(DefaultDirection);

            owner.velocity = exitDirection * FinalStrikeExitSpeed;
            Projectile.velocity = owner.velocity;
        }

        private bool SegmentHitsTarget(NPC target, Vector2 lineStart, Vector2 lineEnd)
        {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                target.Hitbox.TopLeft(),
                target.Hitbox.Size(),
                lineStart,
                lineEnd,
                GetCurrentCollisionWidth() * Projectile.scale,
                ref collisionPoint);
        }

        private void HandleStrikeImpact(NPC target)
        {
            impactTriggeredThisStrike = true;
            Projectile.netUpdate = true;

            Vector2 strikeDirection = lockedDirection.SafeNormalize(DefaultDirection);
            executionGlowIntensity = MathHelper.Clamp(executionGlowIntensity + ExecutionGlowGain, 0f, ExecutionGlowMax);
            SpawnImpactShurikenBurst(target, strikeDirection);
            Vector2 afterimageVelocity = Projectile.velocity;
            SpawnImpactAfterimage(Owner, afterimageVelocity);

            BBSD_Strike_Effects.SpawnStrikeImpactEffects(Projectile, target.Center, strikeDirection, strikeIndex, totalStrikes);
            BBSD_Strike_Effects.SpawnStrikeSlowdownEffects(Projectile, target.Center, strikeDirection, GetStrikeProgress(), executionGlowIntensity);
            float shakePower = MathHelper.Lerp(15f, 8f, GetStrikeProgress());
            float distanceFactor = Utils.GetLerpValue(1000f, 0f, Projectile.Distance(Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                Main.LocalPlayer.Calamity().GeneralScreenShakePower,
                shakePower * distanceFactor);

            if (Main.myPlayer == Projectile.owner)
                Owner.GetModPlayer<BBSuperDashCameraPlayer>().AddImpactShake(shakePower);

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

            AdvanceAfterStrike(Owner, target);
        }

        private void SpawnImpactAfterimage(Player owner, Vector2 driftVelocity)
        {
            if (Main.dedServ)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                owner.Center,
                driftVelocity,
                ModContent.ProjectileType<BBSuperDashAfterimage>(),
                0,
                0f,
                Projectile.owner,
                lockedDirection.ToRotation() + MathHelper.PiOver4,
                Projectile.scale,
                lockedDirection.X >= 0f ? 1 : -1);
        }

        private void FireForwardShuriken(Player owner, Vector2 dashDir)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            if (!focusDashMode && strikeIndex % 3 != 0)
                return;

            Vector2 shootDirection = dashDir.SafeNormalize(lockedDirection);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                owner.MountedCenter + shootDirection * 34f,
                shootDirection * 15.5f,
                ModContent.ProjectileType<BrinyBaron_RightClick_Shuriken>(),
                Math.Max(1, (int)(Projectile.damage * 0.35f)),
                Projectile.knockBack * 0.4f,
                Projectile.owner,
                0f);
        }

        private void SpawnImpactShurikenBurst(NPC target, Vector2 strikeDirection)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 forward = strikeDirection.SafeNormalize(DefaultDirection);
            int shurikenCount = GetStrikeProgress() < 0.55f ? 2 : 1;
            for (int i = 0; i < shurikenCount; i++)
            {
                float angle = forward.ToRotation() + MathHelper.TwoPi * i / shurikenCount + Main.rand.NextFloat(-0.08f, 0.08f);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(12.5f, 15.5f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center + velocity.SafeNormalize(forward) * 18f,
                    velocity,
                    ModContent.ProjectileType<BrinyBaron_RightClick_Shuriken>(),
                    Math.Max(1, (int)(Projectile.damage * 0.28f)),
                    Projectile.knockBack * 0.35f,
                    Projectile.owner,
                0f);
            }
        }

        private float GetStrikeProgress()
        {
            if (totalStrikes <= 1)
                return 1f;

            return MathHelper.Clamp(strikeIndex / (float)(totalStrikes - 1), 0f, 1f);
        }

        private int GetCurrentStrikeFrames()
        {
            float frames = MathHelper.Lerp(StartingStrikeFrames, EndingStrikeFrames, GetStrikeProgress());
            return Math.Max(1, (int)Math.Round(frames));
        }

        private int GetCurrentImpactDriftFrames()
        {
            float frames = MathHelper.Lerp(StartingImpactDriftFrames, EndingImpactDriftFrames, GetStrikeProgress());
            return Math.Max(1, (int)Math.Round(frames));
        }

        private float GetCurrentImpactDriftDecay()
        {
            return (float)Math.Pow(HitSlowEndFactor / HitSlowStartFactor, 1f / GetCurrentImpactDriftFrames());
        }

        private float GetCurrentCollisionWidth()
        {
            return phase == SuperDashPhase.FinalExecutionStrike ? FinalStrikeCollisionWidth : StrikeCollisionWidth;
        }

        private void DecayExecutionGlow()
        {
            executionGlowIntensity = MathHelper.Max(0f, executionGlowIntensity - ExecutionGlowDecay);

            if (phase == SuperDashPhase.FinalPause)
                executionGlowIntensity = MathHelper.Max(executionGlowIntensity, 1.12f);
            else if (phase == SuperDashPhase.FinalExecutionStrike)
                executionGlowIntensity = MathHelper.Max(executionGlowIntensity, 1.28f);
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

            if (phase == SuperDashPhase.Teleporting ||
                phase == SuperDashPhase.Striking ||
                phase == SuperDashPhase.ImpactDrifting ||
                phase == SuperDashPhase.FinalPause ||
                phase == SuperDashPhase.FinalExecutionStrike)
            {
                // ===== 闁告瑯浜濆﹢渚€鎯囬悢鍓插妧閺夆晜绋戦崣鍡樺緞閸曨偄鐏夐梻鍐煐椤斿矂骞嶅澶嬫暁闂傗偓濠婂啨浠?=====
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

        private void AbortAndKill(bool restartCooldown, bool clearCooldown = false)
        {
            BBSuperDashCooldownPlayer cooldownPlayer = Owner.GetModPlayer<BBSuperDashCooldownPlayer>();

            if (clearCooldown)
                cooldownPlayer.ClearCooldown();
            else if (restartCooldown)
                cooldownPlayer.StartCooldown();

            Projectile.Kill();
        }

        private void CancelLockedDashAndRefundCooldown()
        {
            AbortAndKill(restartCooldown: false, clearCooldown: true);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (phase == SuperDashPhase.Locked)
                BBSD_Lock_Effects.DrawLockBeam(Owner.MountedCenter, CurrentTarget?.Center ?? focusPoint, 1f);

            if (phase == SuperDashPhase.Charging || phase == SuperDashPhase.Locked)
                BBSD_Lock_Effects.DrawTargetingReticle(focusPoint, CurrentTarget, phase == SuperDashPhase.Locked && CurrentTarget is not null);

            Texture2D weaponTexture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = weaponTexture.Frame();
            
            float chargeCompletion = Utils.GetLerpValue(0f, ChargeFrames, phaseTimer, true);
            Vector2 jitter = Vector2.Zero;
            if (phase == SuperDashPhase.Charging)
            {
                float jitterMax = chargeCompletion * 8f;
                jitter = Main.rand.NextVector2Circular(jitterMax, jitterMax);
            }
            else if (phase == SuperDashPhase.Locked)
            {
                jitter = Main.rand.NextVector2Circular(1.2f, 1.2f);
            }

            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY) + jitter;
            Vector2 origin = frame.Size() * 0.5f;
            float executionGlow = MathHelper.Clamp(executionGlowIntensity / ExecutionGlowMax, 0f, 1f);
            Color bladeColor = Color.Lerp(Projectile.GetAlpha(lightColor), Color.White, executionGlow * 0.35f);
            bool facingLeft = BladeDirection.X < 0f;
            SpriteEffects effects = facingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float drawRotation = Projectile.rotation + (facingLeft ? MathHelper.PiOver2 : 0f);
            bool drawingExecutionAura =
                phase == SuperDashPhase.Striking ||
                phase == SuperDashPhase.ImpactDrifting ||
                phase == SuperDashPhase.FinalPause ||
                phase == SuperDashPhase.FinalExecutionStrike;
            bool drawingSlashSwoosh =
                phase == SuperDashPhase.Striking ||
                phase == SuperDashPhase.ImpactDrifting ||
                phase == SuperDashPhase.FinalExecutionStrike;

            if (phase == SuperDashPhase.Charging && chargeCompletion > 0f)
            {
                for (int i = 0; i < 4; i++)
                {
                    float angle = MathHelper.TwoPi * i / 4f + Main.GlobalTimeWrappedHourly * 4f;
                    Vector2 outlineOffset = angle.ToRotationVector2() * (chargeCompletion * 5f);
                    Main.EntitySpriteDraw(
                        weaponTexture,
                        drawPosition + outlineOffset,
                        frame,
                        new Color(95, 206, 255, 0) * (chargeCompletion * 0.75f),
                        drawRotation,
                        origin,
                        Projectile.scale * (1f + chargeCompletion * 0.06f),
                        effects,
                        0f);
                }
            }

            if (drawingExecutionAura)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
                Main.EntitySpriteDraw(
                    bloom,
                    drawPosition,
                    null,
                    new Color(80, 220, 255, 0) * (0.18f + executionGlow * 0.44f),
                    Main.GlobalTimeWrappedHourly,
                    bloom.Size() * 0.5f,
                    Projectile.scale * (0.22f + executionGlow * 0.16f),
                    SpriteEffects.None,
                    0f);

                if (drawingSlashSwoosh)
                {
                    Texture2D swoosh = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge").Value;
                    float swingSign = strikeIndex % 2 == 0 ? 1f : -1f;
                    float swooshRotation = Projectile.rotation - MathHelper.PiOver4 + MathHelper.PiOver2 * swingSign;
                    float swooshScale = phase == SuperDashPhase.FinalExecutionStrike ? 1.08f : 0.65f;

                    Main.EntitySpriteDraw(
                        swoosh,
                        drawPosition,
                        null,
                        Color.DeepSkyBlue with { A = 0 } * (0.42f + executionGlow * 0.24f),
                        swooshRotation,
                        swoosh.Size() * 0.5f,
                        Projectile.scale * swooshScale,
                        SpriteEffects.None,
                        0f);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                // Draw ghost outlines for trailing aura effect
                Texture2D ghost = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaronGoest").Value;
                for (int i = 0; i < 18; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 18f).ToRotationVector2() * (4.2f + executionGlow * 2.5f);
                    Main.EntitySpriteDraw(
                        ghost,
                        drawPosition + offset,
                        frame,
                        Color.Aqua with { A = 0 } * (0.1f + executionGlow * 0.1f),
                        drawRotation,
                        origin,
                        Projectile.scale * Vector2.One,
                        effects,
                        0f);
                }

                int outlineCount = phase == SuperDashPhase.FinalExecutionStrike ? 8 : 4;
                for (int i = 0; i < outlineCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / outlineCount + Main.GlobalTimeWrappedHourly * 4f;
                    Vector2 outlineOffset = angle.ToRotationVector2() * (2f + executionGlow * 2f);
                    Main.EntitySpriteDraw(
                        weaponTexture,
                        drawPosition + outlineOffset,
                        frame,
                        new Color(105, 220, 255, 0) * (0.36f + executionGlow * 0.24f),
                        drawRotation,
                        origin,
                        Projectile.scale * (1.04f + executionGlow * 0.04f),
                        effects,
                        0f);
                }
            }

            Main.EntitySpriteDraw(
                weaponTexture,
                drawPosition,
                frame,
                bladeColor,
                drawRotation,
                origin,
                Projectile.scale,
                effects,
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
