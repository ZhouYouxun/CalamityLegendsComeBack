using CalamityMod;
using CalamityMod.Items.DraedonMisc;
using CalamityMod.Items.Placeables.DraedonStructures;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.QOL.DraedonShop
{
    /// <summary>
    /// The Mechanic stocks Draedon's arsenal hardware as progression opens it up.
    /// Schematics are never sold; only the codebreaker parts they would unlock.
    /// </summary>
    internal sealed class MechanicDraedonShop : GlobalNPC
    {
        public override void ModifyShop(NPCShop shop)
        {
            if (shop.NpcType != NPCID.Mechanic || CalamityLegendsComeBackConfig.Instance?.AllowMechanicDraedonShop != true)
                return;

            // 初始解锁：救出机械师本身就意味着骷髅王已被击败，无需额外条件
            shop.AddWithCustomValue<DraedonPowerCell>(Item.buyPrice(silver: 10))
                .AddWithCustomValue<ChargingStationItem>(Item.buyPrice(gold: 2))
                .AddWithCustomValue<PowerCellFactoryItem>(Item.buyPrice(gold: 5))
                .AddWithCustomValue<CodebreakerBase>(Item.buyPrice(gold: 5));

            // 解密计算机对应沉没之海蓝图（T1），没有Boss门槛
            shop.AddWithCustomValue<DecryptionComputer>(Item.buyPrice(gold: 8));

            // 长程传感器阵列对应小行星实验室蓝图（T2），材料需要秘银/山铜
            shop.AddWithCustomValue<LongRangedSensorArray>(Item.buyPrice(gold: 16), Condition.Hardmode);

            // 高级显示器对应丛林蓝图（T3），材料需要生命合金
            shop.AddWithCustomValue<AdvancedDisplay>(Item.buyPrice(gold: 24), Condition.DownedPlantera);

            // 电压调节系统对应地狱蓝图（T4），材料需要夜明锭与神明之花
            shop.AddWithCustomValue<VoltageRegulationSystem>(Item.buyPrice(gold: 40), CalamityConditions.DownedProvidence);

            // 极光量子冷却电池对应冰原蓝图（T5），材料需要极光锭
            shop.AddWithCustomValue<AuricQuantumCoolingCell>(Item.buyPrice(gold: 40), CalamityConditions.DownedYharon);
        }
    }
}
