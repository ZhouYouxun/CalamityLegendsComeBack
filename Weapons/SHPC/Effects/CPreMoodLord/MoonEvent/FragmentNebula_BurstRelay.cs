using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    internal class FragmentNebula_BurstRelay : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.timeLeft = 22;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (Projectile.owner == Main.myPlayer && ((int)Timer == 0 || (int)Timer == 5 || (int)Timer == 10))
                FireBurstShot((int)(Timer / 5f));

            SpawnRelayDust();
            Timer++;
        }

        private void FireBurstShot(int shotIndex)
        {
            Vector2 forward = new Vector2(Projectile.ai[0], Projectile.ai[1]).SafeNormalize(Vector2.UnitX);
            float angleOffset = MathHelper.ToRadians((shotIndex - 1) * 5f);
            bool isBlueVariant = shotIndex == 2;
            Vector2 direction = forward.RotatedBy(angleOffset).SafeNormalize(forward);
            float speed = isBlueVariant ? 17.8f : 17.2f;
            int damage = isBlueVariant ? Projectile.damage * 3 : Projectile.damage;
            float homingDelayFrames = isBlueVariant ? 24f : 12f;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + forward * 8f,
                direction * speed,
                ModContent.ProjectileType<FragmentNebula_Star>(),
                Math.Max(1, damage),
                Projectile.knockBack,
                Projectile.owner,
                isBlueVariant ? 1f : 0f,
                homingDelayFrames,
                shotIndex);
        }

        private void SpawnRelayDust()
        {
            Vector2 forward = new Vector2(Projectile.ai[0], Projectile.ai[1]).SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + normal * Main.rand.NextFloat(-8f, 8f),
                    Main.rand.NextBool() ? DustID.PurpleCrystalShard : DustID.BlueTorch,
                    -forward * Main.rand.NextFloat(0.6f, 1.8f) + normal * Main.rand.NextFloat(-0.4f, 0.4f),
                    0,
                    Main.rand.NextBool() ? new Color(185, 72, 255) : new Color(72, 196, 255),
                    Main.rand.NextFloat(0.8f, 1.25f));

                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
