using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.PlagueCell
{
    public class PlagueCellEffect : DefaultEffect
    {
        public override int EffectID => 18;
        public override int AmmoType => ModContent.ItemType<PlagueCellCanister>();

        public override Color ThemeColor => new(110, 20, 20);
        public override Color StartColor => new(255, 70, 50);
        public override Color EndColor => new(80, 0, 0);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override float GlowIntensityFactor => 0f;
        public override bool EnableDefaultSlowdown => false;
        public override bool PlayDefaultLeftClickFireSound => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            PlagueCell_GP gp = projectile.GetGlobalProjectile<PlagueCell_GP>();
            gp.firstFrame = true;

            projectile.timeLeft = 2;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.friendly = false;
            projectile.hide = true;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            PlagueCell_GP gp = projectile.GetGlobalProjectile<PlagueCell_GP>();
            if (!gp.firstFrame)
                return;

            gp.firstFrame = false;
            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center + direction * 18f,
                direction,
                ModContent.ProjectileType<PlagueCell_Lazer>(),
                projectile.damage,
                projectile.knockBack,
                owner.whoAmI,
                projectile.ai[0],
                projectile.ai[1],
                projectile.ai[2]);
        }
    }

    public class PlagueCell_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool firstFrame;
    }
}
