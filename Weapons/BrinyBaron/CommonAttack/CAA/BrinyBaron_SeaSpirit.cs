using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal class BrinyBaron_SeaSpirit : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public int Time = 0;
        public int randTimer;
        private Vector2 initialVelocity;
        private bool hasInitialVelocity = false;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = BB_Balance.GetLeftProjectileHitCooldown(BBLeftProjectile.SeaSpirit);
        }

        public override bool? CanDamage()
        {
            if (Time < 10)
                return false;
            return null;
        }

        public override void AI()
        {
            Time++;
            if (Time == 1)
            {
                randTimer = Main.rand.Next(200, 261);
                Projectile.timeLeft = randTimer;
            }

            if (!hasInitialVelocity)
            {
                initialVelocity = Projectile.velocity;
                hasInitialVelocity = true;
            }

            int growthStage = (int)Projectile.ai[0];
            bool isSinWave = Projectile.ai[1] == 1f;

            if (isSinWave && Time <= 20)
            {
                float waveSpeed = initialVelocity.Length();
                Vector2 perp = initialVelocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
                Projectile.velocity = initialVelocity + perp * MathF.Cos(Time * 0.5f) * waveSpeed * 0.6f;
            }
            else
            {
                // Homing behavior
                if (growthStage == 1)
                {
                    // Weak homing, starts after 20 frames like DepthCrusher
                    if (Time > 20 && Time < (randTimer - 70))
                    {
                        CalamityUtils.HomeInOnNPC(Projectile, true, 384f, MathHelper.Clamp(1f + Time * 0.12f, 1f, 11f), 20f);
                    }
                    else if (Time >= (randTimer - 70))
                    {
                        if (Projectile.velocity.Y < 10)
                            Projectile.velocity.Y += 0.4f;
                        Projectile.velocity.X *= 0.97f;
                    }
                }
                else if (growthStage == 2)
                {
                    // Stronger homing
                    if (Time > 15 && Time < (randTimer - 50))
                    {
                        CalamityUtils.HomeInOnNPC(Projectile, true, 500f, MathHelper.Clamp(1.5f + Time * 0.15f, 1.5f, 16f), 30f);
                    }
                    else if (Time >= (randTimer - 50))
                    {
                        if (Projectile.velocity.Y < 10)
                            Projectile.velocity.Y += 0.4f;
                        Projectile.velocity.X *= 0.97f;
                    }
                }
                else // Stage 3 or 4/5
                {
                    float homingVelocity = isSinWave ? 24f : 18f;
                    float homingAcceleration = isSinWave ? 40f : 35f;
                    int startFrame = isSinWave ? 20 : 12;

                    if (Time > startFrame && Time < (randTimer - 40))
                    {
                        CalamityUtils.HomeInOnNPC(Projectile, true, 800f, homingVelocity, homingAcceleration);
                    }
                    else if (Time >= (randTimer - 40))
                    {
                        if (Projectile.velocity.Y < 10)
                            Projectile.velocity.Y += 0.4f;
                        Projectile.velocity.X *= 0.97f;
                    }
                }
            }

            // Visual effects
            Color mainColor = Color.Lerp(Color.DeepSkyBlue, Color.Cyan, 0.5f);
            
            // Add lighting
            Lighting.AddLight(Projectile.Center, mainColor.R / 255f * 0.4f, mainColor.G / 255f * 0.4f, mainColor.B / 255f * 0.4f);

            // Spawn smoke particles
            if (Time % 2 == 0)
            {
                Particle smoke = new HeavySmokeParticle(
                    Projectile.Center, 
                    Projectile.velocity * Main.rand.NextFloat(-0.2f, -0.6f), 
                    mainColor, 
                    30, 
                    Main.rand.NextFloat(0.35f, 0.5f), 
                    0.3f, 
                    Main.rand.NextFloat(-0.2f, 0.2f), 
                    false, 
                    required: true);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            // Spawn dust
            for (int i = 0; i < 3; i++)
            {
                Vector2 dustPos = Projectile.Center;
                int dustType = Main.rand.NextBool(3) ? DustID.Frost : DustID.Water;
                Dust dust = Dust.NewDustPerfect(dustPos, dustType);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.8f, 1.5f);
                dust.velocity = new Vector2(0.5f, 0.5f).RotatedByRandom(100) * Main.rand.NextFloat(0.2f, 1.1f);
            }

            // Glow effects in later stages
            if (growthStage >= 2 && Time % 4 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    Main.rand.NextVector2Circular(1.5f, 1.5f),
                    false,
                    8,
                    Main.rand.NextFloat(0.12f, 0.25f),
                    Color.Cyan,
                    true,
                    false,
                    true));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<RiptideDebuff>(), 180);
            SoundEngine.PlaySound(SoundID.ShimmerWeak1 with { Pitch = 0.35f }, Projectile.Center);
        }
    }
}
