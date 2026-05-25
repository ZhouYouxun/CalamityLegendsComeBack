using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera.Essence
{
    public class EssenceofHavocEffect : DefaultEffect
    {
        public override int EffectID => 5;

        public override int AmmoType => ModContent.ItemType<EssenceofHavoc>();

        public override Color ThemeColor => new Color(255, 110, 40);
        public override Color StartColor => new Color(255, 180, 80);
        public override Color EndColor => new Color(200, 60, 20);

        public override float SquishyLightParticleFactor => 1.35f;
        public override float ExplosionPulseFactor => 1.35f;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.velocity *= 1.15f;
            projectile.timeLeft = 300;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.velocity.Y = MathHelper.Min(projectile.velocity.Y + 0.34f, 18f);
            projectile.rotation = projectile.velocity.ToRotation();
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (projectile.velocity.Y > 0f)
                modifiers.SourceDamage *= 1.5f;
            else if (projectile.velocity.Y < 0f)
                modifiers.SourceDamage *= 0.75f;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            bool movingDownward = projectile.velocity.Y > 0f;

            Vector2 dirX = Vector2.UnitX;
            Vector2 dirY = Vector2.UnitY;

            int layers = 10;
            float baseSpeed = 6f;

            for (int i = 0; i < layers; i++)
            {
                float speed = baseSpeed + i * 1.8f;
                float scale = (0.8f + i * 0.08f) * SquishyLightParticleFactor;
                Color color = Color.Lerp(ThemeColor, Color.White, i / (float)layers);
                int life = 28 + i * 2;

                Vector2[] dirs =
                {
                    dirX,
                    -dirX,
                    dirY,
                    -dirY
                };

                foreach (Vector2 dir in dirs)
                {
                    SquishyLightParticle particle = new(
                        projectile.Center,
                        dir * speed,
                        scale,
                        color,
                        life
                    );

                    GeneralParticleHandler.SpawnParticle(particle);
                }
            }

            Vector2[] invDirs =
            {
                Vector2.UnitX,
                -Vector2.UnitX,
                Vector2.UnitY,
                -Vector2.UnitY
            };

            float[] speeds = { 6f, 10f, 14f };

            foreach (Vector2 dir in invDirs)
            {
                foreach (float speed in speeds)
                {
                    Projectile.NewProjectile(
                        projectile.GetSource_FromThis(),
                        projectile.Center,
                        dir * speed,
                        ModContent.ProjectileType<EssenceofHavoc_INV>(),
                        (int)(projectile.damage * (movingDownward ? 0.9f : 0.5f)),
                        projectile.knockBack,
                        projectile.owner
                    );
                }
            }
        }
    }
}
