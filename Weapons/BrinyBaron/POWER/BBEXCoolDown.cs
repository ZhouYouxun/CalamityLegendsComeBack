using System;
using CalamityMod.Cooldowns;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.Localization;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.POWER
{
    internal class BBEXCoolDown : CooldownHandler
    {
        private float AdjustedCompletion =>
            instance.player.GetModPlayer<BBEXPlayer>().TideValue / (float)instance.player.GetModPlayer<BBEXPlayer>().CurrentTideMax;
        private Color TextColor => Color.AliceBlue;
        private Color TextBorderColor => Color.Black;

        public static new string ID => "BrinyBaron_Tide";
        public override bool CanTickDown => false;

        public override bool ShouldDisplay =>
            instance.player.HeldItem.type == ModContent.ItemType<NewLegendBrinyBaron>();

        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.BrinyBaron_Tide");

        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/POWER/BBEXCoolDown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/BrinyBaron/POWER/BBEXCoolDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/BrinyBaron/POWER/BBEXCoolDownOverlay";

        public override Color OutlineColor => Color.DarkSlateGray;

        public override Color CooldownStartColor =>
            Color.Lerp(new Color(35, 125, 190), new Color(15, 55, 95), instance.Completion);

        public override Color CooldownEndColor =>
            Color.Lerp(new Color(145, 235, 255), new Color(30, 90, 120), instance.Completion);

        public override void ApplyBarShaders(float opacity)
        {
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseOpacity(opacity);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseSaturation(AdjustedCompletion);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseColor(CooldownStartColor);
            GameShaders.Misc["CalamityMod:CircularBarShader"].UseSecondaryColor(CooldownEndColor);
            GameShaders.Misc["CalamityMod:CircularBarShader"].Apply();
        }

        public override void DrawExpanded(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            base.DrawExpanded(spriteBatch, position, opacity, scale);

            DrawBorderStringEightWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                instance.timeLeft.ToString(),
                position + new Vector2(-6f, 10f) * scale,
                TextColor,
                TextBorderColor,
                scale);
        }

        public override void DrawCompact(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            Texture2D sprite = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D outline = ModContent.Request<Texture2D>(OutlineTexture).Value;
            Texture2D overlay = ModContent.Request<Texture2D>(OverlayTexture).Value;

            spriteBatch.Draw(outline, position, null, OutlineColor * opacity, 0f, outline.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(sprite, position, null, Color.White * opacity, 0f, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            int lostHeight = (int)Math.Ceiling(overlay.Height * AdjustedCompletion);
            Rectangle crop = new Rectangle(0, lostHeight, overlay.Width, overlay.Height - lostHeight);

            spriteBatch.Draw(
                overlay,
                position + Vector2.UnitY * lostHeight * scale,
                crop,
                OutlineColor * opacity * 0.9f,
                0f,
                sprite.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0f);

            DrawBorderStringEightWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                instance.player.GetModPlayer<BBEXPlayer>().TideValue.ToString(),
                position + new Vector2(-6f, 10f) * scale,
                TextColor,
                TextBorderColor,
                scale);
        }
    }
}
