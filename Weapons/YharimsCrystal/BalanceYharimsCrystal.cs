using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal
{
    internal enum YCRightLaserVisualTier
    {
        Cell,
        Classic,
        MoonLord,
        Providence,
        Yharon
    }

    public class BalanceYharimsCrystal
    {
        public const float BaseLeftBladeScale = 0.65f;

        private static readonly int[] LeftClickBaseDamage =
        {
            30,
            44,
            58,
            88,
            126,
            190,
            250,
            360,
            485,
            720
        };

        private static readonly int[] RightClickBaseDamage =
        {
            38,
            54,
            72,
            116,
            165,
            250,
            330,
            470,
            640,
            940
        };

        public int GetCompletedStageIndex()
        {
            int stage = 0;
            if (DownedDesertScourge)
                stage = 1;
            if (DownedEvilTier1)
                stage = 2;
            if (DownedWallOfFlesh)
                stage = 3;
            if (DownedBrimstoneElemental)
                stage = 4;
            if (DownedGolem)
                stage = 5;
            if (DownedEmpress)
                stage = 6;
            if (DownedMoonLord)
                stage = 7;
            if (DownedProvidence)
                stage = 8;
            if (DownedYharon)
                stage = 9;

            return stage;
        }

        public int GetLeftClickBaseDamage() => GetValueForStage(LeftClickBaseDamage, GetCompletedStageIndex());
        public int GetRightClickBaseDamage() => GetValueForStage(RightClickBaseDamage, GetCompletedStageIndex());

        public float GetLeftBladeScale()
        {
            float scale = BaseLeftBladeScale;
            if (DownedEvilTier1)
                scale *= 1.15f;
            if (DownedBrimstoneElemental)
                scale *= 1.10f;
            if (DownedGolem)
                scale *= 1.10f;
            if (DownedEmpress)
                scale *= 1.10f;
            if (DownedMoonLord)
                scale *= 1.10f;
            if (DownedProvidence)
                scale *= 1.10f;
            if (DownedYharon)
                scale *= 1.10f;

            return scale;
        }

        public int GetRightChargeFrames() => DownedGolem ? 45 : 90;

        internal YCRightLaserVisualTier GetRightLaserTier()
        {
            if (DownedYharon)
                return YCRightLaserVisualTier.Yharon;
            if (DownedProvidence)
                return YCRightLaserVisualTier.Providence;
            if (DownedMoonLord)
                return YCRightLaserVisualTier.MoonLord;
            if (DownedWallOfFlesh)
                return YCRightLaserVisualTier.Classic;

            return YCRightLaserVisualTier.Cell;
        }

        public int GetPassiveManaRestore(Player player)
        {
            if (DownedProvidence && player.statLife <= player.statLifeMax2 / 2)
                return 5;
            if (DownedGolem)
                return 4;
            if (DownedEvilTier1)
                return 3;

            return 1;
        }

        public int GetPassiveLifeRestore(Player player)
        {
            if (DownedProvidence && player.statLife <= player.statLifeMax2 / 2)
                return 10;
            if (DownedGolem)
                return 8;
            if (DownedEvilTier1)
                return 7;

            return 5;
        }

        public int GetFireDebuffType()
        {
            if (DownedSupremeCalamitas)
                return ModContent.BuffType<TrueVulnerabilityHex>();
            if (DownedYharon)
                return ModContent.BuffType<Dragonfire>();
            if (DownedDoG)
                return ModContent.BuffType<GodSlayerInferno>();
            if (DownedProvidence)
                return ModContent.BuffType<BanishingFire>();
            if (DownedMoonLord)
                return ModContent.BuffType<HolyFlames>();
            if (NPC.downedAncientCultist)
                return ModContent.BuffType<Daybroken>();
            if (DownedBrimstoneElemental)
                return ModContent.BuffType<BrimstoneFlames>();
            if (DownedWallOfFlesh)
                return ModContent.BuffType<DemonicFlames>();
            if (DownedDesertScourge)
                return BuffID.OnFire3;

            return BuffID.OnFire;
        }

        public bool ShouldShardReleaseBrimstoneMissiles() => DownedBrimstoneElemental;
        public bool ShouldShardReleaseRainbowBolts() => DownedEmpress;
        public bool UltimateEmpowersAfterUse() => DownedYharon;

        public string BuildProgressionSummary()
        {
            List<string> parts = new();

            parts.Add("成长链: 荒灾/邪恶T1/血肉墙/硫磺火/石巨人/光女/月亮领主/亵渎/犽戎。");
            parts.Add($"剑形态: 初始大小 {BaseLeftBladeScale:0.00}，当前 {GetLeftBladeScale():0.00}。");

            if (DownedEvilTier1)
                parts.Add("邪恶T1: 地狱之刃回复提升，压力木巨刃大小 +15%。");
            if (DownedWallOfFlesh)
                parts.Add("血肉墙: 右键从细胞式短激光升级为传统持续激光。");
            if (DownedBrimstoneElemental)
                parts.Add("硫磺火: 压力木巨刃大小 +10%，燃火碎片死亡时释放左旋硫火飞弹。");
            if (DownedGolem)
                parts.Add("石巨人: 右键蓄力时长减半，回复提升，剑形态大小 +10%。");
            if (DownedEmpress)
                parts.Add("光女: 燃火碎片死亡时追加 7 个七彩矢，剑形态大小 +10%。");
            if (DownedMoonLord)
                parts.Add("月亮领主: 右键激光改为月总式大光束视觉，剑形态大小 +10%。");
            if (DownedProvidence)
                parts.Add("亵渎: 低血量回复进一步提升，右键激光改为亵渎新星式视觉，剑形态大小 +10%。");
            if (DownedYharon)
                parts.Add("犽戎: 右键激光改为细化的焚天龙卷视觉，大招使用后强化当前主武器 8 秒。");

            return string.Join("\n", parts);
        }

        private int GetValueForStage(int[] values, int stageIndex)
        {
            if (values == null || values.Length == 0)
                return 1;

            int clampedIndex = Utils.Clamp(stageIndex, 0, values.Length - 1);
            return System.Math.Max(1, values[clampedIndex]);
        }

        public static bool DownedDesertScourge => DownedBossSystem.downedDesertScourge;
        public static bool DownedEvilTier1 => NPC.downedBoss2;
        public static bool DownedWallOfFlesh => Main.hardMode;
        public static bool DownedBrimstoneElemental => DownedBossSystem.downedBrimstoneElemental;
        public static bool DownedGolem => NPC.downedGolemBoss;
        public static bool DownedEmpress => NPC.downedEmpressOfLight;
        public static bool DownedMoonLord => NPC.downedMoonlord;
        public static bool DownedProvidence => DownedBossSystem.downedProvidence;
        public static bool DownedDoG => DownedBossSystem.downedDoG;
        public static bool DownedYharon => DownedBossSystem.downedYharon;
        public static bool DownedSupremeCalamitas => DownedBossSystem.downedCalamitas;
    }
}
