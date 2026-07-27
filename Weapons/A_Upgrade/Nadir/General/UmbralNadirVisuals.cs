using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General
{
    /// <summary>
    /// 冥蚀天底所有部件共用的视觉工具：分层黑洞事件视界、拉扯、双色火花、反向虚空尘，
    /// 以及矛尖/回旋共用的黑→绿着色器拖痕渲染。集中一处，保证左键/右键/回旋/奇点的家族质感统一。
    /// 黑核一律用透明底 SmallBloom（AlphaBlend 安全），绿晕一律 with{A=0} 加色。
    /// </summary>
    internal static class UmbralNadirVisuals
    {
        private static readonly Color Green = UmbralNadirPalette.MeldGreen;

        /// <summary>分层黑洞事件视界：扩张纯黑吸光核 + 双重黑核 + 绿色事件视界环 + 绿白闪。</summary>
        public static void EventHorizon(Vector2 center, float sizeMult, bool finale)
        {
            // 扩张纯黑吸光核（SmallBloom 透明底 → AlphaBlend 不出黑框）
            int blackLayers = finale ? 2 : 1;
            for (int i = 0; i < blackLayers; i++)
                GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, Color.Black,
                    "CalamityMod/Particles/SmallBloom", Vector2.One, Main.rand.NextFloat(-10f, 10f),
                    0.12f * sizeMult, (0.55f + i * 0.35f) * sizeMult, finale ? 62 : 42, false));

            // 冥思招牌"外绿内黑"双重坍缩核（DetailedExplosion）
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, Green with { A = 0 },
                Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.18f, 0.85f * sizeMult, finale ? 28 : 24),
                false, GeneralDrawLayer.AfterEverything);
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, Color.Black,
                Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.12f, 0.5f * sizeMult, finale ? 26 : 22, false));

            // 荧光绿事件视界环
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, Green,
                "CalamityMod/Particles/BloomRing", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, (finale ? 1.8f : 1.15f) * sizeMult, 18),
                false, GeneralDrawLayer.AfterEverything);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, Green,
                "CalamityMod/Particles/SoftRoundExplosion", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.22f * sizeMult, 28),
                false, GeneralDrawLayer.AfterEverything);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, Green with { A = 0 },
                "CalamityMod/Particles/LargeBloom", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.5f * sizeMult, 12),
                false, GeneralDrawLayer.AfterEverything);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, Color.White with { A = 0 },
                "CalamityMod/Particles/LargeBloom", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.36f * sizeMult, 12),
                false, GeneralDrawLayer.AfterEverything);
        }

        /// <summary>向外爆散的反向虚空尘 + 绿色烟花尘。</summary>
        public static void ImplosionDust(Vector2 center, float sizeMult)
        {
            for (int i = 0; i < (int)(14 * sizeMult); i++)
            {
                Dust vd = Dust.NewDustPerfect(center, ModContent.DustType<VoidDustInverted>());
                vd.noGravity = true;
                vd.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 9f) * sizeMult;
                vd.scale = Main.rand.NextFloat(1.3f, 2.1f) * sizeMult;
                vd.color = Green;
            }
            for (int i = 0; i < (int)(10 * sizeMult); i++)
            {
                Dust fd = Dust.NewDustPerfect(center, DustID.FireworksRGB);
                fd.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 10f) * sizeMult;
                fd.scale = Main.rand.NextFloat(0.8f, 1.2f);
                fd.color = Green;
            }
        }

        /// <summary>黑 GlowSpark2 + 绿 GlowSpark 双色火花爆裂。</summary>
        public static void MeldSparkBurst(Vector2 center, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 v = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, speed);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(center, v, "CalamityMod/Particles/GlowSpark2",
                    false, Main.rand.Next(14, 20), Main.rand.NextFloat(0.05f, 0.08f), Color.Black, new Vector2(0.6f, 1.3f), false));
                if (Main.rand.NextBool())
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(center, v * 0.8f, "CalamityMod/Particles/GlowSpark",
                        false, Main.rand.Next(12, 18), Main.rand.NextFloat(0.025f, 0.045f), Green, new Vector2(0.6f, 1.3f), true, false),
                        false, GeneralDrawLayer.AfterEverything);
            }
        }

        /// <summary>黑洞拉扯：把范围内非 Boss 敌人吸向 center（Boss 只轻微拖拽）。</summary>
        public static void PullNPCs(Vector2 center, float range, float strength)
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.friendly || npc.dontTakeDamage || npc.immortal)
                    continue;
                float dist = Vector2.Distance(npc.Center, center);
                if (dist > range || dist < 8f)
                    continue;
                float falloff = 1f - dist / range;
                float pull = strength * falloff * (npc.boss || npc.knockBackResist <= 0f ? 0.12f : 1f);
                npc.velocity += (center - npc.Center).SafeNormalize(Vector2.Zero) * pull;
            }
        }

        /// <summary>矛尖 / 回旋共用的黑→绿着色器拖痕（吞光的黑色矛尖残像）。points 为世界坐标。</summary>
        public static void RenderTipTrail(List<Vector2> points, float maxWidth, float scale, bool finale)
        {
            if (points == null || points.Count < 2)
                return;

            float wobble = finale ? 0.09f : 0f;
            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            PrimitiveRenderer.RenderTrail(points,
                new PrimitiveSettings(
                    (c, _) => MathHelper.Lerp(maxWidth, 3f, c) * (1f + wobble * (float)Math.Sin(c * 20f + Main.GlobalTimeWrappedHourly * 6f)) * scale,
                    (c, _) => Color.Lerp(Color.Lerp(Color.Black, UmbralNadirPalette.MeldGreenDeep, c * 0.7f), Green, Utils.GetLerpValue(0.78f, 1f, c)) * (1f - c * 0.85f),
                    (_, _) => Vector2.Zero, shader: GameShaders.Misc["CalamityMod:TrailStreak"]),
                points.Count);
        }

        /// <summary>按距离衰减的震屏。</summary>
        public static void ScreenShake(Vector2 source, float power)
        {
            float f = Utils.GetLerpValue(1500f, 0f, Vector2.Distance(source, Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower =
                Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * f);
        }
    }
}
