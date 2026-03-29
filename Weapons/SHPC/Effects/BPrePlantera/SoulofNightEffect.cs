using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class SoulofNightEffect : DefaultEffect
    {
        public override int EffectID => 10;
        public override int AmmoType => ItemID.SoulofNight;

        // ===== 暗紫色 =====
        public override Color ThemeColor => new Color(90, 0, 120);
        public override Color StartColor => new Color(140, 40, 180);
        public override Color EndColor => new Color(40, 0, 60);

        public override float SquishyLightParticleFactor => 1.1f;
        public override float ExplosionPulseFactor => 1.1f;
        public override bool EnableDefaultSlowdown => false;

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            // ===== 箭头形6发 =====
            for (int i = 0; i < 6; i++)
            {
                float offset = (i - 2.5f) * 0.25f;

                Vector2 dir = forward.RotatedBy(offset);

                // 中间更快，两侧更慢
                float speed = MathHelper.Lerp(16f, 10f, Math.Abs(offset) / 0.625f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    dir * speed,
                    ModContent.ProjectileType<NewSHPS>(),
                    projectile.damage,
                    projectile.knockBack,
                    projectile.owner,
                    2 // preset1
                );
            }
        }

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.Kill();
        }
        public override void AI(Projectile projectile, Player owner) { }
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers) { }
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone) { }
    }
}