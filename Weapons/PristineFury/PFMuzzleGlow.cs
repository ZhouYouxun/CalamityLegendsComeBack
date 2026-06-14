using CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    // 枪口星芒特效的绘制逻辑统一放在这里，便于快速调整贴图和参数。
    // 在 NewLegendPristineFuryHoldOut.DrawMuzzleGlow 中调用 PFMuzzleGlow.Draw。
    internal static class PFMuzzleGlow
    {
        // ── 贴图路径（换贴图只需改这里）────────────────────────────────────
        internal const string BloomTexPath = "CalamityMod/Particles/BloomCircle";
        internal const string StarTexPath  = "CalamityMod/Particles/HalfStar";
        internal const string MagicTexPath = "CalamityLegendsComeBack/Texture/KsTexture/magic_03";

        // ── 绘制参数（调整视觉强度只需改这里）──────────────────────────────
        internal const float BaseScale    = 1.5f;
        internal const float BloomOpacity = 0.55f;
        internal const float MagicOpacity = 0.34f;
        internal const float StarOpacity  = 0.62f;
        internal const int   StarArmCount = 4;
        internal const float StarSpinBase = 1.1f;
        internal const float StarSpinStep = 0.15f;

        /// <summary>
        /// 绘制枪口星芒。必须在叠加混合模式下调用（方法内自行开关）。
        /// </summary>
        /// <param name="drawCenter">枪口中心（屏幕坐标，已减 Main.screenPosition）</param>
        /// <param name="aimDirection">瞄准方向单位向量</param>
        /// <param name="rotation">武器旋转角</param>
        /// <param name="color">主题色（A=0，已乘以功率）</param>
        /// <param name="power">当前功率 0~1</param>
        internal static void Draw(Vector2 drawCenter, Vector2 aimDirection, float rotation, Color color, float power)
        {
            if (power <= 0.02f || Main.dedServ)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>(BloomTexPath).Value;
            Texture2D star  = ModContent.Request<Texture2D>(StarTexPath).Value;
            Texture2D magic = ModContent.Request<Texture2D>(MagicTexPath).Value;
            float t = Main.GlobalTimeWrappedHourly;

            PFLeftEffectRules.BeginAdditive();

            Main.EntitySpriteDraw(bloom, drawCenter, null,
                color * BloomOpacity, rotation,
                bloom.Size() * 0.5f,
                new Vector2(0.34f + power * 0.24f, 0.18f + power * 0.12f) * BaseScale,
                SpriteEffects.None, 0);

            Main.EntitySpriteDraw(magic, drawCenter + aimDirection * 4f, null,
                color * MagicOpacity, -rotation + t * 1.1f,
                magic.Size() * 0.5f,
                (0.05f + power * 0.05f) * BaseScale,
                SpriteEffects.None, 0);

            for (int i = 0; i < StarArmCount; i++)
            {
                float rot = rotation + MathHelper.PiOver4 * i + t * (StarSpinBase + i * StarSpinStep);
                Main.EntitySpriteDraw(star, drawCenter, null,
                    color * StarOpacity, rot,
                    star.Size() * 0.5f,
                    new Vector2(0.24f + power * 0.18f, 1.1f + power * 1.4f) * BaseScale,
                    SpriteEffects.None, 0);
            }

            PFLeftEffectRules.EndAdditive();
        }
    }
}
