using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
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

            // ===== 主弹幕大幅提速 =====
            projectile.velocity *= 2.1f;

            // ===== 生成3个伴随弹幕 =====
            for (int i = 0; i < 3; i++)
            {
                int id = Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<NewSHPS>(),
                    (projectile.damage) * 2,
                    projectile.knockBack,
                    projectile.owner,
                    0,                      // presetIndex = 0
                    projectile.whoAmI       // 绑定主弹幕
                );
            }
        }

        public override void AI(Projectile projectile, Player owner)
        {
            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.55f);

            // 波浪形变化的播放间隔：在14~52帧之间正弦起伏
            int interval = (int)(33f + 19f * (float)Math.Sin(Main.GameUpdateCount * 0.055f));
            if (Main.GameUpdateCount % interval == 0 && owner.whoAmI == Main.myPlayer)
                SoundEngine.PlaySound(SoundID.DD2_SkyDragonsFurySwing, projectile.Center);
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers) { }
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            int soulType = ModContent.ProjectileType<NewSHPS>();

            foreach (Projectile soul in Main.ActiveProjectiles)
            {
                if (soul.owner != projectile.owner || soul.type != soulType)
                    continue;

                if ((int)soul.ai[0] != 0 || (int)soul.ai[1] != projectile.whoAmI)
                    continue;

                soul.ai[2] = 4f;
                soul.localAI[0] = target.whoAmI;
                soul.timeLeft = System.Math.Max(soul.timeLeft, 90);
                soul.netUpdate = true;
            }
        }
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 center = projectile.Center;

            // ===== 五角星基础朝向：尖角朝前 =====
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitY);
            float baseRotation = forward.ToRotation() - MathHelper.PiOver2;

            float outerRadius = 74f;
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
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                Color.Lerp(StartColor, Color.White, 0.55f),
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                baseRotation,
                0.34f,
                2.05f,
                24));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Vector2.Zero,
                Color.Lerp(ThemeColor, Color.White, 0.4f),
                Vector2.One,
                baseRotation,
                0.16f,
                2.95f,
                26));

            for (int i = 0; i < 14; i++)
            {
                SquishyLightParticle core = new(
                    center + Main.rand.NextVector2Circular(9f, 9f),
                    Main.rand.NextVector2Circular(3.2f, 3.2f),
                    Main.rand.NextFloat(0.95f, 1.45f),
                    Color.Lerp(StartColor, Color.White, Main.rand.NextFloat(0.35f, 0.9f)),
                    Main.rand.Next(24, 34)
                );
                GeneralParticleHandler.SpawnParticle(core);
            }

            // ===== 2. 五角星主体：沿5条线段布点，并整体向外扩散 =====
            for (int seg = 0; seg < 5; seg++)
            {
                Vector2 start = outerPoints[starOrder[seg]];
                Vector2 end = outerPoints[starOrder[seg + 1]];

                for (int j = 0; j < 13; j++)
                {
                    float t = j / 12f;
                    Vector2 pos = Vector2.Lerp(start, end, t);

                    // 从中心指向当前点，作为"向外绽放"的主方向
                    Vector2 outward = (pos - center).SafeNormalize(Vector2.UnitY);

                    // 轻微切线扰动，让它不像死板几何线
                    Vector2 tangent = outward.RotatedBy(MathHelper.Pi / 2f);
                    Vector2 velocity =
                        outward * Main.rand.NextFloat(3.2f, 7.2f) +
                        tangent * Main.rand.NextFloat(-1.25f, 1.25f);

                    float scale = MathHelper.Lerp(1.16f, 0.5f, t);
                    Color color = Color.Lerp(Color.White, ThemeColor, Main.rand.NextFloat(0.15f, 0.55f));

                    SquishyLightParticle starLine = new(
                        pos,
                        velocity,
                        scale,
                        color,
                        Main.rand.Next(22, 31)
                    );
                    GeneralParticleHandler.SpawnParticle(starLine);
                }
            }

            // ===== 3. 五个外尖角额外强化，让"星角"更明显 =====
            for (int i = 0; i < 5; i++)
            {
                Vector2 tipDir = (outerPoints[i] - center).SafeNormalize(Vector2.UnitY);

                for (int j = 0; j < 6; j++)
                {
                    SquishyLightParticle tipFlash = new(
                        outerPoints[i] + Main.rand.NextVector2Circular(6f, 6f),
                        tipDir * Main.rand.NextFloat(4.6f, 9.2f),
                        Main.rand.NextFloat(0.85f, 1.32f),
                        Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.35f, 0.75f)),
                        Main.rand.Next(20, 30)
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

                for (int j = 0; j < 10; j++)
                {
                    float t = j / 9f;
                    Vector2 pos = Vector2.Lerp(start, end, t);

                    Particle spark = new SparkParticle(
                        pos,
                        lineDir.RotatedByRandom(0.28f) * Main.rand.NextFloat(3.4f, 6.8f),
                        false,
                        Main.rand.Next(18, 28),
                        Main.rand.NextFloat(0.82f, 1.25f),
                        Color.Lerp(Color.White, ThemeColor, Main.rand.NextFloat(0.12f, 0.55f))
                    );
                    GeneralParticleHandler.SpawnParticle(spark);
                }
            }

            // ===== 5. 中心再补一点十字式星爆，让收尾更亮 =====
            for (int i = 0; i < 20; i++)
            {
                float angle = baseRotation + MathHelper.TwoPi * i / 20f;
                Vector2 dir = angle.ToRotationVector2();

                Particle spark = new SparkParticle(
                    center,
                    dir * Main.rand.NextFloat(4.2f, 8.8f),
                    false,
                    Main.rand.Next(18, 29),
                    Main.rand.NextFloat(0.9f, 1.38f),
                    Color.Lerp(StartColor, Color.White, Main.rand.NextFloat(0.35f, 0.85f))
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }


    }
}
