using System;
using CalamityMod;
using CalamityMod.Items.Accessories;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    public class BFRecoveryShieldPlayer : ModPlayer
    {
        public float ShieldHitPoints;
        public float ShieldMaxHitPoints;
        public int ShieldHitFlashTimer;

        public bool ShieldActive => ShieldHitPoints > 0f && !Player.dead;

        public float ShieldChargeRatio =>
            ShieldMaxHitPoints <= 0f ? 0f : MathHelper.Clamp(ShieldHitPoints / ShieldMaxHitPoints, 0f, 1f);

        public bool ShouldDrawShield => ShieldActive;

        public override void ResetEffects()
        {
        }

        public override void UpdateDead()
        {
            ShieldHitPoints = 0f;
            ShieldMaxHitPoints = 0f;
            ShieldHitFlashTimer = 0;
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server)
                BFRecoveryShieldPackets.SendState(Player, toWho, fromWho);
        }

        public override void PostUpdate()
        {
            if (ShieldHitFlashTimer > 0)
                ShieldHitFlashTimer--;

            if (Player.dead)
            {
                ShieldHitPoints = 0f;
                ShieldMaxHitPoints = 0f;
                return;
            }

            if (ShieldHitPoints > ShieldMaxHitPoints)
                ShieldHitPoints = ShieldMaxHitPoints;

            if (Main.myPlayer != Player.whoAmI || !ShouldDrawShield)
                return;

            if (Player.ownedProjectileCounts[ModContent.ProjectileType<BFRecoveryShieldVisual>()] <= 0)
            {
                Projectile.NewProjectile(
                    Player.GetSource_Misc("BlossomFluxRecoveryShield"),
                    Player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<BFRecoveryShieldVisual>(),
                    0,
                    0f,
                    Player.whoAmI);
            }
        }

        internal void StartNewShieldBurst(float burstMaxShield)
        {
            float maxShield = Math.Max(10f, burstMaxShield);
            ShieldMaxHitPoints = maxShield;
            // A release creates a new shield. Carrying durability from the previous burst made
            // the displayed capacity and the damage absorber disagree, and could preserve an
            // old full shield through repeated releases.
            ShieldHitPoints = 0f;
            ShieldHitFlashTimer = 0;
        }

        public void AddShieldHitPoints(float amount)
        {
            if (amount <= 0f)
                return;

            if (ShieldMaxHitPoints <= 0f)
                ShieldMaxHitPoints = Math.Max(30f, amount);

            ShieldHitPoints = Math.Min(ShieldMaxHitPoints, ShieldHitPoints + amount);
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            // Damage resolution must happen exactly once on the authoritative side. Client-side
            // subtraction was the source of the intermittent "no damage" and desync reports.
            if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient || !ShieldActive)
                return;

            modifiers.ModifyHurtInfo += ApplyShieldDamage;
        }

        private void ApplyShieldDamage(ref Player.HurtInfo info)
        {
            if (info.Cancelled || info.Damage <= 0 || !ShieldActive)
                return;

            int incomingDamage = info.Damage;
            float absorbedDamage = Math.Min(incomingDamage, ShieldHitPoints);

            ShieldHitPoints = Math.Max(0f, ShieldHitPoints - absorbedDamage);
            info.Damage -= (int)absorbedDamage;
            ShieldHitFlashTimer = 18;

            if (info.Damage <= 0)
            {
                info.Damage = 0;
                Player.GiveIFrames(info.CooldownCounter, Player.ComputeHitIFrames(info), true);
            }

            if (Main.myPlayer == Player.whoAmI)
            {
                SoundEngine.PlaySound(
                    ShieldHitPoints <= 0f ? RoverDrive.BreakSound : RoverDrive.ShieldHurtSound,
                    Player.Center);
            }

            if (Main.netMode == Terraria.ID.NetmodeID.Server)
                BFRecoveryShieldPackets.SendState(Player, Player.whoAmI);
        }

        internal void ReceiveState(float hitPoints, float maxHitPoints, int hitFlashTimer)
        {
            ShieldMaxHitPoints = Math.Max(0f, maxHitPoints);
            ShieldHitPoints = MathHelper.Clamp(hitPoints, 0f, ShieldMaxHitPoints);
            ShieldHitFlashTimer = Math.Max(0, hitFlashTimer);
        }
    }
}
