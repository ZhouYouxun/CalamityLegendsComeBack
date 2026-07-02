using System;
using System.Collections.Generic;
using System.Linq;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.ZhuangFangYiPet
{
    internal sealed class ZhuangFangYiPetPlayer : ModPlayer
    {
        private const int StrongAttackCooldown = 17 * 60;
        private const int HarmonyAttackCooldown = 3 * 60;
        private const int WeakAttackInterval = 25 * 60;
        private const int CandidateResolveDelay = 4;

        private readonly List<int> strongAttackCandidates = new List<int>();
        private int candidateResolveTimer;
        private int queuedStrongTarget = -1;
        private int queuedWeakTarget = -1;
        private int weakAttackTimer = WeakAttackInterval;
        private int requestedHarmonyDuration;
        private bool transformRequested;

        public int StrongAttackCooldownTimer { get; private set; }

        public override void UpdateDead()
        {
            StrongAttackCooldownTimer = 0;
            candidateResolveTimer = 0;
            queuedStrongTarget = -1;
            queuedWeakTarget = -1;
            weakAttackTimer = WeakAttackInterval;
            requestedHarmonyDuration = 0;
            transformRequested = false;
            strongAttackCandidates.Clear();
        }

        public override void PostUpdateEquips()
        {
            if (!HasAzureThunderInInventory() || Player.dead)
                return;

            if (!AnyBossAlive() && ModContent.TryFind<ModBuff>("CalamityMod", "DoGExtremeGravity", out ModBuff extremeGravity))
            {
                Player.AddBuff(extremeGravity.Type, 4);
                if (Player.HasBuff(extremeGravity.Type) && Player.wingTimeMax > 0)
                    Player.wingTimeMax *= 2;
            }
        }

        public override void PostUpdate()
        {
            if (StrongAttackCooldownTimer > 0)
                StrongAttackCooldownTimer--;

            if (!HasAzureThunderInInventory() || Player.dead)
            {
                ResetPetRuntimeState();
                return;
            }

            Player.AddBuff(ModContent.BuffType<ZhuangFangYiPetBuff>(), 18000);

            if (Main.myPlayer == Player.whoAmI)
            {
                EnsurePetProjectile();
                HandleWeakAttackTimer();
            }

            ResolveStrongAttackCandidates();
        }

        public bool HasAzureThunderInInventory()
        {
            return TryGetAzureThunderItem(out _);
        }

        public bool TryGetAzureThunderItem(out Item azureThunder)
        {
            for (int i = 0; i < Player.inventory.Length; i++)
            {
                Item item = Player.inventory[i];
                if (item != null && !item.IsAir && item.type == ModContent.ItemType<AzureThunder>())
                {
                    azureThunder = item;
                    return true;
                }
            }

            azureThunder = null;
            return false;
        }

        public int GetAzureThunderDamage(float multiplier)
        {
            if (!TryGetAzureThunderItem(out Item azureThunder))
                return 1;

            return Math.Max(1, (int)(Player.GetWeaponDamage(azureThunder) * multiplier));
        }

        public float GetAzureThunderKnockback()
        {
            return TryGetAzureThunderItem(out Item azureThunder) ? azureThunder.knockBack : 0f;
        }

        public bool TryStartHarmonyTransform(int duration)
        {
            if (!HasAzureThunderInInventory() || transformRequested || Player.HasBuff(ModContent.BuffType<AzureThunderHarmonyBuff>()))
                return false;

            requestedHarmonyDuration = Math.Max(1, duration);
            transformRequested = true;
            queuedStrongTarget = -1;
            queuedWeakTarget = -1;
            weakAttackTimer = WeakAttackInterval;
            strongAttackCandidates.Clear();

            if (Main.myPlayer == Player.whoAmI)
                EnsurePetProjectile();

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.72f, Pitch = 0.22f }, Player.Center);
            return true;
        }

        internal bool TryConsumeTransformRequest(out int duration)
        {
            duration = requestedHarmonyDuration;
            if (!transformRequested || duration <= 0)
                return false;

            transformRequested = false;
            requestedHarmonyDuration = 0;
            return true;
        }

        internal void CompleteHarmonyTransform(int duration, Vector2 petCenter)
        {
            Player.GetModPlayer<AzureThunderPlayer>().StartHarmonyFromPet(duration);

            if (Main.myPlayer == Player.whoAmI)
            {
                AzureThunderPlayer.SpawnVerticalLightning(
                    Player.GetSource_FromThis(),
                    Player.Center,
                    null,
                    1,
                    0f,
                    Player.whoAmI,
                    visualOnly: true,
                    spawnHeightMultiplier: 0.72f,
                    fixedTiltRadians: 0f,
                    speedLines: true,
                    oneThirdVisualIntensity: true,
                    lightningScale: 1.1f);

                AzureThunderPlayer.SpawnVerticalLightning(
                    Player.GetSource_FromThis(),
                    petCenter,
                    null,
                    1,
                    0f,
                    Player.whoAmI,
                    visualOnly: true,
                    spawnHeightMultiplier: 0.58f,
                    fixedTiltRadians: MathHelper.ToRadians(-8f),
                    speedLines: true,
                    oneThirdVisualIntensity: true,
                    lightningScale: 0.9f);
            }

            Player.Calamity().GeneralScreenShakePower = Math.Max(Player.Calamity().GeneralScreenShakePower, 6f);
        }

        public void QueueStrongAttackCandidate(NPC target)
        {
            if (Main.myPlayer != Player.whoAmI ||
                StrongAttackCooldownTimer > 0 ||
                target == null ||
                !target.active ||
                !target.CanBeChasedBy() ||
                AzureThunderPlayer.CountElectroDebuffs(target) <= 0)
            {
                return;
            }

            if (!strongAttackCandidates.Contains(target.whoAmI))
                strongAttackCandidates.Add(target.whoAmI);

            candidateResolveTimer = CandidateResolveDelay;
        }

        internal bool TryConsumeStrongTarget(out int targetIndex)
        {
            targetIndex = queuedStrongTarget;
            if (targetIndex < 0)
                return false;

            queuedStrongTarget = -1;
            return true;
        }

        internal bool TryConsumeWeakTarget(out int targetIndex)
        {
            targetIndex = queuedWeakTarget;
            if (targetIndex < 0)
                return false;

            queuedWeakTarget = -1;
            return true;
        }

        private void EnsurePetProjectile()
        {
            int petType = ModContent.ProjectileType<ZhuangFangYiPetProjectile>();
            if (Player.ownedProjectileCounts[petType] > 0)
                return;

            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Center + new Vector2(-48f * Player.direction, -64f),
                Vector2.Zero,
                petType,
                0,
                0f,
                Player.whoAmI);
        }

        private void HandleWeakAttackTimer()
        {
            if (Player.HeldItem != null && !Player.HeldItem.IsAir && Player.HeldItem.type == ModContent.ItemType<AzureThunder>())
            {
                weakAttackTimer = WeakAttackInterval;
                return;
            }

            if (transformRequested || Player.HasBuff(ModContent.BuffType<AzureThunderHarmonyBuff>()))
            {
                weakAttackTimer = WeakAttackInterval;
                return;
            }

            if (queuedWeakTarget >= 0)
                return;

            if (weakAttackTimer > 0)
            {
                weakAttackTimer--;
                return;
            }

            NPC target = FindNearestPetTarget(Player.Center, 1600f, requireElectricDebuff: true);
            if (target != null)
            {
                queuedWeakTarget = target.whoAmI;
                weakAttackTimer = WeakAttackInterval;
            }
            else
                weakAttackTimer = 60;
        }

        private void ResolveStrongAttackCandidates()
        {
            if (candidateResolveTimer <= 0)
                return;

            candidateResolveTimer--;
            if (candidateResolveTimer > 0)
                return;

            NPC[] validTargets = strongAttackCandidates
                .Where(index => Main.npc.IndexInRange(index))
                .Select(index => Main.npc[index])
                .Where(npc => npc.active && npc.CanBeChasedBy() && AzureThunderPlayer.CountElectroDebuffs(npc) > 0)
                .ToArray();

            strongAttackCandidates.Clear();
            if (validTargets.Length <= 0)
                return;

            NPC[] bossTargets = validTargets.Where(npc => npc.boss).ToArray();
            NPC[] pool = bossTargets.Length > 0 ? bossTargets : validTargets;
            NPC chosen = pool[Main.rand.Next(pool.Length)];
            queuedStrongTarget = chosen.whoAmI;
            StrongAttackCooldownTimer = Player.HasBuff(ModContent.BuffType<AzureThunderHarmonyBuff>())
                ? HarmonyAttackCooldown
                : StrongAttackCooldown;
        }

        private void ResetPetRuntimeState()
        {
            queuedStrongTarget = -1;
            queuedWeakTarget = -1;
            weakAttackTimer = WeakAttackInterval;
            candidateResolveTimer = 0;
            requestedHarmonyDuration = 0;
            transformRequested = false;
            strongAttackCandidates.Clear();
        }

        internal static NPC FindNearestPetTarget(Vector2 origin, float maxDistance, bool requireElectricDebuff)
        {
            NPC bestTarget = null;
            float bestDistance = maxDistance;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;
                if (requireElectricDebuff && AzureThunderPlayer.CountElectroDebuffs(npc) <= 0)
                    continue;

                float distance = Vector2.Distance(origin, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        private static bool AnyBossAlive()
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.boss)
                    return true;
            }

            return false;
        }
    }
}
