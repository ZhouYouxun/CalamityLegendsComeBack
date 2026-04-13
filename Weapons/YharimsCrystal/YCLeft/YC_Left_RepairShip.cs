using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft
{
    public class YC_Left_RepairShip : YC_LeftWarshipBase
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        private bool timerInitialized;

        private ref float SupportTimer => ref Projectile.localAI[0];
        private ref float ShieldTimer => ref Projectile.localAI[1];

        protected override Color AccentColor => new(110, 255, 220);
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCLeft/YC_Left_RepairShip";

        protected override float PositionLerp => 0.2f;
        protected override float ScaleBase => 0.92f;
        protected override float ScaleAmplitude => 0.03f;
        protected override float LightStrength => 0.48f;

        protected override Vector2 CalculateLocalOffset(float globalTime)
        {
            float phase = globalTime * 1.9f;
            float sideOffset = (float)Math.Sin(phase) * 10f;
            float forwardOffset = -84f + (float)Math.Cos(phase * 1.4f) * 8f;
            return new Vector2(sideOffset, forwardOffset);
        }

        protected override Vector2 CalculateDesiredAimDirection(YC_LeftHoldOut holdout, Projectile holdoutProjectile)
        {
            return (Owner.Center - Projectile.Center).SafeNormalize(holdout.ForwardDirection);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (ShieldTimer > 0f)
            {
                Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
                float shieldOpacity = Utils.GetLerpValue(0f, 16f, ShieldTimer, true);
                Main.EntitySpriteDraw(
                    glow,
                    Projectile.Center - Main.screenPosition,
                    null,
                    new Color(120, 245, 225, 0) * (0.22f * shieldOpacity),
                    0f,
                    glow.Size() * 0.5f,
                    Projectile.scale * 0.2f,
                    SpriteEffects.None,
                    0);
            }

            return base.PreDraw(ref lightColor);
        }

        protected override void UpdateAttack(YC_LeftHoldOut holdout, Projectile holdoutProjectile)
        {
            if (!timerInitialized)
            {
                SupportTimer = 40f;
                ShieldTimer = 0f;
                timerInitialized = true;
            }

            if (ShieldTimer > 0f)
                ShieldTimer--;

            if (SupportTimer > 0f)
            {
                SupportTimer--;
                EmitShieldFX();
                return;
            }

            ShieldTimer = 78f;
            SupportTimer = 78f;

            if (Projectile.owner != Main.myPlayer)
                return;

            int missingLife = Owner.statLifeMax2 - Owner.statLife;
            if (missingLife > 0)
            {
                Vector2 launchDirection = (Owner.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + launchDirection * 12f,
                    launchDirection * 4.8f,
                    ModContent.ProjectileType<YC_Left_RepairBolt>(),
                    0,
                    0f,
                    Projectile.owner);

                EmitMuzzleBurst(launchDirection, AccentColor, 2.8f, 5);
            }

            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.17f, Pitch = 0.28f }, Projectile.Center);
            EmitShieldBurst();
        }

        private void EmitShieldFX()
        {
            if (Main.dedServ || ShieldTimer <= 0f || Main.GameUpdateCount % 6 != 0)
                return;

            Vector2 orbitOffset = Main.rand.NextVector2CircularEdge(15f, 15f);
            YC_LeftSquadronHelper.EmitTechDust(
                Projectile.Center + orbitOffset,
                orbitOffset.RotatedBy(MathHelper.PiOver2) * 0.05f + Main.rand.NextVector2Circular(0.25f, 0.25f),
                Color.Lerp(new Color(110, 255, 225), Color.White, Main.rand.NextFloat(0.25f, 0.55f)),
                Main.rand.NextFloat(0.75f, 1.05f));
        }

        private void EmitShieldBurst()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 10; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                YC_LeftSquadronHelper.EmitTechDust(
                    Projectile.Center,
                    direction * Main.rand.NextFloat(1f, 2.8f),
                    Color.Lerp(new Color(100, 255, 220), Color.White, Main.rand.NextFloat(0.2f, 0.5f)),
                    Main.rand.NextFloat(0.8f, 1.1f));
            }
        }
    }
}
