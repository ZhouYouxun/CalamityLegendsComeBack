using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables.Ores;
using CalamityMod.Items.Potions;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL.QuickStart
{
    public abstract class ProgressionSupplyBox : ModItem, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/QOL/QuickStart/QuickStartBox";
        public new string LocalizationCategory => "Items.Consumables";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: 1);
        }

        public override bool CanRightClick() => true;

        protected static void Give(ItemLoot loot, int itemType, int quantity) =>
            loot.Add(ItemDropRule.Common(itemType, 1, quantity, quantity));

        protected static void Give<T>(ItemLoot loot, int quantity) where T : ModItem =>
            Give(loot, ModContent.ItemType<T>(), quantity);
    }

    public sealed class HardmodeSupplyBox : ProgressionSupplyBox
    {
        public override void ModifyItemLoot(ItemLoot loot)
        {
            Give(loot, ItemID.SoulofLight, 100);
            Give(loot, ItemID.SoulofNight, 100);
            Give(loot, ItemID.SoulofFlight, 100);
            Give(loot, ItemID.CobaltOre, 200);
            Give(loot, ItemID.PalladiumOre, 200);
            Give(loot, ItemID.MythrilOre, 200);
            Give(loot, ItemID.OrichalcumOre, 200);
            Give(loot, ItemID.AdamantiteOre, 200);
            Give(loot, ItemID.TitaniumOre, 200);
            Give(loot, ItemID.GoldenCrateHard, 10);
            Give(loot, ItemID.LifeFruit, 20);
            Give(loot, ItemID.PirateMap, 1);
            Give<EssenceofEleum>(loot, 100);
            Give<EssenceofSunlight>(loot, 100);
            Give<EssenceofHavoc>(loot, 100);
            Give<StarblightSoot>(loot, 100);
            Give<TitanHeart>(loot, 5);
        }
    }

    public sealed class PostPlanteraSupplyBox : ProgressionSupplyBox
    {
        public override void ModifyItemLoot(ItemLoot loot)
        {
            Give(loot, ItemID.SolarTablet, 1);
            Give<SolarVeil>(loot, 25);
            Give<GrandScale>(loot, 5);
            Give<LivingShard>(loot, 50);
        }
    }

    public sealed class PostMoonLordSupplyBox : ProgressionSupplyBox
    {
        public override void ModifyItemLoot(ItemLoot loot)
        {
            Give<UnholyEssence>(loot, 100);
            Give(loot, ItemID.Ectoplasm, 100);
        }
    }

    public sealed class PostProvidenceSupplyBox : ProgressionSupplyBox
    {
        public override void ModifyItemLoot(ItemLoot loot)
        {
            Give<DivineGeode>(loot, 100);
            Give<Bloodstone>(loot, 100);
            Give<SupremeHealingPotion>(loot, 100);
        }
    }

    public sealed class PostDevourerSupplyBox : ProgressionSupplyBox
    {
        public override void ModifyItemLoot(ItemLoot loot)
        {
            Give<EndothermicEnergy>(loot, 200);
            Give<DarksunFragment>(loot, 200);
            Give<NightmareFuel>(loot, 200);
        }
    }

    public sealed class PostYharonSupplyBox : ProgressionSupplyBox
    {
        public override void ModifyItemLoot(ItemLoot loot)
        {
            Give<YharonSoulFragment>(loot, 50);
            Give<AuricOre>(loot, 1000);
        }
    }
}
