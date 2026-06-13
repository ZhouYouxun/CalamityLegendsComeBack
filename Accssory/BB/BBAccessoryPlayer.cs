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
        public const int OffshoreWindTurbineTideBonus = 2;
        public const int ImpactRestarterShortDashCooldown = 0;
        public const int ImpactRestarterSpinDashCooldown = 60;
        public const int HighTideDefenseBonus = 20;
        public const float HighTideDamageReduction = 0.20f;
        public const float SurgeChainReactorDamageFactor = 0.55f;
        public const int TideRadarDamageBonusPercent = 15;

        public bool OffshoreWindTurbineEquipped;
        public bool ImpactRestarterEquipped;
        public bool HighTideOverloadBarrierEquipped;
        public bool SurgeChainReactorEquipped;
        public bool DrinkingFountainEquipped;
        public bool AdrenalineInjectorEquipped;
        public bool TideRadarEquipped;
        public bool BBPassiveChannelerEquipped;
        public float GeneralMeleeDamageBonus;
        public int TideRadarTarget = -1;

        private int rightClickPriority;
        private int adrenalineTimer;
        private int adrenalineStacks;

        public BBRightClickMode RightClickMode { get; private set; }
        public float AdrenalineDamageBonus => adrenalineStacks * 0.018f;
        public float AdrenalineAttackSpeedBonus => adrenalineStacks * 0.014f;

        public int BonusTideMax => OffshoreWindTurbineEquipped ? OffshoreWindTurbineTideBonus : 0;

        public override void ResetEffects()
        {
            OffshoreWindTurbineEquipped = false;
            ImpactRestarterEquipped = false;
            HighTideOverloadBarrierEquipped = false;
            SurgeChainReactorEquipped = false;
            DrinkingFountainEquipped = false;
            AdrenalineInjectorEquipped = false;
            TideRadarEquipped = false;
            BBPassiveChannelerEquipped = false;
            GeneralMeleeDamageBonus = 0f;
            TideRadarTarget = -1;
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

        public override void PostUpdateEquips()
        {
            Player.GetDamage(DamageClass.Melee) += GeneralMeleeDamageBonus + AdrenalineDamageBonus;
            Player.GetAttackSpeed(DamageClass.Melee) += AdrenalineAttackSpeedBonus;

            UpdateTideRadarTarget();

            if (!HighTideOverloadBarrierEquipped)
                return;

            if (!Player.GetModPlayer<BBTideValuePlayer>().TideFull)
                return;

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

        private void UpdateTideRadarTarget()
        {
            if (!TideRadarEquipped)
                return;

            float bestDistance = 1200f;
            int bestTarget = -1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.lifeMax <= 5)
                    continue;

                float distance = Player.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = i;
            }

            TideRadarTarget = bestTarget;
        }
    }
}
