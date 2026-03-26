// SHPCEffectLoader.cs
using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera.Essence;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects
{
    public class SHPCEffectRegister : ModSystem
    {
        public override void PostSetupContent()
        {
            RegisterAllEffects();
        }

        private void RegisterAllEffects()
        {
            // 在这里注册所有弹药 → Effect

            EffectRegistry.RegisterEffect(new EnergyCoreEffect()); // 钨钢能源核心
            EffectRegistry.RegisterEffect(new PurifiedGelEffect()); // 纯净凝胶
            EffectRegistry.RegisterEffect(new StormlionMandibleEffect()); // 风暴之颚
            EffectRegistry.RegisterEffect(new SulphuricScaleEffect()); // 硫磺鳞片


            EffectRegistry.RegisterEffect(new EssenceofHavocEffect()); // 混沌精华 5
            EffectRegistry.RegisterEffect(new EssenceofSnowEffect()); // 冰川精华 6
            EffectRegistry.RegisterEffect(new EssenceofSunlightEffect()); // 日光精华 7

            EffectRegistry.RegisterEffect(new TitanHeartEffect()); // 泰坦之星 8

            // 9 10 11   12 13 14，为暂时未实现的六魂

            EffectRegistry.RegisterEffect(new LivingShardEffect()); // 生命碎片 15
            EffectRegistry.RegisterEffect(new EctoplasmEffect()); // 灵气 16
            EffectRegistry.RegisterEffect(new DepthCellsEffect()); // 深渊细胞 17
            EffectRegistry.RegisterEffect(new PlagueCellEffect()); // 瘟疫罐 18

            // 19、20 为跳过的灾厄尘和甲虫外壳

            EffectRegistry.RegisterEffect(new FragmentSolarEffect()); // 日耀碎片 21
            EffectRegistry.RegisterEffect(new FragmentVortexEffect()); // 漩涡碎片 22
            EffectRegistry.RegisterEffect(new FragmentNebulaEffect()); // 星云碎片 23
            EffectRegistry.RegisterEffect(new FragmentStardustEffect()); // 星尘碎片 24
            EffectRegistry.RegisterEffect(new FragmentEntropyEffect()); // 冥思溶剂 25



            EffectRegistry.RegisterEffect(new UnholyEssenceEffect()); // 浊火精华 26
            EffectRegistry.RegisterEffect(new NecroplasmEffect()); // 灵质 27

            EffectRegistry.RegisterEffect(new DivineGeodeEffect()); // 神圣晶石 28
            EffectRegistry.RegisterEffect(new BloodstoneCoreEffect()); // 血石核心 29
            EffectRegistry.RegisterEffect(new RuinousSoulEffect()); // 毁灭之灵 30

            EffectRegistry.RegisterEffect(new TitanHeartEffect()); // 扭曲虚空 31
            EffectRegistry.RegisterEffect(new DarkPlasmaEffect()); // 暗离子体 32







            // 以后就一直往这里加 ↓↓↓
            // EffectRegistry.RegisterEffect(new XXXEffect());
            // EffectRegistry.RegisterEffect(new YYYEffect());








        }
    }
}