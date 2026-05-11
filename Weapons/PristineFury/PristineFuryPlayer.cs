using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    internal sealed class PristineFuryPlayer : ModPlayer
    {
        internal PristineFuryMark CurrentMark = PristineFuryMark.Idle;
        internal bool HoldingPristineFury;
        internal int HookChargeFrames;
        internal float HookChargeOpacity;

        public override void ResetEffects()
        {
            HoldingPristineFury = false;
            HookChargeFrames = 0;
            HookChargeOpacity = MathHelper.Clamp(HookChargeOpacity - 0.04f, 0f, 1f);
        }

        internal void ExtractMark(PristineFuryMark mark)
        {
            CurrentMark = mark;

            if (Main.myPlayer != Player.whoAmI)
                return;

            string markName = PristineFuryMarkHelper.GetName(mark);
            string text = Language.GetTextValue("Mods.CalamityLegendsComeBack.PristineFury.MarkExtracted", markName);
            CombatText.NewText(Player.getRect(), PristineFuryMarkHelper.GetColor(mark), text, dramatic: true);
        }
    }
}
