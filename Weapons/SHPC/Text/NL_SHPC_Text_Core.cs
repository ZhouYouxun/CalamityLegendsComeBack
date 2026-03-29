using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Text
{
    public static class NL_SHPC_Text_Core
    {
        private static int cooldownTimer = 0;
        private const int CooldownMax = 180;

        public static void Update()
        {
            if (cooldownTimer > 0)
                cooldownTimer--;
        }

        public static void Request(Player player, string keyBase)
        {
            if (cooldownTimer > 0)
                return;

            if (!CanSpeak(player))
                return;

            string text = GetRandomText(keyBase);

            // ===== 直接显示 =====
            Vector2 pos = player.Center - new Vector2(0f, player.height / 2f + 20f);

            CombatText.NewText(
                new Rectangle((int)pos.X, (int)pos.Y, 1, 1),
                Color.Cyan,
                text,
                false,
                false
            );

            cooldownTimer = CooldownMax;
        }

        private static bool CanSpeak(Player player)
        {
            if (player == null || !player.active || player.dead)
                return false;

            if (player.HeldItem.type != ModContent.ItemType<NewLegendSHPC>())
                return false;

            return true;
        }

        private static string GetRandomText(string keyBase)
        {
            int index = Main.rand.Next(1, 5);
            return Terraria.Localization.Language.GetTextValue(
                $"Mods.CalamityLegendsComeBack.SHPC.Text.{keyBase}{index}"
            );
        }
    }
}