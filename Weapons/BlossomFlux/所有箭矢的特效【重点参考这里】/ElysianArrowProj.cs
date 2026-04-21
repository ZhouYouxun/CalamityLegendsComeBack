using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    public class ElysianArrowProj : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        public override string Texture => "CalamityMod/Items/Ammo/ElysianArrow";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 1200;
            Projectile.extraUpdates = 2;
            Projectile.aiStyle = ProjAIStyleID.Arrow;
        }

        public override void AI()
        {
            Projectile.localAI[0] += 1f;

            Player Owner = Main.player[Projectile.owner];

            if (Projectile.localAI[0] > 5)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool() ? 262 : 87, -Projectile.velocity * Main.rand.NextFloat(0.1f, 0.7f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.4f, 1.1f);
                dust.alpha = 235;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }
        public override void OnKill(int timeLeft)
        {
            Player Owner = Main.player[Projectile.owner];
            float targetDist = Vector2.Distance(Owner.Center, Projectile.Center);

            NPC target = Projectile.Center.ClosestNPCAt(2000);
            Vector2 targetPosition = target == null ? Projectile.Center : target.Center;
            Vector2 spawnSpot = (target == null ? Projectile.Center : target.Center) + new Vector2(Main.rand.NextFloat(-450, 450), Main.rand.NextFloat(-750, -950));

            Vector2 velocity;
            if (target == null)
                velocity = (targetPosition - spawnSpot).SafeNormalize(Vector2.UnitX) * 20;
            else
                velocity = CalamityUtils.CalculatePredictiveAimToTargetMaxUpdates(spawnSpot, target, 20f, ElysianArrowRain.MaxUpdate);

            if (targetDist < 1400f)
            {
                int Dusts = 8;
                float radians = MathHelper.TwoPi / Dusts;
                Vector2 spinningPoint = Vector2.Normalize(new Vector2(-1f, -1f));
                for (int i = 0; i < Dusts; i++)
                {
                    Vector2 dustVelocity = spinningPoint.RotatedBy(radians * i) * 3.5f;
                    Dust dust = Dust.NewDustPerfect(spawnSpot, Main.rand.NextBool() ? 262 : 87, dustVelocity, 0, default, 0.9f);
                    dust.noGravity = true;

                    Dust dust2 = Dust.NewDustPerfect(spawnSpot, Main.rand.NextBool() ? 262 : 87, dustVelocity * 0.6f, 0, default, 1.2f);
                    dust2.noGravity = true;
                }
            }

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnSpot, velocity, ModContent.ProjectileType<ElysianArrowRain>(), (int)(Projectile.damage * 0.15f), 0f, Projectile.owner, 0f, 0f);

            SoundStyle onKill = new("CalamityMod/Sounds/Custom/Providence/ProvidenceHolyBlastShoot");
            SoundEngine.PlaySound(onKill with { Volume = 0.4f, Pitch = 0.4f }, Projectile.position);

            if (!Main.dedServ)
            {
                for (int k = 0; k < 7; k++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool() ? 262 : 87, Projectile.velocity.RotatedByRandom(0.4) * Main.rand.NextFloat(0.5f, 1.5f));
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.7f, 1.25f);
                    dust.alpha = 235;
                    if (Main.rand.NextBool())
                    {
                        Dust dust2 = Dust.NewDustPerfect(Projectile.Center, DustID.SteampunkSteam, new Vector2(3, 3).RotatedByRandom(100) * Main.rand.NextFloat(0.5f, 1.5f));
                        dust2.noGravity = true;
                        dust2.scale = Main.rand.NextFloat(0.8f, 1.5f);
                        dust2.alpha = 70;
                    }
                }
                for (int k = 0; k < 2; k++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.FireworksRGB, Projectile.velocity.RotatedByRandom(0.4) * Main.rand.NextFloat(0.5f, 1.5f));
                    dust.noGravity = false;
                    dust.scale = Main.rand.NextFloat(0.85f, 1f);
                    dust.color = Main.rand.NextBool() ? Color.Orange : Color.Khaki;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 180);
        }
    }
}
