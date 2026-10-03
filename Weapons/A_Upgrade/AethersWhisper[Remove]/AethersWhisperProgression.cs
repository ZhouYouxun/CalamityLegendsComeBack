using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper
{
    /// <summary>
    /// 以太之低语传奇成长/解锁中枢：按 Boss 进程决定「解锁了哪些攻击、哪些成长效果」。
    /// 所有解锁点集中在这里，武器各处只读这里的布尔/计数，方便统一调平衡。
    ///
    /// ⚠ 两个 Boss 映射尚未确认（本项目可能有自定义 downed 标记）：
    ///   · 白金星舰 → 暂映射 Exo 机械（ScaleSpray）
    ///   · 星神游龙 → 暂映射 卡萌龙 Yharon（FlightBoostBig）
    /// 若这两个是本 mod 的自定义 Boss，请告知对应的 downed 字段，我改这两行即可。
    /// </summary>
    internal static class AethersWhisperProgression
    {
        // ===== 左键 5 段攻击的解锁 =====
        // 第 1 段：基础，永远可用。
        public static bool Attack2Unlocked => DownedBossSystem.downedDesertScourge;      // 荒灾
        public static bool Attack3Unlocked => NPC.downedBoss2;                            // 邪恶 Boss（脑/蠕）
        public static bool Attack4Unlocked => DownedBossSystem.downedSlimeGod;            // 史莱姆神
        public static bool Attack5Unlocked => Main.hardMode;                              // 肉山（进入困难模式）

        /// <summary>当前已解锁的左键攻击数量（1..5），左键按顺序在这些里循环。</summary>
        public static int UnlockedLeftAttacks
        {
            get
            {
                int n = 1;
                if (Attack2Unlocked) n = 2;
                if (Attack3Unlocked) n = 3;
                if (Attack4Unlocked) n = 4;
                if (Attack5Unlocked) n = 5;
                return n;
            }
        }

        // ===== 右键 / 被动 =====
        /// <summary>右键：浮游炮射出能量弹 30 帧后，手上以太之低语也开始射击（月总后）。</summary>
        public static bool RightMainHandShot => NPC.downedMoonlord;
        /// <summary>被动后半：非主手时浮游炮每 5 秒朝鼠标发射小激光（骷髅王后）。</summary>
        public static bool PassiveIdleCannonLaser => NPC.downedBoss3;

        // ===== 后期成长 =====
        /// <summary>白金星舰：最终射击后坐期，浮游炮持续发射密集鳞片弹（⚠待确认 Boss 映射）。</summary>
        public static bool ScaleSprayOnFinal => DownedBossSystem.downedExoMechs;
        /// <summary>星神游龙：手持时飞行时间 ×1.5（⚠待确认 Boss 映射）。</summary>
        public static bool FlightTimeBoost => DownedBossSystem.downedYharon;
        /// <summary>西格纳斯：右键能量弹互不干涉，每炮各发一组（倍率减半）。</summary>
        public static bool IndependentEnergyBalls => DownedBossSystem.downedSignus;
        /// <summary>神吞：最终射击 / 右键激光升级为「终焉裂隙」（更大、倍率略高）。</summary>
        public static bool FinalityRift => DownedBossSystem.downedDoG;
    }
}
