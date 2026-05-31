using CalamityLegendsComeBack.Weapons.SHPC;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.Barrier
{
    public class BarrierPlayer : ModPlayer
    {
        public const int ChargeDelayFrames = 10 * 60;
        public const int FullChargeFrames = 5 * 60;

        public bool BarrierEquipped;
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

        public bool ShieldActive =>
            BarrierEquipped &&
            HoldingSHPC &&
            shieldHitPoints > 0f &&
            !Player.dead;

        public override void ResetEffects()
        {
            BarrierEquipped = false;
        }

        public override void UpdateDead()
        {
            BarrierEquipped = false;
            chargeDelayTimer = 0;
            shieldHitPoints = 0f;
            ShieldHitFlashTimer = 0;
        }

        public override void PostUpdate()
        {
            if (ShieldHitFlashTimer > 0)
                ShieldHitFlashTimer--;

            if (!BarrierEquipped || !HoldingSHPC || Player.dead)
            {
                chargeDelayTimer = 0;
                shieldHitPoints = 0f;
                return;
            }

            int shieldMax = ShieldMaxHitPoints;
            if (shieldHitPoints > shieldMax)
                shieldHitPoints = shieldMax;

            if (shieldHitPoints < shieldMax)
            {
                if (chargeDelayTimer < ChargeDelayFrames)
                    chargeDelayTimer++;
                else
                    shieldHitPoints = System.Math.Min(shieldMax, shieldHitPoints + shieldMax / (float)FullChargeFrames);
            }

            if (Main.myPlayer != Player.whoAmI || !ShieldActive)
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
                int absorbedDamage = System.Math.Min(info.Damage, ShieldCurrentHitPoints);
                shieldHitPoints = System.Math.Max(0f, shieldHitPoints - absorbedDamage);
                info.Damage -= absorbedDamage;
                ShieldHitFlashTimer = 18;
            }

            chargeDelayTimer = 0;
        }
    }
}
