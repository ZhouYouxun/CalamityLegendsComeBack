using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class BossSoulofFrightEffect : DefaultEffect
    {
        public override int EffectID => 12;
        public override int AmmoType => ItemID.SoulofFright;

        // ===== 赤红色 =====
        public override Color ThemeColor => new Color(200, 40, 40);
        public override Color StartColor => new Color(255, 80, 80);
        public override Color EndColor => new Color(120, 10, 10);

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // ===== 爆8个灵魂 =====
            for (int i = 0; i < 8; i++)
            {
                Vector2 dir = Main.rand.NextVector2Unit();
                float speed = Main.rand.NextFloat(6f, 12f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    dir * speed,
                    ModContent.ProjectileType<NewSHPS>(),
                    projectile.damage,
                    projectile.knockBack,
                    projectile.owner,
                    3 // preset3
                );
            }

            // ===== 骷髅头 =====
            for (int i = 0; i < 4; i++)
            {
                Particle smoke = new DesertProwlerSkullParticle(
                    projectile.Center,
                    projectile.velocity * 0.5f,
                    Color.DarkGray * 0.8f,
                    Color.LightGray,
                    Main.rand.NextFloat(0.5f, 1.0f),
                    150
                );

                GeneralParticleHandler.SpawnParticle(smoke);
            }

            // ===== 冲击波 =====
            Particle expandingPulse = new DirectionalPulseRing(
                projectile.Center,
                Vector2.Zero,
                ThemeColor,
                new Vector2(1.2f, 1.2f),
                0f,
                0.5f,
                6.0f,
                20
            );

            GeneralParticleHandler.SpawnParticle(expandingPulse);
        }
    }
}