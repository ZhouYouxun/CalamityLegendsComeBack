using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC
{
    public class TwistingNetherEffect : DefaultEffect
    {
        public override int EffectID => 33;
        public override int AmmoType => ModContent.ItemType<TwistingNether>();

        public override Color ThemeColor => new(30, 0, 40);
        public override Color StartColor => new(120, 40, 160);
        public override Color EndColor => new(5, 0, 10);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override bool EnableDefaultSlowdown => false;
        public override bool PlayDefaultLeftClickFireSound => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.penetrate = -1;
            projectile.timeLeft = 2;
            projectile.velocity *= 1.6f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (projectile.owner == Main.myPlayer)
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/HellbornImpact"), projectile.Center);

            if (projectile.owner != Main.myPlayer)
                return;

            Vector2 baseVelocity = projectile.velocity.SafeNormalize(Vector2.UnitX) * MathHelper.Clamp(projectile.velocity.Length(), 14f, 18f);
            Vector2 side = baseVelocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 3; i++)
            {
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + side * ((i - 1) * 28f),
                    baseVelocity,
                    ModContent.ProjectileType<TwistingNether_Blade>(),
                    (int)(projectile.damage * 0.44),
                    projectile.knockBack,
                    projectile.owner
                );
            }
        }
    }
}
