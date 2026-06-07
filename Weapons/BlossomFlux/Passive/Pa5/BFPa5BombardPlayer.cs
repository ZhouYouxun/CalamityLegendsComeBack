using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Passive.Pa5
{
    internal sealed class BFPa5BombardPlayer : ModPlayer
    {
        public int FireDelayReduction => GetFireDelayReduction(Player);

        public override float UseTimeMultiplier(Item item)
        {
            if (!BFPa5PassiveSystem.IsActive(Player, BlossomFluxChloroplastPresetType.Chlo_DBomb) || item.type != ModContent.ItemType<NewLegendBlossomFlux>())
                return 1f;

            int reduction = FireDelayReduction;
            if (reduction <= 0)
                return 1f;

            return System.Math.Max(1, item.useTime - reduction) / (float)item.useTime;
        }

        public override float UseAnimationMultiplier(Item item)
        {
            if (!BFPa5PassiveSystem.IsActive(Player, BlossomFluxChloroplastPresetType.Chlo_DBomb) || item.type != ModContent.ItemType<NewLegendBlossomFlux>())
                return 1f;

            int reduction = FireDelayReduction;
            if (reduction <= 0)
                return 1f;

            return System.Math.Max(1, item.useAnimation - reduction) / (float)item.useAnimation;
        }

        public static int GetFireDelayReduction(Player player)
        {
            if (!BFPa5PassiveSystem.IsActive(player, BlossomFluxChloroplastPresetType.Chlo_DBomb))
                return 0;

            int enemies = BFPa5PassiveSystem.CountHostileEnemiesOnScreen();
            if (enemies >= 20)
                return 3;

            if (enemies >= 10)
                return 2;

            return enemies >= 5 ? 1 : 0;
        }
    }
}
