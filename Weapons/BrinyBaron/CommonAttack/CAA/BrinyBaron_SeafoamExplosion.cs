using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal class BrinyBaron_SeafoamExplosion : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                SoundEngine.PlaySound(SoundID.Item21 with { Volume = 0.6f, Pitch = 0.2f }, Projectile.Center);

                if (!Main.dedServ)
                {
                    Color waterColor = new Color(90, 205, 255);
                    // Pulse ring
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                        Projectile.Center,
                        Vector2.Zero,
                        waterColor * 0.75f,
                        Vector2.One * 0.2f,
                        0f,
                        0.05f,
                        0.45f,
                        14
                    ));

                    // Denser water foams
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                        GeneralParticleHandler.SpawnParticle(new WaterFoamParticle(
                            Projectile.Center,
                            vel,
                            Main.rand.Next(15, 25),
                            Main.rand.NextFloat(0.4f, 0.7f),
                            Color.Lerp(waterColor, Color.White, 0.3f)
                        ));
                    }
                }
            }
        }
    }
}
