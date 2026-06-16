using CalamityMod;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal static class SS_Balance
    {
        // 14 stages:
        // Initial, EoC, Evil, Skeletron, Hardmode, Mech, Plantera, Golem,
        // MoonLord, Providence, Polterghast, DoG, Yharon, Exo+SC
        //
        // SeasSearing fires 3-round bursts (left click) and a resonance pulse
        // (right click at Projectile.damage / 8). Both share this base damage.
        // The weapon is crafted with Hardmode Calamity materials and becomes
        // meaningfully available around the Mechanical Boss stage.

        public static readonly int[] BaseDamage =
        {
            18,     // Initial
            26,     // Eye of Cthulhu
            36,     // Evil Boss
            48,     // Skeletron
            64,     // Hardmode
            96,     // Any Mechanical Boss  ← intended first-available stage
            140,    // Plantera
            188,    // Golem
            256,    // Moon Lord
            332,    // Providence
            444,    // Polterghast
            586,    // Devourer of Gods
            756,    // Yharon
            9550,   // Exo Mechs & Supreme Calamitas
        };

        public static int GetBaseDamage()
        {
            int stage = Utils.Clamp(GetCompletedStageIndex(), 0, BaseDamage.Length - 1);
            return System.Math.Max(1, BaseDamage[stage]);
        }

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
                if (!clearedStages[i])
                    break;

                stageIndex = i + 1;
            }

            return stageIndex;
        }
    }
}
