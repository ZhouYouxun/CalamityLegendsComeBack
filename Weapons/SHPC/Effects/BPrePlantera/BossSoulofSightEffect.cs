using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class BossSoulofSightEffect : DefaultEffect
    {
        public override int EffectID => 14;
        public override int AmmoType => ItemID.SoulofSight;

        // ===== 亮绿色 =====
        public override Color ThemeColor => new Color(120, 255, 120);
        public override Color StartColor => new Color(180, 255, 180);
        public override Color EndColor => new Color(60, 180, 60);

        private Vector2 spawnPos;
        private float traveledDistance;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            spawnPos = projectile.Center;
            traveledDistance = 0f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            traveledDistance = Vector2.Distance(spawnPos, projectile.Center);
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // ===== 距离转倍率 =====
            float t = MathHelper.Clamp(traveledDistance / (80f * 16f), 0.2f, 2f);

            // ===== 爆炸 =====
            int explosionIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                (int)(projectile.damage * 0.1f * t),
                projectile.knockBack,
                projectile.owner
            );

            Projectile explosion = Main.projectile[explosionIndex];
            explosion.width = 150;
            explosion.height = 150;

            // ===== 7个灵魂 =====
            for (int i = 0; i < 7; i++)
            {
                Vector2 dir = Main.rand.NextVector2Unit();
                float speed = Main.rand.NextFloat(6f, 10f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    dir * speed,
                    ModContent.ProjectileType<NewSHPS>(),
                    (int)(projectile.damage * t),
                    projectile.knockBack,
                    projectile.owner,
                    5
                );
            }

            // ================= 荷鲁斯之眼 =================

            // 外圈（椭圆）
            for (int i = 0; i < 20; i++)
            {
                float angle = MathHelper.TwoPi * i / 20f;
                Vector2 offset = new Vector2((float)Math.Cos(angle) * 60f, (float)Math.Sin(angle) * 25f);

                SquishyLightParticle particle = new(
                    projectile.Center + offset,
                    Vector2.Zero,
                    1.2f,
                    ThemeColor,
                    40
                );
                GeneralParticleHandler.SpawnParticle(particle);
            }

            // 中心瞳孔
            for (int i = 0; i < 6; i++)
            {
                GlowOrbParticle orb = new GlowOrbParticle(
                    projectile.Center,
                    Vector2.Zero,
                    false,
                    5,
                    1.2f,
                    ThemeColor,
                    true,
                    false,
                    true
                );
                GeneralParticleHandler.SpawnParticle(orb);
            }

            // 上下“眼线”
            for (int i = 0; i < 10; i++)
            {
                float x = MathHelper.Lerp(-60f, 60f, i / 9f);

                Vector2 pos1 = projectile.Center + new Vector2(x, -20f);
                Vector2 pos2 = projectile.Center + new Vector2(x, 20f);

                Particle trail1 = new SparkParticle(pos1, Vector2.Zero, false, 40, 1f, ThemeColor);
                Particle trail2 = new SparkParticle(pos2, Vector2.Zero, false, 40, 1f, ThemeColor);

                GeneralParticleHandler.SpawnParticle(trail1);
                GeneralParticleHandler.SpawnParticle(trail2);
            }
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers) { }
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone) { }
    }
}