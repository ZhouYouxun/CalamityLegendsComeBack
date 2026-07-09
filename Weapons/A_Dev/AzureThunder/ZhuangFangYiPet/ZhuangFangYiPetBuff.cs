using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.ZhuangFangYiPet
{
    internal sealed class ZhuangFangYiPetBuff : ModBuff
    {
        public new string LocalizationCategory => "Buffs";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/ZhuangFangYiPet/庄方宜宠物BUFF图标";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.vanityPet[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            ZhuangFangYiPetPlayer petPlayer = player.GetModPlayer<ZhuangFangYiPetPlayer>();
            if (!petPlayer.IsHoldingAzureThunder())
            {
                player.DelBuff(buffIndex);
                buffIndex--;
                return;
            }

            player.buffTime[buffIndex] = 18000;
        }
    }
}
