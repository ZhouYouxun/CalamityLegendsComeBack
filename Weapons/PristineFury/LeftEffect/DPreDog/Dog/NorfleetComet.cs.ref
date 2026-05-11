using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using CalamityMod.Particles;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;

namespace CalamityMod.Projectiles.Ranged
{
    public class NorfleetComet : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public int time = 0;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 15;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 5;
            Projectile.timeLeft = 1200;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            float rotationRando = Main.rand.NextFloat(-0.02f, 0.02f);
            Projectile.velocity = Projectile.velocity.RotatedBy(rotationRando);
            Projectile.rotation += 0.05f;
            if (Projectile.ai[2] == 1)
            {
                Projectile.friendly = false;
                if (time >= 75)
                {
                    Projectile.hostile = true;

                    float distanceFromPlayer = Projectile.Distance(player.Center);

                    // This is done instead of a Normalize or DirectionTo call because the variables needed are already present and calculating the square root again would be unnecessary.
                    Vector2 idealVelocity = (player.Center - Projectile.Center) / distanceFromPlayer * 8f;

                    Projectile.velocity.X += Math.Sign(idealVelocity.X - Projectile.velocity.X) * (0.0005f * time);
                    Projectile.velocity.Y += Math.Sign(idealVelocity.Y - Projectile.velocity.Y) * (0.0005f * time);

                    if (Projectile.Hitbox.Intersects(player.Hitbox))
                        Projectile.Kill();

                    Projectile.timeLeft = 5;
                }
            }
            if (time > 25)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 dustPos = Projectile.Center + (i * MathHelper.Pi + Projectile.rotation + MathHelper.PiOver2).ToRotationVector2() * 30f;
                    Dust dust = Dust.NewDustPerfect(dustPos, (Projectile.ai[2] == 1 ? 219 : Main.rand.NextBool(3) ? 272 : 86), (i * MathHelper.Pi + Projectile.rotation * Math.Sign(Projectile.velocity.X)).ToRotationVector2() * 3f);
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.6f, 0.9f);
                }
            }

            // Projectile ai 1 decides the type, 0 is fire, 1 is electricity, and 2 is poison

            time++;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player Owner = Main.player[Projectile.owner];
            Owner.SetScreenshake(7.5f);
            SoundStyle fire = new("CalamityMod/Sounds/Item/ScorpioNukeHit");
            SoundEngine.PlaySound(fire with { Volume = 0.75f, Pitch = 0.6f, PitchVariance = 0.2f }, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<NorfleetExplosion>(), Projectile.damage / 2, Projectile.knockBack, Projectile.owner, 0, Projectile.ai[1], Projectile.ai[2] == 1 ? 1 : 0);

            if (Projectile.ai[1] == 0)
            {
                Color partColor = Color.OrangeRed;
                int partLifetime = 10;
                float scale = 0.1f;
                Vector2 spawnPos = Projectile.Center;
                Particle blastRing = new CustomPulse(spawnPos, Vector2.Zero, partColor, "CalamityMod/Particles/FlameExplosion", Vector2.One, Main.rand.NextFloat(-10, 10), 0.07f, scale, partLifetime);
                GeneralParticleHandler.SpawnParticle(blastRing);
                Particle blastRing2 = new CustomPulse(spawnPos, Vector2.Zero, partColor * 0.4f, "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, Main.rand.NextFloat(-10, 10), 0.07f, scale * 4, partLifetime);
                GeneralParticleHandler.SpawnParticle(blastRing2);
                for (int k = 0; k < 15; k++)
                {
                    Vector2 velocity = new Vector2(15, 15).RotatedByRandom(100) * Main.rand.NextFloat(0.3f, 1.2f);
                    Particle spark = new SparkParticle(Projectile.Center + velocity, velocity, false, 45, Main.rand.NextFloat(0.95f, 1.35f), partColor);
                    GeneralParticleHandler.SpawnParticle(spark);
                }
                for (int k = 0; k < 30; k++)
                {
                    Vector2 velocity = new Vector2(10, 10).RotatedByRandom(100) * Main.rand.NextFloat(0.3f, 1.2f);
                    Dust dust2 = Dust.NewDustPerfect(Projectile.Center + velocity, DustID.SolarFlare, velocity);
                    dust2.scale = Main.rand.NextFloat(1.15f, 1.45f);
                    dust2.noGravity = true;
                }
            }
            if (Projectile.ai[1] == 1)
            {
                Color partColor = Color.Cyan;
                int partLifetime = 10;
                float scale = 0.1f;
                Vector2 spawnPos = Projectile.Center;
                Particle blastRing = new CustomPulse(spawnPos, Vector2.Zero, partColor, "CalamityMod/Particles/PlasmaExplosion", Vector2.One, Main.rand.NextFloat(-10, 10), 0.07f, scale, partLifetime);
                GeneralParticleHandler.SpawnParticle(blastRing);
                Particle blastRing2 = new CustomPulse(spawnPos, Vector2.Zero, partColor * 0.4f, "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, Main.rand.NextFloat(-10, 10), 0.07f, scale * 4, partLifetime);
                GeneralParticleHandler.SpawnParticle(blastRing2);
                for (int k = 0; k < 15; k++)
                {
                    Vector2 velocity = new Vector2(15, 15).RotatedByRandom(100) * Main.rand.NextFloat(0.3f, 1.2f);
                    Particle spark = new CritSpark(Projectile.Center + velocity, velocity, Color.White, partColor, Main.rand.NextFloat(0.45f, 0.65f), 45, Main.rand.NextFloat(-2, 2), 2);
                    GeneralParticleHandler.SpawnParticle(spark);
                }
                for (int k = 0; k < 30; k++)
                {
                    Vector2 velocity = new Vector2(10, 10).RotatedByRandom(100) * Main.rand.NextFloat(0.3f, 1.2f);
                    Dust dust2 = Dust.NewDustPerfect(Projectile.Center + velocity, DustID.Electric, velocity);
                    dust2.scale = Main.rand.NextFloat(1.15f, 1.45f);
                    dust2.noGravity = false;
                }
            }
            if (Projectile.ai[1] == 2)
            {
                Color partColor = Color.GreenYellow;
                int partLifetime = 10;
                float scale = 0.1f;
                Vector2 spawnPos = Projectile.Center;
                Particle blastRing = new CustomPulse(spawnPos, Vector2.Zero, partColor, "CalamityMod/Particles/DustyCircleHardEdge", Vector2.One, Main.rand.NextFloat(-10, 10), 0.07f, scale, partLifetime);
                GeneralParticleHandler.SpawnParticle(blastRing);
                Particle blastRing2 = new CustomPulse(spawnPos, Vector2.Zero, partColor * 0.4f, "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, Main.rand.NextFloat(-10, 10), 0.07f, scale * 4, partLifetime);
                GeneralParticleHandler.SpawnParticle(blastRing2);
                for (int k = 0; k < 15; k++)
                {
                    Vector2 velocity = new Vector2(15, 15).RotatedByRandom(100) * Main.rand.NextFloat(0.3f, 1.2f);
                    Particle smoke = new HeavySmokeParticle(Projectile.Center + velocity, velocity, partColor, 45, Main.rand.NextFloat(0.9f, 2.3f), 0.7f);
                    GeneralParticleHandler.SpawnParticle(smoke);
                }
                for (int k = 0; k < 30; k++)
                {
                    Vector2 velocity = new Vector2(10, 10).RotatedByRandom(100) * Main.rand.NextFloat(0.3f, 1.2f);
                    Dust dust2 = Dust.NewDustPerfect(Projectile.Center + velocity, Main.rand.NextBool() ? 39 : 298, velocity);
                    dust2.scale = Main.rand.NextFloat(1.15f, 1.45f);
                    dust2.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D vortexTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SoulVortex").Value;
            for (int i = 0; i < 4; i++)
            {
                float angle = MathHelper.TwoPi * i / 3f + Main.GlobalTimeWrappedHourly * MathHelper.TwoPi;
                Color outerColor = Color.Lerp(Color.Purple, Color.MediumOrchid, i * 0.2f);
                Color drawColor = outerColor;
                drawColor.A = 0;
                Vector2 drawPosition = Projectile.Center - Main.screenPosition;

                Main.EntitySpriteDraw(vortexTexture, drawPosition, null, drawColor * Projectile.Opacity, -angle + MathHelper.PiOver2, vortexTexture.Size() * 0.5f, ((Projectile.scale * (1 - i * 0.07f)) * 0.18f) * Utils.GetLerpValue(0, 25, time, true), SpriteEffects.None, 0);
            }

            Texture2D rechargeTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float randSize = Main.rand.NextFloat(0.8f, 1.2f);
            Color drawColor2 = Projectile.ai[1] == 0 ? Color.OrangeRed : Projectile.ai[1] == 1 ? Color.Cyan : Color.GreenYellow;
            Main.EntitySpriteDraw(rechargeTexture, Projectile.Center - Main.screenPosition, null, drawColor2 with { A = 0 }, Projectile.rotation, rechargeTexture.Size() * 0.5f, 0.25f * Utils.GetLerpValue(0, 25, time, true) * randSize, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(rechargeTexture, Projectile.Center - Main.screenPosition, null, Color.White with { A = 0 }, Projectile.rotation, rechargeTexture.Size() * 0.5f, 0.1f * Utils.GetLerpValue(0, 25, time, true) * randSize, SpriteEffects.None, 0);

            //CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, 75, targetHitbox);
    }
}
