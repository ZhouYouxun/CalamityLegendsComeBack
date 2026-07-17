using System;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.HDMC
{
    /// <summary>
    /// 超维矩阵主宰——攻击模组实现与程序化绘制。
    /// 5 阶段 × 每阶段 6 种攻击 = 30 模组；同一武器模组衍生多个 Boss 形态变体。
    /// 所有弹幕遵循同一分寸：图案化、有预警、射出后不追踪。
    ///
    /// ── P1 校准（1-6）──
    ///  1 数据面板·三翼      玩家上方三面板齐射
    ///  2 几何爆裂·三点环    玩家周围三方位多面体爆碎片
    ///  3 莫比乌斯·点射      Boss 环面连发单矛
    ///  4 数据矛·向心轮盘    快照点四周凝滞成环 → 向心收拢（离开原地即安全）
    ///  5 碎片幕帘           侧向横扫的矛幕（135px 间隙可穿）
    ///  6 扫描十字           快照点正十字 / 斜十字激光
    /// ── P2 展开（7-12）──
    ///  7 斐波那契·螺旋雨    金角序列旋转喷射
    ///  8 分形地刺·三连      地下预警 → 破土爆射
    ///  9 七芒星·封印        定点纹章 → 七道汇聚激光
    /// 10 数据面板·四方合围  四面板错峰节拍齐射
    /// 11 莫比乌斯·交叉火力  环面对点双矛交叉
    /// 12 天降矛雨           头顶横列矛阵垂落（3 波）
    /// ── P3 过载（13-18）──
    /// 13 沃罗诺伊·囚笼      雷达扫描封锁牢笼
    /// 14 彭罗斯·陨晶雨      高空坍缩 → 天降晶核
    /// 15 克利福德·光轮      环面节点旋转风车激光
    /// 16 几何爆裂·多米诺    朝玩家方向行军的连锁引爆链
    /// 17 七芒星·双星        两次快照连续封印
    /// 18 激光栅栏·行军      纵向激光栅栏两波错位
    /// ── P4 临界（19-24）──
    /// 19 洛伦兹·乱流        漂移混沌吸引子区 + 扇形矛
    /// 20 聚变·新星          金属球汇聚 → 带缺口环波
    /// 21 谢尔宾斯基·坍缩    波次点亮分形三角
    /// 22 分形·四象地刺      四方向分形树向心生长
    /// 23 彭罗斯·流星链      五连小陨晶追迹玩家足迹
    /// 24 环波·三重奏        三点错峰环波干涉
    /// ── P5 编译风暴（25-30）──
    /// 25 编译风暴           全模组连锁总攻（脚本化）
    /// 26 模组·随机脉冲      高频随机轻量模组
    /// 27 克利福德·全域风车  双波风车 + 瞄准束
    /// 28 奇点·引力井        微型奇点：牵引 + 环波 + 爆心
    /// 29 沃罗诺伊·双重牢笼  错位双笼反向扫描
    /// 30 矩阵审判·矛雨      全屏列阵矛雨（含安全列）
    /// </summary>
    public sealed partial class HDMCSovereign
    {
        private static int GetAttackDuration(int id) => id switch
        {
            1 => 300, 2 => 250, 3 => 320, 4 => 290, 5 => 300, 6 => 280,
            7 => 350, 8 => 270, 9 => 250, 10 => 330, 11 => 330, 12 => 300,
            13 => 270, 14 => 330, 15 => 340, 16 => 300, 17 => 300, 18 => 340,
            19 => 390, 20 => 330, 21 => 290, 22 => 320, 23 => 340, 24 => 320,
            25 => 640, 26 => 370, 27 => 380, 28 => 330, 29 => 360, 30 => 380,
            _ => 60
        };

        internal void ExecuteAttack(int id, Player target)
        {
            switch (id)
            {
                case 1:  Attack_GridTriad(target); break;
                case 2:  Attack_GeoTriRing(target); break;
                case 3:  Attack_MobiusVolley(target); break;
                case 4:  Attack_LanceWheel(target); break;
                case 5:  Attack_ShardCurtain(target); break;
                case 6:  Attack_ScanCross(target); break;
                case 7:  Attack_FibonacciSweep(target); break;
                case 8:  Attack_FractalTriple(target); break;
                case 9:  Attack_HeptagramSeal(target); break;
                case 10: Attack_GridSiege(target); break;
                case 11: Attack_MobiusCrossfire(target); break;
                case 12: Attack_SkyLanceRain(target); break;
                case 13: Attack_VoronoiPrison(target); break;
                case 14: Attack_PenroseRain(target); break;
                case 15: Attack_CliffordSweep(target); break;
                case 16: Attack_GeoDomino(target); break;
                case 17: Attack_HeptagramTwin(target); break;
                case 18: Attack_LaserFence(target); break;
                case 19: Attack_LorenzFlux(target); break;
                case 20: Attack_FusionNova(target); break;
                case 21: Attack_SierpinskiCollapse(target); break;
                case 22: Attack_FractalCross(target); break;
                case 23: Attack_PenroseChain(target); break;
                case 24: Attack_RingTrio(target); break;
                case 25: Attack_CompileStorm(target); break;
                case 26: Attack_ModulePulse(target); break;
                case 27: Attack_CliffordWindmill(target); break;
                case 28: Attack_GravityWell(target); break;
                case 29: Attack_VoronoiTwin(target); break;
                case 30: Attack_JudgmentRain(target); break;
                default: SwitchState(StateRepos); return;
            }

            if (Timer >= GetAttackDuration(id))
                SwitchState(StateRepos);
        }

        /// <summary>从 Boss 发射一枚"凝滞展开"数据矛，方向 = 当前瞄准玩家（射出后不追踪）。</summary>
        private void FireAimedLance(Player target, float spreadDegrees, float damageMult, float maxSpeed = 18f)
        {
            Vector2 dir = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY)
                .RotatedBy(MathHelper.ToRadians(spreadDegrees));
            SpawnHostile<HDMCLanceHostile>(NPC.Center + dir * 60f, dir * 2.4f, damageMult, maxSpeed, 10f);
        }

        // ══════════════════════════════════════════════════
        // P1 · 校准阶段
        // ══════════════════════════════════════════════════

        /// <summary>1 · 数据面板·三翼：玩家两翼+头顶展开面板，间隙点射。</summary>
        private void Attack_GridTriad(Player target)
        {
            HoverBesideTarget(target, HoverSide * 480f, -260f, 16f, 24f);

            if (Timer == 30)
            {
                SpawnHostile<HDMCGridPanelHostile>(target.Center + new Vector2(-390f, -150f), Vector2.Zero, 0.75f);
                SpawnHostile<HDMCGridPanelHostile>(target.Center + new Vector2(390f, -150f), Vector2.Zero, 0.75f);
                SpawnHostile<HDMCGridPanelHostile>(target.Center + new Vector2(0f, -380f), Vector2.Zero, 0.75f);

                if (!Main.dedServ)
                    SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.4f, Pitch = 0.24f, MaxInstances = 3 }, NPC.Center);
            }

            if (Timer == 130 || Timer == 210)
            {
                FireAimedLance(target, -6f, 0.7f);
                FireAimedLance(target, 6f, 0.7f);
            }
        }

        /// <summary>2 · 几何爆裂·三点环：三方位多面体错峰爆破。</summary>
        private void Attack_GeoTriRing(Player target)
        {
            HoverBesideTarget(target, HoverSide * 440f, -280f, 16f, 24f);

            if (Timer == 25)
            {
                float baseAngle = HDMCUtil.Hash01(NPC.whoAmI * 31 + (int)NPC.ai[1]) * MathHelper.TwoPi;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 offset = (baseAngle + MathHelper.TwoPi * i / 3f).ToRotationVector2() * 320f;
                    SpawnHostile<HDMCGeoBurstHostile>(target.Center + offset, Vector2.Zero, 0.8f, 0f, i * 9f);
                }

                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndGeoBurst) { Volume = 0.55f }, target.Center);
            }
        }

        private const float MobiusRadius = 135f;
        private const int MobiusStripPts = 28;

        /// <summary>3 · 莫比乌斯·点射：环面采样点连发单矛。</summary>
        private void Attack_MobiusVolley(Player target)
        {
            HoverBesideTarget(target, HoverSide * 420f, -230f, 15f, 26f);

            if (Timer > 70 && Timer < 260 && Timer % 13 == 0)
            {
                Vector2[] pts = GetMobiusPoints(NPC.Center, Main.GlobalTimeWrappedHourly);
                int idx = (Timer / 13 * 5) % pts.Length;
                Vector2 from = pts[idx];
                Vector2 dir = (target.Center - from).SafeNormalize(Vector2.UnitY);
                SpawnHostile<HDMCLanceHostile>(from, dir * 2.6f, 0.85f, 19f, 7f);

                if (!Main.dedServ)
                    SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.22f, Pitch = 0.5f, MaxInstances = 5 }, from);
            }
        }

        /// <summary>莫比乌斯环采样（同武器模组的参数投影）。</summary>
        private Vector2[] GetMobiusPoints(Vector2 center, float time)
        {
            Vector2[] pts = new Vector2[MobiusStripPts];
            Matrix rot = Matrix.CreateFromYawPitchRoll(time * 0.8f, time * 0.55f, time * 0.3f);

            for (int i = 0; i < MobiusStripPts; i++)
            {
                float u = MathHelper.TwoPi * i / MobiusStripPts;
                Vector3 pt3 = Vector3.Transform(
                    new Vector3(MobiusRadius * MathF.Cos(u), MobiusRadius * MathF.Sin(u), 0f), rot);
                float perspective = 620f / MathF.Max(180f, 620f + pt3.Z);
                pts[i] = center + new Vector2(pt3.X * perspective, pt3.Y * perspective);
            }

            return pts;
        }

        /// <summary>
        /// 4 · 数据矛·向心轮盘：以玩家快照点为心，四周 480px 处凝滞成环的
        /// 10 枚矛向心收拢。36° 间距 + 长凝滞预警——离开快照点即安全。
        /// </summary>
        private void Attack_LanceWheel(Player target)
        {
            HoverBesideTarget(target, HoverSide * 460f, -280f, 16f, 24f);

            // 向心轮盘主体：P3+ 追加第三环，P4+ 每环 12 枚（安全窗更窄）。
            bool ring = Timer == 40 || Timer == 150 || (Phase >= 3 && Timer == 250);
            if (ring)
            {
                Vector2 snapshot = target.Center;
                int count = Phase >= 4 ? 12 : 10;
                float baseAngle = HDMCUtil.Hash01(NPC.whoAmI * 23 + Timer) * MathHelper.TwoPi;
                for (int i = 0; i < count; i++)
                {
                    Vector2 dir = (baseAngle + MathHelper.TwoPi * i / count).ToRotationVector2();
                    Vector2 from = snapshot + dir * 480f;
                    SpawnHostile<HDMCLanceHostile>(from, -dir * 2f, 0.75f, 15f, 26f);
                }

                if (!Main.dedServ)
                    SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.35f, Pitch = 0.1f, MaxInstances = 3 }, snapshot);
            }

            // 底流：环与环之间的持续压力——每 34 帧一枚慢速瞄准矛（P2+）。
            // 让"躲到快照点外站着不动"不再是免费解，但矛速慢、单发轻，保持可读。
            if (Phase >= 2 && Timer > 70 && Timer < 260 && Timer % 34 == 0)
                FireAimedLance(target, (Timer / 34 % 2 == 0 ? 8f : -8f), 0.6f, 13f);
        }

        /// <summary>
        /// 5 · 碎片幕帘：从玩家一侧 900px 外横扫来一列矛幕（间距 135px 可穿），
        /// 第二波从对侧错半格——迫使纵向微调两次。
        /// </summary>
        private void Attack_ShardCurtain(Player target)
        {
            HoverBesideTarget(target, HoverSide * 440f, -300f, 16f, 24f);

            // 幕帘主体：P3+ 追加第三波（从上一波对侧再扫回来）。
            bool wave = Timer == 30 || Timer == 150 || (Phase >= 3 && Timer == 250);
            if (wave)
            {
                float side = Timer == 30 ? HoverSide : (Timer == 150 ? -HoverSide : HoverSide);
                float xFrom = target.Center.X + side * 900f;
                float yOffset = Timer == 150 ? 67f : 0f; // 第二波错半格
                Vector2 dir = new(-side, 0f);

                for (int i = -3; i <= 3; i++)
                {
                    Vector2 from = new(xFrom, target.Center.Y + i * 135f + yOffset);
                    SpawnHostile<HDMCLanceHostile>(from, dir * 2f, 0.75f, 14f, 20f);
                }

                if (!Main.dedServ)
                    SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.3f, Pitch = 0.3f, MaxInstances = 4 }, target.Center);
            }

            // 底流：横扫幕帘之间补一枚慢速瞄准矛（P2+），封住"站在幕帘缝里等下一波"的白嫖。
            if (Phase >= 2 && Timer > 60 && Timer < 260 && Timer % 40 == 0)
                FireAimedLance(target, 0f, 0.6f, 13f);
        }

        /// <summary>
        /// 6 · 扫描十字：玩家快照点先来一记正十字激光，再来一记斜十字。
        /// 斜向走位即可躲第一记，直向走位躲第二记。
        /// </summary>
        private void Attack_ScanCross(Player target)
        {
            HoverBesideTarget(target, HoverSide * 500f, -240f, 15f, 26f);

            if (Timer == 30 || Timer == 150)
            {
                Vector2 snapshot = target.Center;
                float baseAngle = Timer == 30 ? 0f : MathHelper.PiOver4;
                for (int k = 0; k < 2; k++)
                {
                    Vector2 dir = (baseAngle + MathHelper.PiOver2 * k).ToRotationVector2();
                    Vector2 origin = snapshot - dir * 750f;
                    SpawnHostile<HDMCLaserHostile>(origin, dir, 0.85f, 1500f, 55f);
                }

                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndSpaceWarp) { Volume = 0.4f }, snapshot);
            }
        }

        // ══════════════════════════════════════════════════
        // P2 · 展开阶段
        // ══════════════════════════════════════════════════

        private const float GoldenAngle = 2.399963f;
        private const float SpiralA = 10f;
        private const float SpiralB = 0.12f;
        private const float SpiralMaxR = 165f;
        private const int SpiralNodes = 18;

        private Vector2 GetSpiralNode(int index, float rotation)
        {
            float angle = index * GoldenAngle;
            float r = Math.Min(SpiralA * MathF.Exp(SpiralB * angle), SpiralMaxR);
            return NPC.Center + (angle + rotation).ToRotationVector2() * r;
        }

        /// <summary>7 · 斐波那契·螺旋雨：金角序列旋转喷射，每三发补一枚慢速瞄准矛。</summary>
        private void Attack_FibonacciSweep(Player target)
        {
            HoverBesideTarget(target, HoverSide * 400f, -300f, 14f, 28f);

            if (Timer > 90 && Timer % 9 == 0)
            {
                int k = (Timer - 90) / 9;
                if (k < SpiralNodes * 1.6f)
                {
                    int nodeIdx = k % SpiralNodes;
                    float rotation = Main.GlobalTimeWrappedHourly * 0.4f;
                    Vector2 node = GetSpiralNode(nodeIdx, rotation);
                    Vector2 dir = (node - NPC.Center).SafeNormalize(Vector2.UnitX);
                    SpawnHostile<HDMCLanceHostile>(node, dir * 2.2f, 0.75f, 16.5f, 6f);

                    if (k % 3 == 2)
                        FireAimedLance(target, (k % 2 == 0 ? 4f : -4f), 0.7f, 14f);

                    if (!Main.dedServ && k % 2 == 0)
                        SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.2f, Pitch = 0.45f, MaxInstances = 5 }, node);
                }
            }
        }

        /// <summary>8 · 分形地刺·三连：玩家脚下三棵分形树破土。</summary>
        private void Attack_FractalTriple(Player target)
        {
            HoverBesideTarget(target, HoverSide * 460f, -300f, 16f, 24f);

            if (Timer == 20)
            {
                const float spacing = 370f;
                for (int i = -1; i <= 1; i++)
                {
                    Vector2 root = new(target.Center.X + spacing * i, target.Center.Y + 280f);
                    SpawnHostile<HDMCFractalTreeHostile>(root, Vector2.Zero, 0.8f);
                }
            }
        }

        /// <summary>9 · 七芒星·封印：玩家快照点纹章 → 七道汇聚激光。</summary>
        private void Attack_HeptagramSeal(Player target)
        {
            HoverBesideTarget(target, HoverSide * 430f, -260f, 15f, 26f);

            if (Timer == 20)
            {
                SpawnHostile<HDMCHeptagramHostile>(target.Center, Vector2.Zero, 1f);
                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndInscription) { Volume = 0.6f }, target.Center);
            }

            if (Timer == 110 || Timer == 170)
                FireAimedLance(target, Timer == 110 ? -5f : 5f, 0.7f, 15f);
        }

        /// <summary>
        /// 10 · 数据面板·四方合围：东南西北四面板错峰生成——
        /// 开火节拍依次到来，形成"四拍"节奏躲避。
        /// </summary>
        private void Attack_GridSiege(Player target)
        {
            HoverBesideTarget(target, HoverSide * 470f, -260f, 15f, 26f);

            // 错峰 20 帧 → 开火时刻也错开（面板固定 96 帧后开火）
            if (Timer == 25 || Timer == 45 || Timer == 65 || Timer == 85)
            {
                int step = (Timer - 25) / 20;
                Vector2 offset = step switch
                {
                    0 => new Vector2(0f, -400f),
                    1 => new Vector2(400f, 0f),
                    2 => new Vector2(0f, 400f),
                    _ => new Vector2(-400f, 0f)
                };
                SpawnHostile<HDMCGridPanelHostile>(target.Center + offset, Vector2.Zero, 0.7f);

                if (!Main.dedServ)
                    SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.3f, Pitch = 0.2f + step * 0.08f, MaxInstances = 4 }, target.Center + offset);
            }

            if (Timer == 240 || Timer == 290)
            {
                FireAimedLance(target, -7f, 0.7f);
                FireAimedLance(target, 7f, 0.7f);
            }
        }

        /// <summary>11 · 莫比乌斯·交叉火力：环面对点成对开火，双向交叉压迫。</summary>
        private void Attack_MobiusCrossfire(Player target)
        {
            HoverBesideTarget(target, 0f, -320f, 15f, 26f);

            if (Timer > 70 && Timer < 280 && Timer % 17 == 0)
            {
                Vector2[] pts = GetMobiusPoints(NPC.Center, Main.GlobalTimeWrappedHourly);
                int idx = (Timer / 17 * 7) % pts.Length;
                int opposite = (idx + pts.Length / 2) % pts.Length;

                foreach (int i in new[] { idx, opposite })
                {
                    Vector2 from = pts[i];
                    Vector2 dir = (target.Center - from).SafeNormalize(Vector2.UnitY);
                    SpawnHostile<HDMCLanceHostile>(from, dir * 2.4f, 0.8f, 17f, 9f);
                }

                if (!Main.dedServ)
                    SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.25f, Pitch = 0.45f, MaxInstances = 5 }, NPC.Center);
            }
        }

        /// <summary>
        /// 12 · 天降矛雨：玩家头顶横列 9 枚矛凝滞成阵后垂落，三波，
        /// 每波以玩家当下位置为中心——横向持续移动即可安全。
        /// </summary>
        private void Attack_SkyLanceRain(Player target)
        {
            HoverBesideTarget(target, HoverSide * 490f, -280f, 16f, 24f);

            if (Timer == 40 || Timer == 120 || Timer == 200)
            {
                for (int i = -4; i <= 4; i++)
                {
                    float jitter = (HDMCUtil.Hash01(i * 41 + Timer) - 0.5f) * 46f;
                    Vector2 from = new(target.Center.X + i * 150f + jitter, target.Center.Y - 380f);
                    SpawnHostile<HDMCLanceHostile>(from, Vector2.UnitY * 2f, 0.75f, 16f, 22f);
                }

                if (!Main.dedServ)
                    SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.32f, Pitch = 0.35f, MaxInstances = 4 }, target.Center);
            }
        }

        // ══════════════════════════════════════════════════
        // P3 · 过载阶段
        // ══════════════════════════════════════════════════

        /// <summary>13 · 沃罗诺伊·囚笼：雷达扫描封锁牢笼。</summary>
        private void Attack_VoronoiPrison(Player target)
        {
            HoverBesideTarget(target, HoverSide * 500f, -220f, 15f, 26f);

            if (Timer == 15)
                SpawnHostile<HDMCVoronoiHostile>(target.Center, Vector2.Zero, 0.85f);
        }

        /// <summary>14 · 彭罗斯·陨晶雨：三批锁定落点的天降晶核。</summary>
        private void Attack_PenroseRain(Player target)
        {
            HoverBesideTarget(target, HoverSide * 450f, -320f, 15f, 26f);

            if (Timer == 25 || Timer == 90 || Timer == 155)
            {
                float off = (Timer switch { 25 => -1f, 90 => 0f, _ => 1f }) * 240f;
                SpawnHostile<HDMCPenroseHostile>(target.Center + new Vector2(off, 60f), Vector2.Zero, 1f);
            }
        }

        /// <summary>15 · 克利福德·光轮：三波 120° 风车激光，首波必有一道对准快照位。</summary>
        private void Attack_CliffordSweep(Player target)
        {
            HoverBesideTarget(target, 0f, -340f, 16f, 24f);

            if (Timer == 80 || Timer == 170 || Timer == 260)
            {
                int wave = Timer / 90;
                float baseAngle = Timer == 80
                    ? (target.Center - NPC.Center).ToRotation()
                    : HDMCUtil.Hash01(NPC.whoAmI * 47 + Timer) * MathHelper.TwoPi;

                for (int k = 0; k < 3; k++)
                {
                    Vector2 dir = (baseAngle + MathHelper.TwoPi * k / 3f + wave * 0.35f).ToRotationVector2();
                    Vector2 origin = NPC.Center - dir * 650f;
                    SpawnHostile<HDMCLaserHostile>(origin, dir, 0.9f, 1300f, 44f);
                }

                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndSpaceWarp) { Volume = 0.45f }, NPC.Center);
            }
        }

        /// <summary>
        /// 16 · 几何爆裂·多米诺：五枚多面体沿"Boss→玩家"直线依次落位、
        /// 依次引爆——爆炸链朝玩家行军，侧移一步即可让开整条链。
        /// </summary>
        private void Attack_GeoDomino(Player target)
        {
            HoverBesideTarget(target, HoverSide * 520f, -200f, 15f, 26f);

            if (Timer == 30)
            {
                Vector2 dir = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                for (int i = 0; i < 5; i++)
                {
                    Vector2 pos = NPC.Center + dir * (230f + i * 190f);
                    SpawnHostile<HDMCGeoBurstHostile>(pos, Vector2.Zero, 0.8f, 0f, i * 13f);
                }

                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndGeoBurst) { Volume = 0.55f }, NPC.Center);
            }
        }

        /// <summary>17 · 七芒星·双星：两次快照连续封印，强制两段走位。</summary>
        private void Attack_HeptagramTwin(Player target)
        {
            HoverBesideTarget(target, HoverSide * 440f, -270f, 15f, 26f);

            if (Timer == 20 || Timer == 105)
            {
                SpawnHostile<HDMCHeptagramHostile>(target.Center, Vector2.Zero, 0.95f);
                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndInscription) { Volume = 0.55f, Pitch = Timer == 105 ? 0.15f : 0f }, target.Center);
            }
        }

        /// <summary>
        /// 18 · 激光栅栏·行军：六道纵向激光栅栏（间距 340px 即安全巷），
        /// 第二波错半格——必须换巷。
        /// </summary>
        private void Attack_LaserFence(Player target)
        {
            HoverBesideTarget(target, 0f, -380f, 16f, 24f);

            if (Timer == 40 || Timer == 175)
            {
                float xOffset = Timer == 40 ? 0f : 170f;
                for (int i = -3; i <= 2; i++)
                {
                    float x = target.Center.X + i * 340f + 170f + xOffset;
                    Vector2 origin = new(x, target.Center.Y - 700f);
                    SpawnHostile<HDMCLaserHostile>(origin, Vector2.UnitY, 0.85f, 1400f, 58f);
                }

                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndSpaceWarp) { Volume = 0.45f, Pitch = -0.1f }, target.Center);
            }
        }

        // ══════════════════════════════════════════════════
        // P4 · 临界阶段
        // ══════════════════════════════════════════════════

        /// <summary>19 · 洛伦兹·乱流：双漂移混沌区 + 三波扇形矛。</summary>
        private void Attack_LorenzFlux(Player target)
        {
            HoverBesideTarget(target, HoverSide * 470f, -270f, 15f, 26f);

            if (Timer == 25)
            {
                SpawnHostile<HDMCLorenzHostile>(target.Center + new Vector2(-340f, -60f), new Vector2(0.7f, 0.1f), 0.9f);
                SpawnHostile<HDMCLorenzHostile>(target.Center + new Vector2(340f, -60f), new Vector2(-0.7f, 0.1f), 0.9f);

                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndShaderOrbs) { Volume = 0.5f }, target.Center);
            }

            if (Timer == 130 || Timer == 230 || Timer == 320)
            {
                for (int i = -2; i <= 2; i++)
                    FireAimedLance(target, i * 11f, 0.75f, 16f);
            }
        }

        /// <summary>20 · 聚变·新星：金属球汇聚定桩 → 两轮带缺口环波 + 放射碎片。</summary>
        private void Attack_FusionNova(Player target)
        {
            NPC.velocity *= 0.93f;

            if (Timer == 5 && !Main.dedServ)
                SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndFusion) { Volume = 0.6f }, NPC.Center);

            if (Timer == 128 || Timer == 218)
            {
                SpawnHostile<HDMCRingWaveHostile>(NPC.Center, Vector2.Zero, 0.95f, Timer == 128 ? 7f : 8.5f, 1250f);

                float baseAngle = HDMCUtil.Hash01(NPC.whoAmI * 71 + Timer) * MathHelper.TwoPi;
                for (int i = 0; i < 10; i++)
                {
                    Vector2 dir = (baseAngle + MathHelper.TwoPi * i / 10f).ToRotationVector2();
                    SpawnHostile<HDMCShardHostile>(NPC.Center + dir * 30f, dir * 11f, 0.8f);
                }

                if (!Main.dedServ)
                {
                    HDMCUtil.DataBurstParticles(NPC.Center, 20, 12, 10f);
                    HDMCUtil.ScreenShake(NPC.Center, 3f, 900f);
                    SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndFusionBoom) { Volume = 0.6f }, NPC.Center);
                }
            }
        }

        /// <summary>21 · 谢尔宾斯基·坍缩：波次点亮分形三角牢笼。</summary>
        private void Attack_SierpinskiCollapse(Player target)
        {
            HoverBesideTarget(target, HoverSide * 520f, -240f, 15f, 26f);

            if (Timer == 15)
            {
                SpawnHostile<HDMCSierpinskiHostile>(target.Center, Vector2.Zero, 0.9f);
                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndFusion) { Volume = 0.55f }, target.Center);
            }
        }

        /// <summary>
        /// 22 · 分形·四象地刺：四方向分形树向玩家快照点合拢生长——
        /// 上下左右皆刺，安全区在四个对角象限。
        /// </summary>
        private void Attack_FractalCross(Player target)
        {
            HoverBesideTarget(target, HoverSide * 480f, -300f, 15f, 26f);

            if (Timer == 25)
            {
                Vector2 snapshot = target.Center;
                // (生成偏移, 生长角)：下→上、上→下、左→右、右→左
                (Vector2 off, float angle)[] roots =
                {
                    (new Vector2(0f, 330f),  -MathHelper.PiOver2),
                    (new Vector2(0f, -330f),  MathHelper.PiOver2),
                    (new Vector2(-330f, 0f),  0.0001f),
                    (new Vector2(330f, 0f),   MathHelper.Pi)
                };
                foreach (var (off, angle) in roots)
                    SpawnHostile<HDMCFractalTreeHostile>(snapshot + off, Vector2.Zero, 0.8f, angle);

                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndGeoBurst) { Volume = 0.5f }, snapshot);
            }
        }

        /// <summary>
        /// 23 · 彭罗斯·流星链：五连小陨晶（缩减爆炸半径）沿玩家足迹追落——
        /// 每颗以生成瞬间的玩家位置为落点，持续移动即甩开整条链。
        /// </summary>
        private void Attack_PenroseChain(Player target)
        {
            HoverBesideTarget(target, HoverSide * 460f, -320f, 15f, 26f);

            if (Timer >= 25 && Timer <= 205 && (Timer - 25) % 45 == 0)
            {
                float jitter = (HDMCUtil.Hash01(NPC.whoAmI * 13 + Timer) - 0.5f) * 180f;
                SpawnHostile<HDMCPenroseHostile>(
                    target.Center + new Vector2(jitter, 50f), Vector2.Zero, 0.85f, 145f);
            }
        }

        /// <summary>
        /// 24 · 环波·三重奏：玩家周围三点错峰引爆带缺口环波，
        /// 干涉图样中沿缺口白色节点穿行。
        /// </summary>
        private void Attack_RingTrio(Player target)
        {
            HoverBesideTarget(target, 0f, -360f, 15f, 26f);

            if (Timer == 30 || Timer == 62 || Timer == 94)
            {
                int step = (Timer - 30) / 32;
                float angle = HDMCUtil.Hash01(NPC.whoAmI * 37) * MathHelper.TwoPi + step * MathHelper.TwoPi / 3f;
                Vector2 origin = target.Center + angle.ToRotationVector2() * 380f;
                SpawnHostile<HDMCRingWaveHostile>(origin, Vector2.Zero, 0.9f, 6f, 950f);
            }
        }

        // ══════════════════════════════════════════════════
        // P5 · 编译风暴阶段
        // ══════════════════════════════════════════════════

        /// <summary>25 · 编译风暴：全模组连锁总攻（脚本化时间轴）。</summary>
        private void Attack_CompileStorm(Player target)
        {
            HoverBesideTarget(target, 0f, -400f, 18f, 20f);

            switch (Timer)
            {
                case 5:
                    if (!Main.dedServ)
                        SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndCompileStorm) { Volume = 0.85f }, NPC.Center);
                    break;

                case 70: // 数据面板双翼
                    SpawnHostile<HDMCGridPanelHostile>(target.Center + new Vector2(-430f, -130f), Vector2.Zero, 0.7f);
                    SpawnHostile<HDMCGridPanelHostile>(target.Center + new Vector2(430f, -130f), Vector2.Zero, 0.7f);
                    break;

                case 150: // 三角几何爆破
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 offset = (MathHelper.TwoPi * i / 3f + 0.5f).ToRotationVector2() * 330f;
                        SpawnHostile<HDMCGeoBurstHostile>(target.Center + offset, Vector2.Zero, 0.75f, 0f, i * 8f);
                    }
                    break;

                case 230: // 地刺三连
                    for (int i = -1; i <= 1; i++)
                        SpawnHostile<HDMCFractalTreeHostile>(
                            new Vector2(target.Center.X + i * 360f, target.Center.Y + 280f), Vector2.Zero, 0.8f);
                    break;

                case 310: // 七芒星封印
                    SpawnHostile<HDMCHeptagramHostile>(target.Center, Vector2.Zero, 1f);
                    break;

                case 390: // 彭罗斯三连陨晶
                    for (int i = -1; i <= 1; i++)
                        SpawnHostile<HDMCPenroseHostile>(target.Center + new Vector2(i * 290f, 60f), Vector2.Zero, 0.95f);
                    break;

                case 470: // 沃罗诺伊囚笼
                    SpawnHostile<HDMCVoronoiHostile>(target.Center, Vector2.Zero, 0.85f);
                    break;

                case 550: // 聚变新星终响
                    SpawnHostile<HDMCRingWaveHostile>(NPC.Center, Vector2.Zero, 0.95f, 7.5f, 1300f);
                    float baseAngle = HDMCUtil.Hash01(NPC.whoAmI * 71 + Timer) * MathHelper.TwoPi;
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 dir = (baseAngle + MathHelper.TwoPi * i / 12f).ToRotationVector2();
                        SpawnHostile<HDMCShardHostile>(NPC.Center + dir * 30f, dir * 11.5f, 0.85f);
                    }
                    if (!Main.dedServ)
                    {
                        HDMCUtil.DataBurstParticles(NPC.Center, 24, 14, 11f);
                        HDMCUtil.ScreenShake(NPC.Center, 4f, 1100f);
                    }
                    break;
            }
        }

        /// <summary>
        /// 26 · 模组·随机脉冲：每 45 帧随机调用一个轻量模组——
        /// 面板 / 单几何体 / 五连扇 / 短天降，高频但每发都轻。
        /// </summary>
        private void Attack_ModulePulse(Player target)
        {
            HoverBesideTarget(target, HoverSide * 450f, -280f, 17f, 22f);

            if (Timer >= 30 && Timer <= 330 && (Timer - 30) % 45 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                switch (Main.rand.Next(4))
                {
                    case 0:
                        SpawnHostile<HDMCGridPanelHostile>(
                            target.Center + new Vector2(Main.rand.NextBool() ? -380f : 380f, -180f), Vector2.Zero, 0.65f);
                        break;
                    case 1:
                        SpawnHostile<HDMCGeoBurstHostile>(
                            target.Center + Main.rand.NextVector2Unit() * 300f, Vector2.Zero, 0.7f);
                        break;
                    case 2:
                        for (int i = -2; i <= 2; i++)
                            FireAimedLance(target, i * 12f, 0.65f, 15f);
                        break;
                    default:
                        for (int i = -2; i <= 2; i++)
                        {
                            Vector2 from = new(target.Center.X + i * 170f, target.Center.Y - 360f);
                            SpawnHostile<HDMCLanceHostile>(from, Vector2.UnitY * 2f, 0.65f, 15f, 18f);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 27 · 克利福德·全域风车：两波正交风车激光（4 道 90°），
        /// 第二波错 45°，收尾一组窄幅瞄准束。
        /// </summary>
        private void Attack_CliffordWindmill(Player target)
        {
            HoverBesideTarget(target, 0f, -340f, 16f, 24f);

            if (Timer == 70 || Timer == 180)
            {
                float baseAngle = HDMCUtil.Hash01(NPC.whoAmI * 53) * MathHelper.TwoPi
                    + (Timer == 180 ? MathHelper.PiOver4 : 0f);
                for (int k = 0; k < 4; k++)
                {
                    Vector2 dir = (baseAngle + MathHelper.PiOver2 * k).ToRotationVector2();
                    Vector2 origin = NPC.Center - dir * 750f;
                    SpawnHostile<HDMCLaserHostile>(origin, dir, 0.9f, 1500f, 55f);
                }

                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndSpaceWarp) { Volume = 0.5f }, NPC.Center);
            }

            if (Timer == 290)
            {
                Vector2 aim = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                for (int i = -1; i <= 1; i++)
                {
                    Vector2 dir = aim.RotatedBy(i * MathHelper.ToRadians(16f));
                    Vector2 origin = NPC.Center - dir * 650f;
                    SpawnHostile<HDMCLaserHostile>(origin, dir, 0.85f, 1300f, 45f);
                }
            }
        }

        /// <summary>
        /// 28 · 奇点·引力井：微型奇点缓拉全场（可反向移动抵抗），
        /// 中途放出一记环波，收束时爆心 + 放射碎片。
        /// </summary>
        private void Attack_GravityWell(Player target)
        {
            NPC.velocity *= 0.93f;

            if (Timer == 10 && !Main.dedServ)
                SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndSingularity) { Volume = 0.55f, Pitch = 0.2f }, NPC.Center);

            // 温和牵引：强度低于终章奇点，反向移动可完全抵消
            if (Timer > 40 && Timer < 200)
            {
                foreach (Player player in Main.ActivePlayers)
                {
                    if (player.dead)
                        continue;
                    float dist = Vector2.Distance(player.Center, NPC.Center);
                    if (dist > 1400f || dist < 120f)
                        continue;

                    float pullStrength = MathHelper.Lerp(0.16f, 0.04f, dist / 1400f);
                    player.velocity += (NPC.Center - player.Center).SafeNormalize(Vector2.Zero) * pullStrength;
                }
            }

            if (Timer == 110)
                SpawnHostile<HDMCRingWaveHostile>(NPC.Center, Vector2.Zero, 0.9f, 6.5f, 1100f);

            if (Timer == 200)
            {
                SpawnHostile<HDMCFusionBlastHostile>(NPC.Center, Vector2.Zero, 1f, 200f);
                float baseAngle = HDMCUtil.Hash01(NPC.whoAmI * 83) * MathHelper.TwoPi;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 dir = (baseAngle + MathHelper.TwoPi * i / 8f).ToRotationVector2();
                    SpawnHostile<HDMCShardHostile>(NPC.Center + dir * 40f, dir * 12f, 0.8f);
                }
            }
        }

        /// <summary>29 · 沃罗诺伊·双重牢笼：错位双笼，扫描起点相异形成反向封锁。</summary>
        private void Attack_VoronoiTwin(Player target)
        {
            HoverBesideTarget(target, HoverSide * 520f, -240f, 15f, 26f);

            if (Timer == 15)
                SpawnHostile<HDMCVoronoiHostile>(target.Center, Vector2.Zero, 0.8f);

            if (Timer == 85)
            {
                float angle = HDMCUtil.Hash01(NPC.whoAmI * 97 + 3) * MathHelper.TwoPi;
                SpawnHostile<HDMCVoronoiHostile>(
                    target.Center + angle.ToRotationVector2() * 260f, Vector2.Zero, 0.8f);
            }
        }

        /// <summary>
        /// 30 · 矩阵审判·矛雨：全屏九列矛雨三波，每波两条安全列（无凝滞矛
        /// 成形处即安全）——观察成形位置选列站位。
        /// </summary>
        private void Attack_JudgmentRain(Player target)
        {
            HoverBesideTarget(target, 0f, -420f, 16f, 24f);

            if (Timer == 50 || Timer == 160 || Timer == 270)
            {
                int wave = Timer / 100;
                int safeA = (int)(HDMCUtil.Hash01(NPC.whoAmI * 67 + wave * 11) * 9f);
                int safeB = (safeA + 4) % 9;

                for (int col = 0; col < 9; col++)
                {
                    if (col == safeA || col == safeB)
                        continue;

                    float x = target.Center.X + (col - 4) * 195f;
                    for (int row = 0; row < 2; row++)
                    {
                        Vector2 from = new(x, target.Center.Y - 420f - row * 150f);
                        SpawnHostile<HDMCLanceHostile>(from, Vector2.UnitY * 2f, 0.75f, 15.5f, 26f + row * 6f);
                    }
                }

                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndInscFire) { Volume = 0.5f, Pitch = 0.1f * wave }, target.Center);
            }
        }

        // ══════════════════════════════════════════════════
        // 程序化绘制
        // ══════════════════════════════════════════════════

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            float t = Main.GlobalTimeWrappedHourly;
            float opacity = State == StateIntro ? MathHelper.Clamp(Timer / 150f, 0f, 1f) : 1f;
            Vector2 center = NPC.Center;

            // ── 本体：三层反向旋转几何 + 中心超立方 ──
            float spinBoost = State is StateTransition or StateFinale ? 1.6f : 1f;
            HyperdimensionalMatrixVisuals.DrawGeometry(
                center, MatrixGeometryShape.Icosahedron,
                86f, t * 1.5f * spinBoost, opacity * 0.55f, NPC.whoAmI);
            HyperdimensionalMatrixVisuals.DrawGeometry(
                center, MatrixGeometryShape.Cube,
                54f, -t * 2.1f * spinBoost + 0.42f, opacity * 0.42f, NPC.whoAmI + 7, false);
            HyperdimensionalMatrixVisuals.DrawHypercube(center, 42f, t * spinBoost, opacity * 0.9f);

            // 高阶段外层增幅
            if (Phase >= 3)
                HyperdimensionalMatrixVisuals.DrawGeometry(
                    center, MatrixGeometryShape.Icosahedron,
                    120f, -t * 0.8f, opacity * 0.22f, NPC.whoAmI + 13, false);

            // ── 扫描环组 ──
            HyperdimensionalMatrixVisuals.DrawScanRing(center, 112f, t * 0.48f,
                HDMCUtil.DataColor(0.18f, opacity * 0.36f), 32, 1.5f);
            HyperdimensionalMatrixVisuals.DrawScanRing(center, 132f, -t * 0.32f,
                HDMCUtil.DataColor(0.62f, opacity * 0.26f), 24, 1.1f);
            if (Phase >= 4)
                HyperdimensionalMatrixVisuals.DrawScanRing(center, 152f, t * 0.7f,
                    HDMCUtil.DataColor(0.4f, opacity * 0.2f), 28, 1f);

            // ── 模组状态环：当前阶段 6 模组指示灯 ──
            DrawModuleRing(center, t, opacity);

            // ── 环绕数据块 ──
            for (int i = 0; i < 16; i++)
            {
                float angle = MathHelper.TwoPi * i / 16f + t * (0.55f + i % 3 * 0.07f);
                float r = 68f + (float)Math.Sin(t * 2.6f + i * 0.72f) * 12f;
                Vector2 pos = center + angle.ToRotationVector2() * r;
                float blink = 0.45f + 0.55f * (float)Math.Sin(t * 5f + i * 1.27f);
                HyperdimensionalMatrixVisuals.DrawNode(pos,
                    HDMCUtil.DataColor(i * 0.071f, opacity * 0.52f * blink), 3f + i % 3);
            }

            // ── P5 红色警报环 ──
            if (Phase >= 5)
            {
                float pulse = 0.5f + 0.5f * (float)Math.Sin(t * 8f);
                Color alertColor = new Color(255, 50, 10, 0) * ((0.4f + pulse * 0.3f) * opacity);
                HyperdimensionalMatrixVisuals.DrawScanRing(center, 96f + pulse * 16f, -t * 2.1f, alertColor, 8, 2f);
                HyperdimensionalMatrixVisuals.DrawScanRing(center, 78f + pulse * 10f, t * 2.8f, alertColor * 0.55f, 6, 1.4f);
            }

            // ── 攻击专属覆盖层 ──
            DrawAttackOverlays(center, t, opacity);

            // ── 阶段转换：超配方形态环 ──
            if (State == StateTransition)
                DrawSuperformulaRing(center, t, MathHelper.Clamp(Timer / 30f, 0f, 1f) * (Timer > 90 ? (115 - Timer) / 25f : 1f));

            return false;
        }

        /// <summary>当前阶段 6 模组指示灯环：正在使用的模组高亮放大并连线到本体。</summary>
        private void DrawModuleRing(Vector2 center, float t, float opacity)
        {
            int[] pool = CurrentPool;
            for (int slot = 0; slot < pool.Length; slot++)
            {
                int m = pool[slot];
                float mAngle = MathHelper.TwoPi * slot / pool.Length - t * 0.22f;
                Vector2 mPos = center + mAngle.ToRotationVector2() * 168f;

                bool active = State == m;
                Color mColor = GetModuleColor(m);
                float alpha = active ? 1f : 0.55f;
                float size = active ? 8f + 2f * (float)Math.Sin(t * 12f) : 5f;

                HyperdimensionalMatrixVisuals.DrawNode(mPos, mColor * (alpha * opacity), size);
                if (active)
                {
                    HyperdimensionalMatrixVisuals.DrawNode(mPos, mColor * (0.35f * opacity), size * 2.2f);
                    Main.spriteBatch.DrawLineBetter(center, mPos, mColor * (0.12f * opacity), 1f);
                }
            }
        }

        /// <summary>模组指示色：黄金角跳跃取色，相邻编号色相错开且全局稳定。</summary>
        private static Color GetModuleColor(int m)
        {
            Color c = Main.hslToRgb((m * 0.618034f) % 1f, 0.85f, 0.62f);
            c.A = 0;
            return c;
        }

        /// <summary>依据当前攻击状态绘制 Boss 本体上的专属特效层。</summary>
        private void DrawAttackOverlays(Vector2 center, float t, float opacity)
        {
            switch (State)
            {
                case 3:  // 莫比乌斯·点射
                case 11: // 莫比乌斯·交叉火力
                {
                    float buildPct = MathHelper.Clamp(Timer / 60f, 0f, 1f);
                    Vector2[] pts = GetMobiusPoints(center, t);
                    int visible = (int)(pts.Length * buildPct);
                    for (int i = 0; i < visible - 1; i++)
                    {
                        Color c = HDMCUtil.DataColor(i / (float)pts.Length, opacity * buildPct);
                        Main.spriteBatch.DrawLineBetter(pts[i], pts[i + 1], c, 1.9f);
                    }
                    if (visible >= pts.Length && pts.Length > 1)
                        Main.spriteBatch.DrawLineBetter(pts[^1], pts[0],
                            HDMCUtil.DataColor(0.95f, opacity * buildPct), 1.9f);

                    for (int n = 0; n < 6; n++)
                    {
                        float flow = (t * 0.8f + n / 6f) % 1f;
                        int segIdx = Math.Min((int)(flow * (pts.Length - 1)), pts.Length - 2);
                        float segFrac = flow * (pts.Length - 1) - segIdx;
                        Vector2 nodePos = Vector2.Lerp(pts[segIdx], pts[segIdx + 1], segFrac);
                        HyperdimensionalMatrixVisuals.DrawNode(nodePos, HDMCUtil.DataColor(flow, opacity * buildPct), 4.5f);
                    }
                    break;
                }

                case 7: // 斐波那契螺旋
                {
                    float buildPct = MathHelper.Clamp(Timer / 80f, 0f, 1f);
                    float rotation = t * 0.4f;
                    float maxTheta = MathF.Log(SpiralMaxR / SpiralA) / SpiralB;
                    Vector2 prevPt = center;
                    const int segs = 56;
                    for (int s = 1; s <= segs; s++)
                    {
                        float theta = maxTheta * buildPct * s / segs;
                        float r = Math.Min(SpiralA * MathF.Exp(SpiralB * theta), SpiralMaxR);
                        Vector2 pt = center + (theta + rotation).ToRotationVector2() * r;
                        Main.spriteBatch.DrawLineBetter(prevPt, pt,
                            HDMCUtil.DataColor(s / (float)segs, opacity * buildPct * 0.6f), 1.3f);
                        prevPt = pt;
                    }

                    int visibleNodes = (int)(SpiralNodes * buildPct);
                    for (int i = 0; i < visibleNodes; i++)
                    {
                        Vector2 nodePos = GetSpiralNode(i, rotation);
                        float pulse = 3.5f + 1.5f * MathF.Sin(t * 6f + i * 0.7f);
                        HyperdimensionalMatrixVisuals.DrawNode(nodePos,
                            HDMCUtil.DataColor(i / (float)SpiralNodes, opacity * buildPct * 0.9f), pulse);
                    }
                    break;
                }

                case 15: // 克利福德·光轮
                case 27: // 克利福德·全域风车
                {
                    float buildPct = MathHelper.Clamp(Timer / 60f, 0f, 1f);
                    for (int r = 0; r < 3; r++)
                    {
                        Color c = HDMCUtil.DataColor(r / 3f, opacity * buildPct * 0.7f);
                        Vector2 prevPt = GetCliffordPoint(center, r, 0f, t);
                        const int segs = 40;
                        for (int i = 1; i <= segs; i++)
                        {
                            float phi = MathHelper.TwoPi * i / segs;
                            Vector2 pt = GetCliffordPoint(center, r, phi, t);
                            Main.spriteBatch.DrawLineBetter(prevPt, pt, c, 1.7f);
                            prevPt = pt;
                        }

                        float flow = (t * 1.5f + r * 0.33f) % MathHelper.TwoPi;
                        HyperdimensionalMatrixVisuals.DrawNode(
                            GetCliffordPoint(center, r, flow, t),
                            HDMCUtil.DataColor(r / 3f + 0.5f, opacity * buildPct), 5f);
                    }
                    break;
                }

                case 20: // 聚变新星：金属球汇聚
                {
                    if (Timer < 128)
                    {
                        float convergePct = Timer / 128f;
                        float eased = convergePct * convergePct * (3f - 2f * convergePct);
                        for (int i = 0; i < 8; i++)
                        {
                            float angle = MathHelper.TwoPi * i / 8f + i * 0.42f + t * 0.3f;
                            float radius = 200f * (1f - eased);
                            Vector2 ballPos = center + angle.ToRotationVector2() * radius;
                            Color bc = HDMCUtil.DataColor(i / 8f, opacity);
                            HyperdimensionalMatrixVisuals.DrawNode(ballPos, bc, 7f + eased * 4f);
                            HyperdimensionalMatrixVisuals.DrawNode(ballPos, bc * 0.3f, 16f + eased * 8f);
                            Main.spriteBatch.DrawLineBetter(ballPos, center, bc * (0.15f + eased * 0.2f), 1.2f);
                        }
                        HyperdimensionalMatrixVisuals.DrawScanRing(center, 200f * (1f - eased) + 40f, t * 2f,
                            HDMCUtil.DataColor(0.3f, opacity * 0.5f * convergePct), 20, 2f);
                    }
                    break;
                }

                case 28: // 奇点·引力井：向心数据流
                {
                    if (Timer > 30 && Timer < 210)
                    {
                        float wellPct = MathHelper.Clamp((Timer - 30) / 40f, 0f, 1f);
                        HyperdimensionalMatrixVisuals.DrawNode(center, Color.White with { A = 0 } * wellPct,
                            12f + 3f * MathF.Sin(t * 8f));

                        for (int i = 0; i < 14; i++)
                        {
                            float angle = MathHelper.TwoPi * i / 14f + t * 0.5f;
                            float streamLen = 420f - 240f * ((t * 1.5f + i * 0.19f) % 1f);
                            Vector2 streamStart = center + angle.ToRotationVector2() * streamLen;
                            Color streamColor = HDMCUtil.DataColor(i * 0.07f, wellPct * 0.45f * opacity);
                            Main.spriteBatch.DrawLineBetter(streamStart,
                                Vector2.Lerp(streamStart, center, 0.18f), streamColor, 1.4f);
                        }
                    }
                    break;
                }

                case 25: // 编译风暴：全息编译面板 + 汇聚数据流
                {
                    float buildPct = MathHelper.Clamp(Timer / 70f, 0f, 1f);
                    DrawCompilePanel(center + new Vector2(0f, -120f), buildPct * opacity, t);

                    for (int i = 0; i < 18; i++)
                    {
                        float angle = MathHelper.TwoPi * i / 18f + t * 0.4f;
                        float streamLen = MathHelper.Lerp(430f, 70f, buildPct * buildPct);
                        Vector2 streamStart = center + angle.ToRotationVector2() * streamLen;
                        Color streamColor = HDMCUtil.DataColor(i * 0.055f, buildPct * 0.55f * opacity);
                        Main.spriteBatch.DrawLineBetter(streamStart, center, streamColor, 1.4f);
                    }
                    break;
                }
            }
        }

        /// <summary>克利福德环面霍普夫圆采样（同武器模组）。</summary>
        private static Vector2 GetCliffordPoint(Vector2 center, int ringIdx, float phi, float time)
        {
            float theta = ringIdx * (MathHelper.TwoPi / 3f) + time * 0.25f;

            float x = MathF.Cos(phi) * MathF.Cos(theta);
            float y = MathF.Sin(phi) * MathF.Cos(theta);
            float z = MathF.Cos(phi) * MathF.Sin(theta);
            float w = MathF.Sin(phi) * MathF.Sin(theta);

            const float scale3D = 105f;
            const float wOffset = 1.4f;
            Vector3 pt3D = new Vector3(x, y, z) * (scale3D / (wOffset - w));

            Matrix rot = Matrix.CreateFromYawPitchRoll(time * 0.4f, time * 0.3f, time * 0.2f);
            Vector3 rotated = Vector3.Transform(pt3D, rot);
            float perspective = 620f / MathF.Max(180f, 620f + rotated.Z);

            return center + new Vector2(rotated.X * perspective, rotated.Y * perspective);
        }

        /// <summary>全息编译面板（模组进度格阵，编译风暴前奏）。</summary>
        private void DrawCompilePanel(Vector2 panelCenter, float buildPct, float t)
        {
            const float cellSize = 5f;
            const float gap = 1.8f;
            const int cols = 13;
            const int rows = 4;
            float panelW = cols * cellSize + (cols - 1) * gap;
            float panelH = rows * cellSize + (rows - 1) * gap;

            Vector2 topLeft = panelCenter - new Vector2(panelW * 0.5f, panelH * 0.5f);
            Vector2 botRight = panelCenter + new Vector2(panelW * 0.5f, panelH * 0.5f);
            Color frameColor = HDMCUtil.DataColor(0.28f, buildPct * 0.65f);

            const float bl = 10f;
            void Corner(Vector2 a, float sx, float sy)
            {
                Main.spriteBatch.DrawLineBetter(a, a + Vector2.UnitX * (bl * sx), frameColor, 1.6f);
                Main.spriteBatch.DrawLineBetter(a, a + Vector2.UnitY * (bl * sy), frameColor, 1.6f);
            }
            Corner(topLeft, 1f, 1f);
            Corner(new Vector2(botRight.X, topLeft.Y), -1f, 1f);
            Corner(new Vector2(topLeft.X, botRight.Y), 1f, -1f);
            Corner(botRight, -1f, -1f);

            for (int m = 0; m < cols; m++)
            {
                Color mColor = GetModuleColor(m + 1);
                float cx = topLeft.X + m * (cellSize + gap) + cellSize * 0.5f;

                for (int r = 0; r < rows; r++)
                {
                    float threshold = (rows - 1 - r) / (float)(rows - 1);
                    float mOffset = m * (1f / (cols * 6f));
                    bool lit = buildPct > threshold * 0.9f + mOffset;
                    float cellAlpha = lit
                        ? MathHelper.Lerp(0.55f, 1f, buildPct) * (0.78f + 0.22f * MathF.Sin(t * 9f + m * 1.4f))
                        : 0.12f;
                    float cy = topLeft.Y + r * (cellSize + gap) + cellSize * 0.5f;
                    HyperdimensionalMatrixVisuals.DrawNode(new Vector2(cx, cy), mColor * cellAlpha, cellSize * 0.85f);
                }
            }

            float scanX = topLeft.X + panelW * ((t * 0.72f) % 1f);
            Main.spriteBatch.DrawLineBetter(
                new Vector2(scanX, topLeft.Y - 2f), new Vector2(scanX, botRight.Y + 2f),
                frameColor * 0.38f, 1f);
        }

        /// <summary>阶段转换时的超配方形态环（对称数随时间渐变）。</summary>
        private void DrawSuperformulaRing(Vector2 center, float t, float opacity)
        {
            float m = 5f + 3f * MathF.Sin(t * 3f);
            const float n1 = 1f, n2 = 1.2f, n3 = 1.2f;
            const int pts = 64;

            Vector2 prevPt = GetSuperformulaPoint(center, 0f, m, n1, n2, n3, t);
            for (int i = 1; i <= pts; i++)
            {
                float theta = MathHelper.TwoPi * i / pts;
                Vector2 pt = GetSuperformulaPoint(center, theta, m, n1, n2, n3, t);
                Color c = HDMCUtil.DataColor(i / (float)pts + t * 0.2f, opacity);
                Main.spriteBatch.DrawLineBetter(prevPt, pt, c, 2f);
                prevPt = pt;
            }
        }

        private static Vector2 GetSuperformulaPoint(Vector2 center, float theta, float m, float n1, float n2, float n3, float time)
        {
            float term1 = MathF.Pow(MathF.Abs(MathF.Cos(m * theta / 4f)), n2);
            float term2 = MathF.Pow(MathF.Abs(MathF.Sin(m * theta / 4f)), n3);
            float r = MathF.Pow(term1 + term2, -1f / n1);
            r *= 150f + 25f * MathF.Sin(time * 4f);
            return center + (theta + time * 0.5f).ToRotationVector2() * r;
        }
    }
}
