using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeDevourerAscensionBuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/CosmicDischarge/CosmicDischarge";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetDamage(DamageClass.Generic) += 0.3f;
            player.moveSpeed += 0.15f;
            player.statDefense += 30;
            player.endurance += 0.1f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.15f;

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    player.Center + Main.rand.NextVector2Circular(player.width * 0.8f, player.height * 0.8f),
                    DustID.PurpleTorch,
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-3f, -1.2f)),
                    110,
                    CosmicDischargeCommon.RandomDoGColor(),
                    Main.rand.NextFloat(0.9f, 1.35f)
                );
                d.noGravity = true;
            }
        }
    }
}
