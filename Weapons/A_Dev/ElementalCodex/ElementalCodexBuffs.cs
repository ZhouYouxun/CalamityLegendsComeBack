using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.ElementalCodex
{
    internal abstract class ElementalCodexBaseDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
    }

    internal sealed class ElementalFireDebuff : ElementalCodexBaseDebuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/ElementalCodex/Buffs/ElementalFireDebuff";
    }

    internal sealed class ElementalWaterDebuff : ElementalCodexBaseDebuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/ElementalCodex/Buffs/ElementalWaterDebuff";
    }

    internal sealed class ElementalIceDebuff : ElementalCodexBaseDebuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/ElementalCodex/Buffs/ElementalIceDebuff";
    }

    internal sealed class ElementalLightningDebuff : ElementalCodexBaseDebuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/ElementalCodex/Buffs/ElementalLightningDebuff";
    }

    internal sealed class ElementalNatureDebuff : ElementalCodexBaseDebuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/ElementalCodex/Buffs/ElementalNatureDebuff";
    }

    internal sealed class ElementalDiseaseDebuff : ElementalCodexBaseDebuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/ElementalCodex/Buffs/ElementalDiseaseDebuff";
    }
}
