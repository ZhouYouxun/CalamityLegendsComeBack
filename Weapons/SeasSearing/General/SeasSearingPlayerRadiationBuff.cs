using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // TODO: Replace texture with a custom player radiation buff icon (currently borrowing Irradiated sprite)
    public sealed class SeasSearingPlayerRadiationBuff : ModBuff
    {
        public override string Texture => "CalamityMod/Buffs/StatDebuffs/Irradiated";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<SeasSearingPlayer>().HasRadiationBuff = true;
        }
    }
}
