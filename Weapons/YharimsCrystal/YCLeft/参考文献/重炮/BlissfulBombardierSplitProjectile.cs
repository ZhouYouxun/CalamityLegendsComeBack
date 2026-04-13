using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.Projectiles.Ranged.BlissfulBombardierHoldout;

namespace CalamityMod.Projectiles.Ranged
{
    public class BlissfulBombardierSplitProjectile : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        private ref float RocketID => ref Projectile.ai[0];
        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 4;
            Projectile.timeLeft = 400;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Player Owner = Main.player[Projectile.owner];
            float targetDist = Vector2.Distance(Owner.Center, Projectile.Center);
            if (Projectile.Center.Y > Owner.ClampedMouseWorld().Y)
                Projectile.tileCollide = true;

            //Animation
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Projectile.velocity *= 1.005f;

            if (Projectile.timeLeft % 2 == 0 && targetDist < 1400)
            {
                Particle spark = new GlowSparkParticle(Projectile.Center + Projectile.velocity * Main.rand.NextFloat(-2, -1), -Projectile.velocity * 0.3f, false, 5, 0.06f, effectsColor * 0.65f, new Vector2(1, 0.3f), true, false, 1.5f);
                GeneralParticleHandler.SpawnParticle(spark);
            }
            else
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<LightDust>(), (Projectile.velocity * -4).RotatedByRandom(0.2) * Main.rand.NextFloat(0.2f, 1f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.35f, 0.55f);
                dust.color = Main.rand.NextBool(3) ? Color.Orange : Color.Goldenrod;
                dust.noLightEmittence = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(ModContent.BuffType<HolyFlames>(), 120);

        public override void OnKill(int timeLeft)
        {
            // Don't do rocket effects on the server
            if (Main.dedServ)
                return;

            var info = new CalamityUtils.RocketBehaviorInfo((int)RocketID)
            {
                // Since we use our own spawning method for the cluster rockets, we don't need them to shoot anything,
                // we'll do it ourselves.
                clusterProjectileID = ProjectileID.None,
                destructiveClusterProjectileID = ProjectileID.None,
            };

            bool isClusterRocket = (RocketID == ItemID.ClusterRocketI || RocketID == ItemID.ClusterRocketII);
            SoundStyle fire = new("CalamityMod/Sounds/Custom/Providence/ProvidenceHolyBlastImpact");
            SoundEngine.PlaySound(fire with { Volume = 0.6f, PitchVariance = 0.2f }, Projectile.Center);

            int blastRadius = (int)(MathHelper.Clamp(Projectile.RocketBehavior(info), 3, 100) * 0.5f);
            Projectile.ExpandHitboxBy((float)blastRadius);
            Projectile.damage = (int)(Projectile.damage * 0.5f);
            Projectile.penetrate = -1;
            Projectile.Damage();

            Particle orb5 = new CustomPulse(Projectile.Center, Vector2.Zero, Color.Orange, "CalamityMod/Particles/SoftRoundExplosion", new Vector2(1, 1), Main.rand.NextFloat(-10, 10), 0, 0.025f * blastRadius, 13);
            GeneralParticleHandler.SpawnParticle(orb5);

            Particle orb3 = new CustomPulse(Projectile.Center, Vector2.Zero, staticEffectsColor, "CalamityMod/Particles/SmallBloom", new Vector2(1, 1), Main.rand.NextFloat(-10, 10), 0, 0.22f * blastRadius, 10, true);
            GeneralParticleHandler.SpawnParticle(orb3);

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<LightDust>(), (new Vector2(5, 5) * blastRadius).RotatedByRandom(100) * Main.rand.NextFloat(0.2f, 1f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.85f, 2.45f) * blastRadius * 0.08f;
                dust.color = Color.Goldenrod;
                dust.noLightEmittence = true;
            }
            for (int i = 0; i < 4; i++)
            {
                Particle spark = new CustomSpark(Projectile.Center + Main.rand.NextVector2Circular(10, 10), (Projectile.velocity.SafeNormalize(Vector2.UnitX) * 10) * Main.rand.NextFloat(0.2f, 1f), "CalamityMod/Particles/ProvidenceMarkParticle", false, 15, Main.rand.NextFloat(1.35f, 1.6f), Main.rand.NextBool(4) ? Color.Khaki : effectsColor, new Vector2(1.3f, 0.5f), true, false, 0, false, false, Main.rand.NextFloat(0.45f, 0.55f));
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Ranged/BlissfulBombardierSplitProjectile").Value;

            Projectile.DrawProjectileWithBackglow(staticEffectsColor with { A = 0 }, lightColor, 3f, texture);
            return false;
        }
    }
}
