using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.SHPC.RightClickMortar;
using CalamityLegendsComeBack.Accssory.SHPC.General;

namespace CalamityLegendsComeBack.Weapons.SHPC.Passive
{
    internal sealed class SHPCPassivePlayer : ModPlayer
    {
        private bool holdingSHPC;
        private float manaRegenAccumulator;
        private int passiveVisualTimer;
        private int fastManaDelayTimer;
        private int unscaledManaSicknessTime;

        public override void ResetEffects()
        {
            holdingSHPC = false;
        }

        public override void UpdateDead()
        {
            holdingSHPC = false;
            manaRegenAccumulator = 0f;
            passiveVisualTimer = 0;
            fastManaDelayTimer = 0;
            unscaledManaSicknessTime = 0;
        }

        public void SetHoldingSHPC()
        {
            holdingSHPC = true;
        }

        public bool HoldingSHPC => holdingSHPC && Player.HeldItem.type == ModContent.ItemType<NewLegendSHPC>();

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
                manaRegenAccumulator = 0f;
                passiveVisualTimer = 0;
                fastManaDelayTimer = 0;
                return;
            }

            Player.statManaMax2 += 100;

            SHPCEnergyCorePlayer energyCore = Player.GetModPlayer<SHPCEnergyCorePlayer>();
            if (energyCore.HasInfiniteSHPCMana)
            {
                Player.statMana = Player.statManaMax2;
                return;
            }

            TryAutoUseManaPotion(energyCore);

            if (!energyCore.HasEnergyCore)
            {
                manaRegenAccumulator = 0f;
                passiveVisualTimer = 0;
                fastManaDelayTimer = 0;
                return;
            }

            if (!PassiveCanTrigger())
            {
                passiveVisualTimer = 0;
                fastManaDelayTimer = 0;
                return;
            }

            if (++fastManaDelayTimer < 10)
                return;

            RestoreManaPerFrame();

            if (Player.whoAmI != Main.myPlayer)
                return;

            if (++passiveVisualTimer >= 5)
            {
                passiveVisualTimer = 0;

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
            if (tier >= 3)
                return notFiring;

            bool notMovingHorizontally =
                !Player.controlLeft &&
                !Player.controlRight &&
                Math.Abs(Player.velocity.X) <= 0.08f;

            if (tier >= 2)
                return notFiring && notMovingHorizontally;

            bool stationary = Player.velocity.LengthSquared() <= 0.01f && Player.grapCount <= 0;
            return stationary && notFiring;
        }

        private bool IsNotFiring()
        {
            bool noHeldProjectile =
                Player.ownedProjectileCounts[ModContent.ProjectileType<RightClick.SHPCRight_HoulOut>()] <= 0 &&
                Player.ownedProjectileCounts[ModContent.ProjectileType<RightClickMortar_HoldOut>()] <= 0;

            return Player.itemAnimation <= 0 &&
                   Player.itemTime <= 0 &&
                   noHeldProjectile &&
                   !Player.controlUseItem &&
                   !Player.controlUseTile;
        }

        private void RestoreManaPerFrame()
        {
            if (Player.statMana >= Player.statManaMax2)
                return;

            manaRegenAccumulator += Player.statManaMax2 * 0.02f;
            int manaToRestore = (int)manaRegenAccumulator;
            if (manaToRestore <= 0)
                return;

            manaRegenAccumulator -= manaToRestore;
            int previousMana = Player.statMana;
            Player.statMana = Utils.Clamp(Player.statMana + manaToRestore, 0, Player.statManaMax2);

            int restored = Player.statMana - previousMana;
            if (restored > 0 && Main.GameUpdateCount % 15 == 0)
                Player.ManaEffect(restored);
        }

        private void TryAutoUseManaPotion(SHPCEnergyCorePlayer energyCore)
        {
            if (!energyCore.AutoManaPotion || Player.statMana > Player.statManaMax2 - 60)
                return;

            Player.QuickMana();
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

                Vector2 inwardVelocity = (-spawnOffset).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.8f, 3.6f);
                inwardVelocity += spawnOffset.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(-0.7f, 0.7f);

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
