using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.PF
{
    internal sealed class PFAccessoryPlayer : ModPlayer
    {
        // General (lily) stat bonuses — reset every frame, set by accessory UpdateAccessory.
        public int BonusMarkSlots;       // Extra queue slots over the default 3 (0-4)
        public int PurificationCap;      // Max purification level (default 2, max 6)
        public float RangedDamageBonus;  // Ranged damage bonus from lily tiers

        // Skill accessory flags — reset every frame, set by accessory UpdateAccessory.
        public bool ChangEFireCanEquipped;   // 嫦娥怨火罐
        public bool MooncakeEquipped;        // 月饼
        public bool NineTailsEquipped;       // 无秽九尾
        public bool LingmuFOMOEquipped;      // 灵梦FOMO
        public bool HellFairyTorchEquipped;  // 地狱妖精的火把

        public override void ResetEffects()
        {
            BonusMarkSlots = 0;
            PurificationCap = 2;
            RangedDamageBonus = 0f;
            ChangEFireCanEquipped = false;
            MooncakeEquipped = false;
            NineTailsEquipped = false;
            LingmuFOMOEquipped = false;
            HellFairyTorchEquipped = false;
        }

        public override void PostUpdateEquips()
        {
            if (RangedDamageBonus > 0f)
                Player.GetDamage(DamageClass.Ranged) += RangedDamageBonus;
        }
    }
}
