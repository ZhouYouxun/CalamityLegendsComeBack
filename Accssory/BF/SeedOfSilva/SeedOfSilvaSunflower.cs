using CalamityLegendsComeBack.Weapons.BlossomFlux;

namespace CalamityLegendsComeBack.Accssory.BF.SeedOfSilva
{
    internal sealed class SeedOfSilvaSunflower : SeedOfSilvaFlowerProjectile
    {
        protected override int FlowerSlot => 0;
        protected override BlossomFluxChloroplastPresetType FlowerPreset => BlossomFluxChloroplastPresetType.Chlo_ABreak;
        protected override string FlowerTexturePath => "CalamityLegendsComeBack/Accssory/BF/SeedOfSilva/SeedPack/Sunflower";
    }
}
