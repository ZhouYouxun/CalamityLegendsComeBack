using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    internal sealed class CosmicDischargeChainKnifeBuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/CosmicDischarge/CosmicDischarge";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.moveSpeed += 0.12f;
            player.GetArmorPenetration(DamageClass.Generic) += 15f;
            Lighting.AddLight(player.Center, CosmicDischargeCommon.DoGPurpleColor.ToVector3() * 0.24f);
        }
    }
}
