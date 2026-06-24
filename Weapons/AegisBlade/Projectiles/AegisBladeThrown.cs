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
    public class AegisBladeThrown : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/AegisBlade/AegisBlade";

        private bool embedded;
        private bool wallSpawned;
        private int embedTimer;

        private static readonly Color CoreColor = new(255, 244, 190);
        private static readonly Color GoldColor = new(255, 196, 62);
        private static readonly Color FireColor = new(255, 120, 38);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.ignoreWater = true;
            Projectile.scale = 1.35f;
        }

        public override void AI()
        {
            if (!embedded)
            {
                Projectile.rotation = MathHelper.PiOver2 + MathHelper.PiOver4;
                Projectile.velocity.Y = System.MathF.Min(Projectile.velocity.Y + 0.8f, 30f);
                Lighting.AddLight(Projectile.Center, GoldColor.ToVector3() * 0.65f);

                if (!Main.dedServ && Main.rand.NextBool(2))
                    EmitFallFlame();
                return;
            }

            embedTimer++;
            if (!wallSpawned && embedTimer == 1)
            {
                SpawnWalls();
                wallSpawned = true;
            }

            if (embedTimer > 40)
                Projectile.Kill();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            embedded = true;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60;
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 1f, Pitch = -0.38f }, Projectile.Center);
            EmitImpact(Projectile.Center, oldVelocity.SafeNormalize(Vector2.UnitY));
            return false;
        }

        private void EmitFallFlame()
        {
            Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(8f, 8f) + Vector2.UnitY * Main.rand.NextFloat(8f, 24f);
            GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                position, new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(1.2f, 4.8f)),
                Color.Lerp(FireColor, GoldColor, Main.rand.NextFloat(0.25f, 0.85f)), Color.Transparent,
                Main.rand.NextFloat(0.38f, 0.64f), Main.rand.Next(16, 25), Main.rand.NextFloat(-0.07f, 0.07f)));
        }

        private void EmitImpact(Vector2 position, Vector2 direction)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(position, Vector2.Zero,
                CoreColor, new Vector2(1.2f, 0.78f), direction.ToRotation(), 0f, 0.88f, 22));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(position, Vector2.Zero,
                GoldColor, new Vector2(2.1f, 0.66f), 0f, 0.08f, 1.45f, 20));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(position, Vector2.Zero,
                FireColor, new Vector2(1.35f, 0.46f), MathHelper.PiOver2, 0.1f, 1.2f, 18));
            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-6.5f, 6.5f), -Main.rand.NextFloat(2.5f, 11f));
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    position + Main.rand.NextVector2Circular(12f, 5f), velocity,
                    Color.Lerp(FireColor, GoldColor, Main.rand.NextFloat()), Color.Transparent,
                    Main.rand.NextFloat(0.42f, 0.82f), Main.rand.Next(18, 30), Main.rand.NextFloat(-0.08f, 0.08f)));
            }
        }

        private void SpawnWalls()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            int wallType = ModContent.ProjectileType<AegisWallProjectile>();
            int halfHeight = AegisWallProjectile.WallHalfHeight;
            int spread = BalanceAegisBlade.WallSpreadPixels;
            float riseSpeed = halfHeight / (float)BalanceAegisBlade.WallRiseTime;
            int wallDamage = System.Math.Max(1, (int)(Projectile.damage * 0.8f));

            Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                new Vector2(Projectile.Center.X - spread, Projectile.Center.Y), new Vector2(0f, -riseSpeed),
                wallType, wallDamage, 4f, Projectile.owner, -1f);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                new Vector2(Projectile.Center.X + spread, Projectile.Center.Y), new Vector2(0f, -riseSpeed),
                wallType, wallDamage, 4f, Projectile.owner, 1f);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.72f, Pitch = -0.48f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D swordTexture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = new(0f, swordTexture.Height);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            float glowStrength = embedded ? System.MathF.Max(0f, 1f - embedTimer / 40f) : 0.55f;
            for (int i = 0; i < 6; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 2.4f * glowStrength;
                Main.EntitySpriteDraw(swordTexture, drawPosition + offset, null, GoldColor with { A = 0 } * glowStrength * 0.1f,
                    Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            }
            Main.EntitySpriteDraw(bloomTexture, drawPosition, null, CoreColor with { A = 0 } * glowStrength * 0.5f,
                0f, bloomTexture.Size() * 0.5f, 0.55f + glowStrength * 0.25f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();

            Main.EntitySpriteDraw(swordTexture, drawPosition, null, lightColor,
                Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
