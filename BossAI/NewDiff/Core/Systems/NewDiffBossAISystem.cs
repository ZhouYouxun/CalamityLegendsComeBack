using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.UI;
using CalamityMod.Systems;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Core.Systems
{
    internal sealed class NewDiffBossAISystem : ModSystem
    {
        public override void Load()
        {
            DifficultyModeSystem.Difficulties.Add(new IUMWDifficulty());
            DifficultyModeSystem.CalculateDifficultyData();
        }

        public override void PostSetupContent()
        {
            IUMWBossAIRegistry.Load();
        }

        public override void Unload()
        {
            IUMWBossAIRegistry.Unload();
        }
    }
}
