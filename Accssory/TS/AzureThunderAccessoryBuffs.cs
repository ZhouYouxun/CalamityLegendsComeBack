using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.TS
{
    internal sealed class AzureThunderGuZhouDamageBuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/ATallbuff";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }
    }

    internal sealed class AzureThunderYiGanDamageBuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/ATallbuff";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }
    }

    internal sealed class AzureThunderGuZhouSlowDebuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/ATallbuff";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }
    }

    internal sealed class AzureThunderQingTingDebuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/ATallbuff";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }
    }
}
