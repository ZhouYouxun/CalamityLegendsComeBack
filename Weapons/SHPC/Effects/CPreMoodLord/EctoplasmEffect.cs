using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    public class EctoplasmEffect : DefaultEffect
    {
        public override int EffectID => 16;

        // 原版材料
        public override int AmmoType => ItemID.Ectoplasm;

        // ===== 鬼蓝主题 =====
        public override Color ThemeColor => new Color(120, 200, 255);
        public override Color StartColor => new Color(160, 240, 255);
        public override Color EndColor => new Color(80, 140, 220);

        public override float SquishyLightParticleFactor => 1.55f;
        public override float ExplosionPulseFactor => 1.55f;

        // ===== 自定义计时器 =====
        private int timer;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.penetrate = 2;

            // 初速度提升
            projectile.velocity *= 1.25f;

            timer = 0;
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            timer++;

            // ===== 每6帧释放一次 =====
            if (timer % 6 == 0)
            {
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<Ectoplasm_Damage>(),
                    (int)(projectile.damage * 0.5f),
                    0f,
                    projectile.owner
                );
            }

            // ===== 抵消减速 =====
            projectile.velocity *= 1.020408f;
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 8f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    target.Center,
                    velocity,
                    ModContent.ProjectileType<Ectoplasm_Damage>(),
                    (int)(projectile.damage * Main.rand.NextFloat(0.8f, 1.2f)),
                    0f,
                    projectile.owner
                );
            }
        }







    }
}