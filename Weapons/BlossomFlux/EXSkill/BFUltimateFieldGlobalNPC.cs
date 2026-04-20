using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill
{
    internal sealed class BFUltimateFieldGlobalNPC : GlobalNPC
    {
        private const int RefreshFrames = 2;
        private const float PlagueReleaseDistance = 16f;

        private readonly int[] fieldTimers = new int[Main.maxPlayers];
        private readonly int[] fieldDamage = new int[Main.maxPlayers];
        private readonly int[] bombardCooldown = new int[Main.maxPlayers];
        private readonly float[] plagueDistance = new float[Main.maxPlayers];
        private readonly Vector2[] lastTrackedPosition = new Vector2[Main.maxPlayers];
        private readonly BlossomFluxChloroplastPresetType[] activePreset = new BlossomFluxChloroplastPresetType[Main.maxPlayers];
        private readonly HashSet<int>[] breakthroughProjectileTypes = new HashSet<int>[Main.maxPlayers];

        public override bool InstancePerEntity => true;

        public override void PostAI(NPC npc)
        {
            if (!npc.active)
                return;

            for (int ownerIndex = 0; ownerIndex < Main.maxPlayers; ownerIndex++)
            {
                if (bombardCooldown[ownerIndex] > 0)
                    bombardCooldown[ownerIndex]--;

                if (fieldTimers[ownerIndex] <= 0)
                    continue;

                fieldTimers[ownerIndex]--;
                if (fieldTimers[ownerIndex] <= 0)
                {
                    ClearFieldState(ownerIndex);
                    continue;
                }

                if (activePreset[ownerIndex] == BlossomFluxChloroplastPresetType.Chlo_EPlague)
                    UpdatePlagueRelease(npc, ownerIndex);
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            int ownerIndex = projectile.owner;
            if (ownerIndex < 0 || ownerIndex >= Main.maxPlayers || fieldTimers[ownerIndex] <= 0)
                return;

            if (activePreset[ownerIndex] == BlossomFluxChloroplastPresetType.Chlo_ABreak &&
                breakthroughProjectileTypes[ownerIndex] != null &&
                breakthroughProjectileTypes[ownerIndex].Contains(projectile.type))
            {
                modifiers.SourceDamage *= 1.05f;
            }
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            int ownerIndex = projectile.owner;
            if (ownerIndex < 0 || ownerIndex >= Main.maxPlayers || fieldTimers[ownerIndex] <= 0)
                return;

            switch (activePreset[ownerIndex])
            {
                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                    (breakthroughProjectileTypes[ownerIndex] ??= new HashSet<int>()).Add(projectile.type);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                    TrySpawnBombardExplosion(npc, ownerIndex);
                    break;
            }
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            int ownerIndex = player.whoAmI;
            if (ownerIndex < 0 || ownerIndex >= Main.maxPlayers || fieldTimers[ownerIndex] <= 0)
                return;

            if (activePreset[ownerIndex] == BlossomFluxChloroplastPresetType.Chlo_DBomb)
                TrySpawnBombardExplosion(npc, ownerIndex);
        }

        public void RefreshFromField(int ownerIndex, BlossomFluxChloroplastPresetType preset, int sourceDamage, Vector2 currentCenter)
        {
            if (ownerIndex < 0 || ownerIndex >= Main.maxPlayers)
                return;

            if (fieldTimers[ownerIndex] <= 0 || activePreset[ownerIndex] != preset)
            {
                plagueDistance[ownerIndex] = 0f;
                lastTrackedPosition[ownerIndex] = currentCenter;

                if (preset != BlossomFluxChloroplastPresetType.Chlo_ABreak)
                    breakthroughProjectileTypes[ownerIndex]?.Clear();
            }

            fieldTimers[ownerIndex] = RefreshFrames;
            activePreset[ownerIndex] = preset;
            fieldDamage[ownerIndex] = System.Math.Max(sourceDamage, 1);
        }

        private void UpdatePlagueRelease(NPC npc, int ownerIndex)
        {
            Player owner = Main.player[ownerIndex];
            if (!owner.active || owner.dead)
                return;

            plagueDistance[ownerIndex] += Vector2.Distance(lastTrackedPosition[ownerIndex], npc.Center);
            lastTrackedPosition[ownerIndex] = npc.Center;

            if (plagueDistance[ownerIndex] < PlagueReleaseDistance || Main.myPlayer != ownerIndex)
                return;

            int spawnCount = (int)(plagueDistance[ownerIndex] / PlagueReleaseDistance);
            plagueDistance[ownerIndex] -= spawnCount * PlagueReleaseDistance;
            spawnCount = Utils.Clamp(spawnCount, 1, 3);

            for (int i = 0; i < spawnCount; i++)
            {
                Projectile.NewProjectile(
                    owner.GetSource_FromThis(),
                    npc.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextVector2Circular(0.8f, 0.8f),
                    ModContent.ProjectileType<BFArrow_EPlagueGas>(),
                    System.Math.Max(1, (int)(fieldDamage[ownerIndex] * 0.6f)),
                    0f,
                    ownerIndex,
                    Main.rand.Next(3),
                    0.9f);
            }
        }

        private void TrySpawnBombardExplosion(NPC npc, int ownerIndex)
        {
            if (bombardCooldown[ownerIndex] > 0 || Main.myPlayer != ownerIndex)
                return;

            bombardCooldown[ownerIndex] = 8;
            Player owner = Main.player[ownerIndex];
            Projectile.NewProjectile(
                owner.GetSource_FromThis(),
                npc.Center,
                Vector2.Zero,
                ModContent.ProjectileType<BFFuckyouExplosion>(),
                System.Math.Max(1, (int)(fieldDamage[ownerIndex] * 1.25f)),
                0f,
                ownerIndex);
        }

        private void ClearFieldState(int ownerIndex)
        {
            fieldDamage[ownerIndex] = 0;
            plagueDistance[ownerIndex] = 0f;
            bombardCooldown[ownerIndex] = 0;
            breakthroughProjectileTypes[ownerIndex]?.Clear();
        }
    }
}
