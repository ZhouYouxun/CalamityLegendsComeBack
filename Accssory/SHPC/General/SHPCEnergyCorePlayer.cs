using CalamityLegendsComeBack.Weapons.SHPC;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.General
{
    // Shared access for existing SHPC attack paths; upgrades now come from world progression.
    public sealed class SHPCEnergyCorePlayer : ModPlayer
    {
        public int EnergyCoreTier => BalanceSHPC.EnergyUpgradeTier;
        public bool HasEnergyCore => true;
        public bool AllowRightClickCrit => BalanceSHPC.EnergyAllowsRightClickCrit;
        public bool HasInfiniteSHPCMana => BalanceSHPC.HasInfiniteSHPCMana;
        public int BonusMagazineCount => BalanceSHPC.EnergyBonusMagazineCount;
        public float AmmoCapacityMultiplier => BalanceSHPC.EnergyAmmoCapacityMultiplier;
        public float SHPCDamageMultiplier => BalanceSHPC.EnergyDamageMultiplier;
        public float LeftManaCostMultiplier => BalanceSHPC.EnergyLeftManaCostMultiplier;
        public float RightManaCostMultiplier => BalanceSHPC.EnergyRightManaCostMultiplier;
        public float LeftAmmoSaveChance => BalanceSHPC.EnergyLeftAmmoSaveChance;
        public float SHPCCritBonus => BalanceSHPC.EnergyCritBonus;

        public bool ShouldSaveLeftClickAmmo()
        {
            return LeftAmmoSaveChance > 0f && Main.rand.NextFloat() < LeftAmmoSaveChance;
        }

        public int GetRightClickManaCost(int baseCost)
        {
            return System.Math.Max(0, (int)System.Math.Ceiling(baseCost * RightManaCostMultiplier));
        }

        public int GetRightClickStartupFrames(int defaultFrames)
        {
            return System.Math.Min(defaultFrames, 10);
        }
    }
}
