using CalamityMod;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    internal class AshesofCalamity_Portal : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // ===== 资源 =====
        public static Asset<Texture2D> screamTex;

        // ===== 基础计时 =====
        private int lifeTimer;

        // ===== 粒子系统 =====
        private int bloomTimer;
        private int sparkTimer;

        private readonly List<BloomRing> ownedBloomRings = new();
        private readonly List<CritSpark> ownedCritSparks = new();

        private Vector2 lastCenter;

        // ===== 射击逻辑 =====
        private int shootTimer;

        public override void SetStaticDefaults()
        {
            if (!Main.dedServ)
            {
                screamTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/ScreamyFace", AssetRequestMode.AsyncLoad);
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 140;
            Projectile.height = 140;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 100;
            Projectile.Opacity = 0f;
        }

        public override void AI()
        {
            lifeTimer++;

            // ===== 淡入淡出（完全照搬）=====
            int fadeInTime = 15;
            int fadeOutTime = 15;

            if (lifeTimer <= fadeInTime)
                Projectile.Opacity = lifeTimer / (float)fadeInTime;
            else if (Projectile.timeLeft < fadeOutTime)
                Projectile.Opacity = Projectile.timeLeft / (float)fadeOutTime;
            else
                Projectile.Opacity = 1f;

            // ===== 呼吸 =====
            float pulsate = 1f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f);
            Projectile.scale = 0.42f * pulsate;

            // ===== 自转 =====
            Projectile.rotation += 0.03f;

            // ===== 固定在原地（替代原“绑定枪口”）=====
            Projectile.velocity = Vector2.Zero;

            if (!Main.dedServ)
            {
                // ===========================
                // 2）BloomRing：原地光晕（完全照搬）
                // ===========================
                bloomTimer++;
                if (bloomTimer % 3 == 0)
                {
                    Color ringColor = Color.Lerp(new Color(250, 50, 50), new Color(120, 10, 10), Main.rand.NextFloat(0.2f, 0.9f));
                    BloomRing bloomRing = new BloomRing(
                        Projectile.Center,
                        Vector2.Zero,
                        ringColor,
                        1.0f,
                        45
                    );
                    GeneralParticleHandler.SpawnParticle(bloomRing);
                    ownedBloomRings.Add(bloomRing);
                }

                // ===========================
                // 3）CritSpark结构（完整保留，但不生成）
                // ===========================
                sparkTimer++;
                if (sparkTimer % 5 == 0)
                {
                    int sparkCount = 30;
                    float baseAngle = Main.GlobalTimeWrappedHourly * 2.3f;

                    for (int i = 0; i < sparkCount; i++)
                    {
                        float progress = i / (float)sparkCount;

                        float angle = baseAngle
                                      + MathHelper.TwoPi * progress
                                      + 0.5f * (float)Math.Sin(3f * baseAngle + progress * MathHelper.TwoPi);

                        float radialSpeed = MathHelper.Lerp(4f, 8f,
                            (float)Math.Sin(progress * MathHelper.Pi) * 0.5f + 0.5f);

                        Vector2 dir = angle.ToRotationVector2();
                        Vector2 sparkVelocity = dir * radialSpeed;

                        Color startColor = Color.Lerp(new Color(250, 50, 50), new Color(120, 10, 10),
                            0.5f + 0.5f * (float)Math.Sin(baseAngle + progress * 6f));
                        Color endColor = Color.Lerp(startColor, Color.Black, 0.6f);

                        // 故意不生成（你原要求）
                    }
                }

                // ===========================
                // 4）粒子跟随模块（完整照搬）
                // ===========================
                if (lastCenter == Vector2.Zero)
                    lastCenter = Projectile.Center;

                Vector2 delta = Projectile.Center - lastCenter;
                lastCenter = Projectile.Center;

                // Bloom 跟随
                for (int i = ownedBloomRings.Count - 1; i >= 0; i--)
                {
                    BloomRing p = ownedBloomRings[i];

                    if (p.Time >= p.Lifetime)
                    {
                        ownedBloomRings.RemoveAt(i);
                        continue;
                    }

                    p.Position += delta;
                }

                // CritSpark 跟随（即使为空也必须保留结构）
                for (int i = ownedCritSparks.Count - 1; i >= 0; i--)
                {
                    CritSpark p = ownedCritSparks[i];

                    if (p.Time >= p.Lifetime)
                    {
                        ownedCritSparks.RemoveAt(i);
                        continue;
                    }

                    p.Position += delta;
                }
            }

            // 考灾厄之影的：BurningFireblast、CalamitousDart、CalamitousFireball

            // ===== 发射逻辑（保留你的）=====
            shootTimer++;

            float shootProgress = Utils.GetLerpValue(0f, 100f, lifeTimer, true);
            int currentDelay = (int)MathHelper.Lerp(18f, 4f, shootProgress);

            if (shootTimer >= currentDelay && Projectile.owner == Main.myPlayer)
            {
                shootTimer = 0;

                float radius = 2f * 16f;
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                Vector2 spawnPos = Projectile.Center + offset;
                Vector2 direction = GetRandomSoulDirection(spawnPos);
                float speed = Main.rand.NextFloat(7.5f, 12.5f) + shootProgress * 3f;

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPos,
                    direction * speed,
                    ModContent.ProjectileType<AshesofCalamity_Soul>(),
                    (int)(Projectile.damage * 0.75f),
                    0f,
                    Projectile.owner,
                    0f
                );
            }
        }

        private Vector2 GetRandomSoulDirection(Vector2 spawnPos)
        {
            NPC target = spawnPos.ClosestNPCAt(720f);

            if (target != null && Main.rand.NextBool(3))
                return (target.Center - spawnPos).SafeNormalize(Main.rand.NextVector2Unit());

            return Main.rand.NextVector2Unit();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();

            Effect shieldEffect = Filters.Scene["CalamityMod:HellBall"].GetShader().Shader;

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                shieldEffect, Main.GameViewMatrix.TransformationMatrix);

            shieldEffect.Parameters["time"].SetValue(Projectile.timeLeft / 60f * 0.24f);
            shieldEffect.Parameters["blowUpPower"].SetValue(3.2f);
            shieldEffect.Parameters["blowUpSize"].SetValue(0.4f);
            shieldEffect.Parameters["noiseScale"].SetValue(0.6f);
            shieldEffect.Parameters["shieldOpacity"].SetValue(Projectile.Opacity);
            shieldEffect.Parameters["shieldEdgeBlendStrenght"].SetValue(4f);

            Color edgeColor = Color.Black * Projectile.Opacity;
            Color shieldColor = new Color(250, 50, 50) * Projectile.Opacity;

            shieldEffect.Parameters["shieldColor"].SetValue(shieldColor.ToVector3());
            shieldEffect.Parameters["shieldEdgeColor"].SetValue(edgeColor.ToVector3());

            Vector2 pos = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.Draw(screamTex.Value, pos, null, Color.White * Projectile.Opacity, 0,
                screamTex.Size() * 0.5f, 0.715f * Projectile.scale, 0, 0);

            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D[] vortexTextures = new Texture2D[]
            {
                ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/Sun/fbmnoise2_003").Value,
                ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/Sun/fbmnoise2_004").Value,
                ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/Sun/fbmnoise2_005").Value,
                ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/Sun/fbmnoise2_006").Value,
                ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/window_04").Value
            };

            Texture2D centerTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/LargeBloom").Value;

            for (int i = 0; i < 10; i++)
            {
                float angle = MathHelper.TwoPi * i / 3f + Main.GlobalTimeWrappedHourly * MathHelper.TwoPi;

                Color outerColor = (i % 2 == 0) ? new Color(250, 50, 50) : new Color(120, 10, 10);
                Color drawColor = Color.Lerp(outerColor, Color.Black, i * 0.15f) * 0.6f;
                drawColor.A = 0;

                Vector2 drawPosition = Projectile.Center - Main.screenPosition;
                drawPosition += (angle + Main.GlobalTimeWrappedHourly * i / 16f).ToRotationVector2() * 6f;

                foreach (var texLayer in vortexTextures)
                {
                    Main.EntitySpriteDraw(texLayer, drawPosition, null, drawColor * Projectile.Opacity,
                        -angle + MathHelper.PiOver2, texLayer.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
                }
            }

            Main.EntitySpriteDraw(centerTexture, Projectile.Center - Main.screenPosition, null,
                Color.Black * Projectile.Opacity, Projectile.rotation,
                centerTexture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles,
            List<int> behindNPCs, List<int> behindProjectiles,
            List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }
    }
}
