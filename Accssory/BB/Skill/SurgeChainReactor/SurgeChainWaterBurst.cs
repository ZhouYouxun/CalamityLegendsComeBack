using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.SurgeChainReactor
{
    public class SurgeChainWaterBurst : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.BrinyBaron";

        private const int BurstSize = 75;
        private const int Lifetime = 18;

        public override void SetDefaults()
        {
            Projectile.width = BurstSize;
            Projectile.height = BurstSize;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
            Projectile.alpha = 255;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            EmitBurst(1f);
            SoundEngine.PlaySound(SoundID.Splash with { Volume = 0.65f, Pitch = -0.18f }, Projectile.Center);
        }

        public override void AI()
        {
            float progress = 1f - Projectile.timeLeft / (float)Lifetime;
            if (!Main.dedServ && Main.rand.NextBool(2))
                EmitBurst(MathHelper.Lerp(0.35f, 0.08f, progress));

            Lighting.AddLight(Projectile.Center, new Vector3(0.05f, 0.25f, 0.38f) * (1f - progress));
        }

        private void EmitBurst(float strength)
        {
            int count = strength >= 0.9f ? 34 : 4;
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 offset = direction * Main.rand.NextFloat(4f, BurstSize * 0.46f);
                Vector2 velocity = direction.RotatedByRandom(0.46f) * Main.rand.NextFloat(2.1f, 8.6f) * strength;
                int dustType = Main.rand.NextBool(4) ? DustID.Frost : DustID.Water;

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + offset,
                    dustType,
                    velocity,
                    100,
                    Main.rand.NextBool() ? new Color(50, 165, 255) : new Color(150, 238, 255),
                    Main.rand.NextFloat(0.8f, 1.45f) * MathHelper.Lerp(0.7f, 1.2f, strength));
                dust.noGravity = true;
                dust.alpha = dustType == DustID.Water ? 40 : 0;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Vector2.Distance(Projectile.Center, targetHitbox.Center.ToVector2()) <= BurstSize * 0.5f + targetHitbox.Size().Length() * 0.25f;
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
