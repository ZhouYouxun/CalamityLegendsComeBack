using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // Straight-flying companion projectile spawned with torpedoes in stages 3-4.
    // Unlike AcidRocket it doesn't home; it leaves a toxic cloud on death.
    internal sealed class SeasSearingPollutionRocket : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/AcidRocket";

        private static readonly int TrailLength = 14;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type]     = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width          = 12;
            Projectile.height         = 12;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.penetrate      = 2;
            Projectile.timeLeft       = 320;
            Projectile.tileCollide    = true;
            Projectile.ignoreWater    = true;
            Projectile.ArmorPenetration = 14;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 12;
        }

        public override void AI()
        {
            Projectile.rotation  = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity += Vector2.UnitY * 0.08f;

            Lighting.AddLight(Projectile.Center, SeasSearingPalette.ToxicGreen.ToVector3() * 0.35f);

            if (!Main.dedServ && Main.GameUpdateCount % 2 == 0)
            {
                Dust trail = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity * 0.5f,
                    DustID.GemEmerald,
                    Projectile.velocity * -Main.rand.NextFloat(0.18f, 0.44f),
                    115, Color.Lerp(SeasSearingPalette.ToxicGreen, SeasSearingPalette.RadioactiveCyan, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.45f, 0.85f));
                trail.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SeasSearingPollutionNPC pollution = target.GetGlobalNPC<SeasSearingPollutionNPC>();
            pollution.ApplyPollution(target, Projectile.owner, 5, 12 * 60);
            target.AddBuff(BuffID.Venom, 240);

            if (Main.myPlayer == Projectile.owner)
                SeasSearingVisualUtility.SpawnAbyssDust(target.Center, 8, 2.2f, 16f);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = 0.28f, PitchVariance = 0.1f }, Projectile.Center);
            SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 16, 3.2f, 24f, 1.1f);
            SeasSearingVisualUtility.SpawnPressureRing(Projectile.Center, 2.5f, 18f, 14, SeasSearingPalette.ToxicGreen);

            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<SeasSearingFalloutCloud>(),
                    Math.Max(1, Projectile.damage / 2), 0f, Projectile.owner, 8f);
            }
        }
    }
}
