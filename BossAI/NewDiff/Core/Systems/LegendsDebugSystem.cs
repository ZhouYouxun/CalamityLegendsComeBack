using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;
using CalamityLegendsComeBack.BossAI.NewDiff.Core.Configs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using Terraria.UI;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Core.Systems
{
    public class LegendsDebugSystem : ModSystem
    {
        private static LegendsDebugInfo currentInfo;

        private static float currentDistance;

        public override void PreUpdateNPCs() => ClearFrame();

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text", StringComparison.Ordinal));
            if (mouseTextIndex < 0)
                return;

            layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer("CalamityLegendsComeBack: NewDiff Debug", DrawDebugText, InterfaceScaleType.UI));
        }

        public static void Clear()
        {
            currentInfo = null;
            currentDistance = float.MaxValue;
        }

        internal static void Report(NPC npc, LegendsBossAI ai, LegendsGlobalNPC data)
        {
            if (Main.dedServ || Main.LocalPlayer is null || !npc.active)
                return;

            float distance = Vector2.Distance(Main.LocalPlayer.Center, npc.Center);
            if (currentInfo is not null && distance >= currentDistance)
                return;

            currentDistance = distance;
            currentInfo = new LegendsDebugInfo(
                ai.BossName,
                data.CurrentPhase,
                ai.MaxPhaseCount,
                ai.PhaseName(data.CurrentPhase),
                ai.StateName(data),
                data.PatternTimer,
                npc.lifeMax <= 0 ? 1f : npc.life / (float)npc.lifeMax,
                ai.DebugColor);
        }

        private static void ClearFrame()
        {
            currentInfo = null;
            currentDistance = float.MaxValue;
        }

        private static bool DrawDebugText()
        {
            if (!LegendsWorldSystem.LegendsModeEnabled || Main.gameMenu || LegendsClientConfig.Instance?.ShowBossAIDebugText == false)
                return true;

            string text = currentInfo is null
                ? "Legends DEBUG\nBoss: none\nMode: waiting for tracked boss"
                : $"Legends DEBUG\nBoss: {currentInfo.BossName}\nPhase: {currentInfo.Phase}/{currentInfo.MaxPhase} - {currentInfo.PhaseName}\nState: {currentInfo.StateName}\nTimer: {currentInfo.Timer}\nLife: {currentInfo.LifeRatio:P0}";

            string[] lines = text.Split('\n');
            Vector2 position = new(18f, Main.screenHeight - 18f - lines.Length * 18f);
            DrawLines(lines, position, currentInfo?.Color ?? new Color(88, 255, 211));
            return true;
        }

        private static void DrawLines(string[] lines, Vector2 position, Color color)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;

            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 linePosition = position + Vector2.UnitY * i * 18f;
                ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, lines[i], linePosition, color, 0f, Vector2.Zero, Vector2.One * 0.85f);
            }
        }

        private sealed record LegendsDebugInfo(string BossName, int Phase, int MaxPhase, string PhaseName, string StateName, int Timer, float LifeRatio, Color Color);
    }
}
