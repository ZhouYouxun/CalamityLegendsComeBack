using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
using CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Passive.Pa5;
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
    internal sealed partial class NewLegendBlossomFluxHoldOut
    {
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

        private int GetInitialLeftFireDelay() => CurrentPreset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => GetNextBreakthroughFireInterval(),
            BlossomFluxChloroplastPresetType.Chlo_BRecov => RecoveryBurstInterval,
            BlossomFluxChloroplastPresetType.Chlo_CDetec => ReconFireInterval,
            BlossomFluxChloroplastPresetType.Chlo_DBomb => GetBombardAdjustedLeftFireDelay(),
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
                        SoundEngine.PlaySound(BlossomFluxSounds.LeftPastLingeringBurst, Owner.Center);
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
                    leftBurstTimer = GetBombardAdjustedLeftFireDelay();
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                    leftBurstTimer = PlagueFireInterval;
                    break;
            }
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

        private int GetBombardAdjustedLeftFireDelay()
        {
            int baseDelay = Math.Max(1, BFBombardLeftBalance.GetStats().FireInterval / 2);
            int passiveReduction = Owner.GetModPlayer<BFPa5BombardPlayer>().FireDelayReduction;
            return Math.Max(1, baseDelay - passiveReduction);
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

        private void FireBreakthroughShot(IEntitySource source, int projectileType, float speed, int damage, float knockback)
        {
            float finalSpeed = Math.Max(speed, BreakthroughSpeed) * BFBreakthroughLeftBalance.GetStats().ProjectileSpeedMultiplier * 0.8f;
            Vector2 shootVelocity = GetAimVelocity(finalSpeed);
            Vector2 spawnPosition = GetShootOrigin(shootVelocity);

            SpawnLeftProjectile(source, spawnPosition, shootVelocity, projectileType, damage, knockback, CurrentPreset);
            SpawnSHPCLeftMuzzleParticles(spawnPosition, shootVelocity, CurrentPreset, 1.05f);
            if (leftShotsFired % 3 == 0)
                SoundEngine.PlaySound(BlossomFluxSounds.LeftBreakthroughFire, Owner.Center);
        }

        private void FirePastLingeringVolley(IEntitySource source, int projectileType, float speed, int damage, float knockback)
        {
            int fireSpeedTier = GetPastLingeringFireSpeedTier();
            Vector2 direction = GetAimVelocity(1f).SafeNormalize(Vector2.UnitX * Owner.direction);
            float projectileSpeed = speed;
            Vector2 visualVelocity = direction * Owner.HeldItem.shootSpeed * 0.55f;
            Projectile.velocity = visualVelocity;

            for (int i = 0; i < 3; i++)
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

            SpawnSHPCLeftMuzzleParticles(GunTipPosition, direction * projectileSpeed, CurrentPreset, 0.94f + fireSpeedTier * 0.08f);
            if (leftShotsFired % 4 == 0)
                SoundEngine.PlaySound(BlossomFluxSounds.LeftPastLingeringFire, Owner.Center);
        }

        private void FireRecoveryVolley(IEntitySource source, int projectileType, float speed, int damage, float knockback)
        {
            Vector2 velocity = GetAimVelocity(speed);
            FireRecoveryDnaPair(source, velocity, projectileType, damage, knockback);
            SpawnSHPCLeftMuzzleParticles(GetShootOrigin(velocity), velocity, CurrentPreset, 0.92f + burstGroupsStarted * 0.08f);
            BlossomFluxSounds.PlayLeftRecoveryFire(Owner.Center, burstGroupsStarted);
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

            SpawnSHPCLeftMuzzleParticles(origin, baseVelocity, CurrentPreset, 0.92f + reconShotsFiredInBurst * 0.06f);
            BlossomFluxSounds.PlayLeftReconFire(Owner.Center, reconShotsFiredInBurst);
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
                SoundEngine.PlaySound(BlossomFluxSounds.LeftBombardFire1, Owner.Center);
            else
                SoundEngine.PlaySound(BlossomFluxSounds.LeftBombardFire2, Owner.Center);

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

            SpawnSHPCLeftMuzzleParticles(GetCurrentMouseWorld(), Vector2.UnitY * Owner.gravDir, CurrentPreset, 0.92f);
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

            SpawnSHPCLeftMuzzleParticles(spawnPosition, shootVelocity, CurrentPreset, 0.66f);
            BlossomFluxSounds.PlayLeftPlagueFire(spawnPosition);
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
    }
}
