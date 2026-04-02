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

        // ===== 全部改成亮粉 =====
        public override Color ThemeColor => new Color(255, 80, 180);
        public override Color StartColor => new Color(255, 120, 200);
        public override Color EndColor => new Color(200, 40, 140);

        public override float SquishyLightParticleFactor => 1.85f;
        public override float ExplosionPulseFactor => 1.85f;

        private float sinTimer;
        private int timer;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            sinTimer = 0f;
            timer = 0;

            projectile.penetrate = 5;

            // 初始更快一点
            projectile.velocity *= 1.8f;
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            timer++;

            // 抵消减速（更快）
            projectile.velocity *= 1.04f;

            // ===== 强化追踪 =====
            NPC target = projectile.Center.ClosestNPCAt(3000f);
            if (target != null)
            {
                Vector2 desired = projectile.SafeDirectionTo(target.Center);
                projectile.velocity = (projectile.velocity * 5f + desired * 16f) / 6f;
            }

            // ===== 每6帧释放子弹 =====
            if (timer % 6 == 0)
            {
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<Necroplasm_Damage>(),
                    (int)(projectile.damage * 0.5f),
                    0f,
                    projectile.owner
                );
            }

            // === ∞视觉 ===
            sinTimer += 0.22f;

            float pulse = (float)System.Math.Sin(sinTimer);

            float angle = projectile.velocity.ToRotation() + (MathHelper.Pi / 2f);
            Vector2 normal = angle.ToRotationVector2();

            float radius = 6f;

            Vector2 offset = normal * pulse * radius;

            CreateVoidDust(projectile.Center + offset);
            CreateVoidDust(projectile.Center - offset);

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

            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.35f);
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
            // ===== 原爆炸保留 =====
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

            // ===== 新：分裂6个 =====
            for (int i = 0; i < 6; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 8f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    velocity,
                    ModContent.ProjectileType<Necroplasm_Damage>(),
                    (int)(projectile.damage * Main.rand.NextFloat(0.8f, 1.2f)),
                    0f,
                    projectile.owner
                );
            }

            // ===== 粉色爆散 =====
            for (int i = 0; i < 36; i++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f);

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    DustID.FireworkFountain_Pink,
                    dir,
                    0,
                    Color.Lerp(StartColor, EndColor, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.2f, 1.9f)
                );
                dust.noGravity = true;
            }

            Particle pulse = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                ThemeColor,
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
                DustID.FireworkFountain_Pink,
                Vector2.Zero,
                0,
                Color.Lerp(StartColor, EndColor, Main.rand.NextFloat()),
                Main.rand.NextFloat(1.1f, 1.6f)
            );
            dust.noGravity = true;
        }
    }
}