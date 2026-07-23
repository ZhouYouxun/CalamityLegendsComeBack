using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle
{
    internal sealed class AMRSubBullet : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/Ranged/AMRShot";

        private bool initialized;
        private int visualAge;

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.light = 0.5f;
            Projectile.alpha = 255;
            Projectile.extraUpdates = 10;
            Projectile.scale = 1.18f;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = ProjAIStyleID.Arrow;
            AIType = ProjectileID.BulletHighVelocity;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.alpha > 0)
                Projectile.alpha -= (int)(Projectile.velocity.Length() * 0.9f);
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (!initialized)
            {
                initialized = true;
                SpawnNitroReveal();
            }

            if (!CalamityUtils.FinalExtraUpdate(Projectile))
                return;

            visualAge++;
            if (Main.dedServ || visualAge > 18)
                return;

            Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
            GeneralParticleHandler.SpawnParticle(new AltSparkParticle(
                Projectile.Center,
                backward * 0.8f,
                false,
                13,
                0.72f,
                Color.Gold * 0.18f));

            if (visualAge % 2 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    backward * 0.42f,
                    false,
                    10,
                    0.25f,
                    new Color(255, 201, 72),
                    true,
                    false,
                    true));
            }
        }

        private void SpawnNitroReveal()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GemTopaz,
                    Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(24f)) * Main.rand.NextFloat(0.08f, 0.42f),
                    0,
                    new Color(255, 207, 91),
                    Main.rand.NextFloat(0.58f, 0.98f));
                dust.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GemTopaz,
                    Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(15f)) * Main.rand.NextFloat(0.08f, 0.65f),
                    0,
                    new Color(255, 215, 112),
                    Main.rand.NextFloat(0.6f, 1.05f));
                dust.noGravity = true;
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            if (Projectile.alpha < 140)
                return new Color(255, 255, 255, 100);

            return Color.Transparent;
        }
    }
}
