using CalamityMod.Graphics.Metaballs;
using CalamityMod.Items.Weapons.Melee;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC.TwistingNether的参考文献
{
    public class DeathsAscensionRift : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = DeathsAscension.RiftLifeTime;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        public override void AI()
        {
            // Metaball visuals
            int ballAmt = 20;
            for (int i = 0; i < ballAmt; i++)
            {
                // How far balls are between each other
                float offset = i * 5;
                // Allows the rift to be rotated 
                Vector2 rotationOffset = Vector2.UnitY.RotatedBy(Projectile.rotation) * i;
                // How random the sizing can get. Randomness is decreased as the balls get smaller
                float sizeRandomness = MathHelper.Lerp(10, 0, (float)(i / (float)ballAmt));
                // How random the positioning can get
                float positionRandomness = 20;
                // Size decreases with each iteration as the balls are spawned further out. They also have some slight bubbly randomness and will scale in while the rift is spawning
                float scale = (MathHelper.Lerp(90, 10, (float)(i / (float)ballAmt)) + Main.rand.NextFloat(-sizeRandomness, sizeRandomness)) * Utils.GetLerpValue(DeathsAscension.RiftLifeTime, DeathsAscension.RiftLifeTime - 10, Projectile.timeLeft, true);
                // Spawn the metaballs. The first moves downwards while the other moves upwards
                StreamGougeMetaball.SpawnParticle(Projectile.Center + Vector2.UnitY * (offset + Main.rand.NextFloat(-positionRandomness, positionRandomness)) + rotationOffset , Vector2.Zero, scale);
                StreamGougeMetaball.SpawnParticle(Projectile.Center + Vector2.UnitY * -(offset + Main.rand.NextFloat(-positionRandomness, positionRandomness)) - rotationOffset, Vector2.Zero, scale);
            }

            // Spawn orbital scythes on spawn. These last until the rift dies.
            if (Projectile.ai[2] == 0)
            {
                int scytheAmt = DeathsAscension.RiftOrbitalAmount;
                float speed = 30;
                for (int i = 0; i < scytheAmt; i++)
                {
                    Vector2 scytheVelocity = Vector2.UnitY.RotatedBy(MathHelper.Lerp(0, 3 * MathHelper.PiOver2, (float)(i / (float)(scytheAmt - 1))) + Projectile.ai[1]) * speed;
                    if (Projectile.owner == Main.myPlayer)
                    {
                        int p = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, scytheVelocity, ModContent.ProjectileType<DeathsAscensionProjectile>(), (int)(Projectile.damage * DeathsAscension.OrbitalScytheDamageMult), Projectile.knockBack, Projectile.owner, ai2: 1);
                        
                        Main.projectile[p].penetrate = -1;
                        Main.projectile[p].timeLeft = DeathsAscension.RiftLifeTime;
                    }
                }
                Projectile.ai[2] = 1;
            }

            // When the weapon is swung fire scythes from the rift
            if (Projectile.ai[0] == 10)
            {
                SoundEngine.PlaySound(SoundID.Item104 with { Pitch = 0.4f }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item71 with { Pitch = -0.4f }, Projectile.Center);

                // 14NOV2024: Ozzatron: clamped mouse position unnecessary, only used for direction
                Vector2 direction = Projectile.Center.DirectionTo(Main.MouseWorld) * 12;
                int spreadfactor = 9;
                for (int index = 0; index < DeathsAscension.ScytheShotAmount; ++index)
                {
                    float SpeedX = direction.X + Main.rand.NextFloat(-spreadfactor, spreadfactor + 1);
                    float SpeedY = direction.Y + Main.rand.NextFloat(-spreadfactor, spreadfactor + 1); 
                    if (Projectile.owner == Main.myPlayer)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, SpeedX, SpeedY, ModContent.ProjectileType<DeathsAscensionProjectile>(), (int)(Projectile.damage * DeathsAscension.RiftScytheDamageMult), Projectile.knockBack, Projectile.owner);
                    }
                }
            }
            
            // Controls the rotational offset of orbital scythes
            Projectile.ai[1] += 0.1f;

            // Decrement scythe cooldown timer
            if (Projectile.ai[0] > 0)
            {
                Projectile.ai[0]--;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(CalPlayer.CalamityPlayer.DrownSound, Projectile.Center);
        }
    }
}
