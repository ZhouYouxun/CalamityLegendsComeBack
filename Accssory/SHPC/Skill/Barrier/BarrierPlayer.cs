using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityMod;
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
        public int ShieldHitFlashTimer;

        private int chargeDelayTimer;
        private float shieldHitPoints;

        public bool HoldingSHPC =>
            Player.HeldItem?.ModItem is NewLegendSHPC;

        public int ShieldMaxHitPoints
        {
            get
            {
                int rawShield = (int)System.MathF.Floor(Player.statLifeMax2 * 0.25f);
                return System.Math.Max(5, rawShield / 5 * 5);
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
        }

        public override void UpdateDead()
        {
            BarrierEquipped = false;
            BarrierVisible = false;
            chargeDelayTimer = 0;
            shieldHitPoints = 0f;
            ShieldHitFlashTimer = 0;
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

            if (ShieldActive)
                modifiers.SourceDamage *= 0.9f;

            modifiers.ModifyHurtInfo += ApplyShieldAndResetCharge;
        }

        private void ApplyShieldAndResetCharge(ref Player.HurtInfo info)
        {
            if (ShieldActive && info.Damage > 0)
            {
                int incomingDamage = info.Damage;
                int absorbedDamage = System.Math.Min(incomingDamage, ShieldCurrentHitPoints);
                bool fullyAbsorbedByShield = absorbedDamage >= incomingDamage;

                shieldHitPoints = System.Math.Max(0f, shieldHitPoints - absorbedDamage);
                info.Damage = System.Math.Max(0, incomingDamage - absorbedDamage);
                ShieldHitFlashTimer = 18;

                Player.GiveIFrames(info.CooldownCounter, Player.ComputeHitIFrames(info), true);
                if (fullyAbsorbedByShield)
                    Player.Calamity().freeDodgeFromShieldAbsorption = true;

                if (Main.myPlayer == Player.whoAmI)
                {
                    SoundEngine.PlaySound(
                        shieldHitPoints <= 0f ? RoverDrive.BreakSound : RoverDrive.ShieldHurtSound,
                        Player.Center);
                }
            }

            chargeDelayTimer = 0;
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
