using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    internal class BFArrow_ABreak : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        // A 战术右键箭：先弹射开路，再在飞行后段自动追猎。
        private ref float BounceCounter => ref Projectile.ai[0];
        private ref float FlightTimer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            BFArrowCommon.SetBaseArrowDefaults(Projectile, width: 14, height: 34, timeLeft: 240, penetrate: 2, extraUpdates: 1, tileCollide: true);
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            FlightTimer++;
            BFArrowCommon.FaceForward(Projectile);
            Lighting.AddLight(Projectile.Center, new Color(132, 255, 132).ToVector3() * 0.45f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Grass,
                    -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    100,
                    new Color(132, 255, 132),
                    Main.rand.NextFloat(0.85f, 1.2f));
                dust.noGravity = true;
            }

            if (FlightTimer >= 24f)
            {
                NPC target = Projectile.Center.ClosestNPCAt(950f);
                if (target != null)
                {
                    BFArrowCommon.WeakHomeTowards(Projectile, target, 18f, 19f);
                    BFArrowCommon.MaintainSpeed(Projectile, 19f, 0.08f);
                }
            }
            else
            {
                Projectile.velocity *= 1.003f;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.35f, Pitch = 0.2f }, Projectile.Center);
            if (BFArrowCommon.Bounce(Projectile, oldVelocity, ref BounceCounter, 3, 0.98f))
                return true;

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GrassBlades,
                    Main.rand.NextVector2Circular(2.2f, 2.2f),
                    100,
                    new Color(132, 255, 132),
                    Main.rand.NextFloat(0.85f, 1.3f));
                dust.noGravity = true;
            }

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(3f, 3f) * Main.rand.NextFloat(1.2f, 4.8f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Grass,
                    velocity,
                    100,
                    new Color(150, 255, 150),
                    Main.rand.NextFloat(1f, 1.35f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.22f, Pitch = 0.25f }, target.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            BFArrowCommon.DrawAfterimagesThenProjectile(Projectile, lightColor);
            return false;
        }
    }
}
