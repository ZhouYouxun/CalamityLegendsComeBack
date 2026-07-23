using System;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Visuals
{
    /// <summary>
    /// 庇护之刃「亵渎圣火」统一视觉体系。
    ///
    /// 这把武器原本每个弹幕各写各的金色，全是 BloomCircle 叠加法混合，缺少暗部，
    /// 于是所有弹幕糊成同一坨发光黄。本模块提供整把武器共用的五层调色盘、贴图清单
    /// 与具名特效函数，让左键旋刃、火球、光矢、天火、盾牌、土墙、终结技共享同一套
    /// 「白金内芯 → 圣金主体 → 火橙中层 → 亵渎余烬 → 焦黑底光」的视觉语言。
    ///
    /// 设计来源：
    ///   · Providence / 亵渎守卫 —— 暗红背光(DrawBackglow)、三重 CustomPulse 爆闪、
    ///     GlowOrb+MediumMist 双层尾迹、挤压呼吸动画。
    ///   · 正义旗 WarbanneroftheRighteous —— VelChangingSpark 汇聚火光。
    ///   · 贯星枪·人马座 / 落日 —— GlowSparkParticle 鱼骨喷流、日冕爆发环。
    /// </summary>
    internal static class AegisVisuals
    {
        // ────────────────────────────────────────────────────────────────
        // 一、五层调色盘
        // ────────────────────────────────────────────────────────────────

        /// <summary>白金内芯：只用于最亮的核心与命中瞬闪，面积必须最小。</summary>
        public static readonly Color Core = new(255, 250, 222);

        /// <summary>圣金主色：武器的身份色，绝大多数光效的主体。</summary>
        public static readonly Color Gold = new(255, 199, 74);

        /// <summary>火橙中层：主体与暗部之间的过渡，负责"这是火"的观感。</summary>
        public static readonly Color Flame = new(255, 132, 36);

        /// <summary>亵渎余烬：暗部。没有这一层，所有金色都会糊成一片白黄。</summary>
        public static readonly Color Ember = new(198, 54, 22);

        /// <summary>焦黑褐：底层背光与烟。让亮色能从暗底里"烧"出来。</summary>
        public static readonly Color Charred = new(58, 20, 14);

        /// <summary>亵渎圣火的官方尘埃（Calamity ProfanedFire）。</summary>
        public static int ProfanedFireDust => (int)CalamityDusts.ProfanedFire;

        /// <summary>加法混合专用：把 A 通道清零再乘亮度（A 不清零会露黑底）。</summary>
        public static Color Add(Color color, float brightness) => (color with { A = 0 }) * brightness;

        /// <summary>0 → 1 走完「白金 → 圣金 → 火橙 → 余烬」四段渐变。</summary>
        public static Color Gradient(float completion)
        {
            completion = MathHelper.Clamp(completion, 0f, 1f);
            if (completion < 0.34f)
                return Color.Lerp(Core, Gold, completion / 0.34f);
            if (completion < 0.68f)
                return Color.Lerp(Gold, Flame, (completion - 0.34f) / 0.34f);
            return Color.Lerp(Flame, Ember, (completion - 0.68f) / 0.32f);
        }

        /// <summary>随机取一个火焰主体色，偏圣金与火橙。</summary>
        public static Color RandomFlameColor() => Gradient(Main.rand.NextFloat(0.18f, 0.85f));

        // ────────────────────────────────────────────────────────────────
        // 二、贴图清单（本项目 Texture 库 + Calamity 粒子库）
        // ────────────────────────────────────────────────────────────────

        private const string Ks = "CalamityLegendsComeBack/Texture/KsTexture/";
        private const string Stp = "CalamityLegendsComeBack/Texture/SuperTexturePack/";

        public const string TexFireBody = Ks + "fire_01";          // 噪点火团，火焰实体
        public const string TexFireBodyAlt = Ks + "fire_02";       // 噪点火团（异形）
        public const string TexFlameWisp = Ks + "flame_03";        // 火焰丝缕
        public const string TexFlameWispAlt = Ks + "flame_04";     // 火焰丝缕（宽）
        public const string TexCrescent = Ks + "slash_01";         // 厚新月刀光
        public const string TexCrescentThin = Ks + "slash_03";     // 薄新月刀光
        public const string TexTwirl = Ks + "twirl_01";            // 旋抹弧（宽）
        public const string TexTwirlThin = Ks + "twirl_02";        // 旋抹弧（细）
        public const string TexTwirlWisp = Ks + "twirl_03";        // 旋抹弧（残）
        public const string TexRuneRing = Ks + "magic_01";         // 五点符文环   ×0.3
        public const string TexRuneRingDense = Ks + "magic_02";    // 多点符文环   ×0.3
        public const string TexRuneCross = Ks + "magic_03";        // 十字带环圣印 ×0.5
        public const string TexRuneSpike = Ks + "magic_04";        // 锐十字       ×0.5
        public const string TexRingSoft = Ks + "circle_03";        // 柔边空心环
        public const string TexRingThick = Ks + "circle_04";       // 厚边空心环
        public const string TexOrbSoft = Ks + "circle_05";         // 柔光实心球
        public const string TexStarThin = Ks + "star_04";          // 细四芒星
        public const string TexStarPinch = Ks + "star_08";         // 收腰四芒星
        public const string TexScorch = Ks + "scorch_01";          // 焦痕（小刺）
        public const string TexScorchBig = Ks + "scorch_02";       // 焦痕（大刺）
        public const string TexScorchSplat = Ks + "scorch_03";     // 焦痕（散溅）
        public const string TexRockA = Ks + "dirt_01";             // 碎岩团
        public const string TexRockB = Ks + "dirt_02";             // 碎岩团（密）
        public const string TexRockC = Ks + "dirt_03";             // 碎岩团（散）
        public const string TexJet = Ks + "muzzle_04";             // 竖向火焰喷流
        public const string TexBeamLine = Ks + "trace_05";         // 竖向柔光线
        public const string TexSmokePuff = Ks + "smoke_04";        // 烟团

        public const string TexSolarCore = Stp + "Sun/sun_001";            // 噪点日核
        public const string TexCorona = Stp + "Sun/flameeye_003";          // 日冕分段环
        public const string TexRadiance = Stp + "Sun/gradationline_003";   // 硬放射光芒
        public const string TexRadianceSoft = Stp + "Sun/gradationline_005"; // 柔放射光芒
        public const string TexExplosionCloud = Stp + "Sun/explosion2_004"; // 爆炸火云
        public const string TexNoise = Stp + "Sun/fbmnoise2_004";          // fbm 噪声
        public const string TexBarrierShell = Stp + "fx_Halo2";            // 裂纹护罩壳
        public const string TexImpactStar = Stp + "fx_ImpactMark4";        // 放射冲击星
        public const string TexBurstStar = Stp + "fx_Blast3";              // 爆闪星
        public const string TexShockRing = Stp + "fx_BlastWave2";          // 冲击柔球

        public const string TexBloom = "CalamityMod/Particles/BloomCircle";
        public const string TexSmallBloom = "CalamityMod/Particles/SmallBloom";
        public const string TexPinpoint = "CalamityMod/ExtraTextures/BloomCirclePinpoint";
        public const string TexSimpleStar = "CalamityMod/ExtraTextures/SimpleStar";
        public const string TexSoftExplosion = "CalamityMod/Particles/SoftRoundExplosion";
        public const string TexShatteredExplosion = "CalamityMod/Particles/ShatteredExplosion";
        public const string TexBlastCone = "CalamityMod/Particles/BlastCone";
        public const string TexSmearFire1 = "CalamityMod/Particles/CircularSmearFire1";
        public const string TexSmearFire2 = "CalamityMod/Particles/CircularSmearFire2";
        public const string TexSmearFire3 = "CalamityMod/Particles/CircularSmearFire3";
        public const string TexThickLine = "CalamityMod/Particles/ThickEndedLine";
        public const string TexThinLine = "CalamityMod/Particles/ThinEndedLine";
        // 注意：BloomLineSoftEdge 已被《BloomLineSoftEdge 纹理使用报告》列为禁用于会转向/散射的特效，
        // 本武器全部弹幕都在旋转或追踪，因此整套视觉体系不使用它，改用 muzzle_04 / trace_05 / ThickEndedLine。

        /// <summary>KsTexture 的 magic_01 / magic_02 原图 512²，项目口径要求在预期缩放上再 ×0.3。</summary>
        public const float RuneRingShrink = 0.3f;

        /// <summary>KsTexture 的 magic_03 / magic_04 原图 512²，项目口径要求在预期缩放上再 ×0.5。</summary>
        public const float RuneCrossShrink = 0.5f;

        public static Texture2D Tex(string path) => ModContent.Request<Texture2D>(path).Value;

        /// <summary>把"我要画多大（半径像素）"换算成 spriteBatch 需要的 scale。</summary>
        public static float RadiusScale(Texture2D texture, float radiusPixels) =>
            radiusPixels * 2f / texture.Width;

        // ────────────────────────────────────────────────────────────────
        // 三、绘制模块
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 亵渎背光：Providence 全系弹幕的招牌手法。在本体之下环形铺一圈暗红副本，
        /// 让上面的圣金能从暗底里"烧"出来，而不是直接糊在背景上。
        /// </summary>
        public static void ProfanedBackglow(Texture2D texture, Vector2 drawPosition, Rectangle? frame,
            float rotation, Vector2 origin, Vector2 scale, float opacity, float radius = 4f, int copies = 8)
        {
            if (opacity <= 0.004f)
                return;

            Color inner = Add(Ember, 0.30f * opacity);
            Color outer = Add(Charred, 0.42f * opacity);

            for (int i = 0; i < copies; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / copies).ToRotationVector2() * radius;
                Main.EntitySpriteDraw(texture, drawPosition + offset, frame, outer,
                    rotation, origin, scale * 1.06f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(texture, drawPosition + offset * 0.55f, frame, inner,
                    rotation, origin, scale, SpriteEffects.None, 0);
            }
        }

        /// <summary>
        /// 亵渎符文圣印：底环 → 日冕 → 双向符文环 → 十字圣印 → 星芒，六层反向自转。
        /// radius 是圣印的外缘半径（像素）。magic_* 贴图按项目口径分别再 ×0.3 / ×0.5。
        /// </summary>
        public static void DrawRuneSigil(Vector2 drawPosition, float radius, float spin, float opacity,
            Vector2 squish = default, float tierBrightness = 1f)
        {
            if (opacity <= 0.004f || radius <= 1f)
                return;

            if (squish == default)
                squish = Vector2.One;

            Texture2D ringSoft = Tex(TexRingSoft);
            Texture2D corona = Tex(TexCorona);
            Texture2D runeRing = Tex(TexRuneRing);
            Texture2D runeDense = Tex(TexRuneRingDense);
            Texture2D runeCross = Tex(TexRuneCross);
            Texture2D star = Tex(TexStarThin);

            // ① 暗底环：先压一层余烬色，避免圣印整体发白
            Main.EntitySpriteDraw(ringSoft, drawPosition, null, Add(Ember, 0.34f * opacity),
                -spin * 0.35f, ringSoft.Size() * 0.5f,
                new Vector2(RadiusScale(ringSoft, radius)) * squish, SpriteEffects.None, 0);

            // ② 日冕分段环：慢速正转，给圣印"在燃烧"的质感
            Main.EntitySpriteDraw(corona, drawPosition, null, Add(Flame, 0.30f * opacity * tierBrightness),
                spin * 0.5f, corona.Size() * 0.5f,
                new Vector2(RadiusScale(corona, radius * 0.86f)) * squish, SpriteEffects.None, 0);

            // ③ 外符文环（magic_01，预期缩放后再 ×0.3）
            float ringIntended = RadiusScale(runeRing, radius * 2.6f);
            Main.EntitySpriteDraw(runeRing, drawPosition, null, Add(Gold, 0.46f * opacity * tierBrightness),
                spin, runeRing.Size() * 0.5f,
                new Vector2(ringIntended * RuneRingShrink) * squish, SpriteEffects.None, 0);

            // ④ 内符文环（magic_02，反向自转，预期缩放后再 ×0.3）
            float denseIntended = RadiusScale(runeDense, radius * 1.85f);
            Main.EntitySpriteDraw(runeDense, drawPosition, null, Add(Gold, 0.34f * opacity * tierBrightness),
                -spin * 1.45f, runeDense.Size() * 0.5f,
                new Vector2(denseIntended * RuneRingShrink) * squish, SpriteEffects.None, 0);

            // ⑤ 十字圣印（magic_03，预期缩放后再 ×0.5）
            float crossIntended = RadiusScale(runeCross, radius * 1.1f);
            Main.EntitySpriteDraw(runeCross, drawPosition, null, Add(Core, 0.30f * opacity * tierBrightness),
                spin * 0.8f, runeCross.Size() * 0.5f,
                new Vector2(crossIntended * RuneCrossShrink) * squish, SpriteEffects.None, 0);

            // ⑥ 中心星芒
            Main.EntitySpriteDraw(star, drawPosition, null, Add(Core, 0.28f * opacity * tierBrightness),
                spin * 2.1f, star.Size() * 0.5f,
                new Vector2(RadiusScale(star, radius * 0.5f)) * squish, SpriteEffects.None, 0);
        }

        /// <summary>
        /// 日核：暗红外晕 → 噪点日盘 → 火团 → 白芯 → 放射光芒 → 星芒。
        /// 所有"这里是一颗圣火球"的地方都走这一套，保证火球家族长得像一家人。
        /// </summary>
        public static void DrawSolarCore(Vector2 drawPosition, float radius, float opacity, float spin,
            Vector2 squish = default)
        {
            if (opacity <= 0.004f || radius <= 0.5f)
                return;

            if (squish == default)
                squish = Vector2.One;

            Texture2D bloom = Tex(TexBloom);
            Texture2D solar = Tex(TexSolarCore);
            Texture2D fire = Tex(TexFireBody);
            Texture2D orb = Tex(TexOrbSoft);
            Texture2D radiance = Tex(TexRadiance);
            Texture2D star = Tex(TexSimpleStar);

            // ① 焦黑/余烬外晕：底子，让核心不会直接飘在背景上
            Main.EntitySpriteDraw(bloom, drawPosition, null, Add(Ember, 0.55f * opacity),
                0f, bloom.Size() * 0.5f,
                new Vector2(RadiusScale(bloom, radius * 2.05f)) * squish, SpriteEffects.None, 0);

            // ② 放射光芒：硬边光刺，慢转
            Main.EntitySpriteDraw(radiance, drawPosition, null, Add(Flame, 0.20f * opacity),
                spin * 0.32f, radiance.Size() * 0.5f,
                new Vector2(RadiusScale(radiance, radius * 2.6f)) * squish, SpriteEffects.None, 0);

            // ③ 噪点日盘：本体质感层
            Main.EntitySpriteDraw(solar, drawPosition, null, Add(Flame, 0.85f * opacity),
                -spin * 0.55f, solar.Size() * 0.5f,
                new Vector2(RadiusScale(solar, radius)) * squish, SpriteEffects.None, 0);

            // ④ 火团：正反两片交错，火在自己翻滚
            Main.EntitySpriteDraw(fire, drawPosition, null, Add(Gold, 0.72f * opacity),
                spin, fire.Size() * 0.5f,
                new Vector2(RadiusScale(fire, radius * 0.94f)) * squish, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(fire, drawPosition, null, Add(Gold, 0.46f * opacity),
                -spin * 1.6f + 1.1f, fire.Size() * 0.5f,
                new Vector2(RadiusScale(fire, radius * 0.72f)) * squish, SpriteEffects.None, 0);

            // ⑤ 白金内芯：面积最小、最亮
            Main.EntitySpriteDraw(orb, drawPosition, null, Add(Core, 0.80f * opacity),
                0f, orb.Size() * 0.5f,
                new Vector2(RadiusScale(orb, radius * 0.44f)) * squish, SpriteEffects.None, 0);

            // ⑥ 星芒
            Main.EntitySpriteDraw(star, drawPosition, null, Add(Core, 0.34f * opacity),
                spin * 0.9f, star.Size() * 0.5f,
                new Vector2(RadiusScale(star, radius * 1.15f)) * squish, SpriteEffects.None, 0);
        }

        /// <summary>地面/敌人身上的焦痕贴花，用于"这里刚刚被圣火烧过"。</summary>
        public static void DrawScorchDecal(Vector2 drawPosition, float rotation, float radius, float fade,
            Vector2 squish = default)
        {
            if (fade <= 0.004f)
                return;

            if (squish == default)
                squish = Vector2.One;

            Texture2D splat = Tex(TexScorchSplat);
            Texture2D spikes = Tex(TexScorchBig);

            Main.EntitySpriteDraw(splat, drawPosition, null, Add(Ember, 0.42f * fade),
                rotation, splat.Size() * 0.5f,
                new Vector2(RadiusScale(splat, radius)) * squish, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(spikes, drawPosition, null, Add(Flame, 0.34f * fade),
                -rotation * 0.6f, spikes.Size() * 0.5f,
                new Vector2(RadiusScale(spikes, radius * 0.78f)) * squish, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(spikes, drawPosition, null, Add(Core, 0.18f * fade),
                rotation * 1.4f, spikes.Size() * 0.5f,
                new Vector2(RadiusScale(spikes, radius * 0.42f)) * squish, SpriteEffects.None, 0);
        }

        /// <summary>
        /// 圣火拖尾三层配色。外层余烬、中层圣金、内层白芯，配 primitive 拖尾用。
        /// layer：0 = 外焰、1 = 主焰、2 = 内芯。
        /// </summary>
        public static Color TrailColor(float completion, int layer, float opacity)
        {
            completion = MathHelper.Clamp(completion, 0f, 1f);
            Color body = layer switch
            {
                0 => Color.Lerp(Flame, Ember, completion),
                1 => Color.Lerp(Gold, Flame, completion * 0.85f),
                _ => Color.Lerp(Core, Gold, completion * 0.7f),
            };
            body *= opacity * Utils.GetLerpValue(1f, 0.55f, completion, true);
            body.A = 0;
            return body;
        }

        // ────────────────────────────────────────────────────────────────
        // 四、粒子模块
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 三重圣火爆闪 —— Providence 全系弹幕的死亡签名：
        /// BloomCircle 白闪 + SoftRoundExplosion 圣光波 + ShatteredExplosion 碎裂波，
        /// 外加 FlameParticle 火焰体积与 HeavySmokeParticle 深灰圣灰。
        /// </summary>
        /// <param name="power">1 = 一次普通命中；2.5 左右 = 大火球炸裂；5 = 终结技级别。</param>
        public static void HolyDetonation(Vector2 position, float power, bool smoke = true, float rotation = 0f)
        {
            if (Main.dedServ)
                return;

            // SoftRoundExplosion / ShatteredExplosion 原图都是 2048²，
            // 0.07 ≈ 143 像素直径，正好是"一次武器级命中"的量级（Providence 用 0.25 是 BOSS 级）。
            float pulse = 0.07f * power;

            GeneralParticleHandler.SpawnParticle(new CustomPulse(position, Vector2.Zero,
                Add(Core, 0.9f), TexBloom, Vector2.One, rotation,
                0.32f * power, 0.05f * power, 8));

            for (float i = 0.45f; i <= 1f; i += 0.275f)
            {
                GeneralParticleHandler.SpawnParticle(new CustomPulse(position, Vector2.Zero,
                    Add(Gold, 0.85f), TexSoftExplosion, Vector2.One,
                    Main.rand.NextFloat(MathHelper.TwoPi),
                    pulse * 0.2f * i, pulse * i, (int)(18 + 10 * i)));
            }

            GeneralParticleHandler.SpawnParticle(new CustomPulse(position, Vector2.Zero,
                Add(Ember, 0.9f), TexShatteredExplosion, Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                pulse * 0.18f, pulse * 0.78f, 20));

            int flames = (int)MathHelper.Clamp(6f * power, 5f, 34f);
            for (int i = 0; i < flames; i++)
            {
                Particle flame = new FlameParticle(
                    position + Main.rand.NextVector2Circular(14f * power, 14f * power),
                    Main.rand.Next(22, 38), Main.rand.NextFloat(0.32f, 0.55f) * MathF.Sqrt(power),
                    Main.rand.NextFloat(0.9f, 2.1f), Gold, Ember);
                flame.Velocity = Main.rand.NextVector2CircularEdge(1f, 1f) *
                                 Main.rand.NextFloat(2.4f, 9f) * MathF.Sqrt(power);
                GeneralParticleHandler.SpawnParticle(flame);
            }

            if (smoke)
            {
                int smokes = (int)MathHelper.Clamp(4f * power, 3f, 20f);
                for (int i = 0; i < smokes; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                        position + Main.rand.NextVector2Circular(10f * power, 10f * power),
                        Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.6f, 6.5f) * MathF.Sqrt(power),
                        Color.Lerp(Charred, Color.DarkSlateGray, Main.rand.NextFloat(0.35f, 0.8f)),
                        Main.rand.Next(28, 46), Main.rand.NextFloat(0.5f, 0.95f) * MathF.Sqrt(power),
                        0.62f, Main.rand.NextFloat(-0.04f, 0.04f), true));
                }
            }
        }

        /// <summary>
        /// 定向冲击：给"打中了/冲进去了"的瞬间用。比 <see cref="HolyDetonation"/> 更扁、更有方向。
        /// </summary>
        public static void DirectionalImpact(Vector2 position, Vector2 direction, float power)
        {
            if (Main.dedServ)
                return;

            direction = direction.SafeNormalize(Vector2.UnitX);
            float angle = direction.ToRotation();

            // DirectionalPulseRing 的实际绘制尺寸 = Scale × Squish × 156px(HollowCircleHardEdge)，
            // 所以 finalScale 必须给到 ~1 才看得见；旧代码这里给的是 0.06~0.08，
            // 等于画了一个十几像素的环，"有特效但看不见"。
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(position, Vector2.Zero,
                Add(Flame, 0.9f), new Vector2(1.25f, 0.66f), angle, 0f, 0.36f * power, 18));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(position, direction * 1.4f,
                Add(Gold, 0.95f), new Vector2(0.5f, 1.45f), angle, 0.06f * power, 0.95f * power, 17));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(position, Vector2.Zero,
                Add(Ember, 0.8f), new Vector2(1.5f, 0.55f), angle,
                0.05f * power, 0.72f * power, 21));
            GeneralParticleHandler.SpawnParticle(new SparkleParticle(position, Vector2.Zero,
                Add(Core, 1f), Add(Flame, 1f), 0.85f * power, 13, 0.05f, 1.7f));
        }

        /// <summary>
        /// 火星喷流：贯星枪的「鱼骨 + 鱼肉」结构 —— GlowSparkParticle 长条主骨架，
        /// SparkParticle 填肉，ProfanedFire 尘埃收边。
        /// </summary>
        public static void EmberJet(Vector2 position, Vector2 direction, int count, float power,
            float spreadRadians = 0.32f)
        {
            if (Main.dedServ)
                return;

            direction = direction.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < count; i++)
            {
                Vector2 jetVelocity = direction.RotatedByRandom(spreadRadians) *
                                      Main.rand.NextFloat(3.6f, 9.5f) * power;

                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(position, jetVelocity, false,
                    Main.rand.Next(8, 14), Main.rand.NextFloat(0.09f, 0.16f) * power,
                    Add(Gradient(Main.rand.NextFloat(0f, 0.6f)), 0.85f),
                    new Vector2(2.3f, 0.46f), true, false, 1f));

                if (Main.rand.NextBool())
                {
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(position,
                        jetVelocity.RotatedByRandom(0.22f) * Main.rand.NextFloat(0.45f, 0.9f), false,
                        Main.rand.Next(16, 30), Main.rand.NextFloat(0.45f, 0.95f) * power,
                        Gradient(Main.rand.NextFloat(0.3f, 0.95f))));
                }

                if (Main.rand.NextBool(3))
                {
                    Dust ember = Dust.NewDustPerfect(position, ProfanedFireDust,
                        jetVelocity * Main.rand.NextFloat(0.3f, 0.7f), 0, Color.White,
                        Main.rand.NextFloat(0.9f, 1.6f) * power);
                    ember.noGravity = true;
                }
            }
        }

        /// <summary>
        /// 日冕爆发环：向四周均匀铺开的一圈长条光刺，用于"释放/爆发"的一次性节点。
        /// </summary>
        public static void CoronaRing(Vector2 position, int count, float power, float baseAngle = 0f)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < count; i++)
            {
                float angle = baseAngle + MathHelper.TwoPi * i / count;
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(2.4f, 4.8f) * power;
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(position, velocity, false,
                    Main.rand.Next(11, 17), 0.12f * power,
                    Add(i % 2 == 0 ? Gold : Flame, 0.85f),
                    new Vector2(1.6f, 0.5f), true, false, 1f));
            }
        }

        /// <summary>
        /// 正义旗式汇聚火光：火星从目标外围 220 像素处沿 <paramref name="inwardDirection"/> 被"吸"向目标，
        /// 起始速度带随机偏角、终点速度笔直，于是每一颗火星都走一条弧线。
        /// 这是《正义旗》WarbanneroftheRighteous 灼烧标记的原始结构。
        /// </summary>
        public static void WarbannerConverge(Vector2 target, Vector2 inwardDirection, float intensity,
            int count, float sizeBonus = 1f)
        {
            if (Main.dedServ)
                return;

            inwardDirection = inwardDirection.SafeNormalize(Vector2.UnitX);
            Vector2 sparkOrigin = target - inwardDirection * 220f;

            for (int i = 0; i < count; i++)
            {
                Color color = Main.rand.NextBool()
                    ? Color.Lerp(Gold, Color.Goldenrod, Main.rand.NextFloat())
                    : Color.Lerp(Ember, Flame, Main.rand.NextFloat());

                float speed = Main.rand.NextFloat(2f, 7f) * intensity * sizeBonus;
                Vector2 endVelocity = inwardDirection * speed;
                Vector2 startVelocity = endVelocity.RotatedByRandom(0.6f * intensity);

                GeneralParticleHandler.SpawnParticle(new VelChangingSpark(
                    sparkOrigin + Main.rand.NextVector2Circular(26f, 26f),
                    startVelocity, endVelocity, TexSmallBloom,
                    Main.rand.Next(18, 23), Main.rand.NextFloat(0.1f, 0.25f) * sizeBonus,
                    color * 0.75f, new Vector2(0.7f, 1f), true, false, 0f, false, 0.45f, 0.1f));

                if (Main.rand.NextBool())
                {
                    Dust trail = Dust.NewDustPerfect(sparkOrigin, ModContent.DustType<LightDust>(),
                        startVelocity, 0, color, Main.rand.NextFloat(0.5f, 0.9f) * Math.Min(sizeBonus, 1.3f));
                    trail.noGravity = true;
                    trail.noLightEmittence = true;
                }
            }
        }

        /// <summary>圣火尾迹：Providence 的 GlowOrb + MediumMist 双层结构。飞行弹幕每帧调用。</summary>
        public static void FlightTrail(Vector2 position, Vector2 velocity, float scale, int timer,
            int orbInterval = 4, bool mist = true)
        {
            if (Main.dedServ)
                return;

            Vector2 backwards = -velocity.SafeNormalize(Vector2.UnitX);

            if (timer % orbInterval == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    position + Main.rand.NextVector2Circular(6f * scale, 6f * scale),
                    backwards * Main.rand.NextFloat(0.3f, 1.1f), false,
                    Main.rand.Next(10, 17), Main.rand.NextFloat(0.16f, 0.32f) * scale,
                    RandomFlameColor(), true, false, true));
            }

            if (mist && Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    position + backwards * Main.rand.NextFloat(4f, 16f) * scale,
                    backwards * Main.rand.NextFloat(0.4f, 1.5f),
                    Color.Lerp(Charred, Color.DarkSlateGray, Main.rand.NextFloat(0.3f, 0.85f)),
                    Color.Transparent, Main.rand.NextFloat(0.3f, 0.6f) * scale,
                    Main.rand.Next(22, 40), Main.rand.NextFloat(-0.05f, 0.05f)));
            }

            if (Main.rand.NextBool(4))
            {
                Dust ember = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(7f * scale, 7f * scale),
                    ProfanedFireDust, backwards * Main.rand.NextFloat(0.4f, 1.6f), 0, Color.White,
                    Main.rand.NextFloat(0.8f, 1.45f) * scale);
                ember.noGravity = true;
            }
        }

        /// <summary>余烬滴落：从某个体积上"烧下来"的火屑，给盾牌/土墙/插地剑做静态存在感。</summary>
        public static void EmberDrip(Vector2 position, float spreadX, float spreadY, float scale)
        {
            if (Main.dedServ)
                return;

            Vector2 spawn = position + new Vector2(Main.rand.NextFloat(-spreadX, spreadX),
                Main.rand.NextFloat(-spreadY, spreadY));

            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(spawn,
                new Vector2(Main.rand.NextFloat(-0.35f, 0.35f), Main.rand.NextFloat(0.5f, 1.7f)),
                false, Main.rand.Next(16, 28), Main.rand.NextFloat(0.1f, 0.2f) * scale,
                RandomFlameColor(), true, false, true));

            if (Main.rand.NextBool(3))
            {
                Dust ember = Dust.NewDustPerfect(spawn, ProfanedFireDust,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(0.6f, 2.2f)),
                    0, Color.White, Main.rand.NextFloat(0.7f, 1.2f) * scale);
                ember.noGravity = false;
            }
        }

        /// <summary>把一次震屏请求按距离衰减后写给本地玩家。</summary>
        public static void Screenshake(Vector2 position, float power, float range = 1400f)
        {
            if (Main.dedServ)
                return;

            float falloff = Utils.GetLerpValue(range, 0f, Vector2.Distance(position, Main.LocalPlayer.Center), true);
            var calamityPlayer = Main.LocalPlayer.Calamity();
            calamityPlayer.GeneralScreenShakePower = Math.Max(calamityPlayer.GeneralScreenShakePower, power * falloff);
        }

        /// <summary>统一的圣火照明。</summary>
        public static void Light(Vector2 position, float strength) =>
            Lighting.AddLight(position, Gold.ToVector3() * strength);
    }
}
