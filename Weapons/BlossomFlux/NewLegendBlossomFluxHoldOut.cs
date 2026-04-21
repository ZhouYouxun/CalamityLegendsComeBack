using System;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        private const float RailgunSightSize = 9f;
        private const float RailgunMaxSightAngle = MathHelper.Pi * (2f / 3f);

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
        private Vector2 ChargeFxAnchor => Projectile.Center - AimDirection * MathHelper.Lerp(11f, 6f, ChargeCompletion) + new Vector2(0f, MathHelper.Lerp(-7f, -4f, ChargeCompletion));
        private Color PresetColor => BFArrowCommon.GetPresetColor(CurrentPreset);
        private Color AccentColor => BFArrowCommon.GetPresetAccentColor(CurrentPreset);
        private bool BombardChargePoseActive => rightChargeActive && CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb;

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

            UpdateTimedLeftProjectiles();
            UpdateRightChargeState(rightUIPlayer);

            if (!rightChargeActive)
                HandleLeftClickInput();
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
                Vector2 desiredVelocity = GetDesiredHoldoutDirection(armPosition);
                Vector2 oldVelocity = Projectile.velocity;
                Projectile.velocity = oldVelocity == Vector2.Zero ? desiredVelocity : Vector2.Lerp(oldVelocity, desiredVelocity, 0.35f);
                if (Vector2.DistanceSquared(oldVelocity, Projectile.velocity) > 0.0001f)
                    Projectile.netUpdate = true;
            }

            Projectile.Center = armPosition + AimDirection * offsetLengthFromArm + GetHoldoutPositionOffset();
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
            if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb)
            {
                extraFrontArmRotation = MathHelper.Lerp(0.16f, 0.04f, reloadProgress);
                extraBackArmRotation = MathHelper.Lerp(0.26f, 0.1f, reloadProgress);
                offsetLengthFromArm = MathHelper.Lerp(IdleOffsetLength - 2f, IdleOffsetLength + 2f, reloadProgress);
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
            else
            {
                offsetLengthFromArm = MathHelper.Lerp(IdleOffsetLength - 2f, IdleOffsetLength - 8f, ChargeCompletion);
                extraFrontArmRotation = -0.08f * ChargeCompletion;
                extraBackArmRotation = 0.05f * ChargeCompletion;
            }

            SpawnChargingDust();

            if (chargeTimer % 10 == 0)
                SpawnChargeCircle();

            if (chargeTimer >= MaxChargeFrames && !readyBurstPlayed)
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
            ReleaseChargedShot(CurrentPreset, ChargeCompletion);
            Owner.GetModPlayer<BFEXPlayer>().GainEX(3);
        }

        private void ReleaseChargedShot(BlossomFluxChloroplastPresetType preset, float chargeCompletion)
        {
            switch (preset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                    FireSpecialArrow(chargeCompletion, ModContent.ProjectileType<BFArrow_ABreak>(), 21.6f, 1.12f);
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

        private void FireBombardSpecialArrow(float chargeCompletion, int projectileType, float baseSpeed, float damageMultiplier)
        {
            float speed = MathHelper.Lerp(baseSpeed * 0.76f, baseSpeed * 1.12f, chargeCompletion);
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
            readyBurstPlayed = true;
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

        private void DrawRailgunTelegraph()
        {
            float chargeVisual = MathHelper.SmoothStep(0f, 1f, ChargeCompletion);
            if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb)
            {
                DrawBombardTelegraph(chargeVisual);
                return;
            }

            Color scopeColor = Color.Lerp(PresetColor, AccentColor, 0.36f);
            DrawScopedAimTelegraph(scopeColor, chargeVisual, RailgunMaxSightAngle, RailgunSightSize, 0.04f, 7f);
        }

        private void DrawBombardTelegraph(float chargeVisual)
        {
            Color scopeColor = Color.Lerp(Color.Goldenrod, Color.Khaki, 0.55f);
            DrawScopedAimTelegraph(scopeColor, chargeVisual, RailgunMaxSightAngle * 0.9f, RailgunSightSize + 28f, 0.048f, 7.4f);
        }

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
            Vector2 aimDirection = GetCurrentMouseWorld() - Owner.RotatedRelativePoint(Owner.MountedCenter);
            if (aimDirection == Vector2.Zero)
                aimDirection = Vector2.UnitX * Owner.direction;

            return aimDirection.SafeNormalize(Vector2.UnitX * Owner.direction) * speed;
        }

        private Vector2 GetDesiredHoldoutDirection(Vector2 armPosition)
        {
            if (BombardChargePoseActive)
                return GetBombardSkyAimDirection();

            Vector2 aimDirection = GetCurrentMouseWorld() - armPosition;
            if (aimDirection == Vector2.Zero)
                aimDirection = Vector2.UnitX * Owner.direction;

            return aimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
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
            float outlinePulse = 0.78f + 0.22f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.8f + Projectile.identity * 0.43f);
            float outlineDistance = 1.7f + 0.8f * outlinePulse;
            Color outlineColor = Color.Lerp(PresetColor, AccentColor, 0.45f) * (0.65f + 0.22f * outlinePulse);
            Color glowColor = Color.Lerp(PresetColor, Color.White, 0.45f) * (0.34f + 0.18f * outlinePulse);

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
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < 16; i++)
            {
                float angle = MathHelper.TwoPi * i / 16f;
                Vector2 offset = angle.ToRotationVector2() * outlineDistance;
                Main.EntitySpriteDraw(
                    weaponTexture,
                    drawPosition + offset,
                    null,
                    outlineColor,
                    rotation,
                    origin,
                    Projectile.scale,
                effects,
                0);
            }

            Main.EntitySpriteDraw(
                weaponTexture,
                drawPosition,
                null,
                glowColor,
                rotation,
                origin,
                Projectile.scale * (1.04f + 0.05f * outlinePulse),
                effects,
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

            Main.EntitySpriteDraw(weaponTexture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, effects, 0);

            if (!rightChargeActive || reloadTimer > 0)
                return false;

            DrawRailgunTelegraph();

            Texture2D arrowTexture = ModContent.Request<Texture2D>(BFArrowCommon.GetTexturePathForPreset(CurrentPreset)).Value;
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
    }
}
