using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    internal sealed class CosmicDischargeSwordBuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/CosmicDischarge/CosmicDischarge";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetCritChance(DamageClass.Generic) += 10f;
            player.GetDamage(DamageClass.Generic) += 0.12f;
            Lighting.AddLight(player.Center, CosmicDischargeCommon.DoGFuchsiaColor.ToVector3() * 0.25f);
        }
    }
}
