using CalamityMod.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentSolar_Spear : ModProjectile
    {
        // ===== 自定义计数器（禁止用localAI）=====
        private int hitCount;
        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;

            Texture2D tex = ModContent.Request<Texture2D>(
                Projectile.ModProjectile.Texture
            ).Value;

            Vector2 origin = tex.Size() * 0.5f;

            // ======== 太阳黑子橙色调色盘 ========
            Color[] firePalette = new Color[]
            {
        new Color(255, 200, 80),   // 金
        new Color(255, 150, 40),   // 橙
        new Color(255, 100, 30),   // 深橙
        new Color(255, 60, 20),    // 红橙
            };

            // ======== EXO 风格：能量丝带拖尾（Primitive Trail） ========
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
                     DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            // === 定义宽度函数 ===
            float PrimitiveWidthFunction(float completionRatio, Vector2 vertexPos)
            {
                float w = Projectile.width * 3.55f;
                w *= MathHelper.SmoothStep(
                    0.5f,
                    1.0f,
                    Utils.GetLerpValue(0f, 0.25f, completionRatio, true)
                );
                return w;
            }

            // === 定义颜色函数 ===
            Color PrimitiveTrailColor(float completionRatio, Vector2 vertexPos)
            {
                Color c = firePalette[
                    (int)(completionRatio * firePalette.Length) % firePalette.Length
                ];

                c *= Projectile.Opacity * (1f - completionRatio);

                float speedBoost =
                    Utils.GetLerpValue(1f, 6f, Projectile.velocity.Length(), true);

                c *= speedBoost;

                c.A = 0;
                return c;
            }


            // === 将 oldPos 整体往前移动到“弹幕前端” ===
            Vector2 frontOffset = Projectile.velocity.SafeNormalize(Vector2.Zero) * (Projectile.width * 1.5f);

            // 创建一个新的数组存前推后的 oldPos
            Vector2[] shiftedOldPos = new Vector2[Projectile.oldPos.Length];
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                shiftedOldPos[i] = Projectile.oldPos[i] + frontOffset;
            }


            // === 偏移：让丝带稍微抬起（增强立体感）===
            Vector2 PrimitiveOffsetFunction(float t, Vector2 vertexPos)
            {
                return Projectile.Size * 0.5f + Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.scale * 2f;
            }

            // === 绘制能量丝带（关键）=====
            GameShaders.Misc["CalamityMod:SideStreakTrail"].UseImage1("Images/Misc/Perlin");

            PrimitiveRenderer.RenderTrail(
                shiftedOldPos,
                new(
                    PrimitiveWidthFunction,
                    PrimitiveTrailColor,
                    PrimitiveOffsetFunction,
                    shader: GameShaders.Misc["CalamityMod:SideStreakTrail"]
                ),
                60
            );

            // ======== 回到正常绘图（主体绘制） ========
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                     DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            // === 主体绘制 ===
            {
                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                sb.Draw(tex, drawPos, null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            // ======== 第二层：普通虚化拖尾（oldPos-based fade trail） ========
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;

                // 透明度衰减
                float fade = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;

                Color c = new Color(255, 160, 60) * 0.35f * fade; // 柔和橙色虚光
                c.A = 0;

                float scale = Projectile.scale * (0.6f + fade * 0.4f);

                Main.spriteBatch.Draw(
                    tex,
                    pos,
                    null,
                    c,
                    Projectile.rotation,
                    tex.Size() * 0.5f,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }


            return false;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;

            Projectile.friendly = true;
            Projectile.hostile = false;

            Projectile.penetrate = 2; // 穿透两次
            Projectile.timeLeft = 180;

            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;

            Projectile.DamageType = DamageClass.Ranged;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;

            Projectile.extraUpdates = 1;
        }

        // ================= OnSpawn =================
        public override void OnSpawn(IEntitySource source)
        {
        }

        // ================= AI =================
        public override void AI()
        {
            // 基础旋转（长矛感）
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            // ===== 日耀飞行光效 =====
            if (Main.rand.NextBool(2))
            {
                Vector2 dir = -Projectile.velocity.SafeNormalize(Vector2.UnitX);

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.SolarFlare,
                    dir * Main.rand.NextFloat(2f, 6f),
                    0,
                    default,
                    Main.rand.NextFloat(1.2f, 1.8f)
                );
                dust.noGravity = true;
            }

            // 微量橙色火焰补层
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Torch,
                    Main.rand.NextVector2Circular(1.5f, 1.5f),
                    0,
                    Color.Orange,
                    Main.rand.NextFloat(1.0f, 1.5f)
                );
                d.noGravity = true;
            }

            // 微加速（有一点“日耀冲刺感”）
            Projectile.velocity *= 1.01f;
        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hitCount++;

            // ===== 日耀爆炸（每次命中触发）=====
            SpawnSolarExplosion();

            // 达到穿透次数后强制结束
            if (hitCount >= 2)
            {
                Projectile.Kill();
            }
        }

        // ================= OnKill =================
        public override void OnKill(int timeLeft)
        {
            // 死亡也补一个爆炸
            SpawnSolarExplosion();
        }

        // ================= TileCollide =================
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return true;
        }

        // ================= 爆炸特效 =================
        private void SpawnSolarExplosion()
        {
            Vector2 center = Projectile.Center;

            // ===== 第一层：规则环（16向）=====
            for (int i = 0; i < 16; i++)
            {
                float angle = MathHelper.TwoPi * i / 16f;
                Vector2 dir = angle.ToRotationVector2();

                Dust d = Dust.NewDustPerfect(
                    center + dir * 6f,
                    DustID.Torch,
                    dir * 4f,
                    0,
                    Color.Orange,
                    1.6f
                );
                d.noGravity = true;
            }

            // ===== 第二层：SolarFlare 爆散 =====
            for (int i = 0; i < 24; i++)
            {
                Dust d = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(12f, 12f),
                    DustID.SolarFlare,
                    Main.rand.NextVector2Circular(6f, 6f),
                    0,
                    default,
                    Main.rand.NextFloat(1.5f, 2.3f)
                );
                d.noGravity = true;
                d.fadeIn = 0.5f;
            }

            // ===== 第三层：冲击火花 =====
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 7f);

                Dust spark = Dust.NewDustPerfect(
                    center,
                    DustID.Torch,
                    vel,
                    0,
                    Color.Yellow,
                    1.2f
                );
                spark.noGravity = true;
            }

            // ===== 局部光照 =====
            Lighting.AddLight(center, Color.Orange.ToVector3() * 0.8f);
        }
    }
}