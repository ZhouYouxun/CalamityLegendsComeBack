using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle
{
    internal sealed class DesertEagleSilverGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool SilverMarked;
        private bool impactHandled;

        public override void AI(Projectile projectile)
        {
            if (!SilverMarked || !projectile.friendly || projectile.damage <= 0 || projectile.velocity.LengthSquared() <= 1f)
                return;

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    DustID.SilverCoin,
                    -projectile.velocity * Main.rand.NextFloat(0.02f, 0.08f) + Main.rand.NextVector2Circular(0.6f, 0.6f),
                    100,
                    Color.Lerp(DesertEagleEffects.SilverMain, DesertEagleEffects.SilverAccent, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.65f, 1f));
                dust.noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    projectile.Center,
                    -projectile.velocity * Main.rand.NextFloat(0.018f, 0.04f),
                    "CalamityMod/Particles/BloomLineSoftEdge",
                    false,
                    Main.rand.Next(8, 11),
                    Main.rand.NextFloat(0.016f, 0.024f),
                    Color.Lerp(DesertEagleEffects.SilverMain, DesertEagleEffects.SilverAccent, Main.rand.NextFloat()),
                    new Vector2(Main.rand.NextFloat(0.9f, 1.25f), Main.rand.NextFloat(0.38f, 0.6f)),
                    shrinkSpeed: 0.8f));
            }
        }

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!SilverMarked)
                return;

            modifiers.ArmorPenetration += 12f;
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!SilverMarked || impactHandled)
                return;

            impactHandled = true;
            DesertEagleEffects.SpawnSilverImpact(projectile.Center, projectile.velocity.SafeNormalize(Vector2.UnitX), 0.85f);
        }

        public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
        {
            if (!SilverMarked || impactHandled)
                return base.OnTileCollide(projectile, oldVelocity);

            impactHandled = true;
            DesertEagleEffects.SpawnSilverImpact(projectile.Center, oldVelocity.SafeNormalize(Vector2.UnitX), 0.8f);
            return base.OnTileCollide(projectile, oldVelocity);
        }
    }
}
