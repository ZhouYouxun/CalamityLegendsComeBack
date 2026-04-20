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

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Passive
{
    internal class BFPassiveCoolDown : CooldownHandler
    {
        private BFPassivePlayer PassivePlayer => instance.player.GetModPlayer<BFPassivePlayer>();
        private int CurrentMaxFrames => PassivePlayer.FinalStandActive ? BFPassivePlayer.FinalStandDurationFrames : BFPassivePlayer.PassiveCooldownFrames;
        private float AdjustedCompletion => CurrentMaxFrames <= 0 ? 0f : instance.timeLeft / (float)CurrentMaxFrames;
        private int DisplayValue => instance.timeLeft <= 0 ? 0 : (int)Math.Ceiling(instance.timeLeft / 60f);

        public static new string ID => "BlossomFlux_Passive";
        public override bool CanTickDown => false;

        public override bool ShouldDisplay => PassivePlayer.ShouldShowCooldownDisplay;

        public override LocalizedText DisplayName =>
            Language.GetText("Mods.CalamityLegendsComeBack.Cooldowns.BlossomFlux_Passive");

        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/POWER/BBEXCoolDown";
        public override string OutlineTexture => "CalamityLegendsComeBack/Weapons/BrinyBaron/POWER/BBEXCoolDownOutline";
        public override string OverlayTexture => "CalamityLegendsComeBack/Weapons/BrinyBaron/POWER/BBEXCoolDownOverlay";

        public override Color OutlineColor => PassivePlayer.FinalStandActive ? new Color(82, 20, 20) : new Color(24, 48, 24);

        public override Color CooldownStartColor =>
            PassivePlayer.FinalStandActive
                ? new Color(255, 120, 108)
                : new Color(126, 236, 134);

        public override Color CooldownEndColor =>
            PassivePlayer.FinalStandActive
                ? new Color(255, 214, 190)
                : new Color(218, 255, 198);

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
                DisplayValue.ToString(),
                position + new Vector2(-6f, 10f) * scale,
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
                DisplayValue.ToString(),
                position + new Vector2(-6f, 10f) * scale,
                Color.White,
                Color.Black,
                scale);
        }
    }
}
