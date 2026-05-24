using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    internal sealed class AzureThunderHarmonyBuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/AzureThunder/ATallbuff";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += 9;
            player.endurance += 0.09f;
            player.buffImmune[BuffID.ManaSickness] = true;

            // TODO: Adrenaline immunity.
            player.GetDamage(DamageClass.Generic) *= 1.35f;

            player.lifeRegenTime += 2;
            if (player.lifeRegen < 0)
                player.lifeRegen = 0;
            player.lifeRegen += 18;

            Lighting.AddLight(player.Center, AzureThunderColors.PaleYellow.ToVector3() * 0.45f);
        }
    }
}
