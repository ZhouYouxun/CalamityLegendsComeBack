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

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<AshesofCalamity_Portal>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner,
                projectile.velocity.ToRotation() // 记录方向
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