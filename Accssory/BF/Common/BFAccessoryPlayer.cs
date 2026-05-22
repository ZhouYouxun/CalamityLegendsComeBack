using CalamityLegendsComeBack.Accssory.BF.SeedOfSilva;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.Common
{
    internal sealed class BFAccessoryPlayer : ModPlayer
    {
        public const int SilvaHarpPassiveCooldownFrames = 30 * 60;
        public const int SilvaHarpFinalStandImmuneFrames = 10 * 60;

        public int QuiverTier;
        public bool DominationQuiverEquipped;
        public bool SeedOfSilvaEquipped;
        public bool SilvaHarpEquipped;
        public bool PastLingeringEquipped;

        public bool HoldingBlossomFlux => Player.HeldItem?.type == ModContent.ItemType<NewLegendBlossomFlux>();

        public BlossomFluxChloroplastPresetType CurrentPreset =>
            HoldingBlossomFlux ? Player.GetModPlayer<BFRightUIPlayer>().CurrentPreset : BlossomFluxChloroplastPresetType.Chlo_ABreak;

        public override void ResetEffects()
        {
            QuiverTier = 0;
            DominationQuiverEquipped = false;
            SeedOfSilvaEquipped = false;
            SilvaHarpEquipped = false;
            PastLingeringEquipped = false;
        }

        public override void UpdateDead()
        {
            ResetEffects();
        }

        public override void PostUpdate()
        {
            if (SeedOfSilvaEquipped && Player.whoAmI == Main.myPlayer)
                EnsureSilvaSeeds();
        }

        public override void UpdateLifeRegen()
        {
            if (!SilvaHarpEquipped)
                return;

            Player.lifeRegen += 10;
            if (Player.lifeRegen < 10)
                Player.lifeRegen = 10;
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (SilvaHarpEquipped)
                Player.immuneTime += 60;
        }

        public void EquipQuiver(int tier)
        {
            QuiverTier = System.Math.Max(QuiverTier, tier);
            if (tier >= 3)
                DominationQuiverEquipped = true;
        }

        public float GetQuiverSpeedMultiplier(BlossomFluxChloroplastPresetType preset)
        {
            if (QuiverTier <= 0)
                return 1f;

            return QuiverTier switch
            {
                1 => preset switch
                {
                    BlossomFluxChloroplastPresetType.Chlo_ABreak => 1.08f,
                    BlossomFluxChloroplastPresetType.Chlo_BRecov => 1.06f,
                    BlossomFluxChloroplastPresetType.Chlo_CDetec => 1.10f,
                    BlossomFluxChloroplastPresetType.Chlo_DBomb => 1.04f,
                    BlossomFluxChloroplastPresetType.Chlo_EPlague => 1.07f,
                    _ => 1.08f
                },
                2 => preset switch
                {
                    BlossomFluxChloroplastPresetType.Chlo_ABreak => 1.16f,
                    BlossomFluxChloroplastPresetType.Chlo_BRecov => 1.12f,
                    BlossomFluxChloroplastPresetType.Chlo_CDetec => 1.18f,
                    BlossomFluxChloroplastPresetType.Chlo_DBomb => 1.10f,
                    BlossomFluxChloroplastPresetType.Chlo_EPlague => 1.14f,
                    _ => 1.16f
                },
                _ => preset switch
                {
                    BlossomFluxChloroplastPresetType.Chlo_ABreak => 1.25f,
                    BlossomFluxChloroplastPresetType.Chlo_BRecov => 1.18f,
                    BlossomFluxChloroplastPresetType.Chlo_CDetec => 1.28f,
                    BlossomFluxChloroplastPresetType.Chlo_DBomb => 1.15f,
                    BlossomFluxChloroplastPresetType.Chlo_EPlague => 1.20f,
                    _ => 1.25f
                }
            };
        }

        private void EnsureSilvaSeeds()
        {
            int seedType = ModContent.ProjectileType<SeedOfSilvaSeed>();
            bool[] existingSlots = new bool[SeedOfSilvaSeed.SeedCount];

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != Player.whoAmI || projectile.type != seedType)
                    continue;

                int slot = (int)projectile.ai[0];
                if (slot < 0 || slot >= existingSlots.Length || existingSlots[slot])
                {
                    projectile.Kill();
                    continue;
                }

                existingSlots[slot] = true;
            }

            for (int i = 0; i < existingSlots.Length; i++)
            {
                if (existingSlots[i])
                    continue;

                Projectile.NewProjectile(
                    Player.GetSource_FromThis(),
                    Player.Center,
                    Vector2.Zero,
                    seedType,
                    0,
                    0f,
                    Player.whoAmI,
                    i);
            }
        }
    }
}
