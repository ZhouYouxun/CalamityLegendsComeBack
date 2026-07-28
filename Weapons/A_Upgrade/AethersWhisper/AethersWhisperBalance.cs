using Microsoft.Xna.Framework;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper
{
    /// <summary>
    /// 「以太之低语 / Aether's Whisper」传奇重做的平衡中枢。
    /// 全部数值均来自设计文档《以太之低语_传奇重做完整规划》——设计冻结合同，改动请先改文档。
    /// 与原武器一致以 600 基础伤害为锚点；无进程成长表、无被动、无大招（故不含大招倍率表）。
    /// 左键：微光坍缩炮（蓄力单发重击）；右键：微光折返扫射（四连伪激光 + 八枚回收晶片）。
    /// </summary>
    internal static class AethersWhisperBalance
    {
        // =====================================================================
        // 物品基准（第 2.2 节）
        // =====================================================================
        /// <summary>基础魔法伤害（沿用原值，作为所有倍率的锚点）。</summary>
        public const int BaseDamage = 600;
        /// <summary>击退。</summary>
        public const float KnockBack = 5.5f;
        /// <summary>左键实际发射时扣除的魔力（未达最小蓄力松开不扣）。</summary>
        public const int LeftManaCost = 30;
        /// <summary>右键每束扣除的魔力（四束共 32；扣魔失败立刻结束本轮）。</summary>
        public const int RightManaPerBeam = 8;

        // =====================================================================
        // 左键：蓄力（第 3.1 / 3.2 节）
        // =====================================================================
        /// <summary>最小蓄力 tick（0.30 秒）；此前松开取消，不耗魔、不发射。</summary>
        public const int MinChargeTicks = 18;
        /// <summary>满蓄 tick（1.50 秒）；继续按住保持满蓄，不自动发射、不惩罚。</summary>
        public const int FullChargeTicks = 90;

        // 四个可读阶段的分界（tick）：起振/压缩/稳定/临界/满蓄。
        public const int TierStableTicks = 48;   // 稳定段：移速上限 85%
        public const int TierCriticalTicks = 72; // 临界段：移速上限 70%

        /// <summary>稳定段移速上限倍率。</summary>
        public const float StableMoveSpeedMult = 0.85f;
        /// <summary>临界段移速上限倍率。</summary>
        public const float CriticalMoveSpeedMult = 0.70f;

        /// <summary>满蓄松开时的后坐（沿瞄准反方向）px/tick。</summary>
        public const float FullChargeRecoilSpeed = 5.5f;
        /// <summary>满蓄后坐持续 tick。</summary>
        public const int FullChargeRecoilTicks = 6;
        /// <summary>满蓄屏幕震动强度。</summary>
        public const float FullChargeScreenShake = 6f;

        /// <summary>
        /// 蓄力进度：Charge = clamp((ChargeTicks - 18) / 72, 0, 1)（第 3.2 节固定公式）。
        /// </summary>
        public static float ChargeProgress(int chargeTicks) =>
            MathHelper.Clamp((chargeTicks - MinChargeTicks) / (float)(FullChargeTicks - MinChargeTicks), 0f, 1f);

        /// <summary>
        /// 蓄力伤害倍率：DamageMultiplier = 0.70 + 1.45 × SmoothStep(Charge)（第 3.2 节固定公式）。
        /// 满蓄 = 2.15×，最低 = 0.70×。
        /// </summary>
        public static float ChargeDamageMultiplier(float charge) =>
            0.70f + 1.45f * MathHelper.SmoothStep(0f, 1f, charge);

        // 四个可读阶段（最低18 / 中段48 / 高段72 / 满蓄90）的规格锚点（第 3.2 节表）。
        // charge 对应：0 / (48-18)/72 / (72-18)/72 / 1 = 0 / 0.4167 / 0.75 / 1。
        private static readonly float[] TierCharge = { 0f, (TierStableTicks - MinChargeTicks) / 72f, (TierCriticalTicks - MinChargeTicks) / 72f, 1f };
        private static readonly float[] TierSpeed = { 15f, 13f, 11f, 9f };           // 初速 px/tick
        private static readonly float[] TierVisualWidth = { 18f, 32f, 46f, 60f };     // 视觉最大宽度
        private static readonly float[] TierHitWidth = { 12f, 18f, 24f, 30f };        // 实际线碰撞宽度
        private static readonly float[] TierCollapseRadius = { 48f, 72f, 96f, 128f };  // 命中后坍缩半径

        /// <summary>晶核初速（按蓄力在四阶段锚点间连续插值）。</summary>
        public static float ChargedShotSpeed(float charge) => Lerp4(charge, TierSpeed);
        /// <summary>晶核视觉最大宽度。</summary>
        public static float ChargedShotVisualWidth(float charge) => Lerp4(charge, TierVisualWidth);
        /// <summary>晶核实际线碰撞宽度。</summary>
        public static float ChargedShotHitWidth(float charge) => Lerp4(charge, TierHitWidth);
        /// <summary>命中/撞墙后坍缩半径。</summary>
        public static float ChargedShotCollapseRadius(float charge) => Lerp4(charge, TierCollapseRadius);

        /// <summary>晶核飞行寿命（tick）。</summary>
        public const int ChargedShotLifetime = 70;
        /// <summary>晶核 extraUpdates（子步数）：更新次数 = 1 + 9 = 10 次/帧。</summary>
        public const int ChargedShotExtraUpdates = 9;
        /// <summary>晶核绝对速度额外倍率（在阶段初速基础上再 ×1.3）。</summary>
        public const float ChargedShotSpeedMult = 1.3f;
        /// <summary>周边坍缩伤害相对直击伤害的比例（排除直击目标）。</summary>
        public const float CollapseDamageRatio = 0.30f;
        /// <summary>满蓄直击额外护甲穿透。</summary>
        public const int FullChargeArmorPen = 40;
        /// <summary>满蓄直击的暗影焰时长（tick）。</summary>
        public const int FullChargeShadowflameTicks = 300;
        /// <summary>非满蓄直击的暗影焰时长（tick）。</summary>
        public const int NormalShadowflameTicks = 180;

        // =====================================================================
        // 右键：二连散射折返扫射
        // 一「组」= 2 次散射；每次散射 5~7 束随机角度伪激光；两次散射间隔 15 tick，
        // 两组之间间隔 35 tick（即散射①@0、散射②@15、下一组@50）。
        // =====================================================================
        /// <summary>每组的散射次数。</summary>
        public const int ScattersPerRound = 2;
        /// <summary>每次散射的最少 / 最多束数（随机）。</summary>
        public const int ScatterBeamsMin = 5;
        public const int ScatterBeamsMax = 7;
        /// <summary>同组两次散射之间的间隔 tick。</summary>
        public const int ScatterGapTicks = 15;
        /// <summary>两组之间的间隔 tick（末次散射后再等这么多才开新一组）。</summary>
        public const int RoundGapTicks = 35;
        /// <summary>一组的总周期 tick = (次数-1)×散射间隔 + 组间隔。</summary>
        public const int RoundPeriodTicks = (ScattersPerRound - 1) * ScatterGapTicks + RoundGapTicks;
        /// <summary>散射的半张角（弧度），充满随机性。</summary>
        public const float ScatterSpread = 0.42f;
        /// <summary>每次散射扣一次魔力（一次扳机=一次开销，不按束数）。</summary>
        public const int ScatterManaCost = 10;

        /// <summary>每束主伪激光直接伤害倍率（相对物品基础伤害）。</summary>
        public const float BeamDamageMult = 0.30f;
        /// <summary>主伪激光飞行速度 px/tick。</summary>
        public const float BeamSpeed = 58f;
        /// <summary>主伪激光可见宽度 px。</summary>
        public const float BeamVisualWidth = 32f;
        /// <summary>主伪激光碰撞宽度 px（明显细于可见体）。</summary>
        public const float BeamHitWidth = 16f;
        /// <summary>右键最大射程 px（96 格）。</summary>
        public const float BeamMaxRange = 1536f;
        /// <summary>反射后剩余射程保留比例。</summary>
        public const float BeamReflectRangeRetain = 0.55f;

        // =====================================================================
        // 回收晶片（第 4.3 / 4.4 节）
        // =====================================================================
        /// <summary>分解展开时长（tick，无伤害）。</summary>
        public const int ShardExpandTicks = 6;
        /// <summary>回收折返时长（tick，线段伤害）。</summary>
        public const int ShardReturnTicks = 24;
        /// <summary>收束重组时长（tick，无伤害）。</summary>
        public const int ShardReassembleTicks = 5;
        /// <summary>展开时两片各沿末端左右法线移开的距离 px。</summary>
        public const float ShardExpandOffset = 28f;
        /// <summary>一对晶片对任一 NPC 的回收伤害倍率（相对物品基础伤害，同组共享一次）。</summary>
        public const float ShardReturnDamageMult = 0.45f;
        /// <summary>回收段线段碰撞宽度 px。</summary>
        public const float ShardHitWidth = 12f;
        /// <summary>枪口回收环半径 px（晶片钻入即消失）。</summary>
        public const float MuzzleRingRadius = 18f;

        // 贝塞尔镜像控制点偏移（第 4.3 节固定值）：
        /// <summary>无反射：控制点 = 终点 + 主束末段法线 × ±120。</summary>
        public const float ShardControlNoReflectNormal = 120f;
        /// <summary>发生反射：控制点 = 终点 + 反射墙面法线 × 72 + 主束末段法线 × ±96。</summary>
        public const float ShardControlReflectWall = 72f;
        public const float ShardControlReflectNormal = 96f;

        /// <summary>同组防重：某组命中记录的存活窗口 tick（覆盖整段回收）。</summary>
        public const int ReturnGroupImmuneWindow = ShardExpandTicks + ShardReturnTicks + ShardReassembleTicks + 10;

        // =====================================================================
        // 工具
        // =====================================================================
        /// <summary>按四个阶段锚点（charge = 0 / 0.4167 / 0.75 / 1）对一组数值做连续分段插值。</summary>
        private static float Lerp4(float charge, float[] values)
        {
            charge = MathHelper.Clamp(charge, 0f, 1f);
            for (int i = 1; i < TierCharge.Length; i++)
            {
                if (charge <= TierCharge[i])
                {
                    float t = (charge - TierCharge[i - 1]) / (TierCharge[i] - TierCharge[i - 1]);
                    return MathHelper.Lerp(values[i - 1], values[i], t);
                }
            }
            return values[values.Length - 1];
        }
    }
}
