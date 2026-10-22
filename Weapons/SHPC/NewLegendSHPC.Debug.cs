using System;
using System.Linq;
using CalamityLegendsComeBack.Accssory.SHPC;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.SHPC;

public partial class NewLegendSHPC
{
    private RulesOfEffect[] debugEffects = Array.Empty<RulesOfEffect>();
    private int debugSelectedIndex;
    public bool DebugModeActive => debugEffects.Length > 0;
    private RulesOfEffect DebugEffect => debugEffects[debugSelectedIndex];

    internal void SyncDebugMode(Player player, bool carried = true)
    {
        if (player.whoAmI != Main.myPlayer)
            return;

        bool enabled = carried && SHPCDebugTool.IsEnabled(player);
        if (enabled == DebugModeActive)
            return;

        // Destructive by design. Never back up or refund the original magazines.
        Array.Clear(magazineEffectPowers);
        Array.Clear(magazineAmmoTypes);
        Array.Clear(magazineEffectIDs);
        Array.Clear(magazineReserve);
        selectedMagazineIndex = debugSelectedIndex = 0;
        debugEffects = enabled
            ? EffectRegistry.GetRegisteredEffects().OrderBy(effect => effect.EffectID).ToArray()
            : Array.Empty<RulesOfEffect>();
        if (SHPCLoadingUI.IsOpen)
            SHPCLoadingUI.Close();
    }

    private SHPCMagazineSlot GetDebugMagazineSlot(int index)
    {
        index = Math.Clamp(index, 0, debugEffects.Length - 1);
        RulesOfEffect effect = debugEffects[index];
        return new SHPCMagazineSlot(index, effect.AmmoType, effect.EffectID,
            GetBaseAmmoCapacity(effect.EffectID), 0, index == debugSelectedIndex);
    }

    private bool SelectDebugMagazine(int index)
    {
        if (index < 0 || index >= debugEffects.Length)
            return false;
        debugSelectedIndex = index;
        return true;
    }

    public override void UpdateInventory(Player player) => SyncDebugMode(player);
}
