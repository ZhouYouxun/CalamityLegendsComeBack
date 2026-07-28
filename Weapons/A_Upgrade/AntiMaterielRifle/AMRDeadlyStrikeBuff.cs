using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle
{
    internal sealed class AMRDeadlyStrikeBuff : ModBuff
    {
        public const int DurationFrames = 30;

        // Reuse Terraria's Striking Moment icon until bespoke art is available.
        public override string Texture => $"Terraria/Images/Buff_{BuffID.ParryDamageBuff}";
    }
}
