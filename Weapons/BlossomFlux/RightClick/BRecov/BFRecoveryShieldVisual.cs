using CalamityMod.Particles;
using CalamityLegendsComeBack.UI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    public class BFRecoveryShieldVisual : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            BFRecoveryShieldPlayer modPlayer = owner.GetModPlayer<BFRecoveryShieldPlayer>();
            if (!modPlayer.ShouldDrawShield)
            {
                bool brokeFromDamage = modPlayer.ShieldHitPoints == 0f && modPlayer.ShieldHitFlashTimer > 0;
                if (!Main.dedServ && brokeFromDamage)
                    SpawnGreenBreakEffect(owner.Center + new Vector2(0f, -46f));
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 2;
        }

        private static void SpawnGreenBreakEffect(Vector2 headCenter)
        {
            Color green = new Color(70, 240, 140) with { A = 0 };
            Color brightGreen = new Color(180, 255, 210) with { A = 0 };

            for (int i = 0; i < 18; i++)
            {
                Vector2 offset = new Vector2(
                    Main.rand.NextFloat(-24f, 24f),
                    Main.rand.NextFloat(-8f, 8f));
                Vector2 vel = offset.SafeNormalize(Vector2.UnitY * -1f) * Main.rand.NextFloat(1.5f, 5.5f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    headCenter + offset, vel, false,
                    Main.rand.Next(12, 24),
                    Main.rand.NextFloat(0.5f, 1.1f),
                    Color.Lerp(green, brightGreen, Main.rand.NextFloat())));
            }

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                headCenter, Vector2.Zero,
                new Color(80, 240, 150) * 0.6f,
                new Vector2(1.4f, 0.6f),
                0f, 0.04f, 0.9f, 18));

            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    headCenter + Main.rand.NextVector2Circular(16f, 6f),
                    Main.rand.NextBool(2) ? DustID.GemEmerald : DustID.GreenTorch,
                    Main.rand.NextVector2Circular(3f, 3f),
                    90,
                    Color.Lerp(green, brightGreen, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.0f, 1.4f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            BFRecoveryShieldPlayer modPlayer = owner.GetModPlayer<BFRecoveryShieldPlayer>();

            float hit = MathHelper.Clamp(modPlayer.ShieldHitFlashTimer / 18f, 0f, 1f);
            float charge = MathHelper.Clamp(modPlayer.ShieldChargeRatio, 0f, 1f);
            Vector2 center = owner.Center + new Vector2(0f, owner.gfxOffY - 48f) - Main.screenPosition;
            if (hit > 0f)
                center += Main.rand.NextVector2Circular(1.2f * hit, 1.2f * hit);

            float opacity = MathHelper.Clamp(0.68f + charge * 0.24f + hit * 0.2f, 0f, 1f);
            BoundedHeadBarRenderer.DrawImmediate(
                Main.spriteBatch,
                center,
                charge,
                new Color(10, 33, 19, 224),
                new Color(60, 220, 120),
                new Color(190, 255, 215),
                opacity,
                hit,
                Main.GlobalTimeWrappedHourly + Projectile.identity * 0.11f);

            return false;
        }
    }
}
