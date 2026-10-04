using CalamityMod;
using CalamityMod.Items.Fishing.BrimstoneCragCatches;
using CalamityMod.Items.Fishing.SunkenSeaCatches;
using CalamityMod.Items.Placeables.Crags;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.NPCs.TownNPCs;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL
{
    internal sealed class OtherNPCQuickShop : GlobalNPC
    {
        private static readonly Condition AllSentinelsDefeated = new(
            Language.GetText("Mods.CalamityLegendsComeBack.Conditions.AllSentinelsDefeated"),
            () => CalamityMod.DownedBossSystem.downedCeaselessVoid &&
                  CalamityMod.DownedBossSystem.downedStormWeaver &&
                  CalamityMod.DownedBossSystem.downedSignus);

        public override void ModifyShop(NPCShop shop)
        {
            if (CalamityLegendsComeBackConfig.Instance?.AllowOtherNPCQuickShop != true)
                return;

            switch (shop.NpcType)
            {
                case NPCID.Wizard:
                    shop.AddWithCustomValue(ItemID.RodofDiscord, Item.buyPrice(platinum: 1), Condition.Hardmode);
                    break;

                case NPCID.GoblinTinkerer:
                    shop.AddWithCustomValue(ItemID.NightKey, Item.buyPrice(gold: 5), Condition.Hardmode)
                        .AddWithCustomValue(ItemID.LightKey, Item.buyPrice(gold: 5), Condition.Hardmode)
                        .AddWithCustomValue(ItemID.GoldenKey, Item.buyPrice(gold: 6), Condition.DownedSkeletron)
                        .AddWithCustomValue(ItemID.ShadowKey, Item.buyPrice(gold: 50), Condition.DownedSkeletron);
                    break;

                case NPCID.Demolitionist:
                    shop.AddWithCustomValue<ScorchedBone>(Item.buyPrice(silver: 3));
                    break;

                case NPCID.ArmsDealer:
                    shop.Add<CrackshotColt>()
                        .Add<BulletFilledShotgun>(CalamityConditions.DownedHiveMindOrPerforator)
                        .Add<NitroExpressRifle>(Condition.Hardmode)
                        .Add<MidasPrime>(Condition.Hardmode)
                        .Add(ItemID.Uzi, Condition.Hardmode)
                        .Add(ItemID.ChainGun, Condition.DownedPlantera)
                        .Add(ItemID.TacticalShotgun, Condition.DownedPlantera)
                        .Add(ItemID.SniperRifle, Condition.DownedPlantera)
                        .Add<BlightSpewer>(CalamityConditions.DownedPlaguebringer)
                        .Add<Shredder>(Condition.DownedMoonLord)
                        .Add<TheAnomalysNanogun>(CalamityConditions.DownedDevourerOfGods);
                    break;

                case NPCID.BestiaryGirl:
                    shop.AddWithCustomValue<DragoonDrizzlefish>(Item.buyPrice(gold: 50))
                        .AddWithCustomValue<SparklingEmpress>(Item.buyPrice(gold: 50));
                    break;

                default:
                    if (shop.NpcType == ModContent.NPCType<Archmage>())
                    {
                        shop.Add<PhantasmalFury>(AllSentinelsDefeated)
                            .Add<ShadowboltStaff>(AllSentinelsDefeated)
                            .Add<VenusianTrident>(AllSentinelsDefeated)
                            .Add<NebulousCataclysm>(CalamityConditions.DownedDevourerOfGods)
                            .Add<IceBarrage>(CalamityConditions.DownedDevourerOfGods);
                    }
                    break;
            }
        }
    }
}
