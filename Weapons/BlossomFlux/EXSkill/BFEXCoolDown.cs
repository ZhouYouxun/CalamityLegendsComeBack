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

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill
{
    internal sealed class BFEXCooldown : CooldownHandler
    {
        private BFEXPlayer EXPlayer => instance.player.GetModPlayer<BFEXPlayer>();
        private float AdjustedCompletion => instance.timeLeft / (float)BFEXPlayer.EXMax;

        public static new string ID => "BlossomFlux_EX";
        public override bool CanTickDown => false;
        public override bool ShouldDisplay => EXPlayer.ShouldShowDisplay;

        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.BlossomFlux_EX");

        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/POWER/BBEXCoolDown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/BrinyBaron/POWER/BBEXCoolDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/BrinyBaron/POWER/BBEXCoolDownOverlay";

        public override Color OutlineColor => new Color(18, 56, 24);
        public override Color CooldownStartColor => new Color(88, 255, 148);
        public override Color CooldownEndColor => new Color(212, 255, 220);

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
                position + new Vector2(instance.timeLeft > 9 ? -11f : -6f, 10f) * scale,
                Color.White,
                Color.Black,
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
                OutlineColor * opacity * 0.88f,
                0f,
                sprite.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0f);

            DrawBorderStringEightWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                instance.timeLeft.ToString(),
                position + new Vector2(instance.timeLeft > 9 ? -11f : -6f, 10f) * scale,
                Color.White,
                Color.Black,
                scale);
        }
    }
}
