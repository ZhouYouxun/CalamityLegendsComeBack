using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class SoulofLightEffect : DefaultEffect
    {
        public override int EffectID => 9;
        public override int AmmoType => ItemID.SoulofLight;

        // ===== 三段粉色 =====
        public override Color ThemeColor => new Color(255, 120, 200);
        public override Color StartColor => new Color(255, 180, 230);
        public override Color EndColor => new Color(255, 80, 160);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override bool EnableDefaultSlowdown => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            // ===== 穿透设置 =====
            projectile.tileCollide = false;
            projectile.penetrate = 1;

            // ===== 初速度提升 =====
            projectile.velocity *= 1.1f;

            // ===== 生成3个伴随弹幕 =====
            for (int i = 0; i < 3; i++)
            {
                int id = Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<NewSHPS>(),
                    (projectile.damage) * 3,
                    projectile.knockBack,
                    projectile.owner,
                    0,                      // presetIndex = 0
                    projectile.whoAmI       // 绑定主弹幕
                );
            }
        }

        public override void AI(Projectile projectile, Player owner)
        {
            // ===== 屏幕反弹 =====
            Rectangle screenRect = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
            Vector2 screenPosition = projectile.Center - Main.screenPosition;

            if (!screenRect.Contains(screenPosition.ToPoint()))
            {
                if (screenPosition.X <= 0 || screenPosition.X >= Main.screenWidth)
                    projectile.velocity.X *= -1;

                if (screenPosition.Y <= 0 || screenPosition.Y >= Main.screenHeight)
                    projectile.velocity.Y *= -1;
            }
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers) { }
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone) { }
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 center = projectile.Center;

            // ===== 五角星基础朝向：尖角朝前 =====
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitY);
            float baseRotation = forward.ToRotation() - MathHelper.PiOver2;

            float outerRadius = 54f;
            Vector2[] outerPoints = new Vector2[5];

            // ===== 先求正五边形5个外点 =====
            for (int i = 0; i < 5; i++)
            {
                float angle = baseRotation + MathHelper.TwoPi * i / 5f;
                outerPoints[i] = center + angle.ToRotationVector2() * outerRadius;
            }

            // ===== 五角星连线顺序：0→2→4→1→3→0 =====
            int[] starOrder = { 0, 2, 4, 1, 3, 0 };

            // ===== 1. 中心核心闪光 =====
            for (int i = 0; i < 8; i++)
            {
                SquishyLightParticle core = new(
                    center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2Circular(2f, 2f),
                    Main.rand.NextFloat(0.75f, 1.05f),
                    Color.Lerp(ThemeColor, StartColor, Main.rand.NextFloat(0.25f, 0.8f)),
                    Main.rand.Next(20, 28)
                );
                GeneralParticleHandler.SpawnParticle(core);
            }

            // ===== 2. 五角星主体：沿5条线段布点，并整体向外扩散 =====
            for (int seg = 0; seg < 5; seg++)
            {
                Vector2 start = outerPoints[starOrder[seg]];
                Vector2 end = outerPoints[starOrder[seg + 1]];

                for (int j = 0; j < 8; j++)
                {
                    float t = j / 7f;
                    Vector2 pos = Vector2.Lerp(start, end, t);

                    // 从中心指向当前点，作为“向外绽放”的主方向
                    Vector2 outward = (pos - center).SafeNormalize(Vector2.UnitY);

                    // 轻微切线扰动，让它不像死板几何线
                    Vector2 tangent = outward.RotatedBy(MathHelper.Pi / 2f);
                    Vector2 velocity =
                        outward * Main.rand.NextFloat(2.4f, 5.2f) +
                        tangent * Main.rand.NextFloat(-0.9f, 0.9f);

                    float scale = MathHelper.Lerp(0.9f, 0.42f, t);
                    Color color = Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat(0.25f, 0.7f));

                    SquishyLightParticle starLine = new(
                        pos,
                        velocity,
                        scale,
                        color,
                        Main.rand.Next(18, 26)
                    );
                    GeneralParticleHandler.SpawnParticle(starLine);
                }
            }

            // ===== 3. 五个外尖角额外强化，让“星角”更明显 =====
            for (int i = 0; i < 5; i++)
            {
                Vector2 tipDir = (outerPoints[i] - center).SafeNormalize(Vector2.UnitY);

                for (int j = 0; j < 3; j++)
                {
                    SquishyLightParticle tipFlash = new(
                        outerPoints[i] + Main.rand.NextVector2Circular(4f, 4f),
                        tipDir * Main.rand.NextFloat(3.5f, 6.5f),
                        Main.rand.NextFloat(0.7f, 1.0f),
                        Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.1f, 0.3f)),
                        Main.rand.Next(18, 24)
                    );
                    GeneralParticleHandler.SpawnParticle(tipFlash);
                }
            }

            // ===== 4. Spark 补线条锐感：沿五角星边线扫出去 =====
            for (int seg = 0; seg < 5; seg++)
            {
                Vector2 start = outerPoints[starOrder[seg]];
                Vector2 end = outerPoints[starOrder[seg + 1]];
                Vector2 lineDir = (end - start).SafeNormalize(Vector2.UnitX);

                for (int j = 0; j < 6; j++)
                {
                    float t = j / 5f;
                    Vector2 pos = Vector2.Lerp(start, end, t);

                    Particle spark = new SparkParticle(
                        pos,
                        lineDir.RotatedByRandom(0.35f) * Main.rand.NextFloat(2.5f, 4.8f),
                        false,
                        Main.rand.Next(16, 24),
                        Main.rand.NextFloat(0.65f, 0.95f),
                        Color.Lerp(EndColor, ThemeColor, Main.rand.NextFloat(0.3f, 0.8f))
                    );
                    GeneralParticleHandler.SpawnParticle(spark);
                }
            }

            // ===== 5. 中心再补一点十字式星爆，让收尾更亮 =====
            for (int i = 0; i < 10; i++)
            {
                float angle = baseRotation + MathHelper.TwoPi * i / 10f;
                Vector2 dir = angle.ToRotationVector2();

                Particle spark = new SparkParticle(
                    center,
                    dir * Main.rand.NextFloat(2.8f, 5.2f),
                    false,
                    Main.rand.Next(14, 22),
                    Main.rand.NextFloat(0.7f, 1.0f),
                    Color.Lerp(StartColor, Color.White, Main.rand.NextFloat(0.15f, 0.35f))
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }


    }
}