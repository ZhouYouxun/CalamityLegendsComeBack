using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class BossSoulofMightEffect : DefaultEffect
    {
        public override int EffectID => 13;

        public override int AmmoType => ItemID.SoulofMight;

        public override Color ThemeColor => new Color(70, 110, 255);   // 力量核心蓝（高能电蓝）
        public override Color StartColor => new Color(150, 190, 255);  // 高亮（发光边缘）
        public override Color EndColor => new Color(20, 40, 120);      // 深蓝（阴影/核心）

        public override float SquishyLightParticleFactor => 0.01f;
        public override float ExplosionPulseFactor => 0.01f;
        public override bool EnableDefaultSlowdown => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.Kill();
        }

        public override void AI(Projectile projectile, Player owner)
        {

        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {

        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {

        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 baseVelocity = projectile.velocity.SafeNormalize(Vector2.UnitX) * 12f;

            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity;

                // ===== 你指定的随机扩散 =====
                {
                    float speedX = baseVelocity.X + Main.rand.Next(-20, 21) * 0.05f;
                    float speedY = baseVelocity.Y + Main.rand.Next(-20, 21) * 0.05f;

                    velocity = new Vector2(speedX, speedY);
                }

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    velocity,
                    ModContent.ProjectileType<BossSoulofMight_Ball>(),
                    (int)(projectile.damage * 0.5f), // 50%伤害
                    projectile.knockBack,
                    projectile.owner
                );
            }
        }













    }
}