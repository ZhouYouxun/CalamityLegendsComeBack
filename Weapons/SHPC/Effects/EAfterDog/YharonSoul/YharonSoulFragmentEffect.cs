using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.YharonSoul
{
    public class YharonSoulFragmentEffect : DefaultEffect
    {
        public override int EffectID => 37;

        public override int AmmoType => ModContent.ItemType<YharonSoulFragment>();

        public override Color ThemeColor => new Color(255, 140, 40);
        public override Color StartColor => new Color(255, 200, 80);
        public override Color EndColor => new Color(120, 30, 0);
        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.Kill();
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitX);

            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                direction * 12f,
                ModContent.ProjectileType<YharonSoulFragment_Flame>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner
            );
        }
    }
}