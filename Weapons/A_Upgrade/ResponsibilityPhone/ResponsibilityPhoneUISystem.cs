using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.ResponsibilityPhone
{
    internal sealed class ResponsibilityPhoneUISystem : ModSystem
    {
        internal const float CancelRadius = 22f;
        private const float SlotRadius = 70f;
        private const float SectorLineRadius = 92f;
        private const int SlotFrameSize = 50;
        private const int InnerFrameSize = 38;
        private const int BorderThickness = 2;

        private float wheelOpacity;

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
            if (mouseTextIndex < 0)
                return;

            layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                "CalamityLegendsComeBack: Responsibility Phone Wheel",
                DrawWheel,
                InterfaceScaleType.None));
        }

        private bool DrawWheel()
        {
            if (Main.gameMenu || Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers)
            {
                wheelOpacity = 0f;
                return true;
            }

            ResponsibilityPhonePlayer phonePlayer = Main.LocalPlayer.GetModPlayer<ResponsibilityPhonePlayer>();
            if (!phonePlayer.WheelOpen || ResponsibilityLanguageRegistry.Count <= 0)
            {
                wheelOpacity = 0f;
                return true;
            }

            wheelOpacity = MathHelper.Clamp(wheelOpacity + 0.16f, 0f, 1f);
            SpriteBatch spriteBatch = Main.spriteBatch;
            Vector2 center = phonePlayer.WheelCenter.Floor();
            float time = Main.GlobalTimeWrappedHourly;
            float sector = MathHelper.TwoPi / ResponsibilityLanguageRegistry.Count;

            Texture2D innerRing = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_03").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color ringColor = new Color(82, 207, 255) * (0.25f * wheelOpacity);
            Main.EntitySpriteDraw(innerRing, center, null, ringColor, -time * 0.5f,
                innerRing.Size() * 0.5f, 0.38f, SpriteEffects.None, 0f);

            ResponsibilityLanguageDefinition selected = ResponsibilityLanguageRegistry.Get(phonePlayer.SelectedLanguageIndex);
            if (selected != null)
            {
                Main.EntitySpriteDraw(bloom, center, null, selected.Color * (0.26f * wheelOpacity), 0f,
                    bloom.Size() * 0.5f, 0.17f, SpriteEffects.None, 0f);
            }

            DrawSectorLines(spriteBatch, center, sector, phonePlayer.WheelHoverIndex, wheelOpacity);

            for (int index = 0; index < ResponsibilityLanguageRegistry.Count; index++)
            {
                ResponsibilityLanguageDefinition definition = ResponsibilityLanguageRegistry.Get(index);
                bool highlighted = phonePlayer.WheelHoverIndex == index;
                bool isSelected = phonePlayer.SelectedLanguageIndex == index;
                float angle = index * sector;
                Vector2 slotCenter = center + angle.ToRotationVector2() * SlotRadius;
                Rectangle slotArea = Utils.CenteredRectangle(slotCenter, new Vector2(SlotFrameSize));

                DrawSlotFrame(spriteBatch, slotArea, definition.Color, highlighted, isSelected, wheelOpacity);

                float symbolScale = highlighted ? 1.08f : isSelected ? 1f : 0.92f;
                Vector2 symbolSize = FontAssets.MouseText.Value.MeasureString(definition.Symbol);
                Utils.DrawBorderString(spriteBatch, definition.Symbol, slotCenter - symbolSize * symbolScale * 0.5f,
                    highlighted ? Color.White * wheelOpacity : definition.AccentColor * wheelOpacity, symbolScale);
            }

            string centerText = phonePlayer.ArmyActive
                ? Language.GetTextValue("Mods.CalamityLegendsComeBack.UI.ResponsibilityPhone.Return")
                : "脳";
            Vector2 centerSize = FontAssets.MouseText.Value.MeasureString(centerText);
            Color centerColor = phonePlayer.ArmyActive ? new Color(194, 255, 67) : Color.LightGray;
            Utils.DrawBorderString(spriteBatch, centerText, center - centerSize * 0.42f,
                centerColor * wheelOpacity, 0.84f);
            return true;
        }

        private static void DrawSectorLines(SpriteBatch spriteBatch, Vector2 center, float sector, int hoveredIndex, float opacity)
        {
            Color lineColor = new Color(94, 181, 205) * (0.26f * opacity);
            for (int index = 0; index < ResponsibilityLanguageRegistry.Count; index++)
            {
                float boundaryAngle = (index + 0.5f) * sector;
                Vector2 direction = boundaryAngle.ToRotationVector2();
                DrawScreenLine(spriteBatch, center + direction * CancelRadius, center + direction * SectorLineRadius, lineColor, 1.4f);
            }

            if (hoveredIndex < 0)
                return;

            ResponsibilityLanguageDefinition hovered = ResponsibilityLanguageRegistry.Get(hoveredIndex);
            Vector2 hoveredDirection = (hoveredIndex * sector).ToRotationVector2();
            DrawScreenLine(spriteBatch, center + hoveredDirection * CancelRadius,
                center + hoveredDirection * (SectorLineRadius * 0.9f), hovered.Color * (0.58f * opacity), 3f);
        }

        private static void DrawSlotFrame(SpriteBatch spriteBatch, Rectangle slotArea, Color languageColor, bool hovered, bool selected, float opacity)
        {
            Color slotBack = Color.Lerp(new Color(13, 28, 35), languageColor, selected ? 0.25f : 0.12f);
            Color slotBorder = Color.Lerp(new Color(72, 124, 139), languageColor, selected ? 0.56f : 0.3f);
            if (hovered)
            {
                slotBack = Color.Lerp(slotBack, new Color(75, 99, 105), 0.42f);
                slotBorder = Color.Lerp(slotBorder, Color.White, 0.38f);
            }

            DrawRectangle(spriteBatch, slotArea, slotBack * (0.82f * opacity));
            DrawBorder(spriteBatch, slotArea, slotBorder * (0.94f * opacity), BorderThickness);

            Rectangle innerArea = Utils.CenteredRectangle(slotArea.Center.ToVector2(), new Vector2(InnerFrameSize));
            DrawRectangle(spriteBatch, innerArea, Color.Lerp(new Color(7, 17, 22), languageColor, 0.08f) * (0.76f * opacity));
            DrawBorder(spriteBatch, innerArea, slotBorder * (0.68f * opacity), 1);
        }

        private static void DrawScreenLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 edge = end - start;
            if (edge.LengthSquared() < 0.01f)
                return;

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, start, new Rectangle(0, 0, 1, 1), color,
                edge.ToRotation(), new Vector2(0f, 0.5f), new Vector2(edge.Length(), thickness), SpriteEffects.None, 0f);
        }

        private static void DrawRectangle(SpriteBatch spriteBatch, Rectangle rectangle, Color color)
            => spriteBatch.Draw(TextureAssets.MagicPixel.Value, rectangle, new Rectangle(0, 0, 1, 1), color);

        private static void DrawBorder(SpriteBatch spriteBatch, Rectangle rectangle, Color color, int thickness)
        {
            DrawRectangle(spriteBatch, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
            DrawRectangle(spriteBatch, new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
            DrawRectangle(spriteBatch, new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
            DrawRectangle(spriteBatch, new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height), color);
        }
    }
}
