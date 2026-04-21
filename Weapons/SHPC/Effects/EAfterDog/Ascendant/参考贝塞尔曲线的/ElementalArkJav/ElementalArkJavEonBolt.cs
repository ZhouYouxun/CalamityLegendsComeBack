using System;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;
using CalamityMod.Graphics.Primitives;

namespace CalamityMod.Projectiles.Melee
{
    public class ElementalArkJavEonBolt : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.NewWeapons.DPreDog";

        public override string Texture => "CalamityMod/Projectiles/Melee/GalaxiaBolt";

        public NPC target;
        public Player Owner => Main.player[Projectile.owner];

        public ref float Hue => ref Projectile.ai[0];
        public ref float HomingStrenght => ref Projectile.ai[1];

        Particle Head;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Head == null)
            {
                Head = new GenericSparkle(Projectile.Center, Vector2.Zero, Color.White, Main.hslToRgb(Hue, 100, 50), 1.2f, 2, 0.06f, 3, true);
                GeneralParticleHandler.SpawnParticle(Head);
            }
            else
            {
                Head.Position = Projectile.Center + Projectile.velocity * 0.5f;
                Head.Time = 0;
                Head.Scale += (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6) * 0.02f * Projectile.scale;
            }


            if (target == null)
                target = Projectile.Center.ClosestNPCAt(812f, true);

            else if (CalamityUtils.AngleBetween(Projectile.velocity, target.Center - Projectile.Center) < MathHelper.Pi) //Home in
            {
                float idealDirection = Projectile.AngleTo(target.Center);
                float updatedDirection = Projectile.velocity.ToRotation().AngleTowards(idealDirection, HomingStrenght);
                Projectile.velocity = updatedDirection.ToRotationVector2() * Projectile.velocity.Length() * 0.995f;
            }


            Lighting.AddLight(Projectile.Center, 0.75f, 1f, 0.24f);

            if (Main.rand.NextBool(2))
            {
                Particle smoke = new HeavySmokeParticle(Projectile.Center, Projectile.velocity * 0.5f, Color.Lerp(Color.DodgerBlue, Color.MediumVioletRed, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f)), 20, Main.rand.NextFloat(0.6f, 1.2f) * Projectile.scale, 0.28f, 0, false, 0, true);
                GeneralParticleHandler.SpawnParticle(smoke);

                if (Main.rand.NextBool(3))
                {
                    Particle smokeGlow = new HeavySmokeParticle(Projectile.Center, Projectile.velocity * 0.5f, Main.hslToRgb(Hue, 1, 0.7f), 15, Main.rand.NextFloat(0.4f, 0.7f) * Projectile.scale, 0.8f, 0, true, 0.05f, true);
                    GeneralParticleHandler.SpawnParticle(smokeGlow);
                }
            }


            // 前15帧不追踪，之后开始追踪敌人
            if (Projectile.ai[1] > 15)
            {


                // 状态机：0=追踪，1=转方块
                if (Projectile.localAI[1] == 0)
                {
                    // 正常追踪阶段（持续 60 帧）
                    Projectile.localAI[0]++;
                    if (Projectile.localAI[0] >= 60)
                    {
                        Projectile.localAI[0] = 0;
                        Projectile.localAI[1] = 1; // 切换至转方块模式
                        Projectile.localAI[2] = 0; // 重置已转次数
                    }
                    else
                    {
                        NPC target = Projectile.Center.ClosestNPCAt(1800);
                        if (target != null)
                        {
                            Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                            Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction * 12f, 0.08f);
                        }
                    }
                }
                else if (Projectile.localAI[1] == 1)
                {
                    // 转方块模式（转 4 次，每次间隔 5 帧）
                    Projectile.localAI[0]++;
                    if (Projectile.localAI[0] % 5 == 0 && Projectile.localAI[2] < 4)
                    {
                        // 执行左转 90°
                        Projectile.velocity = Projectile.velocity.RotatedBy(-MathHelper.PiOver2);

                        // 触发独特特效（使用两种内置特效）
                        // 特效 1：Dust (白色高速)
                        for (int i = 0; i < 8; i++)
                        {
                            Dust d = Dust.NewDustPerfect(
                                Projectile.Center,
                                DustID.GoldFlame,
                                Main.rand.NextVector2Circular(4f, 4f),
                                150,
                                Color.White,
                                Main.rand.NextFloat(0.8f, 1.2f)
                            );
                            d.noGravity = true;
                        }

                        // 特效 2：SparkParticle（白黄光点）
                        Particle spark = new SparkParticle(
                            Projectile.Center,
                            Main.rand.NextVector2Circular(2f, 2f),
                            false,
                            18,
                            Main.rand.NextFloat(0.5f, 0.8f),
                            Color.LightYellow
                        );
                        GeneralParticleHandler.SpawnParticle(spark);

                        Projectile.localAI[2]++; // 已转次数 +1
                    }

                    if (Projectile.localAI[2] >= 4)
                    {
                        // 完成 4 次转方块后切回追踪
                        Projectile.localAI[0] = 0;
                        Projectile.localAI[1] = 0;
                    }
                }



            }
            else
            {
                Projectile.ai[1]++;


                // 在前15帧做“横向微幅抖动”
                float frequency = 0.6f; // 振荡频率
                float amplitude = 2.5f; // 振幅像素
                float sinWave = (float)Math.Sin(Main.GameUpdateCount * frequency) * amplitude;

                Vector2 orthogonal = new Vector2(-Projectile.velocity.Y, Projectile.velocity.X).SafeNormalize(Vector2.Zero);
                Vector2 offset = orthogonal * sinWave * 0.1f; // 控制实际偏移量

                Projectile.position += offset;


            }
        }

        internal Color ColorFunction(float completionRatio, Vector2 vertexPos)
        {
            float fadeToEnd = MathHelper.Lerp(0.65f, 1f, (float)Math.Cos(-Main.GlobalTimeWrappedHourly * 3f) * 0.5f + 0.5f);
            float fadeOpacity = Utils.GetLerpValue(1f, 0.64f, completionRatio, true) * Projectile.Opacity;
            Color colorHue = Main.hslToRgb(Hue, 1, 0.8f);

            Color endColor = Color.Lerp(colorHue, Color.PaleTurquoise, (float)Math.Sin(completionRatio * MathHelper.Pi * 1.6f - Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);
            return Color.Lerp(Color.White, endColor, fadeToEnd) * fadeOpacity;
        }

        internal float WidthFunction(float completionRatio, Vector2 vertexPos)
        {
            float expansionCompletion = (float)Math.Pow(1 - completionRatio, 3);
            return MathHelper.Lerp(0f, 22 * Projectile.scale * Projectile.Opacity, expansionCompletion);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new(WidthFunction, ColorFunction, (completionRatio, vertexPos) => Projectile.Size * 0.5f, shader: GameShaders.Misc["CalamityMod:TrailStreak"]), 30);

            Texture2D texture = Request<Texture2D>("CalamityMod/Projectiles/Melee/GalaxiaBolt").Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.Lerp(lightColor, Color.White, 0.5f), Projectile.rotation, texture.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
