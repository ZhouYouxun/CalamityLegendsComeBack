using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    public class AegisSparkExplosion : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color DirtGold = new(190, 132, 54);
        private static readonly Color HotGold = new(255, 216, 96);
        private static readonly Color HotWhite = new(255, 246, 196);

        public override void SetDefaults()
        {
            Projectile.width = 75;
            Projectile.height = 75;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 12;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Projectile.timeLeft >= 9 ? null : false;

        public override void AI()
        {
            if (Projectile.localAI[0]++ > 0f)
                return;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.62f, Pitch = -0.25f }, Projectile.Center);

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(Projectile.Center, Vector2.Zero,
                HotGold, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0f, 0.56f, 16));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero,
                HotWhite, Vector2.One, 0f, 0.07f, 1.05f, 17));

            for (int i = 0; i < 22; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.6f, 9.2f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(18f, 18f),
                    Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Torch, velocity, 0,
                    Main.rand.NextBool(3) ? HotWhite : HotGold, Main.rand.NextFloat(0.9f, 1.55f));
                dust.noGravity = true;
            }

            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.4f, 5.8f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f, 20f),
                    DustID.Dirt, velocity, 0, DirtGold, Main.rand.NextFloat(0.85f, 1.35f));
                dust.noGravity = Main.rand.NextBool(3);
            }

            for (int i = 0; i < 7; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 6.8f);
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(15f, 15f), velocity,
                    Color.Lerp(DirtGold, HotGold, Main.rand.NextFloat()), Color.Transparent,
                    Main.rand.NextFloat(0.32f, 0.62f), Main.rand.Next(14, 24), Main.rand.NextFloat(-0.08f, 0.08f)));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float fade = Projectile.timeLeft / 12f;
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null,
                HotGold with { A = 0 } * fade * 0.46f, 0f, bloom.Size() * 0.5f, 0.5f + (1f - fade) * 0.35f,
                SpriteEffects.None, 0f);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
