using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentNebulaEffect : DefaultEffect
    {
        public override int EffectID => 23;

        public override int AmmoType => ItemID.FragmentNebula;

        public override Color ThemeColor => new Color(180, 80, 255);
        public override Color StartColor => new Color(220, 140, 255);
        public override Color EndColor => new Color(120, 40, 200);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override bool EnableDefaultSlowdown => false;
        public override bool PlayDefaultLeftClickFireSound => false;
        public override int LeftClickBurstCount => 3;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.penetrate = -1;
            projectile.timeLeft = 2;
            projectile.tileCollide = false;
        }

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
                projectile.Center + forward * 10f,
                Vector2.Zero,
                ModContent.ProjectileType<FragmentNebula_BurstRelay>(),
                (int)(projectile.damage * 1.15),
                projectile.knockBack,
                owner.whoAmI,
                forward.X,
                forward.Y);
        }
    }
}
