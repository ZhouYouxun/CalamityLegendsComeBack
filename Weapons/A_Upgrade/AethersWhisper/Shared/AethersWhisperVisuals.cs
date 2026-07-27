using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Shared
{
    /// <summary>
    /// 以太之低语的统一视觉语言（第 5.1 / 5.4 节）：冷青、珠白、深紫、蓝黑三层关系，
    /// 以及几个复用的程序绘制工具。禁止亮红/橙火/黄电/彩虹/大面积纯粉。
    /// 绘制顺序合同：AlphaBlend 打底（实体/轮廓）→ Additive 只用于白核与冷青细描边与 1 tick bloom。
    /// 加色发光一律用 with{A=0}，避免黑底出框（见项目通病记录）。
    /// </summary>
    internal static class AethersWhisperVisuals
    {
        /// <summary>微光核心 #F6FFFF 珠白——激光/炮弹最中心的 1–3 px，仅关键瞬间出现。</summary>
        public static readonly Color PearlWhite = new(246, 255, 255);
        /// <summary>微光薄膜 #80F3F2 冷青——蓄力环、反射环、右键外缘。</summary>
        public static readonly Color ShimmerCyan = new(128, 243, 242);
        /// <summary>以太裂隙 #A65CFF 深紫——枪体环芯、炮弹外壳、晶片边缘与命中轮廓。</summary>
        public static readonly Color AetherPurple = new(166, 92, 255);
        /// <summary>暗部 #15152C 蓝黑——金属阴影与坍缩中心；不能被发光层填满。</summary>
        public static readonly Color VoidBlue = new(21, 21, 44);

        // 常用通用贴图（均确认存在于 CalamityMod，避免运行时加载失败）。
        public static Asset<Texture2D> BloomCircle => ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
        public static Asset<Texture2D> SmallBloom => ModContent.Request<Texture2D>("CalamityMod/Particles/SmallBloom");
        public static Asset<Texture2D> BloomLine => ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLine");
        /// <summary>硬边空心圆——用作六边形收束环/反射符号/坍缩环的占位（正式资产见文档 5.2，须新绘替换）。</summary>
        public static Asset<Texture2D> HollowRing => ModContent.Request<Texture2D>("CalamityMod/Particles/HollowCircleHardEdge");

        /// <summary>切到加色批次（顶层白核 / 冷青描边 / bloom 用）。</summary>
        public static void BeginAdditive(SpriteBatch sb)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>恢复默认 AlphaBlend 批次（第 5.4 节：加色后必须恢复）。</summary>
        public static void EndAdditive(SpriteBatch sb)
        {
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// 用 BloomLine 把一条世界线段绘制成一束发光光带（起点→终点），thickness 为像素粗细。
        /// 需在调用方已选定的批次内使用。
        /// </summary>
        public static void DrawBeamSegment(SpriteBatch sb, Vector2 worldStart, Vector2 worldEnd, Color color, float thickness)
        {
            Texture2D tex = BloomLine.Value;
            Vector2 delta = worldEnd - worldStart;
            float length = delta.Length();
            if (length < 0.5f)
                return;

            float rotation = delta.ToRotation() + MathHelper.PiOver2; // BloomLine 竖直朝上
            Vector2 origin = new(tex.Width * 0.5f, tex.Height); // 底端为起点
            Vector2 scale = new(thickness / tex.Width, length / tex.Height);
            sb.Draw(tex, worldStart - Main.screenPosition, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
        }

        /// <summary>画一枚（占位六边形）收束环：外冷青、内深紫。加色批次内调用。</summary>
        public static void DrawShimmerRing(SpriteBatch sb, Vector2 worldCenter, float radius, float rotation, float opacity)
        {
            Texture2D tex = HollowRing.Value;
            Vector2 pos = worldCenter - Main.screenPosition;
            Vector2 origin = tex.Size() * 0.5f;
            float baseScale = radius * 2f / tex.Width;
            sb.Draw(tex, pos, null, ShimmerCyan with { A = 0 } * opacity, rotation, origin, baseScale, SpriteEffects.None, 0f);
            sb.Draw(tex, pos, null, AetherPurple with { A = 0 } * (opacity * 0.75f), -rotation, origin, baseScale * 0.82f, SpriteEffects.None, 0f);
        }
    }
}
