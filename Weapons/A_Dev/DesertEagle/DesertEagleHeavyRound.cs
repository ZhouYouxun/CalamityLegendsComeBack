using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle
{
    internal sealed class DesertEagleHeavyRound : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/DesertEagle/HandheldTankShell";

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 14;
            Projectile.timeLeft = 1500;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            DrawOffsetX = -8;
            DrawOriginOffsetX = -2;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.localAI[0] += 1f;

            DesertEagleEffects.SpawnBulletTrail(Projectile.Center, Projectile.velocity, 1.25f, true);

            if (Projectile.localAI[0] < 10f && !Main.dedServ)
            {
                for (int i = 0; i < 2; i++)
                    DesertEagleEffects.SpawnHeavySmoke(Projectile.Center - Projectile.velocity * i * 0.15f, -Projectile.velocity * 0.08f, 0.9f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            DesertEagleEffects.SpawnSilverImpact(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), 1.65f, true);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.9f, Pitch = -0.2f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = 0.12f }, Projectile.Center);

            DesertEagleEffects.SpawnSilverImpact(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), 2f, true);

            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 220;
            Projectile.Center = Projectile.position;
            Projectile.position -= new Vector2(Projectile.width / 2f, Projectile.height / 2f);
            Projectile.Damage();

            for (int i = 0; i < 8; i++)
                DesertEagleEffects.SpawnHeavySmoke(Projectile.Center + Main.rand.NextVector2Circular(18f, 18f), Main.rand.NextVector2Circular(1.6f, 1.6f) + Vector2.UnitY * -Main.rand.NextFloat(0.5f, 1.8f), 1.1f);
        }
    }
}
