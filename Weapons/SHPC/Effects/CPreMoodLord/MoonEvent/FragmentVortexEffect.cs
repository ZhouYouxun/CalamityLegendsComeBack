using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentVortexEffect : DefaultEffect
    {
        public override int EffectID => 22;

        public override int AmmoType => ItemID.FragmentVortex;

        public override Color ThemeColor => new Color(0, 128, 128);
        public override Color StartColor => new Color(80, 220, 220);
        public override Color EndColor => new Color(0, 70, 90);

        public override float SquishyLightParticleFactor => 1.55f;
        public override float ExplosionPulseFactor => 1.55f;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.velocity *= 1.9f;
            projectile.timeLeft = 12;
            projectile.penetrate = 2;

            if (projectile.owner == Main.myPlayer)
                SoundEngine.PlaySound(new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/电弧发射器-蓄力结束"), projectile.Center);
        }

        private int fireTimer;
        private int shardTimer;

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            // 抵消默认减速，尽量保持高速感
            projectile.velocity *= 1.1f;

            // 后向双侧导弹
            {
                fireTimer++;

                if (fireTimer >= 2)
                {
                    fireTimer = 0;

                    Vector2 backDir = -projectile.velocity.SafeNormalize(Vector2.UnitX);

                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 shootDir = backDir.RotatedBy(MathHelper.ToRadians(30f * i));
                        int projID = Projectile.NewProjectile(
                            projectile.GetSource_FromThis(),
                            projectile.Center,
                            shootDir * 8.5f,
                            ProjectileID.VortexBeaterRocket,
                            (int)(projectile.damage * 0.72f),
                            1f,
                            projectile.owner
                        );

                        if (Main.projectile.IndexInRange(projID))
                        {
                            Projectile missile = Main.projectile[projID];
                            missile.friendly = true;
                            missile.hostile = false;
                            missile.usesIDStaticNPCImmunity = false;
                            missile.usesLocalNPCImmunity = true;
                            missile.localNPCHitCooldown = 6;
                            missile.DamageType = DamageClass.Magic;
                        }
                    }
                }
            }

            // StellarContempt-style LightDust 旋转轨道粒子
            if (Main.rand.NextBool(2))
            {
                Vector2 orbitOffset = new Vector2(16f, 0f).RotatedByRandom(MathHelper.TwoPi);
                Dust ld = Dust.NewDustPerfect(
                    projectile.Center + orbitOffset,
                    ModContent.DustType<LightDust>(),
                    projectile.velocity * 0.2f + new Vector2(3f, 0f).RotatedBy(orbitOffset.ToRotation()));
                ld.noGravity = true;
                ld.color = Main.rand.NextBool(3) ? Color.PaleTurquoise : new Color(0, 200, 200);
                ld.scale = Main.rand.NextFloat(0.8f, 1.3f);
            }

            // StellarVortexShard 纯视觉碎片，每3帧从两侧各喷一枚
            shardTimer++;
            if (shardTimer >= 3 && projectile.owner == Main.myPlayer)
            {
                shardTimer = 0;
                Vector2 perp = projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 shardVel = perp * side * Main.rand.NextFloat(2f, 4f)
                                     - projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(0.5f, 2.5f);
                    Projectile.NewProjectile(
                        projectile.GetSource_FromThis(),
                        projectile.Center,
                        shardVel,
                        ModContent.ProjectileType<StellarVortexShard>(),
                        0, 0f, projectile.owner,
                        Main.rand.NextFloat(4f, 9f),
                        Main.rand.NextFloat(0f, 1f),
                        Main.rand.Next(12, 22));
                }
            }

            // 青绿色飞行粒子
            if (Main.rand.NextBool(2))
            {
                int dustType = Utils.SelectRandom(Main.rand, 99, 202, 229);

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    dustType,
                    Main.rand.NextVector2Circular(1.6f, 1.6f),
                    0,
                    Color.Teal,
                    Main.rand.NextFloat(1f, 1.5f)
                );
                dust.noGravity = true;
            }

            // 轻微前向拖尾感
            if (Main.rand.NextBool(3))
            {
                Vector2 backward = -projectile.velocity.SafeNormalize(Vector2.UnitX);

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + backward * Main.rand.NextFloat(4f, 10f),
                    DustID.WhiteTorch,
                    backward * Main.rand.NextFloat(0.5f, 2f),
                    0,
                    Color.Cyan,
                    Main.rand.NextFloat(0.9f, 1.3f)
                );
                dust.noGravity = true;
            }

            SpawnVortexPixelTrail(projectile);
        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 命中特效：青绿能量散开
            for (int i = 0; i < 12; i++)
            {
                float angle = MathHelper.TwoPi * i / 12f;
                Vector2 dir = angle.ToRotationVector2();

                Dust dust = Dust.NewDustPerfect(
                    target.Center,
                    Utils.SelectRandom(Main.rand, 99, 202, 229),
                    dir * Main.rand.NextFloat(2.5f, 5f),
                    0,
                    Main.rand.NextBool() ? Color.Cyan : Color.Teal,
                    Main.rand.NextFloat(1.1f, 1.6f)
                );
                dust.noGravity = true;
            }

            SpawnVortexPixelTrail(projectile, true, target.Center);
        }

        private void SpawnVortexPixelTrail(Projectile projectile, bool burst = false, Vector2? centerOverride = null)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            Vector2 center = centerOverride ?? projectile.Center;
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            float age = 25f - projectile.timeLeft;
            float phase = age * 0.53f + projectile.identity * 0.31f;

            if (burst)
            {
                int ringCount = Main.rand.Next(28, 36);
                for (int i = 0; i < ringCount; i++)
                {
                    float t = i / (float)ringCount;
                    float angle = MathHelper.TwoPi * t + phase * 0.85f;
                    float radius = MathHelper.Lerp(18f, 94f, t) + (float)Math.Sin(phase + i * 1.71f) * 9f;
                    Vector2 dir = angle.ToRotationVector2();
                    Vector2 spawnPos =
                        center +
                        dir * radius +
                        forward * ((float)Math.Sin(t * MathHelper.TwoPi * 2f + phase) * 11f);

                    Vector2 drift =
                        dir * Main.rand.NextFloat(0.18f, 0.72f) -
                        forward * Main.rand.NextFloat(0.02f, 0.22f);

                    float colorFactor = 0.35f + 0.65f * (0.5f + 0.5f * (float)Math.Sin(angle * 3f + phase));
                    SpawnVortexPixel(projectile, spawnPos, drift, Main.rand.NextFloat(5.5f, 13.5f), colorFactor, Main.rand.Next(24, 42));
                }

                for (int i = -2; i <= 2; i++)
                {
                    if (i == 0)
                        continue;

                    Vector2 spawnPos = center + normal * i * 18f + forward * (float)Math.Sin(phase + i) * 14f;
                    Vector2 drift = normal * i * 0.12f + forward * Main.rand.NextFloat(-0.18f, 0.28f);
                    SpawnVortexPixel(projectile, spawnPos, drift, Main.rand.NextFloat(7f, 16f), 0.92f, Main.rand.Next(26, 38));
                }

                return;
            }

            for (int i = 0; i < 5; i++)
            {
                float laneSign = i % 2 == 0 ? 1f : -1f;
                float trailOffset = 8f + i * 11f + (float)Math.Sin(phase + i * 0.77f) * 3f;
                float laneOffset =
                    laneSign * (8f + i * 3.6f) +
                    (float)Math.Sin(phase * 1.42f + i * 1.11f) * 10f;

                Vector2 spawnPos = center - forward * trailOffset + normal * laneOffset;
                Vector2 drift = -forward * Main.rand.NextFloat(0.02f, 0.13f) + normal * laneSign * Main.rand.NextFloat(0.02f, 0.1f);
                float colorFactor = MathHelper.Clamp(0.28f + i * 0.13f + Main.rand.NextFloat(-0.08f, 0.16f), 0f, 1f);
                SpawnVortexPixel(projectile, spawnPos, drift, Main.rand.NextFloat(3.5f, 8.5f), colorFactor, Main.rand.Next(20, 34));
            }

            if (((int)age + projectile.identity) % 3 == 0)
            {
                float gateOffset = 28f + (float)Math.Sin(phase * 1.8f) * 8f;
                for (int sideIndex = -1; sideIndex <= 1; sideIndex += 2)
                {
                    Vector2 spawnPos =
                        center -
                        forward * (18f + (float)Math.Cos(phase) * 6f) +
                        normal * sideIndex * gateOffset;

                    Vector2 drift = normal * sideIndex * 0.18f - forward * 0.08f;
                    SpawnVortexPixel(projectile, spawnPos, drift, Main.rand.NextFloat(5f, 10f), Main.rand.NextFloat(0.68f, 1f), Main.rand.Next(22, 36));
                }
            }

            if (Main.rand.NextBool(4))
            {
                Vector2 spawnPos =
                    center -
                    forward * Main.rand.NextFloat(16f, 62f) +
                    normal * Main.rand.NextFloatDirection() * Main.rand.NextFloat(34f, 58f);

                Vector2 drift = Main.rand.NextVector2Circular(0.16f, 0.16f) - forward * 0.06f;
                SpawnVortexPixel(projectile, spawnPos, drift, Main.rand.NextFloat(4f, 9f), Main.rand.NextFloat(0.18f, 0.95f), Main.rand.Next(18, 30));
            }
        }

        private static void SpawnVortexPixel(Projectile projectile, Vector2 position, Vector2 velocity, float size, float colorFactor, int lifetime)
        {
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                position,
                velocity,
                ModContent.ProjectileType<FragmentVortex_Pixel>(),
                0,
                0f,
                projectile.owner,
                size,
                MathHelper.Clamp(colorFactor, 0f, 1f),
                lifetime);
        }

        public override void PreDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch)
        {
        }

        private void DrawVortexPixelAfterimages(Projectile projectile, SpriteBatch spriteBatch)
        {
            Texture2D shockPixel = TextureAssets.MagicPixel.Value;
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            float spawnFade = Utils.GetLerpValue(25f, 18f, projectile.timeLeft, true);
            float deathFade = Utils.GetLerpValue(0f, 7f, projectile.timeLeft, true);
            float globalFade = spawnFade * deathFade;

            if (globalFade <= 0f)
                return;

            int trailLength = projectile.oldPos.Length;
            for (int i = 0; i < trailLength; i++)
            {
                if (projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float age = i / (float)Math.Max(1, trailLength - 1);
                float ageFade = (float)Math.Pow(1f - age, 3.1f);
                if (ageFade <= 0.02f)
                    continue;

                Vector2 oldCenter = projectile.oldPos[i] + projectile.Size * 0.5f;
                Vector2 segmentForward = forward;
                if (i > 0 && projectile.oldPos[i - 1] != Vector2.Zero)
                    segmentForward = (projectile.oldPos[i - 1] - projectile.oldPos[i]).SafeNormalize(forward);
                Vector2 segmentNormal = segmentForward.RotatedBy(MathHelper.PiOver2);

                int pixelCount = i < 3 ? 12 : 7;
                UnifiedRandom seededRandom = new(projectile.identity * 997 + i * 131);
                for (int j = 0; j < pixelCount; j++)
                {
                    float along = seededRandom.NextFloat(-18f, 16f) * (1f - age * 0.45f);
                    float lateral = seededRandom.NextFloat(-34f, 34f) * (1f - age * 0.25f);
                    float wobble = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 18f + i * 1.7f + j * 0.53f) * seededRandom.NextFloat(1f, 5f);
                    Vector2 worldPos = oldCenter + segmentForward * along + segmentNormal * (lateral + wobble);
                    Vector2 drawPos = worldPos - Main.screenPosition;

                    float cellSize = seededRandom.NextFloat(3f, 10f) * MathHelper.Lerp(1.15f, 0.35f, age);
                    float intensity = 0.45f + 0.55f *
                        (float)Math.Sin(i * 0.73f + j * 1.17f + Main.GlobalTimeWrappedHourly * 9f);

                    Color pixelColor = Color.Lerp(
                        new Color(0, 62, 82),
                        new Color(62, 255, 235),
                        intensity);

                    if ((i + j + projectile.identity) % 11 == 0)
                        pixelColor = Color.Lerp(pixelColor, Color.White, 0.32f);

                    float alpha = globalFade * ageFade * seededRandom.NextFloat(0.32f, 0.82f);
                    Rectangle voxelRect = new(
                        (int)drawPos.X,
                        (int)drawPos.Y,
                        Math.Max(1, (int)cellSize),
                        Math.Max(1, (int)cellSize));

                    spriteBatch.Draw(
                        shockPixel,
                        voxelRect,
                        null,
                        pixelColor * alpha,
                        0f,
                        Vector2.Zero,
                        SpriteEffects.None,
                        0f);
                }
            }

            if (Main.rand.NextBool(3))
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 drawPos = projectile.Center - Main.screenPosition +
                        forward * Main.rand.NextFloat(-18f, 8f) +
                        normal * Main.rand.NextFloat(-20f, 20f);
                    float size = Main.rand.NextFloat(4f, 11f);
                    Color color = Color.Lerp(Color.Cyan, Color.Teal, Main.rand.NextFloat(0.15f, 0.75f));
                    color.A = 0;

                    spriteBatch.Draw(
                        shockPixel,
                        new Rectangle((int)drawPos.X, (int)drawPos.Y, (int)size, (int)size),
                        null,
                        color * 0.45f * globalFade,
                        0f,
                        Vector2.Zero,
                        SpriteEffects.None,
                        0f);
                }
            }
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // 退场粒子
            for (int i = 0; i < 20; i++)
            {
                int dustType = Utils.SelectRandom(Main.rand, 99, 202, 229);

                Dust dust = Dust.NewDustPerfect(
                    projectile.Center,
                    dustType,
                    Main.rand.NextVector2Circular(4f, 4f),
                    0,
                    Main.rand.NextBool() ? Color.Cyan : Color.Teal,
                    Main.rand.NextFloat(1.2f, 1.8f)
                );
                dust.noGravity = true;
            }

            // ===== StellarContempt Echo 风格：旋转环形爆炸 =====
            const float stellarDustCount = 120f;
            float stellarRotFactor = 360f / stellarDustCount;
            for (int i = 0; i < (int)stellarDustCount; i++)
            {
                float rot = MathHelper.ToRadians(i * stellarRotFactor);
                float intensity = Main.rand.NextFloat(0.25f, 1f);
                Vector2 offset = new Vector2(28f, 5f).RotatedBy(rot);
                Vector2 velOffset = new Vector2(24f, 5.4f).RotatedBy(rot);

                if (i % 3 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(
                        projectile.Center + offset,
                        velOffset * intensity * 0.65f,
                        "CalamityMod/Particles/Sparkle",
                        false,
                        (int)(38 * intensity),
                        intensity,
                        Main.rand.NextBool(3) ? Color.PaleTurquoise : new Color(0, 200, 200),
                        new Vector2(1f, 2f), true, true));
                }
                else
                {
                    Dust d = Dust.NewDustPerfect(
                        projectile.Center + offset,
                        ModContent.DustType<LightDust>(),
                        velOffset);
                    d.noGravity = true;
                    d.velocity = velOffset * intensity;
                    d.scale = Main.rand.NextFloat(1.6f, 2.4f) * intensity;
                    d.color = Main.rand.NextBool(3) ? Color.PaleTurquoise : new Color(0, 200, 200);
                }
            }

            // GlowSparkParticle 八方向迸射
            for (int i = 0; i < 8; i++)
            {
                Vector2 sparkVel = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * Main.rand.NextFloat(3.35f, 6.7f);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    projectile.Center,
                    sparkVel,
                    false, 16, 0.18f,
                    Color.Lerp(new Color(0, 200, 200), Color.PaleTurquoise, Main.rand.NextFloat()),
                    new Vector2(2f, 0.7f),
                    true, true, 0.85f));
            }

            // CustomPulse BloomRing 双层：内紧外宽
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                projectile.Center, Vector2.Zero,
                new Color(0, 200, 200),
                "CalamityMod/Particles/BloomRing",
                new Vector2(0.7f, 1f),
                projectile.velocity.ToRotation(),
                0f, 2.35f, 28));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                projectile.Center, Vector2.Zero,
                Color.PaleTurquoise,
                "CalamityMod/Particles/BloomRing",
                new Vector2(0.45f, 1f),
                projectile.velocity.ToRotation(),
                0f, 3.2f, 28));

            // ===== 前向导弹：傅里叶调制七发结构 =====
            {
                Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

                float[] baseIndex = { -3f, -2f, -1f, 0f, 1f, 2f, 3f };

                for (int i = 0; i < 7; i++)
                {
                    float t = baseIndex[i];

                    float baseAngle = t * 9f;
                    float waveOffset = 6f * (float)Math.Sin(t * 1.3f);
                    float finalAngle = baseAngle + waveOffset;

                    Vector2 dir = forward.RotatedBy(MathHelper.ToRadians(finalAngle));

                    float speedFactor = 1f - Math.Abs(t) / 3f;
                    float speed = MathHelper.Lerp(7.5f, 13f, speedFactor);

                    Vector2 velocity = dir * speed;

                    int projID = Projectile.NewProjectile(
                        projectile.GetSource_FromThis(),
                        projectile.Center,
                        velocity,
                        ProjectileID.VortexBeaterRocket,
                        (int)(projectile.damage * 0.2f),
                        1f,
                        projectile.owner
                    );

                    if (Main.projectile.IndexInRange(projID))
                    {
                        Projectile missile = Main.projectile[projID];
                        missile.friendly = true;
                        missile.hostile = false;
                        missile.usesIDStaticNPCImmunity = false;
                        missile.usesLocalNPCImmunity = true;
                        missile.localNPCHitCooldown = 6;
                        missile.DamageType = DamageClass.Magic;
                    }
                }
            }
        }
    }
}
