using CalamityLegendsComeBack.Accssory.BB.Skill;
using CalamityLegendsComeBack.Weapons.BrinyBaron;
using CalamityLegendsComeBack.Weapons.BrinyBaron.TideValue;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB
{
    public enum BBRightClickMode
    {
        DefaultShuriken,
        VortexEye
    }

    public class BBAccessoryPlayer : ModPlayer
    {
        public bool BottledBoatEquipped;
        public bool BottledBlackPearlEquipped;
        public bool BottledAircraftCarrierEquipped;
        public bool OceanHormoneEquipped;
        public bool TideWiseHatEquipped;
        public bool AbyssalBastionEquipped;
        public bool BaronHelixEquipped;
        public bool VortexEyeEquipped;
        public bool TideRadarEquipped;
        public bool DrinkingFountainEquipped;

        public float GeneralMeleeDamageBonus;
        public int BottleTideCapBonus;
        public BBRightClickMode RightClickMode { get; private set; }
        public int BonusTideMax => BottleTideCapBonus;

        private int autoTideTimer;

        public override void ResetEffects()
        {
            BottledBoatEquipped = false;
            BottledBlackPearlEquipped = false;
            BottledAircraftCarrierEquipped = false;
            OceanHormoneEquipped = false;
            TideWiseHatEquipped = false;
            AbyssalBastionEquipped = false;
            BaronHelixEquipped = false;
            VortexEyeEquipped = false;
            TideRadarEquipped = false;
            DrinkingFountainEquipped = false;
            GeneralMeleeDamageBonus = 0f;
            BottleTideCapBonus = 0;
            RightClickMode = BBRightClickMode.DefaultShuriken;
        }

        public override void PostUpdate()
        {
            if (BottledAircraftCarrierEquipped)
            {
                autoTideTimer++;
                if (autoTideTimer >= 360)
                {
                    autoTideTimer = 0;
                    Player.GetModPlayer<BBTideValuePlayer>().AddTide(2);
                }
            }
            else if (BottledBlackPearlEquipped)
            {
                autoTideTimer++;
                if (autoTideTimer >= 720)
                {
                    autoTideTimer = 0;
                    Player.GetModPlayer<BBTideValuePlayer>().AddTide();
                }
            }
            else
            {
                autoTideTimer = 0;
            }
        }

        public override void PostUpdateEquips()
        {
            Player.GetDamage(DamageClass.Melee) += GeneralMeleeDamageBonus;
            BBTideValuePlayer tidePlayer = Player.GetModPlayer<BBTideValuePlayer>();
            int currentTide = tidePlayer.TideValue;

            if (BottledBoatEquipped || BottledBlackPearlEquipped || BottledAircraftCarrierEquipped)
            {
                Player.GetDamage(DamageClass.Melee) += currentTide * 0.01f;
                Player.GetAttackSpeed(DamageClass.Melee) += currentTide * 0.01f;
            }

            if (tidePlayer.TideFull)
            {
                if (BottledBlackPearlEquipped || BottledAircraftCarrierEquipped)
                {
                    Player.statDefense += 10;
                    Player.endurance += 0.10f;
                }

                if (BottledAircraftCarrierEquipped)
                    Player.GetCritChance(DamageClass.Melee) += 30;
            }

            if (OceanHormoneEquipped)
            {
                Player.GetCritChance(DamageClass.Generic) -= 10;
                Player.AddBuff(BuffID.Rabies, 2);
                Player.GetAttackSpeed(DamageClass.Melee) += 0.20f;
                Player.Calamity().laudanum = true;
            }

            if (TideWiseHatEquipped)
            {
                Player.statManaMax2 += 50;
                Player.GetDamage(DamageClass.Melee) += 0.15f;
                Player.GetCritChance(DamageClass.Melee) += currentTide;
            }

            if (AbyssalBastionEquipped)
            {
                Player.statDefense += 6;
                Player.endurance += 0.06f;
                Player.noKnockback = true;

            }

            if (BaronHelixEquipped)
            {
                Player.GetDamage(DamageClass.Melee) += 0.10f;
                Player.GetAttackSpeed(DamageClass.Melee) += 0.10f;
            }
        }

        public void SetRightClickMode(BBRightClickMode mode) => RightClickMode = mode;

        public void RegisterBrinyBaronBladeHit(NPC target, NPC.HitInfo hit)
        {
            if (DrinkingFountainEquipped && Main.netMode != NetmodeID.MultiplayerClient)
            {
                const int maxWorldOrbs = 20;
                int orbType = ModContent.ProjectileType<BBDrinkingFountainOrb>();
                int availableSlots = maxWorldOrbs - CountActiveProjectiles(orbType);
                int orbCount = System.Math.Min(Main.rand.Next(3, 6), System.Math.Max(0, availableSlots));

                for (int i = 0; i < orbCount; i++)
                {
                    Vector2 velocity = (Player.Center - target.Center).SafeNormalize(Vector2.UnitY) *
                                       Main.rand.NextFloat(6.5f, 9f) + Main.rand.NextVector2Circular(1.2f, 1.2f);
                    Projectile.NewProjectile(
                        Player.GetSource_FromThis(),
                        target.Center + Main.rand.NextVector2Circular(10f, 10f),
                        velocity,
                        orbType,
                        0,
                        0f,
                        Player.whoAmI);
                }
            }
        }

        public void GrantBubbleShield()
        {
            if (!AbyssalBastionEquipped || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int shieldType = ModContent.ProjectileType<BrinyBaron_BubbleShield>();
            BrinyBaronBubbleShieldPlayer shieldPlayer = Player.GetModPlayer<BrinyBaronBubbleShieldPlayer>();
            if (!shieldPlayer.CanSpawnBubble || Player.ownedProjectileCounts[shieldType] > 0)
                return;

            Projectile.NewProjectile(
                Player.GetSource_Misc("AbyssalBastionDashHit"),
                Player.Center,
                Vector2.Zero,
                shieldType,
                0,
                0f,
                Player.whoAmI);
            shieldPlayer.StartCooldown();
        }

        private static int CountActiveProjectiles(int projectileType)
        {
            int count = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.type == projectileType)
                    count++;
            }

            return count;
        }

    }
}
