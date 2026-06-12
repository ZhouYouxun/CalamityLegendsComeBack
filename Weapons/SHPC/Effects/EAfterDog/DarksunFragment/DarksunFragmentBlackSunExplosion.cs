using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.DarksunFragment
{
    internal class DarksunFragmentBlackSunExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/Rogue/EclipseMirrorBurst";

        private int Level => Utils.Clamp((int)Projectile.ai[0], 3, DarksunFragmentBlackSun.MaxLevel);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 220;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 28;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.Resize(260 + Level * 64, 260 + Level * 64);
            Projectile.Center = Projectile.Center;

            if (Main.dedServ)
                return;

            Player owner = Main.player[Projectile.owner];
            owner.SetScreenshake(10f + Level * 1.5f);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DeadSunExplosion")
            {
                Volume = 1f,
                Pitch = -0.22f,
                PitchVariance = 0.04f,
                MaxInstances = 3
            }, Projectile.Center);

            for (int i = 0; i < 70 + Level * 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(5f, 20f + Level * 2f);
                Color color = Main.rand.NextBool(3) ? Color.Black : new Color(255, 196, 45);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(36f, 36f), DustID.GoldFlame, velocity, 0, color, Main.rand.NextFloat(1.2f, 2.4f));
                dust.noGravity = true;

                if (i % 2 == 0)
                {
                    Particle spark = new CustomSpark(
                        Projectile.Center,
                        velocity * Main.rand.NextFloat(0.55f, 0.9f),
                        "CalamityMod/Particles/VerticalSmearRagged",
                        false,
                        Main.rand.Next(18, 30),
                        Main.rand.NextFloat(1.4f, 2.8f),
                        color,
                        new Vector2(0.18f, 1f));
                    GeneralParticleHandler.SpawnParticle(spark);
                }
            }

            for (int i = 0; i < 4; i++)
            {
                Particle pulse = new CustomPulse(
                    Projectile.Center,
                    Vector2.Zero,
                    i % 2 == 0 ? new Color(255, 194, 42) : Color.Black,
                    i % 2 == 0 ? "CalamityMod/Particles/PlasmaExplosion" : "CalamityMod/Particles/BloomRing",
                    Vector2.One,
                    Main.rand.NextFloat(-8f, 8f),
                    0.05f,
                    0.55f + i * 0.12f,
                    22 + i * 4);
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            for (int directionIndex = 0; directionIndex < 8; directionIndex++)
            {
                Vector2 direction = (MathHelper.TwoPi * directionIndex / 8f).ToRotationVector2();
                Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
                for (int i = 0; i < 12 + Level * 2; i++)
                {
                    float distanceFactor = i / (11f + Level * 2f);
                    Vector2 start = Projectile.Center + normal * Main.rand.NextFloat(-10f, 10f) + direction * Main.rand.NextFloat(8f, 28f);
                    Vector2 velocity = direction * MathHelper.Lerp(7f, 24f + Level * 2.2f, distanceFactor) + normal * Main.rand.NextFloat(-1.9f, 1.9f);
                    Color color = i % 3 == 0 ? Color.Black : Color.Lerp(new Color(255, 210, 72), Color.White, Main.rand.NextFloat(0.18f));

                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        start,
                        velocity,
                        false,
                        Main.rand.Next(18, 34),
                        Main.rand.NextFloat(0.07f, 0.16f),
                        color,
                        new Vector2(3.4f, 0.42f),
                        true));
                }
            }
        }

        public override void AI()
        {
            Projectile.rotation += 0.18f;
            Projectile.Opacity = Projectile.timeLeft > 18 ? Utils.GetLerpValue(28f, 18f, Projectile.timeLeft, true) : Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.56f, 0.08f) * Projectile.Opacity);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float progress = 1f - Projectile.timeLeft / 28f;
            float scale = (0.72f + progress * 1.2f) * (1f + Level * 0.08f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPos, null, Color.Black * 0.7f * Projectile.Opacity, -Projectile.rotation, bloom.Size() * 0.5f, scale * 0.85f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPos, null, new Color(255, 196, 45, 0) * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, scale, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPos, null, Color.Black * 0.52f * Projectile.Opacity, -Projectile.rotation * 0.6f, texture.Size() * 0.5f, scale * 0.76f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            return false;
        }
    }
}
