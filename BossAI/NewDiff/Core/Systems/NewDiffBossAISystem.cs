using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Core.Systems
{
    internal sealed class NewDiffBossAISystem : ModSystem
    {
        public override void Load()
        {
            // Temporarily keep the custom mode out of Calamity's difficulty icon row.
            // DifficultyModeSystem.Difficulties.Add(new LegendsDifficulty());
            // DifficultyModeSystem.CalculateDifficultyData();
        }

        public override void PostSetupContent()
        {
            // Boss AI rebuilds are temporarily hidden during the final cleanup.
            // LegendsBossAIRegistry.Load();
        }

        public override void Unload()
        {
            // LegendsBossAIRegistry.Unload();
        }
    }
}
