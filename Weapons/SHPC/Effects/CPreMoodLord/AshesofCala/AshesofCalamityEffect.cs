using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.AshesofCala
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
        public override bool EnableDefaultSlowdown => false;
        public override bool PlayDefaultLeftClickFireSound => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.GetGlobalProjectile<AshesofCalamity_GP>().firstFrame = true;
            projectile.timeLeft = 2;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.friendly = false;
            projectile.hide = true;
        }

        public override bool? CanDamage(Projectile projectile, Player owner) => false;

        public override void AI(Projectile projectile, Player owner)
        {
            AshesofCalamity_GP gp = projectile.GetGlobalProjectile<AshesofCalamity_GP>();
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
            float speed = System.Math.Max(projectile.velocity.Length(), 18f);

            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center + forward * 12f,
                forward * 6f,
                ModContent.ProjectileType<AshesofCalamity_SoulRelay>(),
                (int)(projectile.damage * 1.00),
                projectile.knockBack,
                projectile.owner,
                forward.X,
                forward.Y
            );
        }
    }

    public class AshesofCalamity_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool firstFrame;
    }
}
