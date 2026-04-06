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

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill
{
    internal class YCEXCoolDown : CooldownHandler
    {
        private YCEXPlayer ExPlayer => instance.player.GetModPlayer<YCEXPlayer>();
        private float AdjustedCompletion => MathHelper.Clamp(ExPlayer.DisplayCompletion, 0f, 1f);
        private int DisplayValue => ExPlayer.DisplayValue;

        private Color TextColor => Color.AliceBlue;
        private Color TextBorderColor => Color.Black;

        public static new string ID => "YharimsCrystal_EX";

        public override bool CanTickDown => false;

        public override bool ShouldDisplay =>
            instance.player.HeldItem.type == ModContent.ItemType<NewLegendYharimsCrystal>();

        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.YC_EX");

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/EXSkill/YCEXCoolDown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/EXSkill/YCEXCoolDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/EXSkill/YCEXCoolDownOverlay";

        public override Color OutlineColor => Color.Black;

        public override Color CooldownStartColor =>
            ExPlayer.IsCoolingDown
                ? Color.Lerp(new Color(120, 52, 22), new Color(220, 150, 65), AdjustedCompletion)
                : Color.Lerp(new Color(155, 110, 32), new Color(255, 208, 90), AdjustedCompletion);

        public override Color CooldownEndColor =>
            ExPlayer.IsCoolingDown
                ? Color.Lerp(new Color(28, 18, 12), new Color(150, 82, 35), AdjustedCompletion)
                : Color.Lerp(new Color(255, 235, 165), Color.White, AdjustedCompletion);

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

            Vector2 textOffset = new Vector2(DisplayValue > 99 ? -15f : DisplayValue > 9 ? -11f : -6f, 10f);

            DrawBorderStringEightWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                DisplayValue.ToString(),
                position + textOffset * scale,
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

            Vector2 textOffset = new Vector2(DisplayValue > 99 ? -15f : DisplayValue > 9 ? -11f : -6f, 10f);

            DrawBorderStringEightWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                DisplayValue.ToString(),
                position + textOffset * scale,
                TextColor,
                TextBorderColor,
                scale);
        }
    }
}
