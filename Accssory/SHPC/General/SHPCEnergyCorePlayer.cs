using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.General
{
    public sealed class SHPCEnergyCorePlayer : ModPlayer
    {
        public int EnergyCoreTier { get; private set; }

        public bool HasEnergyCore => EnergyCoreTier > 0;
        public bool AllowRightClickCrit => EnergyCoreTier >= 3;
        public bool HasInfiniteSHPCMana => EnergyCoreTier >= 4;
        public int BonusMagazineCount => Utils.Clamp(EnergyCoreTier, 0, 3);

        public float AmmoCapacityMultiplier => EnergyCoreTier switch
        {
            1 => 1.10f,
            2 => 1.25f,
            3 => 1.50f,
            >= 4 => 2f,
            _ => 1f
        };

        public float SHPCDamageMultiplier => EnergyCoreTier switch
        {
            1 => 1.07f,
            2 or 3 => 1.15f,
            >= 4 => 1.20f,
            _ => 1f
        };

        public float LeftManaCostMultiplier => EnergyCoreTier switch
        {
            1 => 0.92f,
            2 => 0.90f,
            3 => 0.85f,
            >= 4 => 0f,
            _ => 1f
        };

        public float RightManaCostMultiplier => EnergyCoreTier switch
        {
            1 => 0.88f,
            2 => 0.80f,
            3 => 0.70f,
            >= 4 => 0f,
            _ => 1f
        };

        public float LeftAmmoSaveChance => EnergyCoreTier switch
        {
            2 or 3 => 0.20f,
            >= 4 => 0.50f,
            _ => 0f
        };

        public float SHPCCritBonus => EnergyCoreTier switch
        {
            3 => 5f,
            >= 4 => 10f,
            _ => 0f
        };

        public override void ResetEffects()
        {
            EnergyCoreTier = 0;
        }

        public void SetEnergyCoreTier(int tier)
        {
            if (tier > EnergyCoreTier)
                EnergyCoreTier = tier;

            if (tier > 0)
                Player.manaFlower = true;
        }

        public bool ShouldSaveLeftClickAmmo()
        {
            return LeftAmmoSaveChance > 0f && Main.rand.NextFloat() < LeftAmmoSaveChance;
        }

        public int GetRightClickManaCost(int baseCost)
        {
            if (HasInfiniteSHPCMana)
                return 0;

            return System.Math.Max(0, (int)System.Math.Ceiling(baseCost * RightManaCostMultiplier));
        }

        public int GetRightClickStartupFrames(int defaultFrames)
        {
            return HasEnergyCore ? System.Math.Min(defaultFrames, 10) : defaultFrames;
        }

        public static bool IsEnergyCoreItem(int itemType)
        {
            return itemType == ModContent.ItemType<global::CalamityLegendsComeBack.Accssory.SHPC.General.WastelandEnergyCore.WastelandEnergyCore>() ||
                   itemType == ModContent.ItemType<global::CalamityLegendsComeBack.Accssory.SHPC.General.EverfrostEnergyCore.EverfrostEnergyCore>() ||
                   itemType == ModContent.ItemType<global::CalamityLegendsComeBack.Accssory.SHPC.General.EvolutionEnergyCore.EvolutionEnergyCore>() ||
                   itemType == ModContent.ItemType<global::CalamityLegendsComeBack.Accssory.SHPC.General.ExoEnergyCore.ExoEnergyCore>();
        }

        public static bool CanEquipWith(Item equippedItem, Item incomingItem)
        {
            return equippedItem == null ||
                   incomingItem == null ||
                   !IsEnergyCoreItem(equippedItem.type) ||
                   !IsEnergyCoreItem(incomingItem.type);
        }
    }
}
