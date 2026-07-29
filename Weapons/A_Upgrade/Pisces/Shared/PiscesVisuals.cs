using System;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.Shared
{
    /// <summary>
    /// 双鱼座的统一视觉语言——一切 bloom / additive 纹理都在这里切换并恢复 SpriteBatch（恢复 AlphaBlend 是硬性要求）。
    /// 刻意维持“暴躁硫火 vs 冷静光学”的二分：
    ///   左键（硫火）：硫火红 × 橙红余烬 × 硫黄黄绿烟 × 虚空黑（沿用灾厄 Drizzlefish/Brimstone 家族）。
    ///   右键（光学）：蓝白核心 → 青蓝主带 → 金白薄边（极光），联动链用细青白，终点爆破回到橙红。
    /// 禁止把紫/激光/黑内层带进硫火，也禁止把红橙火尘带进光学激光。
    /// </summary>
    internal static class PiscesVisuals
    {
        // ===== 硫火（左键）=====
        public static readonly Color BrimstoneRed = new(255, 63, 52);
        public static readonly Color EmberOrange = new(255, 138, 46);
        public static readonly Color SulfurGreen = new(176, 206, 74);   // 硫黄黄绿烟
        public static readonly Color VoidBlack = new(24, 10, 14);

        // ===== 光学（右键 / 激光）=====
        public static readonly Color AuroraWhite = new(224, 244, 255);   // 蓝白核心
        public static readonly Color AuroraCyan = new(96, 206, 255);     // 青蓝主带
        public static readonly Color GoldWhite = new(255, 240, 196);     // 金白薄边
        public static readonly Color ChainCyan = new(158, 232, 255);     // 联动细青白链

        /// <summary>按 0..1 在青蓝↔蓝白之间取光学主色。</summary>
        public static Color AuroraLerp(float t) => Color.Lerp(AuroraCyan, AuroraWhite, MathHelper.Clamp(t, 0f, 1f));
        /// <summary>按 0..1 在橙红↔硫火红之间取硫火主色。</summary>
        public static Color BrimLerp(float t) => Color.Lerp(EmberOrange, BrimstoneRed, MathHelper.Clamp(t, 0f, 1f));
        public static Color ToWhite(Color c, float t) => Color.Lerp(c, Color.White, MathHelper.Clamp(t, 0f, 1f));
        public static Color ToGold(Color c, float t) => Color.Lerp(c, GoldWhite, MathHelper.Clamp(t, 0f, 1f));

        // ===== 签名尘 =====
        public static int BrimstoneDust => ModContent.DustType<BrimstoneFlame>();
        public static int HolyDust => ModContent.DustType<HolyFireDust>();
        public const int FireworkDust = 278;   // 可染色烟花尘

        // ===== 通用贴图 =====
        public static Asset<Texture2D> BloomCircle => ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
        public static Asset<Texture2D> SmallBloom => ModContent.Request<Texture2D>("CalamityMod/Particles/SmallBloom");
        public static Asset<Texture2D> BloomLine => ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge");
        public static Asset<Texture2D> HollowRing => ModContent.Request<Texture2D>("CalamityMod/Particles/HollowCircleHardEdge");
        public static Asset<Texture2D> LargeBloom => ModContent.Request<Texture2D>("CalamityMod/Particles/LargeBloom");

        // ===== 批次管理（统一 Begin/Additive/Restore）=====
        public static void BeginAdditive(SpriteBatch sb)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public static void EndAdditive(SpriteBatch sb)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        // ===== 曲线词汇表 =====
        public static float ShockwaveExpand(float t) { t = MathHelper.Clamp(t, 0f, 1f); float u = 1f - t; return 1f - u * u * u * u; }
        public static float BurstFade(float t) => (float)Math.Cos(MathHelper.Clamp(t, 0f, 1f) * MathHelper.PiOver2);
        public static float GatherPulse(float t) => (float)Math.Sin(MathHelper.Clamp(t, 0f, 1f) * MathHelper.Pi);
        public static float SharpFade(float t) { t = MathHelper.Clamp(t, 0f, 1f); return 1f - t * t * t; }

        // ===== 复用绘制（均在加色批次内调用）=====
        /// <summary>把一段世界线画成发光光带（用于联动链线 / 激光外发光）。</summary>
        public static void DrawBeamSegment(SpriteBatch sb, Vector2 worldStart, Vector2 worldEnd, Color color, float thickness)
        {
            Texture2D tex = BloomLine.Value;
            Vector2 delta = worldEnd - worldStart;
            float length = delta.Length();
            if (length < 0.5f) return;
            float rotation = delta.ToRotation() + MathHelper.PiOver2;
            Vector2 origin = new(tex.Width * 0.5f, tex.Height);
            Vector2 scale = new(thickness / tex.Width, length / tex.Height);
            sb.Draw(tex, worldStart - Main.screenPosition, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
        }

        /// <summary>多层生长辉光球（逐层向白偏、逐层放大）。</summary>
        public static void DrawEnergyOrb(SpriteBatch sb, Vector2 worldCenter, float radius, Color baseColor, float opacity, Vector2 squash)
        {
            Texture2D bloom = BloomCircle.Value;
            Vector2 pos = worldCenter - Main.screenPosition;
            Vector2 origin = bloom.Size() * 0.5f;
            for (int i = 0; i < 6; i++)
            {
                Color c = ToWhite(baseColor, i * 0.1f) with { A = 0 } * (opacity * (0.7f - i * 0.07f));
                float s = radius / bloom.Width * (0.5f + i * 0.15f);
                sb.Draw(bloom, pos, null, c, 0f, origin, new Vector2(s) * squash, SpriteEffects.None, 0f);
            }
        }

        /// <summary>空心收束环（外主色、内偏白反向缩小）。</summary>
        public static void DrawRing(SpriteBatch sb, Vector2 worldCenter, float radius, float rotation, Color color, float opacity)
        {
            Texture2D tex = HollowRing.Value;
            Vector2 pos = worldCenter - Main.screenPosition;
            Vector2 origin = tex.Size() * 0.5f;
            float baseScale = radius * 2f / tex.Width;
            sb.Draw(tex, pos, null, color with { A = 0 } * opacity, rotation, origin, baseScale, SpriteEffects.None, 0f);
            sb.Draw(tex, pos, null, ToWhite(color, 0.6f) with { A = 0 } * (opacity * 0.7f), -rotation, origin, baseScale * 0.8f, SpriteEffects.None, 0f);
        }

        /// <summary>单张辉光贴图快捷绘制（加色批次内）。</summary>
        public static void DrawBloom(SpriteBatch sb, Vector2 worldCenter, float scale, Color color, float opacity)
        {
            Texture2D tex = BloomCircle.Value;
            sb.Draw(tex, worldCenter - Main.screenPosition, null, color with { A = 0 } * opacity, 0f, tex.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }
    }
}
