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

            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            float[] angleOffsets =
            {
                MathHelper.ToRadians(-10f),
                0f,
                MathHelper.ToRadians(10f)
            };

            for (int i = 0; i < angleOffsets.Length; i++)
            {
                bool isBlueVariant = Main.rand.NextBool(5);
                Vector2 direction = forward.RotatedBy(angleOffsets[i]).SafeNormalize(forward);
                Vector2 spawnPosition = projectile.Center + forward * 10f + normal * (i - 1f) * 8f;
                float speed = isBlueVariant ? 13.8f : 15.6f;
                int damage = isBlueVariant ? projectile.damage * 3 : projectile.damage;
                float homingDelayFrames = isBlueVariant ? 46f : 24f;

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    spawnPosition,
                    direction * speed,
                    ModContent.ProjectileType<FragmentNebula_Star>(),
                    damage,
                    projectile.knockBack,
                    owner.whoAmI,
                    isBlueVariant ? 1f : 0f,
                    homingDelayFrames,
                    i);
            }
        }
    }
}
