using System;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Ashes
{
    internal class AshesofAnnEffect : DefaultEffect
    {
        public override int EffectID => 39;

        public override int AmmoType => ModContent.ItemType<AshesofAnnihilation>();

        // 主题颜色：赤红
        public override Color ThemeColor => new Color(180, 0, 0);
        public override Color StartColor => new Color(255, 40, 40);
        public override Color EndColor => new Color(60, 0, 0);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.velocity *= 1.5f;

        }

        public override void AI(Projectile projectile, Player owner)
        {
            // 基础黑红照明
            Lighting.AddLight(projectile.Center, new Vector3(0.55f, 0.05f, 0.03f));

            if (Main.dedServ)
                return;

            CreateAshFlightFX(projectile);
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {

        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 在敌人身上生成标记弹幕，伤害直接传递
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<AshesofAnn_Located>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner,
                target.whoAmI
            );
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {

        }

        private void CreateAshFlightFX(Projectile projectile)
        {
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            // 新增：前移后的特效中心（2×16）
            Vector2 fxCenter = projectile.Center + forward * (2f * 16f);

            float time = Main.GlobalTimeWrappedHourly * 60f + projectile.identity * 17f;

            // 1. 中心最亮最烫：小范围 RancorLavaMetaball 内核
            for (int i = 0; i < 2; i++)
            {
                Vector2 corePos = fxCenter - forward * Main.rand.NextFloat(6f, 18f) + right * Main.rand.NextFloat(-10f, 10f);
                float coreRadius = Main.rand.NextFloat(36f, 64f);
                RancorLavaMetaball.SpawnParticle(corePos, coreRadius);
            }

            // 2. 黑红肉块拖尾：GruesomeMetaball 负责邪恶主体
            for (int i = 0; i < 4; i++)
            {
                float spread = i == 0 ? -1f : 1f;
                Vector2 spawnPos = fxCenter
                    - forward * Main.rand.NextFloat(18f, 44f)
                    + right * Main.rand.NextFloat(18f, 34f) * spread;

                Vector2 velocity = -forward * Main.rand.NextFloat(1.5f, 4.6f)
                    + right * Main.rand.NextFloat(0.6f, 1.8f) * spread;

                float size = Main.rand.NextFloat(22f, 44f);
                GruesomeMetaball.SpawnParticle(spawnPos, velocity, size);
            }

            // 3. 双螺旋暗焰：数学感主轴
            for (int i = -1; i <= 1; i += 2)
            {
                float helix = (float)Math.Sin(time * 0.22f + i * 1.57f) * 22f;
                Vector2 helixPos = fxCenter - forward * 14f + right * helix;

                Dust helixDust = Dust.NewDustPerfect(helixPos, 267);
                helixDust.noGravity = true;
                helixDust.scale = Main.rand.NextFloat(1.15f, 1.8f);
                helixDust.color = Color.Lerp(new Color(80, 0, 0), new Color(220, 20, 20), Main.rand.NextFloat());
                helixDust.velocity = -forward * Main.rand.NextFloat(0.8f, 2.2f) + right * i * Main.rand.NextFloat(0.3f, 1.2f);
            }

            // 4. 邪术圆环：用 Dust 拼出围绕弹幕旋转的小型魔法阵
            if (Main.rand.NextBool(2))
                CreateAshMiniMagicCircle(fxCenter, forward, right, time);

            // 5. 中层填充：黑烟为主
            for (int i = 0; i < 2; i++)
            {
                Vector2 smokePos = fxCenter
                    - forward * Main.rand.NextFloat(10f, 28f)
                    + Main.rand.NextVector2Circular(8f, 8f);

                Vector2 smokeVel = -forward * Main.rand.NextFloat(0.4f, 1.8f) + Main.rand.NextVector2Circular(0.7f, 0.7f);
                float smokeScale = Main.rand.NextFloat(0.85f, 1.35f);

                Particle smoke = new SmallSmokeParticle(
                    smokePos,
                    smokeVel,
                    Color.DimGray,
                    Main.rand.NextBool() ? Color.SlateGray : Color.Black,
                    smokeScale,
                    100
                );
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            // 6. 少量骷髅头点缀：占比低，但恐怖感强
            if (Main.rand.NextBool(10))
            {
                Vector2 skullPos = fxCenter
                    - forward * Main.rand.NextFloat(16f, 36f)
                    + right * Main.rand.NextFloat(-24f, 24f);

                Vector2 skullVel = -forward * Main.rand.NextFloat(0.6f, 1.8f) + Main.rand.NextVector2Circular(0.5f, 0.5f);

                Particle skull = new DesertProwlerSkullParticle(
                    skullPos,
                    skullVel,
                    Color.DarkGray * 0.8f,
                    Color.LightGray,
                    Main.rand.NextFloat(0.55f, 0.9f),
                    150
                );
                GeneralParticleHandler.SpawnParticle(skull);
            }

            // 7. 最外层细火花：用细 Dust 当 spark，做出割裂感
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                Vector2 sparkPos = fxCenter
                    - forward * Main.rand.NextFloat(6f, 20f)
                    + right * Main.rand.NextFloat(26f, 42f) * side;

                Dust spark = Dust.NewDustPerfect(sparkPos, 264);
                spark.noGravity = true;
                spark.scale = Main.rand.NextFloat(0.9f, 1.35f);
                spark.color = Color.Lerp(new Color(255, 80, 30), new Color(255, 190, 90), Main.rand.NextFloat());
                spark.velocity = right * side * Main.rand.NextFloat(0.8f, 2.6f) - forward * Main.rand.NextFloat(0.4f, 1.4f);
            }
        }

        private void CreateAshMiniMagicCircle(Vector2 center, Vector2 forward, Vector2 right, float time)
        {
            float baseRadius = 18f + (float)Math.Sin(time * 0.15f) * 4f;
            float rotation = time * 0.06f;

            // 外圈 6 点小法阵
            for (int i = 0; i < 6; i++)
            {
                float angle = rotation + MathHelper.TwoPi * i / 6f;
                Vector2 outward = angle.ToRotationVector2();
                Vector2 spawnPos = center - forward * 10f + outward * baseRadius;

                Dust ringDust = Dust.NewDustPerfect(spawnPos, 267);
                ringDust.noGravity = true;
                ringDust.scale = Main.rand.NextFloat(0.95f, 1.35f);
                ringDust.color = Color.Lerp(new Color(70, 0, 0), new Color(170, 15, 15), Main.rand.NextFloat());
                ringDust.velocity = outward * Main.rand.NextFloat(0.15f, 0.55f) + right * Main.rand.NextFloat(-0.4f, 0.4f);
            }

            // 内层双三角，做出六芒感
            CreateAshMiniTriangle(center - forward * 8f, 11f, rotation);
            CreateAshMiniTriangle(center - forward * 8f, 11f, rotation + MathHelper.Pi);
        }

        private void CreateAshMiniTriangle(Vector2 center, float radius, float rotation)
        {
            Vector2[] points = new Vector2[3];
            for (int i = 0; i < 3; i++)
                points[i] = center + (rotation - MathHelper.PiOver2 + MathHelper.TwoPi * i / 3f).ToRotationVector2() * radius;

            for (int i = 0; i < 3; i++)
            {
                Vector2 start = points[i];
                Vector2 end = points[(i + 1) % 3];

                for (int j = 0; j < 4; j++)
                {
                    float t = j / 3f;
                    Vector2 pos = Vector2.Lerp(start, end, t);

                    Dust lineDust = Dust.NewDustPerfect(pos, 267);
                    lineDust.noGravity = true;
                    lineDust.scale = Main.rand.NextFloat(0.85f, 1.15f);
                    lineDust.color = Color.Lerp(new Color(90, 0, 0), new Color(220, 20, 20), Main.rand.NextFloat());
                    lineDust.velocity = (pos - center).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.1f, 0.45f);
                }
            }
        }
    }
}