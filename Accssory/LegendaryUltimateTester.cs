using CalamityLegendsComeBack.Weapons.SHPC.EXSkill;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory
{
    internal sealed class LegendaryUltimateTester : ModItem
    {
        public new string LocalizationCategory => "Items.Accessories";
        //public override string Texture => "CalamityLegendsComeBack/Accssory/LegendaryEmblem";

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
        // V1.0 only ships the SHPC ultimate system. Keep this test accessory self-contained
        // instead of retaining DEV-only dependencies for weapons removed from this branch.
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

            ChargeSHPC();
        }

        private void ChargeSHPC()
        {
            NewLegend_EXPlayer exPlayer = Player.GetModPlayer<NewLegend_EXPlayer>();
            int max = NewLegend_EXPlayer.GetCurrentEXMax(Player);
            exPlayer.EXValue = System.Math.Min(max, exPlayer.EXValue + FramesPerTick(max));
        }

        private static int FramesPerTick(int maxValue)
        {
            return System.Math.Max(1, (int)System.Math.Ceiling(maxValue / (float)FullChargeFrames));
        }
    }
}
