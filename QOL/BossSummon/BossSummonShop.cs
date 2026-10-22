using CalamityMod;
using CalamityMod.Items.DraedonMisc;
using CalamityMod.Items;
using CalamityMod.Items.SummonItems;
using CalamityMod.Items.SummonItems.Invasion;
using CalamityMod.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL.BossSummon
{
    internal static class BossSummonShop
    {
        // The quartermaster owns these offers; keep their prices and first-kill exceptions here.
        internal static void AddSummons(NPCShop shop)
        {
            // Keep vanilla and Calamity summons in encounter order. Summons are
            // the one shop category allowed to offer a boss before its first kill.
            shop.AddWithCustomValue(ItemID.SlimeCrown, Item.buyPrice(gold: 4))
                .AddWithCustomValue(ItemID.SuspiciousLookingEye, Item.buyPrice(gold: 4))
                .AddWithCustomValue<DesertMedallion>(Item.buyPrice(gold: 4))
                .AddWithCustomValue<DecapoditaSprout>(Item.buyPrice(gold: 4))
                .AddWithCustomValue(ItemID.WormFood, Item.buyPrice(gold: 4))
                .AddWithCustomValue(ItemID.BloodySpine, Item.buyPrice(gold: 4))
                .AddWithCustomValue<Teratoma>(Item.buyPrice(gold: 4))
                .AddWithCustomValue<BloodyWormFood>(Item.buyPrice(gold: 4))
                .AddWithCustomValue(ItemID.Abeemination, Item.buyPrice(gold: 4))
                .AddWithCustomValue(ItemID.DeerThing, Item.buyPrice(gold: 4))
                .AddWithCustomValue(ItemID.ClothierVoodooDoll, Item.buyPrice(gold: 4), Condition.DownedSkeletron)
                .AddWithCustomValue<OverloadedSludge>(Item.buyPrice(gold: 4))
                .AddWithCustomValue(ItemID.GuideVoodooDoll, Item.buyPrice(gold: 4), Condition.DownedEowOrBoc)
                .AddWithCustomValue(ItemID.QueenSlimeCrystal, Item.buyPrice(gold: 8), Condition.Hardmode)
                .AddWithCustomValue<CryoKey>(Item.buyPrice(gold: 8), Condition.Hardmode)
                .AddWithCustomValue<Seafood>(Item.buyPrice(gold: 8), Condition.Hardmode)
                .AddWithCustomValue<CharredIdol>(Item.buyPrice(gold: 8), Condition.Hardmode)
                .AddWithCustomValue(ItemID.MechanicalWorm, Item.buyPrice(gold: 8), Condition.Hardmode)
                .AddWithCustomValue(ItemID.MechanicalEye, Item.buyPrice(gold: 8), Condition.Hardmode)
                .AddWithCustomValue(ItemID.MechanicalSkull, Item.buyPrice(gold: 8), Condition.Hardmode)
                .AddWithCustomValue<EyeofDesolation>(Item.buyPrice(gold: 8), Condition.DownedMechBossAny)
                .AddWithCustomValue<Portabulb>(Item.buyPrice(gold: 8), Condition.DownedMechBossAny)
                .AddWithCustomValue(ItemID.TruffleWorm, Item.buyPrice(gold: 16), Condition.Hardmode)
                .AddWithCustomValue<AstralChunk>(Item.buyPrice(gold: 16), Condition.DownedPlantera)
                .AddWithCustomValue(ItemID.EmpressButterfly, Item.buyPrice(gold: 16), Condition.DownedPlantera)
                .AddWithCustomValue(ItemID.LihzahrdPowerCell, Item.buyPrice(gold: 16), Condition.DownedPlantera)
                .AddWithCustomValue<EidolonTablet>(Item.buyPrice(gold: 16), Condition.DownedPlantera)
                .AddWithCustomValue<Abombination>(Item.buyPrice(gold: 24), Condition.DownedGolem)
                .AddWithCustomValue<MartianDistressRemote>(Item.buyPrice(gold: 24), Condition.DownedGolem)
                .AddWithCustomValue<DeathWhistle>(Item.buyPrice(gold: 24), Condition.DownedGolem)
                .AddWithCustomValue(ItemID.CelestialSigil, Item.buyPrice(gold: 40), Condition.DownedCultist)
                .AddWithCustomValue<ExoticPheromones>(Item.buyPrice(gold: 40), Condition.DownedMoonLord)
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
