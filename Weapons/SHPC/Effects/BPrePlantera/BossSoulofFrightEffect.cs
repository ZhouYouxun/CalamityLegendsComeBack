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





            // ===== 爆8个灵魂（保留不变）=====
            for (int i = 0; i < 8; i++)
            {
                Vector2 dir = Main.rand.NextVector2Unit();
                float speed = Main.rand.NextFloat(6f, 12f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + Main.rand.NextVector2Circular(80f, 80f),
                    dir * speed,
                    ModContent.ProjectileType<NewSHPS>(),
                    projectile.damage,
                    projectile.knockBack,
                    projectile.owner,
                    3 // preset3
                );
            }










            // ===== 骷髅头主体：向外大范围爆散 =====
            for (int i = 0; i < 12; i++)
            {
                Vector2 dir = Main.rand.NextVector2Unit();
                Vector2 velocity = dir * Main.rand.NextFloat(2.5f, 7.5f);

                Particle skull = new DesertProwlerSkullParticle(
                    projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    velocity,
                    Color.Lerp(EndColor, Color.Black, Main.rand.NextFloat(0.25f, 0.55f)),
                    Color.Lerp(ThemeColor, StartColor, Main.rand.NextFloat(0.15f, 0.45f)),
                    Main.rand.NextFloat(0.75f, 1.35f),
                    Main.rand.Next(120, 185)
                );

                GeneralParticleHandler.SpawnParticle(skull);
            }

            // ===== 第二层骷髅头：沿原本前进方向喷出，强化“恐惧扑脸”感 =====
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 6; i++)
            {
                Vector2 dir = forward.RotatedByRandom(0.9f) * Main.rand.NextFloat(3.5f, 8.5f);

                Particle skull = new DesertProwlerSkullParticle(
                    projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    dir,
                    Color.Lerp(ThemeColor, EndColor, Main.rand.NextFloat(0.35f, 0.75f)),
                    Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat(0.2f, 0.5f)),
                    Main.rand.NextFloat(0.95f, 1.55f),
                    Main.rand.Next(135, 210)
                );

                GeneralParticleHandler.SpawnParticle(skull);
            }

            // ===== 光粒子核心：中心炸亮，制造“灵魂爆裂”感 =====
            for (int i = 0; i < 16; i++)
            {
                Vector2 dir = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 6f);

                SquishyLightParticle light = new(
                    projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    dir,
                    Main.rand.NextFloat(0.45f, 0.95f),
                    Color.Lerp(ThemeColor, StartColor, Main.rand.NextFloat(0.2f, 0.6f)),
                    Main.rand.Next(18, 30),
                    opacity: 1f,
                    squishStrenght: Main.rand.NextFloat(0.8f, 1.35f),
                    maxSquish: Main.rand.NextFloat(2.2f, 3.8f),
                    hueShift: 0f
                );

                GeneralParticleHandler.SpawnParticle(light);
            }

            // ===== 外层光粒子：向外拉出一圈凌厉感 =====
            for (int i = 0; i < 10; i++)
            {
                Vector2 dir = Main.rand.NextVector2Unit();
                Vector2 spawnPos = projectile.Center + dir * Main.rand.NextFloat(12f, 26f);
                Vector2 velocity = dir * Main.rand.NextFloat(3.5f, 7f);

                SquishyLightParticle light = new(
                    spawnPos,
                    velocity,
                    Main.rand.NextFloat(0.35f, 0.7f),
                    Color.Lerp(EndColor, ThemeColor, Main.rand.NextFloat(0.4f, 0.85f)),
                    Main.rand.Next(16, 24),
                    opacity: 1f,
                    squishStrenght: Main.rand.NextFloat(0.7f, 1.2f),
                    maxSquish: Main.rand.NextFloat(2f, 3.2f),
                    hueShift: 0f
                );

                GeneralParticleHandler.SpawnParticle(light);
            }

            // ===== 保留一个冲击波 =====
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