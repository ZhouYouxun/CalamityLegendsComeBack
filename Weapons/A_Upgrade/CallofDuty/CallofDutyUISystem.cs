using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.CallofDuty
{
    internal sealed class CallofDutyUISystem : ModSystem
    {
        internal const float CancelRadius = 22f;

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
            if (mouseTextIndex < 0)
                return;

            layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                "CalamityLegendsComeBack: Responsibility Phone HUD",
                DrawLCDHUD,
                InterfaceScaleType.None));
        }

        private bool DrawLCDHUD()
        {
            if (Main.gameMenu || Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers)
                return true;

            Player localPlayer = Main.LocalPlayer;
            if (localPlayer.HeldItem?.type != ModContent.ItemType<CallofDuty>())
                return true;

            CallofDutyPlayer phonePlayer = localPlayer.GetModPlayer<CallofDutyPlayer>();
            SpriteBatch sb = Main.spriteBatch;

            // 1. Draw Speed Dial Charge Progress Meter (when holding L+R)
            if (phonePlayer.BothHoldTimer > 0)
            {
                Vector2 playerScreenPos = localPlayer.Center - Main.screenPosition - Vector2.UnitY * 40f;
                float progress = MathHelper.Clamp(phonePlayer.BothHoldTimer / 30f, 0f, 1f);

                string text = $"[SPEED DIAL: {(int)(progress * 100)}%]";
                Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text);
                Utils.DrawBorderString(sb, text, playerScreenPos - textSize * 0.5f, new Color(132, 226, 255), 0.85f);
            }

            // 2. Draw LCD lock-on status over Redial Target
            if (phonePlayer.RedialTarget >= 0 && Main.npc.IndexInRange(phonePlayer.RedialTarget))
            {
                NPC target = Main.npc[phonePlayer.RedialTarget];
                if (target.active)
                {
                    Vector2 targetScreenPos = target.Top - Main.screenPosition - Vector2.UnitY * 16f;
                    string signalBars = phonePlayer.FastDialPriorityTarget == target.whoAmI ? "[||||] <VIP LINKED>" : "[|||] <REDIAL READY>";
                    Color statusColor = phonePlayer.FastDialPriorityTarget == target.whoAmI ? new Color(194, 255, 67) : new Color(132, 226, 255);

                    Vector2 size = FontAssets.MouseText.Value.MeasureString(signalBars);
                    Utils.DrawBorderString(sb, signalBars, targetScreenPos - size * 0.5f, statusColor * 0.9f, 0.8f);
                }
            }

            return true;
        }
    }
}
