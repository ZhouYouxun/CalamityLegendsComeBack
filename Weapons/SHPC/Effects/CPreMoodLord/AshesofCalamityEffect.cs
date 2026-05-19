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
            projectile.timeLeft = 90;
            projectile.penetrate = 1;
            projectile.tileCollide = false;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = -1;
            projectile.localAI[0] = 0f;
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.localAI[0] == 1f)
                return;

            projectile.localAI[0] = 1f;

            if (projectile.owner != Main.myPlayer)
                return;

            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<AshesofCalamity_Portal>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner
            );
        }




    }
}
