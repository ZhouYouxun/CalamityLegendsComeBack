using CalamityMod;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Passive.Pa5
{
    internal sealed class BFPa5RecoveryPlayer : ModPlayer
    {
        public override void PostUpdateMiscEffects()
        {
            if (BFPa5PassiveSystem.IsActive(Player, BlossomFluxChloroplastPresetType.Chlo_BRecov))
                Player.Calamity().externalDefenseDamageImmunity = true;
        }

        public override void UpdateLifeRegen()
        {
            if (BFPa5PassiveSystem.IsActive(Player, BlossomFluxChloroplastPresetType.Chlo_BRecov) && Player.lifeRegen < 0)
                Player.lifeRegen = (int)(Player.lifeRegen * 0.5f);
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (BFPa5PassiveSystem.IsActive(Player, BlossomFluxChloroplastPresetType.Chlo_BRecov))
                modifiers.ModifyHurtInfo += HalveExcessDamage;
        }

        private void HalveExcessDamage(ref Player.HurtInfo info)
        {
            int threshold = System.Math.Max(1, (int)(Player.statLifeMax2 * 0.3f));
            if (info.Damage > threshold)
                info.Damage = threshold + (info.Damage - threshold + 1) / 2;
        }
    }

    internal sealed class BFPa5RecoveryGlobalItem : GlobalItem
    {
        public override void GrabRange(Item item, Player player, ref int grabRange)
        {
            if (!BFPa5PassiveSystem.IsActive(player, BlossomFluxChloroplastPresetType.Chlo_BRecov))
                return;

            if (item.type == ItemID.Heart || item.type == ItemID.CandyApple || item.type == ItemID.CandyCane)
                grabRange = System.Math.Max(grabRange, 960);
        }
    }
}
