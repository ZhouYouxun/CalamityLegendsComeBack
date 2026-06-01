using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode.PearlShard
{
    public class PearlShardLargePearl : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/APreHardMode/PearlShard/PearlShardParticle";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 25;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 2;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Vector3(0.38f, 0.24f, 0.32f));

            if (Main.rand.NextBool(3))
                PearlShardVisuals.SpawnPearlParticle(Projectile.Center, -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.18f), 0.34f, 18);

            PearlShardVisuals.SpawnPearlGodTrail(Projectile, 1f);
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.55f, Pitch = 0.15f }, Projectile.Center);
            PearlShardVisuals.SpawnBurst(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitY), 1f);

            if (Projectile.owner != Main.myPlayer)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Main.rand.NextVector2Unit());
            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = forward.RotatedBy(MathHelper.ToRadians(-18f + 18f * i)) * Main.rand.NextFloat(7.5f, 9.5f);
                int small = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    velocity,
                    ModContent.ProjectileType<PearlShardSmallPearl>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    Main.rand.NextFloat(MathHelper.TwoPi));

                if (Main.projectile.IndexInRange(small))
                    Main.projectile[small].CritChance = Projectile.CritChance;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            PearlShardVisuals.DrawPearl(Projectile, 1f);
            return false;
        }
    }
}
