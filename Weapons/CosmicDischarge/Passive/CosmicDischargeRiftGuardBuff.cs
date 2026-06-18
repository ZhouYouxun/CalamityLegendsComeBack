using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    internal sealed class CosmicDischargeRiftGuardBuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/CosmicDischarge/CosmicDischarge";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.lifeRegenTime += 4;
            if (player.lifeRegen < 0)
                player.lifeRegen = 0;

            player.lifeRegen += 25;
            player.immune = true;
            player.immuneNoBlink = true;
            player.immuneTime = System.Math.Max(player.immuneTime, 4);
            Lighting.AddLight(player.Center, CosmicDischargeCommon.DoGSpecialColor.ToVector3() * 0.35f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    player.Center + Main.rand.NextVector2Circular(player.width * 0.55f, player.height * 0.55f),
                    DustID.PurpleTorch,
                    new Vector2(Main.rand.NextFloat(-0.45f, 0.45f), Main.rand.NextFloat(-2f, -0.8f)),
                    120,
                    CosmicDischargeCommon.RandomDoGColor(),
                    Main.rand.NextFloat(0.8f, 1.2f));
                dust.noGravity = true;
            }
        }
    }
}
