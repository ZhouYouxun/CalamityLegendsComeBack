using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    internal sealed class CosmicDischargeRiftRevivalBuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/CosmicDischarge/CosmicDischarge";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetDamage(DamageClass.Generic) += 0.20f;
            player.statDefense = (int)(player.statDefense * 1.20f);
            player.moveSpeed += 0.20f;
            Lighting.AddLight(player.Center, CosmicDischargeCommon.DoGSpecialColor.ToVector3() * 0.4f);
        }
    }
}
