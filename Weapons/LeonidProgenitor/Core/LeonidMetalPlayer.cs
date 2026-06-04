using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core
{
    public class LeonidMetalPlayer : ModPlayer
    {
        private readonly int[] highlightedSlots = new int[LeonidMetalSelection.MaxTrackedMetals];
        private readonly Color[] highlightedColors = new Color[LeonidMetalSelection.MaxTrackedMetals];

        public bool HoldingLeonid { get; private set; }

        public override void Initialize()
        {
            ClearHighlights();
        }

        public override void ResetEffects()
        {
            HoldingLeonid = false;
            ClearHighlights();
        }

        public void UpdateHighlights(LeonidSelectedMetal[] metals)
        {
            HoldingLeonid = true;
            ClearHighlights();

            if (metals == null)
                return;

            for (int i = 0; i < metals.Length && i < highlightedSlots.Length; i++)
            {
                if (!metals[i].IsValid)
                    continue;

                highlightedSlots[i] = metals[i].SlotIndex;
                highlightedColors[i] = metals[i].Entry.ThemeColor;
            }
        }

        public bool TryGetHighlight(Item item, out Color color)
        {
            color = Color.Transparent;
            if (!HoldingLeonid || !Main.playerInventory || item == null)
                return false;

            for (int i = 0; i < highlightedSlots.Length; i++)
            {
                int slotIndex = highlightedSlots[i];
                if (slotIndex < 0 || slotIndex >= Player.inventory.Length)
                    continue;

                if (!ReferenceEquals(item, Player.inventory[slotIndex]))
                    continue;

                color = highlightedColors[i];
                return color != Color.Transparent;
            }

            return false;
        }

        private void ClearHighlights()
        {
            for (int i = 0; i < highlightedSlots.Length; i++)
            {
                highlightedSlots[i] = -1;
                highlightedColors[i] = Color.Transparent;
            }
        }
    }
}
