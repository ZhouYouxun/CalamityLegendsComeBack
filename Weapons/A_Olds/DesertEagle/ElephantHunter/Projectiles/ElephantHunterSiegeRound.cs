using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.DesertEagle.ElephantHunter.Projectiles
{
    internal sealed class ElephantHunterSiegeRound : ModProjectile
    {
        private static readonly Color SiegeColor = new(255, 133, 62);

        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/DesertEagle/HandheldTankShell";

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 720;
            Projectile.extraUpdates = 5;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, SiegeColor.ToVector3() * 0.7f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.1f, Main.rand.NextBool() ? DustID.Torch : DustID.Smoke, -Projectile.velocity * 0.07f + Main.rand.NextVector2Circular(1.2f, 1.2f), 110, SiegeColor, 1.2f);
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => SpawnSiegeImpact(1f);

        public override void OnKill(int timeLeft)
        {
            SpawnSiegeImpact(1.55f);
            Vector2 impactCenter = Projectile.Center;
            Projectile.Resize(144, 144);
            Projectile.Center = impactCenter;
            Projectile.Damage();
        }

        private void SpawnSiegeImpact(float scale)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f, Pitch = -0.15f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.55f, Pitch = -0.18f }, Projectile.Center);

            for (int i = 0; i < 24; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, i % 3 == 0 ? DustID.Smoke : DustID.Torch, Main.rand.NextVector2Circular(8f, 8f) * scale, 95, SiegeColor, Main.rand.NextFloat(1f, 1.4f) * scale);
                dust.noGravity = true;
            }
        }
    }
}
