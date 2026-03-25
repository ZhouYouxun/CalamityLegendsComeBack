using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    public class NecroplasmEffect : DefaultEffect
    {
        public override int EffectID => 31;

        public override int AmmoType => ModContent.ItemType<Necroplasm>();

        public override Color ThemeColor => new Color(40, 40, 40);
        public override Color StartColor => new Color(120, 120, 120);
        public override Color EndColor => new Color(5, 5, 5);

        public override float SquishyLightParticleFactor => 1.85f;
        public override float ExplosionPulseFactor => 1.85f;

        private float sinTimer;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            sinTimer = 0f;

            // 穿透5次
            projectile.penetrate = 5;

            // 初始速度1.5倍
            projectile.velocity *= 1.5f;
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            // 抵消减速
            projectile.velocity *= 1.02f;

            // === ∞式追踪核心（来自 EternityHoming）===
            NPC target = projectile.Center.ClosestNPCAt(3000f);
            if (target != null)
            {
                Vector2 desired = projectile.SafeDirectionTo(target.Center);
                projectile.velocity = (projectile.velocity * 7f + desired * 10f) / 8f;
            }

            // === ∞视觉 ===
            sinTimer += 0.22f;

            float pulse = (float)System.Math.Sin(sinTimer);

            float angle = projectile.velocity.ToRotation() + (MathHelper.Pi / 2f);
            Vector2 normal = angle.ToRotationVector2();

            float radius = 6f;

            Vector2 offset = normal * pulse * radius;

            // 左右双轨
            CreateVoidDust(projectile.Center + offset);
            CreateVoidDust(projectile.Center - offset);

            // 中心弱粒子（增加层次）
            if (Main.rand.NextBool(3))
            {
                SquishyLightParticle p = new(
                    projectile.Center,
                    -projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.05f, 0.2f),
                    Main.rand.NextFloat(0.3f, 0.6f),
                    Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat()),
                    Main.rand.Next(10, 16)
                );
                GeneralParticleHandler.SpawnParticle(p);
            }

            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.25f);
        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // ===== 大型爆炸 =====
            int projIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner
            );

            Projectile proj = Main.projectile[projIndex];

            proj.width = 250;
            proj.height = 250;

            // 黑暗爆散
            for (int i = 0; i < 36; i++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f);

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    DustID.Shadowflame,
                    dir,
                    0,
                    Color.Lerp(StartColor, EndColor, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.2f, 1.9f)
                );
                dust.noGravity = true;
            }

            // 虚空脉冲
            Particle pulse = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                new Color(30, 30, 30),
                "CalamityMod/Particles/BloomCircle",
                Vector2.One * 1.2f,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.02f,
                0.3f,
                26,
                true
            );
            GeneralParticleHandler.SpawnParticle(pulse);
        }

        // ================= 工具函数 =================
        private void CreateVoidDust(Vector2 pos)
        {
            Dust dust = Dust.NewDustPerfect(
                pos,
                DustID.Shadowflame,
                Vector2.Zero,
                0,
                Color.Lerp(new Color(80, 80, 80), new Color(10, 10, 10), Main.rand.NextFloat()),
                Main.rand.NextFloat(1.1f, 1.6f)
            );
            dust.noGravity = true;
        }
    }
}