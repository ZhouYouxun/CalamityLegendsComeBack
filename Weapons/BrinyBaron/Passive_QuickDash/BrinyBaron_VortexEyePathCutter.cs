using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.Passive_QuickDash
{
    // Tiny high-speed cutting lights left across each teleport line. They are intentionally
    // compact: the path reads as a rapid sequence of cuts instead of a continuous beam.
    internal sealed class BrinyBaron_VortexEyePathCutter : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 13;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.04f, 0.22f, 0.32f);

            if (Main.dedServ || Projectile.timeLeft % 2 != 0)
                return;

            Color color = Color.Lerp(Color.DodgerBlue, Color.Cyan, Main.rand.NextFloat());
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                Projectile.Center, -Projectile.velocity * 0.025f, false, 10,
                Main.rand.NextFloat(0.14f, 0.25f), color, true, false, true));
            GeneralParticleHandler.SpawnParticle(new SparkParticle(
                Projectile.Center, Projectile.velocity.RotatedByRandom(0.22f) * 0.12f,
                true, 9, 0.33f, color, true));

            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch, -Projectile.velocity * 0.035f, 90, color, 0.9f);
            dust.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
