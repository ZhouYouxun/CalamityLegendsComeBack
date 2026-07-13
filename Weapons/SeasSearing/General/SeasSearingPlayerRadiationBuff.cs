using System;
using Terraria;
using Terraria.Localization;
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

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            int level = Math.Clamp(Main.LocalPlayer.GetModPlayer<SeasSearingPlayer>().RadiationLevel, 1, 4);
            string key = "Mods.CalamityLegendsComeBack.Buffs.SeasSearingPlayerRadiationBuff";
            buffName = Language.GetTextValue($"{key}.DisplayName_Level{level}");
            tip = Language.GetTextValue($"{key}.Description_Level{level}");
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<SeasSearingPlayer>().HasRadiationBuff = true;
        }
    }
}
