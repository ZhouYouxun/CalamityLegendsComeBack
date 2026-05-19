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

        internal void ExtractMark(PristineFuryMark mark, bool temporaryDebugSwitch = false)
        {
            CurrentMark = mark;

            if (Main.myPlayer != Player.whoAmI)
                return;

            string markName = PristineFuryMarkHelper.GetName(mark);
            string textKey = temporaryDebugSwitch
                ? "Mods.CalamityLegendsComeBack.PristineFury.DebugMarkSwitched"
                : "Mods.CalamityLegendsComeBack.PristineFury.MarkExtracted";
            string text = Language.GetTextValue(textKey, markName);
            Rectangle textArea = new((int)Player.Center.X - 24, (int)Player.Top.Y - 28, 48, 20);
            CombatText.NewText(textArea, PristineFuryMarkHelper.GetColor(mark), text, dramatic: true);
        }
    }
}
