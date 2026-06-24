using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode
{
    public class AnodizedWulfrumMetalEffect : DefaultEffect
    {
        public override int EffectID => 45;
        public override int AmmoType => ModContent.Find<ModItem>("CalamityMod/AnodizedWulfrumMetal").Type;

        public override bool EnableDefaultSlowdown => false;
        public override bool EnableProximityExplosion => false;
        public override bool SuppressDefaultOnKillEffects => true;
        public override float ExplosionPulseFactor => 0f;
        public override float SquishyLightParticleFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override float GlowIntensityFactor => 0f;

        // The source orb exists for one tick only: it is the muzzle-side burst point,
        // never a projectile that travels or falls through the world.
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.timeLeft = 1;
            projectile.tileCollide = false;
        }

        public override void AI(Projectile projectile, Player owner)
        {
        }

        public override void ModifyDamageHitbox(Projectile projectile, Player owner, ref Rectangle hitbox)
        {
            int size = 8;
            hitbox = new Rectangle(
                (int)(projectile.Center.X - size / 2f),
                (int)(projectile.Center.Y - size / 2f),
                size,
                size
            );
        }

        // A direct burst of 7-10 forward-facing WulfrumProsthesis-style shards.
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            int shardCount = Main.rand.Next(7, 11);
            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            float speed = projectile.velocity.Length();
            float halfSpread = MathHelper.ToRadians(2.5f);

            for (int i = 0; i < shardCount; i++)
            {
                float angle = Main.rand.NextFloat(-halfSpread, halfSpread);
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    direction.RotatedBy(angle) * speed,
                    ModContent.ProjectileType<AnodizedWulfrumShard>(),
                    projectile.damage,
                    projectile.knockBack,
                    projectile.owner
                );
            }

            for (int i = 0; i < 14; i++)
            {
                Dust spark = Dust.NewDustPerfect(
                    projectile.Center,
                    DustID.Electric,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 6f),
                    0,
                    new Color(190, 255, 60),
                    Main.rand.NextFloat(0.9f, 1.5f)
                );
                spark.noGravity = true;
            }

            Particle flash = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                new Color(200, 255, 70) * 0.75f,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One * 0.1f,
                0f,
                0.06f,
                0.28f,
                10,
                true
            );
            GeneralParticleHandler.SpawnParticle(flash);
        }
    }

    // Inherits WulfrumBolt so the original trail, homing, launch particles,
    // slowdown, hit particles, and hit sound stay intact. Only a small head is added.
    public class AnodizedWulfrumShard : WulfrumBolt
    {
        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center + direction * 4f - Main.screenPosition;

            // BloomCircle has a black source background, so it must use additive blending.
            // This follows WulfrumBolt's own primitive-drawing setup.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Additive,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );
            Main.spriteBatch.Draw(
                bloom,
                drawPosition,
                null,
                new Color(70, 215, 255) * 0.62f,
                0f,
                bloom.Size() / 2f,
                0.125f,
                SpriteEffects.None,
                0f
            );
            Main.spriteBatch.Draw(
                bloom,
                drawPosition,
                null,
                Color.White * 0.85f,
                0f,
                bloom.Size() / 2f,
                0.045f,
                SpriteEffects.None,
                0f
            );
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            return false;
        }
    }
}
