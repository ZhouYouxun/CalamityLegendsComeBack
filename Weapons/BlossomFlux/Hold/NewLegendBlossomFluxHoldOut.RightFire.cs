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
    internal sealed partial class NewLegendBlossomFluxHoldOut
    {
        private void BeginRightCharge()
        {
            CloseSelectionPanel();

            if (CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb && HasActiveBombardStrike())
            {
                SoundEngine.PlaySound(BlossomFluxSounds.RightBombardStrikeBlocked, Owner.Center);
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

            SoundEngine.PlaySound(BlossomFluxSounds.RightBeginCharge, Projectile.Center);

            // 瘟疫形态一进蓄力就把孢子球挂在准星前方，之后随蓄力长大。
            SpawnPlagueSporeOrb();
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
            UpdatePlagueSporeOrb();
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
            if (RecoveryChargePoseActive)
                Owner.GetModPlayer<BFAccessoryPlayer>().UpdateSilvaRecoveryAura();
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

            // 没甩出去的孢子球随蓄力一起散掉，已经脱手的不受影响。
            CancelPlagueSporeOrb();
            KillAimScopeProjectiles();
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
            BFAccessories.TriggerFairyRightAttack();
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
                    ReleasePlagueSporeOrb(chargeCompletion);
                    break;
            }
        }

        private int FireSpecialArrow(float chargeCompletion, int projectileType, float baseSpeed, float damageMultiplier)
        {
            float speed = MathHelper.Lerp(baseSpeed * 0.76f, baseSpeed * 1.22f, chargeCompletion) * GetAccessoryArrowSpeedMultiplier(CurrentPreset) * 0.8f;
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
            SpawnSHPCLeftMuzzleParticles(GunTipPosition, skyDirection * 12f, CurrentPreset, 1.3f);

            if (Projectile.owner != Main.myPlayer)
                return;

            int flashCount = stats.FlashCount + Owner.GetModPlayer<BFAccessoryPlayer>().RecoveryExtraFlashes;
            // The recovery transfers are spawned by the owning client, but shield durability is
            // server-authoritative. Start the new, empty shield on the server before any of the
            // returning transfers can replenish it.
            BFRecoveryShieldPackets.RequestStartBurst(Owner);

            Vector2 upward = -Vector2.UnitY * Owner.gravDir;
            Vector2 side = skyDirection.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.UnitX);

            for (int i = 0; i < flashCount; i++)
            {
                float progress = flashCount <= 1 ? 0.5f : i / (flashCount - 1f);
                Vector2 treeOffset = GetRecoveryTreeOffset(i, flashCount, upward, side);
                Vector2 spawnPosition = Owner.Center + treeOffset;
                Vector2 velocityDirection = Vector2.Lerp(upward, treeOffset.SafeNormalize(upward), 0.34f + progress * 0.2f).SafeNormalize(upward);
                Vector2 velocity = velocityDirection * MathHelper.Lerp(2.2f, 3.8f, progress) * GetAccessoryArrowSpeedMultiplier(BlossomFluxChloroplastPresetType.Chlo_BRecov, convertedLeafArrow: true) * 0.8f;

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
            {
                int markDuration = stats.MarkDuration + Owner.GetModPlayer<BFAccessoryPlayer>().ReconExtraMarkFrames;
                reconArrow.ConfigureMark(markDuration, stats.DamageAmpDuration, stats.EffectTier);
            }

            Main.projectile[projectileIndex].netUpdate = true;
        }

        private int GetCurrentRightClickDamage()
        {
            int baseDamage = damageBalance.GetRightClickBaseDamage(CurrentPreset);
            DamageClass damageType = Owner.HeldItem?.DamageType ?? DamageClass.Ranged;
            return (int)Owner.GetTotalDamage(damageType).ApplyTo(baseDamage);
        }

        private void FireBreakthroughSpecialArrows()
        {
            BFBreakthroughRightStats stats = BFBreakthroughRightBalance.GetStats();
            int arrowCount = Math.Max(1, breakthroughLoadedArrows);
            float speed = 21.6f * 1.22f * stats.ProjectileSpeedMultiplier * GetAccessoryArrowSpeedMultiplier(BlossomFluxChloroplastPresetType.Chlo_ABreak) * 0.8f;
            int damage = (int)(GetCurrentRightClickDamage() * RightClickBaseDamageMultiplier * 1.35f * 1.12f);
            damage = (int)(damage * (1f + arrowCount * stats.DamagePerChargeStack));
            float knockback = Projectile.knockBack * 1.15f;

            breakthroughQueuedShotCount = arrowCount;
            breakthroughQueuedShotIndex = 0;
            breakthroughQueuedShotTimer = 0;
            breakthroughQueuedDamage = damage;
            breakthroughQueuedSilvaDamage = (int)(GetCurrentRightClickDamage() * RightClickBaseDamageMultiplier * 1.5f);
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

            if (Owner.GetModPlayer<BFAccessoryPlayer>().SilvaHarpEquipped && Projectile.owner == Main.myPlayer)
                SpawnSilvaBreakthroughLeaves(shootDirection, breakthroughQueuedSpeed);

            SpawnSHPCLeftMuzzleParticles(spawnPosition, shootVelocity, CurrentPreset, 0.68f);
            BlossomFluxSounds.PlayRightBreakthroughQueuedFire(spawnPosition, breakthroughQueuedShotIndex);

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

        private void SpawnSilvaBreakthroughLeaves(Vector2 shootDirection, float speed)
        {
            Vector2 behind = -shootDirection;
            for (int i = 0; i < 2; i++)
            {
                float angle = Main.rand.NextFloat(-0.7f, 0.7f);
                float distance = Main.rand.NextFloat(70f, 150f);
                Vector2 spawnPosition = Owner.Center + behind.RotatedBy(angle) * distance + Main.rand.NextVector2Circular(12f, 12f);
                Vector2 target = GetCurrentMouseWorld() + Main.rand.NextVector2Circular(10f, 10f);
                Vector2 velocity = (target - spawnPosition).SafeNormalize(shootDirection) * speed;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<BFLeafProj>(),
                    System.Math.Max(1, breakthroughQueuedSilvaDamage),
                    breakthroughQueuedKnockback,
                    Projectile.owner,
                    (float)BlossomFluxChloroplastPresetType.Chlo_ABreak);
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
            BFBombardRightStats stats = BFBombardRightBalance.GetStats();
            float speed = MathHelper.Lerp(baseSpeed * 1.52f, baseSpeed * 2.24f, chargeCompletion) * GetAccessoryArrowSpeedMultiplier(BlossomFluxChloroplastPresetType.Chlo_DBomb) * 0.8f;
            int damage = (int)(GetCurrentRightClickDamage() * RightClickBaseDamageMultiplier * MathHelper.Lerp(0.8f, 1.35f, chargeCompletion) * damageMultiplier);
            float knockback = Projectile.knockBack * MathHelper.Lerp(0.85f, 1.15f, chargeCompletion);
            Vector2 bombardTarget = GetBombardReticleCenter();

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
            {
                BFAccessoryPlayer acc = Owner.GetModPlayer<BFAccessoryPlayer>();
                float explosionSize = stats.ExplosionSize + (acc.BombardExplosionBonus ? 60f : 0f);
                float rainMult = acc.BombardRainDamageBonus ? 1.5f : stats.SkyRainMultiplier;
                bombardArrow.ConfigureBombardTarget(bombardTarget, explosionSize, rainMult, stats.WaveCount);
            }

            Main.projectile[projectileIndex].netUpdate = true;
        }
    }
}
