using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    internal sealed class CosmicDischargeIceboundBuff : ModBuff
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
            Lighting.AddLight(player.Center, CosmicDischargeCommon.FrostCoreColor.ToVector3() * 0.35f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    player.Center + Main.rand.NextVector2Circular(player.width * 0.55f, player.height * 0.55f),
                    DustID.SnowflakeIce,
                    new Vector2(Main.rand.NextFloat(-0.45f, 0.45f), Main.rand.NextFloat(-2f, -0.8f)),
                    120,
                    CosmicDischargeCommon.FrostWhiteColor,
                    Main.rand.NextFloat(0.8f, 1.2f));
                dust.noGravity = true;
            }
        }
    }

    internal sealed class CosmicDischargeColdWindBuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/CosmicDischarge/CosmicDischarge";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetDamage(DamageClass.Generic) *= 1.5f;
            Lighting.AddLight(player.Center, new Color(130, 220, 255).ToVector3() * 0.4f);
        }
    }

    internal sealed class CosmicDischargeUltimateGuardBuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/CosmicDischarge/CosmicDischarge";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (!player.GetModPlayer<CosmicDischargePlayer>().UltimateFieldActive)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
                return;
            }

            player.statDefense += 20;
            Lighting.AddLight(player.Center, CosmicDischargeCommon.FrostGlowColor.ToVector3() * 0.35f);
        }
    }
}
