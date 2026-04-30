using CalamityLegendsComeBack.Weapons.BrinyBaron.POWER;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB
{
    public class BBAccessoryPlayer : ModPlayer
    {
        public const int OffshoreWindTurbineTideBonus = 2;
        public const int ImpactRestarterShortDashCooldown = 0;
        public const int ImpactRestarterSpinDashCooldown = 60;
        public const int HighTideDefenseBonus = 20;
        public const float HighTideDamageReduction = 0.20f;
        public const float SurgeChainReactorDamageFactor = 0.55f;

        public bool OffshoreWindTurbineEquipped;
        public bool ImpactRestarterEquipped;
        public bool HighTideOverloadBarrierEquipped;
        public bool SurgeChainReactorEquipped;

        public int BonusTideMax => OffshoreWindTurbineEquipped ? OffshoreWindTurbineTideBonus : 0;

        public override void ResetEffects()
        {
            OffshoreWindTurbineEquipped = false;
            ImpactRestarterEquipped = false;
            HighTideOverloadBarrierEquipped = false;
            SurgeChainReactorEquipped = false;
        }

        public override void PostUpdateEquips()
        {
            if (!HighTideOverloadBarrierEquipped)
                return;

            if (!Player.GetModPlayer<BBEXPlayer>().TideFull)
                return;

            Player.statDefense += HighTideDefenseBonus;
            Player.endurance += HighTideDamageReduction;

            if (Main.dedServ || Main.rand.NextBool(4))
                return;

            Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.6f, 1.8f);
            Dust water = Dust.NewDustPerfect(
                Player.Center + Main.rand.NextVector2Circular(Player.width * 0.65f, Player.height * 0.65f),
                Terraria.ID.DustID.Water,
                velocity,
                120,
                new Color(75, 175, 255),
                Main.rand.NextFloat(0.7f, 1.05f));
            water.noGravity = true;
        }
    }
}
