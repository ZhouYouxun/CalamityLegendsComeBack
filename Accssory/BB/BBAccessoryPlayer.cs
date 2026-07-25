using CalamityLegendsComeBack.Accssory.BB.Skill.BaronHelix;
using CalamityLegendsComeBack.Weapons.BrinyBaron.TideValue;
using CalamityMod;
using CalamityMod.World;
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

        public float GeneralMeleeDamageBonus;
        public int BottleTideCapBonus;
        public int BubbleShieldHealth;
        public bool HasBubbleShield => BubbleShieldHealth > 0;
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

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (AbyssalBastionEquipped && HasBubbleShield)
                modifiers.FinalDamage *= 0.75f;
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (!AbyssalBastionEquipped || !HasBubbleShield)
                return;

            BubbleShieldHealth = Utils.Clamp(BubbleShieldHealth - info.Damage, 0, 100);
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

                if (HasBubbleShield)
                {
                    Player.statDefense += 25;
                    Player.endurance += 0.25f;
                }
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
            if (TideWiseHatEquipped && hit.Crit && Main.myPlayer == Player.whoAmI && Main.rand.NextBool(100))
                Player.GetModPlayer<BBTideValuePlayer>().AddTide();
        }

        public void GrantBubbleShield()
        {
            if (AbyssalBastionEquipped)
                BubbleShieldHealth = 100;
        }

        public void TrySpawnBaronHelixBubble()
        {
            if (!BaronHelixEquipped || Main.myPlayer != Player.whoAmI)
                return;

            bool healPlayer = Player.statLife < Player.statLifeMax2;
            int damage = DownedBossSystem.downedDoG ? 300 : NPC.downedMoonlord ? 140 : 60;
            Vector2 spawnOffset = Main.rand.NextVector2Circular(110f, 70f);
            Vector2 velocity = spawnOffset.SafeNormalize(Vector2.UnitY) * -2f;
            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Center + spawnOffset,
                velocity,
                ModContent.ProjectileType<BaronHelixBubble>(),
                damage,
                1f,
                Player.whoAmI,
                healPlayer ? 1f : 0f);
        }
    }
}
