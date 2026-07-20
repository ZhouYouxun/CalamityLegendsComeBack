using CalamityMod;
using CalamityMod.Items.DraedonMisc;
using CalamityMod.Items;
using CalamityMod.Items.SummonItems;
using CalamityMod.Items.SummonItems.Invasion;
using CalamityMod.Systems;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL.BossSummon
{
    internal sealed class BossSummonShop : GlobalNPC
    {
        public override void ModifyShop(NPCShop shop)
        {
            if (shop.NpcType != NPCID.BestiaryGirl || CalamityLegendsComeBackConfig.Instance?.AllowBossSummonShop != true)
                return;

            // 困难模式前，任何时期：4金
            shop.AddWithCustomValue<DesertMedallion>(Item.buyPrice(gold: 4))
                .AddWithCustomValue<DecapoditaSprout>(Item.buyPrice(gold: 4))
                .AddWithCustomValue<Teratoma>(Item.buyPrice(gold: 4))
                .AddWithCustomValue<BloodyWormFood>(Item.buyPrice(gold: 4))
                .AddWithCustomValue<OverloadedSludge>(Item.buyPrice(gold: 4));

            // 困难模式后、任意机械Boss后（仍属于世纪之花前的困难模式阶段）：8金
            shop.AddWithCustomValue<CryoKey>(Item.buyPrice(gold: 8), Condition.Hardmode)
                .AddWithCustomValue<Seafood>(Item.buyPrice(gold: 8), Condition.Hardmode)
                .AddWithCustomValue<CharredIdol>(Item.buyPrice(gold: 8), Condition.Hardmode)
                .AddWithCustomValue<EyeofDesolation>(Item.buyPrice(gold: 8), Condition.DownedMechBossAny)
                .AddWithCustomValue<Portabulb>(Item.buyPrice(gold: 8), Condition.DownedMechBossAny);

            // 世纪之花后：16金
            shop.AddWithCustomValue<AstralChunk>(Item.buyPrice(gold: 16), Condition.DownedPlantera)
                .AddWithCustomValue<EidolonTablet>(Item.buyPrice(gold: 16), Condition.DownedPlantera);

            // 石巨人后：24金
            shop.AddWithCustomValue<Abombination>(Item.buyPrice(gold: 24), Condition.DownedGolem)
                .AddWithCustomValue<MartianDistressRemote>(Item.buyPrice(gold: 24), Condition.DownedGolem)
                .AddWithCustomValue<DeathWhistle>(Item.buyPrice(gold: 24), Condition.DownedGolem);

            // 月球领主后：40金
            shop.AddWithCustomValue<ExoticPheromones>(Item.buyPrice(gold: 40), Condition.DownedMoonLord)
                .AddWithCustomValue<ProfanedShard>(Item.buyPrice(gold: 40), Condition.DownedMoonLord)
                .AddWithCustomValue<ProfanedCore>(Item.buyPrice(gold: 40), Condition.DownedMoonLord)
                .AddWithCustomValue<NecroplasmicBeacon>(Item.buyPrice(gold: 40), Condition.DownedMoonLord)
                .AddWithCustomValue<CosmicWorm>(Item.buyPrice(gold: 40), Condition.DownedMoonLord)
                .AddWithCustomValue<YharonEgg>(Item.buyPrice(gold: 40), Condition.DownedMoonLord)
                .AddWithCustomValue<BloodwormItem>(Item.buyPrice(gold: 40), CalamityConditions.DownedPolterghast)
                .AddWithCustomValue<AuricQuantumCoolingCell>(Item.buyPrice(gold: 40), CalamityConditions.DownedYharon);
        }
    }
}
