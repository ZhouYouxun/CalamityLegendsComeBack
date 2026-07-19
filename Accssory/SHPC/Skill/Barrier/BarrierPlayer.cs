using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.Cooldowns;
using CalamityMod.Items.Accessories;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.Barrier
{
    public class BarrierPlayer : ModPlayer
    {
        public const int ChargeDelayFrames = 10 * 60;
        public const int FullChargeFrames = 5 * 60;

        public bool BarrierEquipped;
        public bool BarrierVisible;
        public bool AIOCBarrierBoost;
        public int ShieldHitFlashTimer;

        private int chargeDelayTimer;
        private float shieldHitPoints;
        private bool spongeShieldDeferred;

        public bool HoldingSHPC =>
            Player.HeldItem?.ModItem is NewLegendSHPC;

        public int ShieldMaxHitPoints
        {
            get
            {
                int rawShield = (int)System.MathF.Floor(Player.statLifeMax2 * 0.25f);
                int baseShield = System.Math.Max(5, rawShield / 5 * 5);
                return AIOCBarrierBoost ? (int)System.MathF.Round(baseShield * 1.5f) : baseShield;
            }
        }

        public int ShieldCurrentHitPoints => System.Math.Max(0, (int)System.MathF.Ceiling(shieldHitPoints));

        public float ShieldChargeRatio =>
            ShieldMaxHitPoints <= 0 ? 0f : MathHelper.Clamp(shieldHitPoints / ShieldMaxHitPoints, 0f, 1f);

        public int RechargeDelayRemainingFrames =>
            shieldHitPoints >= ShieldMaxHitPoints
                ? 0
                : System.Math.Max(0, ChargeDelayFrames - chargeDelayTimer);

        public bool ShieldActive =>
            BarrierEquipped &&
            HoldingSHPC &&
            shieldHitPoints > 0f &&
            !Player.dead;

        public bool ShouldDrawShield =>
            ShieldActive &&
            BarrierVisible;

        public override void ResetEffects()
        {
            BarrierEquipped = false;
            BarrierVisible = false;
            AIOCBarrierBoost = false;
            spongeShieldDeferred = false;
        }

        public override void UpdateDead()
        {
            BarrierEquipped = false;
            BarrierVisible = false;
            AIOCBarrierBoost = false;
            chargeDelayTimer = 0;
            shieldHitPoints = 0f;
            ShieldHitFlashTimer = 0;
            spongeShieldDeferred = false;
        }

        public override void PostUpdate()
        {
            if (ShieldHitFlashTimer > 0)
                ShieldHitFlashTimer--;

            if (Player.dead || !BarrierEquipped)
            {
                chargeDelayTimer = 0;
                shieldHitPoints = 0f;
                return;
            }

            int shieldMax = ShieldMaxHitPoints;
            if (shieldHitPoints > shieldMax)
                shieldHitPoints = shieldMax;

            float previousHitPoints = shieldHitPoints;
            if (shieldHitPoints < shieldMax)
            {
                if (chargeDelayTimer < ChargeDelayFrames)
                    chargeDelayTimer++;
                else
                    shieldHitPoints = System.Math.Min(shieldMax, shieldHitPoints + shieldMax / (float)FullChargeFrames);
            }

            if (previousHitPoints < shieldMax && shieldHitPoints >= shieldMax && Main.myPlayer == Player.whoAmI)
                SoundEngine.PlaySound(RoverDrive.ActivationSound, Player.Center);

            SyncCooldownDisplays();

            if (Main.myPlayer != Player.whoAmI || !ShouldDrawShield)
                return;

            if (Player.ownedProjectileCounts[ModContent.ProjectileType<BarrierShieldVisual>()] <= 0)
            {
                Projectile.NewProjectile(
                    Player.GetSource_Accessory(Player.HeldItem),
                    Player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<BarrierShieldVisual>(),
                    0,
                    0f,
                    Player.whoAmI);
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (!BarrierEquipped || !HoldingSHPC)
                return;

            CalamityPlayer calamityPlayer = Player.Calamity();
            bool roverDriveShieldActive = calamityPlayer.roverDrive &&
                                          calamityPlayer.RoverDriveShieldDurability > 0;

            // Rover Drive owns its hit, including the source-damage reduction it provides.
            if (ShieldActive && !roverDriveShieldActive)
                modifiers.SourceDamage *= 0.9f;

            // Calamity normally processes The Sponge before this callback. Defer it while the
            // matrix shield is active, so the final order is Rover Drive -> Matrix -> Sponge.
            spongeShieldDeferred = ShieldActive && calamityPlayer.sponge &&
                                   calamityPlayer.SpongeShieldDurability > 0;
            if (spongeShieldDeferred)
                calamityPlayer.sponge = false;

            modifiers.ModifyHurtInfo += ApplyShieldAndRecharge;
        }

        private void ApplyShieldAndRecharge(ref Player.HurtInfo info)
        {
            CalamityPlayer calamityPlayer = Player.Calamity();
            if (info.Cancelled)
            {
                RestoreDeferredSponge(calamityPlayer);
                return;
            }

            if (ShieldActive && info.Damage > 0)
            {
                int incomingDamage = info.Damage;
                // A single hit can never destroy a fully charged matrix shield. It absorbs the
                // entire hit, but the durability cost is capped at half of its maximum capacity.
                float maximumLossFromOneHit = ShieldMaxHitPoints * 0.5f;
                float absorbedDamage = System.Math.Min(incomingDamage, System.Math.Min(shieldHitPoints, maximumLossFromOneHit));
                bool wasFullyCharged = shieldHitPoints >= ShieldMaxHitPoints;

                shieldHitPoints = System.Math.Max(0f, shieldHitPoints - absorbedDamage);
                // Do not use Calamity's freeDodgeFromShieldAbsorption flag here. That path still
                // applies the standard shield-hit Adrenaline penalty. A zero-damage hurt keeps
                // the shield feedback and iframes while leaving Adrenaline untouched.
                info.Damage = 0;
                ShieldHitFlashTimer = 18;

                // Start a delay only for a new recharge cycle. Damage taken during an existing
                // delay or recharge period deliberately leaves that progress untouched.
                if (wasFullyCharged && shieldHitPoints < ShieldMaxHitPoints)
                    chargeDelayTimer = 0;

                Player.GiveIFrames(info.CooldownCounter, Player.ComputeHitIFrames(info), true);
                if (Main.myPlayer == Player.whoAmI)
                {
                    SoundEngine.PlaySound(
                        shieldHitPoints <= 0f ? RoverDrive.BreakSound : RoverDrive.ShieldHurtSound,
                        Player.Center);
                }
            }

            AbsorbWithDeferredSponge(ref info, calamityPlayer);
        }

        private void AbsorbWithDeferredSponge(ref Player.HurtInfo info, CalamityPlayer calamityPlayer)
        {
            if (!spongeShieldDeferred)
                return;

            RestoreDeferredSponge(calamityPlayer);

            if (info.Damage > 0 && calamityPlayer.SpongeShieldDurability > 0)
            {
                int absorbedDamage = System.Math.Min(info.Damage, calamityPlayer.SpongeShieldDurability);
                bool fullyAbsorbedByShield = absorbedDamage >= info.Damage;
                calamityPlayer.SpongeShieldDurability -= absorbedDamage;
                info.Damage -= absorbedDamage;

                if (calamityPlayer.SpongeShieldDurability <= 0)
                {
                    calamityPlayer.SpongeShieldDurability = 0;
                    SoundEngine.PlaySound(TheSponge.BreakSound, Player.Center);
                    calamityPlayer.GeneralScreenShakePower += 2f;
                }

                if (fullyAbsorbedByShield)
                {
                    Player.GiveIFrames(info.CooldownCounter, Player.ComputeHitIFrames(info), true);
                    calamityPlayer.freeDodgeFromShieldAbsorption = true;
                }

                if (calamityPlayer.cooldowns.TryGetValue(SpongeDurability.ID, out var durabilityCooldown))
                    durabilityCooldown.timeLeft = calamityPlayer.SpongeShieldDurability;
            }

            // The Sponge keeps Calamity's default behavior: every hit restarts its recharge delay.
            Player.AddCooldown(SpongeRecharge.ID, TheSponge.ShieldRechargeDelay, true);
        }

        private void RestoreDeferredSponge(CalamityPlayer calamityPlayer)
        {
            if (!spongeShieldDeferred)
                return;

            calamityPlayer.sponge = true;
            spongeShieldDeferred = false;
        }

        private void SyncCooldownDisplays()
        {
            if (shieldHitPoints > 0f)
                SyncCooldown(BarrierDurabilityCooldown.ID, ShieldMaxHitPoints, ShieldCurrentHitPoints);

            int rechargeFrames = RechargeDelayRemainingFrames;
            if (rechargeFrames > 0)
                SyncCooldown(BarrierRechargeCooldown.ID, ChargeDelayFrames, rechargeFrames);
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
