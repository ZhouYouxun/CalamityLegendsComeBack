using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    internal class AshesofCalamityEffect : DefaultEffect
    {
        public override int EffectID => 19;

        public override int AmmoType => ModContent.ItemType<AshesofCalamity>();

        public override Color ThemeColor => new Color(200, 140, 40);
        public override Color StartColor => new Color(255, 210, 80);
        public override Color EndColor => new Color(40, 25, 10);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override float GlowIntensityFactor => 0f;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.timeLeft = 2;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            Vector2 direction = projectile.velocity.SafeNormalize(owner.direction == 0 ? Vector2.UnitX : new Vector2(owner.direction, 0f));
            float speed = MathHelper.Max(projectile.velocity.Length() * 1.8f, 15.5f);

            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                direction * speed,
                ModContent.ProjectileType<AshesofCalamity_Soul>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner,
                1f
            );
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }




    }
}
