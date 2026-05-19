using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentVortexEffect : DefaultEffect
    {
        public override int EffectID => 22;

        // 先占位，后面你自己换真实弹药
        public override int AmmoType => ItemID.FragmentVortex;

        // ===== Vortex主题色 =====
        public override Color ThemeColor => new Color(0, 128, 128);
        public override Color StartColor => new Color(80, 220, 220);
        public override Color EndColor => new Color(0, 70, 90);

        public override float SquishyLightParticleFactor => 1.55f;
        public override float ExplosionPulseFactor => 1.55f;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            // 初速度翻倍
            projectile.velocity *= 2f;

            // 只存活很短时间
            projectile.timeLeft = 25;
        }

        private int fireTimer;
        private float spiralAngle;

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            // 抵消默认减速，尽量保持高速感
            projectile.velocity *= 1.020408f;


            {
                // ===== 单螺旋导弹释放 =====
                fireTimer++;

                if (fireTimer >= 2)
                {
                    fireTimer = 0;

                    // ===== 后方方向 =====
                    Vector2 backDir = -projectile.velocity.SafeNormalize(Vector2.UnitX);

                    // ===== 单螺旋角度（正弦摆动）=====
                    float maxAngle = MathHelper.ToRadians(55f); // 左右摆幅（你可以调）
                    spiralAngle += 0.28f; // 摆动速度

                    float angleOffset = (float)Math.Sin(spiralAngle) * maxAngle;

                    Vector2 shootDir = backDir.RotatedBy(angleOffset);

                    Vector2 velocity = shootDir * 8.5f;

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
            // 简单命中特效：青绿能量散开
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

            SpawnVortexPixelTrail(projectile, true);
        }

        private void SpawnVortexPixelTrail(Projectile projectile, bool burst = false)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            int pixelCount = burst ? Main.rand.Next(24, 34) : Main.rand.Next(8, 14);

            for (int i = 0; i < pixelCount; i++)
            {
                float backOffset = burst ? Main.rand.NextFloat(-12f, 58f) : Main.rand.NextFloat(2f, 54f);
                float lateralOffset = Main.rand.NextFloat(burst ? -76f : -42f, burst ? 76f : 42f);
                Vector2 spawnPos =
                    projectile.Center -
                    forward * backOffset +
                    normal * lateralOffset +
                    Main.rand.NextVector2Circular(burst ? 18f : 7f, burst ? 18f : 7f);

                Vector2 drift =
                    normal * Main.rand.NextFloat(-0.46f, 0.46f) -
                    forward * Main.rand.NextFloat(0.04f, 0.24f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    spawnPos,
                    drift,
                    ModContent.ProjectileType<FragmentVortex_Pixel>(),
                    0,
                    0f,
                    projectile.owner,
                    Main.rand.NextFloat(burst ? 18f : 12f, burst ? 42f : 28f),
                    Main.rand.NextFloat(),
                    Main.rand.Next(burst ? 34 : 30, burst ? 58 : 48));
            }
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
            // 先补一点Vortex风格退场粒子
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









            {
                // ===== 前向导弹：傅里叶调制七发结构 =====
                Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

                // 基础对称索引（保证结构稳定）
                float[] baseIndex = { -3f, -2f, -1f, 0f, 1f, 2f, 3f };

                for (int i = 0; i < 7; i++)
                {
                    float t = baseIndex[i];

                    // ===== 角度：线性项 + 正弦调制（傅里叶一阶）=====
                    float baseAngle = t * 9f; // 基础展开（线性）
                    float waveOffset = 6f * (float)Math.Sin(t * 1.3f); // 波形扰动

                    float finalAngle = baseAngle + waveOffset;

                    // ===== 方向 =====
                    Vector2 dir = forward.RotatedBy(MathHelper.ToRadians(finalAngle));

                    // ===== 速度：中心强，两侧弱（高斯分布感）=====
                    float speedFactor = 1f - Math.Abs(t) / 3f; // [-3,3] → [0,1]
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
