using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    internal class UnholyEssence_Wave : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles";
        // 自定义计时器
        private int lifeTimer;
        public override string Texture => "Terraria/Images/Projectile_0"; // 透明占位

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 52;
            Projectile.height = 200;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;
            Projectile.light = 0.45f;
            Projectile.scale = 0.9f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // ===== 前10帧不绘制 =====
            if (lifeTimer < 10)
                return true;

            SpriteBatch sb = Main.spriteBatch;

            Texture2D tex = ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;

            // ======== 亮黄色神圣调色盘 ========
            Color[] palette = new Color[]
            {
                new Color(255, 255, 200), // 白金高光
                new Color(255, 230, 120), // 亮黄
                new Color(255, 180, 60),  // 橙黄
                new Color(120, 80, 20),   // 暗金核心
            };

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive);

            //float WidthFunc(float t, Vector2 v)
            //{
            //    float w = Projectile.width * 3.2f;
            //    w *= MathHelper.Lerp(1f, 0.6f, t);
            //    return w;
            //}

            //Color ColorFunc(float t, Vector2 v)
            //{
            //    int idx = (int)(t * (palette.Length - 1));
            //    idx = Utils.Clamp(idx, 0, palette.Length - 1);

            //    Color c = palette[idx];
            //    c *= (1f - t) * Projectile.Opacity * 1.2f;
            //    c.A = 0;
            //    return c;
            //}

            //Vector2 offsetFunc(float t, Vector2 v)
            //{
            //    return Projectile.Size * 0.5f;
            //}

            // ===== 前推 oldPos 到弹幕前端 =====
            Vector2 frontOffset = Projectile.velocity.SafeNormalize(Vector2.Zero) * (Projectile.width * 1.2f);

            Vector2[] shiftedOldPos = new Vector2[Projectile.oldPos.Length];
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                shiftedOldPos[i] = Projectile.oldPos[i] + frontOffset;
            }

            // ===== Shader（关键，不然永远是梯形）=====
            GameShaders.Misc["CalamityMod:SideStreakTrail"].UseImage1("Images/Misc/Perlin");

            // ===== 更宽 + 更圆润（无尖头）=====
            float WidthFunc(float t, Vector2 v)
            {
                // 👉 直接控制整体宽度（≈200px）
                float baseWidth = 200f;

                // 👉 使用更平滑的曲线（中段饱满）
                float shape = (float)Math.Sin(t * MathHelper.Pi);

                // 👉 提高指数，让两端更“钝”，不尖
                shape = (float)Math.Pow(shape, 0.6f);

                // 👉 再稍微抬高尾部，避免收成尖点
                float tailLift = 0.25f; // 越大越不尖
                shape = MathHelper.Lerp(tailLift, 1f, shape);

                return baseWidth * shape;
            }

            // ===== 颜色函数（保持你原本）=====
            Color ColorFunc(float t, Vector2 v)
            {
                int idx = (int)(t * (palette.Length - 1));
                idx = Utils.Clamp(idx, 0, palette.Length - 1);

                Color c = palette[idx];
                c *= (1f - t) * Projectile.Opacity * 1.2f;
                c.A = 0;
                return c;
            }

            // ===== 偏移（稍微抬高）=====
            Vector2 offsetFunc(float t, Vector2 v)
            {
                return Projectile.Size * 0.5f
                    + Projectile.velocity.SafeNormalize(Vector2.Zero) * 4f;
            }

            // ===== 渲染 =====
            PrimitiveRenderer.RenderTrail(
                shiftedOldPos,
                new PrimitiveSettings(
                    WidthFunc,
                    ColorFunc,
                    offsetFunc,
                    shader: GameShaders.Misc["CalamityMod:SideStreakTrail"]
                ),
                60
            );

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0f
            );

            return false;
        }

        public override void AI()
        {
            lifeTimer++;

            Projectile.velocity *= 1.03f; // 更狂一点

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            Lighting.AddLight(Projectile.Center, new Vector3(0.8f, 0.6f, 0.1f));

            Vector2 head = Projectile.Center + forward * 20f;

            float t = Main.GameUpdateCount * 0.2f;
            float swing = (float)Math.Sin(t * 2.5f) * 8f;


            {

                // ===== 在弹幕体积内随机取点 =====
                Vector2 GetRandomPosInProjectile()
                {
                    return Projectile.Center + new Vector2(
                        Main.rand.NextFloat(-Projectile.width * 0.5f, Projectile.width * 0.5f),
                        Main.rand.NextFloat(-Projectile.height * 0.5f, Projectile.height * 0.5f)
                    );
                }

                // ===== 主体爆裂火焰（范围化 + 频率↑）=====
                for (int i = 0; i < 6; i++) // 原来2 → 6
                {
                    Vector2 pos = GetRandomPosInProjectile();

                    Vector2 vel =
                        (-forward).RotatedByRandom(0.5f)
                        + right * Main.rand.NextFloat(-1.2f, 1.2f);

                    vel *= Main.rand.NextFloat(1f, 4f);

                    SquishyLightParticle p = new SquishyLightParticle(
                        pos,
                        vel,
                        Main.rand.NextFloat(0.6f, 1.2f),
                        Main.rand.NextBool() ? new Color(255, 230, 120) : new Color(255, 150, 60),
                        Main.rand.Next(16, 26),
                        1f,
                        Main.rand.NextFloat(1.4f, 2.2f)
                    );
                    GeneralParticleHandler.SpawnParticle(p);
                }

                // ===== 火花喷射（范围化 + 频率↑）=====
                if (lifeTimer % 1 == 0) // 原来 %2 → 每帧
                {
                    for (int i = 0; i < 5; i++) // 原来2 → 5
                    {
                        Vector2 pos = GetRandomPosInProjectile();

                        Vector2 vel =
                            (-forward).RotatedByRandom(0.8f)
                            + right * Main.rand.NextFloat(-1.5f, 1.5f);

                        vel *= Main.rand.NextFloat(2f, 7f);

                        Dust d = Dust.NewDustPerfect(
                            pos,
                            6,
                            vel,
                            0,
                            new Color(255, 180, 60),
                            Main.rand.NextFloat(1.3f, 2.0f)
                        );
                        d.noGravity = true;
                    }
                }

                // ===== 中轴爆点（改为体积爆点）=====
                if (Main.rand.NextBool(1)) // 原来 NextBool(2) → 更频繁
                {
                    for (int i = 0; i < 3; i++) // 原来1 → 3
                    {
                        Vector2 pos = GetRandomPosInProjectile();

                        Vector2 vel =
                            (-forward).RotatedByRandom(0.5f)
                            + right * Main.rand.NextFloat(-1f, 1f);

                        vel *= Main.rand.NextFloat(2f, 6f);

                        PointParticle spark = new PointParticle(
                            pos,
                            vel,
                            false,
                            Main.rand.Next(10, 16),
                            Main.rand.NextFloat(1f, 1.3f),
                            new Color(255, 220, 100)
                        );
                        GeneralParticleHandler.SpawnParticle(spark);
                    }
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 pos = Projectile.Center;

            // ===== 更狂野爆炸 =====
            for (int i = 0; i < 22; i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 8f);

                GlowSparkParticle spark = new GlowSparkParticle(
                    pos,
                    vel,
                    false,
                    Main.rand.Next(10, 16),
                    0.12f,
                    Main.rand.NextBool() ? new Color(255, 240, 120) : new Color(255, 120, 60),
                    new Vector2(2.2f, 0.5f),
                    true,
                    false,
                    1
                );
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // 爆炸弹幕（保留）
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                pos,
                Vector2.Zero,
                ModContent.ProjectileType<FuckYou>(),
                (int)(Projectile.damage * 1.15f),
                Projectile.knockBack,
                Projectile.owner
            );

            //Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            // 清空
        }
    }
}