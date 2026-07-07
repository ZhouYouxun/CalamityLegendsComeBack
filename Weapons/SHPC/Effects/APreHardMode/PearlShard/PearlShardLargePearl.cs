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
            Projectile.width = 45;
            Projectile.height = 45;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 50;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            // 挂一个 0.985 倍的减速
            Projectile.velocity *= 0.96f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Vector3(0.38f, 0.24f, 0.32f));

            if (Main.rand.NextFloat() < 0.58f)
                PearlShardVisuals.SpawnPearlParticle(Projectile.Center, -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.18f), 0.34f, 18);

            PearlShardVisuals.SpawnPearlGodTrail(Projectile, 1f);
        }

        public override bool? CanDamage()
        {
            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            PlayBreakSound(Projectile.Center);
            PearlShardVisuals.SpawnBurst(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitY), 1f);

            if (Projectile.owner != Main.myPlayer)
                return;

            float baseRotation = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = (baseRotation + MathHelper.TwoPi * i / 3f).ToRotationVector2() * Main.rand.NextFloat(7.5f, 9.5f);
                int small = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    velocity,
                    ModContent.ProjectileType<PearlShardSmallPearl>(),
                    (int)(Projectile.damage * 0.30),
                    Projectile.knockBack,
                    Projectile.owner,
                    Main.rand.NextFloat(MathHelper.TwoPi));

                if (Main.projectile.IndexInRange(small))
                {
                    Main.projectile[small].CritChance = Projectile.CritChance;
                    Main.projectile[small].netUpdate = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            PearlShardVisuals.DrawPearl(Projectile, 1.33f);
            return false;
        }

        internal static void PlayBreakSound(Vector2 position, float volumeScale = 1f)
        {
            SoundEngine.PlaySound(SoundID.Item27 with
            {
                Volume = 0.55f * volumeScale,
                Pitch = 0.15f,
                MaxInstances = 6
            }, position);
        }
    }
}
