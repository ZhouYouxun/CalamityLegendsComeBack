using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces
{
    /// <summary>
    /// 「双鱼座 / Pisces」的平衡中枢——数值直接取自《Dragoon Drizzlefish × Polaris Parrotfish 双态联动武器制作文稿》。
    /// 该文档是设计冻结合同：左键=暴躁硫火喷吐（3 小 1 大，重力落地 + 地火锚点），右键=冷静光学蓄力（I/II/III 级光弹 + 满蓄双束神圣激光），
    /// 联动只认锚点。成长（肉山 / 月球领主）只放大左键的规模与节奏，绝不改弹幕身份。
    /// 所有百分比均为文稿建议初值；集中在此以便实战再测，不散落到各弹幕里。
    /// </summary>
    internal static class PiscesBalance
    {
        // =====================================================================
        // 物品基准
        // =====================================================================
        /// <summary>基础伤害（远程）。左右键都以它为锚点。</summary>
        public const int BaseDamage = 168;
        public const float KnockBack = 2.2f;

        // =====================================================================
        // 左键：硫火喷吐（3 小 1 大）
        // 实现基线取自灾厄 Dragoon Drizzlefish：连续发射前三次为小火球，第四次为大火球。
        // =====================================================================
        /// <summary>“3 小 1 大”循环长度：第 4 发是大火球。</summary>
        public const int BigShotInterval = 4;
        /// <summary>基础发射间隔 tick（未成长）。</summary>
        public const int LeftBaseUseTime = 15;
        /// <summary>左键喷吐随机散布（弧度半角）。</summary>
        public const float LeftSpread = 0.09f;
        /// <summary>小火球初速。</summary>
        public const float SmallFireballSpeed = 11f;

        // 小火球（重力落地火球）
        public const int SmallFireballLifetime = 300;
        /// <summary>小火球竖直重力（持续下坠）。</summary>
        public const float SmallFireballGravity = 0.16f;
        /// <summary>小火球水平衰减（缓慢）。</summary>
        public const float SmallFireballDrag = 0.992f;
        public const float SmallFireballMaxFallSpeed = 12f;

        // 大火球（每第 4 发，体积约小火球 1.5 倍，飞行 ~45 tick 后分裂 3 枚）
        public const float BigFireballScale = 1.5f;
        /// <summary>大火球分裂计时 tick（灾厄原值）。</summary>
        public const int BigFireballSplitTime = 45;
        /// <summary>大火球分裂枚数。</summary>
        public const int BigFireballSplitCount = 3;
        /// <summary>分裂弹竖直重力（更快下坠）。</summary>
        public const float SplitFireballGravity = 0.5f;

        // 地火锚点（左键留下的“硫火锚点”）
        /// <summary>地火锚点基础寿命 tick（未成长）。</summary>
        public const int BrimstoneAnchorBaseLifetime = 180;
        /// <summary>地火锚点每次灼烧的伤害间隔 tick。</summary>
        public const int BrimstoneAnchorBurnInterval = 20;
        /// <summary>地火锚点单次灼烧伤害相对左键基础伤害的比例。</summary>
        public const float BrimstoneAnchorBurnDamageRatio = 0.35f;
        /// <summary>地火锚点灼烧半径 px。</summary>
        public const float BrimstoneAnchorBurnRadius = 60f;
        /// <summary>基础地火锚点上限（超出淘汰最早者）。</summary>
        public const int BrimstoneAnchorBaseCap = 6;

        // 命中/落地火球产生的小范围硫火爆破半径 px
        public const float SmallBurstRadius = 46f;
        public const float BigBurstRadius = 78f;

        // ---- 成长（只放大左键的规模与节奏，绝不改弹幕身份）----
        // 肉山：发射频率 +12%、火球 scale +10%、地火持续 +20%。
        // 月球领主（在肉山基础上）：频率再 +18%、scale 再 +15%、地火再 +25%、锚点上限 +2。
        private static bool DownedWoF => Main.hardMode;
        private static bool DownedMoonLord => NPC.downedMoonlord;

        /// <summary>左键发射频率倍率（值越大越快 → 用于缩短 useTime）。</summary>
        public static float LeftFireRateMult => (DownedWoF ? 1.12f : 1f) * (DownedMoonLord ? 1.18f : 1f);
        /// <summary>火球 scale 倍率。</summary>
        public static float FireballScaleMult => (DownedWoF ? 1.10f : 1f) * (DownedMoonLord ? 1.15f : 1f);
        /// <summary>地火持续倍率。</summary>
        public static float GroundFireDurationMult => (DownedWoF ? 1.20f : 1f) * (DownedMoonLord ? 1.25f : 1f);
        /// <summary>当前地火锚点上限。</summary>
        public static int BrimstoneAnchorCap => BrimstoneAnchorBaseCap + (DownedMoonLord ? 2 : 0);

        /// <summary>成长后的左键发射间隔 tick。</summary>
        public static int LeftUseTime()
        {
            int t = (int)System.Math.Round(LeftBaseUseTime / LeftFireRateMult);
            return t < 6 ? 6 : t;
        }

        /// <summary>成长后的地火锚点寿命 tick。</summary>
        public static int BrimstoneAnchorLifetime() => (int)(BrimstoneAnchorBaseLifetime * GroundFireDurationMult);

        // =====================================================================
        // 右键：蓄力让弹幕质量上升（I 校准 / II 聚焦 / III 北辰锁定 / 满蓄双束激光）
        // 蓄力阈值（tick）：I 0-35，II 36-80，III 81-130，满蓄 131+。
        // 每发光弹在发射瞬间把 ChargeTier 写进 ai，之后绝不回读实时蓄力。
        // =====================================================================
        public const int TierIIChargeTicks = 36;
        public const int TierIIIChargeTicks = 81;
        public const int MaxChargeTicks = 131;
        /// <summary>蓄力封顶 tick（到此即视为满蓄）。</summary>
        public const int ChargeCap = 150;
        /// <summary>按住右键时，I/II/III 级持续射击的间隔。蓄力越久，射击越密。</summary>
        public const int TierIShotInterval = 15;
        public const int TierIIShotInterval = 10;
        public const int TierIIIShotInterval = 6;
        /// <summary>满蓄维持期间的快速激光间隔；它是短促点杀，不替代松手双束终结。</summary>
        public const int FullChargeRapidLaserInterval = 32;
        public const float RapidLaserDamageMult = 0.55f;

        /// <summary>满蓄松开后的短前摇 tick（停火一拍，收束亮点再放激光）。</summary>
        public const int MaxChargeWindup = 10;
        /// <summary>松手后空置多久无输入则收起持械（供连续点射复用同一持械）。</summary>
        public const int HoldoutIdleLinger = 24;

        /// <summary>右键各级伤害倍率（相对右键基准 = 物品伤害）。</summary>
        public const float TierIDamageMult = 1.00f;
        public const float TierIIDamageMult = 1.25f;
        public const float TierIIIDamageMult = 1.60f;
        /// <summary>满蓄单束神圣激光伤害倍率（每束）。</summary>
        public const float HolyBeamDamageMult = 2.20f;
        /// <summary>II 级折射碎片伤害倍率。</summary>
        public const float RefractionShardDamageMult = 0.45f;

        // 光弹运动
        public const float PolarShotBaseSpeed = 15f;
        public const float TierIISpeedMult = 1.20f;
        public const float TierIIISpeedMult = 1.40f;
        /// <summary>II 级延迟追踪起始 tick。</summary>
        public const int TierIIHomeDelay = 12;
        /// <summary>III 级延迟追踪起始 tick。</summary>
        public const int TierIIIHomeDelay = 10;
        /// <summary>追踪转向强度。</summary>
        public const float PolarShotHomeStrength = 0.085f;
        /// <summary>追踪搜索半径。</summary>
        public const float PolarShotHomeRange = 1200f;
        public const int PolarShotLifetime = 240;

        // 满蓄双束神圣激光
        /// <summary>激光寿命 tick（最长 18）。</summary>
        public const int HolyBeamLifetime = 18;
        /// <summary>激光长度 px（900-1200 之间）。</summary>
        public const float HolyBeamLength = 1120f;
        /// <summary>激光碰撞宽度 px（20-28 之间）。</summary>
        public const float HolyBeamHitWidth = 24f;
        /// <summary>双束各自相对瞄准方向的半角差（弧度）——总角差约 4°。</summary>
        public const float HolyBeamHalfAngle = 0.035f; // ≈2°

        // =====================================================================
        // 联动：把留下的锚点串起来（锚点是唯一货币）
        // =====================================================================
        /// <summary>Tier III 光弹命中被硫火灼烧的敌人时，寻找硫火锚点的最大距离 px。</summary>
        public const float LinkSearchRadius = 560f;
        /// <summary>Tier III 光弹每次命中最多串联的锚点数。</summary>
        public const int LinkAnchorsPerShot = 2;
        /// <summary>满蓄激光擦过锚点时，沿方向最多串联的锚点数。</summary>
        public const int LinkAnchorsPerBeam = 3;
        /// <summary>满蓄激光联动的全链内部冷却 tick（0.75 秒）。</summary>
        public const int BeamLinkCooldown = 45;
        /// <summary>联动交点化学爆破相对左键基础伤害的比例。</summary>
        public const float LinkBurstDamageRatio = 0.60f;
        public const float LinkBurstRadius = 96f;

        // =====================================================================
        // 工具
        // =====================================================================
        /// <summary>由蓄力 tick 得出当前档位：0=I，1=II，2=III，3=满蓄。</summary>
        public static int ChargeTier(int chargeTicks)
        {
            if (chargeTicks >= MaxChargeTicks) return 3;
            if (chargeTicks >= TierIIIChargeTicks) return 2;
            if (chargeTicks >= TierIIChargeTicks) return 1;
            return 0;
        }

        public static float TierDamageMult(int tier) => tier switch
        {
            2 => TierIIIDamageMult,
            1 => TierIIDamageMult,
            _ => TierIDamageMult,
        };

        public static float TierSpeedMult(int tier) => tier switch
        {
            2 => TierIIISpeedMult,
            1 => TierIISpeedMult,
            _ => 1f,
        };

        public static int SustainedShotInterval(int tier) => tier switch
        {
            >= 2 => TierIIIShotInterval,
            1 => TierIIShotInterval,
            _ => TierIShotInterval,
        };
    }
}
