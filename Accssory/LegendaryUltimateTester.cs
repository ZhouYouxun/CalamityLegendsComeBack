using System;
using CalamityLegendsComeBack.Weapons.SHPC.EXSkill;
using CalamityMod;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory
{
    internal sealed class LegendaryUltimateTester : ModItem
    {
        public new string LocalizationCategory => "Items.Accessories";

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = 0;
            Item.rare = ItemRarityID.Red;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<LegendaryEmblemPlayer>().EXAccessoryEquipped = true;
            player.GetModPlayer<LegendaryUltimateTesterPlayer>().Equipped = true;
        }
    }

    internal sealed class LegendaryUltimateTesterPlayer : ModPlayer
    {
        private const int FullChargeFrames = 60;

        public bool Equipped;

        public override void ResetEffects()
        {
            Equipped = false;
        }

        public override void PostUpdate()
        {
            if (!Equipped || !Player.active || Player.dead)
                return;

            NewLegend_EXPlayer exPlayer = Player.GetModPlayer<NewLegend_EXPlayer>();
            int max = NewLegend_EXPlayer.GetCurrentEXMax(Player);
            exPlayer.EXValue = Math.Min(max, exPlayer.EXValue + FramesPerTick(max));
            SetCooldownProgress(SHPC_EXCooldown.ID, max, exPlayer.EXValue);
        }

        private static int FramesPerTick(int maxValue)
        {
            return Math.Max(1, (int)Math.Ceiling(maxValue / (float)FullChargeFrames));
        }

        private void SetCooldownProgress(string id, int duration, int timeLeft)
        {
            if (Player.Calamity().cooldowns.TryGetValue(id, out var cooldown))
            {
                cooldown.duration = duration;
                cooldown.timeLeft = timeLeft;
                return;
            }

            Player.AddCooldown(id, duration).timeLeft = timeLeft;
        }
    }
}
