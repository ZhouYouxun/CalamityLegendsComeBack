using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.TS
{
    internal sealed class AzureThunderAccessoryPlayer : ModPlayer
    {
        public bool GuZhouEquipped;
        public bool YiGanYiYingEquipped;
        public bool QianDingWanDingEquipped;
        public bool FengYunZhiBianEquipped;

        private int guZhouDamageTimer;
        private int guZhouConsumedCharge;
        private int yiGanDamageTimer;
        private float yiGanDamageBonus;

        public bool AnyTSEquipped => GuZhouEquipped || YiGanYiYingEquipped || QianDingWanDingEquipped || FengYunZhiBianEquipped;

        public override void ResetEffects()
        {
            GuZhouEquipped = false;
            YiGanYiYingEquipped = false;
            QianDingWanDingEquipped = false;
            FengYunZhiBianEquipped = false;
        }

        public override void PostUpdateEquips()
        {
            bool holdingAzureThunder = Player.HeldItem?.type == ModContent.ItemType<AzureThunder>();

            if (GuZhouEquipped)
            {
                Player.GetDamage(DamageClass.Magic) += 0.1f;
                if (holdingAzureThunder)
                    Player.GetDamage(DamageClass.Magic) += 0.05f;
            }

            if (YiGanYiYingEquipped)
            {
                Player.statManaMax2 += 50;
                Player.manaCost -= 0.15f;
            }

            if (QianDingWanDingEquipped)
            {
                Player.statManaMax2 += 100;
                Player.manaCost -= 0.15f;
            }

            if (FengYunZhiBianEquipped)
            {
                if (holdingAzureThunder)
                    Player.GetDamage(DamageClass.Magic) += 0.15f;

                if (Player.HasBuff(ModContent.BuffType<AzureThunderHarmonyBuff>()))
                {
                    Player.GetDamage(DamageClass.Magic) += 0.09f;
                    Player.GetArmorPenetration(DamageClass.Magic) += 36f;
                }
            }

            UpdateTimedDamageBuffs();
        }

        public void OnConsumeThunderCharge(int consumedCharge, bool harmonyActive, int activeSwordCount, NPC lockedTarget)
        {
            if (consumedCharge <= 0)
                return;

            if (GuZhouEquipped)
            {
                guZhouConsumedCharge = consumedCharge;
                guZhouDamageTimer = 20 * 60;
            }

            if (YiGanYiYingEquipped)
            {
                yiGanDamageBonus = activeSwordCount * (harmonyActive ? 0.03f : 0.02f);
                yiGanDamageTimer = (harmonyActive ? 15 : 5) * 60;
            }

            if (GuZhouEquipped && harmonyActive && lockedTarget != null && CanApplyGuZhouSlow(lockedTarget))
                lockedTarget.AddBuff(ModContent.BuffType<AzureThunderGuZhouSlowDebuff>(), 5 * 60);
        }

        public static void ApplyAzureThunderAccessoryOnHit(Projectile projectile, NPC target)
        {
            if (!Main.player.IndexInRange(projectile.owner))
                return;

            Player owner = Main.player[projectile.owner];
            if (owner.GetModPlayer<AzureThunderAccessoryPlayer>().FengYunZhiBianEquipped)
                target.AddBuff(ModContent.BuffType<StaticDischarge>(), 90);
        }

        public static float GetGroundSwordEffectRadius(Player player)
        {
            return player.GetModPlayer<AzureThunderAccessoryPlayer>().QianDingWanDingEquipped ? 75f * 16f : 50f * 16f;
        }

        public static int GetAutoGroundSwordInterval(Player player)
        {
            return player.GetModPlayer<AzureThunderAccessoryPlayer>().QianDingWanDingEquipped ? 6 * 60 : AzureThunderPlayer.AutoGroundSwordInterval;
        }

        public static int GetRightClickLightningEnergyGain(Player player)
        {
            return player.GetModPlayer<AzureThunderAccessoryPlayer>().QianDingWanDingEquipped ? 7 : 6;
        }

        public static int GetHarmonyDuration(Player player)
        {
            return player.GetModPlayer<AzureThunderAccessoryPlayer>().FengYunZhiBianEquipped ? 30 * 60 : AzureThunderPlayer.HarmonyDuration;
        }

        public static bool ShouldGroundSwordFollowPlayer(Projectile projectile, out int followSlot)
        {
            followSlot = 0;
            if (!Main.player.IndexInRange(projectile.owner))
                return false;

            Player owner = Main.player[projectile.owner];
            if (!owner.GetModPlayer<AzureThunderAccessoryPlayer>().QianDingWanDingEquipped)
                return false;

            int groundSwordType = ModContent.ProjectileType<AzureThunderGroundSword>();
            int slot = 0;
            foreach (Projectile other in Main.ActiveProjectiles)
            {
                if (!other.active || other.owner != projectile.owner || other.type != groundSwordType)
                    continue;

                if (other.whoAmI == projectile.whoAmI)
                {
                    followSlot = slot;
                    return slot < 3;
                }

                slot++;
            }

            return false;
        }

        private static bool CanApplyGuZhouSlow(NPC target)
        {
            return target.active && target.realLife < 0 && target.aiStyle != NPCAIStyleID.Worm;
        }

        private void UpdateTimedDamageBuffs()
        {
            if (guZhouDamageTimer > 0)
            {
                Player.GetDamage(DamageClass.Magic) += guZhouConsumedCharge * 0.05f;
                Player.AddBuff(ModContent.BuffType<AzureThunderGuZhouDamageBuff>(), guZhouDamageTimer);
                guZhouDamageTimer--;
            }
            else
                guZhouConsumedCharge = 0;

            if (yiGanDamageTimer > 0)
            {
                Player.GetDamage(DamageClass.Magic) += yiGanDamageBonus;
                Player.AddBuff(ModContent.BuffType<AzureThunderYiGanDamageBuff>(), yiGanDamageTimer);
                yiGanDamageTimer--;
            }
            else
                yiGanDamageBonus = 0f;
        }
    }

    internal sealed class AzureThunderSlowGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public override void PostAI(NPC npc)
        {
            if (npc.HasBuff(ModContent.BuffType<AzureThunderGuZhouSlowDebuff>()) && npc.realLife < 0 && npc.aiStyle != NPCAIStyleID.Worm)
                npc.velocity *= 0.85f;
        }
    }
}
