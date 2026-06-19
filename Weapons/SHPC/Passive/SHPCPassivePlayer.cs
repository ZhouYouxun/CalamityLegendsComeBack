using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.SHPC.RightClickMortar;
using CalamityLegendsComeBack.Weapons.SHPC.RightClickTurret;
using CalamityLegendsComeBack.Accssory.SHPC.General;

namespace CalamityLegendsComeBack.Weapons.SHPC.Passive
{
    internal sealed class SHPCPassivePlayer : ModPlayer
    {
        private const int PassiveManaBonus = 100;

        private int passiveOrbTimer;
        private int passiveWarmupTimer;
        private int unscaledManaSicknessTime;
        private bool currentManaBonusApplied;

        public override void UpdateDead()
        {
            passiveOrbTimer = 0;
            passiveWarmupTimer = 0;
            unscaledManaSicknessTime = 0;
            currentManaBonusApplied = false;
        }

        public bool HoldingSHPC => Player.HeldItem.type == ModContent.ItemType<NewLegendSHPC>();

        public override void PostUpdateEquips()
        {
            if (HoldingSHPC)
            {
                Player.statManaMax2 += PassiveManaBonus;
                if (!currentManaBonusApplied)
                {
                    Player.statMana = Utils.Clamp(Player.statMana + PassiveManaBonus, 0, Player.statManaMax2);
                    currentManaBonusApplied = true;
                }

                return;
            }

            if (currentManaBonusApplied)
            {
                Player.statMana = Utils.Clamp(Player.statMana - PassiveManaBonus, 0, Player.statManaMax2);
                currentManaBonusApplied = false;
            }
        }

        public override void GetHealMana(Item item, bool quickHeal, ref int healValue)
        {
            if (HoldingSHPC)
                healValue *= 2;
        }

        public override void PreUpdateBuffs()
        {
            if (!HoldingSHPC)
            {
                unscaledManaSicknessTime = 0;
                return;
            }

            int buffIndex = Player.FindBuffIndex(BuffID.ManaSickness);
            if (buffIndex < 0)
            {
                unscaledManaSicknessTime = 0;
                return;
            }

            if (unscaledManaSicknessTime <= 0)
                unscaledManaSicknessTime = Utils.Clamp(Player.buffTime[buffIndex], 0, Player.manaSickTimeMax);

            Player.buffTime[buffIndex] = GetScaledManaSicknessTime(unscaledManaSicknessTime);
            unscaledManaSicknessTime--;
        }

        public override void PostUpdate()
        {
            if (!HoldingSHPC)
            {
                passiveOrbTimer = 0;
                passiveWarmupTimer = 0;
                return;
            }

            SHPCEnergyCorePlayer energyCore = Player.GetModPlayer<SHPCEnergyCorePlayer>();
            if (energyCore.HasInfiniteSHPCMana)
            {
                passiveOrbTimer = 0;
                passiveWarmupTimer = 0;
                Player.statMana = Player.statManaMax2;
                return;
            }

            if (!energyCore.HasEnergyCore)
            {
                passiveOrbTimer = 0;
                passiveWarmupTimer = 0;
                return;
            }

            if (!PassiveCanTrigger())
            {
                passiveOrbTimer = 0;
                passiveWarmupTimer = 0;
                return;
            }

            if (++passiveWarmupTimer < 10)
                return;

            if (Player.whoAmI != Main.myPlayer)
                return;

            if (++passiveOrbTimer >= 5)
            {
                passiveOrbTimer = 0;

                // ⭐关键：二次判断（最终判定）
                if (Player.statMana < Player.statManaMax2)
                {
                    SpawnGravityOrbBurst();
                }
            }
        }

        public void RegisterManaPotionUse()
        {
            unscaledManaSicknessTime = Math.Min(Player.manaSickTimeMax, Math.Max(0, unscaledManaSicknessTime) + Player.manaSickTime);
            ApplyScaledManaSicknessTime();
        }

        private bool PassiveCanTrigger()
        {
            if (Player.dead || Player.pulley || Player.statMana >= Player.statManaMax2)
                return false;

            bool notFiring = IsNotFiring();
            int tier = Player.GetModPlayer<SHPCEnergyCorePlayer>().EnergyCoreTier;
            if (tier >= 1)
                return notFiring;

            return false;
        }

        private bool IsNotFiring()
        {
            bool noHeldProjectile =
                Player.ownedProjectileCounts[ModContent.ProjectileType<RightClick.SHPCRight_HoulOut>()] <= 0 &&
                Player.ownedProjectileCounts[ModContent.ProjectileType<RightClickMortar_HoldOut>()] <= 0 &&
                Player.ownedProjectileCounts[ModContent.ProjectileType<MilitaryCaller_HoldOut>()] <= 0;

            return Player.itemAnimation <= 0 &&
                   Player.itemTime <= 0 &&
                   noHeldProjectile &&
                   !Player.controlUseItem &&
                   !Player.controlUseTile;
        }

        private void ApplyScaledManaSicknessTime()
        {
            int buffIndex = Player.FindBuffIndex(BuffID.ManaSickness);
            if (buffIndex < 0)
                return;

            Player.buffTime[buffIndex] = GetScaledManaSicknessTime(unscaledManaSicknessTime);
        }

        private static int GetScaledManaSicknessTime(int unscaledTime)
        {
            return Utils.Clamp((int)MathF.Ceiling(unscaledTime / 3f), 1, Player.manaSickTimeMax);
        }

        private void SpawnGravityOrbBurst()
        {
            int orbCount = Main.rand.Next(1, 3);
            for (int i = 0; i < orbCount; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                //float distance = Main.rand.NextFloat(360f, 360f);


                float inner = 12f * 16f;
                float outer = 16f * 16f;

                // √随机，保证圆盘均匀分布
                float t = Main.rand.NextFloat();
                float distance = MathF.Sqrt(t * (outer * outer - inner * inner) + inner * inner);

                Vector2 spawnOffset = angle.ToRotationVector2() * distance;
                Vector2 spawnPosition = Player.Center + spawnOffset;

                Vector2 inwardVelocity = (-spawnOffset).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(3.2f, 4.8f);

                Projectile.NewProjectile(
                    Player.GetSource_FromThis(),
                    spawnPosition,
                    inwardVelocity,
                    ModContent.ProjectileType<SHPCPassiveOrb>(),
                    0,
                    0f,
                    Player.whoAmI,
                    Player.whoAmI,
                    Main.rand.NextFloat(0.85f, 1.2f));
            }
        }
    }
}
