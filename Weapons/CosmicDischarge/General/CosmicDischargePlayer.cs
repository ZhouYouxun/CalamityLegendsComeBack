using CalamityLegendsComeBack.Weapons;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    internal sealed class CosmicDischargePlayer : ModPlayer
    {
        public const int UltimateEnergyMax = 120;
        public const int RightThrustEnergyGain = 10;
        public const int UltimateFieldDuration = 10 * 60;
        public const int PassiveCooldownFrames = 300 * 60;
        public const int IceboundDuration = 3 * 60;
        public const int ColdWindDuration = 3 * 60;

        public int UltimateEnergy;
        public int PassiveCooldownTimer;
        public int QuickDrawCooldownTimer;
        public CosmicDischargeAttackMode AttackMode;

        private bool holdingCosmicDischarge;
        private bool wasHoldingCosmicDischarge;
        private bool wasUltimateReady;
        private bool hadIcebound;
        private int comboIndex;
        private int comboResetTimer;
        private CosmicDischargeAttackMode comboMode;

        public bool HoldingCosmicDischarge =>
            holdingCosmicDischarge &&
            Player.HeldItem != null &&
            !Player.HeldItem.IsAir &&
            Player.HeldItem.type == ModContent.ItemType<NewLegendCosmicDischarge>();

        public bool UltimateFieldActive => Player.ownedProjectileCounts[ModContent.ProjectileType<CosmicDischargeUltimateField>()] > 0;

        public bool ShouldShowPassiveCooldown =>
            HoldingCosmicDischarge ||
            PassiveCooldownTimer > 0 ||
            Player.HasBuff(ModContent.BuffType<CosmicDischargeIceboundBuff>()) ||
            Player.HasBuff(ModContent.BuffType<CosmicDischargeColdWindBuff>());

        public override void ResetEffects()
        {
            holdingCosmicDischarge = false;
        }

        public override void UpdateDead()
        {
            holdingCosmicDischarge = false;
            UltimateEnergy = 0;
            comboIndex = 0;
            comboResetTimer = 0;
            hadIcebound = false;
            QuickDrawCooldownTimer = 0;
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasUltimateReady, false);
        }

        public override void PostUpdate()
        {
            if (PassiveCooldownTimer > 0)
                PassiveCooldownTimer--;

            if (QuickDrawCooldownTimer > 0)
                QuickDrawCooldownTimer--;

            if (comboResetTimer > 0)
                comboResetTimer--;
            else
                comboIndex = 0;

            bool icebound = Player.HasBuff(ModContent.BuffType<CosmicDischargeIceboundBuff>());
            if (hadIcebound && !icebound && Player.active && !Player.dead)
                Player.AddBuff(ModContent.BuffType<CosmicDischargeColdWindBuff>(), ColdWindDuration);
            hadIcebound = icebound;

            if (HoldingCosmicDischarge)
            {
                ApplyZeroDegreeArmament();
                HandleUltimateInput();
                SyncCooldownDisplays();
            }
            else if (UltimateEnergy > 0 || PassiveCooldownTimer > 0 || QuickDrawCooldownTimer > 0)
            {
                SyncCooldownDisplays();
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (UltimateFieldActive || Player.HasBuff(ModContent.BuffType<CosmicDischargeUltimateGuardBuff>()))
                modifiers.FinalDamage *= 0.65f;
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (Player.HasBuff(ModContent.BuffType<CosmicDischargeIceboundBuff>()))
            {
                Player.statLife = System.Math.Max(Player.statLife, 1);
                Player.SetImmuneTimeForAllTypes(30);
                return false;
            }

            if (!CanTriggerIcebound())
                return true;

            TriggerIcebound();
            playSound = false;
            genGore = false;
            return false;
        }

        public void SetHoldingCosmicDischarge()
        {
            holdingCosmicDischarge = true;
        }

        public void ToggleAttackMode()
        {
            AttackMode = AttackMode == CosmicDischargeAttackMode.Whip
                ? CosmicDischargeAttackMode.Sword
                : CosmicDischargeAttackMode.Whip;

            comboIndex = 0;
            comboResetTimer = 0;

            SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.85f, Pitch = AttackMode == CosmicDischargeAttackMode.Whip ? 0.35f : -0.15f }, Player.Center);
            SoundEngine.PlaySound(SoundID.Item120 with { Volume = 0.55f, Pitch = AttackMode == CosmicDischargeAttackMode.Whip ? 0.2f : -0.2f }, Player.Center);

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new PulseRing(
                    Player.MountedCenter,
                    Vector2.Zero,
                    AttackMode == CosmicDischargeAttackMode.Whip ? CosmicDischargeCommon.FrostCoreColor * 0.8f : CosmicDischargeCommon.FrostGlowColor * 0.8f,
                    0.05f,
                    0.85f,
                    15));

                for (int i = 0; i < 20; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(
                        Player.MountedCenter,
                        velocity,
                        false,
                        Main.rand.Next(15, 25),
                        Main.rand.NextFloat(0.5f, 0.9f),
                        AttackMode == CosmicDischargeAttackMode.Whip ? CosmicDischargeCommon.FrostWhiteColor : CosmicDischargeCommon.FrostCoreColor));
                }
            }
        }

        public int ConsumeComboIndex(CosmicDischargeAttackMode mode)
        {
            if (comboMode != mode || comboResetTimer <= 0)
                comboIndex = 0;

            comboMode = mode;
            int current = comboIndex;
            comboIndex = (comboIndex + 1) % 3;
            comboResetTimer = 72;
            return current;
        }

        public void AddUltimateEnergy(int amount)
        {
            if (amount <= 0 || UltimateFieldActive)
                return;

            UltimateEnergy = Utils.Clamp(UltimateEnergy + amount, 0, UltimateEnergyMax);
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasUltimateReady, UltimateEnergy >= UltimateEnergyMax);
            SyncCooldownDisplays();
        }

        private bool CanTriggerIcebound()
        {
            return HoldingCosmicDischarge &&
                PassiveCooldownTimer <= 0 &&
                Player.active &&
                !Player.dead;
        }

        private void TriggerIcebound()
        {
            int healAmount = System.Math.Min(60, Player.statLifeMax2 - System.Math.Max(Player.statLife, 0));
            Player.statLife = System.Math.Min(Player.statLifeMax2, System.Math.Max(Player.statLife, 0) + 60);
            if (healAmount > 0)
                Player.HealEffect(healAmount, true);

            PassiveCooldownTimer = PassiveCooldownFrames;
            Player.AddBuff(ModContent.BuffType<CosmicDischargeIceboundBuff>(), IceboundDuration);
            Player.immune = true;
            Player.immuneNoBlink = true;
            Player.SetImmuneTimeForAllTypes(70);
            SyncCooldownDisplays();

            if (Player.whoAmI == Main.myPlayer)
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.75f, Pitch = 0.2f }, Player.Center);

            for (int i = 0; i < 32; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Player.Center + Main.rand.NextVector2Circular(Player.width, Player.height),
                    DustID.SnowflakeIce,
                    Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(2f, 7f),
                    120,
                    CosmicDischargeCommon.FrostWhiteColor,
                    Main.rand.NextFloat(1f, 1.55f));
                dust.noGravity = true;
            }
        }

        private void ApplyZeroDegreeArmament()
        {
            if (!Player.ZoneSnow && (!HasColdDebuff() || Player.HasBuff(BuffID.Warmth)))
                return;

            Player.GetDamage(DamageClass.Generic) += 0.1f;
            Player.statDefense += 10;
            Lighting.AddLight(Player.Center, CosmicDischargeCommon.FrostGlowColor.ToVector3() * 0.18f);
        }

        private bool HasColdDebuff()
        {
            return Player.HasBuff(BuffID.Chilled) ||
                Player.HasBuff(BuffID.Frozen) ||
                Player.HasBuff(BuffID.Frostburn) ||
                Player.HasBuff(BuffID.Frostburn2) ||
                Player.HasBuff(ModContent.BuffType<Nightwither>());
        }

        private void HandleUltimateInput()
        {
            if (Main.myPlayer != Player.whoAmI || !KeybindSystem.LegendarySkill.JustPressed)
                return;

            if (UltimateEnergy < UltimateEnergyMax || UltimateFieldActive)
                return;

            UltimateEnergy = 0;
            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref wasUltimateReady, false);
            SyncCooldownDisplays();

            int fieldType = ModContent.ProjectileType<CosmicDischargeUltimateField>();
            Projectile.NewProjectile(
                Player.GetSource_ItemUse(Player.HeldItem),
                Player.Center,
                Vector2.Zero,
                fieldType,
                Player.GetWeaponDamage(Player.HeldItem),
                0f,
                Player.whoAmI);

            SoundEngine.PlaySound(SoundID.Item120 with { Volume = 0.86f, Pitch = -0.12f }, Player.Center);
        }

        private void SyncCooldownDisplays()
        {
            SyncCooldown(CosmicDischargeUltimateCooldown.ID, UltimateEnergyMax, UltimateEnergy);
            SyncCooldown(CosmicDischargePassiveCooldown.ID, PassiveCooldownFrames, PassiveCooldownTimer);
            SyncCooldown(CosmicDischargeQuickDrawCooldown.ID, 1800, QuickDrawCooldownTimer);
        }

        private void SyncCooldown(string id, int duration, int timeLeft)
        {
            if (Player.Calamity().cooldowns.TryGetValue(id, out var cooldown))
            {
                cooldown.duration = duration;
                cooldown.timeLeft = timeLeft;
                return;
            }

            Player.AddCooldown(id, duration).timeLeft = timeLeft;
        }
    }
}
