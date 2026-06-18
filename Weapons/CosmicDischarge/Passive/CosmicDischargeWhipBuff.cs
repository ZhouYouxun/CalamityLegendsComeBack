using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    internal sealed class CosmicDischargeWhipBuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/CosmicDischarge/CosmicDischarge";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetAttackSpeed(DamageClass.Melee) += 0.12f;
            player.GetKnockback(DamageClass.Melee) += 0.15f;
            Lighting.AddLight(player.Center, CosmicDischargeCommon.DoGCyanColor.ToVector3() * 0.22f);
        }
    }
}
