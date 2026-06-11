using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentEntropyEffect : DefaultEffect
    {
        public override int EffectID => 25;
        public override int AmmoType => ModContent.ItemType<MeldBlob>();

        public override Color ThemeColor => new Color(6, 6, 6);
        public override Color StartColor => new Color(20, 20, 20);
        public override Color EndColor => new Color(0, 0, 0);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override float GlowIntensityFactor => 0f;
        public override bool EnableDefaultSlowdown => false;
        public override bool PlayDefaultLeftClickFireSound => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.GetGlobalProjectile<FragmentEntropy_GP>().firstFrame = true;
            projectile.timeLeft = 2;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.friendly = false;
            projectile.hide = true;
        }

        public override bool? CanDamage(Projectile projectile, Player owner) => false;

        public override void AI(Projectile projectile, Player owner)
        {
            FragmentEntropy_GP gp = projectile.GetGlobalProjectile<FragmentEntropy_GP>();
            if (!gp.firstFrame)
                return;

            gp.firstFrame = false;
            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(owner.direction == 0 ? Vector2.UnitX : new Vector2(owner.direction, 0f));
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center + forward * 10f,
                forward * 37.2f,
                ModContent.ProjectileType<FragmentEntropy_CosmicFire>(),
                (int)(projectile.damage * 1.8f),
                projectile.knockBack,
                projectile.owner);
        }
    }

    public class FragmentEntropy_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool firstFrame;
    }
}
