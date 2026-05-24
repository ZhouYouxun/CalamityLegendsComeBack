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

        public float GetQuiverSpeedMultiplier(BlossomFluxChloroplastPresetType preset, bool convertedLeafArrow = false)
        {
            if (QuiverTier <= 0)
                return 1f;

            int tierIndex = Utils.Clamp(QuiverTier - 1, 0, 2);

            return preset switch
            {
                BlossomFluxChloroplastPresetType.Chlo_ABreak => convertedLeafArrow ? BreakthroughWoodArrowSpeed[tierIndex] : BreakthroughOtherArrowSpeed[tierIndex],
                BlossomFluxChloroplastPresetType.Chlo_BRecov => RecoveryArrowSpeed[tierIndex],
                BlossomFluxChloroplastPresetType.Chlo_CDetec => ReconArrowSpeed[tierIndex],
                BlossomFluxChloroplastPresetType.Chlo_DBomb => BombardArrowSpeed[tierIndex],
                BlossomFluxChloroplastPresetType.Chlo_EPlague => PlagueArrowSpeed[tierIndex],
                _ => 1f
            };
        }

        // 箭袋速度加成：调谐 / 共鸣 / 统御。
        private static readonly float[] BreakthroughWoodArrowSpeed = { 1.33f, 1.66f, 2.00f };
        private static readonly float[] BreakthroughOtherArrowSpeed = { 2.00f, 2.25f, 3.00f };
        private static readonly float[] RecoveryArrowSpeed = { 1.33f, 1.66f, 2.00f };
        private static readonly float[] ReconArrowSpeed = { 1.15f, 1.30f, 1.45f };
        private static readonly float[] BombardArrowSpeed = { 1.20f, 1.40f, 1.60f };
        private static readonly float[] PlagueArrowSpeed = { 1.33f, 1.66f, 2.00f };

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
