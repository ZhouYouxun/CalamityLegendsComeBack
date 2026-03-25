using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using System;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentSolarEffect : DefaultEffect
    {
        public override int EffectID => 21;

        // 先随便占一个，不影响逻辑（你后面会改）
        public override int AmmoType => ItemID.FragmentSolar;

        // ===== Solar主题色 =====
        public override Color ThemeColor => new Color(255, 120, 40);
        public override Color StartColor => new Color(255, 180, 80);
        public override Color EndColor => new Color(180, 60, 20);
        public override float SquishyLightParticleFactor => 1.55f;
        public override float ExplosionPulseFactor => 1.55f;

        // ===== 自定义计时器 =====
        private int shootTimer;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            shootTimer = 0;
            projectile.timeLeft = 50;

        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            shootTimer++;

            // ===== 分界点：21帧 =====
            if (projectile.timeLeft > 21)
            {
                // 抵消减速（持续加速）
                projectile.velocity *= 1.02f;
            }
            else
            {
                // 进入释放阶段：进一步减速
                projectile.velocity *= 0.99f;

                // 每3帧释放一发
                if (shootTimer % 3 == 0)
                {
                    // ===== 7方向散射（Bloodstone风格）=====
                    float[] angles = { -12f, -8f, -4f, 0f, 4f, 8f, 12f };

                    int index = (21 - projectile.timeLeft) / 3;

                    // 防止越界
                    if (index >= 0 && index < angles.Length)
                    {
                        Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

                        Vector2 shootVelocity =
                            forward.RotatedBy(MathHelper.ToRadians(angles[index])) * 16f;

                        Projectile.NewProjectile(
                            projectile.GetSource_FromThis(),
                            projectile.Center,
                            shootVelocity,
                            ModContent.ProjectileType<FragmentSolar_Spear>(), // ⚠ 先占位，你后面换 FragmentSolar_Spear
                            (int)(projectile.damage * 0.7f),
                            projectile.knockBack,
                            projectile.owner
                        );
                    }
                }
            }
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
        }
    }
}