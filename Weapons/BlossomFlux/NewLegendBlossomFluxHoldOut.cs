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
        private int BreakthroughMaxLoadedArrows => Math.Max(1, BFBreakthroughRightBalance.GetStats().MaxLoadedArrows);
        private int BreakthroughFramesPerArrow => Math.Max(MinBreakthroughChargeFrames, BFBreakthroughRightBalance.GetStats().FramesPerArrow);
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
            if (Owner is null)
                return false;

            Texture2D weaponTexture = BlossomFluxTacticalTextures.GetWeaponTexture(CurrentPreset);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = weaponTexture.Size() * 0.5f;
            float rotation = Projectile.rotation;
            SpriteEffects effects = SpriteEffects.None;
            float leftFlash = leftStarFlashTimer / (float)LeftStarFlashFrames;
            float chargeGlow = rightChargeActive && reloadTimer <= 0
                ? MathHelper.SmoothStep(0f, 1f, ChargeCompletion)
                : 0f;

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

            DrawSHPCWeaponOutline(weaponTexture, drawPosition, rotation, origin, effects, leftFlash, chargeGlow);
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

            // SHPC 本体就是“手持时枪口一直有核心光”，不能只在那 14 帧闪一下。
            // 上一版看不见，核心问题就是太小、太短、还被条件门控得太死。
            DrawSHPCMagicCore(CurrentPreset, 0.18f, 0f, false, true);

            if (rightChargeActive && reloadTimer <= 0)
                DrawSHPCRightChargeVisuals(CurrentPreset, chargeGlow);
            else
                DrawSHPCLeftAttackVisuals(CurrentPreset, leftFlash);

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

        private void DrawSHPCWeaponOutline(Texture2D weaponTexture, Vector2 drawPosition, float rotation, Vector2 origin, SpriteEffects effects, float leftFlash, float chargeGlow)
        {
            // 武器本体只保留 SHPC 式外轮廓光，不再叠符文、护盾、扫描线、叶流等主题杂项。
            Color mainColor = BFArrowCommon.GetPresetColor(CurrentPreset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(CurrentPreset);
            float leftPulse = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(leftFlash, 0f, 1f));
            float leftBuild = GetLeftAttackBuildGlow();
            float rightPulse = rightOutlinePulseTimer / (float)RightOutlinePulseFrames;
            float activity = MathHelper.Clamp(Math.Max(Math.Max(leftPulse * 0.65f, leftBuild), chargeGlow), 0f, 1f);
            float time = Main.GlobalTimeWrappedHourly;
            float idlePulse = 0.78f + 0.22f * (float)Math.Sin(time * 5.1f + Projectile.identity * 0.37f);
            float outlineDistance = 1.35f + activity * 2.65f + rightPulse * 2.2f;
            int outlineDraws = 8 + (int)(activity * 5f);
            Color outerColor = Color.Lerp(mainColor, Color.White, 0.45f) * (0.18f + activity * 0.34f) * idlePulse;
            Color innerColor = Color.Lerp(accentColor, Color.White, 0.55f) * (0.12f + activity * 0.22f);

            for (int i = 0; i < outlineDraws; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / outlineDraws + time * 0.75f).ToRotationVector2() * outlineDistance;
                Main.EntitySpriteDraw(weaponTexture, drawPosition + offset, null, outerColor, rotation, origin, Projectile.scale, effects, 0);
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = (MathHelper.PiOver2 * i - time * 0.5f).ToRotationVector2() * (0.8f + activity * 0.9f);
                Main.EntitySpriteDraw(weaponTexture, drawPosition + offset, null, innerColor, rotation, origin, Projectile.scale * (1f + activity * 0.02f), effects, 0);
            }

            if (rightPulse > 0.05f)
            {
                HoldoutOutlineHelper.DrawStarmadaRainbowOutline(
                    weaponTexture,
                    drawPosition,
                    rotation,
                    origin,
                    Vector2.One * Projectile.scale,
                    effects,
                    2.4f + rightPulse * 4.2f,
                    rightPulse * 0.38f,
                    time + Projectile.identity * 0.17f,
                    18,
                    manageBlendState: false);
            }
        }        private void DrawSHPCMagicCore(BlossomFluxChloroplastPresetType preset, float power, float phaseKick, bool rightCharge, bool idleCore)
        {
            if (Main.dedServ)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2 direction = AimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 muzzle = GunTipPosition + direction * 2f - Main.screenPosition;
            Color themeColor = BFArrowCommon.GetPresetColor(preset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(preset);
            Color cyan = Color.Lerp(themeColor, new Color(90, 210, 255), 0.35f);
            Color white = Color.Lerp(accentColor, new Color(230, 255, 255), 0.62f);
            float charge = MathHelper.Clamp(power, 0f, 1f);
            float flashPulse = idleCore ? 0f : charge;
            float time = Main.GlobalTimeWrappedHourly;

            // 清爽版 SHPC 星芒：保留 HalfStar 的锋利感，把 BloomCircle 压到只做轻微底光。
            // 这个武器的核心不需要像迫击炮/信标那样炸开一团大光，所以 Bloom 强度和尺寸都刻意收小。
            float bloomOpacity = idleCore
                ? 0.08f + charge * 0.09f
                : 0.10f + charge * 0.12f + flashPulse * 0.14f;
            Vector2 bloomScale = idleCore
                ? new Vector2(0.18f + charge * 0.08f, 0.09f + charge * 0.04f)
                : new Vector2(0.22f + charge * 0.12f + flashPulse * 0.10f, 0.11f + charge * 0.055f);

            Main.EntitySpriteDraw(
                bloom,
                muzzle,
                null,
                Color.Lerp(cyan, white, charge) * bloomOpacity,
                0f,
                bloom.Size() * 0.5f,
                bloomScale,
                SpriteEffects.None,
                0);

            int starCount = rightCharge ? 4 : 3;
            for (int i = 0; i < starCount; i++)
            {
                float rotation = direction.ToRotation() + MathHelper.TwoPi * i / starCount + time * (rightCharge ? 1.25f + i * 0.14f : 1.08f + i * 0.10f) + phaseKick;
                float starOpacity = idleCore
                    ? 0.12f + charge * 0.08f
                    : 0.20f + charge * 0.18f + flashPulse * 0.22f;
                Vector2 starScale = idleCore
                    ? new Vector2(0.12f, 0.48f + charge * 0.16f)
                    : new Vector2(0.18f + flashPulse * 0.12f, 0.72f + charge * 0.32f + flashPulse * 0.55f);

                Main.EntitySpriteDraw(
                    star,
                    muzzle,
                    null,
                    Color.Lerp(cyan, white, 0.58f) * starOpacity,
                    rotation,
                    star.Size() * 0.5f,
                    starScale,
                    SpriteEffects.None,
                    0);
            }
        }
    }
}
