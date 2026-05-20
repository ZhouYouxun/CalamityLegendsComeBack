using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityRangerExpansion.Content.BOWChange;

namespace CalamityRangerExpansion.Content.BOWChange.BPrePlantera.BrimstoneFuryC
{
    internal class BrimstoneFuryReCinderNeedle : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectile.BPrePlantera";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.extraUpdates = 2;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity = Projectile.velocity.RotatedBy((float)System.Math.Sin(Projectile.localAI[0] * 0.08f) * 0.018f) * 1.003f;

            if (Main.rand.NextBool())
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool() ? DustID.Torch : DustID.Flare, -Projectile.velocity * 0.12f, 130, Color.OrangeRed, Main.rand.NextFloat(0.9f, 1.35f));
                d.noGravity = true;
            }

            if (Projectile.localAI[0] % 20f == 1f)
            {
                GeneralParticleHandler.SpawnParticle(new AltSparkParticle(
                    Projectile.Center,
                    -Projectile.velocity.SafeNormalize(Vector2.UnitY) * 0.8f,
                    false,
                    12,
                    1.1f,
                    Color.DarkRed * 0.35f));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 180);
            BowChangeVFX.SpawnImpact(Projectile, BowChangeTheme.Brimstone, 0.75f);
        }

        public override void OnKill(int timeLeft)
        {
            BowChangeVFX.SpawnImpact(Projectile, BowChangeTheme.Brimstone, 0.5f);
        }
    }
}
