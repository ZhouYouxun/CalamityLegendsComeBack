using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ashes
{
    internal class AshesofAnnEffect : DefaultEffect
    {
        public const int AshesofAnnEffectID = 39;

        public override int EffectID => AshesofAnnEffectID;

        public override int AmmoType => ModContent.ItemType<AshesofAnnihilation>();

        public override Color ThemeColor => new(210, 26, 14);
        public override Color StartColor => new(255, 116, 48);
        public override Color EndColor => new(52, 0, 0);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override bool EnableDefaultSlowdown => false;
        public override bool PlayDefaultLeftClickFireSound => false;
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.penetrate = -1;
            projectile.timeLeft = 2;
            projectile.tileCollide = false;
            projectile.hide = true;
            projectile.alpha = 255;
        }

        public override bool? CanDamage(Projectile projectile, Player owner) => false;

        public override void AI(Projectile projectile, Player owner)
        {
            if (projectile.localAI[0] == 1f)
                return;

            projectile.localAI[0] = 1f;
            projectile.Kill();
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (owner.whoAmI != Main.myPlayer)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(owner.direction == 0 ? Vector2.UnitX : new Vector2(owner.direction, 0f));
            if (forward == Vector2.Zero)
                forward = Vector2.UnitX;

            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center + forward * 14f,
                forward * 6f,
                ModContent.ProjectileType<AshesofAnn_BurstRelay>(),
                (int)(projectile.damage * 1.33),
                projectile.knockBack,
                owner.whoAmI,
                forward.X,
                forward.Y);
        }
    }
}
