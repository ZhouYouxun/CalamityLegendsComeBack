using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace CalamityLegendsComeBack
{
    [BackgroundColor(15, 27, 52, 230)]
    public sealed class CalamityLegendsComeBackConfig : ModConfig
    {
        public static CalamityLegendsComeBackConfig Instance;

        public override ConfigScope Mode => ConfigScope.ServerSide;

        public override void OnLoaded()
        {
            Instance = this;
        }

        [Header("RecipeSettings")]
        [BackgroundColor(20, 93, 160, 210)]
        [DefaultValue(true)]
        [ReloadRequired]
        public bool AllowMassMaterialRecipes;

        [BackgroundColor(20, 93, 160, 210)]
        [DefaultValue(true)]
        [ReloadRequired]
        public bool AllowBossRelicWeaponRecipes;

        [BackgroundColor(20, 93, 160, 210)]
        [DefaultValue(true)]
        [ReloadRequired]
        public bool AllowOtherRecipes;

        [DefaultValue(true)]
        [ReloadRequired]
        public bool AllowBossSummonShop;

        [DefaultValue(true)]
        [ReloadRequired]
        public bool AllowMechanicDraedonShop;

        [BackgroundColor(20, 93, 160, 210)]
        [DefaultValue(false)]
        public bool LightningDecryption;
    }
}
