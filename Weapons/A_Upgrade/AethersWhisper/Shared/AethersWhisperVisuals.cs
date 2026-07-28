using System;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Shared
{
    /// <summary>
    /// 以太之低语的统一视觉语言——刻意与「嘉登军械库 / Draedon's Arsenal」同频。
    /// 军械库有五种能量签名（<see cref="CalamityMod.Effects.ArsenalEffects"/>）：等离子绿 / 激光红 /
    /// 脉冲紫 / 电弧青 / 高斯金。以太之低语 = 脉冲紫(#A65CFF) × 电弧青(#80F3F2) × 珠白核心，
    /// 因此它天生就属于这个大家庭：用同一批签名尘（SquashDust/UnstableDust/SquareDust/军械库烟花尘）、
    /// 同一套 CustomSpark/VelChangingSpark 硬光火花、同一种多层生长辉光球与「色→白」渐变。
    /// </summary>
    internal static class AethersWhisperVisuals
    {
        // ===== 主色（脉冲紫 × 电弧青 × 珠白）=====
        public static readonly Color PearlWhite = new(246, 255, 255);
        public static readonly Color ShimmerCyan = new(128, 243, 242);   // ≈ Arsenal 电弧青
        public static readonly Color AetherPurple = new(170, 92, 255);   // ≈ Arsenal 脉冲紫
        public static readonly Color VoidBlue = new(21, 21, 44);

        /// <summary>按 0..1 在青↔紫之间取以太主色。</summary>
        public static Color Lerp(float t) => Color.Lerp(ShimmerCyan, AetherPurple, MathHelper.Clamp(t, 0f, 1f));
        /// <summary>向珠白偏移的高光色（军械库标志性「色→白」渐变）。</summary>
        public static Color ToWhite(Color c, float t) => Color.Lerp(c, PearlWhite, MathHelper.Clamp(t, 0f, 1f));

        // ===== 军械库签名尘 =====
        public static int PulseDust => ModContent.DustType<SquashDustHollow>();   // 脉冲：空心方尘
        public static int ElectricDust => ModContent.DustType<UnstableDust>();     // 电弧：不稳定尘
        public static int SquashDust => ModContent.DustType<CalamityMod.Dusts.SquashDust>(); // 军械库通用压扁尘
        public static int HardLightDust => ModContent.DustType<SquareDust>();      // 硬光方块（晶片碎屑）
        public const int ArsenalFireworkDust = 278;                                // 可染色烟花尘，军械库最常混用

        // ===== 通用贴图 =====
        public static Asset<Texture2D> BloomCircle => ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
        public static Asset<Texture2D> SmallBloom => ModContent.Request<Texture2D>("CalamityMod/Particles/SmallBloom");
        public static Asset<Texture2D> BloomLine => ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLine");
        public static Asset<Texture2D> HollowRing => ModContent.Request<Texture2D>("CalamityMod/Particles/HollowCircleHardEdge");
        /// <summary>军械库同款拉宽脉冲环（横向 stretch）。</summary>
        public const string PulseRingAltTex = "CalamityMod/Particles/HighResHollowCircleHardEdgeAlt";
        public static Asset<Texture2D> PulseRingAlt => ModContent.Request<Texture2D>(PulseRingAltTex);
        /// <summary>硬光方块能量碎片（脉冲步枪/高斯都用它做火花）。</summary>
        public const string GlowSquareTex = "CalamityMod/Particles/GlowSquareFading";
        public const string DualTrailTex = "CalamityMod/Particles/DualTrail";
        // 星芒核心贴图（混用：灾厄 SimpleStar/HalfStar 十字长臂 + 本项目 KsTexture 512² 星）。
        public static Asset<Texture2D> CoreStar => ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/star_06");
        public static Asset<Texture2D> CoreFlower => ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/star_08");
        public static Asset<Texture2D> CoreSimpleStar => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar");
        public static Asset<Texture2D> CoreHalfStar => ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar");
        public static Asset<Texture2D> CoreOrbitStar => ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/star_04");

        // ===== 批次管理 =====
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

        // ===== 曲线词汇表（第 0 篇）=====
        public static float ShockwaveExpand(float t) { t = MathHelper.Clamp(t, 0f, 1f); float u = 1f - t; return 1f - u * u * u * u; }
        public static float BurstFade(float t) => (float)Math.Cos(MathHelper.Clamp(t, 0f, 1f) * MathHelper.PiOver2);
        public static float GatherPulse(float t) => (float)Math.Sin(MathHelper.Clamp(t, 0f, 1f) * MathHelper.Pi);
        public static float SharpFade(float t) { t = MathHelper.Clamp(t, 0f, 1f); return 1f - t * t * t; }

        public static bool CanSpawnGroup(int count) => GeneralParticleHandler.FreeSpacesAvailable() >= count;

        // ===== 复用绘制 =====
        /// <summary>用 BloomLine 把一段世界线画成发光光带（加色批次内）。</summary>
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

        /// <summary>军械库同款多层生长辉光球（7 层，逐层向白偏、逐层放大）。加色批次内调用。</summary>
        public static void DrawEnergyOrb(SpriteBatch sb, Vector2 worldCenter, float radius, Color baseColor, float opacity, Vector2 squash)
        {
            Texture2D bloom = BloomCircle.Value;
            Vector2 pos = worldCenter - Main.screenPosition;
            Vector2 origin = bloom.Size() * 0.5f;
            for (int i = 0; i < 7; i++)
            {
                Color c = ToWhite(baseColor, i * 0.09f) with { A = 0 } * (opacity * (0.7f - i * 0.06f));
                float s = radius / bloom.Width * (0.5f + i * 0.14f);
                sb.Draw(bloom, pos, null, c, 0f, origin, new Vector2(s) * squash, SpriteEffects.None, 0f);
            }
        }

        /// <summary>空心收束环：外青内紫（HollowCircleHardEdge）。加色批次内调用。</summary>
        public static void DrawShimmerRing(SpriteBatch sb, Vector2 worldCenter, float radius, float rotation, float opacity)
        {
            Texture2D tex = HollowRing.Value;
            Vector2 pos = worldCenter - Main.screenPosition;
            Vector2 origin = tex.Size() * 0.5f;
            float baseScale = radius * 2f / tex.Width;
            sb.Draw(tex, pos, null, ShimmerCyan with { A = 0 } * opacity, rotation, origin, baseScale, SpriteEffects.None, 0f);
            sb.Draw(tex, pos, null, AetherPurple with { A = 0 } * (opacity * 0.75f), -rotation, origin, baseScale * 0.82f, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// 弓/枪体上的能量星芒核心（BF 同架构）：3 层 BloomCircle 底光 + 自转 star_06 + 自转 star_08。
        /// 常驻低功率让武器「活着」，蓄力/开火时功率抬高。须在加色批次内调用。
        /// </summary>
        public static void DrawStarCore(SpriteBatch sb, Vector2 worldCenter, Vector2 aimDir, float power, float purpleMix, float phaseKick)
        {
            if (Main.dedServ) return;
            Texture2D bloom = BloomCircle.Value;
            Texture2D simpleStar = CoreSimpleStar.Value;
            Texture2D halfStar = CoreHalfStar.Value;
            Texture2D orbit = CoreOrbitStar.Value;
            Texture2D flower = CoreFlower.Value;
            Vector2 core = worldCenter - Main.screenPosition;
            float charge = MathHelper.Clamp(power, 0f, 1f);
            float time = Main.GlobalTimeWrappedHourly;

            // 紫为主、青点缀（用户要求整体偏紫）。
            Color purple = AetherPurple;
            Color cyan = ShimmerCyan;
            Color starWhite = ToWhite(purple, 0.55f);

            // 3 层底光
            for (int i = 0; i < 3; i++)
            {
                float rot = time * 0.4f * (i % 2 == 0 ? 1f : -1f);
                Vector2 scale = new Vector2(0.15f, 0.14f) * (0.9f + charge * 0.9f) * (1f - 0.15f * i);
                sb.Draw(bloom, core, null, ToWhite(purple, charge * 0.5f) with { A = 0 } * ((0.15f + charge * 0.24f) * (1f - 0.2f * i)),
                    rot, bloom.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }

            // SimpleStar 十字星（画两次、相差 45° = 八芒），紫白、随蓄力放大自转（SHPC 同款）。
            float starRot = aimDir.ToRotation() + time * 0.5f + phaseKick;
            float starScale = 0.035f + charge * 0.06f;
            for (int s = 0; s < 2; s++)
                sb.Draw(simpleStar, core, null, starWhite with { A = 0 } * (0.34f + charge * 0.42f),
                    starRot + s * MathHelper.PiOver4, simpleStar.Size() * 0.5f,
                    new Vector2(0.32f + charge * 0.12f, 1.2f + charge * 0.5f) * starScale, SpriteEffects.None, 0f);

            // HalfStar 长臂（十字两条，青白），比十字星慢一点自转，制造干涉。
            float rayScale = 0.045f + charge * 0.05f;
            for (int s = 0; s < 2; s++)
                sb.Draw(halfStar, core, null, ToWhite(cyan, 0.3f) with { A = 0 } * (0.22f + charge * 0.3f),
                    starRot * 0.6f + s * MathHelper.PiOver2, halfStar.Size() * 0.5f,
                    new Vector2(0.14f, 1f) * rayScale, SpriteEffects.None, 0f);

            // 一圈绕核心公转的小星星（KsTexture star_04，512²），紫——即「一堆小星星旋转发光」。
            int n = 5;
            float orbitR = (13f + charge * 12f);
            for (int i = 0; i < n; i++)
            {
                float a = time * 1.3f + MathHelper.TwoPi * i / n + phaseKick;
                Vector2 p = core + a.ToRotationVector2() * orbitR;
                sb.Draw(orbit, p, null, ToWhite(purple, 0.2f) with { A = 0 } * (0.3f + charge * 0.35f),
                    a * 2f, orbit.Size() * 0.5f, (0.006f + charge * 0.004f), SpriteEffects.None, 0f);
            }

            // star_08 花瓣慢自转垫底
            sb.Draw(flower, core, null, ToWhite(purple, 0.35f) with { A = 0 } * (0.24f + charge * 0.22f),
                time * 0.85f + MathHelper.PiOver2, flower.Size() * 0.5f, (0.7f + charge * 0.5f) * 0.15f, SpriteEffects.None, 0f);
        }
    }
}
