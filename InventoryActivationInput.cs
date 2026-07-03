using System;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack
{
    internal static class InventoryActivationInput
    {
        public static bool HasBoundKey(ModKeybind keybind)
        {
            return keybind?.GetAssignedKeys()?.Any(IsRealAssignedKey) == true;
        }

        public static bool IsPressed(ModKeybind keybind)
        {
            var keys = keybind?.GetAssignedKeys();
            if (keys == null)
                return false;

            foreach (string key in keys)
            {
                if (!IsRealAssignedKey(key))
                    continue;

                if (IsSingleKeyPressed(key))
                    return true;
            }

            return false;
        }

        public static string GetDisplayKeyOrDefault(ModKeybind keybind, string fallback)
        {
            return keybind?.GetAssignedKeys()?.FirstOrDefault(IsRealAssignedKey) ?? fallback;
        }

        private static bool IsRealAssignedKey(string key)
        {
            return !string.IsNullOrWhiteSpace(key) &&
                !key.Equals("None", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSingleKeyPressed(string key)
        {
            if (key.Equals("Mouse1", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("MouseLeft", StringComparison.OrdinalIgnoreCase))
                return Main.mouseLeft;

            if (key.Equals("Mouse2", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("MouseRight", StringComparison.OrdinalIgnoreCase))
                return Main.mouseRight;

            if (key.Equals("Mouse3", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("MouseMiddle", StringComparison.OrdinalIgnoreCase))
                return Main.mouseMiddle;

            return Enum.TryParse(key, true, out Keys xnaKey) &&
                xnaKey != Keys.None &&
                Main.keyState.IsKeyDown(xnaKey);
        }
    }
}
