using System;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow;
using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    internal sealed class NewLegendBlossomFluxHoldOut : BaseIdleHoldoutProjectile, ILocalizedModType
    {
        private const int ParallelArrowCount = 3;
        private const int BurstGroupCount = 4;
        private const int NormalBurstGroupInterval = 15;
        private const int NormalEchoDelay = 4;
        private const int NormalLeafDelayFromBurstStart = 13;
        private const float ParallelSpacing = 18f;
        private const float LeafSpeed = 14f;

        private const float IdleOffsetLength = 22f;
        private const int ReloadFrames = 18;
        private const int MaxChargeFrames = 60;
        private const float ReadyPulseScale = 0.45f;
        private const float RightClickBaseDamageMultiplier = 3f;

        private int burstGroupsStarted;
        private int pendingEchoTimer = -1;
        private int pendingLeafTimer = -1;
        private int leftBurstTimer;
        private bool leftHeldLastFrame;

        private int reloadTimer;
        private int chargeTimer;
        private int chargeFxTimer;
        private bool rightChargeActive;
        private bool readyBurstPlayed;
        private bool releasedShot;
        private float offsetLengthFromArm = IdleOffsetLength;
        private float extraFrontArmRotation;
        private float extraBackArmRotation;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/NewLegendBlossomFlux";
        public override int AssociatedItemID => ModContent.ItemType<NewLegendBlossomFlux>();
        public override int IntendedProjectileType => ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>();

        private BlossomFluxChloroplastPresetType CurrentPreset => Owner.GetModPlayer<BFRightUIPlayer>().CurrentPreset;
        private float ChargeCompletion => MathHelper.Clamp(chargeTimer / (float)MaxChargeFrames, 0f, 1f);
        private bool ChargeReady => chargeTimer >= MaxChargeFrames && readyBurstPlayed;
        private Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        private Vector2 GunTipPosition => Projectile.Center + AimDirection * 42f;
        private Color PresetColor => BFArrowCommon.GetPresetColor(CurrentPreset);

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

            Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);
            Projectile.knockBack = Owner.HeldItem.knockBack;

            BFRightUIPlayer rightUIPlayer = Owner.GetModPlayer<BFRightUIPlayer>();
            BFEXPlayer exPlayer = Owner.GetModPlayer<BFEXPlayer>();

            if (Main.myPlayer == Projectile.owner)
                HandleOwnerLogic(rightUIPlayer, exPlayer);

            UpdateIdlePose();
            UpdateHeldProjectileVariables();
            ManipulatePlayerVariables();
        }

        private void HandleOwnerLogic(BFRightUIPlayer rightUIPlayer, BFEXPlayer exPlayer)
        {
            rightUIPlayer.ProcessRightClickState(HasActiveSelectionPanel(Owner));

            if (HandleLegendarySkill(rightUIPlayer, exPlayer))
                return;

            if (rightUIPlayer.ShortTapReleasedThisFrame && !rightChargeActive)
                ToggleSelectionPanel();

            if (rightUIPlayer.LongHoldReachedThisFrame && !rightChargeActive)
                BeginRightCharge();

            UpdateTimedLeftProjectiles();
            UpdateRightChargeState(rightUIPlayer);

            if (!rightChargeActive)
                HandleLeftClickInput();
        }

        private bool HandleLegendarySkill(BFRightUIPlayer rightUIPlayer, BFEXPlayer exPlayer)
        {
            if (!KeybindSystem.LegendarySkill.JustPressed)
                return false;

            if (!rightUIPlayer.UltimateUnlocked)
            {
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.48f, Pitch = -0.08f }, Owner.Center);
                return true;
            }

            if (!exPlayer.CanTriggerUltimate)
            {
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.46f, Pitch = 0.08f }, Owner.Center);
                return true;
            }

            exPlayer.StartUltimateCooldown();
            CloseSelectionPanel();
            CancelRightCharge();
            ResetBurstState(clearPendingProjectiles: true);
            CastUltimateField();
            return true;
        }

        private void UpdateTimedLeftProjectiles()
        {
            if (pendingEchoTimer > 0 && --pendingEchoTimer == 0)
                FireEchoVolley();

            if (pendingLeafTimer > 0 && --pendingLeafTimer == 0)
                FireLeafBody();
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
                !Owner.Calamity().mouseRight &&
                Main.mouseLeft &&
                !Owner.mouseInterface &&
                !(Main.playerInventory && Main.HoverItem.type == Owner.HeldItem.type);

            if (!validLeftInput)
            {
                if (!Main.mouseLeft)
                    ResetBurstState(clearPendingProjectiles: false);

                return;
            }

            if (!leftHeldLastFrame)
                leftBurstTimer = 0;

            leftHeldLastFrame = true;

            if (leftBurstTimer > 0)
            {
                leftBurstTimer--;
                return;
            }

            if (!Owner.PickAmmo(Owner.HeldItem, out int projectileType, out float speed, out int damage, out float knockback, out _, false))
            {
                leftBurstTimer = 4;
                return;
            }

            Vector2 velocity = GetAimVelocity(speed);
            FireParallelVolley(Projectile.GetSource_FromThis(), velocity, projectileType, damage, knockback);

            burstGroupsStarted++;
            pendingEchoTimer = NormalEchoDelay;
            if (burstGroupsStarted >= BurstGroupCount)
            {
                pendingLeafTimer = NormalLeafDelayFromBurstStart;
                burstGroupsStarted = 0;
            }

            leftBurstTimer = NormalBurstGroupInterval;
        }

        private void BeginRightCharge()
        {
            CloseSelectionPanel();
            rightChargeActive = true;
            reloadTimer = ReloadFrames;
            chargeTimer = 0;
            chargeFxTimer = 0;
            readyBurstPlayed = false;
            releasedShot = false;
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

        private void CancelRightCharge()
        {
            rightChargeActive = false;
            reloadTimer = 0;
            chargeTimer = 0;
            chargeFxTimer = 0;
            readyBurstPlayed = false;
            releasedShot = false;
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
                Vector2 aimTarget = Owner.Calamity().mouseWorld;
                if (aimTarget == Vector2.Zero)
                    aimTarget = Main.MouseWorld;

                Vector2 aimDirection = aimTarget - armPosition;
                if (aimDirection == Vector2.Zero)
                    aimDirection = Vector2.UnitX * Owner.direction;

                Vector2 desiredVelocity = aimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
                Vector2 oldVelocity = Projectile.velocity;
                Projectile.velocity = oldVelocity == Vector2.Zero ? desiredVelocity : Vector2.Lerp(oldVelocity, desiredVelocity, 0.35f);
                if (Vector2.DistanceSquared(oldVelocity, Projectile.velocity) > 0.0001f)
                    Projectile.netUpdate = true;
            }

            Projectile.Center = armPosition + AimDirection * offsetLengthFromArm;
            Projectile.rotation = AimDirection.ToRotation();
            Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;
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

        private void UpdateReloadAnimation()
        {
            float reloadProgress = 1f - reloadTimer / (float)ReloadFrames;
            extraFrontArmRotation = -0.05f * (1f - reloadProgress);
            extraBackArmRotation = 0.04f * (1f - reloadProgress);
            offsetLengthFromArm = MathHelper.Lerp(IdleOffsetLength - 10f, IdleOffsetLength, reloadProgress);

            if (chargeFxTimer % 4 == 0)
                SpawnReloadDust();

            if (reloadTimer == 1)
                SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.45f, Pitch = 0.1f }, GunTipPosition);

            reloadTimer--;
        }

        private void UpdateChargingAnimation()
        {
            offsetLengthFromArm = MathHelper.Lerp(IdleOffsetLength - 2f, IdleOffsetLength - 8f, ChargeCompletion);
            extraFrontArmRotation = -0.08f * ChargeCompletion;
            extraBackArmRotation = 0.05f * ChargeCompletion;

            SpawnChargingDust();

            if (chargeTimer % 10 == 0)
                SpawnChargeCircle();

            if (chargeTimer >= MaxChargeFrames && !readyBurstPlayed)
                PlayChargeReadyBurst();
        }

        private void UpdateChargedAnimation()
        {
            offsetLengthFromArm = IdleOffsetLength - 8f;
            extraFrontArmRotation = -0.08f;
            extraBackArmRotation = 0.05f;

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
            ReleaseChargedShot(CurrentPreset, ChargeCompletion);
        }

        private void ReleaseChargedShot(BlossomFluxChloroplastPresetType preset, float chargeCompletion)
        {
            switch (preset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                    FireSpecialArrow(chargeCompletion, ModContent.ProjectileType<BFArrow_ABreak>(), 18f, 1.12f);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                    FireSpecialArrow(chargeCompletion, ModContent.ProjectileType<BFArrow_BRecov>(), 16.25f, 0.94f);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                    FireSpecialArrow(chargeCompletion, ModContent.ProjectileType<BFArrow_CDetec>(), 18.75f, 0.92f);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                    FireSpecialArrow(chargeCompletion, ModContent.ProjectileType<BFArrow_DBomb>(), 17f, 0.88f);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                    FireSpecialArrow(chargeCompletion, ModContent.ProjectileType<BFArrow_EPlague>(), 15.5f, 0.98f);
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

            Vector2 inwardPosition = GunTipPosition + Main.rand.NextVector2Circular(16f, 16f);
            Vector2 inwardVelocity = (GunTipPosition - inwardPosition).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.9f, 2.1f);

            Dust dust = Dust.NewDustPerfect(
                inwardPosition,
                DustID.GemEmerald,
                inwardVelocity,
                100,
                Color.Lerp(PresetColor, Color.White, 0.2f + 0.3f * ChargeCompletion),
                Main.rand.NextFloat(0.8f, 1.25f));
            dust.noGravity = true;

            if (Main.rand.NextBool(3))
            {
                Dust glowDust = Dust.NewDustPerfect(
                    GunTipPosition + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.TerraBlade,
                    Main.rand.NextVector2Circular(0.6f, 0.6f),
                    100,
                    PresetColor,
                    Main.rand.NextFloat(0.9f, 1.35f));
                glowDust.noGravity = true;
            }
        }

        private void SpawnChargeCircle()
        {
            int points = 10;
            float radius = MathHelper.Lerp(14f, 26f, ChargeCompletion);

            for (int i = 0; i < points; i++)
            {
                float angle = MathHelper.TwoPi * i / points + Main.GlobalTimeWrappedHourly * 2.4f;
                Vector2 offset = angle.ToRotationVector2() * radius;

                Dust dust = Dust.NewDustPerfect(
                    GunTipPosition + offset,
                    DustID.GemEmerald,
                    -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.1f, 2.4f),
                    100,
                    Color.Lerp(PresetColor, Color.White, 0.35f),
                    Main.rand.NextFloat(0.85f, 1.2f));
                dust.noGravity = true;
            }
        }

        private void PlayChargeReadyBurst()
        {
            readyBurstPlayed = true;

            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f, Pitch = 0.25f }, GunTipPosition);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.4f, Pitch = -0.15f }, GunTipPosition);

            for (int i = 0; i < 20; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(1.5f, 4.2f);
                Dust dust = Dust.NewDustPerfect(
                    GunTipPosition,
                    DustID.TerraBlade,
                    velocity,
                    100,
                    Color.Lerp(PresetColor, Color.White, 0.5f),
                    Main.rand.NextFloat(1f, 1.5f));
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
            SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.72f, Pitch = -0.05f }, GunTipPosition);

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity =
                    AimDirection.RotatedByRandom(0.22f) * Main.rand.NextFloat(2.5f, 6f) +
                    Main.rand.NextVector2Circular(1f, 1f);

                Dust dust = Dust.NewDustPerfect(
                    GunTipPosition,
                    DustID.GemEmerald,
                    velocity,
                    100,
                    Color.Lerp(PresetColor, Color.White, 0.4f),
                    Main.rand.NextFloat(0.95f, 1.4f));
                dust.noGravity = true;
            }
        }

        private void FireEchoVolley()
        {
            if (!Owner.PickAmmo(Owner.HeldItem, out int projectileType, out float speed, out int damage, out float knockback, out _, false))
                return;

            FireParallelVolley(Projectile.GetSource_FromThis(), GetAimVelocity(speed), projectileType, damage, knockback);
        }

        private void FireLeafBody()
        {
            Vector2 velocity = GetAimVelocity(LeafSpeed);
            int damage = (int)(Owner.GetWeaponDamage(Owner.HeldItem) * 1.4f);
            int currentPreset = (int)Owner.GetModPlayer<BFRightUIPlayer>().CurrentPreset;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GetShootOrigin(velocity),
                velocity,
                ModContent.ProjectileType<BlossomFluxChloroplast>(),
                damage,
                Owner.HeldItem.knockBack,
                Owner.whoAmI,
                currentPreset);

            SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.2f, Volume = 0.8f }, Owner.Center);
        }

        private void FireParallelVolley(IEntitySource source, Vector2 velocity, int projectileType, int damage, float knockback)
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

                int projectileIndex = Projectile.NewProjectile(source, spawnPosition, shootVelocity, projectileType, damage, knockback, Owner.whoAmI);
                if (!projectileIndex.WithinBounds(Main.maxProjectiles))
                    continue;

                Projectile arrowProjectile = Main.projectile[projectileIndex];
                arrowProjectile.arrow = true;
                arrowProjectile.noDropItem = true;
                arrowProjectile.extraUpdates++;
                BFArrowCommon.TagBlossomFluxLeftArrow(arrowProjectile);
            }

            SoundEngine.PlaySound(SoundID.Item5 with { Pitch = Main.rand.NextFloat(-0.12f, 0.05f), Volume = 0.9f }, Owner.Center);
        }

        private Vector2 GetAimVelocity(float speed)
        {
            Vector2 aimTarget = Owner.Calamity().mouseWorld;
            if (aimTarget == Vector2.Zero)
                aimTarget = Main.MouseWorld;

            Vector2 aimDirection = aimTarget - Owner.RotatedRelativePoint(Owner.MountedCenter);
            if (aimDirection == Vector2.Zero)
                aimDirection = Vector2.UnitX * Owner.direction;

            return aimDirection.SafeNormalize(Vector2.UnitX * Owner.direction) * speed;
        }

        private Vector2 GetShootOrigin(Vector2 velocity)
        {
            Vector2 origin = Owner.RotatedRelativePoint(Owner.MountedCenter);
            Vector2 muzzleOffset = velocity.SafeNormalize(Vector2.UnitX * Owner.direction) * 34f;

            if (Collision.CanHit(origin, 0, 0, origin + muzzleOffset, 0, 0))
                origin += muzzleOffset;

            return origin;
        }

        private void ResetBurstState(bool clearPendingProjectiles)
        {
            burstGroupsStarted = 0;
            leftBurstTimer = 0;
            leftHeldLastFrame = false;

            if (clearPendingProjectiles)
            {
                pendingEchoTimer = -1;
                pendingLeafTimer = -1;
            }
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

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D weaponTexture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = weaponTexture.Size() * 0.5f;
            float rotation = Projectile.rotation;
            SpriteEffects effects = SpriteEffects.None;

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

            Main.EntitySpriteDraw(weaponTexture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, effects, 0);

            if (!rightChargeActive || reloadTimer > 0)
                return false;

            Texture2D arrowTexture = ModContent.Request<Texture2D>(BFArrowCommon.GetTexturePathForPreset(CurrentPreset)).Value;
            Vector2 arrowDrawPosition = GunTipPosition - AimDirection * MathHelper.Lerp(10f, 5f, ChargeCompletion) - Main.screenPosition;
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

        private void CastUltimateField()
        {
            int fieldType = ModContent.ProjectileType<BFUltimateField>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.active && projectile.owner == Owner.whoAmI && projectile.type == fieldType)
                    projectile.Kill();
            }

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.Center,
                Vector2.Zero,
                fieldType,
                Projectile.damage,
                0f,
                Owner.whoAmI,
                (int)CurrentPreset);
        }
    }
}
