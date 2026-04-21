using Microsoft.Xna.Framework;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core
{
    public readonly struct LeonidSelectedMetal
    {
        public LeonidSelectedMetal(int slotIndex, LeonidMetalEntry entry)
        {
            SlotIndex = slotIndex;
            Entry = entry;
        }

        public int SlotIndex { get; }
        public LeonidMetalEntry Entry { get; }
        public bool IsValid => SlotIndex >= 0 && Entry != null;
    }

    public static class LeonidMetalSelection
    {
        // Fixed at 2 for now. Do not expand this logic yet.
        public const int MaxTrackedMetals = 2;

        public static LeonidSelectedMetal[] Scan(Player player)
        {
            LeonidSelectedMetal[] result = new LeonidSelectedMetal[MaxTrackedMetals];
            if (player == null || !player.active)
                return result;

            bool[] seenEffectIDs = new bool[33];
            int foundCount = 0;

            for (int slotIndex = player.inventory.Length - 1; slotIndex >= 0; slotIndex--)
            {
                Item item = player.inventory[slotIndex];
                if (item == null || item.IsAir || item.stack <= 0)
                    continue;

                if (!LeonidMetalRegistry.TryGetByItemType(item.type, out LeonidMetalEntry entry))
                    continue;

                int effectID = entry.EffectID;
                if (effectID <= 0 || effectID >= seenEffectIDs.Length || seenEffectIDs[effectID])
                    continue;

                seenEffectIDs[effectID] = true;
                result[foundCount] = new LeonidSelectedMetal(slotIndex, entry);
                foundCount++;

                if (foundCount >= MaxTrackedMetals)
                    break;
            }

            return result;
        }

        public static int[] CaptureEffectIDs(Player player)
        {
            LeonidSelectedMetal[] selection = Scan(player);
            int[] effectIDs = new int[MaxTrackedMetals];
            for (int i = 0; i < MaxTrackedMetals; i++)
                effectIDs[i] = selection[i].IsValid ? selection[i].Entry.EffectID : 0;

            return effectIDs;
        }

        public static Color GetHighlightColor(LeonidSelectedMetal selectedMetal)
        {
            if (!selectedMetal.IsValid)
                return Color.Transparent;

            return selectedMetal.Entry.ThemeColor;
        }
    }
}
