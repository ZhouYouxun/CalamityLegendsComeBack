using CalamityLegendsComeBack.Accssory.MC.PeacockScroll;
using CalamityLegendsComeBack.Weapons.Malachite;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.MC.General
{
    public sealed class MCGeneralPlayer : ModPlayer
    {
        public float ProjectileSpeedMult = 1f;
        public float BonusStealthMax = 0f;
        public int KunaiArmorPen = 0;
        public bool NormalKunaiIgnoresGravity = false;
        public float StealthRestoreOnStealthStrike = 0f;
        private bool willowBladeEquipped = false;

        public void SetWillowBladeEquipped() => willowBladeEquipped = true;

        public bool PeacockDisplayActive =>
            willowBladeEquipped &&
            MalachiteKunai.HasFullStoredPeacockFan(Player) &&
            MalachiteRightFeather.CountStoredRightFeathers(Player) >= MalachiteBalance.RightFeatherMaxCount;

        public override void ResetEffects()
        {
            ProjectileSpeedMult = 1f;
            BonusStealthMax = 0f;
            KunaiArmorPen = 0;
            NormalKunaiIgnoresGravity = false;
            StealthRestoreOnStealthStrike = 0f;
            willowBladeEquipped = false;
        }

        public override void PostUpdateEquips()
        {
            if (BonusStealthMax > 0f)
            {
                CalamityPlayer calamity = Player.Calamity();
                if (calamity.rogueStealthMax < 1f)
                    calamity.rogueStealthMax = 1f;
                calamity.rogueStealthMax += BonusStealthMax;
            }

            if (!PeacockDisplayActive)
                return;

            Player.moveSpeed += 0.20f;
            Player.maxRunSpeed += 2.4f;
            Player.jumpSpeed += 2.0f;
            if (Player.wingTimeMax > 0)
                Player.wingTimeMax = (int)(Player.wingTimeMax * 3.30f);
        }
    }
}
