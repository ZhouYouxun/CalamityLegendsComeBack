using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
using CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
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
        private const int RecoveryVisualCycleCount = 4;
        private const int RecoveryVolleyShotCount = 2;
        private const int ReconTriangulationShotCount = 3;
        private const int BreakthroughFireInterval = 15;
        private const int RecoveryBurstInterval = 7;
        private const int ReconBurstShotCount = 5;
        private const int ReconFireInterval = 5;
        private const int ReconCyclePause = 20;
        private const int BombardFireInterval = 2;
        private const int PlagueFireInterval = 12;
        private const int BombardAmmoSavePercent = 90;
        private const int PlagueAmmoSavePercent = 95;
        private const int PastLingeringAmmoSavePercent = 66;
        private const int BreakthroughChargeReductionPerUnlock = 7;
        private const int BreakthroughLoadFlashFrames = 14;
        private const int BreakthroughQueuedShotGap = 4;
        private const int LeftStarFlashFrames = 14;
        private const int LeftOutlinePulseFrames = 10;
        private const int RightOutlinePulseFrames = 22;
        private const float RecoveryDnaMaxOffset = 18f;
        private const float RecoveryDnaPhaseStep = MathHelper.Pi / 8f;
        private const float BreakthroughSpeed = 19f;
        private const float ReconTriangulationSpread = 0.24f;
        private const float BombardSpeed = 12f;
        private const float BreakthroughArrowSpread = MathHelper.Pi / 11f;

        private const float IdleOffsetLength = 22f;
        private const int ReloadFrames = 18;
        private const int MaxChargeFrames = 60;
        private const int MinBreakthroughChargeFrames = 24;
        private const float RightClickBaseDamageMultiplier = 3f;
        private BalanceBlossomFlux damageBalance = new();
        private const float RailgunSightSize = 9f;
        private const float RailgunMaxSightAngle = MathHelper.Pi * (2f / 3f);

        private int burstGroupsStarted;
        private int leftBurstTimer;
        private int leftShotsFired;
        private int reconShotsFiredInBurst;
        private bool leftHeldLastFrame;

        private int reloadTimer;
        private int chargeTimer;
        private int breakthroughLoadedArrows;
        private int breakthroughLoadFlashTimer;
        private int breakthroughQueuedShotCount;
        private int breakthroughQueuedShotIndex;
        private int breakthroughQueuedShotTimer;
        private int breakthroughQueuedDamage;
        private int breakthroughQueuedPenetrate;
        private int leftStarFlashTimer;
        private int leftOutlinePulseTimer;
        private int rightOutlinePulseTimer;
        private float breakthroughQueuedSpeed;
        private float breakthroughQueuedKnockback;
        private float breakthroughQueuedNoFalloff;
        private Vector2 bombardReticleCenter;
        private Vector2 bombardReticleVelocity;
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
        private BFAccessoryPlayer BFAccessories => Owner.GetModPlayer<BFAccessoryPlayer>();
        private bool PastLingeringAssaultActive => CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_ABreak && BFAccessories.PastLingeringEquipped;
        private bool BreakthroughChargeActive => rightChargeActive && CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_ABreak;
        private int BreakthroughMaxLoadedArrows => Math.Max(1, BFBreakthroughRightBalance.GetStats().MaxLoadedArrows);
        private int BreakthroughFramesPerArrow => Math.Max(MinBreakthroughChargeFrames, BFBreakthroughRightBalance.GetStats().FramesPerArrow);
        private float BreakthroughCurrentArrowCompletion => MathHelper.Clamp(chargeTimer / (float)BreakthroughFramesPerArrow, 0f, 1f);
        private float ChargeCompletion => BreakthroughChargeActive
            ? MathHelper.Clamp((breakthroughLoadedArrows + (breakthroughLoadedArrows >= BreakthroughMaxLoadedArrows ? 0f : BreakthroughCurrentArrowCompletion)) / BreakthroughMaxLoadedArrows, 0f, 1f)
            : MathHelper.Clamp(chargeTimer / (float)GetCurrentMaxChargeFrames(), 0f, 1f);
        private bool ChargeReady => BreakthroughChargeActive ? breakthroughLoadedArrows > 0 : chargeTimer >= GetCurrentReadyChargeFrames() && readyBurstPlayed;
        private Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        private Vector2 GunTipPosition => Projectile.Center + AimDirection * 42f;
        private Color PresetColor => BFArrowCommon.GetPresetColor(CurrentPreset);
        private Color AccentColor => BFArrowCommon.GetPresetAccentColor(CurrentPreset);
        private bool BombardChargePoseActive => rightChargeActive && CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb;
        private bool RecoveryChargePoseActive => rightChargeActive && CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_BRecov;
        private bool SpecialAimScopeAnchorActive => BombardChargePoseActive || RecoveryChargePoseActive;
        private bool ShouldUseAimScope => rightChargeActive && CurrentPreset != BlossomFluxChloroplastPresetType.Chlo_BRecov && CurrentPreset != BlossomFluxChloroplastPresetType.Chlo_DBomb;

        internal Color GetAimScopeMainColor() => Color.Lerp(PresetColor, AccentColor, 0.18f);

        internal Color GetAimScopeAccentColor() => Color.Lerp(AccentColor, Color.White, 0.25f);

        internal bool ShouldDrawBombardMouseReticle => false;

        internal float GetChargeCompletion() => ChargeCompletion;

        internal float GetAimScopeMaxChargeFrames() => BreakthroughChargeActive
            ? Math.Max(BFAimScope.MinimumCharge, BreakthroughFramesPerArrow)
            : Math.Max(BFAimScope.MinimumCharge, GetCurrentMaxChargeFrames());

        internal bool ShouldKeepAimScopeSlot(int slotIndex)
        {
            if (!ShouldUseAimScope || slotIndex < 0)
                return false;

            return BreakthroughChargeActive
                ? slotIndex < GetBreakthroughAimScopeCount()
                : slotIndex == 0;
        }

        internal float GetAimScopeChargeForSlot(int slotIndex)
        {
            if (!BreakthroughChargeActive)
                return ChargeCompletion * GetAimScopeMaxChargeFrames();

            if (slotIndex < breakthroughLoadedArrows)
                return BreakthroughFramesPerArrow;

            if (slotIndex == breakthroughLoadedArrows && breakthroughLoadedArrows < BreakthroughMaxLoadedArrows)
                return chargeTimer;

            return BreakthroughFramesPerArrow;
        }

        private int GetBreakthroughAimScopeCount()
        {
            if (!BreakthroughChargeActive)
                return 0;

            int drawCount = breakthroughLoadedArrows;
            if (breakthroughLoadedArrows < BreakthroughMaxLoadedArrows)
                drawCount++;

            return Utils.Clamp(drawCount, 0, BreakthroughMaxLoadedArrows);
        }

        private int GetDesiredAimScopeCount()
        {
            if (!ShouldUseAimScope)
                return 0;

            return BreakthroughChargeActive ? GetBreakthroughAimScopeCount() : 1;
        }

        private int GetCurrentReadyChargeFrames()
        {
            return CurrentPreset switch
            {
                BlossomFluxChloroplastPresetType.Chlo_BRecov => BFRecoveryRightBalance.GetStats().ChargeFrames,
                BlossomFluxChloroplastPresetType.Chlo_CDetec => BFReconRightBalance.GetStats().ChargeFrames,
                BlossomFluxChloroplastPresetType.Chlo_DBomb => BFBombardRightBalance.GetStats().ChargeFrames,
                _ => MaxChargeFrames
            };
        }

        private int GetCurrentMaxChargeFrames()
        {
            if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb)
            {
                return BFBombardRightBalance.GetStats().ChargeFrames;
            }

            return GetCurrentReadyChargeFrames();
        }

        internal Vector2 GetBombardReticleCenter() => BombardChargePoseActive ? bombardReticleCenter : GetCurrentMouseWorld();

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
            if (BombardChargePoseActive)
                return GetBombardReticleCenter();

            if (!SpecialAimScopeAnchorActive)
                return Owner.MountedCenter + scopeDirection * BFAimScope.WeaponLength;

            return GetAimScopeBaseAnchor() - scopeDirection * 18f;
        }

        internal Vector2 GetAimScopeSparkOrigin(Vector2 scopeDirection)
        {
            if (BombardChargePoseActive)
                return GetBombardReticleCenter();

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

            if (HasEarlierActiveHoldout())
            {
                Projectile.Kill();
                return;
            }

            Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);
            Projectile.knockBack = Owner.HeldItem.knockBack;

            UpdateOutlinePulseTimers();

            BFRightUIPlayer rightUIPlayer = Owner.GetModPlayer<BFRightUIPlayer>();

            if (Main.myPlayer == Projectile.owner)
            {
                HandleOwnerLogic(rightUIPlayer);
                UpdateBreakthroughQueuedShots();
            }

            UpdateIdlePose();
            UpdateHeldProjectileVariables();
            ManipulatePlayerVariables();
        }

        private void HandleOwnerLogic(BFRightUIPlayer rightUIPlayer)
        {
            bool selectionPanelOpen = HasActiveSelectionPanel(Owner);
            rightUIPlayer.ProcessRightClickState(selectionPanelOpen);
            bool formSwitchInputHandled = HandleFormSwitchInput();

            if (!formSwitchInputHandled && !HasActiveSelectionPanel(Owner) && rightUIPlayer.LongHoldReachedThisFrame && !rightChargeActive)
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
                !Owner.GetModPlayer<BFRightUIPlayer>().FormSwitchKeyHeld &&
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
                reconShotsFiredInBurst = 0;
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

        private bool HandleFormSwitchInput()
        {
            if (KeybindSystem.LegendaryWeaponFormSwitch?.JustPressed != true)
                return false;

            if (rightChargeActive ||
                Owner.HeldItem.type != AssociatedItemID ||
                Owner.noItems ||
                Owner.CCed ||
                Main.mapFullscreen ||
                Main.blockMouse ||
                Owner.mouseInterface ||
                (Main.playerInventory && Main.HoverItem.type == Owner.HeldItem.type))
            {
                return false;
            }

            OpenFormSwitchSelectionPanel();
            return true;
        }

        private int GetInitialLeftFireDelay() => CurrentPreset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => GetNextBreakthroughFireInterval(),
            BlossomFluxChloroplastPresetType.Chlo_BRecov => RecoveryBurstInterval,
            BlossomFluxChloroplastPresetType.Chlo_CDetec => ReconFireInterval,
            BlossomFluxChloroplastPresetType.Chlo_DBomb => Math.Max(1, BFBombardLeftBalance.GetStats().FireInterval / 2),
            BlossomFluxChloroplastPresetType.Chlo_EPlague => PlagueFireInterval,
            _ => BreakthroughFireInterval
        };

        private bool TryPickLeftAmmo(out int projectileType, out float speed, out int damage, out float knockback)
        {
            bool dontConsume = CurrentPreset switch
            {
                BlossomFluxChloroplastPresetType.Chlo_ABreak when PastLingeringAssaultActive => Main.rand.Next(100) < PastLingeringAmmoSavePercent,
                BlossomFluxChloroplastPresetType.Chlo_DBomb => Main.rand.Next(100) < BombardAmmoSavePercent,
                BlossomFluxChloroplastPresetType.Chlo_EPlague => Main.rand.Next(100) < PlagueAmmoSavePercent,
                _ => false
            };

            return Owner.PickAmmo(Owner.HeldItem, out projectileType, out speed, out damage, out knockback, out _, dontConsume);
        }

        private void FireCurrentPresetLeftAttack(IEntitySource source, int projectileType, float speed, int damage, float knockback)
        {
            TriggerLeftStarFlash();

            switch (CurrentPreset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                    if (PastLingeringAssaultActive)
                        FirePastLingeringVolley(source, projectileType, speed, damage, knockback);
                    else
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
                    FirePlagueReapers(source, projectileType, speed, damage, knockback);
                    break;
            }
        }

        private void ScheduleNextLeftFire()
        {
            leftShotsFired++;

            switch (CurrentPreset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                    burstGroupsStarted = (burstGroupsStarted + 1) % RecoveryVisualCycleCount;
                    leftBurstTimer = burstGroupsStarted == 0
                        ? BFRecoveryLeftBalance.GetStats().VolleyPauseFrames
                        : RecoveryBurstInterval;
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                    leftBurstTimer = PastLingeringAssaultActive ? GetPastLingeringFireInterval() : GetNextBreakthroughFireInterval();
                    if (PastLingeringAssaultActive && leftShotsFired == 12)
                        SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.45f, Pitch = 0.35f }, Owner.Center);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                    reconShotsFiredInBurst++;
                    if (reconShotsFiredInBurst >= ReconBurstShotCount)
                    {
                        reconShotsFiredInBurst = 0;
                        leftBurstTimer = ReconCyclePause;
                    }
                    else
                    {
                        leftBurstTimer = ReconFireInterval;
                    }

                    break;

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                    leftBurstTimer = Math.Max(1, BFBombardLeftBalance.GetStats().FireInterval / 2);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                    leftBurstTimer = PlagueFireInterval;
                    break;
            }
        }

        private void BeginRightCharge()
        {
            CloseSelectionPanel();

            if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb && HasActiveBombardStrike())
            {
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.42f, Pitch = -0.3f }, Owner.Center);
                return;
            }

            rightChargeActive = true;
            reloadTimer = ReloadFrames;
            chargeTimer = 0;
            breakthroughLoadedArrows = 0;
            breakthroughLoadFlashTimer = 0;
            readyBurstPlayed = false;
            releasedShot = false;
            bombardReticleCenter = GetCurrentMouseWorld();
            bombardReticleVelocity = Vector2.Zero;

            // 进入右键蓄力时，确保场上只有一个瞄准镜弹幕
            if (ShouldUseAimScope)
                EnsureAimScopeExists();
            else
                KillAimScopeProjectiles();

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

            UpdateBombardReticle();
            UpdateRecoveryChargeBuff();
            EnsureAimScopeExists();

            if (reloadTimer > 0)
            {
                UpdateReloadAnimation();
                rightUIPlayer.SetRightChargeBar(ChargeCompletion);
                return;
            }

            if (BreakthroughChargeActive)
            {
                UpdateBreakthroughChargeState();
                rightUIPlayer.SetRightChargeBar(ChargeCompletion);
                return;
            }

            if (chargeTimer < GetCurrentMaxChargeFrames())
            {
                chargeTimer++;
                UpdateChargingAnimation();
            }
            else
            {
                UpdateChargedAnimation();
            }

            rightUIPlayer.SetRightChargeBar(ChargeCompletion);
        }

        private bool HasActiveBombardStrike()
        {
            int bombardType = ModContent.ProjectileType<BFArrow_DBomb>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.active && projectile.owner == Owner.whoAmI && projectile.type == bombardType)
                    return true;
            }

            return false;
        }

        private void UpdateBombardReticle()
        {
            if (!BombardChargePoseActive)
                return;

            Vector2 mouseWorld = GetCurrentMouseWorld();
            if (bombardReticleCenter == Vector2.Zero)
                bombardReticleCenter = mouseWorld;

            Vector2 toMouse = mouseWorld - bombardReticleCenter;
            bombardReticleVelocity = Vector2.Lerp(bombardReticleVelocity, toMouse * 0.18f, 0.18f);
            if (bombardReticleVelocity.LengthSquared() > 38f * 38f)
                bombardReticleVelocity = bombardReticleVelocity.SafeNormalize(Vector2.Zero) * 38f;

            bombardReticleCenter += bombardReticleVelocity;
            bombardReticleVelocity *= 0.86f;
        }

        private void UpdateRecoveryChargeBuff()
        {
            float chargeDr = RecoveryChargePoseActive ? BFRecoveryRightBalance.GetStats().ChargeDamageReduction : 0f;
            Owner.GetModPlayer<BFRecoveryEcologyPlayer>().SetRecoveryChargeDamageReduction(chargeDr);
        }

        private void UpdateBreakthroughChargeState()
        {
            EnsureAimScopeExists();

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
            EnsureAimScopeExists();

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
            breakthroughLoadedArrows = 0;
            breakthroughLoadFlashTimer = 0;
            readyBurstPlayed = false;
            releasedShot = false;

            // 退出右键蓄力时，立刻移除瞄准镜弹幕
            bombardReticleVelocity = Vector2.Zero;
            Owner.GetModPlayer<BFRecoveryEcologyPlayer>().SetRecoveryChargeDamageReduction(0f);
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
            if (PastLingeringAssaultActive)
                Owner.phantasmTime = 2;

            float armRotation = Projectile.rotation - MathHelper.PiOver2;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation + extraFrontArmRotation);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRotation + extraBackArmRotation);
        }

        private void UpdateOutlinePulseTimers()
        {
            if (leftStarFlashTimer > 0)
                leftStarFlashTimer--;
 
            if (leftOutlinePulseTimer > 0)
                leftOutlinePulseTimer--;
 
            if (rightOutlinePulseTimer > 0)
                rightOutlinePulseTimer--;
        }
 
        private void TriggerLeftStarFlash()
        {
            leftStarFlashTimer = LeftStarFlashFrames;
            leftOutlinePulseTimer = LeftOutlinePulseFrames;
        }
 
        private void TriggerTacticalOutlinePulse(bool rightClick)
        {
            if (rightClick)
                rightOutlinePulseTimer = RightOutlinePulseFrames;
            else
                leftOutlinePulseTimer = LeftOutlinePulseFrames;
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
                BlossomFluxChloroplastPresetType.Chlo_ABreak => Math.Max(1, BFBreakthroughLeftBalance.GetStats().UseInterval),
                BlossomFluxChloroplastPresetType.Chlo_BRecov => burstGroupsStarted == 0 && leftBurstTimer > RecoveryBurstInterval ? BFRecoveryLeftBalance.GetStats().VolleyPauseFrames : RecoveryBurstInterval,
                BlossomFluxChloroplastPresetType.Chlo_CDetec => leftBurstTimer > ReconFireInterval ? ReconCyclePause : ReconFireInterval,
                BlossomFluxChloroplastPresetType.Chlo_DBomb => Math.Max(1, BFBombardLeftBalance.GetStats().FireInterval / 2),
                BlossomFluxChloroplastPresetType.Chlo_EPlague => PlagueFireInterval,
                _ => BreakthroughFireInterval
            };
        }

        private int GetNextBreakthroughFireInterval()
        {
            if (PastLingeringAssaultActive)
                return GetPastLingeringFireInterval();

            return Math.Max(1, BFBreakthroughLeftBalance.GetStats().UseInterval);
        }

        private int GetPastLingeringFireInterval()
        {
            return Math.Max(12, 24 - GetPastLingeringFireSpeedTier() * 2);
        }

        private int GetPastLingeringFireSpeedTier()
        {
            if (!PastLingeringAssaultActive)
                return 0;

            if (leftShotsFired >= 12)
                return 3;

            return leftShotsFired >= 8 ? 2 : leftShotsFired >= 4 ? 1 : 0;
        }

        private float GetBreakthroughShotsPerSecond()
        {
            return BFBreakthroughLeftBalance.GetStats().ShotsPerSecond;
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

            if (!BreakthroughChargeActive && chargeTimer >= GetCurrentReadyChargeFrames() && !readyBurstPlayed)
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

        }

        private void HandleRelease()
        {
            if (releasedShot || !ChargeReady)
                return;
 
            releasedShot = true;
            extraFrontArmRotation = 0f;
            extraBackArmRotation = 0f;
 
            TriggerTacticalOutlinePulse(rightClick: true);
            PlayRightReleaseSound();
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
                    FireRecoverySpecialTransfers();
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                    FireReconSpecialArrow(chargeCompletion);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                    FireBombardSpecialArrow(chargeCompletion, ModContent.ProjectileType<BFArrow_DBomb>(), 19.2f, 0.88f);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                    FireSpecialArrow(chargeCompletion, ModContent.ProjectileType<BFArrow_EPlague>(), 18.6f, 0.98f);
                    break;
            }
        }

        private int FireSpecialArrow(float chargeCompletion, int projectileType, float baseSpeed, float damageMultiplier)
        {
            float speed = MathHelper.Lerp(baseSpeed * 0.76f, baseSpeed * 1.22f, chargeCompletion) * GetAccessoryArrowSpeedMultiplier(CurrentPreset);
            int damage = (int)(GetCurrentRightClickDamage() * RightClickBaseDamageMultiplier * MathHelper.Lerp(0.8f, 1.35f, chargeCompletion) * damageMultiplier);
            float knockback = Projectile.knockBack * MathHelper.Lerp(0.85f, 1.15f, chargeCompletion);

            return Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition,
                AimDirection * speed,
                projectileType,
                damage,
                knockback,
                Projectile.owner);
        }

        private void FireRecoverySpecialTransfers()
        {
            BFRecoveryRightStats stats = BFRecoveryRightBalance.GetStats();
            Vector2 skyDirection = GetRecoverySkyAimDirection();
            SpawnLeftMuzzleFX(GunTipPosition, skyDirection * 12f, CurrentPreset, 1.3f);

            if (Projectile.owner != Main.myPlayer)
                return;

            int flashCount = stats.FlashCount;
            Vector2 upward = -Vector2.UnitY * Owner.gravDir;
            Vector2 side = skyDirection.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.UnitX);

            for (int i = 0; i < flashCount; i++)
            {
                float progress = flashCount <= 1 ? 0.5f : i / (flashCount - 1f);
                Vector2 treeOffset = GetRecoveryTreeOffset(i, flashCount, upward, side);
                Vector2 spawnPosition = Owner.Center + treeOffset;
                Vector2 velocityDirection = Vector2.Lerp(upward, treeOffset.SafeNormalize(upward), 0.34f + progress * 0.2f).SafeNormalize(upward);
                Vector2 velocity = velocityDirection * MathHelper.Lerp(2.2f, 3.8f, progress) * GetAccessoryArrowSpeedMultiplier(BlossomFluxChloroplastPresetType.Chlo_BRecov, convertedLeafArrow: true);

                BFArrow_BRecovTransfer.Spawn(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    Projectile.owner,
                    stats.HealAmount,
                    BFArrow_BRecovTransfer.ChargedReleaseSpawnMode);
            }
        }

        private static Vector2 GetRecoveryTreeOffset(int index, int count, Vector2 upward, Vector2 side)
        {
            float progress = count <= 1 ? 0f : index / (count - 1f);
            float smoothed = MathHelper.SmoothStep(0f, 1f, progress);
            float height = MathHelper.Lerp(26f, 178f, progress);
            float radius = MathHelper.Lerp(0f, 72f, smoothed);
            float angle = -MathHelper.PiOver2 + index * 1.72f;
            float lateral = MathF.Cos(angle) * radius;
            float verticalCurl = MathF.Sin(angle) * radius * 0.16f;

            return upward * (height + verticalCurl) + side * lateral;
        }

        private void FireReconSpecialArrow(float chargeCompletion)
        {
            BFReconRightStats stats = BFReconRightBalance.GetStats();
            int projectileIndex = FireSpecialArrow(chargeCompletion, ModContent.ProjectileType<BFArrow_CDetec>(), 18.75f, 0.92f);
            if (!BFArrowCommon.InBounds(projectileIndex, Main.maxProjectiles))
                return;

            if (Main.projectile[projectileIndex].ModProjectile is BFArrow_CDetec reconArrow)
                reconArrow.ConfigureMark(stats.MarkDuration, stats.EffectTier);

            Main.projectile[projectileIndex].netUpdate = true;
        }

        private int GetCurrentRightClickDamage()
        {
            int baseDamage = damageBalance.GetRightClickBaseDamage();
            DamageClass damageType = Owner.HeldItem?.DamageType ?? DamageClass.Ranged;
            return (int)Owner.GetTotalDamage(damageType).ApplyTo(baseDamage);
        }

        private void FireBreakthroughSpecialArrows()
        {
            BFBreakthroughRightStats stats = BFBreakthroughRightBalance.GetStats();
            int arrowCount = Math.Max(1, breakthroughLoadedArrows);
            float speed = 21.6f * 1.22f * stats.ProjectileSpeedMultiplier * GetAccessoryArrowSpeedMultiplier(BlossomFluxChloroplastPresetType.Chlo_ABreak);
            int damage = (int)(GetCurrentRightClickDamage() * RightClickBaseDamageMultiplier * 1.35f * 1.12f);
            damage = (int)(damage * (1f + arrowCount * stats.DamagePerChargeStack));
            float knockback = Projectile.knockBack * 1.15f;

            breakthroughQueuedShotCount = arrowCount;
            breakthroughQueuedShotIndex = 0;
            breakthroughQueuedShotTimer = 0;
            breakthroughQueuedDamage = damage;
            breakthroughQueuedPenetrate = stats.Penetrate;
            breakthroughQueuedSpeed = speed;
            breakthroughQueuedKnockback = knockback;
            breakthroughQueuedNoFalloff = 1f;
        }

        private void UpdateBreakthroughQueuedShots()
        {
            if (breakthroughQueuedShotCount <= 0)
                return;

            if (breakthroughQueuedShotTimer > 0)
            {
                breakthroughQueuedShotTimer--;
                return;
            }

            Vector2 shootVelocity = GetAimVelocity(breakthroughQueuedSpeed);
            Vector2 shootDirection = shootVelocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 spawnPosition = GetShootOrigin(shootVelocity) - shootDirection * (breakthroughQueuedShotIndex * 6f);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                shootVelocity,
                ModContent.ProjectileType<BFArrow_ABreak>(),
                breakthroughQueuedDamage,
                breakthroughQueuedKnockback,
                Projectile.owner,
                breakthroughQueuedPenetrate,
                breakthroughQueuedNoFalloff);

            SpawnLeftMuzzleFX(spawnPosition, shootVelocity, CurrentPreset, 0.68f);
            SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.28f, Pitch = 0.16f + breakthroughQueuedShotIndex * 0.04f }, spawnPosition);

            breakthroughQueuedShotIndex++;
            if (breakthroughQueuedShotIndex >= breakthroughQueuedShotCount)
            {
                breakthroughQueuedShotCount = 0;
                breakthroughQueuedShotIndex = 0;
                breakthroughQueuedShotTimer = 0;
                return;
            }

            breakthroughQueuedShotTimer = BreakthroughQueuedShotGap - 1;
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
            BFBombardRightStats stats = BFBombardRightBalance.GetStats();
            float speed = MathHelper.Lerp(baseSpeed * 1.52f, baseSpeed * 2.24f, chargeCompletion) * GetAccessoryArrowSpeedMultiplier(BlossomFluxChloroplastPresetType.Chlo_DBomb);
            int damage = (int)(GetCurrentRightClickDamage() * RightClickBaseDamageMultiplier * MathHelper.Lerp(0.8f, 1.35f, chargeCompletion) * damageMultiplier);
            float knockback = Projectile.knockBack * MathHelper.Lerp(0.85f, 1.15f, chargeCompletion);
            Vector2 bombardTarget = GetBombardReticleCenter();
            Vector2 skyAim = GetBombardSkyAimDirection();

            int projectileIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition,
                skyAim * speed,
                projectileType,
                damage,
                knockback,
                Projectile.owner);

            if (!BFArrowCommon.InBounds(projectileIndex, Main.maxProjectiles))
                return;

            if (Main.projectile[projectileIndex].ModProjectile is BFArrow_DBomb bombardArrow)
                bombardArrow.ConfigureBombardTarget(bombardTarget, stats.ExplosionSize, stats.SkyRainMultiplier);

            Main.projectile[projectileIndex].netUpdate = true;
        }

        private void PlayChargeReadyBurst()
        {
            if (readyBurstPlayed)
                return;
 
            readyBurstPlayed = true;
            rightOutlinePulseTimer = Math.Max(rightOutlinePulseTimer, RightOutlinePulseFrames / 2);
 
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f, Pitch = 0.25f }, GunTipPosition);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.4f, Pitch = -0.15f }, GunTipPosition);
        }

        private void PlayBreakthroughArrowLoadedBurst()
        {
            SoundEngine.PlaySound(SoundID.Item108 with { Volume = 0.22f, Pitch = 0.35f }, GunTipPosition);
        }

        private void PlayRightReleaseSound()
        {
            if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb)
            {
                SoundStyle bombardFire = new("CalamityMod/Sounds/Item/LauncherHeavyShot");
                SoundEngine.PlaySound(bombardFire with { Volume = 0.82f, Pitch = -0.08f, PitchVariance = 0.08f }, GunTipPosition);
            }
            else
            {
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.72f, Pitch = -0.05f }, GunTipPosition);
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
            Texture2D scopeTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/BlossomFlux/RightClick/BFAimScope").Value;
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
            float finalSpeed = Math.Max(speed, BreakthroughSpeed) * BFBreakthroughLeftBalance.GetStats().ProjectileSpeedMultiplier * 0.8f;
            Vector2 shootVelocity = GetAimVelocity(finalSpeed);
            Vector2 spawnPosition = GetShootOrigin(shootVelocity);

            SpawnLeftProjectile(source, spawnPosition, shootVelocity, projectileType, damage, knockback, CurrentPreset);
            SpawnLeftMuzzleFX(spawnPosition, shootVelocity, CurrentPreset, 1.05f);
            if (leftShotsFired % 3 == 0)
                SoundEngine.PlaySound(SoundID.Item5 with { Pitch = Main.rand.NextFloat(-0.08f, 0.08f), Volume = 0.52f }, Owner.Center);
        }

        private void FirePastLingeringVolley(IEntitySource source, int projectileType, float speed, int damage, float knockback)
        {
            int fireSpeedTier = GetPastLingeringFireSpeedTier();
            Vector2 direction = GetAimVelocity(1f).SafeNormalize(Vector2.UnitX * Owner.direction);
            float projectileSpeed = speed;
            Vector2 visualVelocity = direction * Owner.HeldItem.shootSpeed * 0.55f;
            Projectile.velocity = visualVelocity;

            for (int i = 0; i < 5; i++)
            {
                Vector2 shotVelocity = direction * projectileSpeed * Main.rand.NextFloat(0.6f, 1.4f);
                Vector2 spawnPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true) + Utils.RandomVector2(Main.rand, -15f, 15f);
                int projectileIndex = Projectile.NewProjectile(source, spawnPosition, shotVelocity, projectileType, damage, knockback, Projectile.owner);
                if (!BFArrowCommon.InBounds(projectileIndex, Main.maxProjectiles))
                    continue;

                Projectile arrowProjectile = Main.projectile[projectileIndex];
                arrowProjectile.friendly = true;
                arrowProjectile.hostile = false;
                arrowProjectile.arrow = true;
                arrowProjectile.noDropItem = true;
                BFArrowCommon.TagBlossomFluxLeftArrow(arrowProjectile);

                BFArrow_CDetecEffect arrowEffect = arrowProjectile.GetGlobalProjectile<BFArrow_CDetecEffect>();
                arrowEffect.Preset = BlossomFluxChloroplastPresetType.Chlo_ABreak;
                arrowEffect.ConvertedLeafArrow = false;

                BFAccessoryGlobalProjectile accessoryEffect = arrowProjectile.GetGlobalProjectile<BFAccessoryGlobalProjectile>();
                accessoryEffect.BlossomFluxArrow = true;
                accessoryEffect.Preset = BlossomFluxChloroplastPresetType.Chlo_ABreak;
            }

            SpawnLeftMuzzleFX(GunTipPosition, direction * projectileSpeed, CurrentPreset, 0.94f + fireSpeedTier * 0.08f);
            if (leftShotsFired % 4 == 0)
                SoundEngine.PlaySound(SoundID.Item5 with { Pitch = Main.rand.NextFloat(-0.04f, 0.08f) + fireSpeedTier * 0.04f, Volume = 0.48f }, Owner.Center);
        }

        private void FireRecoveryVolley(IEntitySource source, int projectileType, float speed, int damage, float knockback)
        {
            Vector2 velocity = GetAimVelocity(speed);
            FireRecoveryDnaPair(source, velocity, projectileType, damage, knockback);
            SpawnLeftMuzzleFX(GetShootOrigin(velocity), velocity, CurrentPreset, 0.92f + burstGroupsStarted * 0.08f);
            SoundEngine.PlaySound(SoundID.Item8 with { Pitch = 0.2f + burstGroupsStarted * 0.04f, Volume = 0.58f }, Owner.Center);
        }

        private void FireRecoveryDnaPair(IEntitySource source, Vector2 velocity, int projectileType, int damage, float knockback)
        {
            Vector2 shootVelocity = velocity.SafeNormalize(Vector2.UnitX * Owner.direction) * velocity.Length();
            if (shootVelocity == Vector2.Zero)
                shootVelocity = Vector2.UnitX * Owner.direction * Owner.HeldItem.shootSpeed;

            Vector2 origin = GetShootOrigin(shootVelocity);
            Vector2 normal = shootVelocity.SafeNormalize(Vector2.UnitX * Owner.direction).RotatedBy(MathHelper.PiOver2);
            float phase = leftShotsFired * RecoveryDnaPhaseStep;
            float offsetAmount = MathF.Cos(phase) * RecoveryDnaMaxOffset;

            for (int i = 0; i < RecoveryVolleyShotCount; i++)
            {
                float sideSign = i == 0 ? 1f : -1f;
                SpawnLeftProjectile(source, origin + normal * offsetAmount * sideSign, shootVelocity, projectileType, damage, knockback, CurrentPreset);
            }
        }

        private void FireReconScatter(IEntitySource source, int projectileType, float speed, int damage, float knockback)
        {
            Vector2 baseVelocity = GetAimVelocity(Math.Max(speed, 15f));
            Vector2 origin = GetShootOrigin(baseVelocity);
            Vector2 normal = baseVelocity.SafeNormalize(Vector2.UnitX * Owner.direction).RotatedBy(MathHelper.PiOver2);
            float weaveDirection = reconShotsFiredInBurst % 2 == 0 ? 1f : -1f;
            float[] offsets = { 0f, 18f * weaveDirection, -26f * weaveDirection };
            float[] angleOffsets =
            {
                0f,
                -ReconTriangulationSpread * 0.78f * weaveDirection,
                ReconTriangulationSpread * 1.12f * weaveDirection
            };
            float[] speedMultipliers = { 1.08f, 0.96f, 1.02f };

            for (int i = 0; i < ReconTriangulationShotCount; i++)
            {
                Vector2 shotVelocity = baseVelocity.RotatedBy(angleOffsets[i]) * speedMultipliers[i];
                Vector2 spawnPosition = origin + normal * offsets[i] - baseVelocity.SafeNormalize(Vector2.UnitX * Owner.direction) * (i == 0 ? 0f : 5f + i * 4f);
                SpawnLeftProjectile(source, spawnPosition, shotVelocity, projectileType, damage, knockback, CurrentPreset);
            }

            SpawnLeftMuzzleFX(origin, baseVelocity, CurrentPreset, 0.92f + reconShotsFiredInBurst * 0.06f);
            SoundEngine.PlaySound(SoundID.Item9 with { Pitch = 0.35f + reconShotsFiredInBurst * 0.03f, Volume = 0.38f }, Owner.Center);
        }

        private void FireBombardRain(IEntitySource source, int projectileType, float speed, int damage, float knockback)
        {
            BFBombardLeftStats stats = BFBombardLeftBalance.GetStats();
            float arrowSpeed = Main.rand.Next(25, 30) * stats.ProjectileSpeedMultiplier;
            Vector2 realPlayerPos = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            float mouseXDist = Main.mouseX + Main.screenPosition.X - realPlayerPos.X;
            float mouseYDist = Main.mouseY + Main.screenPosition.Y - realPlayerPos.Y;

            if (Owner.gravDir == -1f)
                mouseYDist = Main.screenPosition.Y + Main.screenHeight - Main.mouseY - realPlayerPos.Y;

            float mouseDistance = (float)Math.Sqrt(mouseXDist * mouseXDist + mouseYDist * mouseYDist);
            if ((float.IsNaN(mouseXDist) && float.IsNaN(mouseYDist)) || (mouseXDist == 0f && mouseYDist == 0f))
            {
                mouseXDist = Owner.direction;
                mouseYDist = 0f;
                mouseDistance = arrowSpeed;
            }
            else
                mouseDistance = arrowSpeed / mouseDistance;

            if (leftShotsFired % 2 == 0)
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.48f, PitchVariance = 0.2f }, Owner.Center);
            else
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.3f, Pitch = -0.22f, PitchVariance = 0.08f }, Owner.Center);

            for (int i = 0; i < 3; i++)
            {
                realPlayerPos = new Vector2(
                    Owner.position.X + Owner.width * 0.5f + Main.rand.Next(201) * -Owner.direction + Main.mouseX + Main.screenPosition.X - Owner.position.X,
                    Owner.MountedCenter.Y - 600f);
                realPlayerPos.X = (realPlayerPos.X + Owner.Center.X) / 2f + Main.rand.Next(-200, 201);
                realPlayerPos.Y -= 100f * i;

                mouseXDist = Main.mouseX + Main.screenPosition.X - realPlayerPos.X;
                mouseYDist = Main.mouseY + Main.screenPosition.Y - realPlayerPos.Y;
                if (mouseYDist < 0f)
                    mouseYDist *= -1f;

                if (mouseYDist < 20f)
                    mouseYDist = 20f;

                mouseDistance = (float)Math.Sqrt(mouseXDist * mouseXDist + mouseYDist * mouseYDist);
                mouseDistance = arrowSpeed / mouseDistance;
                mouseXDist *= mouseDistance;
                mouseYDist *= mouseDistance;

                float speedX = mouseXDist + Main.rand.Next(-120, 121) * 0.01f;
                float speedY = mouseYDist + Main.rand.Next(-120, 121) * 0.01f;
                SpawnBombardStormArrow(source, realPlayerPos, new Vector2(speedX, speedY * 0.9f), projectileType, damage, knockback, stats);
                SpawnBombardStormArrow(source, realPlayerPos, new Vector2(speedX, speedY * 0.8f), projectileType, damage, knockback, stats);
            }

            SpawnLeftMuzzleFX(GetCurrentMouseWorld(), Vector2.UnitY * Owner.gravDir, CurrentPreset, 0.92f);
        }

        private void SpawnBombardStormArrow(IEntitySource source, Vector2 spawnPosition, Vector2 velocity, int projectileType, int damage, float knockback, BFBombardLeftStats stats)
        {
            int projectileIndex = SpawnLeftProjectile(source, spawnPosition, velocity, projectileType, damage, knockback, CurrentPreset);
            if (!BFArrowCommon.InBounds(projectileIndex, Main.maxProjectiles) || Main.projectile[projectileIndex].type != ModContent.ProjectileType<BFLeafProj>())
                return;

            Main.projectile[projectileIndex].ai[1] = stats.ExplosionsPerArrow;
            Main.projectile[projectileIndex].scale *= stats.ExplosionRadiusMultiplier;
            Main.projectile[projectileIndex].netUpdate = true;
        }

        private void FirePlagueReapers(IEntitySource source, int projectileType, float speed, int damage, float knockback)
        {
            int finalProjectileType = ModContent.ProjectileType<BFLeftPlagueReaper>();
            Vector2 baseDirection = AimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 origin = GunTipPosition;
            float speedMultiplier = GetAccessoryArrowSpeedMultiplier(BlossomFluxChloroplastPresetType.Chlo_EPlague);
            float shotSpeed = Math.Max(speed, 12.5f) * 0.92f * speedMultiplier;
            Vector2 shootVelocity = baseDirection.RotatedBy(Main.rand.NextFloat(-0.05f, 0.05f)) * shotSpeed;
            Vector2 spawnPosition = origin + baseDirection.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-4f, 4f);

            int projectileIndex = Projectile.NewProjectile(
                source,
                spawnPosition,
                shootVelocity,
                finalProjectileType,
                Math.Max(1, (int)(damage * 1.05f)),
                knockback * 0.72f,
                Owner.whoAmI,
                Main.rand.NextFloat(1000f),
                Main.rand.NextFloat(3f));

            if (BFArrowCommon.InBounds(projectileIndex, Main.maxProjectiles))
            {
                Projectile arrowProjectile = Main.projectile[projectileIndex];
                arrowProjectile.arrow = true;
                arrowProjectile.noDropItem = true;
                BFArrowCommon.TagBlossomFluxLeftArrow(arrowProjectile);
                BFArrow_CDetecEffect arrowEffect = arrowProjectile.GetGlobalProjectile<BFArrow_CDetecEffect>();
                arrowEffect.Preset = BlossomFluxChloroplastPresetType.Chlo_EPlague;
                arrowEffect.ConvertedLeafArrow = false;

                BFAccessoryGlobalProjectile accessoryEffect = arrowProjectile.GetGlobalProjectile<BFAccessoryGlobalProjectile>();
                accessoryEffect.BlossomFluxArrow = true;
                accessoryEffect.Preset = BlossomFluxChloroplastPresetType.Chlo_EPlague;
            }

            SpawnLeftMuzzleFX(spawnPosition, shootVelocity, CurrentPreset, 0.66f);
        }

        private int SpawnLeftProjectile(IEntitySource source, Vector2 spawnPosition, Vector2 velocity, int projectileType, int damage, float knockback, BlossomFluxChloroplastPresetType preset, bool noTileCollide = false)
        {
            int leafProjectileType = ModContent.ProjectileType<BFLeafProj>();
            bool convertWoodenArrow = CalamityUtils.CheckWoodenAmmo(projectileType, Owner);
            bool convertToLeaf = preset switch
            {
                BlossomFluxChloroplastPresetType.Chlo_ABreak => convertWoodenArrow,
                BlossomFluxChloroplastPresetType.Chlo_BRecov => true,
                BlossomFluxChloroplastPresetType.Chlo_CDetec => true,
                BlossomFluxChloroplastPresetType.Chlo_DBomb => true,
                _ => false
            };
            int finalProjectileType = convertToLeaf ? leafProjectileType : projectileType;
            float ai0 = convertToLeaf ? (int)preset : 0f;
            Vector2 finalVelocity = velocity;
            if (preset == BlossomFluxChloroplastPresetType.Chlo_ABreak && !convertToLeaf)
                finalVelocity *= 0.7f;

            finalVelocity *= GetAccessoryArrowSpeedMultiplier(preset, convertToLeaf);

            int projectileIndex = Projectile.NewProjectile(source, spawnPosition, finalVelocity, finalProjectileType, damage, knockback, Owner.whoAmI, ai0);
            if (!BFArrowCommon.InBounds(projectileIndex, Main.maxProjectiles))
                return -1;

            Projectile arrowProjectile = Main.projectile[projectileIndex];
            arrowProjectile.friendly = true;
            arrowProjectile.hostile = false;
            arrowProjectile.arrow = true;
            arrowProjectile.noDropItem = true;

            if (noTileCollide || BFAccessories.DominationQuiverEquipped)
                arrowProjectile.tileCollide = false;

            if (!convertToLeaf)
            {
                arrowProjectile.extraUpdates++;
                BFArrowCommon.ForceLocalNPCImmunity(arrowProjectile, preset == BlossomFluxChloroplastPresetType.Chlo_ABreak ? -1 : 10);
            }

            BFArrowCommon.TagBlossomFluxLeftArrow(arrowProjectile);
            BFArrow_CDetecEffect arrowEffect = arrowProjectile.GetGlobalProjectile<BFArrow_CDetecEffect>();
            arrowEffect.Preset = preset;
            arrowEffect.ConvertedLeafArrow = convertToLeaf;

            BFAccessoryGlobalProjectile accessoryEffect = arrowProjectile.GetGlobalProjectile<BFAccessoryGlobalProjectile>();
            accessoryEffect.BlossomFluxArrow = true;
            accessoryEffect.Preset = preset;
            return projectileIndex;
        }

        private float GetAccessoryArrowSpeedMultiplier(BlossomFluxChloroplastPresetType preset, bool convertedLeafArrow = false)
        {
            return BFAccessories.GetQuiverSpeedMultiplier(preset, convertedLeafArrow);
        }

        private void SpawnLeftMuzzleFX(Vector2 center, Vector2 velocity, BlossomFluxChloroplastPresetType preset, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(preset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(preset);
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);

            int dustCount = preset switch
            {
                BlossomFluxChloroplastPresetType.Chlo_CDetec => 8,
                BlossomFluxChloroplastPresetType.Chlo_DBomb => 10,
                BlossomFluxChloroplastPresetType.Chlo_EPlague => 5,
                _ => 6
            };

            for (int i = 0; i < dustCount; i++)
            {
                Vector2 dustVelocity =
                    direction.RotatedByRandom(preset == BlossomFluxChloroplastPresetType.Chlo_CDetec ? 0.52f : 0.26f) * Main.rand.NextFloat(1.1f, 3.4f) +
                    normal * Main.rand.NextFloat(-0.75f, 0.75f);
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(5f, 5f),
                    BFArrowCommon.GetPresetDustType(preset),
                    dustVelocity,
                    100,
                    Color.Lerp(mainColor, accentColor, Main.rand.NextFloat(0.16f, 0.58f)),
                    Main.rand.NextFloat(0.72f, 1.15f) * intensity);
                dust.noGravity = true;
            }

            switch (preset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, direction * 1.2f, mainColor, new Vector2(0.54f, 2.2f), direction.ToRotation(), 0.13f * intensity, 0.028f, 10));
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                    GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, Color.Lerp(mainColor, Color.White, 0.24f), 0.36f * intensity, 10));
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                    GeneralParticleHandler.SpawnParticle(new CritSpark(center + normal * Main.rand.NextFloat(-6f, 6f), direction * 1.8f, Color.White, accentColor, 0.82f * intensity, 11));
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                    GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, Color.Lerp(mainColor, Color.Goldenrod, 0.28f), Vector2.One, Main.rand.NextFloat(-0.2f, 0.2f), 0f, 0.12f * intensity, 9));
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(center + Main.rand.NextVector2Circular(4f, 4f), -direction * 0.6f + Main.rand.NextVector2Circular(0.35f, 0.35f), Color.Lerp(mainColor, Color.White, 0.08f), 14, 0.34f * intensity, 0.52f, Main.rand.NextFloat(-0.04f, 0.04f), false));
                    break;
            }
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
            reconShotsFiredInBurst = 0;
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

        private void OpenFormSwitchSelectionPanel()
        {
            int selectionPanelType = ModContent.ProjectileType<BFSelectionPanel>();

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Owner.whoAmI || projectile.type != selectionPanelType)
                    continue;

                if (projectile.ai[1] == BFSelectionPanel.FormSwitchMode && projectile.ai[0] != 1f && projectile.Opacity > 0.02f)
                    return;

                projectile.ai[0] = 1f;
                projectile.netUpdate = true;
            }

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.Center,
                Vector2.Zero,
                selectionPanelType,
                0,
                0f,
                Owner.whoAmI,
                0f,
                BFSelectionPanel.FormSwitchMode);

            SoundEngine.PlaySound(SoundID.MenuOpen with { Pitch = 0.1f, Volume = 0.55f }, Owner.Center);
        }

        // 生成瞄准镜弹幕；如果已经存在，就不重复生成
        private void EnsureAimScopeExists()
        {
            if (!ShouldUseAimScope)
            {
                KillAimScopeProjectiles();
                return;
            }

            int desiredScopes = GetDesiredAimScopeCount();
            if (desiredScopes <= 0)
            {
                KillAimScopeProjectiles();
                return;
            }

            bool[] existingSlots = new bool[desiredScopes];
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Owner.whoAmI || projectile.type != AimScopeProjectileType)
                    continue;

                int slotIndex = (int)projectile.ai[2];
                if (slotIndex < 0 || slotIndex >= desiredScopes)
                {
                    projectile.Kill();
                    projectile.netUpdate = true;
                    continue;
                }

                existingSlots[slotIndex] = true;
            }

            for (int slotIndex = 0; slotIndex < desiredScopes; slotIndex++)
            {
                if (existingSlots[slotIndex])
                    continue;

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTipPosition,
                    Vector2.Zero,
                    AimScopeProjectileType,
                    0,
                    0f,
                    Owner.whoAmI,
                    0f,
                    GetAimScopeMaxChargeFrames(),
                    slotIndex);
            }
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

        private bool HasEarlierActiveHoldout()
        {
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Owner.whoAmI || projectile.type != Type)
                    continue;

                if (projectile.whoAmI < Projectile.whoAmI)
                    return true;
            }

            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D weaponTexture = BlossomFluxTacticalTextures.GetWeaponTexture(CurrentPreset);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = weaponTexture.Size() * 0.5f;
            float rotation = Projectile.rotation;
            SpriteEffects effects = SpriteEffects.None;
            float chargeGlow = rightChargeActive ? MathHelper.SmoothStep(0f, 1f, ChargeCompletion) : 0f;
            float leftStarFlash = leftStarFlashTimer / (float)LeftStarFlashFrames;

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

            DrawThinWeaponOutline(weaponTexture, drawPosition, rotation, origin, effects);
            DrawTacticalWeaponOutlineGlow(weaponTexture, drawPosition, rotation, origin, effects);
            Main.EntitySpriteDraw(weaponTexture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, effects, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            DrawRestoredHoldoutCoreGlow(drawPosition, rotation, leftStarFlash, chargeGlow);
            if (rightChargeActive && reloadTimer <= 0)
            {
                DrawRestoredChargeAirFlow();
                DrawRestoredChargeArrowVisuals();
            }

            DrawBowCoreStarburst(drawPosition, rotation, leftStarFlash, chargeGlow);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private void DrawThinWeaponOutline(Texture2D weaponTexture, Vector2 drawPosition, float rotation, Vector2 origin, SpriteEffects effects)
        {
            Color outlineColor = (Color.Lerp(PresetColor, AccentColor, 0.35f) with { A = 0 }) * 0.28f;
            const int drawCount = 8;
            const float outlineRadius = 1.35f;

            for (int i = 0; i < drawCount; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / drawCount).ToRotationVector2() * outlineRadius;
                Main.EntitySpriteDraw(weaponTexture, drawPosition + offset, null, outlineColor, rotation, origin, Projectile.scale, effects, 0);
            }
        }

        private void DrawTacticalWeaponOutlineGlow(Texture2D weaponTexture, Vector2 drawPosition, float rotation, Vector2 origin, SpriteEffects effects)
        {
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
        }

        private void DrawBowCoreStarburst(Vector2 drawPosition, float rotation, float leftFlash, float chargeGlow)
        {
            float leftSustain = leftHeldLastFrame && !rightChargeActive ? 0.42f : 0f;
            float leftPower = Math.Max(MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(leftFlash, 0f, 1f)), leftSustain);
            float chargePower = rightChargeActive ? MathHelper.Lerp(0.24f, 1f, chargeGlow) : 0f;
            float power = MathHelper.Clamp(Math.Max(leftPower, chargePower), 0f, 1f);
            if (power <= 0.025f)
                return;

            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D pulseStarTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/PulseStar").Value;
            Texture2D halfStarTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Texture2D lineTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLine").Value;
            Texture2D sparkTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            float presetScale = GetBowCorePresetScale();
            float presetBrightness = GetBowCorePresetBrightness();
            float presetSpin = GetBowCorePresetSpinMultiplier();
            float time = Main.GlobalTimeWrappedHourly;
            float modeSpin = rightChargeActive
                ? MathHelper.Lerp(0.55f, 1.25f, chargeGlow)
                : MathHelper.Lerp(0.92f, 1.72f, leftPower);
            float spin = time * presetSpin * modeSpin;
            Color mainColor = (Color.Lerp(PresetColor, Color.White, 0.34f) with { A = 0 }) * power * presetBrightness;
            Color accentColor = (Color.Lerp(AccentColor, Color.White, 0.58f) with { A = 0 }) * power * presetBrightness;
            Color coreColor = (Color.Lerp(Color.White, AccentColor, 0.14f) with { A = 0 }) * power * presetBrightness;
            Vector2 forward = rotation.ToRotationVector2();
            float pulse = 0.9f + 0.1f * (float)Math.Sin(time * 9.2f + Projectile.identity * 0.41f);
            float visualScale = Projectile.scale * presetScale * MathHelper.Lerp(0.96f, 1.42f, power);
            float releaseTighten = rightChargeActive ? 1f : MathHelper.Lerp(0.82f, 1.08f, leftPower);

            Main.EntitySpriteDraw(
                bloomTexture,
                drawPosition,
                null,
                mainColor * (0.34f + 0.28f * power),
                rotation,
                bloomTexture.Size() * 0.5f,
                new Vector2(0.34f + power * 0.34f, 0.12f + power * 0.15f) * pulse * visualScale,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                bloomTexture,
                drawPosition,
                null,
                coreColor * (0.22f + 0.22f * power),
                -rotation,
                bloomTexture.Size() * 0.5f,
                new Vector2(0.12f + power * 0.11f, 0.12f + power * 0.09f) * visualScale,
                SpriteEffects.None,
                0);

            int spokeCount = rightChargeActive ? 12 : 10;
            for (int i = 0; i < spokeCount; i++)
            {
                float spokeCompletion = i / (float)spokeCount;
                float spokeRotation = rotation + MathHelper.TwoPi * spokeCompletion + spin * MathHelper.Lerp(0.22f, 0.48f, power);
                float spokePulse = 0.86f + 0.14f * (float)Math.Sin(time * 11.5f + i * 1.27f);
                Color spokeColor = Color.Lerp(mainColor, accentColor, i % 2 == 0 ? 0.18f : 0.76f) * (0.62f * spokePulse);

                Main.EntitySpriteDraw(
                    lineTexture,
                    drawPosition,
                    null,
                    spokeColor,
                    spokeRotation,
                    lineTexture.Size() * 0.5f,
                    new Vector2((0.16f + power * 0.28f) * releaseTighten, 2.25f + power * 2.05f) * visualScale,
                    SpriteEffects.None,
                    0);
            }

            Main.EntitySpriteDraw(
                pulseStarTexture,
                drawPosition,
                null,
                Color.Lerp(mainColor, Color.White with { A = 0 }, 0.3f) * 0.82f,
                -rotation + spin * 0.58f,
                pulseStarTexture.Size() * 0.5f,
                (0.095f + power * 0.13f) * pulse * visualScale,
                SpriteEffects.None,
                0);

            for (int i = 0; i < 5; i++)
            {
                float layerFade = 1f - i * 0.11f;
                for (int side = -1; side <= 1; side += 2)
                {
                    float sine = MathHelper.Lerp((float)Math.Sin(time * 7.6f + i * 0.65f), side, 0.72f);
                    float starRotation = rotation + MathHelper.PiOver4 * side + spin * power * (0.24f + i * 0.06f);
                    Vector2 starScale = new Vector2(0.22f + power * 0.075f, (0.92f + power * 1.08f) * sine * side) * layerFade * visualScale;
                    Color starColor = Color.Lerp(accentColor, coreColor, i * 0.12f) * (0.58f + 0.2f * power);

                Main.EntitySpriteDraw(
                    halfStarTexture,
                        drawPosition + forward * (i - 2) * 0.65f,
                        null,
                        starColor,
                    starRotation,
                    halfStarTexture.Size() * 0.5f,
                        starScale,
                    SpriteEffects.None,
                    0);
            }
        }

            for (int i = 0; i < 4; i++)
            {
                float sparkRotation = rotation + MathHelper.PiOver2 * i + spin * 0.72f;
                Main.EntitySpriteDraw(
                    sparkTexture,
                    drawPosition + sparkRotation.ToRotationVector2() * (4f + power * 6f),
                    null,
                    coreColor * 0.78f,
                    sparkRotation,
                    sparkTexture.Size() * 0.5f,
                    (0.085f + power * 0.09f) * visualScale,
                    SpriteEffects.None,
                    0);
            }
        }

        private float GetBowCorePresetScale() => CurrentPreset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => 1.08f,
            BlossomFluxChloroplastPresetType.Chlo_BRecov => 0.92f,
            BlossomFluxChloroplastPresetType.Chlo_CDetec => 1.02f,
            BlossomFluxChloroplastPresetType.Chlo_DBomb => 1.28f,
            BlossomFluxChloroplastPresetType.Chlo_EPlague => 1.13f,
            _ => 1f
        };

        private float GetBowCorePresetBrightness() => CurrentPreset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => 1.12f,
            BlossomFluxChloroplastPresetType.Chlo_BRecov => 0.9f,
            BlossomFluxChloroplastPresetType.Chlo_CDetec => 1.02f,
            BlossomFluxChloroplastPresetType.Chlo_DBomb => 1.26f,
            BlossomFluxChloroplastPresetType.Chlo_EPlague => 1.08f,
            _ => 1f
        };

        private float GetBowCorePresetSpinMultiplier() => CurrentPreset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => 1.12f,
            BlossomFluxChloroplastPresetType.Chlo_BRecov => 0.76f,
            BlossomFluxChloroplastPresetType.Chlo_CDetec => 1.34f,
            BlossomFluxChloroplastPresetType.Chlo_DBomb => 0.92f,
            BlossomFluxChloroplastPresetType.Chlo_EPlague => 1.18f,
            _ => 1f
        };

        private void DrawRestoredHoldoutCoreGlow(Vector2 drawPosition, float rotation, float leftFlash, float chargeGlow)
        {
            float leftPower = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(leftFlash, 0f, 1f));
            float chargePower = rightChargeActive ? MathHelper.Lerp(0.18f, 1f, chargeGlow) : 0f;
            float activity = MathHelper.Clamp(Math.Max(leftPower * 0.82f, chargePower), 0f, 1f);
            if (activity <= 0.025f)
                return;

            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D sparkTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Vector2 muzzlePosition = GunTipPosition - Main.screenPosition;
            Vector2 bodyPosition = Vector2.Lerp(Projectile.Center, GunTipPosition, 0.42f) - Main.screenPosition;
            Vector2 forward = AimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2) * Owner.gravDir;
            Color mainColor = (Color.Lerp(PresetColor, Color.White, 0.4f) with { A = 0 }) * activity;
            Color accentColor = (Color.Lerp(AccentColor, Color.White, 0.62f) with { A = 0 }) * activity;
            float time = Main.GlobalTimeWrappedHourly;
            float pulse = 0.86f + 0.14f * (float)Math.Sin(time * 7.8f + Projectile.identity * 0.33f);

            Main.EntitySpriteDraw(
                bloomTexture,
                bodyPosition,
                null,
                mainColor * (0.3f + activity * 0.34f),
                rotation,
                bloomTexture.Size() * 0.5f,
                new Vector2(0.36f + activity * 0.34f, 0.13f + activity * 0.12f) * pulse * Projectile.scale,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                bloomTexture,
                muzzlePosition + forward * 3f,
                null,
                accentColor * (0.25f + activity * 0.42f),
                rotation,
                bloomTexture.Size() * 0.5f,
                new Vector2(0.24f + activity * 0.28f, 0.08f + activity * 0.12f) * pulse * Projectile.scale,
                SpriteEffects.None,
                0);

            int sparkCount = 3 + (int)(activity * 4f);
            for (int i = 0; i < sparkCount; i++)
            {
                float phase = time * (rightChargeActive ? 0.92f : 1.45f) + i / (float)sparkCount;
                float travel = phase - MathF.Floor(phase);
                Vector2 sparkPosition =
                    drawPosition +
                    forward * MathHelper.Lerp(-20f, 22f, travel) +
                    normal * MathF.Sin(travel * MathHelper.TwoPi + i * 0.7f) * MathHelper.Lerp(4f, 9f, activity);

                Main.EntitySpriteDraw(
                    sparkTexture,
                    sparkPosition,
                    null,
                    Color.Lerp(mainColor, accentColor, i / Math.Max(1f, sparkCount - 1f)) * (0.32f + 0.24f * activity),
                    rotation + MathHelper.PiOver2,
                    sparkTexture.Size() * 0.5f,
                    new Vector2(0.024f, 0.13f + activity * 0.08f) * Projectile.scale,
                    SpriteEffects.None,
                    0);
            }
        }

        private void DrawRestoredChargeAirFlow()
        {
            float charge = MathHelper.SmoothStep(0f, 1f, ChargeCompletion);
            if (charge <= 0.025f)
                return;

            Texture2D smearTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/ForwardSmear").Value;
            Texture2D starTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2 forward = AimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2) * Owner.gravDir;
            Vector2 muzzle = GunTipPosition - Main.screenPosition;
            Color mainColor = (BFArrowCommon.GetPresetColor(CurrentPreset) with { A = 0 }) * (0.25f + charge * 0.42f);
            Color accentColor = (BFArrowCommon.GetPresetAccentColor(CurrentPreset) with { A = 0 }) * (0.18f + charge * 0.46f);
            float time = Main.GlobalTimeWrappedHourly;
            int streamCount = 3 + (int)MathF.Round(charge * 4f);

            for (int i = 0; i < streamCount; i++)
            {
                float lane = i - (streamCount - 1) * 0.5f;
                float phase = time * MathHelper.Lerp(1.1f, 2.6f, charge) + i * 0.173f + Projectile.identity * 0.013f;
                float travel = phase - MathF.Floor(phase);
                Vector2 position =
                    muzzle +
                    forward * MathHelper.Lerp(44f, -12f, travel) +
                    normal * lane * MathHelper.Lerp(3.5f, 6.5f, charge) +
                    normal * MathF.Sin(time * 4.3f + i) * MathHelper.Lerp(0.6f, 2.2f, charge);

                Main.EntitySpriteDraw(
                    smearTexture,
                    position,
                    null,
                    Color.Lerp(mainColor, accentColor, i / Math.Max(1f, streamCount - 1f)) * (0.46f + 0.24f * charge),
                    (-forward).ToRotation() - MathHelper.PiOver2,
                    new Vector2(smearTexture.Width * 0.5f, smearTexture.Height),
                    new Vector2(0.012f + charge * 0.014f, 0.22f + charge * 0.48f) * Projectile.scale,
                    SpriteEffects.None,
                    0);

                if (i % 2 != 0)
                    continue;

                Main.EntitySpriteDraw(
                    starTexture,
                    position + normal * MathF.Sign(lane == 0f ? 1f : lane) * (4f + charge * 5f),
                    null,
                    accentColor * 0.62f,
                    forward.ToRotation() + MathHelper.PiOver4,
                    starTexture.Size() * 0.5f,
                    new Vector2(0.05f + charge * 0.035f, 0.28f + charge * 0.32f) * Projectile.scale,
                    SpriteEffects.None,
                    0);
            }
        }

        private void DrawRestoredChargeArrowVisuals()
        {
            Texture2D arrowTexture = ModContent.Request<Texture2D>(BFArrowCommon.GetTexturePathForPreset(CurrentPreset)).Value;
            if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_ABreak)
            {
                DrawRestoredBreakthroughChargedArrows(arrowTexture);
                return;
            }

            if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_BRecov)
            {
                DrawRestoredRecoveryChargeCore();
                return;
            }

            DrawRestoredSpecialChargeArrow(arrowTexture);
        }

        private void DrawRestoredBreakthroughChargedArrows(Texture2D arrowTexture)
        {
            int maxArrows = Math.Max(1, BreakthroughMaxLoadedArrows);
            int loadedArrows = Utils.Clamp(breakthroughLoadedArrows, 0, maxArrows);
            bool fullyLoaded = loadedArrows >= maxArrows;
            int drawCount = fullyLoaded ? loadedArrows : Math.Min(loadedArrows + 1, maxArrows);
            if (drawCount <= 0)
                return;

            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D sparkTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Vector2 forward = AimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2) * Owner.gravDir;
            Vector2 origin = arrowTexture.Size() * 0.5f;
            float rotation = forward.ToRotation() + MathHelper.PiOver2 + MathHelper.Pi;
            Color loadedColor = Color.Lerp(Color.White, PresetColor, 0.58f);
            Color loadingColor = Color.Lerp(Color.White, loadedColor, BreakthroughCurrentArrowCompletion);

            for (int i = 0; i < drawCount; i++)
            {
                bool loadingArrow = !fullyLoaded && i == loadedArrows;
                float visibility = loadingArrow ? BreakthroughCurrentArrowCompletion : 1f;
                if (visibility <= 0.025f)
                    continue;

                float stackOffset = i - (drawCount - 1) * 0.5f;
                Vector2 drawWorld =
                    Projectile.Center +
                    forward * MathHelper.Lerp(23f, 33f, visibility) -
                    forward * i * 2.2f +
                    normal * stackOffset * 3.4f;
                Vector2 drawPosition = drawWorld - Main.screenPosition;
                float flash = !loadingArrow && breakthroughLoadFlashTimer > 0 && i == loadedArrows - 1
                    ? breakthroughLoadFlashTimer / (float)BreakthroughLoadFlashFrames
                    : 0f;
                float pulse = 1f + flash * 0.12f + (fullyLoaded ? MathF.Sin(Main.GlobalTimeWrappedHourly * 8f + i) * 0.035f : 0f);
                Color arrowColor = (loadingArrow ? loadingColor : loadedColor) * visibility;

                Main.EntitySpriteDraw(
                    bloomTexture,
                    drawPosition + forward * 8f,
                    null,
                    (AccentColor with { A = 0 }) * (0.16f + flash * 0.22f) * visibility,
                    rotation,
                    bloomTexture.Size() * 0.5f,
                    new Vector2(0.13f + flash * 0.05f, 0.045f + flash * 0.02f) * Projectile.scale,
                    SpriteEffects.None,
                    0);

                for (int strand = 0; strand < 2; strand++)
                {
                    float strandPhase = strand * MathHelper.Pi;
                    for (int segment = 0; segment < 5; segment++)
                    {
                        float completion = segment / 4f;
                        float phase = Main.GlobalTimeWrappedHourly * 8f + strandPhase + completion * MathHelper.TwoPi + i * 0.7f;
                        Vector2 helixPosition = drawPosition + forward * MathHelper.Lerp(-18f, 15f, completion) + normal * MathF.Sin(phase) * MathHelper.Lerp(7f, 2f, completion);

                        Main.EntitySpriteDraw(
                            sparkTexture,
                            helixPosition,
                            null,
                            (Color.Lerp(AccentColor, Color.White, 0.2f) with { A = 0 }) * (0.34f * visibility),
                            rotation,
                            sparkTexture.Size() * 0.5f,
                            new Vector2(0.025f, 0.12f + (1f - completion) * 0.08f) * Projectile.scale,
                            SpriteEffects.None,
                            0);
                    }
                }

                Main.EntitySpriteDraw(
                    arrowTexture,
                    drawPosition,
                    null,
                    arrowColor,
                    rotation,
                    origin,
                    MathHelper.Lerp(0.82f, 1.05f, visibility) * pulse * Projectile.scale,
                    SpriteEffects.None,
                    0);
            }
        }

        private void DrawRestoredRecoveryChargeCore()
        {
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 center = Vector2.Lerp(Projectile.Center, GunTipPosition, 0.36f) - Main.screenPosition;
            float charge = MathHelper.SmoothStep(0f, 1f, ChargeCompletion);
            float pulse = 0.84f + 0.16f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8.5f + Projectile.identity * 0.3f);
            Color green = new(98, 255, 142, 0);
            Color pale = new(222, 255, 232, 0);

            Main.EntitySpriteDraw(
                bloomTexture,
                center,
                null,
                green * (0.28f + charge * 0.44f),
                0f,
                bloomTexture.Size() * 0.5f,
                (0.1f + charge * 0.12f) * pulse * Projectile.scale,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                bloomTexture,
                center,
                null,
                pale * (0.16f + charge * 0.24f),
                0f,
                bloomTexture.Size() * 0.5f,
                (0.045f + charge * 0.065f) * pulse * Projectile.scale,
                SpriteEffects.None,
                0f);
        }

        private void DrawRestoredSpecialChargeArrow(Texture2D arrowTexture)
        {
            Vector2 forward = AimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2) * Owner.gravDir;
            Vector2 arrowDrawPosition = Projectile.Center + forward * MathHelper.Lerp(20f, 27f, ChargeCompletion) + normal * MathHelper.Lerp(-4f, -1f, ChargeCompletion) - Main.screenPosition;
            float pulse = readyBurstPlayed ? (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f) * 0.05f : 0f;
            float arrowScale = (0.88f + ChargeCompletion * 0.22f + pulse) * Projectile.scale;
            Color arrowColor = Color.Lerp(Color.White, PresetColor, 0.42f + 0.25f * ChargeCompletion);
            float arrowRotation = forward.ToRotation() + MathHelper.PiOver2 + MathHelper.Pi;

            DrawRestoredSpecialChargeOverlay(arrowDrawPosition, arrowRotation, arrowScale, forward, normal);

            Main.EntitySpriteDraw(
                arrowTexture,
                arrowDrawPosition,
                null,
                arrowColor,
                arrowRotation,
                arrowTexture.Size() * 0.5f,
                arrowScale,
                SpriteEffects.None,
                0);
        }

        private void DrawRestoredSpecialChargeOverlay(Vector2 arrowDrawPosition, float arrowRotation, float arrowScale, Vector2 forward, Vector2 normal)
        {
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D sparkTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            float charge = MathHelper.SmoothStep(0f, 1f, ChargeCompletion);
            Color mainColor = BFArrowCommon.GetPresetColor(CurrentPreset) with { A = 0 };
            Color accentColor = BFArrowCommon.GetPresetAccentColor(CurrentPreset) with { A = 0 };

            for (int i = -1; i <= 1; i++)
            {
                Vector2 offset = normal * i * (4f + 5f * charge);
                Main.EntitySpriteDraw(
                    bloomTexture,
                    arrowDrawPosition + offset * 0.5f + forward * 3f,
                    null,
                    Color.Lerp(mainColor, accentColor, i == 0 ? 0.55f : 0.25f) * (0.16f + charge * 0.2f),
                    arrowRotation,
                    bloomTexture.Size() * 0.5f,
                    new Vector2(0.12f + charge * 0.055f, 0.036f + charge * 0.016f) * arrowScale,
                    SpriteEffects.None,
                    0f);

                Main.EntitySpriteDraw(
                    sparkTexture,
                    arrowDrawPosition + offset - forward * (2f + charge * 4f),
                    null,
                    Color.Lerp(accentColor, Color.White with { A = 0 }, 0.18f) * (0.22f + charge * 0.28f),
                    arrowRotation + i * 0.14f,
                    sparkTexture.Size() * 0.5f,
                    new Vector2(0.026f, 0.16f + charge * 0.12f) * arrowScale,
                    SpriteEffects.None,
                    0f);
            }
        }

    }
}
