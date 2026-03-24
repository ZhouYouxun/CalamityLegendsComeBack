// SHPCEffectLoader.cs
using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode;
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




            // 以后就一直往这里加 ↓↓↓
            // EffectRegistry.RegisterEffect(new XXXEffect());
            // EffectRegistry.RegisterEffect(new YYYEffect());




            



        }
    }
}