using CalamityLegendsComeBack.Accssory.PF;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    internal sealed class PristineFuryPlayer : ModPlayer
    {
        // Absolute max the array can hold (supports all lily tiers: default 3 + up to +4).
        internal const int MaxMarkSlots = 7;

        // Effective capacity this frame based on equipped lily accessories (3 to 7).
        internal int EffectiveMaxSlots =>
            Math.Clamp(3 + Player.GetModPlayer<PFAccessoryPlayer>().BonusMarkSlots, 3, MaxMarkSlots);

        internal readonly PristineFuryMark[] MarkQueue = new PristineFuryMark[MaxMarkSlots];
        internal int MarkQueueCount = 0;
        internal int SelectedMarkIndex = -1;

        internal PristineFuryMark CurrentMark =>
            SelectedMarkIndex >= 0 && SelectedMarkIndex < MarkQueueCount
                ? MarkQueue[SelectedMarkIndex]
                : PristineFuryMark.Idle;

        internal bool HoldingPristineFury;
        internal bool DebugCycleEquipped;
        internal int HookChargeFrames;
        internal float HookChargeOpacity;

        // Ultimate ("劫火重燃") energy meter. Charges only while marks are queued, spent in full on activation.
        internal const int UltimateEnergyMax = 3600;
        internal int UltimateEnergy;
        internal bool UltimateReady => UltimateEnergy >= UltimateEnergyMax;

        public override void ResetEffects()
        {
            HoldingPristineFury = false;
            DebugCycleEquipped = false;
            HookChargeFrames = 0;
            HookChargeOpacity = MathHelper.Clamp(HookChargeOpacity - 0.04f, 0f, 1f);
        }

        public override void PostUpdate()
        {
            // Charges purely from holding the weapon; deliberately independent of the left-click mark queue.
            if (HoldingPristineFury && UltimateEnergy < UltimateEnergyMax)
                UltimateEnergy = Math.Min(UltimateEnergyMax, UltimateEnergy + 1);
        }

        // Pushes a new mark to the back of the queue. If full, evicts the oldest (index 0).
        internal void PushMark(PristineFuryMark mark)
        {
            if (mark == PristineFuryMark.Idle)
                return;

            int cap = EffectiveMaxSlots;

            if (MarkQueueCount < cap)
            {
                MarkQueue[MarkQueueCount++] = mark;
            }
            else
            {
                // Shift everything left, add new mark at back.
                for (int i = 0; i < cap - 1; i++)
                    MarkQueue[i] = MarkQueue[i + 1];
                MarkQueue[cap - 1] = mark;
                // Shift selected index to stay on the same mark.
                if (SelectedMarkIndex > 0)
                    SelectedMarkIndex--;
            }

            // Auto-select first slot when nothing was selected.
            if (SelectedMarkIndex < 0)
                SelectedMarkIndex = 0;
        }

        // Selects a mark from the queue by index. Called from wheel UI on release.
        internal void SelectMark(int index)
        {
            if (index >= 0 && index < MarkQueueCount)
                SelectedMarkIndex = index;
        }

        internal void ExtractMark(PristineFuryMark mark, bool temporaryDebugSwitch = false)
        {
            if (temporaryDebugSwitch)
            {
                if (mark == PristineFuryMark.Idle)
                {
                    SelectedMarkIndex = -1;
                }
                else
                {
                    // Debug cycle: find existing mark in queue and select it, or push if absent.
                    bool found = false;
                    for (int i = 0; i < MarkQueueCount; i++)
                    {
                        if (MarkQueue[i] == mark)
                        {
                            SelectedMarkIndex = i;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        PushMark(mark);
                        SelectedMarkIndex = MarkQueueCount - 1;
                    }
                }
            }
            else
            {
                PushMark(mark);
            }

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
