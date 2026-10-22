using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory;

public sealed class ShrineModelPlayer : ModPlayer
{
    internal bool Equipped;
    internal bool HideVisual;

    public override void ResetEffects()
    {
        Equipped = false;
        HideVisual = false;
    }
}
