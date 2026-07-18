using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.A_Dev.ElementalCodex
{
    // 元素图鉴的专属特效层：每个元素的常驻氛围、每种反应触发瞬间的爆发都要有独立辨识度，
    // 不能只是换色的 Dust。几何结构上刻意区分"数学美感"（黄金角螺旋/正多边形/斐波那契环）
    // 与"狂野感"（散裂、坠落、抖动），让反应的视觉语言和它的机制概念对应起来。
    internal static class ElementalCodexVFX
    {
        private const float GoldenAngle = 2.39996323f;

        private static readonly Color FireCore = new Color(255, 224, 150);
        private static readonly Color FireHot = new Color(255, 120, 40);
        private static readonly Color WaterCore = new Color(150, 210, 255);
        private static readonly Color WaterDeep = new Color(30, 90, 190);
        private static readonly Color IceCore = new Color(210, 245, 255);
        private static readonly Color IceDeep = new Color(120, 200, 235);
        private static readonly Color LightningCore = new Color(230, 200, 255);
        private static readonly Color LightningDeep = new Color(150, 60, 230);
        private static readonly Color NatureCore = new Color(170, 255, 160);
        private static readonly Color NatureDeep = new Color(40, 140, 60);
        private static readonly Color DiseaseCore = new Color(150, 190, 90);
        private static readonly Color DiseaseDeep = new Color(40, 34, 46);

        // ── 数学结构小工具 ─────────────────────────────────────────
        // 叶序螺旋：真实植物排列公式（半径 ∝ √index），用于 Nature/Growth/Wither。
        private static Vector2 GoldenSpiralOffset(int index, float radiusStep, float phase = 0f)
        {
            float angle = index * GoldenAngle + phase;
            float radius = MathF.Sqrt(index + 1f) * radiusStep;
            return angle.ToRotationVector2() * radius;
        }

        private static Vector2 PolygonVertex(Vector2 center, float radius, int index, float phase, int sides = 6)
        {
            float angle = MathHelper.TwoPi * index / sides + phase;
            return center + angle.ToRotationVector2() * radius;
        }

        private static void SpawnRadialCracks(Vector2 center, int count, float radius, Color color, int lifetimeBase)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.12f, 0.12f);
                Vector2 dir = angle.ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new CrackParticle(center + dir * radius * 0.15f, dir * Main.rand.NextFloat(1.5f, 3f), color,
                    Vector2.One, angle, 0.5f, 1.4f, lifetimeBase + Main.rand.Next(-4, 5)));
            }
        }

        // ══════════════════════════════════════════════════════════
        // 元素常驻氛围（每 5 帧调用一次，替代旧的纯 Dust 版本）
        // ══════════════════════════════════════════════════════════

        public static void EmitElementAura(NPC npc, ElementalCodexElement element)
        {
            if (Main.dedServ)
                return;

            switch (element)
            {
                case ElementalCodexElement.Fire: FireAura(npc); break;
                case ElementalCodexElement.Water: WaterAura(npc); break;
                case ElementalCodexElement.Ice: IceAura(npc); break;
                case ElementalCodexElement.Lightning: LightningAura(npc); break;
                case ElementalCodexElement.Nature: NatureAura(npc); break;
                case ElementalCodexElement.Disease: DiseaseAura(npc); break;
            }
        }

        // 火：黄金角余烬螺旋——两枚火星沿黄金角旋转爬升，偶发裂焰爆点 + 焦烟，核心呼吸光晕。
        private static void FireAura(NPC npc)
        {
            Vector2 center = npc.Center;
            float t = Main.GameUpdateCount * 0.05f;
            float radius = MathHelper.Clamp(npc.width, 20f, 90f) * 0.38f;

            for (int i = 0; i < 2; i++)
            {
                float angle = t * 2.3f + i * GoldenAngle;
                Vector2 spawn = center + angle.ToRotationVector2() * radius * new Vector2(1f, 0.5f) + new Vector2(0f, npc.height * 0.18f);
                Vector2 velocity = new Vector2(MathF.Cos(angle) * 0.5f, -Main.rand.NextFloat(1.8f, 3.4f));
                Color emberColor = Color.Lerp(FireHot, FireCore, Main.rand.NextFloat());
                GeneralParticleHandler.SpawnParticle(new PointParticle(spawn, velocity, false, Main.rand.Next(16, 24), Main.rand.NextFloat(0.7f, 1.05f), emberColor));
            }

            if (Main.rand.NextBool(3))
            {
                Vector2 crackSpot = center + Main.rand.NextVector2Circular(radius, npc.height * 0.32f);
                GeneralParticleHandler.SpawnParticle(new CrackParticle(crackSpot, new Vector2(0f, -Main.rand.NextFloat(1f, 2.4f)), FireHot,
                    Vector2.One, Main.rand.NextFloat(-MathHelper.Pi, MathHelper.Pi), 0.4f, 1.05f, 14));
            }

            if (Main.rand.NextBool(6))
                GeneralParticleHandler.SpawnParticle(new SmallSmokeParticle(center + Main.rand.NextVector2Circular(10f, 10f),
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -Main.rand.NextFloat(1.1f, 2f)), Color.DimGray, Color.Black, 0.45f, 0.55f, 22, false));

            if (Main.rand.NextBool(4))
                GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, FireHot, 0.55f + 0.1f * MathF.Sin(t * 1.7f), 18));

            Lighting.AddLight(center, 0.5f, 0.22f, 0.08f);
        }

        // 水：潮汐螺旋——液滴沿正弦轨道环绕，脚下偶发泡沫环，稀薄雾气与气泡点缀。
        private static void WaterAura(NPC npc)
        {
            Vector2 center = npc.Center;
            float t = Main.GameUpdateCount * 0.045f;
            float radiusX = MathHelper.Clamp(npc.width, 20f, 90f) * 0.42f;
            float radiusY = MathHelper.Clamp(npc.height, 20f, 90f) * 0.34f;

            for (int i = 0; i < 2; i++)
            {
                float phase = t * 1.6f + i * MathHelper.Pi;
                Vector2 orbit = new Vector2(MathF.Sin(phase) * radiusX, MathF.Sin(phase * 2f + i) * radiusY * 0.5f);
                Vector2 tangent = new Vector2(-MathF.Sin(phase), MathF.Cos(phase) * 0.5f).SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(0.6f, 1.3f);
                GeneralParticleHandler.SpawnParticle(new WaterGlobParticle(center + orbit, tangent, 1f, 0.03f, Main.rand.Next(28, 40)));
            }

            if (Main.rand.NextBool(4))
                GeneralParticleHandler.SpawnParticle(new WaterFoamParticle(center + new Vector2(0f, npc.height * 0.42f),
                    new Vector2(Main.rand.NextFloat(-1f, 1f), -Main.rand.NextFloat(0.4f, 1f)), 26, 0.85f, WaterCore));

            if (Main.rand.NextBool(8))
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(center, new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), -Main.rand.NextFloat(0.6f, 1.4f)),
                    WaterCore, Color.Transparent, Main.rand.NextFloat(0.6f, 1f), Main.rand.NextFloat(0.5f, 0.8f)));

            if (Main.rand.NextBool(10))
                GeneralParticleHandler.SpawnParticle(new GenericBubbleParticle(center + Main.rand.NextVector2Circular(radiusX, radiusY),
                    new Vector2(0f, -Main.rand.NextFloat(0.8f, 1.4f)), 1f, 0f, Main.rand.Next(30, 45)));

            Lighting.AddLight(center, 0.18f, 0.32f, 0.5f);
        }

        // 冰：六方晶格——每次只点亮六边形一个顶点，一圈下来正好长成完整晶格；偶发裂片与霜雾。
        private static void IceAura(NPC npc)
        {
            Vector2 center = npc.Center;
            float radius = MathHelper.Clamp(Math.Max(npc.width, npc.height), 24f, 100f) * 0.44f;
            float phase = Main.GameUpdateCount * 0.018f;
            int vertex = (int)(Main.GameUpdateCount / 5) % 6;

            Vector2 spot = PolygonVertex(center, radius, vertex, phase);
            GeneralParticleHandler.SpawnParticle(new SnowflakeSparkle(spot, Main.rand.NextVector2Circular(0.3f, 0.3f), IceCore, IceDeep,
                Main.rand.NextFloat(0.6f, 0.9f), 26, 0.03f, 1f, 5));

            if (Main.rand.NextBool(4))
            {
                Vector2 shardSpot = PolygonVertex(center, radius * 0.7f, vertex + 3, phase);
                GeneralParticleHandler.SpawnParticle(new CrackParticle(shardSpot, Vector2.Zero, IceCore, Vector2.One,
                    phase + MathHelper.Pi / 3f * vertex, 0.35f, 0.9f, 20));
            }

            if (Main.rand.NextBool(10))
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(center, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.3f, 0.7f)),
                    IceCore, Color.Transparent, Main.rand.NextFloat(0.6f, 1f), Main.rand.NextFloat(0.4f, 0.7f)));

            if (Main.rand.NextBool(5))
                GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, IceDeep, 0.4f + 0.08f * MathF.Sin(phase * 4f), 16));

            Lighting.AddLight(center, 0.18f, 0.42f, 0.5f);
        }

        // 雷：微弧网——借用 AzureThunder 的活电弧粒子在体表随机两点间连一道会扭动的短弧，加零星电火花。
        private static void LightningAura(NPC npc)
        {
            Vector2 center = npc.Center;
            float radius = MathHelper.Clamp(Math.Max(npc.width, npc.height), 20f, 90f) * 0.4f;

            if (Main.rand.NextBool(2))
            {
                Vector2 from = center + Main.rand.NextVector2CircularEdge(radius, radius * 0.7f);
                Vector2 to = center + Main.rand.NextVector2CircularEdge(radius, radius * 0.7f);
                GeneralParticleHandler.SpawnParticle(new AzureThunderArcParticle(from, to, LightningDeep, 14, 1.1f, 0.7f, false));
            }

            if (Main.rand.NextBool(3))
                GeneralParticleHandler.SpawnParticle(new ElectricSpark(center + Main.rand.NextVector2Circular(radius, radius),
                    Main.rand.NextVector2Circular(1.2f, 1.2f), LightningCore, LightningDeep, 0.6f, 16, MathHelper.PiOver4, 6f, 0.6f));

            if (Main.rand.NextBool(6))
                GeneralParticleHandler.SpawnParticle(new PointParticle(center + Main.rand.NextVector2Circular(radius, radius),
                    Main.rand.NextVector2Circular(1f, 1f), false, 14, 0.7f, LightningCore));

            Lighting.AddLight(center, 0.32f, 0.14f, 0.5f);
        }

        // 自然：黄金角叶序——完全照搬向日葵种子排列公式（半径 ∝ √index），配合心跳般的呼吸光晕。
        private static void NatureAura(NPC npc)
        {
            Vector2 center = npc.Center;
            float t = Main.GameUpdateCount * 0.04f;
            float radiusStep = MathHelper.Clamp(npc.width, 20f, 90f) * 0.09f;
            int seed = (int)(Main.GameUpdateCount / 5) % 21;

            Vector2 spawn = center + GoldenSpiralOffset(seed, radiusStep, t * 0.6f) * new Vector2(1f, 0.55f);
            Color leafColor = Color.Lerp(NatureCore, NatureDeep, Main.rand.NextFloat(0.4f));
            GeneralParticleHandler.SpawnParticle(new GenericSparkle(spawn, new Vector2(0f, -Main.rand.NextFloat(0.3f, 0.7f)), leafColor, NatureCore,
                Main.rand.NextFloat(0.5f, 0.8f), 24, 0.015f, 1.1f));

            float pulse = 0.5f + 0.5f * MathF.Sin(t * 1.1f);
            if (Main.rand.NextBool(4))
                GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, NatureDeep, 0.4f + 0.18f * pulse, 20));

            if (Main.rand.NextBool(9))
                GeneralParticleHandler.SpawnParticle(new PointParticle(center + Main.rand.NextVector2Circular(npc.width * 0.4f, npc.height * 0.4f),
                    new Vector2(0f, -Main.rand.NextFloat(0.4f, 1f)), false, 18, 0.6f, NatureCore));

            Lighting.AddLight(center, 0.14f, 0.44f, 0.14f);
        }

        // 疾病：混沌孢子云——瘟疫湿雾打底，暗色闪烁与焦黑烟丝故意不成规律，呼应"病态失序"的概念。
        private static void DiseaseAura(NPC npc)
        {
            Vector2 center = npc.Center;

            if (Main.rand.NextBool(2))
                GeneralParticleHandler.SpawnParticle(new PlagueHumidifierMist(center + Main.rand.NextVector2Circular(npc.width * 0.35f, npc.height * 0.35f),
                    Main.rand.Next(30, 45), Main.rand.NextFloat(0.7f, 1.1f), Main.rand.NextVector2Circular(0.8f, 0.8f) + new Vector2(0f, -0.6f)));

            if (Main.rand.NextBool(4))
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(center + Main.rand.NextVector2Circular(npc.width * 0.45f, npc.height * 0.45f),
                    Main.rand.NextVector2Circular(0.5f, 0.5f), DiseaseCore, DiseaseDeep, Main.rand.NextFloat(0.5f, 0.85f), 22, 0.01f, 0.9f));

            if (Main.rand.NextBool(6))
                GeneralParticleHandler.SpawnParticle(new SmallSmokeParticle(center + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextVector2Circular(0.6f, 0.6f),
                    DiseaseDeep, Color.Black, 0.5f, 0.6f, 26, false));

            if (Main.rand.NextBool(8))
                GeneralParticleHandler.SpawnParticle(new VoidSparkParticle(center + Main.rand.NextVector2Circular(npc.width * 0.4f, npc.height * 0.4f),
                    Main.rand.NextVector2Circular(0.4f, 0.4f), false, 20, 0.55f, DiseaseDeep, 0.96f));

            Lighting.AddLight(center, 0.1f, 0.08f, 0.06f);
        }

        // ══════════════════════════════════════════════════════════
        // 持续型反应状态的专属氛围（灼烧 / 感电 / 繁茂 / 冻结期间常驻调用）
        // ══════════════════════════════════════════════════════════

        // 灼烧：在火焰氛围基础上叠加焦黑草木灰，和普通"点燃"状态区分开。
        public static void EmitScorchAura(NPC npc)
        {
            if (Main.dedServ)
                return;

            FireAura(npc);
            if (Main.rand.NextBool(6))
                GeneralParticleHandler.SpawnParticle(new SmallSmokeParticle(npc.Center + Main.rand.NextVector2Circular(npc.width * 0.4f, npc.height * 0.4f),
                    new Vector2(0f, -Main.rand.NextFloat(0.6f, 1.2f)), DiseaseDeep, Color.Black, 0.6f, 0.7f, 26, false));
        }

        // 感电：雷弧氛围基础上偶发被电流甩出的水珠，暗示"导电介质是水"。
        public static void EmitElectrifiedAura(NPC npc)
        {
            if (Main.dedServ)
                return;

            LightningAura(npc);
            if (Main.rand.NextBool(3))
                GeneralParticleHandler.SpawnParticle(new WaterGlobParticle(npc.Center + Main.rand.NextVector2Circular(npc.width * 0.3f, npc.height * 0.3f),
                    Main.rand.NextVector2Circular(1.2f, 1.2f), 1f, 0.05f, 20));
        }

        // 繁茂：五边形"锁定光环"缓慢自转，隔一个顶点连一道短弧——视觉上就是一个瞄准标记。
        public static void EmitFlourishAura(NPC npc)
        {
            if (Main.dedServ)
                return;

            Vector2 center = npc.Center;
            float radius = MathHelper.Clamp(Math.Max(npc.width, npc.height), 24f, 100f) * 0.5f;
            float rot = Main.GameUpdateCount * 0.03f;
            const int sides = 5;

            if (Main.rand.NextBool(2))
            {
                int index = (int)(Main.GameUpdateCount / 5) % sides;
                Vector2 nodeA = PolygonVertex(center, radius, index, rot, sides);
                Vector2 nodeB = PolygonVertex(center, radius, (index + 2) % sides, rot, sides);
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(nodeA, Vector2.Zero, Color.Lerp(NatureCore, LightningCore, 0.5f), LightningCore,
                    0.55f, 20, 0.02f, 1f));
                GeneralParticleHandler.SpawnParticle(new AzureThunderArcParticle(nodeA, nodeB, Color.Lerp(NatureDeep, LightningDeep, 0.5f), 12, 0.8f, 0.5f, false));
            }

            Lighting.AddLight(center, 0.2f, 0.34f, 0.24f);
        }

        // 冻结：几乎静止的微光，呼应"定身"本身——和其它元素持续躁动的氛围形成对照。
        public static void EmitFreezeAura(NPC npc)
        {
            if (Main.dedServ)
                return;

            Vector2 center = npc.Center;
            if (Main.rand.NextBool(5))
                GeneralParticleHandler.SpawnParticle(new SnowflakeSparkle(center + Main.rand.NextVector2Circular(npc.width * 0.4f, npc.height * 0.4f),
                    Vector2.Zero, IceCore, IceDeep, Main.rand.NextFloat(0.4f, 0.6f), 18, 0.01f, 0.6f, 4));

            if (Main.rand.NextBool(10))
                GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, IceCore, 0.3f, 12));

            Lighting.AddLight(center, 0.15f, 0.32f, 0.42f);
        }

        // ══════════════════════════════════════════════════════════
        // 反应触发瞬间的一次性爆发（15 种反应各自独立形态）
        // ══════════════════════════════════════════════════════════

        public static void EmitReactionEffect(NPC npc, ElementalCodexReaction reaction, bool corruptFreezeCritical = false)
        {
            if (Main.dedServ)
                return;

            Vector2 center = npc.Center;
            switch (reaction)
            {
                case ElementalCodexReaction.SteamBurst: SteamBurstEffect(center); break;
                case ElementalCodexReaction.MeltingImpact: MeltingImpactEffect(center); break;
                case ElementalCodexReaction.Overload: OverloadEffect(center); break;
                case ElementalCodexReaction.Scorch: ScorchIgniteEffect(center); break;
                case ElementalCodexReaction.Paralysis: ParalysisEffect(center); break;
                case ElementalCodexReaction.Freeze: FreezeSnapEffect(npc); break;
                case ElementalCodexReaction.Electrified: ElectrifiedIgniteEffect(center); break;
                case ElementalCodexReaction.Growth: GrowthEffect(center); break;
                case ElementalCodexReaction.Wither: WitherEffect(center); break;
                case ElementalCodexReaction.Condensation: CondensationEffect(center); break;
                case ElementalCodexReaction.ColdStorage: ColdStorageEffect(center); break;
                case ElementalCodexReaction.CorruptFreeze: CorruptFreezeEffect(center, corruptFreezeCritical); break;
                case ElementalCodexReaction.Flourish: FlourishIgniteEffect(center); break;
                case ElementalCodexReaction.Control: ControlBindEffect(npc); break;
                case ElementalCodexReaction.Neutralization: NeutralizationEffect(center); break;
            }
        }

        // 火+水 蒸汽爆发：中心闪光 + 扩散环，黄金角螺旋里交替喷火星与蒸汽雾团，收尾一阵狂乱蒸汽柱。
        private static void SteamBurstEffect(Vector2 center)
        {
            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, Color.White, 1.6f, 20));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, WaterCore, new Vector2(1.2f, 1.2f), 0f, 0.16f, 0.95f, 26));

            const int seeds = 14;
            for (int i = 0; i < seeds; i++)
            {
                Vector2 dir = (i * GoldenAngle).ToRotationVector2();
                float speed = Main.rand.NextFloat(4f, 8f);
                if (i % 2 == 0)
                    GeneralParticleHandler.SpawnParticle(new MediumMistParticle(center, dir * speed * 0.4f, Color.White, Color.Transparent,
                        Main.rand.NextFloat(0.7f, 1.1f), Main.rand.NextFloat(0.5f, 0.8f)));
                else
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(center, dir * speed, false, Main.rand.Next(16, 24), Main.rand.NextFloat(0.8f, 1.2f), FireHot));
            }

            for (int i = 0; i < 3; i++)
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(center + Main.rand.NextVector2Circular(12f, 12f),
                    new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), -Main.rand.NextFloat(1.6f, 2.6f)), Color.Lerp(Color.WhiteSmoke, WaterCore, 0.3f),
                    32, Main.rand.NextFloat(0.6f, 1f), 0.75f, Main.rand.NextFloat(-0.4f, 0.4f), true));
        }

        // 火+冰 熔融冲击：七点裂纹星（有意用奇数，和冰的六方晶格区分），火星从裂缝里向下滴落而非上扬。
        private static void MeltingImpactEffect(Vector2 center)
        {
            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, Color.Lerp(FireHot, IceCore, 0.3f), 1.1f, 18));
            SpawnRadialCracks(center, 7, 26f, IceCore, 20);

            for (int i = 0; i < 7; i++)
            {
                float angle = MathHelper.TwoPi * i / 7f + MathHelper.Pi / 7f;
                Vector2 dir = angle.ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new SparkParticle(center + dir * 20f, new Vector2(dir.X * 0.6f, Main.rand.NextFloat(1.4f, 2.6f)),
                    false, Main.rand.Next(20, 30), Main.rand.NextFloat(0.7f, 1f), FireHot));
            }

            GeneralParticleHandler.SpawnParticle(new MediumMistParticle(center, new Vector2(0f, -0.6f), IceCore, Color.Transparent, 0.8f, 0.6f));
        }

        // 火+雷 超载：全场最重的一击，中心双闪 + 三层错位冲击波 + 24 点黄金角喷发 + 六道分叉活电弧 + 碎屑。
        private static void OverloadEffect(Vector2 center)
        {
            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, Color.Lerp(FireHot, LightningCore, 0.4f), 2.2f, 30));
            GeneralParticleHandler.SpawnParticle(new BloomRing(center, Vector2.Zero, LightningDeep, 1.8f, 34));

            for (int i = 0; i < 3; i++)
            {
                float rot = Main.rand.NextFloat(-0.5f, 0.5f);
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, Color.Lerp(FireHot, LightningDeep, i / 2f),
                    new Vector2(1f + 0.18f * i, 1f + 0.1f * i), rot, 0.11f * (1f + 0.1f * i), 0.95f, 22 + 4 * i));
            }

            const int seeds = 24;
            for (int i = 0; i < seeds; i++)
            {
                Vector2 dir = (i * GoldenAngle).ToRotationVector2();
                float speed = Main.rand.NextFloat(6f, 11f);
                Color color = Main.rand.NextBool() ? FireHot : LightningCore;
                Particle spark = new SparkParticle(center, dir * speed, false, Main.rand.Next(18, 28), Main.rand.NextFloat(0.9f, 1.4f), color);
                spark.Rotation = dir.ToRotation();
                GeneralParticleHandler.SpawnParticle(spark);
            }

            for (int i = 0; i < 6; i++)
            {
                Vector2 dir = Main.rand.NextVector2Unit();
                GeneralParticleHandler.SpawnParticle(new AzureThunderArcParticle(center, center + dir * Main.rand.NextFloat(60f, 120f), LightningDeep, 16, 1.4f, 1.1f, true));
            }

            for (int i = 0; i < 16; i++)
                GeneralParticleHandler.SpawnParticle(new SquareParticle(center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 7f), false,
                    Main.rand.Next(14, 22), Main.rand.NextFloat(0.7f, 1.1f), Color.Lerp(FireHot, LightningCore, 0.5f)));
        }

        // 火+自然 灼烧点燃闪：五点裂纹（草木碳化）+ 火星四溅，之后转入 EmitScorchAura 常驻。
        private static void ScorchIgniteEffect(Vector2 center)
        {
            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, FireHot, 0.9f, 16));
            SpawnRadialCracks(center, 5, 20f, NatureDeep, 16);
            for (int i = 0; i < 10; i++)
                GeneralParticleHandler.SpawnParticle(new PointParticle(center + Main.rand.NextVector2Circular(14f, 14f),
                    new Vector2(Main.rand.NextFloat(-1f, 1f), -Main.rand.NextFloat(1f, 2.6f)), false, Main.rand.Next(16, 24),
                    Main.rand.NextFloat(0.5f, 0.9f), Color.Lerp(FireHot, NatureDeep, Main.rand.NextFloat(0.4f))));
        }

        // 火+疾病 瘫痪：故意用完全随机角度的电火花（而非黄金角），做出"神经错乱"式的抽搐乱跳感。
        private static void ParalysisEffect(Vector2 center)
        {
            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, Color.Lerp(FireHot, DiseaseDeep, 0.5f), 0.8f, 14));
            GeneralParticleHandler.SpawnParticle(new PulseRing(center, Vector2.Zero, DiseaseDeep, 0.12f, 0.9f, 18));

            for (int i = 0; i < 10; i++)
            {
                Vector2 dir = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new ElectricSpark(center + dir * Main.rand.NextFloat(6f, 20f), dir * Main.rand.NextFloat(0.5f, 2f),
                    Color.Lerp(FireHot, Color.White, 0.4f), DiseaseDeep, Main.rand.NextFloat(0.4f, 0.7f), Main.rand.Next(10, 16),
                    MathHelper.TwoPi, Main.rand.NextFloat(2f, 5f), 0.5f));
            }

            GeneralParticleHandler.SpawnParticle(new VoidSparkParticle(center, Vector2.Zero, false, 18, 0.7f, DiseaseDeep, 0.97f));
        }

        // 水+冰 冻结定身：八点径向对称，全部零速度瞬间成形，和其它反应向外飞散的动感形成"骤然静止"的反差。
        private static void FreezeSnapEffect(NPC npc)
        {
            Vector2 center = npc.Center;
            float radius = Math.Max(npc.width, npc.height) * 0.3f;
            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, IceCore, 1.4f, 16));

            const int points = 8;
            for (int i = 0; i < points; i++)
            {
                float angle = MathHelper.TwoPi * i / points;
                Vector2 dir = angle.ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new CrackParticle(center + dir * radius, Vector2.Zero, IceCore, Vector2.One, angle, 0.6f, 1.2f, 22));
                GeneralParticleHandler.SpawnParticle(new SnowflakeSparkle(center + dir * radius * 0.55f, Vector2.Zero, IceCore, IceDeep, 0.7f, 24, 0.02f, 0.8f, 6));
            }

            GeneralParticleHandler.SpawnParticle(new MediumMistParticle(center, Vector2.Zero, IceCore, Color.Transparent, 1.2f, 0.8f));
        }

        // 水+雷 感电点燃闪：随机方向甩出的几道活电弧 + 被震飞的水珠，之后转入 EmitElectrifiedAura 常驻。
        private static void ElectrifiedIgniteEffect(Vector2 center)
        {
            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, Color.Lerp(WaterCore, LightningCore, 0.5f), 1f, 16));
            for (int i = 0; i < 4; i++)
            {
                Vector2 dir = Main.rand.NextVector2Unit();
                GeneralParticleHandler.SpawnParticle(new AzureThunderArcParticle(center - dir * 10f, center + dir * Main.rand.NextFloat(40f, 80f),
                    Color.Lerp(WaterDeep, LightningDeep, 0.5f), 14, 1f, 0.9f, false));
            }
            for (int i = 0; i < 8; i++)
                GeneralParticleHandler.SpawnParticle(new WaterGlobParticle(center, Main.rand.NextVector2Circular(3f, 3f), 1f, 0.05f, Main.rand.Next(20, 30)));
        }

        // 水+自然 生长：环半径按斐波那契数列 1,1,2,3,5 依次扩张，配合黄金角新芽——两种植物学公式叠在一起。
        private static void GrowthEffect(Vector2 center)
        {
            int[] fib = { 1, 1, 2, 3, 5 };
            for (int i = 0; i < fib.Length; i++)
            {
                float finalScale = fib[i] * 6f / 60f;
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, Color.Lerp(WaterCore, NatureCore, i / (float)(fib.Length - 1)),
                    Vector2.One, 0f, 0.05f + 0.01f * i, finalScale, 20 + 3 * i));
            }

            const int buds = 12;
            for (int i = 0; i < buds; i++)
            {
                Vector2 dir = (i * GoldenAngle).ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(center + dir * MathF.Sqrt(i + 1f) * 5f, dir * 0.6f, NatureCore, WaterCore,
                    Main.rand.NextFloat(0.5f, 0.8f), 22, 0.015f, 1f));
            }
        }

        // 水+疾病 凋零：黄金角螺旋"向内收缩"（速度指向圆心，半径随索引递减），把"生长"的公式反过来用表示枯萎；烟雾向下垂坠。
        private static void WitherEffect(Vector2 center)
        {
            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, DiseaseDeep, 0.7f, 20));

            const int seeds = 14;
            for (int i = 0; i < seeds; i++)
            {
                float radius = MathF.Sqrt(seeds - i) * 8f;
                Vector2 dir = (i * GoldenAngle).ToRotationVector2();
                Vector2 spawn = center + dir * radius;
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(spawn, -dir * Main.rand.NextFloat(0.6f, 1.1f), Color.Lerp(DiseaseCore, WaterDeep, 0.4f),
                    DiseaseDeep, Main.rand.NextFloat(0.4f, 0.7f), 26, 0.012f, 0.9f));
            }

            for (int i = 0; i < 4; i++)
                GeneralParticleHandler.SpawnParticle(new SmallSmokeParticle(center + Main.rand.NextVector2Circular(16f, 16f),
                    new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(0.6f, 1.4f)), DiseaseDeep, Color.Black, 0.5f, 0.6f, 30, false));
        }

        // 冰+雷 冷凝共振：六边形节点各自发光，相邻节点间随机连一道电火花——像是"能量在固定节点上凝结"的共振图。
        private static void CondensationEffect(Vector2 center)
        {
            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, Color.Lerp(IceCore, LightningCore, 0.5f), 1.2f, 18));

            const int nodes = 6;
            const float radius = 34f;
            Vector2[] positions = new Vector2[nodes];
            for (int i = 0; i < nodes; i++)
            {
                positions[i] = PolygonVertex(center, radius, i, 0f);
                GeneralParticleHandler.SpawnParticle(new GlowSquareParticle(positions[i], Vector2.Zero, false, 24, 0.5f, IceCore, true, 0.06f));
            }

            for (int i = 0; i < nodes; i++)
            {
                if (!Main.rand.NextBool(2))
                    continue;

                Vector2 next = positions[(i + 1) % nodes];
                Vector2 mid = (positions[i] + next) * 0.5f;
                GeneralParticleHandler.SpawnParticle(new ElectricSpark(mid, (next - positions[i]).SafeNormalize(Vector2.UnitX) * 2f, LightningCore, IceDeep,
                    0.5f, 14, MathHelper.PiOver4, 4f, 0.4f));
            }

            GeneralParticleHandler.SpawnParticle(new MediumMistParticle(center, Vector2.Zero, IceCore, Color.Transparent, 1f, 0.7f));
        }

        // 冰+自然 冷藏：六边形晶壳（冰+绿双色雪花）+ 珍珠光点上浮——特意选用"珍贵/宝石"质感呼应它给敌人加价值的机制。
        private static void ColdStorageEffect(Vector2 center)
        {
            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, Color.Lerp(IceCore, NatureCore, 0.4f), 0.9f, 20));

            const int shell = 6;
            for (int i = 0; i < shell; i++)
            {
                Vector2 pos = PolygonVertex(center, 30f, i, MathHelper.PiOver4);
                GeneralParticleHandler.SpawnParticle(new SnowflakeSparkle(pos, Vector2.Zero, IceCore, NatureCore, 0.6f, 26, 0.015f, 0.7f, 6));
            }

            for (int i = 0; i < 5; i++)
                GeneralParticleHandler.SpawnParticle(new PearlParticle(center + Main.rand.NextVector2Circular(20f, 20f),
                    new Vector2(0f, -Main.rand.NextFloat(0.8f, 1.6f)), false, Main.rand.Next(24, 32), Main.rand.NextFloat(0.6f, 0.9f),
                    Color.Lerp(NatureCore, Color.Gold, 0.35f), 0.95f, 0.03f, false));
        }

        // 冰+疾病 腐冻：命中(62%)是黑紫裂纹星 + 虚空火花的暴力破裂；回血(38%)则是平静的黄金角绿冰螺旋，两种结果一眼可辨。
        private static void CorruptFreezeEffect(Vector2 center, bool wasCritical)
        {
            if (wasCritical)
            {
                Color accent = Color.Lerp(IceDeep, DiseaseDeep, 0.5f);
                GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, accent, 1.6f, 22));
                SpawnRadialCracks(center, 9, 30f, accent, 20);
                for (int i = 0; i < 18; i++)
                    GeneralParticleHandler.SpawnParticle(new VoidSparkParticle(center, Main.rand.NextVector2Circular(6f, 6f), false,
                        Main.rand.Next(14, 22), Main.rand.NextFloat(0.5f, 0.9f), DiseaseDeep, 0.95f));
            }
            else
            {
                GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, Color.Lerp(NatureCore, IceCore, 0.5f), 0.9f, 16));
                for (int i = 0; i < 10; i++)
                {
                    Vector2 dir = (i * GoldenAngle).ToRotationVector2();
                    GeneralParticleHandler.SpawnParticle(new GenericSparkle(center + dir * 16f, dir * 0.5f, NatureCore, IceCore,
                        Main.rand.NextFloat(0.5f, 0.8f), 24, 0.015f, 0.9f));
                }
            }
        }

        // 雷+自然 繁茂点燃闪：五道点粒子沿正五边形方向喷出，之后转入 EmitFlourishAura 常驻的自转锁定环。
        private static void FlourishIgniteEffect(Vector2 center)
        {
            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, Color.Lerp(NatureCore, LightningCore, 0.5f), 0.9f, 16));
            for (int i = 0; i < 5; i++)
            {
                Vector2 dir = (MathHelper.TwoPi * i / 5f).ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new PointParticle(center, dir * Main.rand.NextFloat(2f, 4f), false, Main.rand.Next(16, 22),
                    Main.rand.NextFloat(0.6f, 0.9f), Color.Lerp(NatureCore, LightningCore, Main.rand.NextFloat())));
            }
        }

        // 雷+疾病 支配：五边形顶点隔一个相连，正是经典的{5/2}五角星连线——"束缚法阵"的数学原型。
        private static void ControlBindEffect(NPC npc)
        {
            Vector2 center = npc.Center;
            float radius = MathHelper.Clamp(Math.Max(npc.width, npc.height), 24f, 100f) * 0.5f;
            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, Color.Lerp(LightningDeep, DiseaseDeep, 0.5f), 1.3f, 20));

            const int points = 5;
            Vector2[] verts = new Vector2[points];
            for (int i = 0; i < points; i++)
                verts[i] = PolygonVertex(center, radius, i, -MathHelper.PiOver2, points);

            for (int i = 0; i < points; i++)
            {
                Vector2 a = verts[i];
                Vector2 b = verts[(i + 2) % points];
                GeneralParticleHandler.SpawnParticle(new AzureThunderArcParticle(a, b, Color.Lerp(LightningDeep, DiseaseDeep, 0.5f), 20, 1f, 0.6f, false));
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(a, Vector2.Zero, LightningCore, DiseaseCore, 0.6f, 24, 0.015f, 0.9f));
            }
        }

        // 自然+疾病 中合：唯一一个"向内收缩"的环（起始比结束大），配合向心的黄金角光点，表现"能量归于平静"。
        private static void NeutralizationEffect(Vector2 center)
        {
            GeneralParticleHandler.SpawnParticle(new GenericBloom(center, Vector2.Zero, Color.Lerp(NatureCore, DiseaseCore, 0.5f), 1f, 26));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, Color.Lerp(NatureDeep, DiseaseDeep, 0.5f),
                Vector2.One, 0f, 1.3f, 0.35f, 30));

            const int motes = 10;
            for (int i = 0; i < motes; i++)
            {
                Vector2 dir = (i * GoldenAngle).ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(center + dir * 40f, -dir * Main.rand.NextFloat(0.5f, 0.9f),
                    Color.Lerp(NatureCore, Color.White, 0.3f), DiseaseCore, Main.rand.NextFloat(0.4f, 0.7f), 30, 0.01f, 0.8f));
            }
        }

        // ══════════════════════════════════════════════════════════
        // 感电连锁传播的活电弧（替代旧的 Dust 连线）
        // ══════════════════════════════════════════════════════════

        public static void EmitElectrifiedArc(Vector2 from, Vector2 to)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new AzureThunderArcParticle(from, to, LightningDeep, 20, 1.3f, 1f, true));

            Vector2 forward = (to - from).SafeNormalize(Vector2.UnitX);
            GeneralParticleHandler.SpawnParticle(new ElectricSpark(from, forward * 2f, LightningCore, LightningDeep, 0.7f, 14, MathHelper.PiOver4, 5f, 0.6f));
            GeneralParticleHandler.SpawnParticle(new ElectricSpark(to, -forward * 2f, LightningCore, LightningDeep, 0.7f, 14, MathHelper.PiOver4, 5f, 0.6f));
        }
    }
}
