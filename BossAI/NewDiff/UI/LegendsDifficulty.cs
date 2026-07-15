using System.Collections.Generic;
using CalamityLegendsComeBack.BossAI.NewDiff.Core.Systems;
using CalamityMod.Systems;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static CalamityMod.Systems.DifficultyModeSystem;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.UI
{
    public class LegendsDifficulty : DifficultyMode
    {
        public override bool Enabled
        {
            get => LegendsWorldSystem.LegendsModeEnabled;
            set
            {
                LegendsWorldSystem.LegendsModeEnabled = value;

                if (!value)
                    return;

                CalamityWorld.revenge = true;
                if (!Main.GameModeInfo.IsJourneyMode)
                    Main.GameMode = BackBoneGameModeID;
                else
                    AlignJourneyDifficultySlider();
            }
        }

        public override Asset<Texture2D> Texture => _texture ??= ModContent.Request<Texture2D>("CalamityLegendsComeBack/BossAI/NewDiff/Assets/UI/LegendsIcon");

        public override Asset<Texture2D> TextureDisabled => _textureDisabled ??= ModContent.Request<Texture2D>("CalamityLegendsComeBack/BossAI/NewDiff/Assets/UI/LegendsIcon_Off");

        public override Asset<Texture2D> OutlineTexture => _outlineTexture ??= ModContent.Request<Texture2D>("CalamityLegendsComeBack/BossAI/NewDiff/Assets/UI/LegendsIcon_Outline");

        public override SoundStyle ActivationSound => SoundID.Item4 with { Volume = 0.75f, Pitch = -0.18f };

        public override int BackBoneGameModeID => GameModeID.Expert;

        public override float DifficultyScale => 0.1f;

        public override LocalizedText Name => Language.GetText("Mods.CalamityLegendsComeBack.DifficultyUI.Name");

        public override Color ChatTextColor => new(88, 255, 211);

        public override LocalizedText ShortDescription => Language.GetText("Mods.CalamityLegendsComeBack.DifficultyUI.ShortDescription");

        public override LocalizedText ExpandedDescription => Language.GetText("Mods.CalamityLegendsComeBack.DifficultyUI.ExpandedDescription");

        public override int[] FavoredDifficultyAtTier(int tier)
        {
            DifficultyMode[] difficultyArray = DifficultyTiers[tier];
            List<int> favored = new();

            for (int i = 0; i < difficultyArray.Length; i++)
            {
                if (difficultyArray[i] is ExpertDifficulty || difficultyArray[i] is RevengeanceDifficulty)
                    favored.Add(i);
            }

            if (favored.Count <= 0)
                favored.Add(0);

            return favored.ToArray();
        }
    }
}
