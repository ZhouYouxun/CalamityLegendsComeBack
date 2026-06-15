using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using CalamityLegendsComeBack.Weapons.BrinyBaron.TideValue;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB
{
    public enum BBRightClickMode
    {
        DefaultShuriken,
        VortexPortal,
        LostGarment,
        CeruleanShield
    }

    public class BBAccessoryPlayer : ModPlayer
    {
        public const int OffshoreWindTurbineTideBonus = 4;
        public const int ImpactRestarterShortDashCooldown = 0;
        public const int ImpactRestarterSpinDashCooldown = 60;
        public const int HighTideDefenseBonus = 20;
        public const float HighTideDamageReduction = 0.20f;
        public const float SurgeChainReactorDamageFactor = 0.55f;

        public bool OffshoreWindTurbineEquipped;
        public bool ImpactRestarterEquipped;
        public bool HighTideOverloadBarrierEquipped;
        public bool SurgeChainReactorEquipped;
        public bool DrinkingFountainEquipped;
        public bool AdrenalineInjectorEquipped;
        public bool BBPassiveChannelerEquipped;
        public float GeneralMeleeDamageBonus;

        public int BottleTideCapBonus;
        public float BottleFullTideDamageBonus;
        public bool ShurikenBoatEnhanced;
        public bool WaveInfinitePenetration;
        public bool BottledBlackPearlEquipped;

        private int rightClickPriority;
        private int adrenalineTimer;
        private int adrenalineStacks;

        public BBRightClickMode RightClickMode { get; private set; }
        public float AdrenalineDamageBonus => adrenalineStacks * 0.018f;
        public float AdrenalineAttackSpeedBonus => adrenalineStacks * 0.014f;

        public int BonusTideMax => (OffshoreWindTurbineEquipped ? OffshoreWindTurbineTideBonus : 0) + BottleTideCapBonus;

        public override void ResetEffects()
        {
            OffshoreWindTurbineEquipped = false;
            ImpactRestarterEquipped = false;
            HighTideOverloadBarrierEquipped = false;
            SurgeChainReactorEquipped = false;
            DrinkingFountainEquipped = false;
            AdrenalineInjectorEquipped = false;
            BBPassiveChannelerEquipped = false;
            GeneralMeleeDamageBonus = 0f;
            BottleTideCapBonus = 0;
            BottleFullTideDamageBonus = 0f;
            ShurikenBoatEnhanced = false;
            WaveInfinitePenetration = false;
            BottledBlackPearlEquipped = false;
            RightClickMode = BBRightClickMode.DefaultShuriken;
            rightClickPriority = -1;
        }

        public override void PostUpdate()
        {
            if (adrenalineTimer > 0)
                adrenalineTimer--;
            else if (adrenalineStacks > 0)
                adrenalineStacks--;
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (!AdrenalineInjectorEquipped || adrenalineStacks <= 0)
                return;

            modifiers.FinalDamage *= 1f + adrenalineStacks * 0.05f;
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (AdrenalineInjectorEquipped && adrenalineStacks > 0)
            {
                adrenalineStacks = 0;
                adrenalineTimer = 0;
            }
        }

        public override void PostUpdateEquips()
        {
            Player.GetDamage(DamageClass.Melee) += GeneralMeleeDamageBonus + AdrenalineDamageBonus;
            Player.GetAttackSpeed(DamageClass.Melee) += AdrenalineAttackSpeedBonus;

            if (HighTideOverloadBarrierEquipped && Player.GetModPlayer<BBTideValuePlayer>().TideFull)
            {
                Player.statDefense += HighTideDefenseBonus;
                Player.endurance += HighTideDamageReduction;

                if (Main.dedServ || Main.rand.NextBool(4))
                    return;

                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.6f, 1.8f);
                Dust water = Dust.NewDustPerfect(
                    Player.Center + Main.rand.NextVector2Circular(Player.width * 0.65f, Player.height * 0.65f),
                    DustID.Water,
                    velocity,
                    120,
                    new Color(75, 175, 255),
                    Main.rand.NextFloat(0.7f, 1.05f));
                water.noGravity = true;
                return;
            }

            if (BottledBlackPearlEquipped && Player.GetModPlayer<BBTideValuePlayer>().TideFull)
            {
                Player.statDefense += 10;
                Player.endurance += 0.10f;
            }
        }

        public void SetRightClickMode(BBRightClickMode mode, int priority)
        {
            if (priority < rightClickPriority)
                return;

            rightClickPriority = priority;
            RightClickMode = mode;
        }

        public void RegisterBrinyBaronBladeHit(NPC target)
        {
            if (AdrenalineInjectorEquipped)
            {
                adrenalineStacks = Utils.Clamp(adrenalineStacks + 1, 0, 18);
                adrenalineTimer = 210;
            }

            if (DrinkingFountainEquipped && Main.myPlayer == Player.whoAmI)
            {
                Vector2 velocity = (Player.Center - target.Center).SafeNormalize(Vector2.UnitY) * 7.5f;
                Projectile.NewProjectile(
                    Player.GetSource_FromThis(),
                    target.Center,
                    velocity,
                    ModContent.ProjectileType<BBDrinkingFountainOrb>(),
                    36,
                    0f,
                    Player.whoAmI);
            }
        }
    }
}
