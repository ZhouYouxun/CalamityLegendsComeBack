using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.YharonSoul
{
    internal class YharonSoulFragment_GN : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public int stack = 0;

        public override void ResetEffects(NPC npc)
        {
            if (!npc.HasBuff(ModContent.BuffType<YharonSoulFragment_Buff>()))
            {
                stack = 0;
            }
        }
    }
}