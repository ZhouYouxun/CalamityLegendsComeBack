using CalamityMod.Cooldowns;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.CallofDuty
{
    internal sealed class CallofDutyUltimateCooldown : CooldownHandler
    {
        public static new string ID => "CallofDuty_Ultimate";

        private CallofDutyPlayer PhonePlayer => instance.player.GetModPlayer<CallofDutyPlayer>();

        public override bool CanTickDown => false;
        public override bool ShouldDisplay => PhonePlayer.HoldingPhone || !PhonePlayer.UltimateReady || PhonePlayer.ArmyActive;
        public override LocalizedText DisplayName => Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.CallofDuty_Ultimate");
        public override string Texture => "CalamityMod/Cooldowns/WulfrumRoverDriveActive";
        public override string OutlineTexture => "CalamityMod/Cooldowns/WulfrumRoverDriveOutline";
        public override string OverlayTexture => "CalamityMod/Cooldowns/WulfrumRoverDriveOverlay";
        public override Color OutlineColor => new(70, 205, 255);
        public override Color CooldownStartColor => Color.Lerp(new Color(32, 99, 168), new Color(194, 255, 67), PhonePlayer.UltimateCompletion);
        public override Color CooldownEndColor => Color.Lerp(new Color(62, 195, 255), Color.White, PhonePlayer.UltimateCompletion);
    }
}
