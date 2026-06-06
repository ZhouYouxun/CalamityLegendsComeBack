using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFIdle_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 168;
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 3;
            Projectile.timeLeft = Lifetime;
            Projectile.MaxUpdates = 2;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Color color = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 146, 62));
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.035f, -8f, 8f);
            Projectile.velocity.X *= 0.996f;
            Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.34f);

            if (Main.dedServ)
                return;

            if (Timer == 7f && !Main.rand.NextBool(4))
                SpawnSparks(3, 0.72f);

            if (Timer < 8f)
                return;

            Dust cinder = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                ModContent.DustType<FinalFlame>(),
                Projectile.velocity * 1.18f + Main.rand.NextVector2Circular(0.34f, 0.34f),
                10,
                Color.Lerp(color, Color.White, Main.rand.NextFloat(0.04f, 0.24f)),
                Main.rand.NextFloat(0.58f, 0.9f));
            cinder.noGravity = true;
            cinder.fadeIn = 1f;
            if (Main.rand.NextBool(3))
            {
                cinder.scale *= 2.6f;
                cinder.velocity *= 1.35f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 120);
            SpawnSparks(5, 1f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnSparks(8, 1f);
            return true;
        }

        public override void OnKill(int timeLeft) => SpawnSparks(5, 0.9f);

        private void SpawnSparks(int count, float scale)
        {
            if (Main.dedServ)
                return;

            Color color = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 146, 62));
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(34f)) * Main.rand.NextFloat(0.7f, 1.8f);
                velocity.Y -= Main.rand.NextFloat(2.5f, 5.8f);
                Particle spark = new SparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    velocity,
                    true,
                    Main.rand.Next(20, 38),
                    Main.rand.NextFloat(0.24f, 0.48f) * scale,
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.08f, 0.38f)));
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
