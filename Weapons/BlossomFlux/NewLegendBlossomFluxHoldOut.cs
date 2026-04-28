using CalamityLegendsComeBack.Weapons.BlossomFlux.AimScope;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow;
using CalamityLegendsComeBack.Weapons.Visuals;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    internal sealed class NewLegendBlossomFluxHoldOut : BaseIdleHoldoutProjectile, ILocalizedModType
    {
        private const int ParallelArrowCount = 3;
        private const int RecoveryBurstGroupCount = 4;
        private const int BreakthroughFireInterval = 7;
        private const int RecoveryBurstInterval = 7;
        private const int RecoveryCyclePause = 40;
        private const int ReconFireInterval = 40;
        private const int BombardFireInterval = 4;
        private const int PlagueFireInterval = 0;
        private const int BombardAmmoSavePercent = 90;
        private const int PlagueAmmoSavePercent = 95;
        private const int BreakthroughChargeReductionPerUnlock = 7;
        private const int BreakthroughLoadFlashFrames = 14;
        private const int LeftOutlinePulseFrames = 10;
        private const int RightOutlinePulseFrames = 22;
        private const float ParallelSpacing = 18f;
        private const float BreakthroughSpeed = 19f;
        private const float ReconSpread = 0.34f;
        private const float BombardSpeed = 12f;
        private const float BreakthroughArrowSpread = MathHelper.Pi / 11f;

        private const float IdleOffsetLength = 22f;
        private const int ReloadFrames = 18;
        private const int MaxChargeFrames = 60;
        private const int MinBreakthroughChargeFrames = 24;
        private const float ReadyPulseScale = 0.45f;
        private const float RightClickBaseDamageMultiplier = 3f;
        private const float RailgunSightSize = 9f;
        private const float RailgunMaxSightAngle = MathHelper.Pi * (2f / 3f);
        private static readonly SoundStyle PlagueUseSound = new("CalamityMod/Sounds/Item/PhotoUseSound") { Volume = 0.28f, PitchVariance = 0.16f };

        private int burstGroupsStarted;
        private int leftBurstTimer;
        private int leftShotsFired;
        private bool leftHeldLastFrame;

        private int reloadTimer;
        private int chargeTimer;
        private int chargeFxTimer;
        private int breakthroughLoadedArrows;
        private int breakthroughLoadFlashTimer;
        private int leftOutlinePulseTimer;
        private int rightOutlinePulseTimer;
        private bool rightChargeActive;
        private bool readyBurstPlayed;
        private bool releasedShot;

        // 瞄准镜弹幕类型缓存，便于统一生成与清理
        private static int AimScopeProjectileType => ModContent.ProjectileType<BFAimScope>();
        private float offsetLengthFromArm = IdleOffsetLength;
        private float extraFrontArmRotation;
        private float extraBackArmRotation;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/NewLegendBlossomFlux";
        public override int AssociatedItemID => ModContent.ItemType<NewLegendBlossomFlux>();
        public override int IntendedProjectileType => ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>();

        private BlossomFluxChloroplastPresetType CurrentPreset => Owner.GetModPlayer<BFRightUIPlayer>().CurrentPreset;
        private bool BreakthroughChargeActive => rightChargeActive && CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_ABreak;
        private int BreakthroughMaxLoadedArrows => Utils.Clamp(Owner.GetModPlayer<BFRightUIPlayer>().UnlockedPresetCount, 1, 5);
        private int BreakthroughFramesPerArrow => Math.Max(MinBreakthroughChargeFrames, MaxChargeFrames - (BreakthroughMaxLoadedArrows - 1) * BreakthroughChargeReductionPerUnlock);
        private float BreakthroughCurrentArrowCompletion => MathHelper.Clamp(chargeTimer / (float)BreakthroughFramesPerArrow, 0f, 1f);
        private float ChargeCompletion => BreakthroughChargeActive
            ? MathHelper.Clamp((breakthroughLoadedArrows + (breakthroughLoadedArrows >= BreakthroughMaxLoadedArrows ? 0f : BreakthroughCurrentArrowCompletion)) / BreakthroughMaxLoadedArrows, 0f, 1f)
            : MathHelper.Clamp(chargeTimer / (float)MaxChargeFrames, 0f, 1f);
        private bool ChargeReady => BreakthroughChargeActive ? breakthroughLoadedArrows > 0 : chargeTimer >= MaxChargeFrames && readyBurstPlayed;
        private Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        private Vector2 GunTipPosition => Projectile.Center + AimDirection * 42f;
        private Vector2 ChargeFxAnchor => Projectile.Center - AimDirection * MathHelper.Lerp(11f, 6f, ChargeCompletion) + new Vector2(0f, MathHelper.Lerp(-7f, -4f, ChargeCompletion));
        private Color PresetColor => BFArrowCommon.GetPresetColor(CurrentPreset);
        private Color AccentColor => BFArrowCommon.GetPresetAccentColor(CurrentPreset);
        private bool BombardChargePoseActive => rightChargeActive && CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb;
        private bool RecoveryChargePoseActive => rightChargeActive && CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_BRecov;
        private bool SpecialAimScopeAnchorActive => BombardChargePoseActive || RecoveryChargePoseActive;

        internal Color GetAimScopeMainColor() => Color.Lerp(PresetColor, AccentColor, 0.18f);

        internal Color GetAimScopeAccentColor() => Color.Lerp(AccentColor, Color.White, 0.25f);

        internal Vector2 GetAimScopeDirection()
        {
            if (RecoveryChargePoseActive)
                return GetRecoverySkyAimDirection();

            if (BombardChargePoseActive)
                return GetBombardSkyAimDirection();

            Vector2 baseAnchor = GetAimScopeBaseAnchor();
            Vector2 scopeDirection = GetCurrentMouseWorld() - baseAnchor;
            if (scopeDirection == Vector2.Zero)
                scopeDirection = Vector2.UnitX * Owner.direction;

            return scopeDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
        }

        internal Vector2 GetAimScopeCenter(Vector2 scopeDirection)
        {
            if (!SpecialAimScopeAnchorActive)
                return Owner.MountedCenter + scopeDirection * BFAimScope.WeaponLength;

            return GetAimScopeBaseAnchor() - scopeDirection * 18f;
        }

        internal Vector2 GetAimScopeSparkOrigin(Vector2 scopeDirection)
        {
            if (!SpecialAimScopeAnchorActive)
                return Owner.MountedCenter + scopeDirection * BFAimScope.WeaponLength;

            return GetAimScopeBaseAnchor();
        }

        public override void SetDefaults()
        {
            Projectile.width = 78;
            Projectile.height = 78;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override bool? CanDamage() => false;

        public override void SafeAI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != AssociatedItemID)
            {
                Projectile.Kill();
                return;
            }

            if (HasActiveEXWeapon())
            {
                KillAimScopeProjectiles();
                Projectile.Kill();
                return;
            }

            Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);
            Projectile.knockBack = Owner.HeldItem.knockBack;

            UpdateOutlinePulseTimers();

            BFRightUIPlayer rightUIPlayer = Owner.GetModPlayer<BFRightUIPlayer>();

            if (Main.myPlayer == Projectile.owner)
                HandleOwnerLogic(rightUIPlayer);

            UpdateIdlePose();
            UpdateHeldProjectileVariables();
            ManipulatePlayerVariables();
        }

        private void HandleOwnerLogic(BFRightUIPlayer rightUIPlayer)
        {
            rightUIPlayer.ProcessRightClickState(HasActiveSelectionPanel(Owner));

            if (rightUIPlayer.ShortTapReleasedThisFrame && !rightChargeActive)
                ToggleSelectionPanel();

            if (rightUIPlayer.LongHoldReachedThisFrame && !rightChargeActive)
                BeginRightCharge();

            UpdateRightChargeState(rightUIPlayer);

            if (!rightChargeActive)
                HandleLeftClickInput();
            else
                ResetBurstState();
        }

        private void HandleLeftClickInput()
        {
            bool validLeftInput =
                Owner.HeldItem.type == AssociatedItemID &&
                !Owner.noItems &&
                !Owner.CCed &&
                !HasActiveSelectionPanel(Owner) &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !Owner.GetModPlayer<BFRightUIPlayer>().RightMouseHeld &&
                Main.mouseLeft &&
                !Owner.mouseInterface &&
                !(Main.playerInventory && Main.HoverItem.type == Owner.HeldItem.type);

            if (!validLeftInput)
            {
                ResetBurstState();
                return;
            }

            if (!leftHeldLastFrame)
            {
                leftHeldLastFrame = true;
                burstGroupsStarted = 0;
                leftShotsFired = 0;
                leftBurstTimer = GetInitialLeftFireDelay();
                if (leftBurstTimer > 0)
                    return;
            }

            if (leftBurstTimer > 0 && --leftBurstTimer > 0)
                return;

            if (!TryPickLeftAmmo(out int projectileType, out float speed, out int damage, out float knockback))
            {
                leftBurstTimer = 4;
                return;
            }

            FireCurrentPresetLeftAttack(Projectile.GetSource_FromThis(), projectileType, speed, damage, knockback);
            ScheduleNextLeftFire();
        }

        private int GetInitialLeftFireDelay() => CurrentPreset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => BreakthroughFireInterval,
            BlossomFluxChloroplastPresetType.Chlo_BRecov => RecoveryBurstInterval,
            BlossomFluxChloroplastPresetType.Chlo_CDetec => ReconFireInterval,
            BlossomFluxChloroplastPresetType.Chlo_DBomb => BombardFireInterval,
            BlossomFluxChloroplastPresetType.Chlo_EPlague => PlagueFireInterval,
            _ => BreakthroughFireInterval
        };

        private bool TryPickLeftAmmo(out int projectileType, out float speed, out int damage, out float knockback)
        {
            bool dontConsume = CurrentPreset switch
            {
                BlossomFluxChloroplastPresetType.Chlo_DBomb => Main.rand.Next(100) < BombardAmmoSavePercent,
                BlossomFluxChloroplastPresetType.Chlo_EPlague => Main.rand.Next(100) < PlagueAmmoSavePercent,
                _ => false
            };

            return Owner.PickAmmo(Owner.HeldItem, out projectileType, out speed, out damage, out knockback, out _, dontConsume);
        }

        private void FireCurrentPresetLeftAttack(IEntitySource source, int projectileType, float speed, int damage, float knockback)
        {
            TriggerTacticalOutlinePulse(rightClick: false);

            switch (CurrentPreset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                    FireBreakthroughShot(source, projectileType, speed, damage, knockback);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                    FireRecoveryVolley(source, projectileType, speed, damage, knockback);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                    FireReconScatter(source, projectileType, speed, damage, knockback);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                    FireBombardRain(source, projectileType, speed, damage, knockback);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                    FirePlagueStream(source, speed, damage, knockback);
                    break;
            }
        }

        private void ScheduleNextLeftFire()
        {
            leftShotsFired++;

            switch (CurrentPreset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                    burstGroupsStarted++;
                    if (burstGroupsStarted >= RecoveryBurstGroupCount)
                    {
                        burstGroupsStarted = 0;
                        leftBurstTimer = RecoveryCyclePause;
                    }
                    else
                    {
                        leftBurstTimer = RecoveryBurstInterval;
                    }

                    break;

                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                    leftBurstTimer = BreakthroughFireInterval;
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                    leftBurstTimer = ReconFireInterval;
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                    leftBurstTimer = BombardFireInterval;
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                    leftBurstTimer = PlagueFireInterval;
                    break;
            }
        }

        private void BeginRightCharge()
        {
            CloseSelectionPanel();
            rightChargeActive = true;
            reloadTimer = ReloadFrames;
            chargeTimer = 0;
            chargeFxTimer = 0;
            breakthroughLoadedArrows = 0;
            breakthroughLoadFlashTimer = 0;
            readyBurstPlayed = false;
            releasedShot = false;

            // 进入右键蓄力时，确保场上只有一个瞄准镜弹幕
            EnsureAimScopeExists();

            SoundEngine.PlaySound(SoundID.Item149 with { Volume = 0.55f, Pitch = -0.2f }, Projectile.Center);
        }

        private void UpdateRightChargeState(BFRightUIPlayer rightUIPlayer)
        {
            if (!rightChargeActive)
                return;

            if (rightUIPlayer.LongHoldReleasedThisFrame)
            {
                if (ChargeReady)
                    HandleRelease();

                CancelRightCharge();
                return;
            }

            chargeFxTimer++;
            Lighting.AddLight(GunTipPosition, PresetColor.ToVector3() * (0.16f + ChargeCompletion * 0.25f));

            if (reloadTimer > 0)
            {
                UpdateReloadAnimation();
                return;
            }

            if (BreakthroughChargeActive)
            {
                UpdateBreakthroughChargeState();
                return;
            }

            if (chargeTimer < MaxChargeFrames)
            {
                chargeTimer++;
                UpdateChargingAnimation();
            }
            else
            {
                UpdateChargedAnimation();
            }
        }

        private void UpdateBreakthroughChargeState()
        {
            if (breakthroughLoadFlashTimer > 0)
                breakthroughLoadFlashTimer--;

            if (breakthroughLoadedArrows >= BreakthroughMaxLoadedArrows)
            {
                UpdateChargedAnimation();
                return;
            }

            chargeTimer++;
            UpdateChargingAnimation();

            if (chargeTimer < BreakthroughFramesPerArrow)
                return;

            chargeTimer = 0;
            breakthroughLoadedArrows++;
            breakthroughLoadFlashTimer = BreakthroughLoadFlashFrames;

            if (breakthroughLoadedArrows >= BreakthroughMaxLoadedArrows)
                PlayChargeReadyBurst();
            else
                PlayBreakthroughArrowLoadedBurst();
        }

        private void CancelRightCharge()
        {
            rightChargeActive = false;
            reloadTimer = 0;
            chargeTimer = 0;
            chargeFxTimer = 0;
            breakthroughLoadedArrows = 0;
            breakthroughLoadFlashTimer = 0;
            readyBurstPlayed = false;
            releasedShot = false;

            // 退出右键蓄力时，立刻移除瞄准镜弹幕
            KillAimScopeProjectiles();
        }

        private void UpdateIdlePose()
        {
            if (rightChargeActive)
                return;

            offsetLengthFromArm = MathHelper.Lerp(offsetLengthFromArm, IdleOffsetLength, 0.16f);
            extraFrontArmRotation = MathHelper.Lerp(extraFrontArmRotation, 0f, 0.16f);
            extraBackArmRotation = MathHelper.Lerp(extraBackArmRotation, 0f, 0.16f);
        }

        private void UpdateHeldProjectileVariables()
        {
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            if (Main.myPlayer == Projectile.owner)
            {
                Vector2 desiredVelocity = GetDesiredHoldoutDirection(armPosition);
                Vector2 oldVelocity = Projectile.velocity;
                Projectile.velocity = oldVelocity == Vector2.Zero ? desiredVelocity : Vector2.Lerp(oldVelocity, desiredVelocity, 0.35f);
                if (Vector2.DistanceSquared(oldVelocity, Projectile.velocity) > 0.0001f)
                    Projectile.netUpdate = true;
            }

            Projectile.Center = armPosition + AimDirection * offsetLengthFromArm + GetHoldoutPositionOffset();
            Projectile.rotation = AimDirection.ToRotation();
            Projectile.direction = Math.Abs(Projectile.velocity.X) <= 0.05f ? Owner.direction : (Projectile.velocity.X >= 0f ? 1 : -1);
            Projectile.spriteDirection = Projectile.direction;
            Projectile.timeLeft = 2;
        }

        private void ManipulatePlayerVariables()
        {
            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;

            float armRotation = Projectile.rotation - MathHelper.PiOver2;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation + extraFrontArmRotation);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRotation + extraBackArmRotation);
        }

        private void UpdateOutlinePulseTimers()
        {
            if (leftOutlinePulseTimer > 0)
                leftOutlinePulseTimer--;

            if (rightOutlinePulseTimer > 0)
                rightOutlinePulseTimer--;
        }

        private void TriggerTacticalOutlinePulse(bool rightClick)
        {
            if (rightClick)
                rightOutlinePulseTimer = RightOutlinePulseFrames;
            else
                leftOutlinePulseTimer = 0;
        }

        private float GetLeftAttackBuildGlow()
        {
            if (!leftHeldLastFrame || rightChargeActive || HasActiveSelectionPanel(Owner))
                return 0f;

            int interval = GetCurrentLeftGlowInterval();
            if (interval <= 0)
                return 0.12f + 0.06f * (0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 13f + Projectile.identity * 0.31f));

            float build = 1f - leftBurstTimer / (float)interval;
            float intervalWeight = MathHelper.Clamp(interval / (float)ReconFireInterval, 0.16f, 1f);
            intervalWeight = MathHelper.Lerp(0.16f, 1f, (float)Math.Pow(intervalWeight, 1.35f));
            return MathHelper.Clamp(build, 0f, 1f) * intervalWeight;
        }

        private int GetCurrentLeftGlowInterval()
        {
            return CurrentPreset switch
            {
                BlossomFluxChloroplastPresetType.Chlo_ABreak => BreakthroughFireInterval,
                BlossomFluxChloroplastPresetType.Chlo_BRecov => leftBurstTimer > RecoveryBurstInterval ? RecoveryCyclePause : RecoveryBurstInterval,
                BlossomFluxChloroplastPresetType.Chlo_CDetec => ReconFireInterval,
                BlossomFluxChloroplastPresetType.Chlo_DBomb => BombardFireInterval,
                BlossomFluxChloroplastPresetType.Chlo_EPlague => PlagueFireInterval,
                _ => BreakthroughFireInterval
            };
        }

        private void UpdateReloadAnimation()
        {
            float reloadProgress = 1f - reloadTimer / (float)ReloadFrames;
            if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb)
            {
                extraFrontArmRotation = MathHelper.Lerp(0.16f, 0.04f, reloadProgress);
                extraBackArmRotation = MathHelper.Lerp(0.26f, 0.1f, reloadProgress);
                offsetLengthFromArm = MathHelper.Lerp(IdleOffsetLength - 2f, IdleOffsetLength + 2f, reloadProgress);
            }
            else if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_BRecov)
            {
                extraFrontArmRotation = MathHelper.Lerp(-0.02f, -0.18f, reloadProgress);
                extraBackArmRotation = MathHelper.Lerp(0.08f, 0.2f, reloadProgress);
                offsetLengthFromArm = MathHelper.Lerp(IdleOffsetLength + 1f, IdleOffsetLength + 7f, reloadProgress);
            }
            else
            {
                extraFrontArmRotation = -0.05f * (1f - reloadProgress);
                extraBackArmRotation = 0.04f * (1f - reloadProgress);
                offsetLengthFromArm = MathHelper.Lerp(IdleOffsetLength - 10f, IdleOffsetLength, reloadProgress);
            }

            if (chargeFxTimer % 4 == 0)
                SpawnReloadDust();

            if (reloadTimer == 1)
                SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.45f, Pitch = 0.1f }, GunTipPosition);

            reloadTimer--;
        }

        private void UpdateChargingAnimation()
        {
            if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb)
            {
                offsetLengthFromArm = MathHelper.Lerp(IdleOffsetLength + 1f, IdleOffsetLength + 4f, ChargeCompletion);
                extraFrontArmRotation = MathHelper.Lerp(0.03f, -0.06f, ChargeCompletion);
                extraBackArmRotation = MathHelper.Lerp(0.12f, 0.22f, ChargeCompletion);
            }
            else if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_BRecov)
            {
                offsetLengthFromArm = MathHelper.Lerp(IdleOffsetLength + 5f, IdleOffsetLength + 12f, ChargeCompletion);
                extraFrontArmRotation = MathHelper.Lerp(-0.14f, -0.28f, ChargeCompletion);
                extraBackArmRotation = MathHelper.Lerp(0.1f, 0.24f, ChargeCompletion);
            }
            else
            {
                offsetLengthFromArm = MathHelper.Lerp(IdleOffsetLength - 2f, IdleOffsetLength - 8f, ChargeCompletion);
                extraFrontArmRotation = -0.08f * ChargeCompletion;
                extraBackArmRotation = 0.05f * ChargeCompletion;
            }

            SpawnChargingDust();

            if (chargeTimer % 10 == 0)
                SpawnChargeCircle();

            if (!BreakthroughChargeActive && chargeTimer >= MaxChargeFrames && !readyBurstPlayed)
                PlayChargeReadyBurst();
        }

        private void UpdateChargedAnimation()
        {
            if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb)
            {
                offsetLengthFromArm = IdleOffsetLength + 4f;
                extraFrontArmRotation = -0.06f;
                extraBackArmRotation = 0.22f;
            }
            else if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_BRecov)
            {
                offsetLengthFromArm = IdleOffsetLength + 12f;
                extraFrontArmRotation = -0.28f;
                extraBackArmRotation = 0.24f;
            }
            else
            {
                offsetLengthFromArm = IdleOffsetLength - 8f;
                extraFrontArmRotation = -0.08f;
                extraBackArmRotation = 0.05f;
            }

            if (!readyBurstPlayed)
                PlayChargeReadyBurst();

            if (chargeFxTimer % 3 == 0)
                SpawnReadyIdleDust();
        }

        private void HandleRelease()
        {
            if (releasedShot || !ChargeReady)
                return;

            releasedShot = true;
            extraFrontArmRotation = 0f;
            extraBackArmRotation = 0f;

            SpawnReleasePulse();
            TriggerTacticalOutlinePulse(rightClick: true);
            ReleaseChargedShot(CurrentPreset, ChargeCompletion);
            Owner.GetModPlayer<BFEXPlayer>().GainEX(3);
        }

        private void ReleaseChargedShot(BlossomFluxChloroplastPresetType preset, float chargeCompletion)
        {
            switch (preset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                    FireBreakthroughSpecialArrows();
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                    FireSpecialArrow(chargeCompletion, ModContent.ProjectileType<BFArrow_BRecov>(), 19.5f, 0.94f);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                    FireSpecialArrow(chargeCompletion, ModContent.ProjectileType<BFArrow_CDetec>(), 18.75f, 0.92f);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                    FireBombardSpecialArrow(chargeCompletion, ModContent.ProjectileType<BFArrow_DBomb>(), 19.2f, 0.88f);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                    FireSpecialArrow(chargeCompletion, ModContent.ProjectileType<BFArrow_EPlague>(), 18.6f, 0.98f);
                    break;
            }
        }

        private void FireSpecialArrow(float chargeCompletion, int projectileType, float baseSpeed, float damageMultiplier)
        {
            float speed = MathHelper.Lerp(baseSpeed * 0.76f, baseSpeed * 1.22f, chargeCompletion);
            int damage = (int)(Projectile.damage * RightClickBaseDamageMultiplier * MathHelper.Lerp(0.8f, 1.35f, chargeCompletion) * damageMultiplier);
            float knockback = Projectile.knockBack * MathHelper.Lerp(0.85f, 1.15f, chargeCompletion);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition,
                AimDirection * speed,
                projectileType,
                damage,
                knockback,
                Projectile.owner);
        }

        private void FireBreakthroughSpecialArrows()
        {
            int arrowCount = Math.Max(1, breakthroughLoadedArrows);
            float speed = 21.6f * 1.22f;
            int damage = (int)(Projectile.damage * RightClickBaseDamageMultiplier * 1.35f * 1.12f);
            float knockback = Projectile.knockBack * 1.15f;

            for (int i = 0; i < arrowCount; i++)
            {
                float spreadAngle = GetBreakthroughArrowAngle(i, arrowCount);
                Vector2 shootVelocity = AimDirection.RotatedBy(spreadAngle) * speed;

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTipPosition,
                    shootVelocity,
                    ModContent.ProjectileType<BFArrow_ABreak>(),
                    damage,
                    knockback,
                    Projectile.owner);
            }
        }

        private static float GetBreakthroughArrowAngle(int index, int arrowCount)
        {
            if (arrowCount <= 1)
                return 0f;

            float halfSpread = BreakthroughArrowSpread * (arrowCount - 1) * 0.5f;
            return MathHelper.Lerp(-halfSpread, halfSpread, index / (arrowCount - 1f));
        }

        private void FireBombardSpecialArrow(float chargeCompletion, int projectileType, float baseSpeed, float damageMultiplier)
        {
            float speed = MathHelper.Lerp(baseSpeed * 1.52f, baseSpeed * 2.24f, chargeCompletion);
            int damage = (int)(Projectile.damage * RightClickBaseDamageMultiplier * MathHelper.Lerp(0.8f, 1.35f, chargeCompletion) * damageMultiplier);
            float knockback = Projectile.knockBack * MathHelper.Lerp(0.85f, 1.15f, chargeCompletion);
            Vector2 bombardTarget = GetCurrentMouseWorld();

            int projectileIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition,
                AimDirection * speed,
                projectileType,
                damage,
                knockback,
                Projectile.owner);

            if (!BFArrowCommon.InBounds(projectileIndex, Main.maxProjectiles))
                return;

            if (Main.projectile[projectileIndex].ModProjectile is BFArrow_DBomb bombardArrow)
                bombardArrow.ConfigureBombardTarget(bombardTarget);

            Main.projectile[projectileIndex].netUpdate = true;
        }

        private void SpawnReloadDust()
        {
            Vector2 backward = -AimDirection;
            Vector2 side = backward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + side * Main.rand.NextFloat(-7f, 7f),
                    DustID.GemEmerald,
                    backward.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.8f, 1.6f),
                    120,
                    Color.Lerp(PresetColor, Color.White, 0.25f),
                    Main.rand.NextFloat(0.85f, 1.15f));
                dust.noGravity = true;
            }
        }

        private void SpawnChargingDust()
        {
            if (chargeFxTimer % 2 != 0)
                return;

            Vector2 inwardPosition = ChargeFxAnchor + Main.rand.NextVector2Circular(16f, 16f);
            Vector2 inwardVelocity = (ChargeFxAnchor - inwardPosition).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.9f, 2.1f);
            bool bombard = CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb;
            int primaryDustType = bombard ? DustID.Torch : DustID.GemEmerald;
            Color primaryColor = bombard
                ? Color.Lerp(Color.Goldenrod, Color.Khaki, 0.45f + 0.2f * ChargeCompletion)
                : Color.Lerp(PresetColor, Color.White, 0.2f + 0.3f * ChargeCompletion);

            Dust dust = Dust.NewDustPerfect(
                inwardPosition,
                primaryDustType,
                inwardVelocity,
                100,
                primaryColor,
                Main.rand.NextFloat(0.8f, 1.25f));
            dust.noGravity = true;

            if (Main.rand.NextBool(3))
            {
                Dust glowDust = Dust.NewDustPerfect(
                    ChargeFxAnchor + Main.rand.NextVector2Circular(5f, 5f),
                    bombard ? DustID.FireworksRGB : DustID.TerraBlade,
                    Main.rand.NextVector2Circular(0.6f, 0.6f),
                    100,
                    bombard ? Color.Lerp(Color.Khaki, PresetColor, 0.35f) : PresetColor,
                    Main.rand.NextFloat(0.9f, 1.35f));
                glowDust.noGravity = true;
            }
        }

        private void SpawnChargeCircle()
        {
            int points = 10;
            float radius = MathHelper.Lerp(14f, 26f, ChargeCompletion);
            bool bombard = CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb;

            for (int i = 0; i < points; i++)
            {
                float angle = MathHelper.TwoPi * i / points + Main.GlobalTimeWrappedHourly * 2.4f;
                Vector2 offset = angle.ToRotationVector2() * radius;

                Dust dust = Dust.NewDustPerfect(
                    ChargeFxAnchor + offset,
                    bombard ? DustID.Torch : DustID.GemEmerald,
                    -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.1f, 2.4f),
                    100,
                    bombard ? Color.Lerp(Color.Goldenrod, Color.Khaki, 0.35f) : Color.Lerp(PresetColor, Color.White, 0.35f),
                    Main.rand.NextFloat(0.85f, 1.2f));
                dust.noGravity = true;
            }
        }

        private void PlayChargeReadyBurst()
        {
            if (readyBurstPlayed)
                return;

            readyBurstPlayed = true;
            rightOutlinePulseTimer = Math.Max(rightOutlinePulseTimer, RightOutlinePulseFrames / 2);
            bool bombard = CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb;

            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f, Pitch = 0.25f }, GunTipPosition);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.4f, Pitch = -0.15f }, GunTipPosition);

            for (int i = 0; i < 20; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(1.5f, 4.2f);
                Dust dust = Dust.NewDustPerfect(
                    GunTipPosition,
                    bombard ? DustID.Torch : DustID.TerraBlade,
                    velocity,
                    100,
                    bombard ? Color.Lerp(Color.Goldenrod, Color.Khaki, 0.5f) : Color.Lerp(PresetColor, Color.White, 0.5f),
                    Main.rand.NextFloat(1f, 1.5f));
                dust.noGravity = true;
            }
        }

        private void PlayBreakthroughArrowLoadedBurst()
        {
            rightOutlinePulseTimer = Math.Max(rightOutlinePulseTimer, BreakthroughLoadFlashFrames);
            SoundEngine.PlaySound(SoundID.Item108 with { Volume = 0.22f, Pitch = 0.35f }, GunTipPosition);

            Color flashColor = new(124, 255, 136);
            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity =
                    AimDirection.RotatedByRandom(0.22f) * Main.rand.NextFloat(1.4f, 3.6f) +
                    Main.rand.NextVector2Circular(0.8f, 0.8f);

                Dust dust = Dust.NewDustPerfect(
                    GunTipPosition,
                    DustID.TerraBlade,
                    velocity,
                    100,
                    Color.Lerp(flashColor, Color.White, Main.rand.NextFloat(0.12f, 0.35f)),
                    Main.rand.NextFloat(0.82f, 1.18f));
                dust.noGravity = true;
            }
        }

        private void SpawnReadyIdleDust()
        {
            Vector2 driftVelocity = -Vector2.UnitY.RotatedByRandom(0.28f) * Main.rand.NextFloat(0.8f, 1.7f);

            Dust dust = Dust.NewDustPerfect(
                GunTipPosition + Main.rand.NextVector2Circular(4f, 4f),
                DustID.GemEmerald,
                driftVelocity,
                100,
                Color.Lerp(PresetColor, Color.White, ReadyPulseScale),
                Main.rand.NextFloat(0.95f, 1.35f));
            dust.noGravity = true;

            if (Main.rand.NextBool(4))
            {
                Dust highlight = Dust.NewDustPerfect(
                    GunTipPosition + Main.rand.NextVector2Circular(2f, 2f),
                    DustID.TerraBlade,
                    driftVelocity * 0.65f,
                    100,
                    Color.White,
                    Main.rand.NextFloat(0.75f, 1.05f));
                highlight.noGravity = true;
            }
        }

        private void SpawnReleasePulse()
        {
            if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb)
            {
                SoundStyle bombardFire = new("CalamityMod/Sounds/Item/LauncherHeavyShot");
                SoundEngine.PlaySound(bombardFire with { Volume = 0.82f, Pitch = -0.08f, PitchVariance = 0.08f }, GunTipPosition);

                for (int i = 0; i < 5; i++)
                {
                    float pulseScale = Main.rand.NextFloat(0.22f, 0.42f);
                    DirectionalPulseRing pulse = new(
                        GunTipPosition,
                        (AimDirection * 18f).RotatedByRandom(0.25f) * Main.rand.NextFloat(0.55f, 1.15f),
                        Color.Lerp(Color.Goldenrod, Color.Khaki, Main.rand.NextFloat(0.25f, 0.7f)) * 0.82f,
                        Vector2.One,
                        pulseScale - 0.25f,
                        pulseScale,
                        0f,
                        20);
                    GeneralParticleHandler.SpawnParticle(pulse);
                }
            }
            else
            {
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.72f, Pitch = -0.05f }, GunTipPosition);
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity =
                    AimDirection.RotatedByRandom(0.22f) * Main.rand.NextFloat(2.5f, 6f) +
                    Main.rand.NextVector2Circular(1f, 1f);

                Dust dust = Dust.NewDustPerfect(
                    GunTipPosition,
                    CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb ? DustID.Torch : DustID.GemEmerald,
                    velocity,
                    100,
                    CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb ? Color.Lerp(Color.Goldenrod, Color.Khaki, 0.4f) : Color.Lerp(PresetColor, Color.White, 0.4f),
                    Main.rand.NextFloat(0.95f, 1.4f));
                dust.noGravity = true;
            }
        }

        //private void DrawRailgunTelegraph()
        //{
        //    float chargeVisual = MathHelper.SmoothStep(0f, 1f, ChargeCompletion);
        //    if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb)
        //    {
        //        DrawBombardTelegraph(chargeVisual);
        //        return;
        //    }

        //    Color scopeColor = Color.Lerp(PresetColor, AccentColor, 0.36f);
        //    //DrawScopedAimTelegraph(scopeColor, chargeVisual, RailgunMaxSightAngle, RailgunSightSize, 0.04f, 7f);
        //}

        //private void DrawBombardTelegraph(float chargeVisual)
        //{
        //    Color scopeColor = Color.Lerp(Color.Goldenrod, Color.Khaki, 0.55f);
        //    //DrawScopedAimTelegraph(scopeColor, chargeVisual, RailgunMaxSightAngle * 0.9f, RailgunSightSize + 28f, 0.048f, 7.4f);
        //}

        private void DrawScopedAimTelegraph(Color scopeColor, float chargeVisual, float maxSightAngle, float sightsSize, float minimumResolution, float laserStrength)
        {
            Texture2D scopeTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/BlossomFlux/BFAimScope").Value;
            Vector2 telegraphCenter = GunTipPosition - AimDirection * 32f - Main.screenPosition;
            float telegraphOpacity = MathHelper.Clamp(0.22f + chargeVisual * 0.88f, 0f, 1f) * (readyBurstPlayed ? 1f : 0.92f);
            float sightsResolution = MathHelper.Lerp(minimumResolution, 0.2f, Math.Min(chargeVisual * 1.5f, 1f));
            float scopedSize = sightsSize * MathHelper.Lerp(1f, 1.5f, chargeVisual);
            float spread = (1f - chargeVisual) * maxSightAngle;
            float halfAngle = spread * 0.5f;

            Effect spreadEffect = Filters.Scene["CalamityMod:SpreadTelegraph"].GetShader().Shader;
            spreadEffect.Parameters["centerOpacity"].SetValue(0.9f);
            spreadEffect.Parameters["mainOpacity"].SetValue(telegraphOpacity);
            spreadEffect.Parameters["halfSpreadAngle"].SetValue(halfAngle);
            spreadEffect.Parameters["edgeColor"].SetValue(scopeColor.ToVector3());
            spreadEffect.Parameters["centerColor"].SetValue(scopeColor.ToVector3());
            spreadEffect.Parameters["edgeBlendLength"].SetValue(0.07f);
            spreadEffect.Parameters["edgeBlendStrength"].SetValue(8f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Additive,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                spreadEffect,
                Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                scopeTexture,
                telegraphCenter,
                null,
                Color.White,
                Projectile.rotation,
                scopeTexture.Size() * 0.5f,
                scopedSize,
                SpriteEffects.None,
                0);

            Effect laserScopeEffect = Filters.Scene["CalamityMod:PixelatedSightLine"].GetShader().Shader;
            laserScopeEffect.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/CertifiedCrustyNoise").Value);
            laserScopeEffect.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -0.003f);
            laserScopeEffect.Parameters["mainOpacity"].SetValue(telegraphOpacity);
            laserScopeEffect.Parameters["Resolution"].SetValue(new Vector2(sightsResolution * scopedSize));
            laserScopeEffect.Parameters["laserAngle"].SetValue(-Projectile.rotation + halfAngle);
            laserScopeEffect.Parameters["laserWidth"].SetValue(0.0025f + (float)Math.Pow(chargeVisual, 5) * ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.002f + 0.002f));
            laserScopeEffect.Parameters["laserLightStrenght"].SetValue(laserStrength);
            laserScopeEffect.Parameters["color"].SetValue(scopeColor.ToVector3());
            laserScopeEffect.Parameters["darkerColor"].SetValue(Color.Black.ToVector3());
            laserScopeEffect.Parameters["bloomSize"].SetValue(0.06f);
            laserScopeEffect.Parameters["bloomMaxOpacity"].SetValue(0.4f);
            laserScopeEffect.Parameters["bloomFadeStrenght"].SetValue(7f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Additive,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                laserScopeEffect,
                Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                scopeTexture,
                telegraphCenter,
                null,
                Color.White,
                0f,
                scopeTexture.Size() * 0.5f,
                scopedSize,
                SpriteEffects.None,
                0);

            laserScopeEffect.Parameters["laserAngle"].SetValue(-Projectile.rotation - halfAngle);

            Main.EntitySpriteDraw(
                scopeTexture,
                telegraphCenter,
                null,
                Color.White,
                0f,
                scopeTexture.Size() * 0.5f,
                sightsSize,
                SpriteEffects.None,
                0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);
        }

        private void FireBreakthroughShot(IEntitySource source, int projectileType, float speed, int damage, float knockback)
        {
            Vector2 shootVelocity = GetAimVelocity(Math.Max(speed, BreakthroughSpeed));
            Vector2 normal = shootVelocity.SafeNormalize(Vector2.UnitX * Owner.direction).RotatedBy(MathHelper.PiOver2);
            Vector2 spawnPosition = GetShootOrigin(shootVelocity) + normal * Main.rand.NextFloat(-19f, 19f);

            SpawnLeftProjectile(source, spawnPosition, shootVelocity, projectileType, damage, knockback, CurrentPreset);
            SoundEngine.PlaySound(SoundID.Item5 with { Pitch = Main.rand.NextFloat(-0.08f, 0.08f), Volume = 0.86f }, Owner.Center);
        }

        private void FireRecoveryVolley(IEntitySource source, int projectileType, float speed, int damage, float knockback)
        {
            Vector2 velocity = GetAimVelocity(speed);
            FireParallelVolley(source, velocity, projectileType, damage, knockback, CurrentPreset);
            SoundEngine.PlaySound(SoundID.Item8 with { Pitch = 0.2f + burstGroupsStarted * 0.04f, Volume = 0.58f }, Owner.Center);
        }

        private void FireReconScatter(IEntitySource source, int projectileType, float speed, int damage, float knockback)
        {
            Vector2 baseVelocity = GetAimVelocity(speed);
            Vector2 forward = baseVelocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 origin = GetShootOrigin(baseVelocity);
            int arrowCount = Main.rand.Next(4, 7);

            for (int i = 0; i < arrowCount; i++)
            {
                float completion = arrowCount == 1 ? 0.5f : i / (arrowCount - 1f);
                float spread = MathHelper.Lerp(-ReconSpread, ReconSpread, completion) + Main.rand.NextFloat(-0.045f, 0.045f);
                Vector2 shotVelocity = baseVelocity.RotatedBy(spread) * Main.rand.NextFloat(0.92f, 1.08f);
                Vector2 spawnPosition = origin + normal * Main.rand.NextFloat(-13f, 13f) + forward * Main.rand.NextFloat(-3f, 8f);

                SpawnLeftProjectile(source, spawnPosition, shotVelocity, projectileType, damage, knockback, CurrentPreset);
            }

            SoundEngine.PlaySound(SoundID.Item9 with { Pitch = 0.35f, Volume = 0.42f }, Owner.Center);
        }

        private void FireBombardRain(IEntitySource source, int projectileType, float speed, int damage, float knockback)
        {
            Vector2 mouseWorld = GetCurrentMouseWorld();
            float rainSpeed = Math.Max(BombardSpeed, speed);

            if (leftShotsFired % 2 == 0)
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.54f, PitchVariance = 0.2f }, Owner.Center);

            for (int i = 0; i < 2; i++)
            {
                float horizontalOffset = Main.rand.NextFloat(-40f, 40f);
                Vector2 spawnPosition = new(
                    MathHelper.Lerp(mouseWorld.X, Owner.Center.X, 0.5f) + horizontalOffset,
                    Owner.Center.Y - Main.rand.NextFloat(560f, 660f) * Owner.gravDir);
                Vector2 targetPosition = mouseWorld + new Vector2(Main.rand.NextFloat(-30f, 30f), Main.rand.NextFloat(-12f, 28f) * Owner.gravDir);
                Vector2 shotVelocity = (targetPosition - spawnPosition).SafeNormalize(Vector2.UnitY * Owner.gravDir);
                shotVelocity = shotVelocity.RotatedBy(horizontalOffset * -0.004f) * rainSpeed;
                int shotDamage = (int)(damage * (i == 0 ? 1.15f : 1f));

                SpawnLeftProjectile(source, spawnPosition, shotVelocity, projectileType, shotDamage, knockback, CurrentPreset, noTileCollide: true);
            }
        }

        private void FirePlagueStream(IEntitySource source, float speed, int damage, float knockback)
        {
            Vector2 shootVelocity = GetAimVelocity(6f).RotatedByRandom(0.028f);
            Vector2 direction = shootVelocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 spawnPosition = GunTipPosition + direction * 8f + Main.rand.NextVector2Circular(2f, 2f);

            Projectile.NewProjectile(
                source,
                spawnPosition,
                shootVelocity,
                ModContent.ProjectileType<BFLeftPlagueFlame>(),
                Math.Max(1, (int)(damage * 0.42f)),
                knockback * 0.45f,
                Owner.whoAmI);

            if (leftShotsFired % 8 == 0)
                SoundEngine.PlaySound(PlagueUseSound, Owner.Center);
        }

        private void FireParallelVolley(IEntitySource source, Vector2 velocity, int projectileType, int damage, float knockback, BlossomFluxChloroplastPresetType preset)
        {
            Vector2 shootVelocity = velocity.SafeNormalize(Vector2.UnitX * Owner.direction) * velocity.Length();
            if (shootVelocity == Vector2.Zero)
                shootVelocity = Vector2.UnitX * Owner.direction * Owner.HeldItem.shootSpeed;

            Vector2 origin = GetShootOrigin(shootVelocity);
            Vector2 normal = shootVelocity.SafeNormalize(Vector2.UnitX * Owner.direction).RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < ParallelArrowCount; i++)
            {
                float offsetAmount = (i - (ParallelArrowCount - 1f) * 0.5f) * ParallelSpacing;
                Vector2 spawnPosition = origin + normal * offsetAmount;

                SpawnLeftProjectile(source, spawnPosition, shootVelocity, projectileType, damage, knockback, preset);
            }
        }

        private void SpawnLeftProjectile(IEntitySource source, Vector2 spawnPosition, Vector2 velocity, int projectileType, int damage, float knockback, BlossomFluxChloroplastPresetType preset, bool noTileCollide = false)
        {
            int tacticalArrowType = ModContent.ProjectileType<BFLeftTacticalArrow>();
            bool convertWoodenArrow = CalamityUtils.CheckWoodenAmmo(projectileType, Owner);
            int finalProjectileType = convertWoodenArrow ? tacticalArrowType : projectileType;
            float ai0 = finalProjectileType == tacticalArrowType ? (int)preset : 0f;

            int projectileIndex = Projectile.NewProjectile(source, spawnPosition, velocity, finalProjectileType, damage, knockback, Owner.whoAmI, ai0);
            if (!BFArrowCommon.InBounds(projectileIndex, Main.maxProjectiles))
                return;

            Projectile arrowProjectile = Main.projectile[projectileIndex];
            arrowProjectile.friendly = true;
            arrowProjectile.hostile = false;
            arrowProjectile.arrow = true;
            arrowProjectile.noDropItem = true;

            if (noTileCollide)
                arrowProjectile.tileCollide = false;

            if (finalProjectileType != tacticalArrowType)
            {
                arrowProjectile.extraUpdates++;
                BFArrowCommon.ForceLocalNPCImmunity(arrowProjectile, 10);
            }

            BFArrowCommon.TagBlossomFluxLeftArrow(arrowProjectile);
        }

        private Vector2 GetAimVelocity(float speed)
        {
            Vector2 aimDirection = GetCurrentMouseWorld() - Owner.RotatedRelativePoint(Owner.MountedCenter);
            if (aimDirection == Vector2.Zero)
                aimDirection = Vector2.UnitX * Owner.direction;

            return aimDirection.SafeNormalize(Vector2.UnitX * Owner.direction) * speed;
        }

        private Vector2 GetDesiredHoldoutDirection(Vector2 armPosition)
        {
            if (RecoveryChargePoseActive)
                return GetRecoverySkyAimDirection();

            if (BombardChargePoseActive)
                return GetBombardSkyAimDirection();

            Vector2 aimDirection = GetCurrentMouseWorld() - armPosition;
            if (aimDirection == Vector2.Zero)
                aimDirection = Vector2.UnitX * Owner.direction;

            return aimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
        }

        private Vector2 GetRecoverySkyAimDirection()
        {
            return (-Vector2.UnitY * Owner.gravDir).SafeNormalize(-Vector2.UnitY * Owner.gravDir);
        }

        private Vector2 GetBombardSkyAimDirection()
        {
            Vector2 mouseWorld = GetCurrentMouseWorld();
            Vector2 skyTarget = new Vector2(
                MathHelper.Lerp(mouseWorld.X, Owner.Center.X, 0.55f),
                Owner.Center.Y - 500f * Owner.gravDir);
            Vector2 aimDirection = skyTarget - Owner.Center;
            if (aimDirection == Vector2.Zero)
                aimDirection = -Vector2.UnitY * Owner.gravDir;

            return aimDirection.SafeNormalize(-Vector2.UnitY * Owner.gravDir);
        }

        private Vector2 GetHoldoutPositionOffset()
        {
            return Vector2.Zero;
        }

        private Vector2 GetAimScopeBaseAnchor()
        {
            if (!SpecialAimScopeAnchorActive)
                return Owner.MountedCenter;

            return GunTipPosition + AimDirection * 8f - new Vector2(0f, 4f * Owner.gravDir);
        }

        private Vector2 GetCurrentMouseWorld()
        {
            Vector2 aimTarget = Owner.Calamity().mouseWorld;
            if (aimTarget == Vector2.Zero)
                aimTarget = Main.MouseWorld;

            return aimTarget;
        }

        private Vector2 GetShootOrigin(Vector2 velocity)
        {
            Vector2 origin = Owner.RotatedRelativePoint(Owner.MountedCenter);
            Vector2 muzzleOffset = velocity.SafeNormalize(Vector2.UnitX * Owner.direction) * 34f;

            if (Collision.CanHit(origin, 0, 0, origin + muzzleOffset, 0, 0))
                origin += muzzleOffset;

            return origin;
        }

        private void ResetBurstState()
        {
            burstGroupsStarted = 0;
            leftBurstTimer = 0;
            leftShotsFired = 0;
            leftHeldLastFrame = false;
        }

        private static bool HasActiveSelectionPanel(Player player) =>
            FindOpenSelectionPanel(player) != null;

        private static Projectile FindOpenSelectionPanel(Player player)
        {
            int selectionPanelType = ModContent.ProjectileType<BFSelectionPanel>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != selectionPanelType)
                    continue;

                if (projectile.ai[0] == 1f || projectile.Opacity <= 0.02f)
                    continue;

                return projectile;
            }

            return null;
        }

        private void ToggleSelectionPanel()
        {
            int selectionPanelType = ModContent.ProjectileType<BFSelectionPanel>();
            Projectile openPanel = null;

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Owner.whoAmI || projectile.type != selectionPanelType)
                    continue;

                if (projectile.ai[0] != 1f && projectile.Opacity > 0.02f)
                {
                    openPanel = projectile;
                    continue;
                }

                projectile.Kill();
                projectile.netUpdate = true;
            }

            if (openPanel != null)
            {
                openPanel.ai[0] = 1f;
                openPanel.netUpdate = true;
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.5f }, Owner.Center);
                return;
            }

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.Center,
                Vector2.Zero,
                selectionPanelType,
                0,
                0f,
                Owner.whoAmI);

            SoundEngine.PlaySound(SoundID.MenuOpen with { Pitch = 0.1f, Volume = 0.55f }, Owner.Center);
        }

        // 生成瞄准镜弹幕；如果已经存在，就不重复生成
        private void EnsureAimScopeExists()
        {
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Owner.whoAmI || projectile.type != AimScopeProjectileType)
                    continue;

                // 已经有了就直接返回，避免重复生成
                return;
            }

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition,
                Vector2.Zero,
                AimScopeProjectileType,
                0,
                0f,
                Owner.whoAmI);
        }

        // 清理玩家当前持有的全部瞄准镜弹幕
        private void KillAimScopeProjectiles()
        {
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Owner.whoAmI || projectile.type != AimScopeProjectileType)
                    continue;

                projectile.Kill();
                projectile.netUpdate = true;
            }
        }

        private void CloseSelectionPanel()
        {
            int selectionPanelType = ModContent.ProjectileType<BFSelectionPanel>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Owner.whoAmI || projectile.type != selectionPanelType)
                    continue;

                if (projectile.ai[0] == 1f || projectile.Opacity <= 0.02f)
                {
                    projectile.Kill();
                    projectile.netUpdate = true;
                    continue;
                }

                projectile.ai[0] = 1f;
                projectile.netUpdate = true;
            }
        }

        private bool HasActiveEXWeapon()
        {
            int exWeaponType = ModContent.ProjectileType<BFEXWeapon>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.active && projectile.owner == Owner.whoAmI && projectile.type == exWeaponType)
                    return true;
            }

            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D weaponTexture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = weaponTexture.Size() * 0.5f;
            float rotation = Projectile.rotation;
            SpriteEffects effects = SpriteEffects.None;
            float time = Main.GlobalTimeWrappedHourly;
            float outlinePulse = 0.72f + 0.28f * (float)Math.Sin(time * 5.2f + Projectile.identity * 0.43f);
            float chargeGlow = rightChargeActive && reloadTimer <= 0 ? MathHelper.SmoothStep(0f, 1f, ChargeCompletion) : 0f;
            float leftOutlinePulse = leftOutlinePulseTimer / (float)LeftOutlinePulseFrames;
            float rightOutlinePulse = rightOutlinePulseTimer / (float)RightOutlinePulseFrames;
            float leftBuildGlow = GetLeftAttackBuildGlow();
            float tacticalOutlinePulse = Math.Max(Math.Max(leftOutlinePulse * 0.28f, rightOutlinePulse * 0.78f), leftBuildGlow);
            bool rightPulseDominates = rightOutlinePulse * 0.78f > leftBuildGlow;
            float glowStrength = 0.32f + outlinePulse * 0.1f + chargeGlow * 0.62f + leftBuildGlow * 0.42f;
            float glowRadius = MathHelper.Lerp(1.75f, 5.7f, chargeGlow) + outlinePulse * 0.24f + leftBuildGlow * 2.6f;
            int glowDraws = 12 + (int)(chargeGlow * 8f);
            glowStrength += tacticalOutlinePulse * (rightPulseDominates ? 0.54f : 0.24f);
            glowRadius += tacticalOutlinePulse * (rightPulseDominates ? 4.4f : 1.8f);
            glowDraws += (int)(tacticalOutlinePulse * (rightPulseDominates ? 10f : 5f));
            Color outerGlowColor = (Color.Lerp(PresetColor, Color.White, 0.48f) with { A = 0 }) * glowStrength;
            Color innerGlowColor = (Color.Lerp(AccentColor, Color.White, 0.68f) with { A = 0 }) * (0.72f + chargeGlow * 0.64f);
            Color coreGlowColor = (Color.Lerp(Color.White, PresetColor, 0.28f) with { A = 0 }) * (0.46f + chargeGlow * 0.54f);

            if (Owner.gravDir == 1f)
            {
                if (Projectile.spriteDirection == -1)
                    effects = SpriteEffects.FlipVertically;
            }
            else
            {
                origin.Y = weaponTexture.Height - origin.Y;
                if (Projectile.spriteDirection == 1)
                    effects = SpriteEffects.FlipVertically;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            if (tacticalOutlinePulse > 0f)
            {
                Color tacticalColor = Color.Lerp(PresetColor, AccentColor, rightPulseDominates ? 0.45f : 0.22f);
                HoldoutOutlineHelper.DrawSolidOutline(
                    weaponTexture,
                    drawPosition,
                    rotation,
                    origin,
                    Vector2.One * Projectile.scale * (1f + tacticalOutlinePulse * (rightPulseDominates ? 0.04f : 0.015f)),
                    effects,
                    tacticalColor,
                    glowRadius + tacticalOutlinePulse * (rightPulseDominates ? 2.4f : 0.8f),
                    MathHelper.Clamp(0.1f + tacticalOutlinePulse * (rightPulseDominates ? 0.58f : 0.24f), 0f, 0.82f),
                    time + Projectile.identity * 0.2f,
                    14 + (int)(tacticalOutlinePulse * (rightPulseDominates ? 8f : 4f)),
                    manageBlendState: false);
            }

            for (int i = 0; i < glowDraws; i++)
            {
                float completion = i / (float)glowDraws;
                float angle = MathHelper.TwoPi * completion + time * (1.7f + chargeGlow * 1.4f);
                float wave = 0.85f + 0.15f * (float)Math.Sin(time * 8f + i * 0.71f);
                Vector2 offset = angle.ToRotationVector2() * glowRadius * wave;
                Color ringColor = Color.Lerp(outerGlowColor, innerGlowColor, completion) * (0.72f - completion * 0.18f);
                Main.EntitySpriteDraw(
                    weaponTexture,
                    drawPosition + offset,
                    null,
                    ringColor,
                    rotation,
                    origin,
                    Projectile.scale * (1.02f + chargeGlow * 0.08f),
                    effects,
                    0);
            }

            int innerDraws = 10;
            for (int i = 0; i < innerDraws; i++)
            {
                float angle = MathHelper.TwoPi * i / innerDraws - time * 2.3f;
                Vector2 offset = angle.ToRotationVector2() * (1.15f + outlinePulse * 0.55f + chargeGlow * 1.2f);
                Main.EntitySpriteDraw(
                    weaponTexture,
                    drawPosition + offset,
                    null,
                    innerGlowColor * (0.58f + chargeGlow * 0.28f),
                    rotation,
                    origin,
                    Projectile.scale * (1.01f + chargeGlow * 0.05f),
                    effects,
                    0);
            }

            Main.EntitySpriteDraw(
                weaponTexture,
                drawPosition,
                null,
                coreGlowColor,
                rotation,
                origin,
                Projectile.scale * (1.07f + 0.08f * outlinePulse + chargeGlow * 0.12f),
                effects,
                0);

            DrawHoldoutChargeBloom(chargeGlow);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(weaponTexture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, effects, 0);

            if (!rightChargeActive || reloadTimer > 0)
                return false;

            //DrawRailgunTelegraph();

            Texture2D arrowTexture = ModContent.Request<Texture2D>(BFArrowCommon.GetTexturePathForPreset(CurrentPreset)).Value;
            if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_ABreak)
            {
                DrawBreakthroughChargedArrows(arrowTexture);
                return false;
            }

            Vector2 chargeArrowOffset = AimDirection * MathHelper.Lerp(20f, 24f, ChargeCompletion) + new Vector2(0f, MathHelper.Lerp(-5f, -2f, ChargeCompletion));
            Vector2 arrowDrawPosition = Projectile.Center + chargeArrowOffset - Main.screenPosition;
            float pulse = readyBurstPlayed ? (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f) * 0.05f : 0f;
            float arrowScale = 0.9f + ChargeCompletion * 0.2f + pulse;
            Color arrowColor = Color.Lerp(Color.White, PresetColor, 0.45f + 0.25f * ChargeCompletion);

            Main.EntitySpriteDraw(
                arrowTexture,
                arrowDrawPosition,
                null,
                arrowColor,
                Projectile.rotation + MathHelper.PiOver2 + MathHelper.Pi,
                arrowTexture.Size() * 0.5f,
                arrowScale,
                SpriteEffects.None,
                0);

            return false;
        }

        private void DrawHoldoutChargeBloom(float chargeGlow)
        {
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D starTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Color bloomColor = (Color.Lerp(PresetColor, Color.White, 0.62f) with { A = 0 }) * (0.28f + chargeGlow * 0.62f);
            Color starColor = (Color.Lerp(AccentColor, Color.White, 0.74f) with { A = 0 }) * (0.18f + chargeGlow * 0.72f);
            Vector2 bodyCenter = Vector2.Lerp(Projectile.Center, GunTipPosition, 0.45f) - Main.screenPosition;
            Vector2 muzzleCenter = GunTipPosition + AimDirection * 3f - Main.screenPosition;
            float pulse = 0.82f + 0.18f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7.5f + Projectile.identity);

            Main.EntitySpriteDraw(
                bloomTexture,
                bodyCenter,
                null,
                bloomColor,
                Projectile.rotation,
                bloomTexture.Size() * 0.5f,
                new Vector2(0.38f + chargeGlow * 0.38f, 0.16f + chargeGlow * 0.14f) * pulse,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                bloomTexture,
                muzzleCenter,
                null,
                bloomColor * (0.72f + chargeGlow * 0.35f),
                Projectile.rotation,
                bloomTexture.Size() * 0.5f,
                new Vector2(0.28f + chargeGlow * 0.34f, 0.12f + chargeGlow * 0.18f) * pulse,
                SpriteEffects.None,
                0);

            if (chargeGlow <= 0.03f)
                return;

            for (int i = 0; i < 4; i++)
            {
                float rotation = Projectile.rotation + MathHelper.PiOver4 * i + Main.GlobalTimeWrappedHourly * (1.2f + i * 0.2f);
                Main.EntitySpriteDraw(
                    starTexture,
                    muzzleCenter,
                    null,
                    starColor,
                    rotation,
                    starTexture.Size() * 0.5f,
                    new Vector2(0.16f + chargeGlow * 0.18f, 0.85f + chargeGlow * 1.25f) * pulse,
                    SpriteEffects.None,
                    0);
            }
        }

        private void DrawBreakthroughChargedArrows(Texture2D arrowTexture)
        {
            int maxArrows = BreakthroughMaxLoadedArrows;
            int loadedArrows = Utils.Clamp(breakthroughLoadedArrows, 0, maxArrows);
            bool fullyLoaded = loadedArrows >= maxArrows;
            int drawCount = fullyLoaded ? loadedArrows : Math.Min(loadedArrows + 1, maxArrows);
            if (drawCount <= 0)
                return;

            Color loadedColor = Color.Lerp(Color.White, BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak), 0.62f);
            Color loadingColor = Color.Lerp(Color.White, loadedColor, BreakthroughCurrentArrowCompletion);
            Color outlineColor = new(116, 255, 134, 0);
            Vector2 origin = arrowTexture.Size() * 0.5f;

            for (int i = 0; i < drawCount; i++)
            {
                bool loadingArrow = !fullyLoaded && i == loadedArrows;
                float visibility = loadingArrow ? BreakthroughCurrentArrowCompletion : 1f;
                if (visibility <= 0.02f)
                    continue;

                float angle = GetBreakthroughArrowAngle(i, drawCount);
                Vector2 arrowDirection = AimDirection.RotatedBy(angle).SafeNormalize(AimDirection);
                Vector2 arrowNormal = arrowDirection.RotatedBy(MathHelper.PiOver2);
                float drawDistance = MathHelper.Lerp(14f, 26f, visibility);
                Vector2 drawWorld = Projectile.Center + arrowDirection * drawDistance - arrowDirection * (1f - visibility) * 26f + arrowNormal * (i - (drawCount - 1f) * 0.5f) * 2.2f;
                float pulse = fullyLoaded ? (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f + i * 0.75f) * 0.04f : 0f;
                float arrowScale = MathHelper.Lerp(0.82f, 1.05f, visibility) + pulse;
                Color arrowColor = (loadingArrow ? loadingColor : loadedColor) * visibility;
                float rotation = arrowDirection.ToRotation() + MathHelper.PiOver2 + MathHelper.Pi;

                bool flashArrow = !loadingArrow && breakthroughLoadFlashTimer > 0 && i == loadedArrows - 1;
                if (flashArrow)
                {
                    float flash = breakthroughLoadFlashTimer / (float)BreakthroughLoadFlashFrames;
                    for (int j = 0; j < 10; j++)
                    {
                        Vector2 offset = (MathHelper.TwoPi * j / 10f).ToRotationVector2() * MathHelper.Lerp(1.4f, 3.2f, flash);
                        Main.EntitySpriteDraw(
                            arrowTexture,
                            drawWorld - Main.screenPosition + offset,
                            null,
                            outlineColor * (0.55f * flash),
                            rotation,
                            origin,
                            arrowScale,
                            SpriteEffects.None,
                            0);
                    }
                }

                Main.EntitySpriteDraw(
                    arrowTexture,
                    drawWorld - Main.screenPosition,
                    null,
                    arrowColor,
                    rotation,
                    origin,
                    arrowScale,
                    SpriteEffects.None,
                    0);
            }
        }
    }
}
