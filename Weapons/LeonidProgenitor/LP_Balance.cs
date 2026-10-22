using CalamityLegendsComeBack.Systems;
using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    internal static class LP_Balance
    {
        private const string SourceFile = "Weapons/LeonidProgenitor/LP_Balance.cs";

        // 14 stages:
        // Initial, EoC, Evil, Skeletron, Hardmode, Mech, Plantera, Golem,
        // MoonLord, Providence, Polterghast, DoG, Yharon, Exo+SC
        //
        // 每行一个时期，三列依次为：通用面板伤害、左键潜行倍率、右键潜行倍率。
        // 右键现在仅能在满潜行时蓄力释放，倍率直接控制整套右键攻击伤害。
        // 倍率只在发射源应用一次；本体、爆炸、衍生弹幕都从该源伤害按原比例继承。
        private static readonly float[,] DefaultAttackDamageTable =
        {
            // Panel   Left stealth   Right stealth
            {    18,          1.0f,          2.2f }, // Initial
            {    21,          1.0f,          2.2f }, // Eye of Cthulhu
            {    25,          1.0f,          2.2f }, // Evil Boss
            {    29,          1.0f,          2.2f }, // Skeletron
            {    39,          1.0f,          2.2f }, // Hardmode
            {    48,          1.0f,          2.2f }, // Any Mechanical Boss
            {    58,          1.0f,          2.2f }, // Plantera
            {    77,          1.0f,          2.2f }, // Golem
            {   134,          1.0f,          2.2f }, // Moon Lord
            {   175,          1.0f,          2.2f }, // Providence
            {   264,          1.0f,          2.2f }, // Polterghast
            {   347,          1.0f,          2.2f }, // Devourer of Gods
            {   448,          1.0f,          2.2f }, // Yharon
            {  5673,          1.0f,          2.2f }, // Exo Mechs & Supreme Calamitas
        };

        private static float GetAttackValue(int column, int stage)
        {
            stage = Utils.Clamp(stage, 0, DefaultAttackDamageTable.GetLength(0) - 1);
            return RuntimeBalanceData.GetSourceTableFloat(SourceFile, nameof(DefaultAttackDamageTable), stage, column,
                DefaultAttackDamageTable[stage, column]);
        }

        public static int GetLeftClickBaseDamage() => System.Math.Max(1, (int)GetAttackValue(0, GetCompletedStageIndex()));
        public static float GetStealthDamageMultiplier(bool rightClick) =>
            System.Math.Max(0f, GetAttackValue(rightClick ? 2 : 1, GetCompletedStageIndex()));
        public static int GetInitialLeftClickBaseDamage() => System.Math.Max(1, (int)GetAttackValue(0, 0));

        public static int GetCompletedStageIndex()
        {
            bool[] clearedStages =
            {
                NPC.downedBoss1,
                NPC.downedBoss2,
                NPC.downedBoss3,
                Main.hardMode,
                NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3,
                NPC.downedPlantBoss,
                NPC.downedGolemBoss,
                NPC.downedMoonlord,
                DownedBossSystem.downedProvidence,
                DownedBossSystem.downedPolterghast,
                DownedBossSystem.downedDoG,
                DownedBossSystem.downedYharon,
                DownedBossSystem.downedExoMechs && DownedBossSystem.downedCalamitas
            };

            int stageIndex = 0;
            for (int i = 0; i < clearedStages.Length; i++)
            {
                if (clearedStages[i])
                    stageIndex = i + 1;
            }

            return stageIndex;
        }
    }
}
