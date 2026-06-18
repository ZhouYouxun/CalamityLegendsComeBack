using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
using CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.Visuals;
using CalamityMod;
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
    internal sealed partial class NewLegendBlossomFluxHoldOut : BaseIdleHoldoutProjectile, ILocalizedModType
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
        // 左键每次开火时额外推进一点星芒角度，让核心更像 SHPC 那种“会转动的能量星”
        private float leftStarburstSpinKick;
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
        private int BreakthroughMaxLoadedArrows => Math.Max(1, BFBalanceTable.Get(BFStat.Breakthrough_Right_MaxArrows));
        private int BreakthroughFramesPerArrow => GetScaledRightChargeFrames(BFBreakthroughRightBalance.GetStats().FramesPerArrow);
        private float BreakthroughCurrentArrowCompletion => MathHelper.Clamp(chargeTimer / (float)BreakthroughFramesPerArrow, 0f, 1f);
        private float ChargeCompletion => BreakthroughChargeActive
            ? MathHelper.Clamp((breakthroughLoadedArrows + (breakthroughLoadedArrows >= BreakthroughMaxLoadedArrows ? 0f : BreakthroughCurrentArrowCompletion)) / BreakthroughMaxLoadedArrows, 0f, 1f)
            : MathHelper.Clamp(chargeTimer / (float)GetCurrentMaxChargeFrames(), 0f, 1f);
        private bool ChargeReady => BreakthroughChargeActive ? breakthroughLoadedArrows > 0 : chargeTimer >= GetCurrentReadyChargeFrames() && readyBurstPlayed;
        private Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        private Vector2 GunTipPosition => Projectile.Center + AimDirection * 28f;
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
            int baseFrames = CurrentPreset switch
            {
                BlossomFluxChloroplastPresetType.Chlo_BRecov => BFRecoveryRightBalance.GetStats().ChargeFrames,
                BlossomFluxChloroplastPresetType.Chlo_CDetec => BFReconRightBalance.GetStats().ChargeFrames,
                BlossomFluxChloroplastPresetType.Chlo_DBomb => BFBombardRightBalance.GetStats().ChargeFrames,
                _ => MaxChargeFrames
            };
            return GetScaledRightChargeFrames(baseFrames);
        }

        private int GetCurrentMaxChargeFrames()
        {
            if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb)
            {
                return GetScaledRightChargeFrames(BFBombardRightBalance.GetStats().ChargeFrames);
            }

            return GetCurrentReadyChargeFrames();
        }

        private int GetScaledRightChargeFrames(int baseFrames)
        {
            int floor = BreakthroughChargeActive ? MinBreakthroughChargeFrames : 1;
            return Math.Max(floor, (int)Math.Round(baseFrames * BFAccessories.ChargeMultiplier));
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
        }        private bool HandleFormSwitchInput()
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
        }        private void UpdateIdlePose()
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
        }        private void UpdateReloadAnimation()
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
                SoundEngine.PlaySound(BlossomFluxSounds.ReloadComplete, GunTipPosition);

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

        }        private Vector2 GetAimVelocity(float speed)
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

            SoundEngine.PlaySound(BlossomFluxSounds.SelectionPanelOpen, Owner.Center);
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

    }
}
